using UnityEngine;
using System.Collections.Generic;

public class ImprovedPitchAnalyzer : MonoBehaviour
{
    [Header("Pitch Settings")]
    public float pitchSensitivity = 1.0f;
    public float minFrequency = 80f;  // 最低周波数
    public float maxFrequency = 1000f; // 最高周波数
    public float volumeThreshold = 0.01f; // ピッチ検知の音量閾値
    public float smoothingFactor = 0.1f; // ピッチのスムージング係数
    
    [Header("Advanced Settings")]
    public float autocorrelationThreshold = 0.1f; // オートコリレーションの閾値（低いほど検出しやすい）
    public int frameHistorySize = 5; // フレーム履歴のサイズ
    public bool useAdvancedFiltering = true; // 高度なフィルタリングを使用
    
    [Header("Movement Pitch Settings")]
    public float leftPitch = 200f;    // 左移動用のピッチ
    public float centerPitch = 400f;  // 中央（停止）用のピッチ
    public float rightPitch = 600f;   // 右移動用のピッチ
    
    [Header("Test Settings")]
    public bool useTestMode = false;  // テストモードを使用
    public float testPitch = 400f;   // テスト用の固定音程
    
    [Header("Debug Settings")]
    public bool enableDebugLog = false; // デバッグログを有効化
    
    private VoiceDetector voiceDetector;
    private VolumeAnalyzer volumeAnalyzer;
    private float[] fftBuffer;
    public float lastDetectedPitch = 0f;
    private float smoothedPitch = 0f;
    private bool hasValidPitch = false;
    private float lastFrameRms = 0f;
    
    // フレーム履歴
    private Queue<float> pitchHistory = new Queue<float>();
    private Queue<float> volumeHistory = new Queue<float>();
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        
        if (voiceDetector == null)
        {
            Debug.LogError("ImprovedPitchAnalyzer: VoiceDetectorが見つかりません！");
        }
        else
        {
            int bufferSize = voiceDetector.bufferSize;
            fftBuffer = new float[bufferSize];
            Debug.Log($"ImprovedPitchAnalyzer: 初期化完了 - BufferSize: {bufferSize}, SampleRate: {voiceDetector.sampleRate}");
        }
        
        if (volumeAnalyzer == null)
        {
            Debug.LogWarning("ImprovedPitchAnalyzer: VolumeAnalyzerが見つかりません（オプション）");
        }
    }
    
    void Update()
    {
        if (useTestMode)
        {
            // テストモード：固定の音程を使用
            if (enableDebugLog)
            {
                Debug.Log($"Test Mode: Using fixed pitch {testPitch} Hz");
            }
            ProcessPitch(testPitch);
        }
        else
        {
            // 通常モード：実際の音声を解析
            if (voiceDetector == null)
            {
                if (enableDebugLog)
                {
                    Debug.LogError("ImprovedPitchAnalyzer: VoiceDetector is null!");
                }
                return;
            }
            
            float[] samples = voiceDetector.GetAudioSamples();
            if (samples == null)
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning("ImprovedPitchAnalyzer: Audio samples are null");
                }
                return;
            }
            
            if (samples.Length == 0)
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning("ImprovedPitchAnalyzer: Audio samples array is empty");
                }
                return;
            }
            
            // 音量をチェックしてピッチ検知の信頼性を判断
            float currentVolume = CalculateVolume(samples);
            lastFrameRms = currentVolume;
            
            if (enableDebugLog)
            {
                Debug.Log($"ImprovedPitchAnalyzer - Volume: {currentVolume:F6}, Threshold: {volumeThreshold:F6}, Samples Length: {samples.Length}");
            }
            
            if (currentVolume > volumeThreshold)
            {
                // 音量が閾値以上の場合のみピッチを検知
                if (enableDebugLog)
                {
                    Debug.Log($"ImprovedPitchAnalyzer - Volume threshold passed, calculating pitch...");
                }
                
                float pitch = CalculatePitchAdvanced(samples);
                
                if (enableDebugLog)
                {
                    Debug.Log($"ImprovedPitchAnalyzer - Pitch detected: {pitch:F1} Hz (Volume: {currentVolume:F6})");
                }
                
                ProcessPitch(pitch);
            }
            else
            {
                // 音量が低い場合はピッチ検知を停止（0を渡すが、ProcessPitch内で以前の値を維持）
                if (enableDebugLog)
                {
                    Debug.Log($"ImprovedPitchAnalyzer - Volume too low: {currentVolume:F6} <= {volumeThreshold:F6}, maintaining previous pitch");
                }
                ProcessPitch(0f);
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
    
    float CalculatePitchAdvanced(float[] samples)
    {
        // 1. 音量正規化 + DC除去 + プリエンファシス
        float[] normalizedSamples = NormalizeSamples(samples);
        
        // 2. バンドパスフィルタリング
        float[] filteredSamples = ApplyBandpassFilter(normalizedSamples);
        
        // 3. オートコリレーション法のみでピッチ検出
        float acConfidence;
        float autocorrelationPitch = CalculatePitchAutocorrelation(filteredSamples, out acConfidence);
        
        // 4. 周波数範囲のチェック
        float finalPitch = 0f;
        if (autocorrelationPitch > 0f)
        {
            if (autocorrelationPitch >= minFrequency && autocorrelationPitch <= maxFrequency)
            {
                finalPitch = autocorrelationPitch;
                
                if (enableDebugLog)
                {
                    Debug.Log($"Pitch in range: {finalPitch:F1} Hz (Range: {minFrequency}-{maxFrequency} Hz)");
                }
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"Pitch out of range: {autocorrelationPitch:F1} Hz (Range: {minFrequency}-{maxFrequency} Hz)");
                }
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Autocorrelation returned 0 - no pitch detected");
            }
        }
        
        // 5. フレーム履歴による安定化
        if (useAdvancedFiltering && finalPitch > 0f)
        {
            finalPitch = StabilizeWithHistory(finalPitch);
        }
        
        return finalPitch;
    }
    
    float[] NormalizeSamples(float[] samples)
    {
        // 音量正規化（音量に依存しないように）
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
        // DCオフセット除去
        float mean = 0f;
        for (int i = 0; i < normalized.Length; i++) mean += normalized[i];
        mean /= normalized.Length;
        for (int i = 0; i < normalized.Length; i++) normalized[i] -= mean;

        // プリエンファシス（高域強調）
        const float pre = 0.97f;
        for (int i = normalized.Length - 1; i >= 1; i--)
        {
            normalized[i] = normalized[i] - pre * normalized[i - 1];
        }
        normalized[0] *= 0.5f;

        return normalized;
    }
    
    float[] ApplyBandpassFilter(float[] samples)
    {
        // 簡易バンドパスフィルタ（人間の声の周波数帯域のみを通過）
        float[] filtered = new float[samples.Length];
        
        // ローパスフィルタ（高周波ノイズ除去）
        float alpha = 0.1f;
        filtered[0] = samples[0];
        for (int i = 1; i < samples.Length; i++)
        {
            filtered[i] = alpha * samples[i] + (1f - alpha) * filtered[i - 1];
        }
        
        // ハイパスフィルタ（低周波ノイズ除去）
        float[] highPassFiltered = new float[samples.Length];
        float beta = 0.9f;
        highPassFiltered[0] = filtered[0];
        for (int i = 1; i < filtered.Length; i++)
        {
            highPassFiltered[i] = beta * (highPassFiltered[i - 1] + filtered[i] - filtered[i - 1]);
        }
        
        return highPassFiltered;
    }
    
    float CalculatePitchAutocorrelation(float[] samples, out float confidence)
    {
        // オートコリレーション法による基本周波数検出
        int minPeriod = Mathf.RoundToInt(voiceDetector.sampleRate / maxFrequency);
        int maxPeriod = Mathf.RoundToInt(voiceDetector.sampleRate / minFrequency);
        
        // 範囲チェック
        if (minPeriod < 1) minPeriod = 1;
        if (maxPeriod >= samples.Length / 2) maxPeriod = samples.Length / 2 - 1;
        if (minPeriod >= maxPeriod)
        {
            confidence = 0f;
            if (enableDebugLog)
            {
                Debug.LogWarning($"Invalid period range: min={minPeriod}, max={maxPeriod}, samples.Length={samples.Length}");
            }
            return 0f;
        }
        
        float maxCorrelation = 0f;
        int bestPeriod = 0;
        
        // 自己相関の正規化用に、period=0の相関を計算
        float zeroCorrelation = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            zeroCorrelation += samples[i] * samples[i];
        }
        zeroCorrelation /= samples.Length;
        
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
                
                // 正規化（period=0の相関で割る）
                if (zeroCorrelation > 0f)
                {
                    correlation = correlation / zeroCorrelation;
                }
                
                if (correlation > maxCorrelation)
                {
                    maxCorrelation = correlation;
                    bestPeriod = period;
                }
            }
        }
        
        confidence = maxCorrelation;
        
        if (enableDebugLog)
        {
            Debug.Log($"Autocorrelation - BestPeriod: {bestPeriod}, MaxCorrelation: {maxCorrelation:F6}, Threshold: {autocorrelationThreshold:F6}");
        }
        
        if (bestPeriod > 1 && maxCorrelation > autocorrelationThreshold)
        {
            int p0 = bestPeriod - 1;
            int p1 = bestPeriod;
            int p2 = bestPeriod + 1;
            float r0 = ComputeAutocorrAt(samples, p0);
            float r1 = ComputeAutocorrAt(samples, p1);
            float r2 = ComputeAutocorrAt(samples, p2);
            float denom = 2f * (r0 - 2f * r1 + r2);
            float delta = denom != 0f ? (r0 - r2) / denom : 0f; // -1..1
            float refined = Mathf.Clamp(p1 + delta, minPeriod, maxPeriod);
            float frequency = (float)voiceDetector.sampleRate / refined;
            
            if (enableDebugLog)
            {
                Debug.Log($"Autocorrelation success - Frequency: {frequency:F1} Hz, Refined period: {refined:F2}");
            }
            
            return frequency;
        }
        else
        {
            if (enableDebugLog)
            {
                if (bestPeriod <= 1)
                {
                    Debug.LogWarning($"Autocorrelation failed - BestPeriod too small: {bestPeriod}");
                }
                else
                {
                    Debug.LogWarning($"Autocorrelation failed - Correlation too low: {maxCorrelation:F6} <= {autocorrelationThreshold:F6}");
                }
            }
        }
        
        return 0f;
    }

    float ComputeAutocorrAt(float[] samples, int period)
    {
        if (period <= 0 || period >= samples.Length) return 0f;
        float c = 0f; int n = 0;
        for (int i = 0; i < samples.Length - period; i++) { c += samples[i] * samples[i + period]; n++; }
        return n > 0 ? c / n : 0f;
    }
    
    
    float StabilizeWithHistory(float currentPitch)
    {
        // フレーム履歴による安定化
        pitchHistory.Enqueue(currentPitch);
        
        if (pitchHistory.Count > frameHistorySize)
        {
            pitchHistory.Dequeue();
        }
        
        // 履歴の平均を計算
        float sum = 0f;
        int count = 0;
        foreach (float pitch in pitchHistory)
        {
            if (pitch > 0f)
            {
                sum += pitch;
                count++;
            }
        }
        
        if (count > 0)
        {
            float averagePitch = sum / count;
            
            // 外れ値を除去
            float variance = 0f;
            foreach (float pitch in pitchHistory)
            {
                if (pitch > 0f)
                {
                    variance += Mathf.Pow(pitch - averagePitch, 2);
                }
            }
            variance /= count;
            float standardDeviation = Mathf.Sqrt(variance);
            
            // 標準偏差の2倍以内の値のみを考慮
            if (Mathf.Abs(currentPitch - averagePitch) <= 2f * standardDeviation)
            {
                return currentPitch;
            }
            else
            {
                return averagePitch;
            }
        }
        
        return currentPitch;
    }
    
    void ProcessPitch(float pitch)
    {
        // 常にスムージングして値を出力（オートコリレーションのみの実装）
        if (hasValidPitch)
        {
            if (pitch > 0f)
            {
                smoothedPitch = Mathf.Lerp(smoothedPitch, pitch, smoothingFactor);
            }
            else
            {
                // ピッチが検知されない場合は、スムージングされた値を維持
                smoothedPitch = smoothedPitch; // 前の値を維持
            }
        }
        else
        {
            if (pitch > 0f)
            {
                smoothedPitch = pitch;
                hasValidPitch = true;
            }
            else
            {
                smoothedPitch = 0f;
            }
        }
        
        lastDetectedPitch = smoothedPitch;
        
        if (enableDebugLog)
        {
            Debug.Log($"ImprovedPitchAnalyzer - ProcessPitch: Raw={pitch:F1} Hz, Smoothed={smoothedPitch:F1} Hz, LastDetected={lastDetectedPitch:F1} Hz");
        }
        
        // イベント発火（smoothedPitchが0より大きい場合のみ）
        if (smoothedPitch > 0f)
        {
            int subscriberCount = OnPitchDetected?.GetInvocationList().Length ?? 0;
            Debug.Log($"ImprovedPitchAnalyzer - Invoking OnPitchDetected with {smoothedPitch:F1} Hz (hasValidPitch={hasValidPitch}, subscribers={subscriberCount})");
            OnPitchDetected?.Invoke(smoothedPitch);
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log($"ImprovedPitchAnalyzer - Not invoking OnPitchDetected (smoothedPitch={smoothedPitch:F1} <= 0, hasValidPitch={hasValidPitch})");
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
    
    public System.Action<float> OnPitchDetected;
    
    // UI初期化用のメソッド
    public void InitializeUIComponents()
    {
        // VoiceDisplayとGameManagerのUIコンポーネントを更新
        VoiceDisplay voiceDisplay = FindObjectOfType<VoiceDisplay>();
        if (voiceDisplay != null)
        {
            voiceDisplay.SetPitchRange(minFrequency, maxFrequency);
            Debug.Log($"ImprovedPitchAnalyzer: Updated VoiceDisplay pitch range to {minFrequency}-{maxFrequency} Hz");
        }
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            // GameManagerの設定を直接更新（publicフィールドがある場合）
            Debug.Log($"ImprovedPitchAnalyzer: Pitch range set to {minFrequency}-{maxFrequency} Hz");
        }
    }
    
    // 設定値変更時にUIを更新するメソッド
    public void UpdateUISettings()
    {
        InitializeUIComponents();
    }
}
