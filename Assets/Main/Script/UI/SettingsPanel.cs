using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 設定画面UIパネル
/// カリブレーション値、検知閾値比率をスライダーで調整できる
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
    
    [Header("Volume Detection Ratio")]
    [Tooltip("検知閾値の比率スライダー（MinVolumeに対するパーセンテージ、0.0-1.0）")]
    [SerializeField] private Slider volumeDetectionRatioSlider;
    
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
    
    [Tooltip("PaintCanvas（キャンバスクリア用、自動検索される）")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Header("Navigation")]
    [Tooltip("戻るボタン")]
    [SerializeField] private Button backButton;
    
    [Tooltip("次へボタン（オフライン/オンライン選択画面へ遷移）")]
    [SerializeField] private Button nextButton;
    
    [Tooltip("オフライン/オンライン選択パネル")]
    [SerializeField] private OnlineOfflineSelectionPanel onlineOfflineSelectionPanel;
    
    [Tooltip("ゲームセレクト画面への参照（後方互換性のため保持）")]
    [SerializeField] private GameModeSelectionPanel gameModeSelectionPanel;
    
    [Header("Canvas Actions")]
    [Tooltip("色を消すボタン（キャンバスをクリア）")]
    [SerializeField] private Button clearCanvasButton;
    
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
        
        // PaintCanvasの取得（キャンバスクリア用）
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
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
        
        // 次へボタンの設定
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(TransitionToOnlineOfflineSelection);
        }
        
        // OnlineOfflineSelectionPanelの自動検索（未設定の場合）
        if (onlineOfflineSelectionPanel == null)
        {
            onlineOfflineSelectionPanel = FindObjectOfType<OnlineOfflineSelectionPanel>();
        }
        
        // GameModeSelectionPanelの自動検索（未設定の場合、後方互換性のため）
        if (gameModeSelectionPanel == null)
        {
            gameModeSelectionPanel = FindObjectOfType<GameModeSelectionPanel>();
        }
        
        // 色を消すボタンの設定
        if (clearCanvasButton != null)
        {
            clearCanvasButton.onClick.RemoveAllListeners();
            clearCanvasButton.onClick.AddListener(ClearCanvas);
        }
        
        isInitialized = true;
    }
    
    void OnEnable()
    {
        if (isInitialized)
        {
            // パネルが表示されるたびに現在の値を読み込む
            LoadCurrentValues();
            
            // 表示時に現在のカリブレーション値を保存
            // （SetActive(true)で直接表示された場合でも保存されるように）
            SaveCurrentCalibrationValues();
        }
    }
    
    void OnDisable()
    {
        if (isInitialized)
        {
            // 非表示になる前に現在のカリブレーション値を保存
            // （SetActive(false)で直接非表示になった場合でも保存されるように）
            SaveCurrentCalibrationValues();
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
            // 最小値を40 dB相当の振幅値に設定
            float minVolumeInDb = 40f;
            float minVolumeAmplitude = CalibrationSettings.ConvertDbToAmplitude(minVolumeInDb);
            minVolumeSlider.minValue = minVolumeAmplitude;
            minVolumeSlider.maxValue = 1f;
            // 初期値を設定（VoiceCalibratorの現在値がデフォルト値(0f)または最小値未満の場合はCalibrationSettingsの初期値を使用）
            float currentMinVol = (VoiceCalibrator.MinVolume == 0f || VoiceCalibrator.MinVolume < minVolumeAmplitude) 
                ? initialMinVol 
                : VoiceCalibrator.MinVolume;
            // 最小値未満にならないように調整
            if (currentMinVol < minVolumeAmplitude)
            {
                currentMinVol = minVolumeAmplitude;
            }
            minVolumeSlider.value = currentMinVol;
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
        
        // 検知閾値比率のスライダー設定
        if (volumeDetectionRatioSlider != null)
        {
            volumeDetectionRatioSlider.minValue = 0f;
            volumeDetectionRatioSlider.maxValue = 1f;
            // 初期値を設定（実際の値があればそれを優先）
            if (improvedPitchAnalyzer != null)
            {
                volumeDetectionRatioSlider.value = improvedPitchAnalyzer.volumeDetectionRatio;
            }
            else
            {
                volumeDetectionRatioSlider.value = 0.75f; // デフォルト値
            }
            volumeDetectionRatioSlider.onValueChanged.RemoveAllListeners();
            volumeDetectionRatioSlider.onValueChanged.AddListener(OnVolumeDetectionRatioChanged);
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
            // 40 dB相当の最小値（約0.006）をチェック
            float minVolumeInDb = 40f;
            float minVolumeAmplitude = CalibrationSettings.ConvertDbToAmplitude(minVolumeInDb);
            float currentMinVol = VoiceCalibrator.MinVolume;
            
            // 最小値未満の場合は調整
            if (currentMinVol < minVolumeAmplitude)
            {
                currentMinVol = minVolumeAmplitude;
                // VoiceCalibratorの値も更新
                if (voiceCalibrator != null)
                {
                    voiceCalibrator.SetCalibrationValuesManually(
                        currentMinVol, 
                        VoiceCalibrator.MaxVolume, 
                        VoiceCalibrator.MinPitch, 
                        VoiceCalibrator.MaxPitch
                    );
                }
            }
            
            minVolumeSlider.value = currentMinVol;
            UpdateValueText(minVolumeValueText, currentMinVol, "F3");
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
        
        // 検知閾値比率
        if (volumeDetectionRatioSlider != null && improvedPitchAnalyzer != null)
        {
            volumeDetectionRatioSlider.value = improvedPitchAnalyzer.volumeDetectionRatio;
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
        // 40 dB相当の最小値（約0.006）をチェック
        float minVolumeInDb = 40f;
        float minVolumeAmplitude = CalibrationSettings.ConvertDbToAmplitude(minVolumeInDb);
        
        if (value < minVolumeAmplitude)
        {
            // 最小値未満の場合は調整
            value = minVolumeAmplitude;
            minVolumeSlider.value = value;
        }
        
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
    /// 検知閾値比率が変更された時
    /// </summary>
    void OnVolumeDetectionRatioChanged(float value)
    {
        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.volumeDetectionRatio = value;
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
    /// 現在のカリブレーション値を保存
    /// </summary>
    void SaveCurrentCalibrationValues()
    {
        Debug.Log("SettingsPanel: SaveCurrentCalibrationValues() が呼ばれました");
        
        // 現在のカリブレーション値を取得（スライダーの値またはVoiceCalibratorの現在値）
        float minVol = minVolumeSlider != null ? minVolumeSlider.value : VoiceCalibrator.MinVolume;
        float maxVol = maxVolumeSlider != null ? maxVolumeSlider.value : VoiceCalibrator.MaxVolume;
        float minPit = minPitchSlider != null ? minPitchSlider.value : VoiceCalibrator.MinPitch;
        float maxPit = maxPitchSlider != null ? maxPitchSlider.value : VoiceCalibrator.MaxPitch;
        
        Debug.Log($"SettingsPanel: 保存する値 - Volume: {minVol:F3} - {maxVol:F3}, Pitch: {minPit:F1} - {maxPit:F1} Hz");
        
        // カリブレーションデータを保存
        bool success = CalibrationSaveSystem.SaveCalibrationData(minVol, maxVol, minPit, maxPit);
        if (success)
        {
            Debug.Log($"SettingsPanel: カリブレーション値を保存しました - Volume: {minVol:F3} - {maxVol:F3}, Pitch: {minPit:F1} - {maxPit:F1} Hz");
        }
        else
        {
            Debug.LogError("SettingsPanel: カリブレーション値の保存に失敗しました");
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
        
        // 表示時に現在のカリブレーション値を保存
        SaveCurrentCalibrationValues();
    }
    
    /// <summary>
    /// 設定画面を非表示
    /// </summary>
    public void Hide()
    {
        // 非表示になる前に現在のカリブレーション値を保存
        SaveCurrentCalibrationValues();
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// オフライン/オンライン選択画面に遷移
    /// </summary>
    private void TransitionToOnlineOfflineSelection()
    {
        // 設定画面を非表示（settingsPanelとthis.gameObjectの両方を非表示にする）
        Hide();
        if (gameObject != null)
        {
            gameObject.SetActive(false);
        }
        
        // オフライン/オンライン選択画面を表示
        if (onlineOfflineSelectionPanel != null)
        {
            onlineOfflineSelectionPanel.Show();
            Debug.Log("SettingsPanel: オフライン/オンライン選択画面に遷移しました");
        }
        else if (gameModeSelectionPanel != null)
        {
            // 後方互換性: OnlineOfflineSelectionPanelがない場合は直接GameModeSelectionPanelに遷移
            gameModeSelectionPanel.Show();
            Debug.LogWarning("SettingsPanel: OnlineOfflineSelectionPanelが設定されていません。GameModeSelectionPanelに直接遷移します（後方互換性）");
        }
        else
        {
            Debug.LogWarning("SettingsPanel: OnlineOfflineSelectionPanel または GameModeSelectionPanel が設定されていません");
        }
    }
    
    /// <summary>
    /// キャンバスをクリア（色を消す）
    /// CreativeModeManagerのClearCanvasと同じ処理
    /// </summary>
    private void ClearCanvas()
    {
        if (paintCanvas != null)
        {
            paintCanvas.ResetCanvas();
            Debug.Log("SettingsPanel: キャンバスをクリアしました");
        }
        else
        {
            Debug.LogWarning("SettingsPanel: PaintCanvasが見つかりません。キャンバスをクリアできません。");
        }
    }
}

