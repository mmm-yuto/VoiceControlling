using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public enum CalibrationStep
{
    None = 0,
    Step1_Silence = 1,      // 無音状態（音量最小値）
    Step2_LoudVoice = 2,     // 大きな声（音量最大値）
    Step3_LowPitch = 3,     // 低い声（ピッチ最小値）
    Step4_HighPitch = 4     // 高い声（ピッチ最大値）
}

public class VoiceCalibrator : MonoBehaviour
{
    // カリブレーション結果を他コンポーネントに提供
    public static float MinVolume { get; private set; } = 0f;
    public static float MaxVolume { get; private set; } = 1f;
    public static float MinPitch { get; private set; } = 80f;
    public static float MaxPitch { get; private set; } = 1000f;
    
    // グラフの中心位置（カリブレーション結果から計算）
    public static float CenterVolume { get; private set; } = 0.5f;
    public static float CenterPitch { get; private set; } = 500f;
    
    // カリブレーション状態（外部から確認可能）
    public static bool IsCalibrating { get; private set; } = false;
    public static bool IsIndividualCalibrating { get; private set; } = false;
    
    // イベント
    public static System.Action<string> OnCalibrationStatusUpdated;
    public static System.Action<float> OnCalibrationProgressUpdated;
    public static System.Action<bool> OnCalibrationRunningStateChanged;
    public static System.Action<int> OnCalibrationStepChanged; // (stepIndex)
    public static System.Action<float, float, float, float> OnCalibrationCompleted; // (minVol, maxVol, minPitch, maxPitch)

    [Header("Calibration Settings")]
    public CalibrationSettings calibrationSettings;
    
    [Header("UI References")]
    public TextMeshProUGUI calibrationStatusText;
    public Slider calibrationProgressSlider;
    public Button startCalibrationButton;
    
    [Header("Graph Edge Labels")]
    [Tooltip("最小音量を表示するテキスト（グラフの端）")]
    [SerializeField] private TextMeshProUGUI minVolumeText;
    
    [Tooltip("最大音量を表示するテキスト（グラフの端）")]
    [SerializeField] private TextMeshProUGUI maxVolumeText;
    
    [Tooltip("最小ピッチを表示するテキスト（グラフの端）")]
    [SerializeField] private TextMeshProUGUI minPitchText;
    
    [Tooltip("最大ピッチを表示するテキスト（グラフの端）")]
    [SerializeField] private TextMeshProUGUI maxPitchText;
    
    [Header("Settings Buttons")]
    [Tooltip("設定画面を表示するボタンの配列（インスペクターで設定）")]
    [SerializeField] private Button[] settingsButtons;
    
    [Header("Settings Objects")]
    [Tooltip("各ボタンに対応する設定画面用オブジェクトの配列（settingsButtonsと同じ順序で設定）")]
    [SerializeField] private GameObject[] settingsObjects;
    
    [Header("Voice Detection Toggle")]
    [Tooltip("音検知のOn/Offを切り替えるボタン（インスペクターで設定）")]
    [SerializeField] private Button voiceDetectionToggleButton;
    
    [Tooltip("音検知の状態を表示するテキスト（オプション）")]
    [SerializeField] private TextMeshProUGUI voiceDetectionStatusText;
    
    [Header("Game Selection Panel")]
    [Tooltip("ゲームセレクト画面への参照（最初の遷移で使用）")]
    [SerializeField] private GameModeSelectionPanel gameModeSelectionPanel;
    
    [Header("Target Components")]
    public VoiceDisplay voiceDisplay;
    public CalibrationPanel calibrationPanel;
    
    private VolumeAnalyzer volumeAnalyzer;
    private ImprovedPitchAnalyzer improvedPitchAnalyzer;
    private VoiceDetector voiceDetector;
    private bool isCalibrating = false;
    private CalibrationStep currentStep = CalibrationStep.None;
    private float stepStartTime;
    
    // 各ステップで収集したデータ
    private List<float> step1VolumeSamples = new List<float>(); // 無音状態の音量
    private List<float> step2VolumeSamples = new List<float>(); // 大きな声の音量
    private List<float> step3PitchSamples = new List<float>();  // 低い声のピッチ
    private List<float> step4PitchSamples = new List<float>();  // 高い声のピッチ
    
    // 個別カリブレーション用のデータ
    private List<float> individualCalibrationSamples = new List<float>();
    private bool isIndividualCalibrating = false;
    private System.Action<float> individualCalibrationCallback = null;
    
    // カリブレーション結果
    private float minVolume = 0f;
    private float maxVolume = 1f;
    private float minPitch = 80f;
    private float maxPitch = 1000f;
    
    // 最初の遷移を追跡するフラグ
    private bool hasShownGameSelection = false;
    
    void Start()
    {
        // 音声分析コンポーネントを取得
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        voiceDetector = FindObjectOfType<VoiceDetector>();
        
        // ボタンのイベント設定
        if (startCalibrationButton != null)
        {
            startCalibrationButton.onClick.AddListener(StartCalibration);
        }
        
        // 音検知On/Offボタンのイベント設定
        if (voiceDetectionToggleButton != null)
        {
            voiceDetectionToggleButton.onClick.AddListener(ToggleVoiceDetection);
            UpdateVoiceDetectionButton();
        }
        
        // 設定ボタンのイベント設定
        SetupSettingsButtons();
        
        // GameModeSelectionPanelの自動検索（未設定の場合）
        if (gameModeSelectionPanel == null)
        {
            gameModeSelectionPanel = FindObjectOfType<GameModeSelectionPanel>();
        }
        
        // セーブデータを読み込み
        if (LoadSavedCalibrationData())
        {
            Debug.Log("VoiceCalibrator: セーブデータからカリブレーション値を読み込みました");
        }
        else
        {
            // セーブデータがない場合は初期カリブレーション値を適用
            ApplyInitialCalibrationValues();
        }
        
        // 初期状態の設定
        UpdateCalibrationStatus("Please start calibration");
        
        // デフォルト設定の確認
        if (calibrationSettings == null)
        {
            Debug.LogWarning("VoiceCalibrator: CalibrationSettingsが設定されていません。デフォルト値を使用します。");
        }
    }
    
    /// <summary>
    /// セーブデータからカリブレーションデータを読み込む
    /// </summary>
    /// <returns>読み込み成功時true</returns>
    bool LoadSavedCalibrationData()
    {
        CalibrationData data;
        if (CalibrationSaveSystem.LoadCalibrationData(out data))
        {
            // セーブデータから値を適用
            SetCalibrationValuesManually(data.minVolume, data.maxVolume, data.minPitch, data.maxPitch);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 初期カリブレーション値を適用（ゲーム開始時、カリブレーション未実行時）
    /// </summary>
    void ApplyInitialCalibrationValues()
    {
        if (calibrationSettings != null)
        {
            // CalibrationSettingsから初期値を取得（デシベル値から振幅値に変換）
            float initialMinVol = calibrationSettings.GetInitialMinVolume();
            float initialMaxVol = calibrationSettings.GetInitialMaxVolume();
            float initialMinPit = calibrationSettings.initialMinPitch;
            float initialMaxPit = calibrationSettings.initialMaxPitch;
            
            // 静的プロパティを更新
            MinVolume = initialMinVol;
            MaxVolume = initialMaxVol;
            MinPitch = initialMinPit;
            MaxPitch = initialMaxPit;
            
            // グラフの中心位置を計算
            CenterVolume = (initialMinVol + initialMaxVol) / 2f;
            CenterPitch = (initialMinPit + initialMaxPit) / 2f;
            
            // 内部変数も更新
            minVolume = initialMinVol;
            maxVolume = initialMaxVol;
            minPitch = initialMinPit;
            maxPitch = initialMaxPit;
            
            // 結果を各コンポーネントに適用
            ApplyCalibrationResults();
            
            // グラフの端のテキストを更新（ApplyCalibrationResults内でも呼ばれるが、念のため）
            UpdateGraphEdgeLabels();
            
            // デバッグログ（デシベル値も表示）
            float minVolDb = calibrationSettings.initialMinVolumeDb;
            float maxVolDb = calibrationSettings.initialMaxVolumeDb;
            Debug.Log($"VoiceCalibrator: Initial calibration values applied - Volume: {minVolDb:F0} - {maxVolDb:F0} dB ({initialMinVol:F3} - {initialMaxVol:F3}), Pitch: {initialMinPit:F1} - {initialMaxPit:F1} Hz");
        }
        else
        {
            // CalibrationSettingsがない場合でも、現在の値をテキストに反映
            UpdateGraphEdgeLabels();
        }
    }
    
    public void StartCalibration()
    {
        if (isCalibrating) return;
        
        isCalibrating = true;
        currentStep = CalibrationStep.Step1_Silence;
        
        // データをクリア
        step1VolumeSamples.Clear();
        step2VolumeSamples.Clear();
        step3PitchSamples.Clear();
        step4PitchSamples.Clear();
        
        // イベント購読
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected += OnVolumeDetected;
        
        if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected += OnPitchDetected;
        
        // ステップ開始
        StartCoroutine(CalibrationCoroutine());
        
        IsCalibrating = true;
        OnCalibrationRunningStateChanged?.Invoke(true);
        Debug.Log("Calibration started - Step 1: Silence");
    }
    
    public void CancelCalibration()
    {
        if (!isCalibrating) return;
        
        StopAllCoroutines();
        isCalibrating = false;
        IsCalibrating = false;
        currentStep = CalibrationStep.None;
        
        // イベント購読解除
        UnsubscribeFromEvents();
        
        OnCalibrationRunningStateChanged?.Invoke(false);
        UpdateCalibrationStatus("Calibration cancelled");
        
        if (calibrationProgressSlider != null)
            calibrationProgressSlider.value = 0f;
        
        Debug.Log("Calibration cancelled");
    }
    
    IEnumerator CalibrationCoroutine()
    {
        float stepDuration = calibrationSettings != null ? calibrationSettings.stepDuration : 3f;
        
        // Step 1: 無音状態（音量最小値）
        currentStep = CalibrationStep.Step1_Silence;
        stepStartTime = Time.time;
        OnCalibrationStepChanged?.Invoke(1);
        UpdateCalibrationStatus("Step 1: Please remain silent (measuring minimum volume)");
        
        if (calibrationPanel != null)
            calibrationPanel.UpdateStepLabel(1);
        
        yield return new WaitForSeconds(stepDuration);
        CompleteCurrentStep();
        
        // Step 2: 大きな声（音量最大値）
        currentStep = CalibrationStep.Step2_LoudVoice;
        stepStartTime = Time.time;
        OnCalibrationStepChanged?.Invoke(2);
        UpdateCalibrationStatus("Step 2: Please speak loudly (measuring maximum volume)");
        
        if (calibrationPanel != null)
            calibrationPanel.UpdateStepLabel(2);
        
        yield return new WaitForSeconds(stepDuration);
        CompleteCurrentStep();
        
        // Step 3: 低い声（ピッチ最小値）
        currentStep = CalibrationStep.Step3_LowPitch;
        stepStartTime = Time.time;
        OnCalibrationStepChanged?.Invoke(3);
        UpdateCalibrationStatus("Step 3: Please speak in a low voice (measuring minimum pitch)");
        
        if (calibrationPanel != null)
            calibrationPanel.UpdateStepLabel(3);
        
        yield return new WaitForSeconds(stepDuration);
        CompleteCurrentStep();
        
        // Step 4: 高い声（ピッチ最大値）
        currentStep = CalibrationStep.Step4_HighPitch;
        stepStartTime = Time.time;
        OnCalibrationStepChanged?.Invoke(4);
        UpdateCalibrationStatus("Step 4: Please speak in a high voice (measuring maximum pitch)");
        
        if (calibrationPanel != null)
            calibrationPanel.UpdateStepLabel(4);
        
        yield return new WaitForSeconds(stepDuration);
        CompleteCurrentStep();
        
        // 全ステップ完了
        CompleteCalibration();
    }
    
    void Update()
    {
        if (!isCalibrating) return;
        
        float stepDuration = calibrationSettings != null ? calibrationSettings.stepDuration : 3f;
        float elapsedTime = Time.time - stepStartTime;
        float progress = elapsedTime / stepDuration;
        
        // 全体の進捗を計算（4ステップ合計）
        float overallProgress = ((int)currentStep - 1) / 4f + (progress / 4f);
        
        if (calibrationProgressSlider != null)
        {
            calibrationProgressSlider.value = overallProgress;
        }
        
        OnCalibrationProgressUpdated?.Invoke(overallProgress);
    }
    
    void CompleteCurrentStep()
    {
        switch (currentStep)
        {
            case CalibrationStep.Step1_Silence:
                // 無音状態の音量平均値を計算
                if (step1VolumeSamples.Count > 0)
                {
                    minVolume = CalculateAverage(step1VolumeSamples);
                    if (calibrationSettings != null)
                        minVolume *= calibrationSettings.minVolumeMargin;
                }
                Debug.Log($"Step 1 Complete - Min Volume: {minVolume:F3}");
                break;
                
            case CalibrationStep.Step2_LoudVoice:
                // 大きな声の音量平均値を計算
                if (step2VolumeSamples.Count > 0)
                {
                    float avgVolume = CalculateAverage(step2VolumeSamples);
                    maxVolume = avgVolume;
                    if (calibrationSettings != null)
                        maxVolume *= calibrationSettings.maxVolumeMargin;
                    Debug.Log($"Step 2 Complete - Max Volume: {maxVolume:F3} (Average: {avgVolume:F3})");
                }
                break;
                
            case CalibrationStep.Step3_LowPitch:
                // 低い声のピッチ平均値を計算
                if (step3PitchSamples.Count > 0)
                {
                    float avgPitch = CalculateAverage(step3PitchSamples);
                    minPitch = avgPitch;
                    if (calibrationSettings != null)
                        minPitch *= calibrationSettings.minPitchMargin;
                    Debug.Log($"Step 3 Complete - Min Pitch: {minPitch:F1} Hz (Average: {avgPitch:F1} Hz)");
                }
                break;
                
            case CalibrationStep.Step4_HighPitch:
                // 高い声のピッチ平均値を計算
                if (step4PitchSamples.Count > 0)
                {
                    float avgPitch = CalculateAverage(step4PitchSamples);
                    maxPitch = avgPitch;
                    if (calibrationSettings != null)
                        maxPitch *= calibrationSettings.maxPitchMargin;
                    Debug.Log($"Step 4 Complete - Max Pitch: {maxPitch:F1} Hz (Average: {avgPitch:F1} Hz)");
                }
                break;
        }
    }
    
    void CompleteCalibration()
    {
        isCalibrating = false;
        IsCalibrating = false;
        currentStep = CalibrationStep.None;
        
        // イベント購読解除
        UnsubscribeFromEvents();
        
        // グラフの中心位置を計算
        CenterVolume = (minVolume + maxVolume) / 2f;
        CenterPitch = (minPitch + maxPitch) / 2f;
        
        // 静的プロパティを更新
        MinVolume = minVolume;
        MaxVolume = maxVolume;
        MinPitch = minPitch;
        MaxPitch = maxPitch;
        
        // 結果を各コンポーネントに適用
        ApplyCalibrationResults();
        
        // カリブレーションデータを保存
        CalibrationSaveSystem.SaveCalibrationData(minVolume, maxVolume, minPitch, maxPitch);
        
        // イベント通知
        OnCalibrationCompleted?.Invoke(minVolume, maxVolume, minPitch, maxPitch);
        OnCalibrationRunningStateChanged?.Invoke(false);
        
        // 結果を表示
        string result = $"Calibration Complete!\n" +
                       $"Volume Range: {minVolume:F3} - {maxVolume:F3}\n" +
                       $"Pitch Range: {minPitch:F1} - {maxPitch:F1} Hz\n" +
                       $"Center: Volume={CenterVolume:F3}, Pitch={CenterPitch:F1} Hz";
        
        UpdateCalibrationStatus(result);
        
        Debug.Log($"Calibration complete - Volume: {minVolume:F3} - {maxVolume:F3}, Pitch: {minPitch:F1} - {maxPitch:F1} Hz");
        Debug.Log($"Center - Volume: {CenterVolume:F3}, Pitch: {CenterPitch:F1} Hz");
    }
    
    void ApplyCalibrationResults()
    {
        // VoiceDisplayの設定を更新
        if (voiceDisplay != null)
        {
            voiceDisplay.SetPitchRange(minPitch, maxPitch);
            voiceDisplay.SetMaxVolume(maxVolume);
        }
        
        // ImprovedPitchAnalyzerの範囲を更新
        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.minFrequency = minPitch;
            improvedPitchAnalyzer.maxFrequency = maxPitch;
        }
        
        // VoiceToScreenMapperの範囲を更新
        VoiceToScreenMapper voiceToScreenMapper = FindObjectOfType<VoiceToScreenMapper>();
        if (voiceToScreenMapper != null)
        {
            voiceToScreenMapper.UpdateCalibrationRanges(minVolume, maxVolume, minPitch, maxPitch);
        }
        
        // グラフの端のテキストを更新
        UpdateGraphEdgeLabels();
    }
    
    /// <summary>
    /// グラフの端のテキストを更新
    /// </summary>
    void UpdateGraphEdgeLabels()
    {
        if (minVolumeText != null)
        {
            float minVolumeDb = ConvertAmplitudeToDb(MinVolume);
            minVolumeText.text = $"{minVolumeDb:F0} dB";
        }
        
        if (maxVolumeText != null)
        {
            float maxVolumeDb = ConvertAmplitudeToDb(MaxVolume);
            maxVolumeText.text = $"{maxVolumeDb:F0} dB";
        }
        
        if (minPitchText != null)
        {
            minPitchText.text = $"{MinPitch:F0} Hz";
        }
        
        if (maxPitchText != null)
        {
            maxPitchText.text = $"{MaxPitch:F0} Hz";
        }
    }
    
    /// <summary>
    /// 振幅値（0-1）をデシベル値に変換
    /// CalibrationSettings.ConvertDbToAmplitude()の逆変換
    /// </summary>
    /// <param name="amplitude">振幅値（0-1）</param>
    /// <returns>デシベル値（0-90）</returns>
    float ConvertAmplitudeToDb(float amplitude)
    {
        if (amplitude <= 0.0001f)
        {
            return 0f; // 無音
        }
        
        // 変換式: dB = (log10(amplitude) + 4) * 22.5
        // 0.0001 = 0 dB, 1.0 = 90 dB
        float db = (Mathf.Log10(amplitude) + 4f) * 22.5f;
        return Mathf.Clamp(db, 0f, 90f);
    }
    
    /// <summary>
    /// カリブレーション値を手動で設定する（SettingsPanelから呼び出される）
    /// </summary>
    public void SetCalibrationValuesManually(float newMinVolume, float newMaxVolume, float newMinPitch, float newMaxPitch)
    {
        // 内部変数を更新
        minVolume = newMinVolume;
        maxVolume = newMaxVolume;
        minPitch = newMinPitch;
        maxPitch = newMaxPitch;
        
        // 中心位置を計算
        CenterVolume = (minVolume + maxVolume) / 2f;
        CenterPitch = (minPitch + maxPitch) / 2f;
        
        // 静的プロパティを更新
        MinVolume = minVolume;
        MaxVolume = maxVolume;
        MinPitch = minPitch;
        MaxPitch = maxPitch;
        
        // 結果を各コンポーネントに適用
        ApplyCalibrationResults();
        
        // カリブレーションデータの保存は、SettingsPanelのShow()/Hide()で行う
        // （スライダーを動かすたびの保存を避けるため）
        
        // イベント通知（手動設定であることを示す）
        OnCalibrationCompleted?.Invoke(minVolume, maxVolume, minPitch, maxPitch);
        
        Debug.Log($"Calibration values set manually - Volume: {minVolume:F3} - {maxVolume:F3}, Pitch: {minPitch:F1} - {maxPitch:F1} Hz");
    }
    
    void OnVolumeDetected(float volume)
    {
        if (!isCalibrating) return;
        
        switch (currentStep)
        {
            case CalibrationStep.Step1_Silence:
                step1VolumeSamples.Add(volume);
                break;
            case CalibrationStep.Step2_LoudVoice:
                step2VolumeSamples.Add(volume);
                break;
        }
    }
    
    void OnPitchDetected(float pitch)
    {
        if (!isCalibrating) return;
        if (pitch <= 0f) return; // 無効なピッチは無視
        
        switch (currentStep)
        {
            case CalibrationStep.Step3_LowPitch:
                step3PitchSamples.Add(pitch);
                break;
            case CalibrationStep.Step4_HighPitch:
                step4PitchSamples.Add(pitch);
                break;
        }
    }
    
    void UnsubscribeFromEvents()
    {
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= OnVolumeDetected;
        if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected -= OnPitchDetected;
    }
    
    float CalculateAverage(List<float> samples)
    {
        if (samples.Count == 0) return 0f;
        
        float sum = 0f;
        foreach (float sample in samples)
        {
            sum += sample;
        }
        return sum / samples.Count;
    }
    
    float CalculateMax(List<float> samples)
    {
        if (samples.Count == 0) return 0f;
        
        float max = samples[0];
        foreach (float sample in samples)
        {
            if (sample > max) max = sample;
        }
        return max;
    }
    
    float CalculateMin(List<float> samples)
    {
        if (samples.Count == 0) return 0f;
        
        float min = samples[0];
        foreach (float sample in samples)
        {
            if (sample > 0f && sample < min) min = sample;
        }
        return min;
    }
    
    void UpdateCalibrationStatus(string message)
    {
        if (calibrationStatusText != null)
        {
            calibrationStatusText.text = message;
        }
        
        OnCalibrationStatusUpdated?.Invoke(message);
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    // ========== 個別カリブレーションメソッド ==========
    
    /// <summary>
    /// 音量最小値の個別カリブレーション
    /// </summary>
    public void CalibrateMinVolume()
    {
        if (isCalibrating || isIndividualCalibrating) return;
        StartCoroutine(CalibrateMinVolumeCoroutine());
    }
    
    /// <summary>
    /// 音量最大値の個別カリブレーション
    /// </summary>
    public void CalibrateMaxVolume()
    {
        if (isCalibrating || isIndividualCalibrating) return;
        StartCoroutine(CalibrateMaxVolumeCoroutine());
    }
    
    /// <summary>
    /// ピッチ最小値の個別カリブレーション
    /// </summary>
    public void CalibrateMinPitch()
    {
        if (isCalibrating || isIndividualCalibrating) return;
        StartCoroutine(CalibrateMinPitchCoroutine());
    }
    
    /// <summary>
    /// ピッチ最大値の個別カリブレーション
    /// </summary>
    public void CalibrateMaxPitch()
    {
        if (isCalibrating || isIndividualCalibrating) return;
        StartCoroutine(CalibrateMaxPitchCoroutine());
    }
    
    private IEnumerator CalibrateMinVolumeCoroutine()
    {
        isIndividualCalibrating = true;
        IsIndividualCalibrating = true;
        individualCalibrationSamples.Clear();
        
        // カリブレーション開始を通知
        OnCalibrationRunningStateChanged?.Invoke(true);
        
        float duration = calibrationSettings != null ? calibrationSettings.stepDuration : 3f;
        float startTime = Time.time;
        
        UpdateCalibrationStatus($"Calibrating minimum volume...\nPlease remain silent for {duration:F1} seconds");
        
        // イベント購読
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected += OnIndividualVolumeDetected;
        
        // サンプル収集
        while (Time.time - startTime < duration)
        {
            float progress = (Time.time - startTime) / duration;
            if (calibrationProgressSlider != null)
                calibrationProgressSlider.value = progress;
            OnCalibrationProgressUpdated?.Invoke(progress);
            yield return null;
        }
        
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= OnIndividualVolumeDetected;
        
        // 平均値を計算
        if (individualCalibrationSamples.Count > 0)
        {
            minVolume = CalculateAverage(individualCalibrationSamples);
            if (calibrationSettings != null)
                minVolume *= calibrationSettings.minVolumeMargin;
            
            // 静的プロパティを更新
            MinVolume = minVolume;
            CenterVolume = (minVolume + maxVolume) / 2f;
            
            // 結果を適用
            ApplyCalibrationResults();
            
            // イベント通知
            OnCalibrationCompleted?.Invoke(minVolume, maxVolume, minPitch, maxPitch);
            
            UpdateCalibrationStatus($"Minimum volume calibrated: {minVolume:F3}");
            Debug.Log($"Individual calibration complete - Min Volume: {minVolume:F3}");
        }
        else
        {
            UpdateCalibrationStatus("Calibration failed: No samples collected");
            Debug.LogWarning("CalibrateMinVolume: No samples collected");
        }
        
        if (calibrationProgressSlider != null)
            calibrationProgressSlider.value = 0f;
        
        isIndividualCalibrating = false;
        IsIndividualCalibrating = false;
        
        // カリブレーション終了を通知
        OnCalibrationRunningStateChanged?.Invoke(false);
    }
    
    private IEnumerator CalibrateMaxVolumeCoroutine()
    {
        isIndividualCalibrating = true;
        IsIndividualCalibrating = true;
        individualCalibrationSamples.Clear();
        
        // カリブレーション開始を通知
        OnCalibrationRunningStateChanged?.Invoke(true);
        
        float duration = calibrationSettings != null ? calibrationSettings.stepDuration : 3f;
        float startTime = Time.time;
        
        UpdateCalibrationStatus($"Calibrating maximum volume...\nPlease speak loudly for {duration:F1} seconds");
        
        // イベント購読
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected += OnIndividualVolumeDetected;
        
        // サンプル収集
        while (Time.time - startTime < duration)
        {
            float progress = (Time.time - startTime) / duration;
            if (calibrationProgressSlider != null)
                calibrationProgressSlider.value = progress;
            OnCalibrationProgressUpdated?.Invoke(progress);
            yield return null;
        }
        
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= OnIndividualVolumeDetected;
        
        // 平均値を計算
        if (individualCalibrationSamples.Count > 0)
        {
            float avgVolume = CalculateAverage(individualCalibrationSamples);
            maxVolume = avgVolume;
            if (calibrationSettings != null)
                maxVolume *= calibrationSettings.maxVolumeMargin;
            
            // 静的プロパティを更新
            MaxVolume = maxVolume;
            CenterVolume = (minVolume + maxVolume) / 2f;
            
            // 結果を適用
            ApplyCalibrationResults();
            
            // イベント通知
            OnCalibrationCompleted?.Invoke(minVolume, maxVolume, minPitch, maxPitch);
            
            UpdateCalibrationStatus($"Maximum volume calibrated: {maxVolume:F3}");
            Debug.Log($"Individual calibration complete - Max Volume: {maxVolume:F3}");
        }
        else
        {
            UpdateCalibrationStatus("Calibration failed: No samples collected");
            Debug.LogWarning("CalibrateMaxVolume: No samples collected");
        }
        
        if (calibrationProgressSlider != null)
            calibrationProgressSlider.value = 0f;
        
        isIndividualCalibrating = false;
        IsIndividualCalibrating = false;
        
        // カリブレーション終了を通知
        OnCalibrationRunningStateChanged?.Invoke(false);
    }
    
    private IEnumerator CalibrateMinPitchCoroutine()
    {
        isIndividualCalibrating = true;
        IsIndividualCalibrating = true;
        individualCalibrationSamples.Clear();
        
        // カリブレーション開始を通知
        OnCalibrationRunningStateChanged?.Invoke(true);
        
        float duration = calibrationSettings != null ? calibrationSettings.stepDuration : 3f;
        float startTime = Time.time;
        
        UpdateCalibrationStatus($"Calibrating minimum pitch...\nPlease speak in a low voice for {duration:F1} seconds");
        
        // イベント購読
        if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected += OnIndividualPitchDetected;
        
        // サンプル収集
        while (Time.time - startTime < duration)
        {
            float progress = (Time.time - startTime) / duration;
            if (calibrationProgressSlider != null)
                calibrationProgressSlider.value = progress;
            OnCalibrationProgressUpdated?.Invoke(progress);
            yield return null;
        }
        
        // イベント購読解除
        if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected -= OnIndividualPitchDetected;
        
        // 平均値を計算
        if (individualCalibrationSamples.Count > 0)
        {
            float avgPitch = CalculateAverage(individualCalibrationSamples);
            minPitch = avgPitch;
            if (calibrationSettings != null)
                minPitch *= calibrationSettings.minPitchMargin;
            
            // 静的プロパティを更新
            MinPitch = minPitch;
            CenterPitch = (minPitch + maxPitch) / 2f;
            
            // 結果を適用
            ApplyCalibrationResults();
            
            // イベント通知
            OnCalibrationCompleted?.Invoke(minVolume, maxVolume, minPitch, maxPitch);
            
            UpdateCalibrationStatus($"Minimum pitch calibrated: {minPitch:F1} Hz");
            Debug.Log($"Individual calibration complete - Min Pitch: {minPitch:F1} Hz");
        }
        else
        {
            UpdateCalibrationStatus("Calibration failed: No samples collected");
            Debug.LogWarning("CalibrateMinPitch: No samples collected");
        }
        
        if (calibrationProgressSlider != null)
            calibrationProgressSlider.value = 0f;
        
        isIndividualCalibrating = false;
        IsIndividualCalibrating = false;
        
        // カリブレーション終了を通知
        OnCalibrationRunningStateChanged?.Invoke(false);
    }
    
    private IEnumerator CalibrateMaxPitchCoroutine()
    {
        isIndividualCalibrating = true;
        IsIndividualCalibrating = true;
        individualCalibrationSamples.Clear();
        
        // カリブレーション開始を通知
        OnCalibrationRunningStateChanged?.Invoke(true);
        
        float duration = calibrationSettings != null ? calibrationSettings.stepDuration : 3f;
        float startTime = Time.time;
        
        UpdateCalibrationStatus($"Calibrating maximum pitch...\nPlease speak in a high voice for {duration:F1} seconds");
        
        // イベント購読
        if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected += OnIndividualPitchDetected;
        
        // サンプル収集
        while (Time.time - startTime < duration)
        {
            float progress = (Time.time - startTime) / duration;
            if (calibrationProgressSlider != null)
                calibrationProgressSlider.value = progress;
            OnCalibrationProgressUpdated?.Invoke(progress);
            yield return null;
        }
        
        // イベント購読解除
        if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected -= OnIndividualPitchDetected;
        
        // 平均値を計算
        if (individualCalibrationSamples.Count > 0)
        {
            float avgPitch = CalculateAverage(individualCalibrationSamples);
            maxPitch = avgPitch;
            if (calibrationSettings != null)
                maxPitch *= calibrationSettings.maxPitchMargin;
            
            // 静的プロパティを更新
            MaxPitch = maxPitch;
            CenterPitch = (minPitch + maxPitch) / 2f;
            
            // 結果を適用
            ApplyCalibrationResults();
            
            // イベント通知
            OnCalibrationCompleted?.Invoke(minVolume, maxVolume, minPitch, maxPitch);
            
            UpdateCalibrationStatus($"Maximum pitch calibrated: {maxPitch:F1} Hz");
            Debug.Log($"Individual calibration complete - Max Pitch: {maxPitch:F1} Hz");
        }
        else
        {
            UpdateCalibrationStatus("Calibration failed: No samples collected");
            Debug.LogWarning("CalibrateMaxPitch: No samples collected");
        }
        
        if (calibrationProgressSlider != null)
            calibrationProgressSlider.value = 0f;
        
        isIndividualCalibrating = false;
        IsIndividualCalibrating = false;
        
        // カリブレーション終了を通知
        OnCalibrationRunningStateChanged?.Invoke(false);
    }
    
    private void OnIndividualVolumeDetected(float volume)
    {
        if (isIndividualCalibrating)
        {
            individualCalibrationSamples.Add(volume);
        }
    }
    
    private void OnIndividualPitchDetected(float pitch)
    {
        if (isIndividualCalibrating && pitch > 0f)
        {
            individualCalibrationSamples.Add(pitch);
        }
    }
    
    /// <summary>
    /// 設定ボタンのイベントを設定
    /// </summary>
    void SetupSettingsButtons()
    {
        Debug.Log($"VoiceCalibrator: SetupSettingsButtons()を呼び出しました。");
        
        if (settingsButtons == null)
        {
            Debug.LogWarning("VoiceCalibrator: settingsButtonsが設定されていません。");
            return;
        }
        
        if (settingsObjects == null)
        {
            Debug.LogWarning("VoiceCalibrator: settingsObjectsが設定されていません。");
            return;
        }
        
        Debug.Log($"VoiceCalibrator: settingsButtons.Length={settingsButtons.Length}, settingsObjects.Length={settingsObjects.Length}");
        
        // ボタンとオブジェクトの数が一致しているか確認
        if (settingsButtons.Length != settingsObjects.Length)
        {
            Debug.LogError($"VoiceCalibrator: 設定ボタン({settingsButtons.Length}個)と設定オブジェクト({settingsObjects.Length}個)の数が一致していません。\n" +
                          $"インスペクターで Settings Objects 配列に {settingsButtons.Length} 個のオブジェクトを設定してください。");
        }
        
        // 各ボタンにイベントを設定
        for (int i = 0; i < settingsButtons.Length; i++)
        {
            if (settingsButtons[i] == null)
            {
                Debug.LogWarning($"VoiceCalibrator: 設定ボタン[{i}]がnullです。");
                continue;
            }
            
            Debug.Log($"VoiceCalibrator: 設定ボタン[{i}]にイベントを設定します。ボタン名: {settingsButtons[i].gameObject.name}");
            
            // settingsObjectがnullでもボタンにはイベントを設定する（最初の遷移処理のため）
            int index = i; // クロージャー用にローカル変数にコピー
            settingsButtons[i].onClick.RemoveAllListeners();
            settingsButtons[i].onClick.AddListener(() => OnSettingsButtonClicked(index));
            
            // イベントリスナーの数を確認
            int listenerCount = settingsButtons[i].onClick.GetPersistentEventCount();
            Debug.Log($"VoiceCalibrator: 設定ボタン[{i}]のイベントリスナー数: {listenerCount}");
            
            if (i >= settingsObjects.Length)
            {
                Debug.LogError($"VoiceCalibrator: 設定オブジェクト[{i}]が設定されていません。\n" +
                              $"インスペクターで Settings Objects 配列の要素[{i}]にオブジェクトを設定してください。\n" +
                              $"現在の配列の長さ: {settingsObjects.Length}, 必要な長さ: {settingsButtons.Length}");
            }
            else if (settingsObjects[i] == null)
            {
                Debug.LogWarning($"VoiceCalibrator: 設定オブジェクト[{i}]がnullです。最初の遷移のみ機能します。");
            }
            else
            {
                Debug.Log($"VoiceCalibrator: 設定オブジェクト[{i}] = {settingsObjects[i].name}");
            }
        }
        
        Debug.Log($"VoiceCalibrator: SetupSettingsButtons()完了。{settingsButtons.Length}個のボタンにイベントを設定しました。");
    }
    
    /// <summary>
    /// 設定ボタンがクリックされた時の処理
    /// 最初の遷移：GameModeSelectionPanelを表示し、SettingsObjectsを非表示にする
    /// その後：SettingsPanelをトグル
    /// </summary>
    /// <param name="index">ボタンのインデックス</param>
    void OnSettingsButtonClicked(int index)
    {
        Debug.Log($"VoiceCalibrator: 設定ボタン{index}がクリックされました。hasShownGameSelection={hasShownGameSelection}");
        
        // 最初の遷移の場合：カリブレーションデータがない場合のみGameModeSelectionPanelを表示
        if (!hasShownGameSelection)
        {
            Debug.Log($"VoiceCalibrator: 最初の遷移処理を実行します（ボタン{index}）");
            
            // カリブレーションデータがある場合は、GameModeSelectionPanelに遷移しない
            if (CalibrationSaveSystem.HasCalibrationData())
            {
                Debug.Log("VoiceCalibrator: カリブレーションデータが存在するため、GameModeSelectionPanelに遷移しません");
                hasShownGameSelection = true; // フラグを立てて、次回からはトグル処理に移行
                // 設定画面を表示する処理に移行（下のトグル処理を実行）
            }
            else
            {
                // カリブレーションデータがない場合のみ、GameModeSelectionPanelに遷移
            // SettingsObjectsを非表示にする
            if (settingsObjects != null)
            {
                foreach (GameObject settingsObject in settingsObjects)
                {
                    if (settingsObject != null)
                    {
                        settingsObject.SetActive(false);
                        
                        // SettingsPanelコンポーネントがある場合はHide()メソッドも呼ぶ
                        SettingsPanel settingsPanel = settingsObject.GetComponent<SettingsPanel>();
                        if (settingsPanel != null)
                        {
                            settingsPanel.Hide();
                        }
                    }
                }
            }
            
            // GameModeSelectionPanelを表示
            if (gameModeSelectionPanel != null)
            {
                gameModeSelectionPanel.Show();
                hasShownGameSelection = true;
                Debug.Log($"VoiceCalibrator: 設定ボタン{index}がクリックされました。ゲームセレクト画面を表示し、設定オブジェクトを非表示にしました。");
            }
            else
            {
                Debug.LogWarning("VoiceCalibrator: GameModeSelectionPanelが設定されていません");
            }
            return;
            }
        }
        
        // その後：SettingsPanelをトグル
        Debug.Log($"VoiceCalibrator: トグル処理を実行します（ボタン{index}）");
        
        if (settingsObjects == null)
        {
            Debug.LogWarning("VoiceCalibrator: settingsObjectsが設定されていません");
            return;
        }
        
        Debug.Log($"VoiceCalibrator: settingsObjects.Length={settingsObjects.Length}, index={index}");
        
        if (index < 0 || index >= settingsObjects.Length)
        {
            Debug.LogError($"VoiceCalibrator: 【エラー】設定ボタン{index}に対応する設定オブジェクトが設定されていません。\n" +
                          $"現在の状況:\n" +
                          $"  - Settings Buttons配列の長さ: {settingsButtons.Length}\n" +
                          $"  - Settings Objects配列の長さ: {settingsObjects.Length}\n" +
                          $"  - クリックされたボタンのインデックス: {index}\n" +
                          $"解決方法:\n" +
                          $"  1. Unityエディタで VoiceCalibrator コンポーネントを選択\n" +
                          $"  2. Settings Objects 配列のサイズを {settingsButtons.Length} に変更\n" +
                          $"  3. Element {index} に2つ目の設定画面オブジェクトを設定\n" +
                          $"現在設定されているオブジェクト:");
            for (int i = 0; i < settingsObjects.Length; i++)
            {
                Debug.Log($"  settingsObjects[{i}] = {(settingsObjects[i] != null ? settingsObjects[i].name : "null")}");
            }
            
            // ユーザーに分かりやすくするため、どのボタンがクリックされたかも表示
            if (settingsButtons != null && index < settingsButtons.Length && settingsButtons[index] != null)
            {
                Debug.LogError($"クリックされたボタン: {settingsButtons[index].gameObject.name}");
            }
            
            return;
        }
        
        GameObject targetObject = settingsObjects[index];
        if (targetObject == null)
        {
            Debug.LogWarning($"VoiceCalibrator: インデックス{index}の設定オブジェクトが設定されていません。");
            // 配列の内容を確認
            for (int i = 0; i < settingsObjects.Length; i++)
            {
                Debug.Log($"VoiceCalibrator: settingsObjects[{i}] = {(settingsObjects[i] != null ? settingsObjects[i].name : "null")}");
            }
            return;
        }
        
        Debug.Log($"VoiceCalibrator: ターゲットオブジェクト = {targetObject.name}, 現在の状態 = {(targetObject.activeSelf ? "表示" : "非表示")}");
        
        // 現在の表示状態を確認してトグル
        bool isCurrentlyActive = targetObject.activeSelf;
        
        if (isCurrentlyActive)
        {
            // 表示されている場合は非表示にする
            Debug.Log($"VoiceCalibrator: 設定オブジェクトを非表示にします。");
            targetObject.SetActive(false);
            
            // SettingsPanelコンポーネントがある場合はHide()メソッドを呼ぶ
            SettingsPanel settingsPanel = targetObject.GetComponent<SettingsPanel>();
            if (settingsPanel != null)
            {
                settingsPanel.Hide();
            }
            
            Debug.Log($"VoiceCalibrator: 設定ボタン{index}がクリックされました。設定オブジェクトを非表示にしました。");
        }
        else
        {
            // 非表示の場合は表示する
            Debug.Log($"VoiceCalibrator: 設定オブジェクトを表示します。");
            targetObject.SetActive(true);
            
            // SettingsPanelコンポーネントがある場合はShow()メソッドを呼ぶ
            SettingsPanel settingsPanel = targetObject.GetComponent<SettingsPanel>();
            if (settingsPanel != null)
            {
                Debug.Log($"VoiceCalibrator: SettingsPanel.Show()を呼び出します。");
                settingsPanel.Show();
            }
            else
            {
                Debug.Log($"VoiceCalibrator: SettingsPanelコンポーネントが見つかりませんでした。SetActive(true)のみ実行しました。");
            }
            
            Debug.Log($"VoiceCalibrator: 設定ボタン{index}がクリックされました。設定オブジェクトを表示しました。現在の状態 = {targetObject.activeSelf}");
        }
    }
    
    /// <summary>
    /// Toggle voice detection On/Off
    /// </summary>
    void ToggleVoiceDetection()
    {
        if (voiceDetector == null)
        {
            Debug.LogWarning("VoiceCalibrator: VoiceDetector not found");
            return;
        }
        
        if (voiceDetector.IsDetectionEnabled)
        {
            voiceDetector.DisableDetection();
            // Stop drawing when detection is disabled
            StopDrawingOnDetectionDisabled();
        }
        else
        {
            voiceDetector.EnableDetection();
        }
        
        UpdateVoiceDetectionButton();
    }
    
    /// <summary>
    /// Stop drawing when voice detection is disabled
    /// </summary>
    void StopDrawingOnDetectionDisabled()
    {
        // Reset VoiceInputHandler's latest values to stop drawing
        VoiceInputHandler voiceInputHandler = FindObjectOfType<VoiceInputHandler>();
        if (voiceInputHandler != null)
        {
            voiceInputHandler.ResetVoiceInput();
        }
        
        // Reset PaintBattleGameManager's last position
        PaintBattleGameManager paintBattleGameManager = FindObjectOfType<PaintBattleGameManager>();
        if (paintBattleGameManager != null)
        {
            paintBattleGameManager.ResetLastPosition();
        }
    }
    
    /// <summary>
    /// Update voice detection button state
    /// </summary>
    void UpdateVoiceDetectionButton()
    {
        if (voiceDetector == null)
        {
            return;
        }
        
        bool isEnabled = voiceDetector.IsDetectionEnabled;
        
        // Update button text
        if (voiceDetectionToggleButton != null)
        {
            TextMeshProUGUI buttonText = voiceDetectionToggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isEnabled ? "Voice Detection: ON" : "Voice Detection: OFF";
            }
        }
        
        // Update status text
        if (voiceDetectionStatusText != null)
        {
            voiceDetectionStatusText.text = isEnabled ? "Voice Detection: Enabled" : "Voice Detection: Disabled";
        }
    }
}
