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
    
    [Header("Enemy Marker Settings")]
    [Tooltip("敵用マーカーを表示するか")]
    public bool showEnemyMarker = true;
    
    [Tooltip("敵用マーカーの色")]
    public Color enemyMarkerColor = Color.red;
    
    [Tooltip("敵用マーカーのサイズ（ピクセル）")]
    [Range(10f, 200f)]
    public float enemyMarkerSize = 50f;
    
    [Tooltip("敵用マーカーのUI要素（自動生成される）")]
    public RectTransform enemyMarkerRectTransform;
    
    [Header("Enemy Effect Settings")]
    [Tooltip("敵用エフェクトを使用するか")]
    public bool useEnemyEffect = true;
    
    [Tooltip("敵用パーティクルシステム（Inspectorで接続、または自動生成）")]
    public ParticleSystem enemyParticleSystem;
    
    [Tooltip("敵用パーティクルの色")]
    public Color enemyParticleColor = Color.red;
    
    [Tooltip("敵用パーティクルのサイズ")]
    [Range(0.1f, 2f)]
    public float enemyParticleSize = 0.5f;
    
    [Tooltip("敵用パーティクルの生存時間（秒）")]
    [Range(0.1f, 5f)]
    public float enemyParticleLifetime = 1f;
    
    [Tooltip("敵用パーティクルの数")]
    [Range(1, 50)]
    public int enemyParticleCount = 10;
    
    [Header("References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    public PaintCanvas paintCanvas;
    
    private Image markerImage;
    private Image enemyMarkerImage;
    private Canvas uiCanvas;
    private Vector2 lastScreenPosition = Vector2.zero; // マーカー位置更新用
    private Vector2 lastEnemyScreenPosition = Vector2.zero; // 敵用マーカー位置更新用
    
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
        
        // 敵用パーティクルシステムが設定されていない場合は自動生成（敵用エフェクトを使用する場合のみ）
        if (useEnemyEffect && enemyParticleSystem == null)
        {
            CreateEnemyParticleSystem();
        }
        
        // マーカーUIを作成
        if (showMarker)
        {
            CreateMarkerUI();
        }
        
        // 敵用マーカーUIを作成
        if (showEnemyMarker)
        {
            CreateEnemyMarkerUI();
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
        
        // 敵用マーカーを表示位置に更新
        if (showEnemyMarker && enemyMarkerRectTransform != null)
        {
            if (useEnemyEffect && enemyParticleSystem != null)
            {
                // エフェクト有効時：パーティクルシステムの位置を使用
                UpdateEnemyMarkerPosition();
            }
            else
            {
                // エフェクト無効時：最後に受け取った画面座標を使用
                UpdateMarkerPositionFromScreen(lastEnemyScreenPosition, enemyMarkerRectTransform);
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
    
    void CreateEnemyParticleSystem()
    {
        GameObject particleObj = new GameObject("EnemyInkParticleSystem");
        particleObj.transform.SetParent(transform);
        enemyParticleSystem = particleObj.AddComponent<ParticleSystem>();
        
        // 敵用パーティクルシステムの設定
        var main = enemyParticleSystem.main;
        main.startColor = enemyParticleColor;
        main.startSize = enemyParticleSize;
        main.startLifetime = enemyParticleLifetime;
        main.maxParticles = 1000;
        
        var emission = enemyParticleSystem.emission;
        emission.enabled = false; // 手動で発射
        
        var shape = enemyParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
    }
    
    void OnPaintCompleted(Vector2 screenPosition, int playerId, float intensity)
    {
        // 画面座標をワールド座標に変換（カメラが必要）
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
        
        // playerIdに応じてエフェクトとマーカーを分岐
        if (playerId == -1)
        {
            // 敵用エフェクト
            if (useEnemyEffect && enemyParticleSystem != null)
            {
                enemyParticleSystem.transform.position = worldPos;
                enemyParticleSystem.Emit(enemyParticleCount);
            }
            
            // 敵用マーカーの位置を更新
            lastEnemyScreenPosition = screenPosition;
            if (showEnemyMarker && enemyMarkerRectTransform != null)
            {
                UpdateMarkerPositionFromScreen(screenPosition, enemyMarkerRectTransform);
            }
        }
        else if (playerId > 0)
        {
            // プレイヤー用エフェクト
            if (useEffect && particleSystem != null)
            {
                particleSystem.transform.position = worldPos;
                particleSystem.Emit(particleCount);
            }
            
            // プレイヤー用マーカーの位置を更新
            lastScreenPosition = screenPosition;
            if (showMarker && markerRectTransform != null)
            {
                UpdateMarkerPositionFromScreen(screenPosition);
            }
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
    
    void CreateEnemyMarkerUI()
    {
        // Canvasを取得または作成（既に作成されている場合は再利用）
        if (uiCanvas == null)
        {
            uiCanvas = FindObjectOfType<Canvas>();
            if (uiCanvas == null)
            {
                GameObject canvasObj = new GameObject("MarkerCanvas");
                uiCanvas = canvasObj.AddComponent<Canvas>();
                uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }
        
        // 敵用マーカー用のGameObjectを作成
        GameObject markerObj = new GameObject("EnemyEffectMarker");
        markerObj.transform.SetParent(uiCanvas.transform, false);
        
        // RectTransformを設定
        enemyMarkerRectTransform = markerObj.AddComponent<RectTransform>();
        enemyMarkerRectTransform.sizeDelta = new Vector2(enemyMarkerSize, enemyMarkerSize);
        enemyMarkerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        enemyMarkerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        enemyMarkerRectTransform.pivot = new Vector2(0.5f, 0.5f);
        enemyMarkerRectTransform.anchoredPosition = Vector2.zero;
        
        // Imageコンポーネントを追加
        enemyMarkerImage = markerObj.AddComponent<Image>();
        enemyMarkerImage.color = enemyMarkerColor;
        
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
        enemyMarkerImage.sprite = sprite;
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
    
    void UpdateEnemyMarkerPosition()
    {
        if (enemyParticleSystem == null || enemyMarkerRectTransform == null)
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
            Vector3 screenPos = mainCamera.WorldToScreenPoint(enemyParticleSystem.transform.position);
            UpdateMarkerPositionFromScreen(screenPos, enemyMarkerRectTransform);
        }
    }
    
    /// <summary>
    /// 画面座標からマーカー位置を更新
    /// </summary>
    void UpdateMarkerPositionFromScreen(Vector2 screenPosition)
    {
        UpdateMarkerPositionFromScreen(screenPosition, markerRectTransform);
    }
    
    /// <summary>
    /// 画面座標からマーカー位置を更新（RectTransform指定版）
    /// </summary>
    void UpdateMarkerPositionFromScreen(Vector2 screenPosition, RectTransform targetRectTransform)
    {
        if (targetRectTransform == null || uiCanvas == null)
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
            targetRectTransform.anchoredPosition = localPoint;
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
        
        // 敵用マーカーの更新
        if (enemyMarkerImage != null)
        {
            enemyMarkerImage.color = enemyMarkerColor;
        }
        
        if (enemyMarkerRectTransform != null)
        {
            enemyMarkerRectTransform.sizeDelta = new Vector2(enemyMarkerSize, enemyMarkerSize);
        }
    }
}

