using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Neutral Sound（基準音声）を検出・記録するコンポーネント
/// ユーザーの基準となるピッチとボリュームを取得
/// </summary>
public class NeutralSoundDetector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("音量分析コンポーネント（自動検索可能）")]
    [SerializeField] private VolumeAnalyzer volumeAnalyzer;
    
    [Tooltip("ピッチ分析コンポーネント（自動検索可能）")]
    [SerializeField] private ImprovedPitchAnalyzer pitchAnalyzer;
    
    [Header("Detection Settings")]
    [Tooltip("Neutral Sound検出のサンプル数")]
    [SerializeField] private int sampleCount = 30; // 1秒間のサンプル（30fps想定）
    
    [Tooltip("検出中の最小音量閾値")]
    [Range(0f, 1f)]
    [SerializeField] private float minVolumeThreshold = 0.1f;
    
    [Tooltip("検出中の最大音量閾値（これ以上は無視）")]
    [Range(0f, 1f)]
    [SerializeField] private float maxVolumeThreshold = 0.9f;
    
    [Header("Default Values")]
    [Tooltip("デフォルトのNeutral Soundピッチ（Hz）")]
    [SerializeField] private float defaultNeutralPitch = 400f;
    
    [Tooltip("デフォルトのNeutral Soundボリューム")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultNeutralVolume = 0.5f;
    
    [Header("UI")]
    [Tooltip("検出状態を表示するテキスト（検知中のみ使用、オプション）")]
    [SerializeField] private TMPro.TextMeshProUGUI statusText;
    
    [Tooltip("Neutral Soundを常時表示するテキスト（検知完了後に使用、オプション）")]
    [SerializeField] private TMPro.TextMeshProUGUI neutralSoundDisplayText;
    
    [Tooltip("現在検出中のピッチとボリュームを表示するテキスト（常時更新、オプション）")]
    [SerializeField] private TMPro.TextMeshProUGUI currentAudioDisplayText;
    
    // Neutral Soundの基準値
    public float NeutralPitch { get; private set; } = 400f;
    public float NeutralVolume { get; private set; } = 0.5f;
    
    // 検出状態
    public bool IsDetecting { get; private set; } = false;
    public bool IsDetected { get; private set; } = false;
    
    // 検出中のサンプルデータ
    private List<float> pitchSamples = new List<float>();
    private List<float> volumeSamples = new List<float>();
    private int currentSampleCount = 0;
    private int warmupFrames = 0; // 検出開始後のウォームアップフレーム数
    
    // イベント
    public System.Action<float, float> OnNeutralSoundDetected; // (pitch, volume)
    public System.Action OnDetectionStarted;
    public System.Action OnDetectionStopped;
    
    void Start()
    {
        // 参照の自動検索
        if (volumeAnalyzer == null)
        {
            volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        }
        
        if (pitchAnalyzer == null)
        {
            pitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        }
        
        // デフォルト値を設定
        NeutralPitch = defaultNeutralPitch;
        NeutralVolume = defaultNeutralVolume;
        
        // セーブデータを読み込み（将来の拡張用）
        LoadNeutralSoundData();
        
        // 初期状態の表示
        if (IsDetected)
        {
            UpdateNeutralSoundDisplay();
            HideStatusText();
        }
        else
        {
            UpdateStatusText("Neutral Sound not detected (using default values)");
            HideNeutralSoundDisplay();
        }
    }
    
    /// <summary>
    /// Neutral Soundの検出を開始
    /// </summary>
    public void StartDetection()
    {
        if (IsDetecting)
        {
            Debug.LogWarning("NeutralSoundDetector: Already detecting");
            return;
        }
        
        IsDetecting = true;
        IsDetected = false;
        pitchSamples.Clear();
        volumeSamples.Clear();
        currentSampleCount = 0;
        warmupFrames = 3; // 3フレーム待機してから記録開始（ImprovedPitchAnalyzerの状態を安定化）
        
        OnDetectionStarted?.Invoke();
        UpdateStatusTextWithProgress();
        HideNeutralSoundDisplay(); // 検知中は常時表示テキストを非表示
        
        Debug.Log("NeutralSoundDetector: Detection started");
    }
    
    /// <summary>
    /// Neutral Soundの検出を停止
    /// </summary>
    public void StopDetection()
    {
        if (!IsDetecting)
        {
            return;
        }
        
        IsDetecting = false;
        OnDetectionStopped?.Invoke();
        
        // サンプルが十分に集まっている場合は平均値を計算
        if (pitchSamples.Count > 0 && volumeSamples.Count > 0)
        {
            CalculateNeutralSound();
        }
        else
        {
            UpdateStatusText("Neutral Sound detection failed (insufficient samples)");
            HideNeutralSoundDisplay();
        }
    }
    
    /// <summary>
    /// Neutral Soundの検出をキャンセル
    /// </summary>
    public void CancelDetection()
    {
        IsDetecting = false;
        pitchSamples.Clear();
        volumeSamples.Clear();
        currentSampleCount = 0;
        
        OnDetectionStopped?.Invoke();
        UpdateStatusText("Neutral Sound detection cancelled");
        HideNeutralSoundDisplay();
    }
    
    void Update()
    {
        // 現在の音声データを常時更新表示（表示用の値）
        UpdateCurrentAudioDisplay();
        
        if (!IsDetecting)
        {
            return;
        }
        
        // 検知中は進行状況を更新
        UpdateStatusTextWithProgress();
        
        // ウォームアップ期間中は記録しない（ImprovedPitchAnalyzerの状態を安定化）
        if (warmupFrames > 0)
        {
            warmupFrames--;
            return;
        }
        
        // 表示と同じ方法で現在の値を取得（表示用の値と同じ）
        float currentVolume = GetCurrentVolume();
        float currentPitch = GetCurrentPitch();
        
        // 有効なサンプルのみを記録（現在のフレームの値のみ）
        if (IsValidSample(currentVolume, currentPitch))
        {
            pitchSamples.Add(currentPitch);
            volumeSamples.Add(currentVolume);
            currentSampleCount++;
            
            // 十分なサンプルが集まったら自動的に検出を完了
            if (currentSampleCount >= sampleCount)
            {
                CalculateNeutralSound();
                IsDetecting = false;
                OnDetectionStopped?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// 現在の音量を取得
    /// </summary>
    private float GetCurrentVolume()
    {
        if (volumeAnalyzer != null)
        {
            return volumeAnalyzer.CurrentVolume;
        }
        return 0f;
    }
    
    /// <summary>
    /// 現在のピッチを取得
    /// </summary>
    private float GetCurrentPitch()
    {
        if (pitchAnalyzer != null)
        {
            return pitchAnalyzer.lastDetectedPitch;
        }
        return defaultNeutralPitch;
    }
    
    /// <summary>
    /// サンプルが有効かどうかを判定
    /// </summary>
    private bool IsValidSample(float volume, float pitch)
    {
        // 音量が閾値内であることを確認
        if (volume < minVolumeThreshold || volume > maxVolumeThreshold)
        {
            return false;
        }
        
        // ピッチが有効な範囲内であることを確認
        if (pitch <= 0f || pitch < VoiceCalibrator.MinPitch || pitch > VoiceCalibrator.MaxPitch)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Neutral Soundの基準値を計算
    /// </summary>
    private void CalculateNeutralSound()
    {
        if (pitchSamples.Count == 0 || volumeSamples.Count == 0)
        {
            Debug.LogWarning("NeutralSoundDetector: Insufficient samples");
            return;
        }
        
        // 外れ値を除去してから平均値を計算（表示される値に近づけるため）
        float averagePitch = CalculateAverageWithOutlierRemoval(pitchSamples);
        float averageVolume = CalculateAverageWithOutlierRemoval(volumeSamples);
        
        // 基準値を設定
        NeutralPitch = averagePitch;
        NeutralVolume = averageVolume;
        IsDetected = true;
        
        // イベント発火
        OnNeutralSoundDetected?.Invoke(NeutralPitch, NeutralVolume);
        
        // セーブ（将来の拡張用）
        SaveNeutralSoundData();
        
        // 検知完了後はStatusテキストを非表示にして、常時表示テキストを使用
        HideStatusText();
        UpdateNeutralSoundDisplay();
        
        Debug.Log($"NeutralSoundDetector: Neutral Sound detected - Pitch: {NeutralPitch:F1}Hz, Volume: {NeutralVolume:F3} (from {pitchSamples.Count} samples)");
    }
    
    /// <summary>
    /// リストの平均値を計算
    /// </summary>
    private float CalculateAverage(List<float> values)
    {
        if (values.Count == 0)
        {
            return 0f;
        }
        
        float sum = 0f;
        foreach (float value in values)
        {
            sum += value;
        }
        
        return sum / values.Count;
    }
    
    /// <summary>
    /// 外れ値を除去してから平均値を計算
    /// </summary>
    private float CalculateAverageWithOutlierRemoval(List<float> values)
    {
        if (values.Count == 0)
        {
            return 0f;
        }
        
        if (values.Count == 1)
        {
            return values[0];
        }
        
        // まず平均値を計算
        float average = CalculateAverage(values);
        
        // 標準偏差を計算
        float variance = 0f;
        foreach (float value in values)
        {
            variance += Mathf.Pow(value - average, 2);
        }
        variance /= values.Count;
        float standardDeviation = Mathf.Sqrt(variance);
        
        // 標準偏差の2倍以内の値のみを使用して平均を再計算
        float sum = 0f;
        int count = 0;
        foreach (float value in values)
        {
            if (Mathf.Abs(value - average) <= 2f * standardDeviation)
            {
                sum += value;
                count++;
            }
        }
        
        if (count > 0)
        {
            return sum / count;
        }
        
        // すべてが外れ値の場合は元の平均を返す
        return average;
    }
    
    /// <summary>
    /// 手動でNeutral Soundを設定
    /// </summary>
    public void SetNeutralSound(float pitch, float volume)
    {
        NeutralPitch = pitch;
        NeutralVolume = volume;
        IsDetected = true;
        
        SaveNeutralSoundData();
        HideStatusText();
        UpdateNeutralSoundDisplay();
        
        Debug.Log($"NeutralSoundDetector: Neutral Sound manually set - Pitch: {NeutralPitch:F1}Hz, Volume: {NeutralVolume:F3}");
    }
    
    /// <summary>
    /// Neutral Soundデータをリセット
    /// </summary>
    public void ResetNeutralSound()
    {
        NeutralPitch = defaultNeutralPitch;
        NeutralVolume = defaultNeutralVolume;
        IsDetected = false;
        
        SaveNeutralSoundData();
        UpdateStatusText("Neutral Sound reset (default values)");
        HideNeutralSoundDisplay();
        
        Debug.Log("NeutralSoundDetector: Neutral Sound reset");
    }
    
    /// <summary>
    /// 状態テキストを更新
    /// </summary>
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 状態テキストを進行状況付きで更新（検知中のみ）
    /// </summary>
    private void UpdateStatusTextWithProgress()
    {
        if (statusText != null)
        {
            string baseMessage = "Detecting Neutral Sound... Please speak";
            string progressMessage = $" ({currentSampleCount}/{sampleCount})";
            statusText.text = baseMessage + progressMessage;
            statusText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 状態テキストを非表示
    /// </summary>
    private void HideStatusText()
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Neutral Soundの常時表示テキストを更新
    /// </summary>
    private void UpdateNeutralSoundDisplay()
    {
        if (neutralSoundDisplayText != null)
        {
            // 音量をdBに変換（基準値1.0に対する比率）
            float volumeDb = VolumeToDecibel(NeutralVolume);
            
            // 表示形式: Pitch\nXXX Hz\nvolume\nXXX dB
            neutralSoundDisplayText.text = $"Pitch\n{NeutralPitch:F0} Hz\nvolume\n{volumeDb:F0} dB";
            neutralSoundDisplayText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 音量値をdBに変換
    /// </summary>
    /// <param name="volume">音量値（0-1の範囲）</param>
    /// <returns>dB値（0-120の範囲）</returns>
    private float VolumeToDecibel(float volume)
    {
        // 音量が0の場合は0 dB、音量が0.1の場合は120 dB
        // 0から0.1の範囲を0から120dBに線形マッピング
        const float maxVolumeFor120dB = 0.1f;
        
        // 音量を0.1で正規化（0.1以上は120dBにクランプ）
        float normalizedVolume = Mathf.Clamp01(volume / maxVolumeFor120dB);
        
        // 0-120 dBの範囲にマッピング
        float db = normalizedVolume * 120f;
        
        return db;
    }
    
    /// <summary>
    /// Neutral Soundの常時表示テキストを非表示
    /// </summary>
    private void HideNeutralSoundDisplay()
    {
        if (neutralSoundDisplayText != null)
        {
            neutralSoundDisplayText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 現在検出中のピッチとボリュームを表示するテキストを更新
    /// </summary>
    private void UpdateCurrentAudioDisplay()
    {
        if (currentAudioDisplayText == null)
        {
            return;
        }
        
        // 現在の音声データを取得
        float currentVolume = GetCurrentVolume();
        float currentPitch = GetCurrentPitch();
        
        // 音量をdBに変換
        float volumeDb = VolumeToDecibel(currentVolume);
        
        // 表示形式: Pitch\nXXX Hz\nvolume\nXXX dB
        currentAudioDisplayText.text = $"Pitch\n{currentPitch:F0} Hz\nvolume\n{volumeDb:F0} dB";
    }
    
    /// <summary>
    /// Neutral Soundデータをセーブ（将来の拡張用）
    /// </summary>
    private void SaveNeutralSoundData()
    {
        // PlayerPrefsを使用して保存
        PlayerPrefs.SetFloat("NeutralSound_Pitch", NeutralPitch);
        PlayerPrefs.SetFloat("NeutralSound_Volume", NeutralVolume);
        PlayerPrefs.SetInt("NeutralSound_Detected", IsDetected ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Neutral Soundデータをロード
    /// </summary>
    private void LoadNeutralSoundData()
    {
        if (PlayerPrefs.HasKey("NeutralSound_Pitch"))
        {
            NeutralPitch = PlayerPrefs.GetFloat("NeutralSound_Pitch", defaultNeutralPitch);
            NeutralVolume = PlayerPrefs.GetFloat("NeutralSound_Volume", defaultNeutralVolume);
            IsDetected = PlayerPrefs.GetInt("NeutralSound_Detected", 0) == 1;
            
            if (IsDetected)
            {
                UpdateNeutralSoundDisplay();
                HideStatusText();
            }
        }
    }
}

