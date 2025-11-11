using UnityEngine;

public class VolumeAnalyzer : MonoBehaviour
{
    [Header("Volume Settings")]
    public float volumeSensitivity = 1.0f;
    public float volumeThreshold = 0.01f;
    
    private VoiceDetector voiceDetector;
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
    }
    
    void Update()
    {
        float[] samples = voiceDetector.GetAudioSamples();
        if (samples != null)
        {
            float volume = CalculateVolume(samples);
            ProcessVolume(volume);
        }
    }
    
    float CalculateVolume(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / samples.Length) * volumeSensitivity;
    }
    
    void ProcessVolume(float volume)
    {
        // 音量が閾値以上の場合のみ処理
        if (volume > volumeThreshold)
        {
            // 音量に基づく処理をここに実装
            Debug.Log($"Volume: {volume:F3}");
            
            // イベント発火
            OnVolumeDetected?.Invoke(volume);
        }
    }
    
    public System.Action<float> OnVolumeDetected;
}
