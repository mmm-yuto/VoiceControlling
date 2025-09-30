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
    public float minPitch = 80f;
    public float maxPitch = 1000f;
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    private ImprovedPitchAnalyzer improvedPitchAnalyzer;
    
    void Start()
    {
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        
        // UI初期化
        InitializeUI();
        
        // イベント購読
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected += UpdateVolumeUI;
        
        // ImprovedPitchAnalyzerを最優先で使用
        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.OnPitchDetected += UpdatePitchUI;
            Debug.Log("GameManager: Using ImprovedPitchAnalyzer for pitch detection");
        }
        else if (pitchAnalyzer != null)
        {
            pitchAnalyzer.OnPitchDetected += UpdatePitchUI;
            Debug.Log("GameManager: Using PitchAnalyzer for pitch detection");
        }
        else
        {
            Debug.LogWarning("GameManager: No pitch analyzer found!");
        }
    }
    
    void InitializeUI()
    {
        // ImprovedPitchAnalyzerの設定値を取得して適用
        if (improvedPitchAnalyzer != null)
        {
            minPitch = improvedPitchAnalyzer.minFrequency;
            maxPitch = improvedPitchAnalyzer.maxFrequency;
            
            Debug.Log($"GameManager: Using ImprovedPitchAnalyzer settings - Pitch range: {minPitch}-{maxPitch} Hz");
        }
        
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = maxVolume;
            volumeSlider.value = 0;
        }
        
        if (pitchSlider != null)
        {
            pitchSlider.minValue = minPitch;
            pitchSlider.maxValue = maxPitch;
            pitchSlider.value = minPitch;
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
            pitchSlider.value = Mathf.Clamp(pitch, minPitch, maxPitch);
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
        if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected -= UpdatePitchUI;
    }
}
