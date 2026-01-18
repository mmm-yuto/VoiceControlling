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
        
        if (volumeAnalyzer == null)
        {
            Debug.LogError("VolumeAnalyzer not found!");
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
                return;
            }
            
            // 音量をチェック
            float currentVolume = CalculateVolume(samples);
            
            if (currentVolume > volumeThreshold)
            {
                // 音量が閾値以上の場合のみピッチを検知
                float pitch = CalculatePitchSimple(samples);
                
                ProcessPitch(pitch);
            }
            else
            {
                // 音量が低い場合はピッチ検知を停止
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
        
        // 範囲内の周波数のみ返す
        if (frequency >= minFrequency && frequency <= maxFrequency)
        {
            return frequency;
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
    }
    
    [ContextMenu("Disable Test Mode")]
    void DisableTestMode()
    {
        useTestMode = false;
    }
    
    [ContextMenu("Lower Volume Threshold")]
    void LowerVolumeThreshold()
    {
        volumeThreshold *= 0.5f;
    }
    
    [ContextMenu("Raise Volume Threshold")]
    void RaiseVolumeThreshold()
    {
        volumeThreshold *= 2f;
    }
    
    public System.Action<float> OnPitchDetected;
}
