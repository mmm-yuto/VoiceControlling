using UnityEngine;

/// <summary>
/// インクエフェクト（簡易版）
/// 塗り位置にパーティクルエフェクトを表示
/// </summary>
public class InkEffect : MonoBehaviour
{
    [Header("Particle System")]
    [Tooltip("パーティクルシステム（Inspectorで接続、または自動生成）")]
    public ParticleSystem inkParticleSystem;
    
    [Header("Effect Settings")]
    [Tooltip("パーティクルの色")]
    public Color particleColor = Color.cyan;
    
    [Tooltip("パーティクルのサイズ")]
    [Range(0.1f, 2f)]
    public float particleSize = 0.5f;
    
    [Tooltip("パーティクルの生存時間（秒）")]
    [Range(0.1f, 5f)]
    public float particleLifetime = 1f;
    
    [Tooltip("パーティクルの数")]
    [Range(1, 50)]
    public int particleCount = 10;
    
    [Header("References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    public PaintCanvas paintCanvas;
    
    void Start()
    {
        // 参照が設定されていない場合は自動検索
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
        }
        
        if (paintCanvas == null)
        {
            Debug.LogError("InkEffect: PaintCanvasが見つかりません！");
        }
        else
        {
            Debug.Log("InkEffect: PaintCanvasが見つかりました");
        }
        
        // パーティクルシステムが設定されていない場合は自動生成
        if (inkParticleSystem == null)
        {
            CreateParticleSystem();
        }
        
        if (inkParticleSystem == null)
        {
            Debug.LogError("InkEffect: ParticleSystemが作成できませんでした！");
        }
        else
        {
            Debug.Log("InkEffect: ParticleSystemが作成されました");
        }
        
        // イベント購読
        if (paintCanvas != null)
        {
            paintCanvas.OnPaintCompleted += OnPaintCompleted;
            Debug.Log("InkEffect: OnPaintCompletedイベントを購読しました");
        }
        else
        {
            Debug.LogError("InkEffect: PaintCanvasがnullのため、イベントを購読できません");
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (paintCanvas != null)
        {
            paintCanvas.OnPaintCompleted -= OnPaintCompleted;
        }
    }
    
    void CreateParticleSystem()
    {
        GameObject particleObj = new GameObject("InkParticleSystem");
        particleObj.transform.SetParent(transform);
        inkParticleSystem = particleObj.AddComponent<ParticleSystem>();
        
        // パーティクルシステムの設定
        var main = inkParticleSystem.main;
        main.startColor = particleColor;
        main.startSize = particleSize;
        main.startLifetime = particleLifetime;
        main.maxParticles = 1000;
        
        var emission = inkParticleSystem.emission;
        emission.enabled = false; // 手動で発射
        
        var shape = inkParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
    }
    
    void OnPaintCompleted(Vector2 screenPosition, int playerId, float intensity)
    {
        Debug.Log($"InkEffect.OnPaintCompleted called: screenPosition={screenPosition}, playerId={playerId}, intensity={intensity:F6}");
        
        if (inkParticleSystem == null)
        {
            Debug.LogError("InkEffect: ParticleSystem is null!");
            return;
        }
        
        // 画面座標をワールド座標に変換（カメラが必要）
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("InkEffect: Cameraが見つかりません！");
            return;
        }
        
        // 画面座標をワールド座標に変換
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane + 1f)
        );
        
        Debug.Log($"InkEffect: Converting screen ({screenPosition.x}, {screenPosition.y}) to world ({worldPos.x}, {worldPos.y}, {worldPos.z})");
        
        // パーティクルシステムの位置を設定
        inkParticleSystem.transform.position = worldPos;
        
        // パーティクルを発射
        inkParticleSystem.Emit(particleCount);
        
        Debug.Log($"InkEffect: Emitted {particleCount} particles at world position ({worldPos.x}, {worldPos.y}, {worldPos.z})");
    }
}

