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
    [Tooltip("高周波数の相関値に対する抑制係数（0.0-1.0、高いほど高周波数を抑制）")]
    [Range(0f, 1f)]
    public float highFrequencyPenalty = 0.4f; // 高周波数抑制係数
    
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
        
        // 4. 検出されたピッチをそのまま返す（範囲チェックは削除）
        // グラフ表示時に範囲外の値はクランプされるため、ここでは実際のピッチを返す
        float finalPitch = 0f;
        if (autocorrelationPitch > 0f)
        {
            finalPitch = autocorrelationPitch;
            
            if (enableDebugLog)
            {
                bool inRange = (autocorrelationPitch >= minFrequency && autocorrelationPitch <= maxFrequency);
                if (inRange)
                {
                    Debug.Log($"Pitch detected: {finalPitch:F1} Hz (Range: {minFrequency}-{maxFrequency} Hz)");
                }
                else
                {
                    Debug.Log($"Pitch detected (out of calibration range): {finalPitch:F1} Hz (Calibration Range: {minFrequency}-{maxFrequency} Hz)");
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
        // オートコリレーション法による基本周波数検出（複数ピーク検出と高周波数抑制を追加）
        int sampleRate = voiceDetector.sampleRate;
        
        // 検索範囲を広げる（カリブレーション範囲に関係なく、実際のピッチを検出可能にする）
        // 人間の声の範囲をカバー: 50Hz（低い男性の声）～ 2000Hz（高い女性の声）
        const float searchMinFrequency = 50f;  // 検索範囲の最小周波数
        const float searchMaxFrequency = 2000f; // 検索範囲の最大周波数
        int minPeriod = Mathf.FloorToInt((float)sampleRate / searchMaxFrequency); // 高周波数に対応する最小周期
        int maxPeriod = Mathf.FloorToInt((float)sampleRate / searchMinFrequency); // 低周波数に対応する最大周期
        
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
        
        // 最大可能相関値を計算（正規化用）
        float maxPossibleCorrelation = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            maxPossibleCorrelation += samples[i] * samples[i];
        }
        
        // 複数ピークを検出するためのリスト
        List<PeakData> peaks = new List<PeakData>();
        
        // 各周期でオートコリレーションを計算
        for (int period = minPeriod; period < maxPeriod && period < samples.Length / 2; period++)
        {
            float correlation = 0f;
            
            // 相関値を計算
            for (int i = 0; i < samples.Length - period; i++)
            {
                correlation += samples[i] * samples[i + period];
            }
            
            // 正規化された相関値を計算
            float normalizedCorr = maxPossibleCorrelation > 0f ? correlation / maxPossibleCorrelation : 0f;
            
            // 閾値以上の相関値を持つ周期をピーク候補として記録
            if (normalizedCorr > autocorrelationThreshold)
            {
                float frequency = (float)sampleRate / period;
                
                // 高周波数の相関値にペナルティを適用
                float frequencyRatio = frequency / searchMaxFrequency; // 0.0 (50Hz) ～ 1.0 (2000Hz)
                float weightedCorr = normalizedCorr * (1.0f - frequencyRatio * highFrequencyPenalty);
                
                peaks.Add(new PeakData
                {
                    period = period,
                    frequency = frequency,
                    rawCorrelation = correlation,
                    normalizedCorrelation = normalizedCorr,
                    weightedCorrelation = weightedCorr
                });
            }
        }
        
        if (peaks.Count == 0)
        {
            confidence = 0f;
            if (enableDebugLog)
            {
                Debug.LogWarning("Autocorrelation failed - No peaks found above threshold");
            }
            return 0f;
        }
        
        // 重み付け後の相関値でソート（高い順）
        peaks.Sort((a, b) => b.weightedCorrelation.CompareTo(a.weightedCorrelation));
        
        // 最も高い重み付け相関値を持つピークを取得
        PeakData bestPeak = peaks[0];
        
        // 周波数が低い順にソートして、最も低い周波数（基本周波数）を探す
        peaks.Sort((a, b) => a.frequency.CompareTo(b.frequency));
        
        // 重み付け相関値が十分高いピークの中で、最も低い周波数を選択
        // ただし、最良の重み付け相関値の一定割合（例：70%）以上のピークのみを考慮
        float correlationThreshold = bestPeak.weightedCorrelation * 0.7f;
        float fundamentalFrequency = bestPeak.frequency;
        
        foreach (var peak in peaks)
        {
            if (peak.weightedCorrelation >= correlationThreshold)
            {
                // より低い周波数で、十分な相関値を持つピークが見つかった場合
                if (peak.frequency < fundamentalFrequency)
                {
                    fundamentalFrequency = peak.frequency;
                }
            }
        }
        
        confidence = bestPeak.weightedCorrelation;
        
        if (enableDebugLog)
        {
            Debug.Log($"Autocorrelation - Found {peaks.Count} peaks. Best weighted correlation: {bestPeak.weightedCorrelation:F6} at {bestPeak.frequency:F1} Hz. Selected fundamental: {fundamentalFrequency:F1} Hz");
            if (peaks.Count > 1)
            {
                string peakInfo = "Peaks: ";
                for (int i = 0; i < Mathf.Min(peaks.Count, 5); i++)
                {
                    peakInfo += $"{peaks[i].frequency:F1}Hz({peaks[i].weightedCorrelation:F3}) ";
                }
                Debug.Log(peakInfo);
            }
        }
        
        return fundamentalFrequency;
    }
    
    // ピークデータを保持する構造体
    private struct PeakData
    {
        public int period;
        public float frequency;
        public float rawCorrelation;
        public float normalizedCorrelation;
        public float weightedCorrelation;
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
