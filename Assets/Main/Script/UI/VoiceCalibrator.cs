using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音量・ピッチの平均値を計測し、各システムに共有するキャリブレーションコントローラー
/// </summary>
public class VoiceCalibrator : MonoBehaviour
{
    public static float LastAverageVolume { get; private set; } = 0f;
    public static float LastAveragePitch { get; private set; } = 0f;
    public static event System.Action<float, float> OnCalibrationAveragesUpdated;

    [Header("Settings")]
    [SerializeField] private CalibrationSettings settings;

    [Header("References")]
    [SerializeField] private VolumeAnalyzer volumeAnalyzer;
    [SerializeField] private ImprovedPitchAnalyzer improvedPitchAnalyzer;
    [SerializeField] private VoiceDisplay voiceDisplay;

    public event System.Action<string> OnStatusChanged;
    public event System.Action<float> OnProgressChanged;
    public event System.Action<bool> OnCalibrationRunningChanged;

    private readonly List<float> volumeSamples = new List<float>();
    private readonly List<float> pitchSamples = new List<float>();
    private readonly List<float> noiseSamples = new List<float>();

    private bool isCalibrating;
    private float calibrationStartTime;
    private Coroutine calibrationRoutine;

    private CalibrationSettings RuntimeSettings
    {
        get
        {
            if (settings != null)
            {
                return settings;
            }

            // ScriptableObject が割り当てられていない場合でも動作するようデフォルト値を生成
            if (_runtimeSettings == null)
            {
                _runtimeSettings = ScriptableObject.CreateInstance<CalibrationSettings>();
            }
            return _runtimeSettings;
        }
    }
    private CalibrationSettings _runtimeSettings;

    void Awake()
    {
        ResolveDependencies();
    }

    void OnEnable()
    {
        BroadcastStatus("Ready to calibrate");
        BroadcastProgress(0f);
    }

    void OnDisable()
    {
        StopCalibrationInternal();
    }

    /// <summary>
    /// ボタンなどから呼び出し、キャリブレーションを開始する
    /// </summary>
    public void StartCalibration()
    {
        if (isCalibrating)
        {
            return;
        }

        if (!EnsureAnalyzers())
        {
            BroadcastStatus("Volume/Pitch analyzer not found.");
            return;
        }

        calibrationRoutine = StartCoroutine(CalibrationFlow());
    }

    /// <summary>
    /// キャリブレーションを中断する
    /// </summary>
    public void CancelCalibration()
    {
        if (!isCalibrating)
        {
            return;
        }

        BroadcastStatus("Calibration cancelled.");
        StopCalibrationInternal();
        BroadcastProgress(0f);
    }

    void ResolveDependencies()
    {
        if (volumeAnalyzer == null)
        {
            volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        }
        if (improvedPitchAnalyzer == null)
        {
            improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        }
        if (voiceDisplay == null)
        {
            voiceDisplay = FindObjectOfType<VoiceDisplay>();
        }
    }

    bool EnsureAnalyzers()
    {
        ResolveDependencies();
        return volumeAnalyzer != null && improvedPitchAnalyzer != null;
    }

    IEnumerator CalibrationFlow()
    {
        isCalibrating = true;
        calibrationStartTime = Time.time;
        volumeSamples.Clear();
        pitchSamples.Clear();
        noiseSamples.Clear();

        BroadcastStatus("Calibrating... Please speak steadily");
        BroadcastProgress(0f);
        BroadcastRunningState(true);

        volumeAnalyzer.OnVolumeDetected += HandleVolumeSample;
        improvedPitchAnalyzer.OnPitchDetected += HandlePitchSample;

        float duration = Mathf.Max(0.5f, RuntimeSettings.calibrationDuration);

        while (Time.time - calibrationStartTime < duration)
        {
            float elapsed = Time.time - calibrationStartTime;
            BroadcastProgress(Mathf.Clamp01(elapsed / duration));
            BroadcastStatus($"Calibrating... {elapsed:F1}/{duration:F1}s");
            yield return null;
        }

        CompleteCalibration();
    }

    void HandleVolumeSample(float volume)
    {
        if (!isCalibrating)
        {
            return;
        }

        volumeSamples.Add(volume);
        float noiseWindow = Mathf.Max(0f, RuntimeSettings.noiseSampleDuration);
        if (Time.time - calibrationStartTime <= noiseWindow)
        {
            noiseSamples.Add(volume);
        }
    }

    void HandlePitchSample(float pitch)
    {
        if (!isCalibrating)
        {
            return;
        }

        if (pitch > 0f)
        {
            pitchSamples.Add(pitch);
        }
    }

    void CompleteCalibration()
    {
        StopCalibrationInternal();

        float averageVolume = CalculateAverage(volumeSamples);
        float averagePitch = CalculateAverage(pitchSamples);

        float newMaxVolume = averageVolume * RuntimeSettings.volumeMultiplier;
        float newMaxPitch = averagePitch * RuntimeSettings.pitchMultiplier;
        float newMinPitch = averagePitch * RuntimeSettings.minPitchFactor;

        if (averagePitch <= 0f || newMaxPitch <= newMinPitch)
        {
            float fallbackMin = improvedPitchAnalyzer != null ? improvedPitchAnalyzer.minFrequency : 80f;
            float fallbackMax = improvedPitchAnalyzer != null ? improvedPitchAnalyzer.maxFrequency : 1000f;
            newMinPitch = fallbackMin;
            newMaxPitch = fallbackMax;
        }

        if (voiceDisplay != null)
        {
            voiceDisplay.SetPitchRange(newMinPitch, newMaxPitch);
            voiceDisplay.SetMaxVolume(newMaxVolume);
        }

        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.minFrequency = newMinPitch;
            improvedPitchAnalyzer.maxFrequency = newMaxPitch;
        }

        float noiseAverage = CalculateAverage(noiseSamples);
        float dynamicThreshold = Mathf.Clamp(
            noiseAverage * RuntimeSettings.dynamicThresholdMultiplier,
            RuntimeSettings.minDynamicThreshold,
            RuntimeSettings.maxDynamicThreshold
        );

        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.volumeThreshold = dynamicThreshold;
            improvedPitchAnalyzer.InitializeUIComponents();
        }

        LastAverageVolume = averageVolume;
        LastAveragePitch = averagePitch;
        OnCalibrationAveragesUpdated?.Invoke(LastAverageVolume, LastAveragePitch);
        VoiceToScreenMapper mapper = FindObjectOfType<VoiceToScreenMapper>();
        mapper?.SyncRanges();

        BroadcastProgress(1f);
        BroadcastStatus(
            $"Calibration Complete!\n" +
            $"Avg Volume: {averageVolume:F3}\n" +
            $"Avg Pitch: {averagePitch:F1} Hz\n" +
            $"Max Volume: {newMaxVolume:F3}\n" +
            $"Pitch Range: {newMinPitch:F1} - {newMaxPitch:F1} Hz\n" +
            $"Silence Threshold: {dynamicThreshold:F3}"
        );
    }

    void StopCalibrationInternal()
    {
        if (!isCalibrating)
        {
            return;
        }

        isCalibrating = false;

        if (calibrationRoutine != null)
        {
            StopCoroutine(calibrationRoutine);
            calibrationRoutine = null;
        }

        if (volumeAnalyzer != null)
        {
            volumeAnalyzer.OnVolumeDetected -= HandleVolumeSample;
        }

        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.OnPitchDetected -= HandlePitchSample;
        }

        BroadcastRunningState(false);
    }

    static float CalculateAverage(List<float> samples)
    {
        if (samples.Count == 0)
        {
            return 0f;
        }

        float sum = 0f;
        for (int i = 0; i < samples.Count; i++)
        {
            sum += samples[i];
        }

        return sum / samples.Count;
    }

    void BroadcastStatus(string message) => OnStatusChanged?.Invoke(message);
    void BroadcastProgress(float progress) => OnProgressChanged?.Invoke(progress);
    void BroadcastRunningState(bool isRunning) => OnCalibrationRunningChanged?.Invoke(isRunning);
}
