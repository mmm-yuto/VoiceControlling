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
    
    // 塗り状態を管理する2D配列（playerIdと強度、色を記録）
    private int[,] playerIdData;
    private float[,] intensityData;
    private Color32[,] colorData;
    private Texture2D paintTexture;
    private readonly Color32 clearColor = new Color32(0, 0, 0, 0);
    
    // イベント
    public event System.Action<Vector2, int, float> OnPaintCompleted;
    
    // 内部状態
    private int frameCount = 0;
    private bool isInitialized = false;
    
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
        playerIdData = new int[settings.textureWidth, settings.textureHeight];
        intensityData = new float[settings.textureWidth, settings.textureHeight];
        colorData = new Color32[settings.textureWidth, settings.textureHeight];
        CreateTexture();
        
        // 初期化：全て0（未塗り）にする
        ResetCanvas();
        
        isInitialized = true;
        Debug.Log($"PaintCanvas: 初期化完了 ({settings.textureWidth}x{settings.textureHeight})");
    }
    
    void CreateTexture()
    {
        paintTexture = new Texture2D(settings.textureWidth, settings.textureHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
    }
    
    bool TryGetCanvasCoordinate(Vector2 screenPosition, out int canvasX, out int canvasY)
    {
        canvasX = 0;
        canvasY = 0;
        
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return false;
        }
        
        float normalizedX = Mathf.Clamp01(screenPosition.x / Screen.width);
        float normalizedY = Mathf.Clamp01(screenPosition.y / Screen.height);
        
        canvasX = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (settings.textureWidth - 1)), 0, settings.textureWidth - 1);
        canvasY = Mathf.Clamp(Mathf.RoundToInt(normalizedY * (settings.textureHeight - 1)), 0, settings.textureHeight - 1);
        return true;
    }
    
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
        if (!TryGetCanvasCoordinate(screenPosition, out int canvasX, out int canvasY))
        {
            return;
        }
        
        // 塗り強度が閾値以上の場合のみ塗る
        float effectiveIntensity = intensity * settings.paintIntensityMultiplier;
        if (effectiveIntensity < settings.minVolumeThreshold)
        {
            return;
        }
        
        // 塗り処理（単一ピクセルを更新、後でブラシ拡張可能）
        playerIdData[canvasX, canvasY] = playerId;
        intensityData[canvasX, canvasY] = effectiveIntensity;
        colorData[canvasX, canvasY] = color;
        paintTexture.SetPixel(canvasX, canvasY, color);
        paintTexture.Apply();
        
        // イベント発火
        OnPaintCompleted?.Invoke(screenPosition, playerId, effectiveIntensity);
        
        if (showDebugGizmos)
        {
            Debug.Log($"PaintCanvas: 塗り完了 ({canvasX}, {canvasY}), Player: {playerId}, Intensity: {effectiveIntensity:F3}");
        }
    }
    
    /// <summary>
    /// 旧シグネチャ互換のためのオーバーロード（デフォルトカラーで塗り）
    /// </summary>
    public void PaintAt(Vector2 screenPosition, int playerId, float intensity)
    {
        PaintAt(screenPosition, playerId, intensity, Color.white);
    }
    
    public void ResetCanvas()
    {
        if (playerIdData == null)
        {
            return;
        }
        
        // 全て0（未塗り）にリセット
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                playerIdData[x, y] = 0;
                intensityData[x, y] = 0f;
                colorData[x, y] = clearColor;
            }
        }
        
        if (paintTexture == null)
        {
            CreateTexture();
        }
        
        var fill = new Color32[settings.textureWidth * settings.textureHeight];
        for (int i = 0; i < fill.Length; i++)
        {
            fill[i] = clearColor;
        }
        paintTexture.SetPixels32(fill);
        paintTexture.Apply();
        
        Debug.Log("PaintCanvas: キャンバスをリセットしました");
    }
    
    public void EraseAt(Vector2 screenPosition, float radius)
    {
        if (!isInitialized || playerIdData == null)
        {
            return;
        }
        
        if (!TryGetCanvasCoordinate(screenPosition, out int centerX, out int centerY))
        {
            return;
        }
        
        int radiusInt = Mathf.Max(0, Mathf.RoundToInt(radius));
        int sqrRadius = radiusInt * radiusInt;
        bool changed = false;
        
        for (int dx = -radiusInt; dx <= radiusInt; dx++)
        {
            for (int dy = -radiusInt; dy <= radiusInt; dy++)
            {
                int px = centerX + dx;
                int py = centerY + dy;
                
                if (px < 0 || px >= settings.textureWidth || py < 0 || py >= settings.textureHeight)
                {
                    continue;
                }
                
                if ((dx * dx) + (dy * dy) > sqrRadius)
                {
                    continue;
                }
                
                if (playerIdData[px, py] == 0 && intensityData[px, py] <= 0f)
                {
                    continue;
                }
                
                playerIdData[px, py] = 0;
                intensityData[px, py] = 0f;
                colorData[px, py] = clearColor;
                paintTexture.SetPixel(px, py, clearColor);
                changed = true;
            }
        }
        
        if (changed)
        {
            paintTexture.Apply();
        }
    }
    
    public CanvasState GetState()
    {
        if (!isInitialized)
        {
            return null;
        }
        
        CanvasState state = new CanvasState(settings.textureWidth, settings.textureHeight);
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                state.playerIds[x, y] = playerIdData[x, y];
                state.intensities[x, y] = intensityData[x, y];
                state.colors[x, y] = colorData[x, y];
            }
        }
        return state;
    }
    
    public void RestoreState(CanvasState state)
    {
        if (state == null)
        {
            Debug.LogWarning("PaintCanvas: RestoreStateにnullが渡されました");
            return;
        }
        
        if (state.width != settings.textureWidth || state.height != settings.textureHeight)
        {
            Debug.LogError("PaintCanvas: CanvasStateのサイズが一致しません");
            return;
        }
        
        if (!isInitialized)
        {
            InitializeCanvas();
        }
        
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                playerIdData[x, y] = state.playerIds[x, y];
                intensityData[x, y] = state.intensities[x, y];
                colorData[x, y] = state.colors[x, y];
                paintTexture.SetPixel(x, y, state.colors[x, y]);
            }
        }
        
        paintTexture.Apply();
    }
    
    public Texture2D GetTexture()
    {
        if (paintTexture == null)
        {
            CreateTexture();
            ResetCanvas();
        }
        
        return paintTexture;
    }
    
    /// <summary>
    /// 指定位置のプレイヤーIDを取得（デバッグ用）
    /// </summary>
    public int GetPlayerIdAt(Vector2 screenPosition)
    {
        if (!isInitialized || playerIdData == null)
        {
            return 0;
        }
        
        if (!TryGetCanvasCoordinate(screenPosition, out int canvasX, out int canvasY))
        {
            return 0;
        }
        
        return playerIdData[canvasX, canvasY];
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !isInitialized || playerIdData == null)
        {
            return;
        }
        
        // デバッグ表示（簡易版：塗られた位置を表示）
        // 実際の可視化はPhase 1後、Texture2D + UI Imageで実装
    }
}


