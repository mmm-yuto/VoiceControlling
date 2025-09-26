using UnityEngine;

public class VoiceController : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebugLog = true;
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    
    void Start()
    {
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        
        // イベント購読（デバッグ用）
        if (enableDebugLog)
        {
            if (volumeAnalyzer != null)
                volumeAnalyzer.OnVolumeDetected += OnVolumeDetected;
            if (pitchAnalyzer != null)
                pitchAnalyzer.OnPitchDetected += OnPitchDetected;
        }
    }
    
    void OnVolumeDetected(float volume)
    {
        // デバッグ用のログ出力のみ
        Debug.Log($"Volume Detected: {volume:F3}");
    }
    
    void OnPitchDetected(float pitch)
    {
        // デバッグ用のログ出力のみ
        Debug.Log($"Pitch Detected: {pitch:F1} Hz");
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= OnVolumeDetected;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected -= OnPitchDetected;
    }
}
