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
        if (samples != null && samples.Length > 0)
        {
            float volume = CalculateVolume(samples);
            ProcessVolume(volume);
        }
        else
        {
            // サンプルが取得できない場合は0を発火
            ProcessVolume(0f);
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
        // 常にイベントを発火（閾値チェックは呼び出し側で行う）
        // これにより、PaintBattleGameManagerで常に最新の音量を取得できる
        OnVolumeDetected?.Invoke(volume);
        
        // デバッグログ（閾値以上の場合のみ）
        if (volume > volumeThreshold)
        {
            Debug.Log($"VolumeAnalyzer - Volume detected: {volume:F6} (threshold: {volumeThreshold:F6})");
        }
    }
    
    public System.Action<float> OnVolumeDetected;
}
