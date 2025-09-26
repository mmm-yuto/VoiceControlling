using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class VoiceCalibrator : MonoBehaviour
{
    [Header("Calibration Settings")]
    public float calibrationDuration = 3f; // キャリブレーション時間（秒）
    public float volumeMultiplier = 1.5f;  // 音量の倍率
    public float pitchMultiplier = 1.2f;   // ピッチの倍率
    
    [Header("UI References")]
    public TextMeshProUGUI calibrationStatusText;
    public Slider calibrationProgressSlider;
    public Button startCalibrationButton;
    
    [Header("Target Components")]
    public VoiceDisplay voiceDisplay;
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    private ImprovedPitchAnalyzer improvedPitchAnalyzer;
    private FixedPitchAnalyzer fixedPitchAnalyzer;
    private bool isCalibrating = false;
    private float calibrationStartTime;
    private List<float> volumeSamples = new List<float>();
    private List<float> pitchSamples = new List<float>();
    
    void Start()
    {
        // 音声分析コンポーネントを取得
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        fixedPitchAnalyzer = FindObjectOfType<FixedPitchAnalyzer>();
        
        // ボタンのイベント設定
        if (startCalibrationButton != null)
        {
            startCalibrationButton.onClick.AddListener(StartCalibration);
        }
        
        // 初期状態の設定
        UpdateCalibrationStatus("Hold SPACE key and speak to start calibration");
        
        // 使用するピッチ分析器をログ出力
        if (fixedPitchAnalyzer != null)
        {
            Debug.Log("VoiceCalibrator: Using FixedPitchAnalyzer");
        }
        else if (improvedPitchAnalyzer != null)
        {
            Debug.Log("VoiceCalibrator: Using ImprovedPitchAnalyzer");
        }
        else if (pitchAnalyzer != null)
        {
            Debug.Log("VoiceCalibrator: Using PitchAnalyzer");
        }
        else
        {
            Debug.LogWarning("VoiceCalibrator: No pitch analyzer found!");
        }
    }
    
    void Update()
    {
        // スペースキーが押されている間、キャリブレーションを実行
        if (Input.GetKey(KeyCode.Space) && !isCalibrating)
        {
            StartCalibration();
        }
        
        // キャリブレーション中の処理
        if (isCalibrating)
        {
            UpdateCalibration();
        }
    }
    
    void StartCalibration()
    {
        if (isCalibrating) return;
        
        isCalibrating = true;
        calibrationStartTime = Time.time;
        volumeSamples.Clear();
        pitchSamples.Clear();
        
        // イベント購読
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected += OnVolumeDetected;
        
        // FixedPitchAnalyzerを最優先で使用
        if (fixedPitchAnalyzer != null)
            fixedPitchAnalyzer.OnPitchDetected += OnPitchDetected;
        else if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected += OnPitchDetected;
        else if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected += OnPitchDetected;
        
        UpdateCalibrationStatus("Calibrating... Please keep speaking");
        
        Debug.Log("Calibration started");
    }
    
    void UpdateCalibration()
    {
        float elapsedTime = Time.time - calibrationStartTime;
        float progress = elapsedTime / calibrationDuration;
        
        // プログレスバーの更新
        if (calibrationProgressSlider != null)
        {
            calibrationProgressSlider.value = progress;
        }
        
        // キャリブレーション完了チェック
        if (elapsedTime >= calibrationDuration)
        {
            CompleteCalibration();
        }
        else
        {
            UpdateCalibrationStatus($"Calibrating... {elapsedTime:F1}s / {calibrationDuration:F1}s");
        }
    }
    
    void OnVolumeDetected(float volume)
    {
        if (isCalibrating)
        {
            volumeSamples.Add(volume);
        }
    }
    
    void OnPitchDetected(float pitch)
    {
        if (isCalibrating)
        {
            pitchSamples.Add(pitch);
        }
    }
    
    void CompleteCalibration()
    {
        isCalibrating = false;
        
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= OnVolumeDetected;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected -= OnPitchDetected;
        
        // 平均値を計算
        float averageVolume = CalculateAverage(volumeSamples);
        float averagePitch = CalculateAverage(pitchSamples);
        
        // 最大値を設定
        float newMaxVolume = averageVolume * volumeMultiplier;
        float newMaxPitch = averagePitch * pitchMultiplier;
        
        // 最小値を設定（平均値の半分）
        float newMinPitch = averagePitch * 0.5f;
        
        // VoiceDisplayの設定を更新
        if (voiceDisplay != null)
        {
            voiceDisplay.SetPitchRange(newMinPitch, newMaxPitch);
            voiceDisplay.SetMaxVolume(newMaxVolume);
        }
        
        // 結果を表示
        string result = $"Calibration Complete!\n" +
                       $"Average Volume: {averageVolume:F3}\n" +
                       $"Average Pitch: {averagePitch:F1} Hz\n" +
                       $"Max Volume: {newMaxVolume:F3}\n" +
                       $"Pitch Range: {newMinPitch:F1} - {newMaxPitch:F1} Hz";
        
        UpdateCalibrationStatus(result);
        
        Debug.Log($"Calibration complete - Volume: {averageVolume:F3}, Pitch: {averagePitch:F1} Hz");
        Debug.Log($"Settings - Max Volume: {newMaxVolume:F3}, Pitch Range: {newMinPitch:F1} - {newMaxPitch:F1} Hz");
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
    
    void UpdateCalibrationStatus(string message)
    {
        if (calibrationStatusText != null)
        {
            calibrationStatusText.text = message;
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= OnVolumeDetected;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected -= OnPitchDetected;
        if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected -= OnPitchDetected;
        if (fixedPitchAnalyzer != null)
            fixedPitchAnalyzer.OnPitchDetected -= OnPitchDetected;
    }
}
