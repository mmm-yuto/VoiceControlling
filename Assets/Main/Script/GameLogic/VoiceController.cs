using UnityEngine;

public class VoiceController : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebugLog = true;
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    private ImprovedPitchAnalyzer improvedPitchAnalyzer;
    private FixedPitchAnalyzer fixedPitchAnalyzer;
    
    void Start()
    {
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        fixedPitchAnalyzer = FindObjectOfType<FixedPitchAnalyzer>();
        
        // イベント購読（デバッグ用）
        if (enableDebugLog)
        {
            if (volumeAnalyzer != null)
                volumeAnalyzer.OnVolumeDetected += OnVolumeDetected;
            
            // FixedPitchAnalyzerを最優先で使用
            if (fixedPitchAnalyzer != null)
                fixedPitchAnalyzer.OnPitchDetected += OnPitchDetected;
            else if (improvedPitchAnalyzer != null)
                improvedPitchAnalyzer.OnPitchDetected += OnPitchDetected;
            else if (pitchAnalyzer != null)
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
        if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected -= OnPitchDetected;
        if (fixedPitchAnalyzer != null)
            fixedPitchAnalyzer.OnPitchDetected -= OnPitchDetected;
    }
}
