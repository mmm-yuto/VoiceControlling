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
    
    [Header("Target Components")]
    public VoiceDisplay voiceDisplay;
    public CalibrationPanel calibrationPanel;
    
    private VolumeAnalyzer volumeAnalyzer;
    private ImprovedPitchAnalyzer improvedPitchAnalyzer;
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
    
    void Start()
    {
        // 音声分析コンポーネントを取得
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        
        // ボタンのイベント設定
        if (startCalibrationButton != null)
        {
            startCalibrationButton.onClick.AddListener(StartCalibration);
        }
        
        // 初期状態の設定
        UpdateCalibrationStatus("Please start calibration");
        
        // デフォルト設定の確認
        if (calibrationSettings == null)
        {
            Debug.LogWarning("VoiceCalibrator: CalibrationSettingsが設定されていません。デフォルト値を使用します。");
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
        
        UpdateCalibrationStatus($"Calibrating minimum volume... Please remain silent for {duration:F1} seconds");
        
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
        
        UpdateCalibrationStatus($"Calibrating maximum volume... Please speak loudly for {duration:F1} seconds");
        
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
        
        UpdateCalibrationStatus($"Calibrating minimum pitch... Please speak in a low voice for {duration:F1} seconds");
        
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
        
        UpdateCalibrationStatus($"Calibrating maximum pitch... Please speak in a high voice for {duration:F1} seconds");
        
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
}
