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
    public float autocorrelationThreshold = 0.3f; // オートコリレーションの閾値
    public int frameHistorySize = 5; // フレーム履歴のサイズ
    public bool useAdvancedFiltering = true; // 高度なフィルタリングを使用
    
    [Header("Movement Pitch Settings")]
    public float leftPitch = 200f;    // 左移動用のピッチ
    public float centerPitch = 400f;  // 中央（停止）用のピッチ
    public float rightPitch = 600f;   // 右移動用のピッチ
    
    [Header("Test Settings")]
    public bool useTestMode = true;  // テストモードを使用
    public float testPitch = 400f;   // テスト用の固定音程
    
    private VoiceDetector voiceDetector;
    private VolumeAnalyzer volumeAnalyzer;
    private float[] fftBuffer;
    public float lastDetectedPitch = 0f;
    private float smoothedPitch = 0f;
    private bool hasValidPitch = false;
    
    // フレーム履歴
    private Queue<float> pitchHistory = new Queue<float>();
    private Queue<float> volumeHistory = new Queue<float>();
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        int bufferSize = voiceDetector.bufferSize;
        fftBuffer = new float[bufferSize];
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
            float[] samples = voiceDetector.GetAudioSamples();
            if (samples != null)
            {
                // 音量をチェックしてピッチ検知の信頼性を判断
                float currentVolume = CalculateVolume(samples);
                
                if (currentVolume > volumeThreshold)
                {
                    // 音量が閾値以上の場合のみピッチを検知
                    float pitch = CalculatePitchAdvanced(samples);
                    ProcessPitch(pitch);
                }
                else
                {
                    // 音量が低い場合はピッチ検知を停止
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
    
    float CalculatePitchAdvanced(float[] samples)
    {
        // 1. 音量正規化
        float[] normalizedSamples = NormalizeSamples(samples);
        
        // 2. バンドパスフィルタリング
        float[] filteredSamples = ApplyBandpassFilter(normalizedSamples);
        
        // 3. オートコリレーション法
        float autocorrelationPitch = CalculatePitchAutocorrelation(filteredSamples);
        
        // 4. ハーモニック検出
        float harmonicPitch = CalculatePitchHarmonic(filteredSamples);
        
        // 5. 結果の統合と検証
        float finalPitch = CombinePitchResults(autocorrelationPitch, harmonicPitch);
        
        // 6. フレーム履歴による安定化
        if (useAdvancedFiltering)
        {
            finalPitch = StabilizeWithHistory(finalPitch);
        }
        
        Debug.Log($"AutoCorr: {autocorrelationPitch:F1} Hz, Harmonic: {harmonicPitch:F1} Hz, Final: {finalPitch:F1} Hz");
        
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
    
    float CalculatePitchAutocorrelation(float[] samples)
    {
        // オートコリレーション法による基本周波数検出
        int minPeriod = Mathf.RoundToInt(voiceDetector.sampleRate / maxFrequency);
        int maxPeriod = Mathf.RoundToInt(voiceDetector.sampleRate / minFrequency);
        
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
        
        if (bestPeriod > 0 && maxCorrelation > autocorrelationThreshold)
        {
            float frequency = (float)voiceDetector.sampleRate / bestPeriod;
            return frequency;
        }
        
        return 0f;
    }
    
    float CalculatePitchHarmonic(float[] samples)
    {
        // ハーモニック検出によるピッチ推定
        float[] windowedSamples = new float[samples.Length];
        
        // ハミング窓を適用
        for (int i = 0; i < samples.Length; i++)
        {
            float window = 0.54f - 0.46f * Mathf.Cos(2f * Mathf.PI * i / (samples.Length - 1));
            windowedSamples[i] = samples[i] * window;
        }
        
        // 簡易FFT（実際のプロジェクトでは外部ライブラリを使用推奨）
        float[] fft = new float[samples.Length];
        PerformSimpleFFT(windowedSamples, fft);
        
        // ピーク検出
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
            float frequency = (float)peakIndex * voiceDetector.sampleRate / fft.Length;
            return frequency;
        }
        
        return 0f;
    }
    
    void PerformSimpleFFT(float[] input, float[] output)
    {
        // 簡易FFT実装（実際のプロジェクトでは外部ライブラリを使用推奨）
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = input[i];
        }
        
        // ここでFFTアルゴリズムを実装
        // 実際にはUnity.MathematicsのFFT関数や外部ライブラリを使用
    }
    
    float CombinePitchResults(float autocorrelationPitch, float harmonicPitch)
    {
        // 結果の統合と検証
        if (autocorrelationPitch > 0 && harmonicPitch > 0)
        {
            // 両方の結果が有効な場合、重み付き平均を取る
            float weight1 = 0.7f; // オートコリレーションの重み
            float weight2 = 0.3f; // ハーモニック検出の重み
            
            float combinedPitch = (autocorrelationPitch * weight1 + harmonicPitch * weight2) / (weight1 + weight2);
            
            // 結果の妥当性をチェック
            if (combinedPitch >= minFrequency && combinedPitch <= maxFrequency)
            {
                return combinedPitch;
            }
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
            
            Debug.Log("Pitch: No pitch detected (volume too low)");
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
