using UnityEngine;

public class PitchAnalyzer : MonoBehaviour
{
    [Header("Pitch Settings")]
    public float pitchSensitivity = 1.0f;
    public float minFrequency = 80f;  // 最低周波数
    public float maxFrequency = 1000f; // 最高周波数
    
    [Header("Movement Pitch Settings")]
    public float leftPitch = 200f;    // 左移動用のピッチ
    public float centerPitch = 400f;  // 中央（停止）用のピッチ
    public float rightPitch = 600f;   // 右移動用のピッチ
    
    [Header("Test Settings")]
    public bool useTestMode = true;  // テストモードを使用
    public float testPitch = 400f;   // テスト用の固定音程
    
    private VoiceDetector voiceDetector;
    private float[] fftBuffer;
    public float lastDetectedPitch = 0f;
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
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
                float pitch = CalculatePitch(samples);
                ProcessPitch(pitch);
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
    
    float CalculatePitch(float[] samples)
    {
        // より確実な音程検知のため、複数の方法を試す
        
        // 方法1: ゼロクロッシング法
        int zeroCrossings = CountZeroCrossings(samples);
        float frequency1 = (zeroCrossings * voiceDetector.sampleRate) / (2f * samples.Length);
        
        // 方法2: 振幅の最大値の位置から推定
        float frequency2 = EstimateFrequencyFromAmplitude(samples);
        
        // 方法3: 簡易的な周波数推定
        float frequency3 = EstimateFrequencySimple(samples);
        
        Debug.Log($"Freq1 (ZeroCross): {frequency1:F1} Hz, Freq2 (Amplitude): {frequency2:F1} Hz, Freq3 (Simple): {frequency3:F1} Hz");
        
        // 最も信頼性の高い値を選択
        float finalFrequency = frequency1;
        
        // 範囲内の周波数のみ返す
        if (finalFrequency >= minFrequency && finalFrequency <= maxFrequency)
        {
            return finalFrequency;
        }
        
        return 0f;
    }
    
    float EstimateFrequencyFromAmplitude(float[] samples)
    {
        // 振幅の最大値の位置から周波数を推定
        float maxAmplitude = 0;
        int maxIndex = 0;
        
        for (int i = 0; i < samples.Length; i++)
        {
            if (Mathf.Abs(samples[i]) > maxAmplitude)
            {
                maxAmplitude = Mathf.Abs(samples[i]);
                maxIndex = i;
            }
        }
        
        // 簡易的な周波数推定
        return (float)maxIndex * voiceDetector.sampleRate / samples.Length;
    }
    
    float EstimateFrequencySimple(float[] samples)
    {
        // 音声の基本周波数を簡易的に推定
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }
        
        float averageAmplitude = sum / samples.Length;
        
        // 振幅に基づいて周波数を推定（簡易版）
        if (averageAmplitude > 0.1f)
        {
            return 300f + (averageAmplitude * 500f); // 300-800Hzの範囲
        }
        
        return 0f;
    }
    
    int CountZeroCrossings(float[] samples)
    {
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
            lastDetectedPitch = pitch;  // 最後に検知された音程を保存
            Debug.Log($"Pitch: {pitch:F1} Hz");
            
            // イベント発火
            OnPitchDetected?.Invoke(pitch);
        }
        else
        {
            Debug.Log("Pitch: No pitch detected");
        }
    }
    
    public System.Action<float> OnPitchDetected;
}
