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
                // 音量が低い場合はピッチ検知を停止
                if (enableDebugLog)
                {
                    Debug.Log($"ImprovedPitchAnalyzer - Volume too low: {currentVolume:F6} <= {volumeThreshold:F6}");
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
        // FromGeneと同じ：最小限のDC除去のみ（フィルタリングは行わない）
        // DCオフセットがあると相関値が大きくなりすぎるため、最小限の除去のみ
        float[] processedSamples = RemoveDCOffsetOnly(samples);
        
        // オートコリレーション法のみでピッチ検出
        float acConfidence;
        float autocorrelationPitch = CalculatePitchAutocorrelation(processedSamples, out acConfidence);
        
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
    
    float[] RemoveDCOffsetOnly(float[] samples)
    {
        // 最小限のDCオフセット除去のみ（FromGeneでは行っていないが、DCオフセットがあると相関値が大きくなりすぎる）
        float[] processed = new float[samples.Length];
        float mean = 0f;
        
        for (int i = 0; i < samples.Length; i++)
        {
            mean += samples[i];
        }
        mean /= samples.Length;
        
        for (int i = 0; i < samples.Length; i++)
        {
            processed[i] = samples[i] - mean;
        }
        
        return processed;
    }
    
    float CalculatePitchAutocorrelation(float[] samples, out float confidence)
    {
        // オートコリレーション法による基本周波数検出（FromGeneプロジェクトの実装を参考）
        int sampleRate = voiceDetector.sampleRate;
        
        // 検索範囲を設定（FromGeneと同じ方法）
        int minPeriod = Mathf.FloorToInt((float)sampleRate / maxFrequency); // 高周波数に対応する最小周期
        int maxPeriod = Mathf.FloorToInt((float)sampleRate / minFrequency); // 低周波数に対応する最大周期
        
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
        int bestPeriod = -1;
        
        // FromGeneの実装を完全に再現
        // correlations配列は使用されないが、FromGeneと同じ構造を維持
        // correlations[0]は常に0なので、normalizedCorr = corr / (0 || 1) = corr / 1 = corr
        // つまり、正規化は行われず、相関値の合計をそのまま使用
        
        // 各周期でオートコリレーションを計算
        for (int period = minPeriod; period < maxPeriod && period < samples.Length / 2; period++)
        {
            float correlation = 0f;
            
            // FromGeneと同じ：合計値を計算（平均を取らない）
            for (int i = 0; i < samples.Length - period; i++)
            {
                correlation += samples[i] * samples[i + period];
            }
            
            // FromGeneの実装：normalizedCorr = corr / (correlations[0] || 1)
            // correlations[0]は0なので、実際には corr / 1 = corr（正規化なし）
            float normalizedCorr = correlation; // 正規化しない
            
            if (normalizedCorr > maxCorrelation)
            {
                maxCorrelation = normalizedCorr;
                bestPeriod = period;
            }
        }
        
        confidence = maxCorrelation;
        
        // FromGeneと同じ閾値チェック
        // FromGeneでは0.9だが、正規化されていない相関値の合計に対する閾値
        // 相関値の合計はサンプル数に依存するため、最大可能相関値で正規化して比較
        float maxPossibleCorrelation = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            maxPossibleCorrelation += samples[i] * samples[i];
        }
        
        // 正規化された相関値（0-1の範囲、FromGeneの0.9に相当）
        float normalizedMaxCorrelation = maxPossibleCorrelation > 0f ? maxCorrelation / maxPossibleCorrelation : 0f;
        
        if (enableDebugLog)
        {
            Debug.Log($"Autocorrelation - BestPeriod: {bestPeriod}, RawCorrelation: {maxCorrelation:F6}, NormalizedCorrelation: {normalizedMaxCorrelation:F6}, Threshold: {autocorrelationThreshold:F6}, ExpectedFreq: {(bestPeriod > 0 ? (float)sampleRate / bestPeriod : 0f):F1} Hz");
        }
        
        // FromGeneの閾値0.9に相当（正規化後）
        // autocorrelationThresholdを0.9に設定すると、FromGeneと同じ動作になる
        float threshold = autocorrelationThreshold; // デフォルト0.1だが、0.9に近い値に調整可能
        
        if (bestPeriod > 1 && normalizedMaxCorrelation > threshold)
        {
            // 周波数計算：sampleRate / period（FromGeneと同じ）
            float frequency = (float)sampleRate / bestPeriod;
            
            if (enableDebugLog)
            {
                Debug.Log($"Autocorrelation success - Period: {bestPeriod}, Frequency: {frequency:F1} Hz, Correlation: {maxCorrelation:F6}");
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
        
        OnPitchDetected?.Invoke(smoothedPitch);
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
