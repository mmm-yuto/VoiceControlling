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
    [Tooltip("検出状態を表示するテキスト（オプション）")]
    [SerializeField] private TMPro.TextMeshProUGUI statusText;
    
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
        
        UpdateStatusText("Neutral Sound未検出（デフォルト値を使用）");
    }
    
    /// <summary>
    /// Neutral Soundの検出を開始
    /// </summary>
    public void StartDetection()
    {
        if (IsDetecting)
        {
            Debug.LogWarning("NeutralSoundDetector: 既に検出中です");
            return;
        }
        
        IsDetecting = true;
        IsDetected = false;
        pitchSamples.Clear();
        volumeSamples.Clear();
        currentSampleCount = 0;
        
        OnDetectionStarted?.Invoke();
        UpdateStatusText("Neutral Sound検出中... 声を出してください");
        
        Debug.Log("NeutralSoundDetector: 検出を開始しました");
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
            UpdateStatusText("Neutral Sound検出失敗（サンプル不足）");
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
        UpdateStatusText("Neutral Sound検出をキャンセルしました");
    }
    
    void Update()
    {
        if (!IsDetecting)
        {
            return;
        }
        
        // 音声データを取得
        float currentVolume = GetCurrentVolume();
        float currentPitch = GetCurrentPitch();
        
        // 有効なサンプルのみを記録
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
            Debug.LogWarning("NeutralSoundDetector: サンプルが不足しています");
            return;
        }
        
        // 平均値を計算
        float averagePitch = CalculateAverage(pitchSamples);
        float averageVolume = CalculateAverage(volumeSamples);
        
        // 基準値を設定
        NeutralPitch = averagePitch;
        NeutralVolume = averageVolume;
        IsDetected = true;
        
        // イベント発火
        OnNeutralSoundDetected?.Invoke(NeutralPitch, NeutralVolume);
        
        // セーブ（将来の拡張用）
        SaveNeutralSoundData();
        
        UpdateStatusText($"Neutral Sound検出完了 - ピッチ: {NeutralPitch:F1}Hz, ボリューム: {NeutralVolume:F3}");
        Debug.Log($"NeutralSoundDetector: Neutral Sound検出完了 - ピッチ: {NeutralPitch:F1}Hz, ボリューム: {NeutralVolume:F3}");
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
    /// 手動でNeutral Soundを設定
    /// </summary>
    public void SetNeutralSound(float pitch, float volume)
    {
        NeutralPitch = pitch;
        NeutralVolume = volume;
        IsDetected = true;
        
        SaveNeutralSoundData();
        UpdateStatusText($"Neutral Sound設定 - ピッチ: {NeutralPitch:F1}Hz, ボリューム: {NeutralVolume:F3}");
        
        Debug.Log($"NeutralSoundDetector: Neutral Soundを手動設定 - ピッチ: {NeutralPitch:F1}Hz, ボリューム: {NeutralVolume:F3}");
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
        UpdateStatusText("Neutral Soundをリセットしました（デフォルト値）");
        
        Debug.Log("NeutralSoundDetector: Neutral Soundをリセットしました");
    }
    
    /// <summary>
    /// 状態テキストを更新
    /// </summary>
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
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
                UpdateStatusText($"Neutral Sound読み込み完了 - ピッチ: {NeutralPitch:F1}Hz, ボリューム: {NeutralVolume:F3}");
            }
        }
    }
}

