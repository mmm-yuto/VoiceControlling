using UnityEngine;

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
    
    // グラフの中心位置（カリブレーション結果から計算）
    private float centerVolume = 0.5f;
    private float centerPitch = 500f;
    
    void Start()
    {
        // カリブレーション結果から初期化
        if (VoiceCalibrator.MinVolume > 0f || VoiceCalibrator.MaxVolume > 0f)
        {
            UpdateCalibrationRanges(VoiceCalibrator.MinVolume, VoiceCalibrator.MaxVolume, 
                                    VoiceCalibrator.MinPitch, VoiceCalibrator.MaxPitch);
        }
        else
        {
            // デフォルト値で初期化
            centerVolume = 0.5f;
            centerPitch = (minPitch + maxPitch) / 2f;
        }
        
        // カリブレーション完了通知を購読
        VoiceCalibrator.OnCalibrationCompleted += OnCalibrationCompleted;
        
        // 範囲を同期
        SyncRanges();
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
        
        Debug.Log($"VoiceToScreenMapper: Ranges updated - Volume: {minVolume:F3} - {maxVolume:F3}, Pitch: {minPitch:F1} - {maxPitch:F1} Hz");
        Debug.Log($"VoiceToScreenMapper: Center - Volume: {centerVolume:F3}, Pitch: {centerPitch:F1} Hz");
    }
    
    void OnCalibrationCompleted(float minVol, float maxVol, float minPit, float maxPit)
    {
        UpdateCalibrationRanges(minVol, maxVol, minPit, maxPit);
    }
    
    /// <summary>
    /// 音量を0-1に正規化（カリブレーション結果の範囲を使用）
    /// </summary>
    float MapVolumeTo01(float volume)
    {
        // カリブレーション結果の範囲で正規化
        float volumeRange = maxVolume - minVolume;
        if (volumeRange <= 0.0001f) return 0.5f;
        
        // 中心位置を基準にマッピング
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
    
    /// <summary>
    /// ピッチを0-1に正規化（カリブレーション結果の範囲を使用）
    /// </summary>
    float MapPitchTo01(float pitch)
    {
        if (matchSliderYAxis)
        {
            return Mathf.InverseLerp(minPitch, Mathf.Max(minPitch + 0.0001f, maxPitch), pitch);
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

