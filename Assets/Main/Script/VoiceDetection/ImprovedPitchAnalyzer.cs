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
    
    [Header("Multi-Algorithm Settings")]
    [Tooltip("オートコリレーション法の重み付け係数")]
    [Range(0f, 1f)]
    public float autocorrelationWeight = 0.4f;
    [Tooltip("YINアルゴリズムの重み付け係数")]
    [Range(0f, 1f)]
    public float yinWeight = 0.3f;
    [Tooltip("ケプストラム法の重み付け係数")]
    [Range(0f, 1f)]
    public float cepstrumWeight = 0.3f;
    [Tooltip("YINアルゴリズムの閾値（低いほど検出しやすい）")]
    [Range(0f, 1f)]
    public float yinThreshold = 0.1f;
    
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
        
        // 複数のアルゴリズムでピッチ検出
        List<PitchResult> results = new List<PitchResult>();
        
        // 1. オートコリレーション法
        if (autocorrelationWeight > 0f)
        {
            float acConfidence;
            float acPitch = CalculatePitchAutocorrelation(processedSamples, out acConfidence);
            if (acPitch > 0f)
            {
                results.Add(new PitchResult
                {
                    pitch = acPitch,
                    confidence = acConfidence,
                    weight = autocorrelationWeight,
                    algorithm = "Autocorrelation"
                });
            }
        }
        
        // 2. YINアルゴリズム
        if (yinWeight > 0f)
        {
            float yinConfidence;
            float yinPitch = CalculatePitchYIN(processedSamples, out yinConfidence);
            if (yinPitch > 0f)
            {
                results.Add(new PitchResult
                {
                    pitch = yinPitch,
                    confidence = yinConfidence,
                    weight = yinWeight,
                    algorithm = "YIN"
                });
            }
        }
        
        // 3. ケプストラム法
        if (cepstrumWeight > 0f)
        {
            float cepstrumConfidence;
            float cepstrumPitch = CalculatePitchCepstrum(processedSamples, out cepstrumConfidence);
            if (cepstrumPitch > 0f)
            {
                results.Add(new PitchResult
                {
                    pitch = cepstrumPitch,
                    confidence = cepstrumConfidence,
                    weight = cepstrumWeight,
                    algorithm = "Cepstrum"
                });
            }
        }
        
        // 結果を統合
        float finalPitch = 0f;
        
        if (results.Count > 0)
        {
            // 重み付け平均を計算
            float weightedSum = 0f;
            float totalWeight = 0f;
            
            foreach (var result in results)
            {
                float effectiveWeight = result.weight * result.confidence;
                weightedSum += result.pitch * effectiveWeight;
                totalWeight += effectiveWeight;
            }
            
            if (totalWeight > 0f)
            {
                finalPitch = weightedSum / totalWeight;
            }
            else
            {
                // 信頼度が低い場合は、重みのみで平均
                weightedSum = 0f;
                totalWeight = 0f;
                foreach (var result in results)
                {
                    weightedSum += result.pitch * result.weight;
                    totalWeight += result.weight;
                }
                if (totalWeight > 0f)
                {
                    finalPitch = weightedSum / totalWeight;
                }
            }
            
            if (enableDebugLog)
            {
                string resultInfo = $"Multi-Algorithm Results: ";
                foreach (var result in results)
                {
                    resultInfo += $"{result.algorithm}: {result.pitch:F1}Hz (conf:{result.confidence:F3}, weight:{result.weight:F2}) ";
                }
                resultInfo += $"Final: {finalPitch:F1}Hz";
                Debug.Log(resultInfo);
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("All pitch detection algorithms returned 0 - no pitch detected");
            }
        }
        
        // フレーム履歴による安定化
        if (useAdvancedFiltering && finalPitch > 0f)
        {
            finalPitch = StabilizeWithHistory(finalPitch);
        }
        
        return finalPitch;
    }
    
    // ピッチ検出結果を保持する構造体
    private struct PitchResult
    {
        public float pitch;
        public float confidence;
        public float weight;
        public string algorithm;
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
                
                // 高周波数の相関値にペナルティを適用（周波数帯域ごとに調整）
                float penaltyFactor = 0f;
                if (frequency <= 300f)
                {
                    // 300Hz以下: 抑制なし（地声のハミングを保護）
                    penaltyFactor = 0f;
                }
                else if (frequency <= 1000f)
                {
                    // 300-1000Hz: 軽い抑制（裏声のハミングを保護）
                    float ratio = (frequency - 300f) / 700f; // 0.0 (300Hz) ～ 1.0 (1000Hz)
                    penaltyFactor = highFrequencyPenalty * 0.3f * ratio;
                }
                else if (frequency <= 1500f)
                {
                    // 1000-1500Hz: 中程度の抑制
                    float ratio = (frequency - 1000f) / 500f; // 0.0 (1000Hz) ～ 1.0 (1500Hz)
                    penaltyFactor = highFrequencyPenalty * (0.3f + 0.4f * ratio); // 0.3 ～ 0.7
                }
                else
                {
                    // 1500Hz以上: 強い抑制（誤検出を防ぐ）
                    float ratio = (frequency - 1500f) / 500f; // 0.0 (1500Hz) ～ 1.0 (2000Hz)
                    penaltyFactor = highFrequencyPenalty * (0.7f + 0.3f * ratio); // 0.7 ～ 1.0
                }
                
                float weightedCorr = normalizedCorr * (1.0f - penaltyFactor);
                
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
        
        // 各ピークにスコアを付与（元の相関値と重み付け相関値の両方を考慮）
        float maxOriginalCorr = 0f;
        float maxWeightedCorr = 0f;
        foreach (var peak in peaks)
        {
            maxOriginalCorr = Mathf.Max(maxOriginalCorr, peak.normalizedCorrelation);
            maxWeightedCorr = Mathf.Max(maxWeightedCorr, peak.weightedCorrelation);
        }
        
        PeakData? bestScoredPeak = null;
        float bestScore = -1f;
        
        foreach (var peak in peaks)
        {
            // スコア計算：元の相関値と重み付け相関値の両方を考慮
            float originalScore = maxOriginalCorr > 0f ? peak.normalizedCorrelation / maxOriginalCorr : 0f;
            float weightedScore = maxWeightedCorr > 0f ? peak.weightedCorrelation / maxWeightedCorr : 0f;
            
            // 低周波数（300Hz以下）にボーナスを付与（地声のハミングを保護）
            float frequencyBonus = peak.frequency <= 300f ? 0.2f : 0f;
            
            // 総合スコア：元の相関値50%、重み付け相関値30%、周波数ボーナス20%
            float totalScore = originalScore * 0.5f + weightedScore * 0.3f + frequencyBonus;
            
            if (totalScore > bestScore)
            {
                bestScore = totalScore;
                bestScoredPeak = peak;
            }
        }
        
        float fundamentalFrequency;
        string selectionReason;
        
        if (bestScoredPeak != null)
        {
            fundamentalFrequency = bestScoredPeak.Value.frequency;
            
            // 元の相関値が高い（0.5以上）場合はそれを優先
            if (bestScoredPeak.Value.normalizedCorrelation >= 0.5f)
            {
                selectionReason = $"High original correlation ({bestScoredPeak.Value.normalizedCorrelation:F3}) with score {bestScore:F3}";
            }
            else if (bestScoredPeak.Value.frequency <= 300f)
            {
                selectionReason = $"Low frequency peak ({bestScoredPeak.Value.frequency:F1} Hz) with score {bestScore:F3} (orig: {bestScoredPeak.Value.normalizedCorrelation:F3}, weighted: {bestScoredPeak.Value.weightedCorrelation:F3})";
            }
            else
            {
                selectionReason = $"Best scored peak ({bestScoredPeak.Value.frequency:F1} Hz) with score {bestScore:F3} (orig: {bestScoredPeak.Value.normalizedCorrelation:F3}, weighted: {bestScoredPeak.Value.weightedCorrelation:F3})";
            }
        }
        else
        {
            // フォールバック：最も高い重み付け相関値を持つピーク
            fundamentalFrequency = bestPeak.frequency;
            selectionReason = $"Fallback to best weighted correlation ({bestPeak.frequency:F1} Hz)";
        }
        
        confidence = bestPeak.weightedCorrelation;
        
        if (enableDebugLog)
        {
            Debug.Log($"Autocorrelation - Found {peaks.Count} peaks. Best weighted correlation: {bestPeak.weightedCorrelation:F6} at {bestPeak.frequency:F1} Hz. Selected fundamental: {fundamentalFrequency:F1} Hz");
            Debug.Log($"Selection reason: {selectionReason}");
            if (peaks.Count > 1)
            {
                string peakInfo = "Top peaks: ";
                // 重み付け相関値でソートし直して上位5つを表示
                List<PeakData> sortedPeaks = new List<PeakData>(peaks);
                sortedPeaks.Sort((a, b) => b.weightedCorrelation.CompareTo(a.weightedCorrelation));
                for (int i = 0; i < Mathf.Min(sortedPeaks.Count, 5); i++)
                {
                    var peak = sortedPeaks[i];
                    float penalty = 0f;
                    if (peak.frequency <= 300f) penalty = 0f;
                    else if (peak.frequency <= 1000f) penalty = highFrequencyPenalty * 0.3f;
                    else if (peak.frequency <= 1500f) penalty = highFrequencyPenalty * 0.7f;
                    else penalty = highFrequencyPenalty;
                    peakInfo += $"{peak.frequency:F1}Hz(orig:{peak.normalizedCorrelation:F3}, weighted:{peak.weightedCorrelation:F3}, penalty:{penalty:F3}) ";
                }
                Debug.Log(peakInfo);
            }
        }
        
        return fundamentalFrequency;
    }
    
    float CalculatePitchYIN(float[] samples, out float confidence)
    {
        // YINアルゴリズムによる基本周波数検出
        int sampleRate = voiceDetector.sampleRate;
        
        // 検索範囲: 50Hz ～ 2000Hz
        const float searchMinFrequency = 50f;
        const float searchMaxFrequency = 2000f;
        int minPeriod = Mathf.FloorToInt((float)sampleRate / searchMaxFrequency);
        int maxPeriod = Mathf.FloorToInt((float)sampleRate / searchMinFrequency);
        
        // 範囲チェック
        if (minPeriod < 1) minPeriod = 1;
        if (maxPeriod >= samples.Length / 2) maxPeriod = samples.Length / 2 - 1;
        if (minPeriod >= maxPeriod)
        {
            confidence = 0f;
            return 0f;
        }
        
        // 差分関数を計算
        float[] difference = new float[maxPeriod + 1];
        
        for (int period = minPeriod; period <= maxPeriod; period++)
        {
            float sum = 0f;
            for (int j = 0; j < samples.Length - maxPeriod; j++)
            {
                float delta = samples[j] - samples[j + period];
                sum += delta * delta;
            }
            difference[period] = sum;
        }
        
        // 累積平均正規化差分関数（CMNDF）を計算
        float[] cmndf = new float[maxPeriod + 1];
        cmndf[0] = 1f; // period=0は使用しない
        
        for (int period = minPeriod; period <= maxPeriod; period++)
        {
            // 累積和を計算（period=1からperiodまで）
            float cumulativeSum = 0f;
            for (int j = 1; j <= period; j++)
            {
                cumulativeSum += difference[j];
            }
            
            if (cumulativeSum > 0f && period > 0)
            {
                cmndf[period] = difference[period] * period / cumulativeSum;
            }
            else
            {
                cmndf[period] = 1f;
            }
        }
        
        // 閾値を超える最初の最小値を探す
        float bestPeriod = 0f;
        float bestValue = float.MaxValue;
        
        for (int period = minPeriod; period <= maxPeriod; period++)
        {
            if (cmndf[period] < yinThreshold)
            {
                // 局所的な最小値を探す（より正確な周期を求めるため）
                if (period > minPeriod && period < maxPeriod)
                {
                    if (cmndf[period] < cmndf[period - 1] && cmndf[period] < cmndf[period + 1])
                    {
                        if (cmndf[period] < bestValue)
                        {
                            bestValue = cmndf[period];
                            bestPeriod = period;
                        }
                    }
                }
            }
        }
        
        // 閾値を超える値が見つからない場合、最小値を探す
        if (bestPeriod == 0f)
        {
            for (int period = minPeriod; period <= maxPeriod; period++)
            {
                if (cmndf[period] < bestValue)
                {
                    bestValue = cmndf[period];
                    bestPeriod = period;
                }
            }
        }
        
        if (bestPeriod > 0f)
        {
            float frequency = (float)sampleRate / bestPeriod;
            confidence = 1f - bestValue; // 値が小さいほど信頼度が高い
            
            if (enableDebugLog)
            {
                Debug.Log($"YIN Algorithm - Period: {bestPeriod:F1}, Frequency: {frequency:F1} Hz, Confidence: {confidence:F3}, CMNDF: {bestValue:F6}");
            }
            
            return frequency;
        }
        
        confidence = 0f;
        if (enableDebugLog)
        {
            Debug.LogWarning("YIN Algorithm - No valid period found");
        }
        return 0f;
    }
    
    float CalculatePitchCepstrum(float[] samples, out float confidence)
    {
        // ケプストラム法による基本周波数検出
        int sampleRate = voiceDetector.sampleRate;
        
        // FFTのサイズを決定（2のべき乗）
        int fftSize = 1;
        while (fftSize < samples.Length)
        {
            fftSize *= 2;
        }
        
        // FFTを実行（簡易実装：UnityのAudioSource.GetSpectrumDataを使用できないため、簡易FFTを実装）
        // 注意: 完全なFFT実装は複雑なため、簡易版を使用
        float[] spectrum = new float[fftSize];
        
        // 簡易FFT（実部のみ、高速化のため簡略化）
        for (int k = 0; k < fftSize / 2; k++)
        {
            float real = 0f;
            float imag = 0f;
            
            for (int n = 0; n < samples.Length && n < fftSize; n++)
            {
                float angle = 2f * Mathf.PI * k * n / fftSize;
                real += samples[n] * Mathf.Cos(angle);
                imag += samples[n] * Mathf.Sin(angle);
            }
            
            spectrum[k] = Mathf.Sqrt(real * real + imag * imag);
        }
        
        // 対数スペクトルを計算
        float[] logSpectrum = new float[fftSize / 2];
        for (int i = 0; i < fftSize / 2; i++)
        {
            logSpectrum[i] = Mathf.Log(spectrum[i] + 1e-10f); // ゼロ除算を防ぐ
        }
        
        // 逆FFT（ケプストラム）を計算
        float[] cepstrum = new float[fftSize / 2];
        
        // 検索範囲: 50Hz ～ 2000Hz に対応するケフレンシー範囲
        int minQuefrency = Mathf.FloorToInt((float)sampleRate / 2000f); // 高周波数に対応
        int maxQuefrency = Mathf.FloorToInt((float)sampleRate / 50f);   // 低周波数に対応
        
        if (minQuefrency < 1) minQuefrency = 1;
        if (maxQuefrency >= fftSize / 2) maxQuefrency = fftSize / 2 - 1;
        
        for (int n = minQuefrency; n <= maxQuefrency && n < fftSize / 2; n++)
        {
            float real = 0f;
            float imag = 0f;
            
            for (int k = 0; k < fftSize / 2; k++)
            {
                float angle = 2f * Mathf.PI * k * n / (fftSize / 2);
                real += logSpectrum[k] * Mathf.Cos(angle);
                imag += logSpectrum[k] * Mathf.Sin(angle);
            }
            
            cepstrum[n] = Mathf.Sqrt(real * real + imag * imag);
        }
        
        // ケプストラムのピークを探す（基本周波数に対応）
        float maxCepstrum = 0f;
        int bestQuefrency = 0;
        
        for (int n = minQuefrency; n <= maxQuefrency && n < fftSize / 2; n++)
        {
            if (cepstrum[n] > maxCepstrum)
            {
                maxCepstrum = cepstrum[n];
                bestQuefrency = n;
            }
        }
        
        if (bestQuefrency > 0)
        {
            float frequency = (float)sampleRate / bestQuefrency;
            confidence = maxCepstrum / (fftSize / 2); // 正規化された信頼度
            
            if (enableDebugLog)
            {
                Debug.Log($"Cepstrum Method - Quefrency: {bestQuefrency}, Frequency: {frequency:F1} Hz, Confidence: {confidence:F3}, Cepstrum: {maxCepstrum:F6}");
            }
            
            return frequency;
        }
        
        confidence = 0f;
        if (enableDebugLog)
        {
            Debug.LogWarning("Cepstrum Method - No valid peak found");
        }
        return 0f;
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
