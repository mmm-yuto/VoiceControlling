using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoiceDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI volumeText;
    public TextMeshProUGUI pitchText;
    public Slider volumeSlider;
    public Slider pitchSlider;
    public Image volumeBar;
    public Image pitchBar;
    
    [Header("Display Settings")]
    public Color lowVolumeColor = Color.green;
    public Color highVolumeColor = Color.red;
    public Color lowPitchColor = Color.blue;
    public Color highPitchColor = Color.yellow;
    public float maxVolume = 1f;
    public float maxPitch = 1000f;
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    
    void Start()
    {
        // 音声分析コンポーネントを取得
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        
        // イベント購読
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected += UpdateVolumeDisplay;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected += UpdatePitchDisplay;
        
        // UI初期化
        InitializeUI();
    }
    
    void InitializeUI()
    {
        // スライダーの初期化
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = maxVolume;
            volumeSlider.value = 0f;
        }
        
        if (pitchSlider != null)
        {
            pitchSlider.minValue = 0f;
            pitchSlider.maxValue = maxPitch;
            pitchSlider.value = 0f;
        }
        
        // テキストの初期化
        if (volumeText != null)
            volumeText.text = "Volume: 0.000";
        if (pitchText != null)
            pitchText.text = "Pitch: 0.0 Hz";
    }
    
    void UpdateVolumeDisplay(float volume)
    {
        // テキスト表示
        if (volumeText != null)
        {
            volumeText.text = $"Volume: {volume:F3}";
        }
        
        // スライダー更新
        if (volumeSlider != null)
        {
            volumeSlider.value = Mathf.Clamp(volume, 0f, maxVolume);
        }
        
        // バー表示更新
        if (volumeBar != null)
        {
            float normalizedVolume = Mathf.Clamp01(volume / maxVolume);
            volumeBar.fillAmount = normalizedVolume;
            volumeBar.color = Color.Lerp(lowVolumeColor, highVolumeColor, normalizedVolume);
        }
    }
    
    void UpdatePitchDisplay(float pitch)
    {
        // テキスト表示
        if (pitchText != null)
        {
            pitchText.text = $"Pitch: {pitch:F1} Hz";
        }
        
        // スライダー更新
        if (pitchSlider != null)
        {
            pitchSlider.value = Mathf.Clamp(pitch, 0f, maxPitch);
        }
        
        // バー表示更新
        if (pitchBar != null)
        {
            float normalizedPitch = Mathf.Clamp01(pitch / maxPitch);
            pitchBar.fillAmount = normalizedPitch;
            pitchBar.color = Color.Lerp(lowPitchColor, highPitchColor, normalizedPitch);
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= UpdateVolumeDisplay;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected -= UpdatePitchDisplay;
    }
}
