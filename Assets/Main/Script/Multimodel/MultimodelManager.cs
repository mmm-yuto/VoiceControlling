using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Multimodel Systemの全体制御を行うマネージャー
/// Neutral Sound検出とオブジェクトタッチの連携を管理
/// </summary>
public class MultimodelManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("NeutralSoundDetectorコンポーネント（自動検索可能）")]
    [SerializeField] private NeutralSoundDetector neutralSoundDetector;
    
    [Tooltip("ColorCalculatorコンポーネント（自動検索可能）")]
    [SerializeField] private ColorCalculator colorCalculator;
    
    [Tooltip("音量分析コンポーネント（自動検索可能）")]
    [SerializeField] private VolumeAnalyzer volumeAnalyzer;
    
    [Tooltip("ピッチ分析コンポーネント（自動検索可能）")]
    [SerializeField] private ImprovedPitchAnalyzer pitchAnalyzer;
    
    [Header("UI References")]
    [Tooltip("Neutral Sound検出開始ボタン")]
    [SerializeField] private Button detectNeutralSoundButton;
    
    [Tooltip("Neutral Sound検出停止ボタン")]
    [SerializeField] private Button stopDetectionButton;
    
    [Tooltip("Neutral Soundリセットボタン")]
    [SerializeField] private Button resetNeutralSoundButton;
    
    [Tooltip("状態表示テキスト")]
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Emotion Text Display")]
    [Tooltip("感情状態を表示するテキスト（常時更新、オプション）")]
    [SerializeField] private TextMeshProUGUI emotionText;
    
    [Tooltip("ピッチがNeutral以下（比率 < 1.0）の時に表示するテキスト")]
    [SerializeField] private string sadText = "sad";
    
    [Tooltip("Neutral（比率 ≈ 1.0）の時に表示するテキスト")]
    [SerializeField] private string neutralText = "Neutral";
    
    [Tooltip("1.0 <= 比率 < 1.5 の時に表示するテキスト")]
    [SerializeField] private string disgustText = "disgust";
    
    [Tooltip("比率 >= 1.5 の時に表示するテキスト")]
    [SerializeField] private string happyText = "Happy";
    
    [Header("Settings")]
    [Tooltip("自動検出モード（シーン開始時に自動でNeutral Sound検出を開始）")]
    [SerializeField] private bool autoDetectOnStart = false;
    
    [Tooltip("検出時間（秒、0で手動停止のみ）")]
    [SerializeField] private float detectionDuration = 0f;
    
    // 状態
    private bool isInitialized = false;
    
    void Start()
    {
        Initialize();
    }
    
    void Update()
    {
        // 感情テキストを常時更新
        UpdateEmotionText();
    }
    
    /// <summary>
    /// 初期化
    /// </summary>
    private void Initialize()
    {
        // 参照の自動検索
        if (neutralSoundDetector == null)
        {
            neutralSoundDetector = FindObjectOfType<NeutralSoundDetector>();
        }
        
        if (colorCalculator == null)
        {
            colorCalculator = FindObjectOfType<ColorCalculator>();
        }
        
        if (volumeAnalyzer == null)
        {
            volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        }
        
        if (pitchAnalyzer == null)
        {
            pitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        }
        
        // UIボタンの設定
        SetupUIButtons();
        
        // イベント購読
        SubscribeToEvents();
        
        // 自動検出
        if (autoDetectOnStart && neutralSoundDetector != null)
        {
            StartNeutralSoundDetection();
        }
        
        isInitialized = true;
        UpdateStatus("Multimodel System initialized");
        
        Debug.Log("MultimodelManager: Initialized");
    }
    
    /// <summary>
    /// UIボタンの設定
    /// </summary>
    private void SetupUIButtons()
    {
        if (detectNeutralSoundButton != null)
        {
            detectNeutralSoundButton.onClick.RemoveAllListeners();
            detectNeutralSoundButton.onClick.AddListener(StartNeutralSoundDetection);
        }
        
        if (stopDetectionButton != null)
        {
            stopDetectionButton.onClick.RemoveAllListeners();
            stopDetectionButton.onClick.AddListener(StopNeutralSoundDetection);
        }
        
        if (resetNeutralSoundButton != null)
        {
            resetNeutralSoundButton.onClick.RemoveAllListeners();
            resetNeutralSoundButton.onClick.AddListener(ResetNeutralSound);
        }
    }
    
    /// <summary>
    /// イベント購読
    /// </summary>
    private void SubscribeToEvents()
    {
        if (neutralSoundDetector != null)
        {
            neutralSoundDetector.OnNeutralSoundDetected += OnNeutralSoundDetected;
            neutralSoundDetector.OnDetectionStarted += OnDetectionStarted;
            neutralSoundDetector.OnDetectionStopped += OnDetectionStopped;
        }
    }
    
    /// <summary>
    /// イベント購読解除
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (neutralSoundDetector != null)
        {
            neutralSoundDetector.OnNeutralSoundDetected -= OnNeutralSoundDetected;
            neutralSoundDetector.OnDetectionStarted -= OnDetectionStarted;
            neutralSoundDetector.OnDetectionStopped -= OnDetectionStopped;
        }
    }
    
    /// <summary>
    /// Neutral Sound検出を開始
    /// </summary>
    public void StartNeutralSoundDetection()
    {
        if (neutralSoundDetector == null)
        {
            Debug.LogError("MultimodelManager: NeutralSoundDetector not found");
            UpdateStatus("Error: NeutralSoundDetector not found");
            return;
        }
        
        neutralSoundDetector.StartDetection();
        
        // 自動停止タイマー
        if (detectionDuration > 0f)
        {
            Invoke(nameof(StopNeutralSoundDetection), detectionDuration);
        }
    }
    
    /// <summary>
    /// Neutral Sound検出を停止
    /// </summary>
    public void StopNeutralSoundDetection()
    {
        if (neutralSoundDetector != null)
        {
            neutralSoundDetector.StopDetection();
        }
    }
    
    /// <summary>
    /// Neutral Soundをリセット
    /// </summary>
    public void ResetNeutralSound()
    {
        if (neutralSoundDetector != null)
        {
            neutralSoundDetector.ResetNeutralSound();
            UpdateStatus("Neutral Sound reset");
        }
    }
    
    /// <summary>
    /// Neutral Sound検出完了時の処理
    /// </summary>
    private void OnNeutralSoundDetected(float pitch, float volume)
    {
        UpdateStatus($"Neutral Sound detected - Pitch: {pitch:F1}Hz, Volume: {volume:F3}");
        Debug.Log($"MultimodelManager: Neutral Sound detected - Pitch: {pitch:F1}Hz, Volume: {volume:F3}");
    }
    
    /// <summary>
    /// 検出開始時の処理
    /// </summary>
    private void OnDetectionStarted()
    {
        UpdateStatus("Detecting Neutral Sound... Please speak");
    }
    
    /// <summary>
    /// 検出停止時の処理
    /// </summary>
    private void OnDetectionStopped()
    {
        if (neutralSoundDetector != null && neutralSoundDetector.IsDetected)
        {
            UpdateStatus($"Neutral Sound detected - Pitch: {neutralSoundDetector.NeutralPitch:F1}Hz, Volume: {neutralSoundDetector.NeutralVolume:F3}");
        }
        else
        {
            UpdateStatus("Neutral Sound not detected");
        }
    }
    
    /// <summary>
    /// 状態テキストを更新
    /// </summary>
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"MultimodelManager: {message}");
    }
    
    /// <summary>
    /// 現在の状態を取得
    /// </summary>
    public string GetStatus()
    {
        if (neutralSoundDetector == null)
        {
            return "NeutralSoundDetector not found";
        }
        
        if (neutralSoundDetector.IsDetected)
        {
            return $"Neutral Sound detected - Pitch: {neutralSoundDetector.NeutralPitch:F1}Hz, Volume: {neutralSoundDetector.NeutralVolume:F3}";
        }
        else if (neutralSoundDetector.IsDetecting)
        {
            return "Detecting Neutral Sound...";
        }
        else
        {
            return "Neutral Sound not detected";
        }
    }
    
    /// <summary>
    /// 感情テキストを更新
    /// </summary>
    private void UpdateEmotionText()
    {
        if (emotionText == null)
        {
            return;
        }
        
        // NeutralSoundが検出されているか確認
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
            
            // カテゴリを判定
            ColorCalculator.PitchCategory category = colorCalculator.GetCategory(ratios.x);
            
            // カテゴリに応じたテキストを選択
            string emotionString = GetEmotionText(category);
            emotionText.text = emotionString;
        }
    }
    
    /// <summary>
    /// カテゴリに応じた感情テキストを取得
    /// </summary>
    private string GetEmotionText(ColorCalculator.PitchCategory category)
    {
        switch (category)
        {
            case ColorCalculator.PitchCategory.LowPitch:
                return sadText;
            
            case ColorCalculator.PitchCategory.NeutralSound:
                return neutralText;
            
            case ColorCalculator.PitchCategory.MediumPitch:
                return disgustText;
            
            case ColorCalculator.PitchCategory.HighPitch:
                return happyText;
            
            default:
                return neutralText;
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
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}

