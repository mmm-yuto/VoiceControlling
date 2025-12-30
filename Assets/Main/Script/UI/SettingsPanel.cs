using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 設定画面UIパネル
/// カリブレーション値、音量閾値、位置のセンシティビティをスライダーで調整できる
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("設定画面のルートオブジェクト")]
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Calibration Sliders")]
    [Tooltip("最小音量のスライダー")]
    [SerializeField] private Slider minVolumeSlider;
    
    [Tooltip("最大音量のスライダー")]
    [SerializeField] private Slider maxVolumeSlider;
    
    [Tooltip("最小ピッチのスライダー")]
    [SerializeField] private Slider minPitchSlider;
    
    [Tooltip("最大ピッチのスライダー")]
    [SerializeField] private Slider maxPitchSlider;
    
    [Header("Volume Threshold Sliders")]
    [Tooltip("ImprovedPitchAnalyzerの音量閾値スライダー")]
    [SerializeField] private Slider volumeThresholdSlider;
    
    [Tooltip("VoiceInputHandlerの無音判定閾値スライダー（オプション）")]
    [SerializeField] private Slider silenceVolumeThresholdSlider;
    
    [Header("Position Sensitivity Sliders")]
    [Tooltip("スムージング係数のスライダー（0.0-1.0）")]
    [SerializeField] private Slider positionSmoothingSlider;
    
    [Header("Value Display Labels (Optional)")]
    [Tooltip("最小音量の現在値を表示するテキスト")]
    [SerializeField] private TextMeshProUGUI minVolumeValueText;
    
    [Tooltip("最大音量の現在値を表示するテキスト")]
    [SerializeField] private TextMeshProUGUI maxVolumeValueText;
    
    [Tooltip("最小ピッチの現在値を表示するテキスト")]
    [SerializeField] private TextMeshProUGUI minPitchValueText;
    
    [Tooltip("最大ピッチの現在値を表示するテキスト")]
    [SerializeField] private TextMeshProUGUI maxPitchValueText;
    
    [Header("References")]
    [Tooltip("VoiceCalibrator（自動検索される）")]
    [SerializeField] private VoiceCalibrator voiceCalibrator;
    
    [Tooltip("VoiceToScreenMapper（自動検索される）")]
    [SerializeField] private VoiceToScreenMapper voiceToScreenMapper;
    
    [Tooltip("ImprovedPitchAnalyzer（自動検索される）")]
    [SerializeField] private ImprovedPitchAnalyzer improvedPitchAnalyzer;
    
    [Tooltip("VoiceInputHandler（自動検索される）")]
    [SerializeField] private VoiceInputHandler voiceInputHandler;
    
    [Tooltip("CalibrationSettings（初期値を取得するために使用、自動検索される）")]
    [SerializeField] private CalibrationSettings calibrationSettings;
    
    [Header("Back Button")]
    [Tooltip("戻るボタン")]
    [SerializeField] private Button backButton;
    
    private bool isInitialized = false;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        // 参照が設定されていない場合は自動検索
        if (voiceCalibrator == null)
        {
            voiceCalibrator = FindObjectOfType<VoiceCalibrator>();
        }
        
        if (voiceToScreenMapper == null)
        {
            voiceToScreenMapper = FindObjectOfType<VoiceToScreenMapper>();
        }
        
        if (improvedPitchAnalyzer == null)
        {
            improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        }
        
        if (voiceInputHandler == null)
        {
            voiceInputHandler = FindObjectOfType<VoiceInputHandler>();
        }
        
        // CalibrationSettingsの取得
        if (calibrationSettings == null && voiceCalibrator != null)
        {
            // VoiceCalibratorからCalibrationSettingsを取得
            calibrationSettings = voiceCalibrator.calibrationSettings;
        }
        
        // スライダーの設定（初期値も設定）
        SetupSliders();
        
        // 現在の値をスライダーに反映
        LoadCurrentValues();
        
        // 戻るボタンの設定
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Hide);
        }
        
        isInitialized = true;
    }
    
    void OnEnable()
    {
        if (isInitialized)
        {
            // パネルが表示されるたびに現在の値を読み込む
            LoadCurrentValues();
        }
    }
    
    /// <summary>
    /// スライダーの設定（範囲とイベント、初期値）
    /// </summary>
    void SetupSliders()
    {
        // CalibrationSettingsから初期値を取得
        float initialMinVol = 0f;
        float initialMaxVol = 1f;
        float initialMinPit = 80f;
        float initialMaxPit = 1000f;
        
        if (calibrationSettings != null)
        {
            initialMinVol = calibrationSettings.GetInitialMinVolume();
            initialMaxVol = calibrationSettings.GetInitialMaxVolume();
            initialMinPit = calibrationSettings.initialMinPitch;
            initialMaxPit = calibrationSettings.initialMaxPitch;
        }
        
        // カリブレーション値のスライダー設定
        if (minVolumeSlider != null)
        {
            minVolumeSlider.minValue = 0f;
            minVolumeSlider.maxValue = 1f;
            // 初期値を設定（VoiceCalibratorの現在値がデフォルト値(0f)の場合はCalibrationSettingsの初期値を使用）
            minVolumeSlider.value = (VoiceCalibrator.MinVolume == 0f) ? initialMinVol : VoiceCalibrator.MinVolume;
            minVolumeSlider.onValueChanged.RemoveAllListeners();
            minVolumeSlider.onValueChanged.AddListener(OnMinVolumeChanged);
        }
        
        if (maxVolumeSlider != null)
        {
            maxVolumeSlider.minValue = 0f;
            maxVolumeSlider.maxValue = 1f;
            // 初期値を設定（VoiceCalibratorの現在値がデフォルト値(1f)の場合はCalibrationSettingsの初期値を使用）
            maxVolumeSlider.value = (VoiceCalibrator.MaxVolume == 1f) ? initialMaxVol : VoiceCalibrator.MaxVolume;
            maxVolumeSlider.onValueChanged.RemoveAllListeners();
            maxVolumeSlider.onValueChanged.AddListener(OnMaxVolumeChanged);
        }
        
        if (minPitchSlider != null)
        {
            minPitchSlider.minValue = 80f;
            minPitchSlider.maxValue = 1000f;
            // 初期値を設定（VoiceCalibratorの現在値がデフォルト値(80f)の場合はCalibrationSettingsの初期値を使用）
            minPitchSlider.value = (VoiceCalibrator.MinPitch == 80f) ? initialMinPit : VoiceCalibrator.MinPitch;
            minPitchSlider.onValueChanged.RemoveAllListeners();
            minPitchSlider.onValueChanged.AddListener(OnMinPitchChanged);
        }
        
        if (maxPitchSlider != null)
        {
            maxPitchSlider.minValue = 80f;
            maxPitchSlider.maxValue = 1000f;
            // 初期値を設定（VoiceCalibratorの現在値がデフォルト値(1000f)の場合はCalibrationSettingsの初期値を使用）
            maxPitchSlider.value = (VoiceCalibrator.MaxPitch == 1000f) ? initialMaxPit : VoiceCalibrator.MaxPitch;
            maxPitchSlider.onValueChanged.RemoveAllListeners();
            maxPitchSlider.onValueChanged.AddListener(OnMaxPitchChanged);
        }
        
        // 音量閾値のスライダー設定
        if (volumeThresholdSlider != null)
        {
            volumeThresholdSlider.minValue = 0f;
            volumeThresholdSlider.maxValue = 0.1f;
            // 初期値を設定（実際の値があればそれを優先）
            if (improvedPitchAnalyzer != null)
            {
                volumeThresholdSlider.value = improvedPitchAnalyzer.volumeThreshold;
            }
            volumeThresholdSlider.onValueChanged.RemoveAllListeners();
            volumeThresholdSlider.onValueChanged.AddListener(OnVolumeThresholdChanged);
        }
        
        if (silenceVolumeThresholdSlider != null)
        {
            silenceVolumeThresholdSlider.minValue = 0f;
            silenceVolumeThresholdSlider.maxValue = 0.1f;
            // 初期値を設定（実際の値があればそれを優先）
            if (voiceInputHandler != null)
            {
                silenceVolumeThresholdSlider.value = voiceInputHandler.silenceVolumeThreshold;
            }
            silenceVolumeThresholdSlider.onValueChanged.RemoveAllListeners();
            silenceVolumeThresholdSlider.onValueChanged.AddListener(OnSilenceVolumeThresholdChanged);
        }
        
        // 位置のセンシティビティのスライダー設定
        if (positionSmoothingSlider != null)
        {
            positionSmoothingSlider.minValue = 0f;
            positionSmoothingSlider.maxValue = 1f;
            // 初期値を設定（実際の値があればそれを優先）
            if (voiceInputHandler != null)
            {
                positionSmoothingSlider.value = voiceInputHandler.positionSmoothing;
            }
            positionSmoothingSlider.onValueChanged.RemoveAllListeners();
            positionSmoothingSlider.onValueChanged.AddListener(OnPositionSmoothingChanged);
        }
    }
    
    /// <summary>
    /// 現在の値をスライダーに読み込む
    /// </summary>
    void LoadCurrentValues()
    {
        // カリブレーション値
        if (minVolumeSlider != null)
        {
            minVolumeSlider.value = VoiceCalibrator.MinVolume;
            UpdateValueText(minVolumeValueText, VoiceCalibrator.MinVolume, "F3");
        }
        
        if (maxVolumeSlider != null)
        {
            maxVolumeSlider.value = VoiceCalibrator.MaxVolume;
            UpdateValueText(maxVolumeValueText, VoiceCalibrator.MaxVolume, "F3");
        }
        
        if (minPitchSlider != null)
        {
            minPitchSlider.value = VoiceCalibrator.MinPitch;
            UpdateValueText(minPitchValueText, VoiceCalibrator.MinPitch, "F1");
        }
        
        if (maxPitchSlider != null)
        {
            maxPitchSlider.value = VoiceCalibrator.MaxPitch;
            UpdateValueText(maxPitchValueText, VoiceCalibrator.MaxPitch, "F1");
        }
        
        // 音量閾値
        if (volumeThresholdSlider != null && improvedPitchAnalyzer != null)
        {
            volumeThresholdSlider.value = improvedPitchAnalyzer.volumeThreshold;
        }
        
        if (silenceVolumeThresholdSlider != null && voiceInputHandler != null)
        {
            silenceVolumeThresholdSlider.value = voiceInputHandler.silenceVolumeThreshold;
        }
        
        // センシティビティ
        if (positionSmoothingSlider != null && voiceInputHandler != null)
        {
            positionSmoothingSlider.value = voiceInputHandler.positionSmoothing;
        }
    }
    
    /// <summary>
    /// 値表示テキストを更新
    /// </summary>
    void UpdateValueText(TextMeshProUGUI text, float value, string format)
    {
        if (text != null)
        {
            text.text = value.ToString(format);
        }
    }
    
    /// <summary>
    /// 最小音量が変更された時
    /// </summary>
    void OnMinVolumeChanged(float value)
    {
        if (maxVolumeSlider != null && value >= maxVolumeSlider.value)
        {
            // 最小値が最大値以上にならないように調整
            value = maxVolumeSlider.value - 0.001f;
            minVolumeSlider.value = value;
        }
        
        ApplyCalibrationValues();
        UpdateValueText(minVolumeValueText, value, "F3");
    }
    
    /// <summary>
    /// 最大音量が変更された時
    /// </summary>
    void OnMaxVolumeChanged(float value)
    {
        if (minVolumeSlider != null && value <= minVolumeSlider.value)
        {
            // 最大値が最小値以下にならないように調整
            value = minVolumeSlider.value + 0.001f;
            maxVolumeSlider.value = value;
        }
        
        ApplyCalibrationValues();
        UpdateValueText(maxVolumeValueText, value, "F3");
    }
    
    /// <summary>
    /// 最小ピッチが変更された時
    /// </summary>
    void OnMinPitchChanged(float value)
    {
        if (maxPitchSlider != null && value >= maxPitchSlider.value)
        {
            // 最小値が最大値以上にならないように調整
            value = maxPitchSlider.value - 1f;
            minPitchSlider.value = value;
        }
        
        ApplyCalibrationValues();
        UpdateValueText(minPitchValueText, value, "F1");
    }
    
    /// <summary>
    /// 最大ピッチが変更された時
    /// </summary>
    void OnMaxPitchChanged(float value)
    {
        if (minPitchSlider != null && value <= minPitchSlider.value)
        {
            // 最大値が最小値以下にならないように調整
            value = minPitchSlider.value + 1f;
            maxPitchSlider.value = value;
        }
        
        ApplyCalibrationValues();
        UpdateValueText(maxPitchValueText, value, "F1");
    }
    
    /// <summary>
    /// 音量閾値が変更された時
    /// </summary>
    void OnVolumeThresholdChanged(float value)
    {
        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.volumeThreshold = value;
        }
    }
    
    /// <summary>
    /// 無音判定閾値が変更された時
    /// </summary>
    void OnSilenceVolumeThresholdChanged(float value)
    {
        if (voiceInputHandler != null)
        {
            voiceInputHandler.silenceVolumeThreshold = value;
        }
    }
    
    /// <summary>
    /// 位置のスムージング係数が変更された時
    /// </summary>
    void OnPositionSmoothingChanged(float value)
    {
        if (voiceInputHandler != null)
        {
            voiceInputHandler.positionSmoothing = value;
        }
    }
    
    /// <summary>
    /// カリブレーション値を適用
    /// </summary>
    void ApplyCalibrationValues()
    {
        float minVol = minVolumeSlider != null ? minVolumeSlider.value : VoiceCalibrator.MinVolume;
        float maxVol = maxVolumeSlider != null ? maxVolumeSlider.value : VoiceCalibrator.MaxVolume;
        float minPit = minPitchSlider != null ? minPitchSlider.value : VoiceCalibrator.MinPitch;
        float maxPit = maxPitchSlider != null ? maxPitchSlider.value : VoiceCalibrator.MaxPitch;
        
        // VoiceCalibratorに値を設定（手動設定用のメソッドを呼び出す）
        if (voiceCalibrator != null)
        {
            voiceCalibrator.SetCalibrationValuesManually(minVol, maxVol, minPit, maxPit);
        }
        else
        {
            // VoiceCalibratorが見つからない場合は、VoiceToScreenMapperのみ更新
            if (voiceToScreenMapper != null)
            {
                voiceToScreenMapper.UpdateCalibrationRanges(minVol, maxVol, minPit, maxPit);
            }
        }
    }
    
    /// <summary>
    /// 設定画面を表示
    /// </summary>
    public void Show()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        
        // 表示時に現在の値を読み込む
        LoadCurrentValues();
    }
    
    /// <summary>
    /// 設定画面を非表示
    /// </summary>
    public void Hide()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
}

