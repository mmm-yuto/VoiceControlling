using UnityEngine;

public class PitchAnalyzer : MonoBehaviour
{
    [Header("Pitch Settings")]
    public float pitchSensitivity = 1.0f;
    public float minFrequency = 80f;  // 最低周波数
    public float maxFrequency = 1000f; // 最高周波数
    
    private VoiceDetector voiceDetector;
    private float[] fftBuffer;
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
        int bufferSize = voiceDetector.bufferSize;
        fftBuffer = new float[bufferSize];
    }
    
    void Update()
    {
        float[] samples = voiceDetector.GetAudioSamples();
        if (samples != null)
        {
            float pitch = CalculatePitch(samples);
            ProcessPitch(pitch);
        }
    }
    
    float CalculatePitch(float[] samples)
    {
        // 簡易的な音程検知（ゼロクロッシング法）
        int zeroCrossings = CountZeroCrossings(samples);
        
        // ゼロクロッシング数から周波数を推定
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
            Debug.Log($"Pitch: {pitch:F1} Hz");
            
            // イベント発火
            OnPitchDetected?.Invoke(pitch);
        }
    }
    
    public System.Action<float> OnPitchDetected;
}
