using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class ExternalPitchAnalyzer : MonoBehaviour
{
    [Header("Pitch Settings")]
    public float pitchSensitivity = 1.0f;
    public float minFrequency = 80f;
    public float maxFrequency = 1000f;
    public float volumeThreshold = 0.01f;
    public float smoothingFactor = 0.1f;
    
    [Header("External Library Settings")]
    public bool useExternalLibrary = false; // 外部ライブラリを使用するか
    public string libraryPath = "pitch_analyzer.dll"; // 外部ライブラリのパス
    
    [Header("Movement Pitch Settings")]
    public float leftPitch = 200f;
    public float centerPitch = 400f;
    public float rightPitch = 600f;
    
    [Header("Test Settings")]
    public bool useTestMode = true;
    public float testPitch = 400f;
    
    private VoiceDetector voiceDetector;
    private VolumeAnalyzer volumeAnalyzer;
    public float lastDetectedPitch = 0f;
    private float smoothedPitch = 0f;
    private bool hasValidPitch = false;
    
    // 外部ライブラリ用のDLLインポート
    [DllImport("pitch_analyzer", CallingConvention = CallingConvention.Cdecl)]
    private static extern float AnalyzePitch(float[] samples, int length, int sampleRate);
    
    [DllImport("pitch_analyzer", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool InitializePitchAnalyzer(int sampleRate, float minFreq, float maxFreq);
    
    [DllImport("pitch_analyzer", CallingConvention = CallingConvention.Cdecl)]
    private static extern void CleanupPitchAnalyzer();
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        
        if (useExternalLibrary)
        {
            InitializeExternalLibrary();
        }
    }
    
    void InitializeExternalLibrary()
    {
        try
        {
            bool success = InitializePitchAnalyzer(voiceDetector.sampleRate, minFrequency, maxFrequency);
            if (success)
            {
                Debug.Log("External pitch analyzer initialized successfully");
            }
            else
            {
                Debug.LogError("Failed to initialize external pitch analyzer");
                useExternalLibrary = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"External library error: {e.Message}");
            useExternalLibrary = false;
        }
    }
    
    void Update()
    {
        if (useTestMode)
        {
            ProcessPitch(testPitch);
        }
        else
        {
            float[] samples = voiceDetector.GetAudioSamples();
            if (samples != null)
            {
                float currentVolume = CalculateVolume(samples);
                
                if (currentVolume > volumeThreshold)
                {
                    float pitch = CalculatePitch(samples);
                    ProcessPitch(pitch);
                }
                else
                {
                    ProcessPitch(0f);
                }
            }
        }
    }
    
    float CalculateVolume(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / samples.Length);
    }
    
    float CalculatePitch(float[] samples)
    {
        if (useExternalLibrary)
        {
            return CalculatePitchExternal(samples);
        }
        else
        {
            return CalculatePitchInternal(samples);
        }
    }
    
    float CalculatePitchExternal(float[] samples)
    {
        try
        {
            float pitch = AnalyzePitch(samples, samples.Length, voiceDetector.sampleRate);
            Debug.Log($"External pitch: {pitch:F1} Hz");
            return pitch;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"External pitch analysis error: {e.Message}");
            return CalculatePitchInternal(samples);
        }
    }
    
    float CalculatePitchInternal(float[] samples)
    {
        // 内部実装（ImprovedPitchAnalyzerと同じロジック）
        float[] normalizedSamples = NormalizeSamples(samples);
        float[] filteredSamples = ApplyBandpassFilter(normalizedSamples);
        
        float autocorrelationPitch = CalculatePitchAutocorrelation(filteredSamples);
        float harmonicPitch = CalculatePitchHarmonic(filteredSamples);
        
        return CombinePitchResults(autocorrelationPitch, harmonicPitch);
    }
    
    float[] NormalizeSamples(float[] samples)
    {
        float maxAmplitude = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            maxAmplitude = Mathf.Max(maxAmplitude, Mathf.Abs(samples[i]));
        }
        
        if (maxAmplitude == 0f) return samples;
        
        float[] normalized = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            normalized[i] = samples[i] / maxAmplitude;
        }
        
        return normalized;
    }
    
    float[] ApplyBandpassFilter(float[] samples)
    {
        float[] filtered = new float[samples.Length];
        
        float alpha = 0.1f;
        filtered[0] = samples[0];
        for (int i = 1; i < samples.Length; i++)
        {
            filtered[i] = alpha * samples[i] + (1f - alpha) * filtered[i - 1];
        }
        
        float[] highPassFiltered = new float[samples.Length];
        float beta = 0.9f;
        highPassFiltered[0] = filtered[0];
        for (int i = 1; i < filtered.Length; i++)
        {
            highPassFiltered[i] = beta * (highPassFiltered[i - 1] + filtered[i] - filtered[i - 1]);
        }
        
        return highPassFiltered;
    }
    
    float CalculatePitchAutocorrelation(float[] samples)
    {
        // 検索範囲を広げる（カリブレーション範囲に関係なく、実際のピッチを検出可能にする）
        const float searchMinFrequency = 50f;  // 検索範囲の最小周波数
        const float searchMaxFrequency = 2000f; // 検索範囲の最大周波数
        int minPeriod = Mathf.RoundToInt(voiceDetector.sampleRate / searchMaxFrequency);
        int maxPeriod = Mathf.RoundToInt(voiceDetector.sampleRate / searchMinFrequency);
        
        float maxCorrelation = 0f;
        int bestPeriod = 0;
        
        for (int period = minPeriod; period <= maxPeriod && period < samples.Length / 2; period++)
        {
            float correlation = 0f;
            int count = 0;
            
            for (int i = 0; i < samples.Length - period; i++)
            {
                correlation += samples[i] * samples[i + period];
                count++;
            }
            
            if (count > 0)
            {
                correlation /= count;
                
                if (correlation > maxCorrelation)
                {
                    maxCorrelation = correlation;
                    bestPeriod = period;
                }
            }
        }
        
        if (bestPeriod > 0 && maxCorrelation > 0.3f)
        {
            return (float)voiceDetector.sampleRate / bestPeriod;
        }
        
        return 0f;
    }
    
    float CalculatePitchHarmonic(float[] samples)
    {
        float[] windowedSamples = new float[samples.Length];
        
        for (int i = 0; i < samples.Length; i++)
        {
            float window = 0.54f - 0.46f * Mathf.Cos(2f * Mathf.PI * i / (samples.Length - 1));
            windowedSamples[i] = samples[i] * window;
        }
        
        float[] fft = new float[samples.Length];
        PerformSimpleFFT(windowedSamples, fft);
        
        float maxMagnitude = 0f;
        int peakIndex = 0;
        
        for (int i = 1; i < fft.Length / 2; i++)
        {
            float magnitude = Mathf.Abs(fft[i]);
            if (magnitude > maxMagnitude)
            {
                maxMagnitude = magnitude;
                peakIndex = i;
            }
        }
        
        if (maxMagnitude > 0.01f)
        {
            return (float)peakIndex * voiceDetector.sampleRate / fft.Length;
        }
        
        return 0f;
    }
    
    void PerformSimpleFFT(float[] input, float[] output)
    {
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = input[i];
        }
    }
    
    float CombinePitchResults(float autocorrelationPitch, float harmonicPitch)
    {
        if (autocorrelationPitch > 0 && harmonicPitch > 0)
        {
            float weight1 = 0.7f;
            float weight2 = 0.3f;
            float combinedPitch = (autocorrelationPitch * weight1 + harmonicPitch * weight2) / (weight1 + weight2);
            // 検出されたピッチをそのまま返す（範囲チェックは削除）
            return combinedPitch;
        }
        else if (autocorrelationPitch > 0)
        {
            return autocorrelationPitch;
        }
        else if (harmonicPitch > 0)
        {
            return harmonicPitch;
        }
        
        return 0f;
    }
    
    void ProcessPitch(float pitch)
    {
        if (pitch > 0)
        {
            if (hasValidPitch)
            {
                smoothedPitch = Mathf.Lerp(smoothedPitch, pitch, smoothingFactor);
            }
            else
            {
                smoothedPitch = pitch;
                hasValidPitch = true;
            }
            
            lastDetectedPitch = smoothedPitch;
            Debug.Log($"Pitch: {smoothedPitch:F1} Hz (Raw: {pitch:F1} Hz)");
            
            OnPitchDetected?.Invoke(smoothedPitch);
        }
        else
        {
            if (hasValidPitch)
            {
                smoothedPitch = Mathf.Lerp(smoothedPitch, 0f, smoothingFactor * 2f);
                lastDetectedPitch = smoothedPitch;
                
                if (smoothedPitch < 1f)
                {
                    hasValidPitch = false;
                    smoothedPitch = 0f;
                    lastDetectedPitch = 0f;
                }
            }
            
            Debug.Log("Pitch: No pitch detected (volume too low)");
        }
    }
    
    void OnDestroy()
    {
        if (useExternalLibrary)
        {
            try
            {
                CleanupPitchAnalyzer();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"External library cleanup error: {e.Message}");
            }
        }
    }
    
    public System.Action<float> OnPitchDetected;
}
