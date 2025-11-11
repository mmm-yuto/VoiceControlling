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
            
            // 画面座標を0-1に正規化
            float normalizedX = mouseScreenPos.x / Screen.width;
            float normalizedY = mouseScreenPos.y / Screen.height;
            
            // VoiceScatterPlotのplotArea内の座標に変換（利用可能な場合）
            Vector2 plotAreaPos;
            if (scatterPlot != null && scatterPlot.plotArea != null)
            {
                plotAreaPos = ConvertMouseToPlotArea(mouseScreenPos);
            }
            else
            {
                // scatterPlotがない場合は画面全体を0-1として使用
                plotAreaPos = new Vector2(normalizedX, normalizedY);
            }
            
            // plotArea内の位置（0-1正規化）からピッチ・ボリュームに逆変換
            float volume = ConvertPlotPositionToVolume(plotAreaPos);
            float pitch = ConvertPlotPositionToPitch(plotAreaPos);
            
            // デバッグログ（開発時のみ）
            #if UNITY_EDITOR
            if (Time.frameCount % 30 == 0) // 30フレームごとにログ出力（パフォーマンス考慮）
            {
                Debug.Log($"VoiceDebugSimulator: Volume={volume:F3}, Pitch={pitch:F1}, MousePos=({normalizedX:F2}, {normalizedY:F2})");
            }
            #endif
            
            // イベントを発火（既存のシステムと同じ）
            if (OnVolumeDetected != null)
            {
                OnVolumeDetected.Invoke(volume);
            }
            else
            {
                Debug.LogWarning("VoiceDebugSimulator: OnVolumeDetectedイベントが購読されていません。PaintBattleGameManagerのVoice Debug Simulatorフィールドを確認してください。");
            }
            
            if (OnPitchDetected != null)
            {
                OnPitchDetected.Invoke(pitch);
            }
            else
            {
                Debug.LogWarning("VoiceDebugSimulator: OnPitchDetectedイベントが購読されていません。PaintBattleGameManagerのVoice Debug Simulatorフィールドを確認してください。");
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
        // scatterPlotがない場合は、画面全体を0-1として使用
        float x01;
        if (scatterPlot != null)
        {
            x01 = scatterPlot.axes == VoiceScatterPlot.AxisMapping.VolumeX_PitchY 
                ? plotPos.x 
                : plotPos.y;
        }
        else
        {
            // scatterPlotがない場合は、X座標を使用
            x01 = plotPos.x;
        }
        
        // 0-1から実際のボリューム値に変換
        float leftExtent = Mathf.Max(0.0001f, zeroVolume - 0f);
        float rightExtent = Mathf.Max(0.0001f, maxVolume - zeroVolume);
        
        float volume;
        if (x01 >= 0.5f)
        {
            // 右側（原点より大きい）
            float frac = (x01 - 0.5f) * 2f; // 0.5-1.0 -> 0-1
            volume = zeroVolume + frac * rightExtent;
        }
        else
        {
            // 左側（原点より小さい）
            float frac = (0.5f - x01) * 2f; // 0-0.5 -> 1-0
            volume = zeroVolume - frac * leftExtent;
        }
        
        // デバッグモード時は最小値を保証（無音判定を回避）
        float minVolumeForDebug = 0.05f; // 無音判定の閾値より大きい値
        return Mathf.Max(volume, minVolumeForDebug);
    }
    
    float ConvertPlotPositionToPitch(Vector2 plotPos)
    {
        // scatterPlotがない場合は、画面全体を0-1として使用
        float y01;
        bool useMatchSliderYAxis = false;
        
        if (scatterPlot != null)
        {
            y01 = scatterPlot.axes == VoiceScatterPlot.AxisMapping.VolumeX_PitchY 
                ? plotPos.y 
                : plotPos.x;
            useMatchSliderYAxis = scatterPlot.matchSliderYAxis;
        }
        else
        {
            // scatterPlotがない場合は、Y座標を使用
            y01 = plotPos.y;
            useMatchSliderYAxis = true; // デフォルトでスライダーと同じ正規化を使用
        }
        
        float pitch;
        if (useMatchSliderYAxis)
        {
            // スライダーと同じ正規化（原点センタリングなし）
            pitch = Mathf.Lerp(minPitch, maxPitch, y01);
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
                pitch = zeroPitch + frac * upExtent;
            }
            else
            {
                // 下側（原点より小さい）
                float frac = (0.5f - y01) * 2f; // 0-0.5 -> 1-0
                pitch = zeroPitch - frac * downExtent;
            }
        }
        
        // デバッグモード時は最小値を保証（無音判定を回避）
        return Mathf.Max(pitch, minPitch + 10f); // 最小ピッチより少し大きい値
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

