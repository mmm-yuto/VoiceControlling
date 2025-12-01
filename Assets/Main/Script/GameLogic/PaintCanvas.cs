using UnityEngine;

/// <summary>
/// 塗りキャンバスの実装
/// 2D配列で塗り状態を管理し、Phase 1の基本機能とPhase 2のクリエイティブモードで使用
/// </summary>
public class PaintCanvas : MonoBehaviour, IPaintCanvas
{
    [Header("Settings")]
    [SerializeField] private PaintSettings settings;
    
    [Header("Debug")]
    [Tooltip("塗り位置をデバッグ表示するか")]
    public bool showDebugGizmos = false;
    
    // 塗り状態を管理する2D配列（playerIdを記録）
    private int[,] paintData;
    
    // Phase 2で追加：色データと強度データ
    private Color[,] colorData;
    private float[,] intensityData;
    
    // Phase 2で追加：内部Texture2D（描画・保存用）
    private Texture2D canvasTexture;
    
    // イベント
    public event System.Action<Vector2, int, float> OnPaintCompleted;
    public event System.Action OnPaintingSuppressed;
    
    // 内部状態
    private int frameCount = 0;
    private bool isInitialized = false;
    private bool textureNeedsFlush = false; // テクスチャの更新が必要かどうか
    
    void Awake()
    {
        if (settings == null)
        {
            Debug.LogError("PaintCanvas: PaintSettingsが設定されていません");
            return;
        }
        
        InitializeCanvas();
    }
    
    void InitializeCanvas()
    {
        paintData = new int[settings.textureWidth, settings.textureHeight];
        colorData = new Color[settings.textureWidth, settings.textureHeight];
        intensityData = new float[settings.textureWidth, settings.textureHeight];
        
        // 内部Texture2Dを作成
        canvasTexture = new Texture2D(settings.textureWidth, settings.textureHeight, TextureFormat.RGBA32, false);
        canvasTexture.filterMode = FilterMode.Bilinear;
        canvasTexture.wrapMode = TextureWrapMode.Clamp;
        
        // 初期化：全て0（未塗り）にする
        ResetCanvas();
        
        isInitialized = true;
        Debug.Log($"PaintCanvas: 初期化完了 ({settings.textureWidth}x{settings.textureHeight})");
    }
    
    /// <summary>
    /// 現在のキャンバス上のプレイヤー／敵ピクセル数を集計する
    /// </summary>
    /// <param name="playerPixels">playerId > 0 のピクセル数</param>
    /// <param name="enemyPixels">playerId == -1 のピクセル数</param>
    public void GetPlayerAndEnemyPixelCounts(out int playerPixels, out int enemyPixels)
    {
        playerPixels = 0;
        enemyPixels = 0;
        
        if (!isInitialized || settings == null || paintData == null)
        {
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
                    playerPixels++;
                }
                else if (id == -1)
                {
                    enemyPixels++;
                }
            }
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
            Debug.LogWarning("PaintCanvas: 初期化されていません");
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
            if (playerId == -1) // 敵の色の場合のみデバッグログ
            {
                Debug.LogWarning($"[PaintCanvas] PaintAt - 強度が閾値以下でスキップ: effectiveIntensity={effectiveIntensity:F4}, minVolumeThreshold={settings.minVolumeThreshold:F4}, playerId={playerId}");
            }
            return;
        }
        
        // 塗り処理（優先順位ロジックを追加）
        int existingPlayerId = paintData[canvasX, canvasY];
        
        // 敵の色を塗る場合、プレイヤーが既に塗っている領域は上塗りしない
        if (playerId == -1 && existingPlayerId > 0)
        {
            // プレイヤーの色を上塗りしない（デバッグログ）
            if (showDebugGizmos)
            {
                Debug.Log($"[PaintCanvas] PaintAt - 敵の色がプレイヤーの色でブロック: キャンバス座標({canvasX}, {canvasY}), 既存playerId={existingPlayerId}");
            }
            return;
        }
        
        // プレイヤーの色を塗る場合、敵の色を上塗り可能
        // （既存のロジックで上書きされるため、追加処理不要）
        
        paintData[canvasX, canvasY] = playerId;
        colorData[canvasX, canvasY] = color;
        intensityData[canvasX, canvasY] = effectiveIntensity;
        
        if (playerId == -1) // 敵の色の場合のみデバッグログ
        {
            Debug.Log($"[PaintCanvas] PaintAt - 敵の色を塗りました: キャンバス座標({canvasX}, {canvasY}), 画面座標({screenPosition.x:F1}, {screenPosition.y:F1}), 色: {color}, 強度: {effectiveIntensity:F4}");
        }
        
        // テクスチャを更新
        UpdateTexturePixel(canvasX, canvasY, color);
        
        // テクスチャの更新をフラッシュ
        FlushTextureUpdates();
        
        // イベント発火
        OnPaintCompleted?.Invoke(screenPosition, playerId, effectiveIntensity);
        
        if (showDebugGizmos)
        {
            Debug.Log($"PaintCanvas: 塗り完了 ({canvasX}, {canvasY}), Player: {playerId}, Intensity: {effectiveIntensity:F3}, Color: {color}");
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
        
        // 塗り強度が閾値以上の場合のみ塗る
        float effectiveIntensity = intensity * settings.paintIntensityMultiplier;
        if (effectiveIntensity < settings.minVolumeThreshold)
        {
            return;
        }
        
        // 円形のブラシで塗る（常に後から塗った色が優先される）
        bool hasPainted = false;
        for (int x = centerX - radiusPixels; x <= centerX + radiusPixels; x++)
        {
            for (int y = centerY - radiusPixels; y <= centerY + radiusPixels; y++)
            {
                if (x < 0 || x >= settings.textureWidth || y < 0 || y >= settings.textureHeight)
                    continue;
                
                // 円形の範囲内かチェック
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (distance <= radiusPixels)
                {
                    // 塗り処理（プレイヤー／敵ともに後から塗った方が優先）
                    paintData[x, y] = playerId;
                    colorData[x, y] = color;
                    intensityData[x, y] = effectiveIntensity;
                    
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
            OnPaintCompleted?.Invoke(screenPosition, playerId, effectiveIntensity);
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
                
                // 円形の範囲内かチェック
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (distance <= radiusPixels)
                {
                    // 消し処理
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
    /// テクスチャの更新をフラッシュ（まとめてApply()を実行）
    /// </summary>
    public void FlushTextureUpdates()
    {
        if (textureNeedsFlush && canvasTexture != null)
        {
            canvasTexture.Apply();
            textureNeedsFlush = false;
        }
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
        
        Debug.Log("PaintCanvas: キャンバスをリセットしました");
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
                    // フォールバック: 円形として判定
                    float radius = canvasSize * 0.5f;
                    isInArea = Vector2.Distance(pixelPos, canvasCenter) <= radius;
                }
                
                if (!isInArea) continue;
                
                // 対象のplayerIdの色を消す
                if (paintData[x, y] == targetPlayerId)
                {
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
            Debug.LogError("PaintCanvas: RestoreState failed - size mismatch");
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

