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
    
    // イベント
    public event System.Action<Vector2, int, float> OnPaintCompleted;
    public event System.Action OnPaintingSuppressed;
    
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
        paintData = new int[settings.textureWidth, settings.textureHeight];
        
        // 初期化：全て0（未塗り）にする
        ResetCanvas();
        
        isInitialized = true;
        Debug.Log($"PaintCanvas: 初期化完了 ({settings.textureWidth}x{settings.textureHeight})");
    }
    
    public void PaintAt(Vector2 screenPosition, int playerId, float intensity)
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
        
        // イベント発火
        OnPaintCompleted?.Invoke(screenPosition, playerId, effectiveIntensity);
        
        if (showDebugGizmos)
        {
            Debug.Log($"PaintCanvas: 塗り完了 ({canvasX}, {canvasY}), Player: {playerId}, Intensity: {effectiveIntensity:F3}");
        }
    }
    
    public void ResetCanvas()
    {
        if (paintData == null)
        {
            return;
        }
        
        // 全て0（未塗り）にリセット
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                paintData[x, y] = 0;
            }
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
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !isInitialized || paintData == null)
        {
            return;
        }
        
        // デバッグ表示（簡易版：塗られた位置を表示）
        // 実際の可視化はPhase 1後、Texture2D + UI Imageで実装
    }
}

