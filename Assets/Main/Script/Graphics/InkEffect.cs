using UnityEngine;

/// <summary>
/// インクエフェクト（簡易版）
/// 塗り位置にパーティクルエフェクトを表示
/// </summary>
public class InkEffect : MonoBehaviour
{
    [Header("Particle System")]
    [Tooltip("パーティクルシステム（Inspectorで接続、または自動生成）")]
    public ParticleSystem particleSystem;
    
    [Header("Mapping")]
    [SerializeField] private VoiceToScreenMapper voiceToScreenMapper;
    
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
        if (voiceToScreenMapper == null)
        {
            voiceToScreenMapper = FindObjectOfType<VoiceToScreenMapper>();
        }
        
        // パーティクルシステムが設定されていない場合は自動生成
        if (particleSystem == null)
        {
            CreateParticleSystem();
        }
        
        // イベント購読
        if (paintCanvas != null)
        {
            paintCanvas.OnPaintCompleted += OnPaintCompleted;
            paintCanvas.OnPaintingSuppressed += OnPaintingSuppressed;
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (paintCanvas != null)
        {
            paintCanvas.OnPaintCompleted -= OnPaintCompleted;
            paintCanvas.OnPaintingSuppressed -= OnPaintingSuppressed;
        }
    }
    
    void CreateParticleSystem()
    {
        GameObject particleObj = new GameObject("InkParticleSystem");
        particleObj.transform.SetParent(transform);
        particleSystem = particleObj.AddComponent<ParticleSystem>();
        
        // パーティクルシステムの設定
        var main = particleSystem.main;
        main.startColor = particleColor;
        main.startSize = particleSize;
        main.startLifetime = particleLifetime;
        main.maxParticles = 1000;
        
        var emission = particleSystem.emission;
        emission.enabled = false; // 手動で発射
        
        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
    }
    
    void OnPaintCompleted(Vector2 screenPosition, int playerId, float intensity)
    {
        if (particleSystem == null)
        {
            return;
        }
        
        // 画面座標をワールド座標に変換（カメラが必要）
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        if (mainCamera != null)
        {
            // 画面座標をワールド座標に変換
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane + 1f)
            );
            
            // パーティクルシステムの位置を設定
            particleSystem.transform.position = worldPos;
            
            // パーティクルを発射
            particleSystem.Emit(particleCount);
        }
    }

    void OnPaintingSuppressed()
    {
        if (particleSystem == null)
        {
            return;
        }

        Vector2 screenPosition = voiceToScreenMapper != null
            ? voiceToScreenMapper.MapToCenter()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        particleSystem.Clear(true);
        PositionParticleSystem(screenPosition, anchorToTarget: true);
    }

    void PositionParticleSystem(Vector2 screenPosition, bool anchorToTarget = false)
    {
        if (anchorToTarget && voiceToScreenMapper != null && voiceToScreenMapper.TargetRectTransform != null)
        {
            particleSystem.transform.position = voiceToScreenMapper.TargetRectTransform.position;
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane + 1f)
        );
        particleSystem.transform.position = worldPos;
    }
}

