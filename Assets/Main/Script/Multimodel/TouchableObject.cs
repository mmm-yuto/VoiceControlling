using UnityEngine;

/// <summary>
/// タッチ可能なオブジェクトのコンポーネント
/// タッチ検出と色変更処理を行う
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TouchableObject : MonoBehaviour
{
    [Header("References")]
    [Tooltip("色を変更する対象（SpriteRenderer、Renderer、Image等）")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    
    [Tooltip("色を変更する対象（Renderer、Image等）")]
    [SerializeField] private Renderer targetRenderer;
    
    [Tooltip("UI Imageコンポーネント（UIオブジェクトの場合）")]
    [SerializeField] private UnityEngine.UI.Image targetImage;
    
    [Tooltip("ColorCalculatorコンポーネント（自動検索可能）")]
    [SerializeField] private ColorCalculator colorCalculator;
    
    [Tooltip("NeutralSoundDetectorコンポーネント（自動検索可能）")]
    [SerializeField] private NeutralSoundDetector neutralSoundDetector;
    
    [Header("Touch Settings")]
    [Tooltip("タッチ検出を有効にするか")]
    [SerializeField] private bool enableTouchDetection = true;
    
    [Tooltip("マウスクリックでも検出するか")]
    [SerializeField] private bool detectMouseClick = true;
    
    [Header("Color Settings")]
    [Tooltip("初期色")]
    [SerializeField] private Color initialColor = Color.white;
    
    [Tooltip("色変更のスムージング係数（0-1、大きいほど滑らか）")]
    [Range(0f, 1f)]
    [SerializeField] private float colorSmoothing = 0.1f;
    
    [Header("Audio Detection")]
    [Tooltip("タッチ中に音声検出を開始するか")]
    [SerializeField] private bool detectAudioWhileTouching = true;
    
    [Tooltip("音声検出の更新頻度（フレーム単位）")]
    [SerializeField] private int audioUpdateFrequency = 1;
    
    [Header("References for Audio Detection")]
    [Tooltip("音量分析コンポーネント（自動検索可能）")]
    [SerializeField] private VolumeAnalyzer volumeAnalyzer;
    
    [Tooltip("ピッチ分析コンポーネント（自動検索可能）")]
    [SerializeField] private ImprovedPitchAnalyzer pitchAnalyzer;
    
    // 状態
    private bool isTouching = false;
    private Color currentColor;
    private Color targetColor;
    private int frameCount = 0;
    
    // イベント
    public System.Action<TouchableObject> OnTouchStarted;
    public System.Action<TouchableObject> OnTouchEnded;
    public System.Action<TouchableObject, Color> OnColorChanged;
    
    void Start()
    {
        // 参照の自動検索
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
            // SpriteRendererの場合は除外（SpriteRendererはRendererを継承しているが、専用処理を使用）
            if (targetRenderer is SpriteRenderer)
            {
                targetRenderer = null;
            }
        }
        
        if (targetImage == null)
        {
            targetImage = GetComponent<UnityEngine.UI.Image>();
        }
        
        if (colorCalculator == null)
        {
            colorCalculator = FindObjectOfType<ColorCalculator>();
        }
        
        if (neutralSoundDetector == null)
        {
            neutralSoundDetector = FindObjectOfType<NeutralSoundDetector>();
        }
        
        if (volumeAnalyzer == null)
        {
            volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        }
        
        if (pitchAnalyzer == null)
        {
            pitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        }
        
        // 初期色を設定
        currentColor = initialColor;
        targetColor = initialColor;
        ApplyColor(initialColor);
        
        // Collider2Dの確認
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning($"TouchableObject: {gameObject.name}にCollider2Dが設定されていません");
        }
    }
    
    void Update()
    {
        // タッチ検出
        if (enableTouchDetection)
        {
            CheckTouch();
        }
        
        // タッチ中の音声検出と色変更
        if (isTouching && detectAudioWhileTouching)
        {
            UpdateColorFromAudio();
        }
        
        // 色のスムージング
        if (currentColor != targetColor)
        {
            currentColor = Color.Lerp(currentColor, targetColor, colorSmoothing);
            ApplyColor(currentColor);
        }
    }
    
    /// <summary>
    /// タッチを検出
    /// </summary>
    private void CheckTouch()
    {
        bool wasTouching = isTouching;
        isTouching = false;
        
        // マウスクリック検出
        if (detectMouseClick && Input.GetMouseButton(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D collider = GetComponent<Collider2D>();
            
            if (collider != null && collider.OverlapPoint(mousePosition))
            {
                isTouching = true;
            }
        }
        
        // タッチ検出（モバイル）
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                Collider2D collider = GetComponent<Collider2D>();
                
                if (collider != null && collider.OverlapPoint(touchPosition))
                {
                    isTouching = true;
                    break;
                }
            }
        }
        
        // タッチ開始
        if (!wasTouching && isTouching)
        {
            OnTouchStarted?.Invoke(this);
            Debug.Log($"TouchableObject: {gameObject.name}がタッチされました");
        }
        
        // タッチ終了
        if (wasTouching && !isTouching)
        {
            OnTouchEnded?.Invoke(this);
            Debug.Log($"TouchableObject: {gameObject.name}のタッチが終了しました");
        }
    }
    
    /// <summary>
    /// 音声から色を更新
    /// </summary>
    private void UpdateColorFromAudio()
    {
        // 更新頻度チェック
        frameCount++;
        if (frameCount % audioUpdateFrequency != 0)
        {
            return;
        }
        
        // Neutral Soundが検出されているか確認
        if (neutralSoundDetector == null || !neutralSoundDetector.IsDetected)
        {
            return;
        }
        
        // 現在の音声データを取得
        float currentPitch = GetCurrentPitch();
        float currentVolume = GetCurrentVolume();
        
        // 有効な音声データか確認
        if (currentPitch <= 0f || currentVolume <= 0f)
        {
            return;
        }
        
        // 比率を計算
        if (colorCalculator != null)
        {
            Vector2 ratios = colorCalculator.CalculateRatios(
                currentPitch,
                currentVolume,
                neutralSoundDetector.NeutralPitch,
                neutralSoundDetector.NeutralVolume
            );
            
            // 色を計算
            Color newColor = colorCalculator.CalculateColor(ratios.x, ratios.y);
            SetTargetColor(newColor);
        }
    }
    
    /// <summary>
    /// 現在のピッチを取得
    /// </summary>
    private float GetCurrentPitch()
    {
        if (pitchAnalyzer != null)
        {
            return pitchAnalyzer.lastDetectedPitch;
        }
        return 0f;
    }
    
    /// <summary>
    /// 現在のボリュームを取得
    /// </summary>
    private float GetCurrentVolume()
    {
        if (volumeAnalyzer != null)
        {
            return volumeAnalyzer.CurrentVolume;
        }
        return 0f;
    }
    
    /// <summary>
    /// ターゲット色を設定
    /// </summary>
    public void SetTargetColor(Color color)
    {
        targetColor = color;
    }
    
    /// <summary>
    /// 色を即座に設定
    /// </summary>
    public void SetColorImmediate(Color color)
    {
        currentColor = color;
        targetColor = color;
        ApplyColor(color);
    }
    
    /// <summary>
    /// 色を適用
    /// </summary>
    private void ApplyColor(Color color)
    {
        // SpriteRendererの場合は専用処理（優先）
        if (targetSpriteRenderer != null)
        {
            targetSpriteRenderer.color = color;
        }
        // 通常のRendererの場合
        else if (targetRenderer != null)
        {
            if (targetRenderer.material != null)
            {
                targetRenderer.material.color = color;
            }
        }
        
        // UI Imageの場合
        if (targetImage != null)
        {
            targetImage.color = color;
        }
        
        OnColorChanged?.Invoke(this, color);
    }
    
    /// <summary>
    /// 初期色にリセット
    /// </summary>
    public void ResetColor()
    {
        SetColorImmediate(initialColor);
    }
    
    /// <summary>
    /// タッチ中かどうかを取得
    /// </summary>
    public bool IsTouching => isTouching;
    
    /// <summary>
    /// 現在の色を取得
    /// </summary>
    public Color CurrentColor => currentColor;
}

