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
            return;
        }
        
        // 塗り処理（最初は単純に上書き、後から上塗り機能を追加）
        paintData[canvasX, canvasY] = playerId;
        colorData[canvasX, canvasY] = color;
        intensityData[canvasX, canvasY] = effectiveIntensity;
        
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
        
        // 円形のブラシで塗る
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
                    // 塗り処理
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
    private void FlushTextureUpdates()
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

