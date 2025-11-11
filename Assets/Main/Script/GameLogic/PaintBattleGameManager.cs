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
        
        // イベント購読
        if (volumeAnalyzer != null)
        {
            volumeAnalyzer.OnVolumeDetected += OnVolumeDetected;
        }
        
        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.OnPitchDetected += OnPitchDetected;
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
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
        if (!isGameActive)
        {
            return;
        }
        
        // 無音判定
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
            paintCanvas.PaintAt(screenPos, playerId, intensity);
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

