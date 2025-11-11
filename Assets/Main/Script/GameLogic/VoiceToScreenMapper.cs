using UnityEngine;

/// <summary>
/// 音声値（音量・ピッチ）を画面座標に変換するシステム
/// VoiceScatterPlotの座標変換ロジックを参考に実装
/// </summary>
public class VoiceToScreenMapper : MonoBehaviour
{
    [Header("Ranges")]
    [Tooltip("ボリュームの最大値（VoiceDisplayと同期）")]
    public float maxVolume = 1f;
    
    [Tooltip("ピッチの最小値（ImprovedPitchAnalyzerと同期）")]
    public float minPitch = 80f;
    
    [Tooltip("ピッチの最大値（ImprovedPitchAnalyzerと同期）")]
    public float maxPitch = 1000f;
    
    [Header("Mapping Options")]
    [Tooltip("有声時、Y軸をスライダーと同じ(minPitch..maxPitch)で正規化する（原点センタリング無効）")]
    public bool matchSliderYAxis = true;
    
    // キャリブレーション平均（原点）
    private float zeroVolume = 0f;
    private float zeroPitch = 80f;
    
    void Start()
    {
        // 原点（0点）をキャリブ平均で初期化
        zeroVolume = Mathf.Max(0f, VoiceCalibrator.LastAverageVolume);
        zeroPitch = VoiceCalibrator.LastAveragePitch > 0f ? VoiceCalibrator.LastAveragePitch : minPitch;
        
        // キャリブ完了通知が来たら原点を更新
        VoiceCalibrator.OnCalibrationAveragesUpdated += OnCalibrationAveragesUpdated;
        
        // 範囲を同期
        SyncRanges();
    }
    
    void OnDestroy()
    {
        VoiceCalibrator.OnCalibrationAveragesUpdated -= OnCalibrationAveragesUpdated;
    }
    
    void SyncRanges()
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
    
    void OnCalibrationAveragesUpdated(float avgVol, float avgPitch)
    {
        zeroVolume = Mathf.Max(0f, avgVol);
        zeroPitch = avgPitch > 0f ? avgPitch : zeroPitch;
    }
    
    /// <summary>
    /// 音量を0-1に正規化（VoiceScatterPlot.MapVolumeTo01のロジックを参考）
    /// </summary>
    float MapVolumeTo01(float volume)
    {
        // 原点(キャリブ平均)を中心に非対称レンジでマッピング
        float leftExtent = Mathf.Max(0.0001f, zeroVolume - 0f);
        float rightExtent = Mathf.Max(0.0001f, maxVolume - zeroVolume);
        
        if (volume >= zeroVolume)
        {
            float frac = (volume - zeroVolume) / rightExtent;
            return 0.5f + 0.5f * Mathf.Clamp01(frac);
        }
        else
        {
            float frac = (zeroVolume - volume) / leftExtent;
            return 0.5f - 0.5f * Mathf.Clamp01(frac);
        }
    }
    
    /// <summary>
    /// ピッチを0-1に正規化（VoiceScatterPlot.MapPitchTo01のロジックを参考）
    /// </summary>
    float MapPitchTo01(float pitch)
    {
        float downExtent = Mathf.Max(0.0001f, zeroPitch - minPitch);
        float upExtent = Mathf.Max(0.0001f, maxPitch - zeroPitch);
        
        if (matchSliderYAxis)
        {
            return Mathf.InverseLerp(minPitch, Mathf.Max(minPitch + 0.0001f, maxPitch), pitch);
        }
        
        if (pitch >= zeroPitch)
        {
            float frac = (pitch - zeroPitch) / upExtent;
            return 0.5f + 0.5f * Mathf.Clamp01(frac);
        }
        else
        {
            float frac = (zeroPitch - pitch) / downExtent;
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
        // Screen座標系：左下が(0,0)、右上が(Screen.width, Screen.height)
        float screenX = vol01 * Screen.width;
        float screenY = pit01 * Screen.height;
        
        return new Vector2(screenX, screenY);
    }
}

