using UnityEngine;
using System.Collections.Generic;

public class ImprovedPitchAnalyzer : MonoBehaviour
{
    [Header("Pitch Settings")]
    public float pitchSensitivity = 1.0f;
    public float minFrequency = 80f;  // 最低周波数
    public float maxFrequency = 1000f; // 最高周波数
    [Tooltip("検知閾値の比率（MinVolumeに対するパーセンテージ、0.0-1.0）")]
    [Range(0.0f, 1.0f)]
    public float volumeDetectionRatio = 0.75f; // ピッチ検知の音量閾値比率（MinVolume * volumeDetectionRatio）
    public float smoothingFactor = 0.1f; // ピッチのスムージング係数
    
    [Header("Advanced Settings")]
    public float autocorrelationThreshold = 0.1f; // オートコリレーションの閾値（低いほど検出しやすい）
    public int frameHistorySize = 5; // フレーム履歴のサイズ
    public bool useAdvancedFiltering = true; // 高度なフィルタリングを使用
    
    [Header("Confidence and Intentional Voice Detection")]
    [Tooltip("最小信頼度（この値未満の検出は無視される）")]
    [Range(0f, 1f)]
    public float minConfidence = 0.3f; // 最小信頼度
    [Tooltip("意図的な音声の最小持続時間（秒）")]
    [Range(0f, 1f)]
    public float intentionalVoiceMinDuration = 0.1f; // 意図的な音声の最小持続時間
    [Tooltip("安定性の閾値（Hz、この値以内の変化は安定とみなす）")]
    [Range(0f, 200f)]
    public float intentionalVoiceStabilityThreshold = 50f; // 安定性の閾値
    [Tooltip("音量の安定性の閾値（0-1、この値以上で安定とみなす）")]
    [Range(0f, 1f)]
    public float intentionalVoiceVolumeStability = 0.8f; // 音量の安定性の閾値
    [Tooltip("適応的スムージングを有効にする")]
    public bool adaptiveSmoothingEnabled = true; // 適応的スムージングを有効にする
    
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
    
    // 意図的な音声の検出用の履歴
    private Queue<PitchDetectionResult> recentDetections = new Queue<PitchDetectionResult>();
    private float lastAcceptedPitch = 0f;
    private float lastAcceptedTime = 0f;
    
    // ピッチ検出結果を保持する構造体
    private struct PitchDetectionResult
    {
        public float pitch;
        public float confidence;
        public float volume;
        public float time;
    }
    
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
            // テスト用の動的閾値を計算
            float testThreshold = VoiceCalibrator.MinVolume > 0f 
                ? VoiceCalibrator.MinVolume * volumeDetectionRatio * 2f 
                : 0.02f;
            ProcessPitch(testPitch, 1.0f, testThreshold); // 高い信頼度と音量でテスト
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
            
            // 動的閾値を計算（MinVolume * volumeDetectionRatio）
            float dynamicThreshold = VoiceCalibrator.MinVolume > 0f 
                ? VoiceCalibrator.MinVolume * volumeDetectionRatio 
                : 0.01f; // MinVolumeが0の場合はデフォルト値を使用
            
            if (currentVolume > dynamicThreshold)
            {
                // 音量が閾値以上の場合のみピッチを検知
                float confidence;
                float pitch = CalculatePitchAdvanced(samples, out confidence);
                
                ProcessPitch(pitch, confidence, currentVolume);
            }
            else
            {
                // 音量が低い場合はピッチ検知を停止
                ProcessPitch(0f, 0f, currentVolume);
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
    
    float CalculatePitchAdvanced(float[] samples, out float confidence)
    {
        // FromGeneと同じ：最小限のDC除去のみ（フィルタリングは行わない）
        // DCオフセットがあると相関値が大きくなりすぎるため、最小限の除去のみ
        float[] processedSamples = RemoveDCOffsetOnly(samples);
        
        // オートコリレーション法のみでピッチ検出
        float acConfidence;
        float autocorrelationPitch = CalculatePitchAutocorrelation(processedSamples, out acConfidence);
        
        // 信頼度を正規化（0-1の範囲に）
        float maxPossibleCorrelation = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            maxPossibleCorrelation += samples[i] * samples[i];
        }
        confidence = maxPossibleCorrelation > 0f ? acConfidence / maxPossibleCorrelation : 0f;
        
        // 4. 検出されたピッチをそのまま返す（範囲チェックは削除）
        // グラフ表示時に範囲外の値はクランプされるため、ここでは実際のピッチを返す
        float finalPitch = 0f;
        if (autocorrelationPitch > 0f)
        {
            finalPitch = autocorrelationPitch;
        }
        else
        {
            confidence = 0f;
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
        
        // 検索範囲を広げる（カリブレーション範囲に関係なく、実際のピッチを検出可能にする）
        // 人間の声の範囲をカバー: 50Hz（低い男性の声）～ 1000Hz（高い女性の声）
        const float searchMinFrequency = 50f;  // 検索範囲の最小周波数
        const float searchMaxFrequency = 1000f; // 検索範囲の最大周波数
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
        
        // FromGeneの閾値0.9に相当（正規化後）
        // autocorrelationThresholdを0.9に設定すると、FromGeneと同じ動作になる
        float threshold = autocorrelationThreshold; // デフォルト0.1だが、0.9に近い値に調整可能
        
        if (bestPeriod > 1 && normalizedMaxCorrelation > threshold)
        {
            // 周波数計算：sampleRate / period（FromGeneと同じ）
            float frequency = (float)sampleRate / bestPeriod;
            
            return frequency;
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
    
    void ProcessPitch(float pitch, float confidence, float volume)
    {
        float currentTime = Time.time;
        
        // 信頼度ベースのフィルタリング
        if (pitch > 0f && confidence < minConfidence)
        {
            pitch = 0f; // 信頼度が低い場合は無視
        }
        
        // 最近の検出結果を記録
        if (pitch > 0f)
        {
            recentDetections.Enqueue(new PitchDetectionResult
            {
                pitch = pitch,
                confidence = confidence,
                volume = volume,
                time = currentTime
            });
            
            // 古い検出結果を削除（最小持続時間の2倍以上前のもの）
            while (recentDetections.Count > 0 && 
                   currentTime - recentDetections.Peek().time > intentionalVoiceMinDuration * 2f)
            {
                recentDetections.Dequeue();
            }
        }
        
        // 意図的な音声の検出
        bool isIntentionalVoice = IsIntentionalVoice(pitch, confidence, volume, currentTime);
        
        // 適応的スムージング
        float effectiveSmoothingFactor = smoothingFactor;
        if (adaptiveSmoothingEnabled && pitch > 0f && hasValidPitch)
        {
            // 信頼度に基づくスムージング係数の調整
            float confidenceFactor = Mathf.Clamp01(confidence);
            effectiveSmoothingFactor = smoothingFactor * (0.5f + confidenceFactor * 0.5f);
            
            // 変化量に基づく調整（急激な変化は遅く）
            if (smoothedPitch > 0f)
            {
                float changeRatio = Mathf.Abs(pitch - smoothedPitch) / Mathf.Max(smoothedPitch, 1f);
                if (changeRatio > 0.2f) // 20%以上の変化
                {
                    effectiveSmoothingFactor *= 0.5f; // より遅く
                }
            }
        }
        
        // スムージング処理
        if (hasValidPitch)
        {
            if (pitch > 0f && isIntentionalVoice)
            {
                // 意図的な音声の場合のみ更新
                smoothedPitch = Mathf.Lerp(smoothedPitch, pitch, effectiveSmoothingFactor);
                lastAcceptedPitch = pitch;
                lastAcceptedTime = currentTime;
            }
            else if (pitch > 0f && !isIntentionalVoice)
            {
                // 意図的でない場合は、前回の値を維持（スムージングしない）
            }
            else
            {
                // ピッチが検知されない場合は、スムージングされた値を維持
                smoothedPitch = smoothedPitch; // 前の値を維持
            }
        }
        else
        {
            if (pitch > 0f && isIntentionalVoice)
            {
                smoothedPitch = pitch;
                hasValidPitch = true;
                lastAcceptedPitch = pitch;
                lastAcceptedTime = currentTime;
            }
            else
            {
                smoothedPitch = 0f;
            }
        }
        
        lastDetectedPitch = smoothedPitch;
        
        OnPitchDetected?.Invoke(smoothedPitch);
    }
    
    bool IsIntentionalVoice(float pitch, float confidence, float volume, float currentTime)
    {
        if (pitch <= 0f || confidence < minConfidence)
        {
            return false;
        }
        
        // 音量の安定性チェック（動的閾値を使用）
        float dynamicThreshold = VoiceCalibrator.MinVolume > 0f 
            ? VoiceCalibrator.MinVolume * volumeDetectionRatio 
            : 0.01f;
        float stabilityThreshold = dynamicThreshold * intentionalVoiceVolumeStability;
        if (volume < stabilityThreshold)
        {
            return false;
        }
        
        // 最小持続時間のチェック
        if (recentDetections.Count == 0)
        {
            return false;
        }
        
        // 最近の検出結果が最小持続時間以上あるかチェック
        float oldestTime = recentDetections.Peek().time;
        float duration = currentTime - oldestTime;
        
        if (duration < intentionalVoiceMinDuration)
        {
            return false;
        }
        
        // ピッチの安定性チェック
        float minPitch = float.MaxValue;
        float maxPitch = float.MinValue;
        int validCount = 0;
        
        foreach (var detection in recentDetections)
        {
            if (detection.pitch > 0f && detection.confidence >= minConfidence)
            {
                minPitch = Mathf.Min(minPitch, detection.pitch);
                maxPitch = Mathf.Max(maxPitch, detection.pitch);
                validCount++;
            }
        }
        
        if (validCount < 2)
        {
            return false;
        }
        
        float pitchRange = maxPitch - minPitch;
        if (pitchRange > intentionalVoiceStabilityThreshold)
        {
            return false;
        }
        
        // 現在のピッチが最近の検出結果と一致しているかチェック
        float averagePitch = 0f;
        int count = 0;
        foreach (var detection in recentDetections)
        {
            if (detection.pitch > 0f && detection.confidence >= minConfidence)
            {
                averagePitch += detection.pitch;
                count++;
            }
        }
        
        if (count > 0)
        {
            averagePitch /= count;
            float pitchDifference = Mathf.Abs(pitch - averagePitch);
            
            if (pitchDifference > intentionalVoiceStabilityThreshold)
            {
                return false;
            }
        }
        
        return true;
    }
    
    // インスペクター用のテストボタン
    [ContextMenu("Test Left Movement")]
    void TestLeftMovement()
    {
        // テスト用の動的閾値を計算
        float testThreshold = VoiceCalibrator.MinVolume > 0f 
            ? VoiceCalibrator.MinVolume * volumeDetectionRatio * 2f 
            : 0.02f;
        ProcessPitch(leftPitch, 1.0f, testThreshold); // 高い信頼度と音量でテスト
    }
    
    [ContextMenu("Test Center Movement")]
    void TestCenterMovement()
    {
        // テスト用の動的閾値を計算
        float testThreshold = VoiceCalibrator.MinVolume > 0f 
            ? VoiceCalibrator.MinVolume * volumeDetectionRatio * 2f 
            : 0.02f;
        ProcessPitch(centerPitch, 1.0f, testThreshold); // 高い信頼度と音量でテスト
    }
    
    [ContextMenu("Test Right Movement")]
    void TestRightMovement()
    {
        // テスト用の動的閾値を計算
        float testThreshold = VoiceCalibrator.MinVolume > 0f 
            ? VoiceCalibrator.MinVolume * volumeDetectionRatio * 2f 
            : 0.02f;
        ProcessPitch(rightPitch, 1.0f, testThreshold); // 高い信頼度と音量でテスト
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
        }
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            // GameManagerの設定を直接更新（publicフィールドがある場合）
        }
    }
    
    // 設定値変更時にUIを更新するメソッド
    public void UpdateUISettings()
    {
        InitializeUIComponents();
    }
}
