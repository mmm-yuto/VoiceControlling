using UnityEngine;

/// <summary>
/// 塗りバトルゲームのマネージャー
/// 音声検出 → 座標変換 → 塗り処理の流れを実装
/// 既存のVoiceControllerやGameManagerのパターンを参考
/// </summary>
public class PaintBattleGameManager : MonoBehaviour
{
    [Header("Voice Detection References")]
    [Tooltip("音量分析コンポーネント（Inspectorで接続）")]
    public VolumeAnalyzer volumeAnalyzer;
    
    [Tooltip("ピッチ分析コンポーネント（Inspectorで接続）")]
    public ImprovedPitchAnalyzer improvedPitchAnalyzer;
    
    [Header("Debug Mode (Optional)")]
    [Tooltip("デバッグモード（VoiceDebugSimulatorを使用する場合）")]
    public VoiceDebugSimulator voiceDebugSimulator;
    
    [Header("Game Logic References")]
    [Tooltip("座標変換コンポーネント（Inspectorで接続）")]
    public VoiceToScreenMapper voiceToScreenMapper;
    
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    public PaintCanvas paintCanvas;
    
    [Header("Game Settings")]
    [Tooltip("プレイヤーID（Phase 1では1で固定）")]
    public int playerId = 1;
    
    [Tooltip("塗り速度の倍率")]
    [Range(0.1f, 5f)]
    public float paintSpeedMultiplier = 1f;
    
    [Tooltip("無音判定の音量閾値")]
    [Range(0f, 0.1f)]
    public float silenceVolumeThreshold = 0.01f;
    
    // 内部状態
    private float latestVolume = 0f;
    private float latestPitch = 0f;
    private bool isGameActive = true;
    
    void Start()
    {
        // 参照が設定されていない場合は自動検索（推奨はInspector接続）
        if (volumeAnalyzer == null)
        {
            volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
            if (volumeAnalyzer == null)
            {
                Debug.LogError("PaintBattleGameManager: VolumeAnalyzerが見つかりません");
            }
        }
        
        if (improvedPitchAnalyzer == null)
        {
            improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
            if (improvedPitchAnalyzer == null)
            {
                Debug.LogError("PaintBattleGameManager: ImprovedPitchAnalyzerが見つかりません");
            }
        }
        
        if (voiceToScreenMapper == null)
        {
            voiceToScreenMapper = FindObjectOfType<VoiceToScreenMapper>();
            if (voiceToScreenMapper == null)
            {
                Debug.LogError("PaintBattleGameManager: VoiceToScreenMapperが見つかりません");
            }
        }
        
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
            if (paintCanvas == null)
            {
                Debug.LogError("PaintBattleGameManager: PaintCanvasが見つかりません");
            }
        }
        
        // VoiceDebugSimulatorの自動検索（Inspectorで接続されていない場合）
        if (voiceDebugSimulator == null)
        {
            voiceDebugSimulator = FindObjectOfType<VoiceDebugSimulator>();
            if (voiceDebugSimulator != null)
            {
                Debug.Log("PaintBattleGameManager: VoiceDebugSimulatorを自動検索しました。Inspectorで接続することを推奨します。");
            }
        }
        
        // イベント購読
        // デバッグモード時はVoiceDebugSimulatorのイベントを優先
        SubscribeToEvents();
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
            Debug.Log("PaintBattleGameManager: デバッグモードを使用します（VoiceDebugSimulator）");
        }
        else
        {
            // デバッグモードが有効になっているが、接続されていない場合の警告
            if (voiceDebugSimulator != null && !voiceDebugSimulator.enableDebugMode)
            {
                Debug.LogWarning("PaintBattleGameManager: VoiceDebugSimulatorが接続されていますが、Enable Debug ModeがOFFです。通常モードを使用します。");
            }
            else if (voiceDebugSimulator == null)
            {
                // voiceDebugSimulatorがnullの場合は通常モード（警告なし）
            }
            
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
            
            // 現在デバッグモードを使用しているかチェック（より確実な方法）
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
        
        if (!isGameActive)
        {
            return;
        }
        
        // デバッグモード時はマウス位置を直接使用
        if (voiceDebugSimulator != null && voiceDebugSimulator.enableDebugMode && Input.GetMouseButton(0))
        {
            if (paintCanvas != null)
            {
                // マウス位置を直接画面座標として使用
                Vector2 mouseScreenPos = Input.mousePosition;
                
                // 塗り処理（デバッグモード時は固定の強度を使用）
                float intensity = 0.5f * paintSpeedMultiplier; // デバッグモード時の固定強度
                paintCanvas.PaintAt(mouseScreenPos, playerId, intensity, Color.white);
            }
            return; // デバッグモード時は通常の処理をスキップ
        }
        
        // 通常モード：無音判定
        float threshold = improvedPitchAnalyzer != null 
            ? improvedPitchAnalyzer.volumeThreshold 
            : silenceVolumeThreshold;
        
        bool isSilent = (latestVolume < threshold) || (latestPitch <= 0f);
        
        if (isSilent)
        {
            // 無音時は塗らない（実装手順書の推奨：Option A）
            return;
        }
        
        // 座標変換
        if (voiceToScreenMapper != null && paintCanvas != null)
        {
            Vector2 screenPos = voiceToScreenMapper.MapVoiceToScreen(latestVolume, latestPitch);
            
            // 塗り処理
            float intensity = latestVolume * paintSpeedMultiplier;
            paintCanvas.PaintAt(screenPos, playerId, intensity, Color.white);
        }
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
    /// ゲームを開始/停止
    /// </summary>
    public void SetGameActive(bool active)
    {
        isGameActive = active;
    }
}

