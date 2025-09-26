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
    
    [Header("Display Settings")]
    public float maxVolume = 1f;
    public float minPitch = 0f;
    public float maxPitch = 1000f;
    
    // 動的な最大値設定（後方互換性のため残す）
    public void SetMaxValues(float newMaxVolume, float newMaxPitch)
    {
        SetMaxVolume(newMaxVolume);
        SetPitchRange(0f, newMaxPitch);
    }
    
    // 音量の最大値設定
    public void SetMaxVolume(float newMaxVolume)
    {
        maxVolume = newMaxVolume;
        
        if (volumeSlider != null)
        {
            volumeSlider.maxValue = maxVolume;
        }
        
        Debug.Log($"Max volume updated: {maxVolume:F3}");
    }
    
    // ピッチの範囲設定
    public void SetPitchRange(float newMinPitch, float newMaxPitch)
    {
        minPitch = newMinPitch;
        maxPitch = newMaxPitch;
        
        if (pitchSlider != null)
        {
            pitchSlider.minValue = minPitch;
            pitchSlider.maxValue = maxPitch;
        }
        
        Debug.Log($"Pitch range updated: {minPitch:F1} - {maxPitch:F1} Hz");
    }
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    private ImprovedPitchAnalyzer improvedPitchAnalyzer;
    private FixedPitchAnalyzer fixedPitchAnalyzer;
    
    void Start()
    {
        // 音声分析コンポーネントを取得
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        fixedPitchAnalyzer = FindObjectOfType<FixedPitchAnalyzer>();
        
        // イベント購読
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected += UpdateVolumeDisplay;
        
        // FixedPitchAnalyzerを最優先で使用
        if (fixedPitchAnalyzer != null)
        {
            fixedPitchAnalyzer.OnPitchDetected += UpdatePitchDisplay;
            Debug.Log("Using FixedPitchAnalyzer for pitch detection");
        }
        else if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.OnPitchDetected += UpdatePitchDisplay;
            Debug.Log("Using ImprovedPitchAnalyzer for pitch detection");
        }
        else if (pitchAnalyzer != null)
        {
            pitchAnalyzer.OnPitchDetected += UpdatePitchDisplay;
            Debug.Log("Using PitchAnalyzer for pitch detection");
        }
        else
        {
            Debug.LogWarning("No pitch analyzer found!");
        }
        
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
            pitchSlider.minValue = minPitch;
            pitchSlider.maxValue = maxPitch;
            pitchSlider.value = minPitch;
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
            pitchSlider.value = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= UpdateVolumeDisplay;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected -= UpdatePitchDisplay;
        if (improvedPitchAnalyzer != null)
            improvedPitchAnalyzer.OnPitchDetected -= UpdatePitchDisplay;
        if (fixedPitchAnalyzer != null)
            fixedPitchAnalyzer.OnPitchDetected -= UpdatePitchDisplay;
    }
}
