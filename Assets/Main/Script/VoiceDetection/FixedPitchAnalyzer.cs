using UnityEngine;
using System.Collections.Generic;

public class FixedPitchAnalyzer : MonoBehaviour
{
    [Header("Pitch Settings")]
    public float pitchSensitivity = 1.0f;
    public float minFrequency = 80f;
    public float maxFrequency = 1000f;
    public float volumeThreshold = 0.01f;
    public float smoothingFactor = 0.1f;
    
    [Header("Advanced Settings")]
    public float autocorrelationThreshold = 0.3f;
    public int frameHistorySize = 5;
    public bool useAdvancedFiltering = true;
    
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
    
    // フレーム履歴
    private Queue<float> pitchHistory = new Queue<float>();
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        
        if (voiceDetector == null)
        {
            Debug.LogError("VoiceDetector not found!");
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
            if (voiceDetector == null) return;
            
            float[] samples = voiceDetector.GetAudioSamples();
            if (samples == null) return;
            
            float currentVolume = CalculateVolume(samples);
            
            if (currentVolume > volumeThreshold)
            {
                float pitch = CalculatePitchFixed(samples);
                ProcessPitch(pitch);
            }
            else
            {
                ProcessPitch(0f);
            }
        }
    }
    
    float CalculateVolume(float[] samples)
    {
        if (samples == null || samples.Length == 0) return 0f;
        
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / samples.Length);
    }
    
    float CalculatePitchFixed(float[] samples)
    {
        if (samples == null || samples.Length == 0) return 0f;
        
        // 1. 音量正規化
        float[] normalizedSamples = NormalizeSamples(samples);
        
        // 2. バンドパスフィルタリング
        float[] filteredSamples = ApplyBandpassFilter(normalizedSamples);
        
        // 3. 修正されたオートコリレーション法
        float autocorrelationPitch = CalculatePitchAutocorrelationFixed(filteredSamples);
        
        // 4. 簡易ハーモニック検出
        float harmonicPitch = CalculatePitchHarmonicSimple(filteredSamples);
        
        // 5. 結果の統合
        float finalPitch = CombinePitchResults(autocorrelationPitch, harmonicPitch);
        
        // 6. 検出されたピッチをそのまま返す（範囲チェックは削除）
        // グラフ表示時に範囲外の値はクランプされるため、ここでは実際のピッチを返す
        // finalPitchをそのまま返す（範囲チェックは削除）
        
        return finalPitch;
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
        
        // ローパスフィルタ
        float alpha = 0.1f;
        filtered[0] = samples[0];
        for (int i = 1; i < samples.Length; i++)
        {
            filtered[i] = alpha * samples[i] + (1f - alpha) * filtered[i - 1];
        }
        
        // ハイパスフィルタ
        float[] highPassFiltered = new float[samples.Length];
        float beta = 0.9f;
        highPassFiltered[0] = filtered[0];
        for (int i = 1; i < filtered.Length; i++)
        {
            highPassFiltered[i] = beta * (highPassFiltered[i - 1] + filtered[i] - filtered[i - 1]);
        }
        
        return highPassFiltered;
    }
    
    float CalculatePitchAutocorrelationFixed(float[] samples)
    {
        // 修正されたオートコリレーション法
        // 検索範囲を広げる（カリブレーション範囲に関係なく、実際のピッチを検出可能にする）
        const float searchMinFrequency = 50f;  // 検索範囲の最小周波数
        const float searchMaxFrequency = 2000f; // 検索範囲の最大周波数
        int minPeriod = Mathf.RoundToInt(voiceDetector.sampleRate / searchMaxFrequency);
        int maxPeriod = Mathf.RoundToInt(voiceDetector.sampleRate / searchMinFrequency);
        
        // 範囲を制限
        minPeriod = Mathf.Max(minPeriod, 1);
        maxPeriod = Mathf.Min(maxPeriod, samples.Length / 4); // より安全な範囲
        
        float maxCorrelation = 0f;
        int bestPeriod = 0;
        
        for (int period = minPeriod; period <= maxPeriod; period++)
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
        
        if (bestPeriod > 0 && maxCorrelation > autocorrelationThreshold)
        {
            float frequency = (float)voiceDetector.sampleRate / bestPeriod;
            // 検出された周波数をそのまま返す（範囲チェックは削除）
            return frequency;
        }
        
        return 0f;
    }
    
    float CalculatePitchHarmonicSimple(float[] samples)
    {
        // 簡易ハーモニック検出（ゼロクロッシング法の改良版）
        int zeroCrossings = CountZeroCrossings(samples);
        
        if (zeroCrossings == 0) return 0f;
        
        // ゼロクロッシング数から周波数を推定
        float frequency = (zeroCrossings * voiceDetector.sampleRate) / (2f * samples.Length);
        
        // 検出された周波数をそのまま返す（範囲チェックは削除）
        return frequency;
    }
    
    int CountZeroCrossings(float[] samples)
    {
        if (samples == null || samples.Length < 2) return 0;
        
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
    
    float CombinePitchResults(float autocorrelationPitch, float harmonicPitch)
    {
        // 結果の統合
        if (autocorrelationPitch > 0 && harmonicPitch > 0)
        {
            // 両方の結果が有効な場合、重み付き平均
            float weight1 = 0.7f; // オートコリレーションの重み
            float weight2 = 0.3f; // ハーモニック検出の重み
            
            return (autocorrelationPitch * weight1 + harmonicPitch * weight2) / (weight1 + weight2);
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
    
    public System.Action<float> OnPitchDetected;
}
