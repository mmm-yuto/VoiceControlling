using UnityEngine;
using TMPro;

/// <summary>
/// 音声値（音量・ピッチ）を画面座標に変換するシステム
/// VoiceScatterPlotの座標変換ロジックを参考に実装
/// </summary>
public class VoiceToScreenMapper : MonoBehaviour
{
    [Header("Ranges")]
    [Tooltip("ボリュームの最小値（カリブレーション結果から設定）")]
    public float minVolume = 0f;
    
    [Tooltip("ボリュームの最大値（VoiceDisplayと同期）")]
    public float maxVolume = 1f;
    
    [Tooltip("ピッチの最小値（ImprovedPitchAnalyzerと同期）")]
    public float minPitch = 80f;
    
    [Tooltip("ピッチの最大値（ImprovedPitchAnalyzerと同期）")]
    public float maxPitch = 1000f;
    
    [Header("Target Mapping Area")]
    [Tooltip("音声入力がマッピングされるUIパネルのRectTransform。未設定の場合はスクリーン全体。")]
    public RectTransform targetRectTransform;
    
    [Header("Mapping Options")]
    [Tooltip("有声時、Y軸をスライダーと同じ(minPitch..maxPitch)で正規化する（原点センタリング無効）")]
    public bool matchSliderYAxis = true;
    
    [Tooltip("Use logarithmic scale for volume mapping (more sensitive to small volume changes)")]
    public bool useLogarithmicVolumeScale = true;
    
    [Header("Range Display Labels")]
    [Tooltip("Minimum volume label (displayed at bottom-left of graph)")]
    public TextMeshProUGUI minVolumeLabel;
    
    [Tooltip("Maximum volume label (displayed at bottom-right of graph)")]
    public TextMeshProUGUI maxVolumeLabel;
    
    [Tooltip("Minimum pitch label (displayed at top-left of graph)")]
    public TextMeshProUGUI minPitchLabel;
    
    [Tooltip("Maximum pitch label (displayed at top-right of graph)")]
    public TextMeshProUGUI maxPitchLabel;
    
    // グラフの中心位置（カリブレーション結果から計算）
    private float centerVolume = 0.5f;
    private float centerPitch = 500f;
    
    void Start()
    {
        // カリブレーション結果から初期化（初期カリブレーション値も含む）
        // VoiceCalibratorのStart()が先に実行されることを前提とする
        UpdateCalibrationRanges(VoiceCalibrator.MinVolume, VoiceCalibrator.MaxVolume, 
                                VoiceCalibrator.MinPitch, VoiceCalibrator.MaxPitch);
        
        // カリブレーション完了通知を購読
        VoiceCalibrator.OnCalibrationCompleted += OnCalibrationCompleted;
        
        // 範囲を同期
        SyncRanges();
        
        // 初期ラベルを更新
        UpdateRangeLabels();
    }
    
    void OnDestroy()
    {
        VoiceCalibrator.OnCalibrationCompleted -= OnCalibrationCompleted;
    }
    
    public void SyncRanges()
    {
        // VoiceDisplayから範囲を取得
        VoiceDisplay voiceDisplay = FindObjectOfType<VoiceDisplay>();
        if (voiceDisplay != null)
        {
            maxVolume = voiceDisplay.maxVolume;
            minPitch = voiceDisplay.minPitch;
            maxPitch = voiceDisplay.maxPitch;
        }
        // ImprovedPitchAnalyzerからも取得可能
        else
        {
            ImprovedPitchAnalyzer pitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
            if (pitchAnalyzer != null)
            {
                minPitch = pitchAnalyzer.minFrequency;
                maxPitch = pitchAnalyzer.maxFrequency;
            }
        }
    }
    
    /// <summary>
    /// カリブレーション結果を適用して範囲を更新
    /// </summary>
    public void UpdateCalibrationRanges(float newMinVolume, float newMaxVolume, float newMinPitch, float newMaxPitch)
    {
        minVolume = newMinVolume;
        maxVolume = newMaxVolume;
        minPitch = newMinPitch;
        maxPitch = newMaxPitch;
        
        // グラフの中心位置を計算
        centerVolume = (minVolume + maxVolume) / 2f;
        centerPitch = (minPitch + maxPitch) / 2f;
        
        // ラベルを更新
        UpdateRangeLabels();
        
        Debug.Log($"VoiceToScreenMapper: Ranges updated - Volume: {minVolume:F3} - {maxVolume:F3}, Pitch: {minPitch:F1} - {maxPitch:F1} Hz");
        Debug.Log($"VoiceToScreenMapper: Center - Volume: {centerVolume:F3}, Pitch: {centerPitch:F1} Hz");
    }
    
    /// <summary>
    /// 範囲ラベルを更新
    /// </summary>
    /// <summary>
    /// 振幅値を音圧レベル（SPL）相当の0-90 dBに変換
    /// 0 dB = 無音、90 dB = 叫び声レベル
    /// </summary>
    float ConvertAmplitudeToSPL(float amplitude)
    {
        // 振幅0を0 dB（無音）、振幅1.0を90 dB（叫び声）にマッピング
        // 対数スケールを使用（0を避けるために最小値を設定）
        float minAmplitude = 0.0001f;
        float clampedAmplitude = Mathf.Max(amplitude, minAmplitude);
        
        // 振幅値を0-90 dBの範囲にマッピング
        // log10(0.0001) ≈ -4, log10(1.0) = 0
        // -4から0の範囲を0から90にマッピング
        float logValue = Mathf.Log10(clampedAmplitude);
        float minLog = Mathf.Log10(minAmplitude);
        float maxLog = 0f; // log10(1.0) = 0
        
        if (logValue <= minLog)
        {
            return 0f; // 無音
        }
        
        // 0-90 dBの範囲に正規化
        float normalized = (logValue - minLog) / (maxLog - minLog);
        return Mathf.Clamp(normalized * 90f, 0f, 90f);
    }
    
    void UpdateRangeLabels()
    {
        if (minVolumeLabel != null)
        {
            float minDb = ConvertAmplitudeToSPL(minVolume);
            minVolumeLabel.text = $"{minDb:F0} dB";
        }
        
        if (maxVolumeLabel != null)
        {
            float maxDb = ConvertAmplitudeToSPL(maxVolume);
            maxVolumeLabel.text = $"{maxDb:F0} dB";
        }
        
        if (minPitchLabel != null)
        {
            minPitchLabel.text = $"{minPitch:F1} Hz";
        }
        
        if (maxPitchLabel != null)
        {
            maxPitchLabel.text = $"{maxPitch:F1} Hz";
        }
    }
    
    void OnCalibrationCompleted(float minVol, float maxVol, float minPit, float maxPit)
    {
        UpdateCalibrationRanges(minVol, maxVol, minPit, maxPit);
    }
    
    /// <summary>
    /// 音量を0-1に正規化（カリブレーション結果の範囲を使用、対数スケール対応）
    /// </summary>
    float MapVolumeTo01(float volume)
    {
        // カリブレーション結果の範囲で正規化
        float volumeRange = maxVolume - minVolume;
        if (volumeRange <= 0.0001f) return 0.5f;
        
        if (useLogarithmicVolumeScale)
        {
            // 対数スケールを使用（人間の聴覚に近い感度）
            // まず0-1に正規化
            float normalizedVolume = Mathf.Clamp01((volume - minVolume) / volumeRange);
            
            // 対数変換（0を避けるために最小値を0.001に設定）
            // normalizedVolumeが0の場合は0.001に、1の場合は1.0になるように変換
            float minLogValue = 0.001f;
            float logInput = normalizedVolume * (1f - minLogValue) + minLogValue;
            float logVolume = Mathf.Log10(logInput) / Mathf.Log10(1f / minLogValue);
            
            // 中心位置も対数変換
            float centerNormalized = Mathf.Clamp01((centerVolume - minVolume) / volumeRange);
            float centerLogInput = centerNormalized * (1f - minLogValue) + minLogValue;
            float centerLog = Mathf.Log10(centerLogInput) / Mathf.Log10(1f / minLogValue);
            
            // 対数空間で中心位置を基準にマッピング
            if (logVolume >= centerLog)
            {
                float rightExtent = 1f - centerLog;
                if (rightExtent <= 0.0001f) return 0.5f;
                float frac = (logVolume - centerLog) / rightExtent;
                return 0.5f + 0.5f * Mathf.Clamp01(frac);
            }
            else
            {
                float leftExtent = centerLog;
                if (leftExtent <= 0.0001f) return 0.5f;
                float frac = (centerLog - logVolume) / leftExtent;
                return 0.5f - 0.5f * Mathf.Clamp01(frac);
            }
        }
        else
        {
            // 線形スケール（従来の方法）
            float leftExtent = Mathf.Max(0.0001f, centerVolume - minVolume);
            float rightExtent = Mathf.Max(0.0001f, maxVolume - centerVolume);
            
            if (volume >= centerVolume)
            {
                float frac = (volume - centerVolume) / rightExtent;
                return 0.5f + 0.5f * Mathf.Clamp01(frac);
            }
            else
            {
                float frac = (centerVolume - volume) / leftExtent;
                return 0.5f - 0.5f * Mathf.Clamp01(frac);
            }
        }
    }
    
    /// <summary>
    /// ピッチを0-1に正規化（カリブレーション結果の範囲を使用）
    /// </summary>
    float MapPitchTo01(float pitch)
    {
        if (matchSliderYAxis)
        {
            // 範囲外のピッチはクランプして、minPitchの位置（0.0）またはmaxPitchの位置（1.0）にマッピング
            return Mathf.Clamp01(Mathf.InverseLerp(minPitch, Mathf.Max(minPitch + 0.0001f, maxPitch), pitch));
        }
        
        // 中心位置を基準にマッピング
        float downExtent = Mathf.Max(0.0001f, centerPitch - minPitch);
        float upExtent = Mathf.Max(0.0001f, maxPitch - centerPitch);
        
        if (pitch >= centerPitch)
        {
            float frac = (pitch - centerPitch) / upExtent;
            return 0.5f + 0.5f * Mathf.Clamp01(frac);
        }
        else
        {
            float frac = (centerPitch - pitch) / downExtent;
            return 0.5f - 0.5f * Mathf.Clamp01(frac);
        }
    }
    
    /// <summary>
    /// 音声値（音量・ピッチ）を画面座標に変換
    /// </summary>
    /// <param name="volume">音量（0-maxVolume）</param>
    /// <param name="pitch">ピッチ（minPitch-maxPitch Hz）</param>
    /// <returns>画面座標（Screen座標系、左下が(0,0)）</returns>
    public Vector2 MapVoiceToScreen(float volume, float pitch)
    {
        // 音量・ピッチを0-1に正規化
        float vol01 = MapVolumeTo01(volume);
        float pit01 = MapPitchTo01(pitch);
        
        // 0-1を画面座標に変換
        if (targetRectTransform != null)
        {
            // パネルが設定されている場合は、パネルの範囲内にマッピング
            Vector3[] corners = new Vector3[4];
            targetRectTransform.GetWorldCorners(corners);
            
            Camera mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindObjectOfType<Camera>();
            
            if (mainCamera != null)
            {
                Vector2 minScreen = mainCamera.WorldToScreenPoint(corners[0]);
                Vector2 maxScreen = mainCamera.WorldToScreenPoint(corners[2]);
                
                float screenX = Mathf.Lerp(minScreen.x, maxScreen.x, vol01);
                float screenY = Mathf.Lerp(minScreen.y, maxScreen.y, pit01);
                
                return new Vector2(screenX, screenY);
            }
        }
        
        // パネルが設定されていない、またはカメラが見つからない場合は画面全体
        // Screen座標系：左下が(0,0)、右上が(Screen.width, Screen.height)
        return new Vector2(vol01 * Screen.width, pit01 * Screen.height);
    }
    
    /// <summary>
    /// マッピング領域の中心座標を取得（カリブレーション結果の中心位置に対応）
    /// </summary>
    /// <returns>画面座標（Screen座標系）</returns>
    public Vector2 MapToCenter()
    {
        if (targetRectTransform != null)
        {
            // パネルの中心位置を取得
            Vector3[] corners = new Vector3[4];
            targetRectTransform.GetWorldCorners(corners);
            
            Camera mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindObjectOfType<Camera>();
            
            if (mainCamera != null)
            {
                Vector2 minScreen = mainCamera.WorldToScreenPoint(corners[0]);
                Vector2 maxScreen = mainCamera.WorldToScreenPoint(corners[2]);
                
                float screenX = Mathf.Lerp(minScreen.x, maxScreen.x, 0.5f);
                float screenY = Mathf.Lerp(minScreen.y, maxScreen.y, 0.5f);
                
                return new Vector2(screenX, screenY);
            }
        }
        
        // パネルが設定されていない、またはカメラが見つからない場合は画面の中心
        // 中心はカリブレーション結果の中心位置（centerVolume, centerPitch）に対応
        // 0.5, 0.5の位置が中心
        return new Vector2(0.5f * Screen.width, 0.5f * Screen.height);
    }
}

