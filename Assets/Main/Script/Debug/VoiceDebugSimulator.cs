using UnityEngine;

/// <summary>
/// デバッグモード：マウス操作で音声入力をシミュレート
/// 開発・テスト効率を向上させるため、声を出さずにテストできる
/// </summary>
public class VoiceDebugSimulator : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("デバッグモードを有効にする")]
    public bool enableDebugMode = false;
    
    [Header("References")]
    [Tooltip("VoiceScatterPlotの参照（マーカー位置を更新するため）")]
    public VoiceScatterPlot scatterPlot;
    
    [Header("Volume/Pitch Ranges")]
    [Tooltip("ボリュームの最大値（VoiceDisplayと同期）")]
    public float maxVolume = 1f;
    
    [Tooltip("ピッチの最小値（ImprovedPitchAnalyzerと同期）")]
    public float minPitch = 80f;
    
    [Tooltip("ピッチの最大値（ImprovedPitchAnalyzerと同期）")]
    public float maxPitch = 1000f;
    
    [Header("Debug Volume")]
    [Range(0f, 1f)]
    [Tooltip("デバッグモード時の音量（マウス位置から計算されるが、手動調整も可能）")]
    public float debugVolume = 0.5f;
    
    private bool isMouseDown = false;
    private float zeroVolume = 0f;
    private float zeroPitch = 80f;
    
    // 既存のイベントと同じシグネチャで発火
    public System.Action<float> OnVolumeDetected;
    public System.Action<float> OnPitchDetected;
    
    void Start()
    {
        // キャリブレーション平均を取得
        zeroVolume = Mathf.Max(0f, VoiceCalibrator.LastAverageVolume);
        zeroPitch = VoiceCalibrator.LastAveragePitch > 0f ? VoiceCalibrator.LastAveragePitch : minPitch;
        
        // キャリブレーション更新を購読
        VoiceCalibrator.OnCalibrationAveragesUpdated += OnCalibrationAveragesUpdated;
        
        // 範囲を同期
        SyncRanges();
    }
    
    void OnDestroy()
    {
        VoiceCalibrator.OnCalibrationAveragesUpdated -= OnCalibrationAveragesUpdated;
    }
    
    void Update()
    {
        if (!enableDebugMode) return;
        
        // マウスの左クリックを検出
        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMouseDown = false;
            // クリック解除時は無音状態をシミュレート（音量0、ピッチ0）
            OnVolumeDetected?.Invoke(0f);
            OnPitchDetected?.Invoke(0f);
            return;
        }
        
        if (isMouseDown)
        {
            // マウス位置を取得
            Vector2 mouseScreenPos = Input.mousePosition;
            
            // VoiceScatterPlotのplotArea内の座標に変換
            if (scatterPlot != null && scatterPlot.plotArea != null)
            {
                Vector2 plotAreaPos = ConvertMouseToPlotArea(mouseScreenPos);
                
                // plotArea内の位置（0-1正規化）からピッチ・ボリュームに逆変換
                float volume = ConvertPlotPositionToVolume(plotAreaPos);
                float pitch = ConvertPlotPositionToPitch(plotAreaPos);
                
                // イベントを発火（既存のシステムと同じ）
                OnVolumeDetected?.Invoke(volume);
                OnPitchDetected?.Invoke(pitch);
            }
        }
    }
    
    Vector2 ConvertMouseToPlotArea(Vector2 mouseScreenPos)
    {
        if (scatterPlot == null || scatterPlot.plotArea == null)
            return Vector2.zero;
        
        RectTransform plotArea = scatterPlot.plotArea;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            plotArea, mouseScreenPos, null, out Vector2 localPoint);
        
        // plotAreaのサイズで正規化（0-1）
        Vector2 size = plotArea.rect.size;
        Vector2 normalizedPos = new Vector2(
            (localPoint.x + size.x * 0.5f) / size.x,
            (localPoint.y + size.y * 0.5f) / size.y
        );
        
        return normalizedPos;
    }
    
    float ConvertPlotPositionToVolume(Vector2 plotPos)
    {
        // VoiceScatterPlot.MapVolumeTo01の逆変換
        float x01 = scatterPlot.axes == VoiceScatterPlot.AxisMapping.VolumeX_PitchY 
            ? plotPos.x 
            : plotPos.y;
        
        // 0-1から実際のボリューム値に変換
        float leftExtent = Mathf.Max(0.0001f, zeroVolume - 0f);
        float rightExtent = Mathf.Max(0.0001f, maxVolume - zeroVolume);
        
        if (x01 >= 0.5f)
        {
            // 右側（原点より大きい）
            float frac = (x01 - 0.5f) * 2f; // 0.5-1.0 -> 0-1
            return zeroVolume + frac * rightExtent;
        }
        else
        {
            // 左側（原点より小さい）
            float frac = (0.5f - x01) * 2f; // 0-0.5 -> 1-0
            return zeroVolume - frac * leftExtent;
        }
    }
    
    float ConvertPlotPositionToPitch(Vector2 plotPos)
    {
        // VoiceScatterPlot.MapPitchTo01の逆変換
        float y01 = scatterPlot.axes == VoiceScatterPlot.AxisMapping.VolumeX_PitchY 
            ? plotPos.y 
            : plotPos.x;
        
        // matchSliderYAxisの設定を確認
        if (scatterPlot.matchSliderYAxis)
        {
            // スライダーと同じ正規化（原点センタリングなし）
            return Mathf.Lerp(minPitch, maxPitch, y01);
        }
        else
        {
            // 原点センタリングあり
            float downExtent = Mathf.Max(0.0001f, zeroPitch - minPitch);
            float upExtent = Mathf.Max(0.0001f, maxPitch - zeroPitch);
            
            if (y01 >= 0.5f)
            {
                // 上側（原点より大きい）
                float frac = (y01 - 0.5f) * 2f; // 0.5-1.0 -> 0-1
                return zeroPitch + frac * upExtent;
            }
            else
            {
                // 下側（原点より小さい）
                float frac = (0.5f - y01) * 2f; // 0-0.5 -> 1-0
                return zeroPitch - frac * downExtent;
            }
        }
    }
    
    void OnCalibrationAveragesUpdated(float avgVol, float avgPitch)
    {
        zeroVolume = Mathf.Max(0f, avgVol);
        zeroPitch = avgPitch > 0f ? avgPitch : zeroPitch;
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
}

