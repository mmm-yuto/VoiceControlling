using UnityEngine;

/// <summary>
/// 音声入力処理を共通化するコンポーネント
/// VolumeAnalyzer、ImprovedPitchAnalyzerからデータを取得し、座標変換まで行う
/// </summary>
public class VoiceInputHandler : MonoBehaviour
{
    [Header("Voice Detection References")]
    [Tooltip("音量分析コンポーネント（Inspectorで接続）")]
    [SerializeField] private VolumeAnalyzer volumeAnalyzer;
    
    [Tooltip("ピッチ分析コンポーネント（Inspectorで接続）")]
    [SerializeField] private ImprovedPitchAnalyzer improvedPitchAnalyzer;
    
    [Header("Debug Mode (Optional)")]
    [Tooltip("デバッグモード（VoiceDebugSimulatorを使用する場合）")]
    [SerializeField] private VoiceDebugSimulator voiceDebugSimulator;
    
    [Header("Mapping Reference")]
    [Tooltip("座標変換コンポーネント（Inspectorで接続）")]
    [SerializeField] private VoiceToScreenMapper voiceToScreenMapper;
    
    [Header("Smoothing Settings")]
    [Tooltip("音量のスムージング係数（0 = スムージングなし、1 = 完全に固定）")]
    [Range(0f, 1f)]
    public float volumeSmoothing = 0.3f;
    
    [Tooltip("ピッチのスムージング係数（0 = スムージングなし、1 = 完全に固定）")]
    [Range(0f, 1f)]
    public float pitchSmoothing = 0.2f;
    
    [Tooltip("画面座標のスムージング係数（0 = スムージングなし、1 = 完全に固定）")]
    [Range(0f, 1f)]
    public float positionSmoothing = 0.25f;
    
    [Header("Dead Zone Settings")]
    [Tooltip("音量のデッドゾーン（この値以下の変動は無視）")]
    [Range(0f, 0.1f)]
    public float volumeDeadZone = 0.005f;
    
    [Tooltip("ピッチのデッドゾーン（Hz、この値以下の変動は無視）")]
    [Range(0f, 50f)]
    public float pitchDeadZone = 5f;
    
    [Tooltip("画面座標のデッドゾーン（ピクセル、この値以下の変動は無視）")]
    [Range(0f, 50f)]
    public float positionDeadZone = 10f;
    
    // 現在の音声データ
    public float CurrentVolume { get; private set; } = 0f;
    public float CurrentPitch { get; private set; } = 0f;
    public Vector2 CurrentScreenPosition { get; private set; } = Vector2.zero;
    public bool IsSilent { get; private set; } = true;
    
    // イベント
    public event System.Action<float, float> OnVoiceInputUpdated; // volume, pitch
    public event System.Action<Vector2> OnScreenPositionUpdated; // screenPosition
    
    // 内部状態
    private float latestVolume = 0f;
    private float latestPitch = 0f;
    
    // スムージング用の変数
    private float smoothedVolume = 0f;
    private float smoothedPitch = 0f;
    private Vector2 smoothedScreenPosition = Vector2.zero;
    
    void Start()
    {
        // 参照が設定されていない場合は自動検索（推奨はInspector接続）
        if (volumeAnalyzer == null)
        {
            volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
            if (volumeAnalyzer == null)
            {
                Debug.LogWarning("VoiceInputHandler: VolumeAnalyzerが見つかりません");
            }
        }
        
        if (improvedPitchAnalyzer == null)
        {
            improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
            if (improvedPitchAnalyzer == null)
            {
                Debug.LogWarning("VoiceInputHandler: ImprovedPitchAnalyzerが見つかりません");
            }
        }
        
        if (voiceToScreenMapper == null)
        {
            voiceToScreenMapper = FindObjectOfType<VoiceToScreenMapper>();
            if (voiceToScreenMapper == null)
            {
                Debug.LogWarning("VoiceInputHandler: VoiceToScreenMapperが見つかりません");
            }
        }
        
        // VoiceDebugSimulatorの自動検索（Inspectorで接続されていない場合）
        if (voiceDebugSimulator == null)
        {
            voiceDebugSimulator = FindObjectOfType<VoiceDebugSimulator>();
        }
        
        // イベント購読
        SubscribeToEvents();
        
        // スムージング用の変数を初期化
        smoothedVolume = 0f;
        smoothedPitch = 0f;
        smoothedScreenPosition = voiceToScreenMapper != null ? voiceToScreenMapper.MapToCenter() : Vector2.zero;
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    void SubscribeToEvents()
    {
        // 既存の購読を解除してから再購読
        UnsubscribeFromEvents();
        
        // デバッグモード時はVoiceDebugSimulatorのイベントを優先
        if (voiceDebugSimulator != null && voiceDebugSimulator.enableDebugMode)
        {
            voiceDebugSimulator.OnVolumeDetected += OnVolumeDetected;
            voiceDebugSimulator.OnPitchDetected += OnPitchDetected;
            Debug.Log("VoiceInputHandler: デバッグモードを使用します（VoiceDebugSimulator）");
        }
        else
        {
            // 通常モード：既存のイベント購読
            if (volumeAnalyzer != null)
            {
                volumeAnalyzer.OnVolumeDetected += OnVolumeDetected;
            }
            
            if (improvedPitchAnalyzer != null)
            {
                improvedPitchAnalyzer.OnPitchDetected += OnPitchDetected;
            }
        }
    }
    
    void UnsubscribeFromEvents()
    {
        // イベント購読解除
        if (voiceDebugSimulator != null)
        {
            voiceDebugSimulator.OnVolumeDetected -= OnVolumeDetected;
            voiceDebugSimulator.OnPitchDetected -= OnPitchDetected;
        }
        
        if (volumeAnalyzer != null)
        {
            volumeAnalyzer.OnVolumeDetected -= OnVolumeDetected;
        }
        
        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.OnPitchDetected -= OnPitchDetected;
        }
    }
    
    void Update()
    {
        // 実行中にデバッグモードが変更された場合に対応
        if (voiceDebugSimulator != null)
        {
            bool shouldUseDebug = voiceDebugSimulator.enableDebugMode;
            
            // 現在デバッグモードを使用しているかチェック
            bool currentlyUsingDebug = false;
            if (voiceDebugSimulator.OnVolumeDetected != null)
            {
                var invocationList = voiceDebugSimulator.OnVolumeDetected.GetInvocationList();
                foreach (var handler in invocationList)
                {
                    if (handler.Target == this && handler.Method.Name == "OnVolumeDetected")
                    {
                        currentlyUsingDebug = true;
                        break;
                    }
                }
            }
            
            // デバッグモードの状態が変わった場合は再購読
            if (shouldUseDebug != currentlyUsingDebug)
            {
                SubscribeToEvents();
            }
        }
        
        // 無音判定（動的閾値計算：MinVolume * volumeDetectionRatio）
        float threshold;
        if (improvedPitchAnalyzer != null)
        {
            threshold = VoiceCalibrator.MinVolume > 0f 
                ? VoiceCalibrator.MinVolume * improvedPitchAnalyzer.volumeDetectionRatio 
                : 0.01f; // MinVolumeが0の場合はデフォルト値
        }
        else
        {
            // フォールバック：デフォルト比率0.75を使用
            threshold = VoiceCalibrator.MinVolume > 0f 
                ? VoiceCalibrator.MinVolume * 0.75f 
                : 0.01f;
        }
        
        IsSilent = (latestVolume < threshold) || (latestPitch <= 0f);
        
        // 無音時の処理
        if (IsSilent)
        {
            // 無音時は徐々にリセット
            smoothedVolume = Mathf.Lerp(smoothedVolume, 0f, volumeSmoothing * 2f);
            smoothedPitch = Mathf.Lerp(smoothedPitch, 0f, pitchSmoothing * 2f);
            // 画面座標は前の位置を維持（中心に戻さない）
        }
        else
        {
            // 音量のスムージング
            float volumeChange = Mathf.Abs(latestVolume - smoothedVolume);
            if (volumeChange > volumeDeadZone)
            {
                smoothedVolume = Mathf.Lerp(smoothedVolume, latestVolume, volumeSmoothing);
            }
            
            // ピッチのスムージング（ImprovedPitchAnalyzerで既にスムージングされているが、追加で適用可能）
            float pitchChange = Mathf.Abs(latestPitch - smoothedPitch);
            if (pitchChange > pitchDeadZone)
            {
                smoothedPitch = Mathf.Lerp(smoothedPitch, latestPitch, pitchSmoothing);
            }
        }
        
        // 座標変換（スムージングされた値を使用）
        if (voiceToScreenMapper != null)
        {
            Vector2 targetPosition = voiceToScreenMapper.MapVoiceToScreen(smoothedVolume, smoothedPitch);
            
            // 画面座標のスムージング
            float positionChange = Vector2.Distance(targetPosition, smoothedScreenPosition);
            if (positionChange > positionDeadZone)
            {
                smoothedScreenPosition = Vector2.Lerp(smoothedScreenPosition, targetPosition, positionSmoothing);
            }
            
            CurrentScreenPosition = smoothedScreenPosition;
            OnScreenPositionUpdated?.Invoke(smoothedScreenPosition);
        }
        
        // 現在の値を更新（スムージングされた値を使用）
        CurrentVolume = smoothedVolume;
        CurrentPitch = smoothedPitch;
        
        // イベント発火（スムージングされた値を使用）
        OnVoiceInputUpdated?.Invoke(smoothedVolume, smoothedPitch);
    }
    
    void OnVolumeDetected(float volume)
    {
        latestVolume = volume;
    }
    
    void OnPitchDetected(float pitch)
    {
        latestPitch = pitch;
    }
    
    /// <summary>
    /// デバッグモード時はマウス位置を直接使用（PaintBattleGameManager用）
    /// </summary>
    public Vector2 GetDebugMousePosition()
    {
        if (voiceDebugSimulator != null && voiceDebugSimulator.enableDebugMode && Input.GetMouseButton(0))
        {
            return Input.mousePosition;
        }
        return Vector2.zero;
    }
    
    /// <summary>
    /// デバッグモードが有効かどうか
    /// </summary>
    public bool IsDebugModeActive()
    {
        return voiceDebugSimulator != null && voiceDebugSimulator.enableDebugMode && Input.GetMouseButton(0);
    }
}

