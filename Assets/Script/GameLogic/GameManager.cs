using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider volumeSlider;
    public Slider pitchSlider;
    public Text volumeText;
    public Text pitchText;
    
    [Header("Game Settings")]
    public float maxVolume = 1f;
    public float maxPitch = 1000f;
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    
    void Start()
    {
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        
        // UI初期化
        InitializeUI();
        
        // イベント購読
        volumeAnalyzer.OnVolumeDetected += UpdateVolumeUI;
        pitchAnalyzer.OnPitchDetected += UpdatePitchUI;
    }
    
    void InitializeUI()
    {
        if (volumeSlider != null)
        {
            volumeSlider.maxValue = maxVolume;
            volumeSlider.value = 0;
        }
        
        if (pitchSlider != null)
        {
            pitchSlider.maxValue = maxPitch;
            pitchSlider.value = 0;
        }
    }
    
    void UpdateVolumeUI(float volume)
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = Mathf.Clamp(volume, 0, maxVolume);
        }
        
        if (volumeText != null)
        {
            volumeText.text = $"Volume: {volume:F3}";
        }
    }
    
    void UpdatePitchUI(float pitch)
    {
        if (pitchSlider != null)
        {
            pitchSlider.value = Mathf.Clamp(pitch, 0, maxPitch);
        }
        
        if (pitchText != null)
        {
            pitchText.text = $"Pitch: {pitch:F1} Hz";
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= UpdateVolumeUI;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected -= UpdatePitchUI;
    }
}
