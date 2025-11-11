using UnityEngine;
using System.Collections.Generic;

public class DebugPitchAnalyzer : MonoBehaviour
{
    [Header("Pitch Settings")]
    public float pitchSensitivity = 1.0f;
    public float minFrequency = 80f;
    public float maxFrequency = 1000f;
    public float volumeThreshold = 0.01f;
    public float smoothingFactor = 0.1f;
    
    [Header("Debug Settings")]
    public bool enableDetailedLogging = true;
    public bool showVolumeInfo = true;
    public bool showPitchInfo = true;
    
    [Header("Movement Pitch Settings")]
    public float leftPitch = 200f;
    public float centerPitch = 400f;
    public float rightPitch = 600f;
    
    [Header("Test Settings")]
    public bool useTestMode = false;  // デフォルトでテストモードを無効化
    public float testPitch = 400f;
    
    private VoiceDetector voiceDetector;
    private VolumeAnalyzer volumeAnalyzer;
    public float lastDetectedPitch = 0f;
    private float smoothedPitch = 0f;
    private bool hasValidPitch = false;
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        
        if (voiceDetector == null)
        {
            Debug.LogError("VoiceDetector not found!");
        }
        else
        {
            Debug.Log($"VoiceDetector found: {voiceDetector.name}");
        }
        
        if (volumeAnalyzer == null)
        {
            Debug.LogError("VolumeAnalyzer not found!");
        }
        else
        {
            Debug.Log($"VolumeAnalyzer found: {volumeAnalyzer.name}");
        }
    }
    
    void Update()
    {
        if (useTestMode)
        {
            // テストモード：固定の音程を使用
            ProcessPitch(testPitch);
        }
        else
        {
            // 通常モード：実際の音声を解析
            if (voiceDetector == null)
            {
                Debug.LogError("VoiceDetector is null!");
                return;
            }
            
            float[] samples = voiceDetector.GetAudioSamples();
            if (samples == null)
            {
                if (enableDetailedLogging)
                {
                    Debug.Log("Audio samples are null");
                }
                return;
            }
            
            // 音量をチェック
            float currentVolume = CalculateVolume(samples);
            
            if (showVolumeInfo && enableDetailedLogging)
            {
                Debug.Log($"Current Volume: {currentVolume:F6}, Threshold: {volumeThreshold:F6}");
            }
            
            if (currentVolume > volumeThreshold)
            {
                // 音量が閾値以上の場合のみピッチを検知
                float pitch = CalculatePitchSimple(samples);
                
                if (showPitchInfo && enableDetailedLogging)
                {
                    Debug.Log($"Raw Pitch: {pitch:F1} Hz");
                }
                
                ProcessPitch(pitch);
            }
            else
            {
                // 音量が低い場合はピッチ検知を停止
                if (enableDetailedLogging)
                {
                    Debug.Log($"Volume too low: {currentVolume:F6} <= {volumeThreshold:F6}");
                }
                ProcessPitch(0f);
            }
        }
    }
    
    float CalculateVolume(float[] samples)
    {
        if (samples == null || samples.Length == 0)
        {
            return 0f;
        }
        
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / samples.Length);
    }
    
    float CalculatePitchSimple(float[] samples)
    {
        if (samples == null || samples.Length == 0)
        {
            return 0f;
        }
        
        // 簡易的なゼロクロッシング法
        int zeroCrossings = CountZeroCrossings(samples);
        float frequency = (zeroCrossings * voiceDetector.sampleRate) / (2f * samples.Length);
        
        if (enableDetailedLogging)
        {
            Debug.Log($"Zero Crossings: {zeroCrossings}, Sample Rate: {voiceDetector.sampleRate}, Buffer Size: {samples.Length}");
            Debug.Log($"Calculated Frequency: {frequency:F1} Hz");
        }
        
        // 範囲内の周波数のみ返す
        if (frequency >= minFrequency && frequency <= maxFrequency)
        {
            return frequency;
        }
        
        if (enableDetailedLogging)
        {
            Debug.Log($"Frequency out of range: {frequency:F1} Hz (Range: {minFrequency}-{maxFrequency} Hz)");
        }
        
        return 0f;
    }
    
    int CountZeroCrossings(float[] samples)
    {
        if (samples == null || samples.Length < 2)
        {
            return 0;
        }
        
        int crossings = 0;
        for (int i = 1; i < samples.Length; i++)
        {
            if ((samples[i] >= 0) != (samples[i - 1] >= 0))
            {
                crossings++;
            }
        }
        return crossings;
    }
    
    void ProcessPitch(float pitch)
    {
        if (pitch > 0)
        {
            // ピッチのスムージング処理
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
            
            // イベント発火
            OnPitchDetected?.Invoke(smoothedPitch);
        }
        else
        {
            // ピッチが検知されない場合は、スムージングされた値を徐々にリセット
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
            
            if (enableDetailedLogging)
            {
                Debug.Log("Pitch: No pitch detected");
            }
        }
    }
    
    // インスペクター用のテストボタン
    [ContextMenu("Test Left Movement")]
    void TestLeftMovement()
    {
        ProcessPitch(leftPitch);
    }
    
    [ContextMenu("Test Center Movement")]
    void TestCenterMovement()
    {
        ProcessPitch(centerPitch);
    }
    
    [ContextMenu("Test Right Movement")]
    void TestRightMovement()
    {
        ProcessPitch(rightPitch);
    }
    
    [ContextMenu("Enable Test Mode")]
    void EnableTestMode()
    {
        useTestMode = true;
        Debug.Log("Test mode enabled");
    }
    
    [ContextMenu("Disable Test Mode")]
    void DisableTestMode()
    {
        useTestMode = false;
        Debug.Log("Test mode disabled");
    }
    
    [ContextMenu("Lower Volume Threshold")]
    void LowerVolumeThreshold()
    {
        volumeThreshold *= 0.5f;
        Debug.Log($"Volume threshold lowered to: {volumeThreshold:F6}");
    }
    
    [ContextMenu("Raise Volume Threshold")]
    void RaiseVolumeThreshold()
    {
        volumeThreshold *= 2f;
        Debug.Log($"Volume threshold raised to: {volumeThreshold:F6}");
    }
    
    public System.Action<float> OnPitchDetected;
}
