using UnityEngine;

public class VolumeAnalyzer : MonoBehaviour
{
    [Header("Volume Settings")]
    public float volumeSensitivity = 1.0f;
    public float volumeThreshold = 0.01f;
    
    private VoiceDetector voiceDetector;
    
    /// <summary>
    /// 直近フレームで計算された生の音量値（しきい値判定前）。
    /// 他コンポーネントから現在の音量を参照するために使用する。
    /// </summary>
    public float CurrentVolume { get; private set; } = 0f;
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
        if (voiceDetector == null)
        {
            Debug.LogError("VolumeAnalyzer: VoiceDetectorが見つかりません。シーンにVoiceDetectorコンポーネントを追加してください。");
        }
    }
    
    void Update()
    {
        // voiceDetectorがnullでないかチェック
        if (voiceDetector == null)
        {
            return;
        }
        
        float[] samples = voiceDetector.GetAudioSamples();
        if (samples != null)
        {
            float volume = CalculateVolume(samples);
            CurrentVolume = Mathf.Max(0f, volume);
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
        // 音量が閾値以上の場合のみ追加処理
        if (volume > volumeThreshold)
        {
            // 音量に基づく処理をここに実装
            // イベント発火
            OnVolumeDetected?.Invoke(volume);
        }
    }
    
    public System.Action<float> OnVolumeDetected;
}
