using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 塗りキャンバスの実装
/// 2D配列で塗り状態を管理し、Phase 1の基本機能とPhase 2のクリエイティブモードで使用
/// </summary>
public class PaintCanvas : MonoBehaviour, IPaintCanvas
{
    [Header("Settings")]
    [Tooltip("オフラインモード用の設定（優先度: 高）")]
    [SerializeField] private PaintSettings offlineSettings;
    
    [Tooltip("オンラインモード用の設定（優先度: 高）")]
    [SerializeField] private PaintSettings onlineSettings;
    
    [Tooltip("デフォルト設定（後方互換性用、offlineSettings/onlineSettingsが未設定の場合に使用）")]
    [SerializeField] private PaintSettings settings;
    
    [Header("Display Reference")]
    [Tooltip("表示サイズの基準となるUIオブジェクト（PaintSpaceImageなど）")]
    [SerializeField] private RectTransform paintSpaceImage;
    
    [Header("Current Status (Read Only)")]
    [Tooltip("現在使用中の設定モード（自動判定）")]
    [SerializeField] private string currentModeStatus = "未判定";
    
    [Tooltip("現在使用中の設定（自動選択）")]
    [SerializeField] private PaintSettings currentActiveSettings;
    
    [Header("Debug")]
    [Tooltip("塗り位置をデバッグ表示するか")]
    public bool showDebugGizmos = false;
    
    [Tooltip("ネットワーク経由の塗り処理のデバッグログを出力するか")]
    [SerializeField] private bool enableNetworkPaintDebugLog = false;
    
    // 塗り状態を管理する2D配列（playerIdを記録）
    private int[,] paintData;
    
    // Phase 2で追加：色データと強度データ
    private Color[,] colorData;
    private float[,] intensityData;
    
    // Phase 4で追加：タイムスタンプデータ（各ピクセルが最後に塗られた時刻）
    private float[,] paintTimestamp;
    
    // Phase 2で追加：内部Texture2D（描画・保存用）
    private Texture2D canvasTexture;
    
    // イベント
    public event System.Action<Vector2, int, float> OnPaintCompleted;
    public event System.Action OnPaintingSuppressed;
    public event System.Action OnTextureUpdated; // テクスチャ更新イベント（PaintRenderer用）
    
    // タイムスタンプ取得用のコールバック（ネットワーク同期用）
    private System.Func<float> getTimestampCallback;
    
    // 内部状態
    private int frameCount = 0;
    private bool isInitialized = false;
    private bool textureNeedsFlush = false; // テクスチャの更新が必要かどうか
    private int lastPaintFrame = -1; // 最後に塗りが実行されたフレーム
    private int textureUpdateFrameCount = 0; // テクスチャ更新のフレームカウント
    
    // ピクセル数のキャッシュ（パフォーマンス最適化）
    private int cachedPlayerPixelCount = 0;
    private int cachedEnemyPixelCount = 0;
    private bool pixelCountCacheValid = false; // キャッシュが有効かどうか
    
    // 補間処理中のイベント発火抑制フラグ（ネットワーク送信最適化用）
    private bool suppressEventFiring = false;
    private Vector2 lastInterpolationEndPosition = Vector2.zero;
    private int lastInterpolationPlayerId = 0;
    private float lastInterpolationIntensity = 0f;
    
    void Awake()
    {
        // オンライン/オフライン判定に基づいて適切な設定を選択
        SelectSettings();
        
        if (settings == null)
        {
            return;
        }
        
        InitializeCanvas();
    }
    
    /// <summary>
    /// オンライン/オフライン判定に基づいて適切な設定を選択
    /// </summary>
    private void SelectSettings()
    {
        bool isOnline = IsOnlineMode();
        
        if (isOnline)
        {
            // オンラインモード: onlineSettingsを優先、未設定の場合はsettingsを使用
            if (onlineSettings != null)
            {
                settings = onlineSettings;
                currentModeStatus = "オンライン";
                currentActiveSettings = onlineSettings;
            }
            else if (settings != null)
            {
                currentModeStatus = "オンライン（デフォルト設定使用）";
                currentActiveSettings = settings;
            }
            else
            {
                currentModeStatus = "オンライン（設定未設定）";
                currentActiveSettings = null;
            }
        }
        else
        {
            // オフラインモード: offlineSettingsを優先、未設定の場合はsettingsを使用
            if (offlineSettings != null)
            {
                settings = offlineSettings;
                currentModeStatus = "オフライン";
                currentActiveSettings = offlineSettings;
            }
            else if (settings != null)
            {
                currentModeStatus = "オフライン（デフォルト設定使用）";
                currentActiveSettings = settings;
            }
            else
            {
                currentModeStatus = "オフライン（設定未設定）";
                currentActiveSettings = null;
            }
        }
    }
    
    /// <summary>
    /// オンラインモードかどうかを判定
    /// </summary>
    private bool IsOnlineMode()
    {
        // Unity NetcodeのNetworkManagerが接続されているかどうかで判定
        if (NetworkManager.Singleton != null)
        {
            return NetworkManager.Singleton.IsClient || 
                   NetworkManager.Singleton.IsServer;
        }
        
        // フォールバック: GameModeManagerを使用
        if (GameModeManager.Instance != null)
        {
            return GameModeManager.Instance.IsOnlineMode;
        }
        
        return false;
    }
    
    void InitializeCanvas()
    {
        paintData = new int[settings.textureWidth, settings.textureHeight];
        colorData = new Color[settings.textureWidth, settings.textureHeight];
        intensityData = new float[settings.textureWidth, settings.textureHeight];
        paintTimestamp = new float[settings.textureWidth, settings.textureHeight];
        
        // 内部Texture2Dを作成
        canvasTexture = new Texture2D(settings.textureWidth, settings.textureHeight, TextureFormat.RGBA32, false);
        canvasTexture.filterMode = FilterMode.Bilinear;
        canvasTexture.wrapMode = TextureWrapMode.Clamp;
        
        // 初期化：全て0（未塗り）にする
        ResetCanvas();
        
        isInitialized = true;
    }
    
    /// <summary>
    /// 現在のキャンバス上のプレイヤー／敵ピクセル数を集計する（キャッシュを使用）
    /// </summary>
    /// <param name="playerPixels">playerId > 0 のピクセル数</param>
    /// <param name="enemyPixels">playerId == -1 のピクセル数</param>
    public void GetPlayerAndEnemyPixelCounts(out int playerPixels, out int enemyPixels)
    {
        if (!isInitialized || settings == null || paintData == null)
        {
            playerPixels = 0;
            enemyPixels = 0;
            return;
        }
        
        // キャッシュが無効な場合は再計算
        if (!pixelCountCacheValid)
        {
            RecalculatePixelCounts();
        }
        
        playerPixels = cachedPlayerPixelCount;
        enemyPixels = cachedEnemyPixelCount;
    }
    
    /// <summary>
    /// ピクセル数を再計算（キャッシュが無効な場合に呼ばれる）
    /// </summary>
    private void RecalculatePixelCounts()
    {
        cachedPlayerPixelCount = 0;
        cachedEnemyPixelCount = 0;
        
        if (!isInitialized || settings == null || paintData == null)
        {
            pixelCountCacheValid = true;
            return;
        }
        
        int width = settings.textureWidth;
        int height = settings.textureHeight;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int id = paintData[x, y];
                if (id > 0)
                {
                    cachedPlayerPixelCount++;
                }
                else if (id == -1)
                {
                    cachedEnemyPixelCount++;
                }
            }
        }
        
        pixelCountCacheValid = true;
    }
    
    /// <summary>
    /// ピクセル数のキャッシュを更新（塗り処理で呼ばれる）
    /// </summary>
    /// <param name="oldPlayerId">変更前のplayerId</param>
    /// <param name="newPlayerId">変更後のplayerId</param>
    private void UpdatePixelCountCache(int oldPlayerId, int newPlayerId)
    {
        // 変更前のカウントを減らす
        if (oldPlayerId > 0)
        {
            cachedPlayerPixelCount = Mathf.Max(0, cachedPlayerPixelCount - 1);
        }
        else if (oldPlayerId == -1)
        {
            cachedEnemyPixelCount = Mathf.Max(0, cachedEnemyPixelCount - 1);
        }
        
        // 変更後のカウントを増やす
        if (newPlayerId > 0)
        {
            cachedPlayerPixelCount++;
        }
        else if (newPlayerId == -1)
        {
            cachedEnemyPixelCount++;
        }
    }
    
    /// <summary>
    /// 指定位置に色を塗る（後方互換性のため、デフォルト色は白）
    /// </summary>
    public void PaintAt(Vector2 screenPosition, int playerId, float intensity)
    {
        PaintAt(screenPosition, playerId, intensity, Color.white);
    }
    
    /// <summary>
    /// 指定位置に色を塗る（Phase 2で追加：色パラメータ付き）
    /// </summary>
    public void PaintAt(Vector2 screenPosition, int playerId, float intensity, Color color)
    {
        if (!isInitialized || settings == null)
        {
            return;
        }
        
        // 更新頻度チェック
        frameCount++;
        if (frameCount % settings.updateFrequency != 0)
        {
            return;
        }
        
        // 画面座標をキャンバス座標に変換
        int canvasX = Mathf.RoundToInt((screenPosition.x / Screen.width) * settings.textureWidth);
        int canvasY = Mathf.RoundToInt((screenPosition.y / Screen.height) * settings.textureHeight);
        
        // 範囲チェック
        if (canvasX < 0 || canvasX >= settings.textureWidth || 
            canvasY < 0 || canvasY >= settings.textureHeight)
        {
            return;
        }
        
        // 塗り強度が閾値以上の場合のみ塗る
        float effectiveIntensity = intensity * settings.paintIntensityMultiplier;
        if (effectiveIntensity < settings.minVolumeThreshold)
        {
            return;
        }
        
        // 塗り処理（優先順位ロジックを追加）
        int existingPlayerId = paintData[canvasX, canvasY];
        
        // 敵の色を塗る場合、プレイヤーが既に塗っている領域は上塗りしない
        if (playerId == -1 && existingPlayerId > 0)
        {
            return;
        }
        
        // プレイヤーの色を塗る場合、敵の色を上塗り可能
        // （既存のロジックで上書きされるため、追加処理不要）
        
        // ピクセル数のキャッシュを更新
        if (pixelCountCacheValid)
        {
            UpdatePixelCountCache(existingPlayerId, playerId);
        }
        
        paintData[canvasX, canvasY] = playerId;
        colorData[canvasX, canvasY] = color;
        intensityData[canvasX, canvasY] = effectiveIntensity;
        
        // タイムスタンプを記録（ネットワーク同期用）
        if (getTimestampCallback != null)
        {
            paintTimestamp[canvasX, canvasY] = getTimestampCallback();
        }
        
        // テクスチャを更新
        UpdateTexturePixel(canvasX, canvasY, color);
        
        // テクスチャの更新をフラッシュ
        FlushTextureUpdates();
        
        // イベント発火（補間処理中は抑制）
        if (suppressEventFiring)
        {
            lastInterpolationEndPosition = screenPosition;
            lastInterpolationPlayerId = playerId;
            lastInterpolationIntensity = effectiveIntensity;
        }
        else
        {
            OnPaintCompleted?.Invoke(screenPosition, playerId, effectiveIntensity);
        }
        
    }
    
    /// <summary>
    /// 指定位置に色を塗る（アルファ値を指定可能）
    /// </summary>
    /// <param name="screenPosition">画面座標</param>
    /// <param name="playerId">プレイヤーID</param>
    /// <param name="intensity">塗り強度</param>
    /// <param name="color">色（アルファ値は無視される）</param>
    /// <param name="alpha">アルファ値（0.0～1.0）</param>
    public void PaintAtWithAlpha(Vector2 screenPosition, int playerId, float intensity, Color color, float alpha)
    {
        // アルファ値を設定した色を作成
        Color colorWithAlpha = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        
        // 既存のPaintAt()を呼び出す（アルファ値が設定された色で）
        PaintAt(screenPosition, playerId, intensity, colorWithAlpha);
    }
    
    /// <summary>
    /// 半径指定で塗る（ブラシタイプ用）
    /// </summary>
    public void PaintAtWithRadius(Vector2 screenPosition, int playerId, float intensity, Color color, float radius)
    {
        // デバッグログ（常に出力して、メソッドが呼ばれているか確認）
        Debug.Log($"[PaintCanvas] PaintAtWithRadius呼び出し - screenPosition: {screenPosition}, playerId: {playerId}, intensity: {intensity}, color: {color}, radius: {radius}, isInitialized: {isInitialized}, settings: {(settings != null ? "設定あり" : "null")}");
        
        PaintAtWithRadiusInternal(screenPosition, playerId, intensity, color, radius, checkUpdateFrequency: true);
    }

    /// <summary>
    /// 半径指定で塗る（更新頻度チェックをスキップする版、爆発時などに使用）
    /// </summary>
    public void PaintAtWithRadiusForced(Vector2 screenPosition, int playerId, float intensity, Color color, float radius)
    {
        PaintAtWithRadiusInternal(screenPosition, playerId, intensity, color, radius, checkUpdateFrequency: false);
    }

    /// <summary>
    /// 半径指定で塗る（内部実装）
    /// </summary>
    private void PaintAtWithRadiusInternal(Vector2 screenPosition, int playerId, float intensity, Color color, float radius, bool checkUpdateFrequency)
    {
        if (!isInitialized || settings == null)
        {
            if (enableNetworkPaintDebugLog)
            {
                Debug.LogWarning($"[PaintCanvas] PaintAtWithRadiusInternal - 初期化されていません (isInitialized: {isInitialized}, settings: {settings})");
            }
            return;
        }
        
        // 更新頻度チェック（checkUpdateFrequency が true の場合のみ）
        if (checkUpdateFrequency)
        {
            frameCount++;
            if (frameCount % settings.updateFrequency != 0)
            {
                if (enableNetworkPaintDebugLog)
                {
                    Debug.Log($"[PaintCanvas] PaintAtWithRadiusInternal - updateFrequencyによる間引き (frameCount: {frameCount}, updateFrequency: {settings.updateFrequency})");
                }
                return;
            }
        }
        
        // 画面座標をキャンバス座標に変換
        int centerX = Mathf.RoundToInt((screenPosition.x / Screen.width) * settings.textureWidth);
        int centerY = Mathf.RoundToInt((screenPosition.y / Screen.height) * settings.textureHeight);
        
        // 半径をキャンバス座標系に変換
        float radiusInCanvas = (radius / Screen.width) * settings.textureWidth;
        int radiusPixels = Mathf.RoundToInt(radiusInCanvas);
        
        // 塗り強度が閾値以上の場合のみ塗る
        float effectiveIntensity = intensity * settings.paintIntensityMultiplier;
        if (effectiveIntensity < settings.minVolumeThreshold)
        {
            if (enableNetworkPaintDebugLog)
            {
                Debug.LogWarning($"[PaintCanvas] PaintAtWithRadiusInternal - minVolumeThreshold未満でスキップ (effectiveIntensity: {effectiveIntensity}, minVolumeThreshold: {settings.minVolumeThreshold})");
            }
            return;
        }
        
        if (enableNetworkPaintDebugLog)
        {
            Debug.Log($"[PaintCanvas] PaintAtWithRadiusInternal開始 - screenPosition: {screenPosition}, centerX: {centerX}, centerY: {centerY}, radiusPixels: {radiusPixels}, playerId: {playerId}, effectiveIntensity: {effectiveIntensity}");
        }
        
        // 円形のブラシで塗る（常に後から塗った色が優先される）
        bool hasPainted = false;
        for (int x = centerX - radiusPixels; x <= centerX + radiusPixels; x++)
        {
            for (int y = centerY - radiusPixels; y <= centerY + radiusPixels; y++)
            {
                if (x < 0 || x >= settings.textureWidth || y < 0 || y >= settings.textureHeight)
                    continue;
                
                // 円形の範囲内かチェック（距離の二乗で比較して平方根の計算を避ける）
                int dx = x - centerX;
                int dy = y - centerY;
                int distanceSquared = dx * dx + dy * dy;
                int radiusPixelsSquared = radiusPixels * radiusPixels;
                if (distanceSquared <= radiusPixelsSquared)
                {
                    // 既に同じ色で塗られている場合はスキップ（パフォーマンス最適化）
                    int existingPlayerId = paintData[x, y];
                    if (existingPlayerId == playerId && colorData[x, y] == color)
                    {
                        continue; // 処理をスキップ
                    }
                    
                    // 塗り処理（プレイヤー／敵ともに後から塗った方が優先）
                    // ピクセル数のキャッシュを更新
                    if (pixelCountCacheValid)
                    {
                        UpdatePixelCountCache(existingPlayerId, playerId);
                    }
                    
                    paintData[x, y] = playerId;
                    colorData[x, y] = color;
                    intensityData[x, y] = effectiveIntensity;
                    
                    // タイムスタンプを記録（ネットワーク同期用）
                    if (getTimestampCallback != null)
                    {
                        paintTimestamp[x, y] = getTimestampCallback();
                    }
                    
                    // テクスチャを更新
                    UpdateTexturePixel(x, y, color);
                    hasPainted = true;
                }
            }
        }
        
        // テクスチャの更新をフラッシュ
        if (hasPainted)
        {
            FlushTextureUpdates();
            lastPaintFrame = Time.frameCount; // 塗りが実行されたフレームを記録
            
            if (enableNetworkPaintDebugLog)
            {
                Debug.Log($"[PaintCanvas] PaintAtWithRadiusInternal完了 - hasPainted: true, centerX: {centerX}, centerY: {centerY}, playerId: {playerId}");
            }
            
            // 補間処理中はイベント発火を抑制（最終位置を記録）
            if (suppressEventFiring)
            {
                lastInterpolationEndPosition = screenPosition;
                lastInterpolationPlayerId = playerId;
                lastInterpolationIntensity = effectiveIntensity;
            }
            else
            {
                OnPaintCompleted?.Invoke(screenPosition, playerId, effectiveIntensity);
            }
        }
        else
        {
            if (enableNetworkPaintDebugLog)
            {
                Debug.LogWarning($"[PaintCanvas] PaintAtWithRadiusInternal完了 - hasPainted: false (ピクセルが更新されませんでした), centerX: {centerX}, centerY: {centerY}, playerId: {playerId}, radiusPixels: {radiusPixels}");
            }
        }
    }
    
    /// <summary>
    /// 最後に塗りが実行されたフレームを取得（塗り処理実行判定用）
    /// </summary>
    public int GetLastPaintFrame()
    {
        return lastPaintFrame;
    }
    
    /// <summary>
    /// 円形塗りデータ（バッチ処理用）
    /// </summary>
    public struct CirclePaintData
    {
        public Vector2 position;
        public float radius;
        
        public CirclePaintData(Vector2 position, float radius)
        {
            this.position = position;
            this.radius = radius;
        }
    }
    
    /// <summary>
    /// 複数の円形領域を一度に塗る（バッチ処理、テクスチャ更新は最後に1回だけ）
    /// </summary>
    public void PaintMultipleCircles(System.Collections.Generic.List<CirclePaintData> circles, int playerId, float intensity, Color color)
    {
        if (!isInitialized || settings == null || circles == null || circles.Count == 0) return;
        
        // 塗り強度が閾値以上の場合のみ塗る
        float effectiveIntensity = intensity * settings.paintIntensityMultiplier;
        if (effectiveIntensity < settings.minVolumeThreshold)
        {
            return;
        }
        
        bool hasPainted = false;
        
        // 全ての円形領域を塗る
        foreach (var circle in circles)
        {
            // 画面座標をキャンバス座標に変換
            int centerX = Mathf.RoundToInt((circle.position.x / Screen.width) * settings.textureWidth);
            int centerY = Mathf.RoundToInt((circle.position.y / Screen.height) * settings.textureHeight);
            
            // 半径をキャンバス座標系に変換
            float radiusInCanvas = (circle.radius / Screen.width) * settings.textureWidth;
            int radiusPixels = Mathf.RoundToInt(radiusInCanvas);
            
            // 円形のブラシで塗る
            for (int x = centerX - radiusPixels; x <= centerX + radiusPixels; x++)
            {
                for (int y = centerY - radiusPixels; y <= centerY + radiusPixels; y++)
                {
                    if (x < 0 || x >= settings.textureWidth || y < 0 || y >= settings.textureHeight)
                        continue;
                    
                    // 円形の範囲内かチェック（距離の二乗で比較して平方根の計算を避ける）
                    int dx = x - centerX;
                    int dy = y - centerY;
                    int distanceSquared = dx * dx + dy * dy;
                    int radiusPixelsSquared = radiusPixels * radiusPixels;
                    if (distanceSquared <= radiusPixelsSquared)
                    {
                        // 既に同じ色で塗られている場合はスキップ（パフォーマンス最適化）
                        int existingPlayerId = paintData[x, y];
                        if (existingPlayerId == playerId && colorData[x, y] == color)
                        {
                            continue; // 処理をスキップ
                        }
                        
                        // 塗り処理（プレイヤー／敵ともに後から塗った方が優先）
                        // ピクセル数のキャッシュを更新
                        if (pixelCountCacheValid)
                        {
                            UpdatePixelCountCache(existingPlayerId, playerId);
                        }
                        
                        paintData[x, y] = playerId;
                        colorData[x, y] = color;
                        intensityData[x, y] = effectiveIntensity;
                        
                        // テクスチャを更新（Applyは最後に1回だけ）
                        UpdateTexturePixel(x, y, color);
                        hasPainted = true;
                    }
                }
            }
        }
        
        // テクスチャの更新をフラッシュ（1回だけ）
        if (hasPainted)
        {
            FlushTextureUpdates();
            lastPaintFrame = Time.frameCount;
            // イベントは中心位置で1回だけ発火
            if (circles.Count > 0)
            {
                OnPaintCompleted?.Invoke(circles[0].position, playerId, effectiveIntensity);
            }
        }
    }
    
    /// <summary>
    /// 消しツール用：指定位置から半径内を消す
    /// </summary>
    public void EraseAt(Vector2 screenPosition, float radius)
    {
        if (!isInitialized || settings == null) return;
        
        // 更新頻度チェック
        frameCount++;
        if (frameCount % settings.updateFrequency != 0)
        {
            return;
        }
        
        // 画面座標をキャンバス座標に変換
        int centerX = Mathf.RoundToInt((screenPosition.x / Screen.width) * settings.textureWidth);
        int centerY = Mathf.RoundToInt((screenPosition.y / Screen.height) * settings.textureHeight);
        
        // 半径をキャンバス座標系に変換
        float radiusInCanvas = (radius / Screen.width) * settings.textureWidth;
        int radiusPixels = Mathf.RoundToInt(radiusInCanvas);
        
        // 範囲チェック
        if (centerX < 0 || centerX >= settings.textureWidth || 
            centerY < 0 || centerY >= settings.textureHeight)
        {
            return;
        }
        
        // 円形の範囲内を消す
        bool hasErased = false;
        for (int x = centerX - radiusPixels; x <= centerX + radiusPixels; x++)
        {
            for (int y = centerY - radiusPixels; y <= centerY + radiusPixels; y++)
            {
                if (x < 0 || x >= settings.textureWidth || y < 0 || y >= settings.textureHeight)
                    continue;
                
                // 円形の範囲内かチェック（距離の二乗で比較して平方根の計算を避ける）
                int dx = x - centerX;
                int dy = y - centerY;
                int distanceSquared = dx * dx + dy * dy;
                int radiusPixelsSquared = radiusPixels * radiusPixels;
                if (distanceSquared <= radiusPixelsSquared)
                {
                    // 既に消されている場合はスキップ（パフォーマンス最適化）
                    if (paintData[x, y] == 0)
                    {
                        continue; // 処理をスキップ
                    }
                    
                    // 消し処理
                    int existingPlayerId = paintData[x, y];
                    
                    // ピクセル数のキャッシュを更新（0に変更）
                    if (pixelCountCacheValid)
                    {
                        UpdatePixelCountCache(existingPlayerId, 0);
                    }
                    
                    paintData[x, y] = 0;
                    colorData[x, y] = Color.clear;
                    intensityData[x, y] = 0f;
                    
                    // テクスチャを更新
                    UpdateTexturePixel(x, y, Color.clear);
                    hasErased = true;
                }
            }
        }
        
        // テクスチャの更新をフラッシュ
        if (hasErased)
        {
            FlushTextureUpdates();
            OnPaintCompleted?.Invoke(screenPosition, 0, 0f);
        }
    }
    
    /// <summary>
    /// テクスチャの特定ピクセルを更新（バッチ処理用：Apply()は呼ばない）
    /// </summary>
    private void UpdateTexturePixel(int x, int y, Color color)
    {
        if (canvasTexture != null && x >= 0 && x < settings.textureWidth && y >= 0 && y < settings.textureHeight)
        {
            canvasTexture.SetPixel(x, y, color);
            textureNeedsFlush = true;
        }
    }
    
    /// <summary>
    /// テクスチャの更新をフラッシュ（LateUpdate()で実行される）
    /// Apply()は呼ばず、textureNeedsFlushフラグのみを設定
    /// </summary>
    public void FlushTextureUpdates()
    {
        // Apply()は呼ばず、LateUpdate()で実行される
        // textureNeedsFlushフラグのみを設定
    }
    
    /// <summary>
    /// フレームごとに1回だけテクスチャを更新（パフォーマンス最適化）
    /// </summary>
    void LateUpdate()
    {
        if (!isInitialized || settings == null || canvasTexture == null)
        {
            return;
        }
        
        // テクスチャ更新頻度チェック
        textureUpdateFrameCount++;
        if (textureUpdateFrameCount % settings.textureUpdateFrequency != 0)
        {
            return; // 更新をスキップ
        }
        
        // フレームごとに1回だけテクスチャを更新
        if (textureNeedsFlush)
        {
            canvasTexture.Apply();
            textureNeedsFlush = false;
            
            // テクスチャ更新イベントを発火（PaintRenderer用）
            OnTextureUpdated?.Invoke();
        }
    }
    
    /// <summary>
    /// OnPaintCompletedイベントを外部から発火する（InkEffect表示用など）
    /// 実際の塗り処理は行わず、イベントのみを発火します。
    /// </summary>
    public void InvokePaintCompletedEvent(Vector2 screenPosition, int playerId, float intensity)
    {
        OnPaintCompleted?.Invoke(screenPosition, playerId, intensity);
    }
    
    public void ResetCanvas()
    {
        if (paintData == null || settings == null)
        {
            return;
        }
        
        // 全て0（未塗り）にリセット
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                paintData[x, y] = 0;
                colorData[x, y] = Color.clear;
                intensityData[x, y] = 0f;
                paintTimestamp[x, y] = 0f;
            }
        }
        
        // テクスチャもクリア
        if (canvasTexture != null)
        {
            Color[] pixels = new Color[settings.textureWidth * settings.textureHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            canvasTexture.SetPixels(pixels);
            canvasTexture.Apply();
        }
        
        // ピクセル数のキャッシュをリセット
        cachedPlayerPixelCount = 0;
        cachedEnemyPixelCount = 0;
        pixelCountCacheValid = true;
    }
    
    /// <summary>
    /// 塗りが抑制されたときに呼び出す（無音時など）
    /// </summary>
    public void NotifyPaintingSuppressed()
    {
        OnPaintingSuppressed?.Invoke();
    }
    
    /// <summary>
    /// 指定位置のプレイヤーIDを取得（デバッグ用）
    /// </summary>
    public int GetPlayerIdAt(Vector2 screenPosition)
    {
        if (!isInitialized || paintData == null)
        {
            return 0;
        }
        
        int canvasX = Mathf.RoundToInt((screenPosition.x / Screen.width) * settings.textureWidth);
        int canvasY = Mathf.RoundToInt((screenPosition.y / Screen.height) * settings.textureHeight);
        
        if (canvasX < 0 || canvasX >= settings.textureWidth || 
            canvasY < 0 || canvasY >= settings.textureHeight)
        {
            return 0;
        }
        
        return paintData[canvasX, canvasY];
    }
    
    /// <summary>
    /// キャンバス座標での色取得（PaintRenderer用）
    /// </summary>
    public Color GetColorAtCanvas(int x, int y)
    {
        if (colorData == null || x < 0 || x >= settings.textureWidth ||
            y < 0 || y >= settings.textureHeight)
        {
            return Color.clear;
        }
        return colorData[x, y];
    }
    
    /// <summary>
    /// キャンバス座標でのプレイヤーID取得（PaintRenderer用）
    /// </summary>
    public int GetPlayerIdAtCanvas(int x, int y)
    {
        if (paintData == null || x < 0 || x >= settings.textureWidth ||
            y < 0 || y >= settings.textureHeight)
        {
            return 0;
        }
        return paintData[x, y];
    }
    
    /// <summary>
    /// 設定を取得（PaintRenderer用）
    /// </summary>
    public PaintSettings GetSettings()
    {
        return settings;
    }
    
    /// <summary>
    /// PaintSpaceImageのRectTransformを取得（表示サイズの基準として使用）
    /// </summary>
    public RectTransform GetPaintSpaceImage()
    {
        return paintSpaceImage;
    }
    
    /// <summary>
    /// 補間処理の開始（イベント発火を抑制）
    /// 補間処理中はイベントを発火せず、最終位置のみを記録
    /// </summary>
    public void BeginInterpolation()
    {
        suppressEventFiring = true;
    }
    
    /// <summary>
    /// 補間処理の終了（最終位置でイベントを発火）
    /// </summary>
    public void EndInterpolation()
    {
        suppressEventFiring = false;
        
        // 補間処理中に記録された最終位置でイベントを発火
        if (lastInterpolationPlayerId > 0)
        {
            OnPaintCompleted?.Invoke(lastInterpolationEndPosition, lastInterpolationPlayerId, lastInterpolationIntensity);
            
            // リセット
            lastInterpolationEndPosition = Vector2.zero;
            lastInterpolationPlayerId = 0;
            lastInterpolationIntensity = 0f;
        }
    }
    
    /// <summary>
    /// 領域内の特定のplayerIdの色を消す（playerIdを0に戻し、色と強度もリセット）
    /// </summary>
    /// <param name="centerPosition">領域の中心位置（画面座標）</param>
    /// <param name="areaSize">領域のサイズ（画面座標）</param>
    /// <param name="targetPlayerId">消す対象のplayerId（例：-1で敵の色）</param>
    /// <param name="shapeData">領域の形状データ（nullの場合は円形として扱う）</param>
    public void ErasePlayerIdInArea(Vector2 centerPosition, float areaSize, int targetPlayerId, AreaShapeData shapeData = null)
    {
        if (!isInitialized || settings == null) return;
        
        // 画面座標をキャンバス座標に変換
        Vector2 canvasCenter = new Vector2(
            (centerPosition.x / Screen.width) * settings.textureWidth,
            (centerPosition.y / Screen.height) * settings.textureHeight
        );
        float canvasSize = (areaSize / Screen.width) * settings.textureWidth;
        
        // 形状を取得
        IAreaShape shape = null;
        if (shapeData != null)
        {
            shape = shapeData.CreateShape();
        }
        
        // バウンディングボックスを取得
        Rect boundingBox = shape != null 
            ? shape.GetBoundingBox(canvasCenter, canvasSize)
            : new Rect(canvasCenter.x - canvasSize * 0.5f, canvasCenter.y - canvasSize * 0.5f, 
                      canvasSize, canvasSize);
        
        // 領域の範囲を計算
        int minX = Mathf.Max(0, Mathf.RoundToInt(boundingBox.xMin));
        int maxX = Mathf.Min(settings.textureWidth - 1, Mathf.RoundToInt(boundingBox.xMax));
        int minY = Mathf.Max(0, Mathf.RoundToInt(boundingBox.yMin));
        int maxY = Mathf.Min(settings.textureHeight - 1, Mathf.RoundToInt(boundingBox.yMax));
        
        bool hasErased = false;
        
        // 領域内の各ピクセルをチェック
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 pixelPos = new Vector2(x, y);
                
                // 領域内かチェック（形状に応じて）
                bool isInArea = false;
                if (shape != null)
                {
                    isInArea = shape.IsPointInArea(pixelPos, canvasCenter, canvasSize);
                }
                else
                {
                    // フォールバック: 円形として判定（距離の二乗で比較して平方根の計算を避ける）
                    float radius = canvasSize * 0.5f;
                    float dx = pixelPos.x - canvasCenter.x;
                    float dy = pixelPos.y - canvasCenter.y;
                    float distanceSquared = dx * dx + dy * dy;
                    float radiusSquared = radius * radius;
                    isInArea = distanceSquared <= radiusSquared;
                }
                
                if (!isInArea) continue;
                
                // 対象のplayerIdの色を消す
                if (paintData[x, y] == targetPlayerId)
                {
                    int existingPlayerId = paintData[x, y];
                    
                    // ピクセル数のキャッシュを更新（0に変更）
                    if (pixelCountCacheValid)
                    {
                        UpdatePixelCountCache(existingPlayerId, 0);
                    }
                    
                    paintData[x, y] = 0;
                    colorData[x, y] = Color.clear;
                    intensityData[x, y] = 0;
                    UpdateTexturePixel(x, y, Color.clear);
                    hasErased = true;
                }
            }
        }
        
        // テクスチャの更新をフラッシュ
        if (hasErased)
        {
            FlushTextureUpdates();
        }
    }
    
    /// <summary>
    /// キャンバスの状態を取得（Undo/Redo用）
    /// </summary>
    public CanvasState GetState()
    {
        if (!isInitialized || settings == null) return null;
        
        CanvasState state = new CanvasState(settings.textureWidth, settings.textureHeight);
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                state.playerIds[x, y] = paintData[x, y];
                state.intensities[x, y] = intensityData[x, y];
                state.colors[x, y] = colorData[x, y];
            }
        }
        return state;
    }
    
    /// <summary>
    /// キャンバスの状態を復元（Undo/Redo用）
    /// </summary>
    public void RestoreState(CanvasState state)
    {
        if (!isInitialized || settings == null || state == null) return;
        if (state.width != settings.textureWidth || state.height != settings.textureHeight)
        {
            return;
        }
        
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                paintData[x, y] = state.playerIds[x, y];
                intensityData[x, y] = state.intensities[x, y];
                colorData[x, y] = state.colors[x, y];
            }
        }
        
        // テクスチャを更新
        UpdateTexture();
        
        // ピクセル数のキャッシュを無効化（次回GetPlayerAndEnemyPixelCounts()で再計算される）
        pixelCountCacheValid = false;
    }
    
    /// <summary>
    /// テクスチャ全体を更新
    /// </summary>
    private void UpdateTexture()
    {
        if (canvasTexture == null || settings == null) return;
        
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                canvasTexture.SetPixel(x, y, colorData[x, y]);
            }
        }
        canvasTexture.Apply();
    }
    
    /// <summary>
    /// テクスチャを取得（保存機能などで使用）
    /// </summary>
    public Texture2D GetTexture()
    {
        return canvasTexture;
    }
    
    /// <summary>
    /// タイムスタンプ配列を取得（ネットワーク同期用）
    /// </summary>
    public float[,] GetPaintTimestamps()
    {
        return paintTimestamp;
    }
    
    /// <summary>
    /// 色データ配列を取得（ネットワーク同期用）
    /// </summary>
    public Color[,] GetColorData()
    {
        return colorData;
    }
    
    /// <summary>
    /// プレイヤーIDデータ配列を取得（ネットワーク同期用）
    /// </summary>
    public int[,] GetPaintData()
    {
        return paintData;
    }
    
    /// <summary>
    /// タイムスタンプ取得用のコールバックを設定（ネットワーク同期用）
    /// </summary>
    public void SetTimestampCallback(System.Func<float> callback)
    {
        getTimestampCallback = callback;
    }
    
    /// <summary>
    /// タイムスタンプ付きで塗る（ネットワーク同期用、コンフリクト解決）
    /// </summary>
    /// <param name="x">キャンバス座標X</param>
    /// <param name="y">キャンバス座標Y</param>
    /// <param name="playerId">プレイヤーID</param>
    /// <param name="color">色</param>
    /// <param name="timestamp">タイムスタンプ（サーバー時刻）</param>
    public void PaintAtWithTimestamp(int x, int y, int playerId, Color color, float timestamp)
    {
        if (!isInitialized || settings == null)
        {
            return;
        }
        
        // 範囲チェック
        if (x < 0 || x >= settings.textureWidth || y < 0 || y >= settings.textureHeight)
        {
            return;
        }
        
        // 既存のタイムスタンプと比較
        if (timestamp > paintTimestamp[x, y])
        {
            // 新しいタイムスタンプの場合のみ塗りを適用
            int existingPlayerId = paintData[x, y];
            
            // ピクセル数のキャッシュを更新
            if (pixelCountCacheValid)
            {
                UpdatePixelCountCache(existingPlayerId, playerId);
            }
            
            paintData[x, y] = playerId;
            colorData[x, y] = color;
            paintTimestamp[x, y] = timestamp;
            
            // テクスチャを更新
            UpdateTexturePixel(x, y, color);
            
            // テクスチャの更新をフラッシュ
            FlushTextureUpdates();
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !isInitialized || paintData == null)
        {
            return;
        }
        
        // デバッグ表示（簡易版：塗られた位置を表示）
        // 実際の可視化はPaintRendererで実装
    }
}

