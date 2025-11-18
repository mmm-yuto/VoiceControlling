using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インクエフェクト（簡易版）
/// 塗り位置にパーティクルエフェクトを表示
/// </summary>
public class InkEffect : MonoBehaviour
{
    [Header("Particle System")]
    [Tooltip("パーティクルシステム（Inspectorで接続、または自動生成）")]
    public ParticleSystem particleSystem;
    
    [Header("Effect Settings")]
    [Tooltip("エフェクトを使用するか（falseの場合はパーティクルを表示しないが、マーカーは表示される）")]
    public bool useEffect = true;
    
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
    
    [Header("Marker Settings")]
    [Tooltip("マーカーを表示するか")]
    public bool showMarker = true;
    
    [Tooltip("マーカーの色")]
    public Color markerColor = Color.white;
    
    [Tooltip("マーカーのサイズ（ピクセル）")]
    [Range(10f, 200f)]
    public float markerSize = 50f;
    
    [Tooltip("マーカーのUI要素（自動生成される）")]
    public RectTransform markerRectTransform;
    
    [Header("References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    public PaintCanvas paintCanvas;
    
    private Image markerImage;
    private Canvas uiCanvas;
    private Vector2 lastScreenPosition = Vector2.zero; // マーカー位置更新用
    
    void Start()
    {
        // 参照が設定されていない場合は自動検索
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
        }
        
        // パーティクルシステムが設定されていない場合は自動生成（エフェクトを使用する場合のみ）
        if (useEffect && particleSystem == null)
        {
            CreateParticleSystem();
        }
        
        // マーカーUIを作成
        if (showMarker)
        {
            CreateMarkerUI();
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
    
    void Update()
    {
        // マーカーを表示位置に更新
        if (showMarker && markerRectTransform != null)
        {
            if (useEffect && particleSystem != null)
            {
                // エフェクト有効時：パーティクルシステムの位置を使用
                UpdateMarkerPosition();
            }
            else
            {
                // エフェクト無効時：最後に受け取った画面座標を使用
                UpdateMarkerPositionFromScreen(lastScreenPosition);
            }
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
        // 画面座標を保存（マーカー更新用）
        lastScreenPosition = screenPosition;
        
        // マーカーが有効な場合は位置を更新
        if (showMarker && markerRectTransform != null)
        {
            UpdateMarkerPositionFromScreen(screenPosition);
        }
        
        // エフェクトが無効の場合はパーティクルを発射しない
        if (!useEffect || particleSystem == null)
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
        // エフェクトが有効な場合のみ処理
        if (useEffect && particleSystem != null)
        {
            particleSystem.Clear();
            Camera mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindObjectOfType<Camera>();

            if (mainCamera != null)
            {
                // VoiceToScreenMapperから中心位置を取得
                VoiceToScreenMapper voiceToScreenMapper = FindObjectOfType<VoiceToScreenMapper>();
                if (voiceToScreenMapper != null)
                {
                    Vector3 centerScreenPos = voiceToScreenMapper.MapToCenter();
                    Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(centerScreenPos.x, centerScreenPos.y, mainCamera.nearClipPlane + 1f));
                    particleSystem.transform.position = worldPos;
                    
                    // マーカーも中心位置に更新
                    if (showMarker && markerRectTransform != null)
                    {
                        UpdateMarkerPositionFromScreen(centerScreenPos);
                        lastScreenPosition = centerScreenPos;
                    }
                }
            }
        }
        else if (showMarker && markerRectTransform != null)
        {
            // エフェクトが無効でもマーカーは中心位置に更新
            VoiceToScreenMapper voiceToScreenMapper = FindObjectOfType<VoiceToScreenMapper>();
            if (voiceToScreenMapper != null)
            {
                Vector3 centerScreenPos = voiceToScreenMapper.MapToCenter();
                UpdateMarkerPositionFromScreen(centerScreenPos);
                lastScreenPosition = centerScreenPos;
            }
        }
    }
    
    void CreateMarkerUI()
    {
        // Canvasを取得または作成
        uiCanvas = FindObjectOfType<Canvas>();
        if (uiCanvas == null)
        {
            GameObject canvasObj = new GameObject("MarkerCanvas");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // マーカー用のGameObjectを作成
        GameObject markerObj = new GameObject("EffectMarker");
        markerObj.transform.SetParent(uiCanvas.transform, false);
        
        // RectTransformを設定
        markerRectTransform = markerObj.AddComponent<RectTransform>();
        markerRectTransform.sizeDelta = new Vector2(markerSize, markerSize);
        markerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        markerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        markerRectTransform.pivot = new Vector2(0.5f, 0.5f);
        markerRectTransform.anchoredPosition = Vector2.zero;
        
        // Imageコンポーネントを追加
        markerImage = markerObj.AddComponent<Image>();
        markerImage.color = markerColor;
        
        // 円形のスプライトを作成（簡易版：白い円）
        Texture2D texture = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];
        Vector2 center = new Vector2(32, 32);
        float radius = 30f;
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                colors[y * 64 + x] = distance <= radius ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        markerImage.sprite = sprite;
    }
    
    void UpdateMarkerPosition()
    {
        if (particleSystem == null || markerRectTransform == null)
        {
            return;
        }
        
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        if (mainCamera != null)
        {
            // パーティクルシステムのワールド座標を画面座標に変換
            Vector3 screenPos = mainCamera.WorldToScreenPoint(particleSystem.transform.position);
            UpdateMarkerPositionFromScreen(screenPos);
        }
    }
    
    /// <summary>
    /// 画面座標からマーカー位置を更新
    /// </summary>
    void UpdateMarkerPositionFromScreen(Vector2 screenPosition)
    {
        if (markerRectTransform == null || uiCanvas == null)
        {
            return;
        }
        
        // UI座標に変換
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiCanvas.transform as RectTransform,
            screenPosition,
            uiCanvas.worldCamera,
            out Vector2 localPoint))
        {
            markerRectTransform.anchoredPosition = localPoint;
        }
    }
    
    void OnValidate()
    {
        // インスペクターで値が変更されたときにマーカーを更新
        if (markerImage != null)
        {
            markerImage.color = markerColor;
        }
        
        if (markerRectTransform != null)
        {
            markerRectTransform.sizeDelta = new Vector2(markerSize, markerSize);
        }
    }
}

