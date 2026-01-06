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
    
    [Header("UI References")]
    [Tooltip("Neutral Sound検出開始ボタン")]
    [SerializeField] private Button detectNeutralSoundButton;
    
    [Tooltip("Neutral Sound検出停止ボタン")]
    [SerializeField] private Button stopDetectionButton;
    
    [Tooltip("Neutral Soundリセットボタン")]
    [SerializeField] private Button resetNeutralSoundButton;
    
    [Tooltip("状態表示テキスト")]
    [SerializeField] private TextMeshProUGUI statusText;
    
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
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}

