using UnityEngine;
using UnityEngine.UI;

public class VoiceVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    public Image volumeBar;
    public Image pitchBar;
    public Color lowVolumeColor = Color.green;
    public Color highVolumeColor = Color.red;
    public Color lowPitchColor = Color.blue;
    public Color highPitchColor = Color.yellow;
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    
    void Start()
    {
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        
        // イベント購読
        volumeAnalyzer.OnVolumeDetected += UpdateVolumeVisualization;
        pitchAnalyzer.OnPitchDetected += UpdatePitchVisualization;
    }
    
    void UpdateVolumeVisualization(float volume)
    {
        if (volumeBar != null)
        {
            volumeBar.fillAmount = Mathf.Clamp01(volume);
            
            // 音量に応じて色を変更
            volumeBar.color = Color.Lerp(lowVolumeColor, highVolumeColor, volume);
        }
    }
    
    void UpdatePitchVisualization(float pitch)
    {
        if (pitchBar != null)
        {
            float normalizedPitch = Mathf.Clamp01((pitch - 200f) / 800f);
            pitchBar.fillAmount = normalizedPitch;
            
            // 音程に応じて色を変更
            pitchBar.color = Color.Lerp(lowPitchColor, highPitchColor, normalizedPitch);
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= UpdateVolumeVisualization;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected -= UpdatePitchVisualization;
    }
}
