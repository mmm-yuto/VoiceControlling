using UnityEngine;

/// <summary>
/// 塗りバトルゲームのマネージャー
/// 音声検出 → 座標変換 → 塗り処理の流れを実装
/// 既存のVoiceControllerやGameManagerのパターンを参考
/// </summary>
public class PaintBattleGameManager : MonoBehaviour
{
    [Header("Voice Input Handler")]
    [Tooltip("音声入力処理コンポーネント（Inspectorで接続）")]
    public VoiceInputHandler voiceInputHandler;
    
    [Header("Game Logic References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    public PaintCanvas paintCanvas;
    
    [Header("Game Settings")]
    [Tooltip("プレイヤーID（Phase 1では1で固定）")]
    public int playerId = 1;
    
    [Tooltip("塗り速度の倍率")]
    [Range(0.1f, 5f)]
    public float paintSpeedMultiplier = 1f;
    
    // 内部状態
    private bool isGameActive = true;
    
    void Start()
    {
        // 参照が設定されていない場合は自動検索（推奨はInspector接続）
        if (voiceInputHandler == null)
        {
            voiceInputHandler = FindObjectOfType<VoiceInputHandler>();
            if (voiceInputHandler == null)
            {
                Debug.LogError("PaintBattleGameManager: VoiceInputHandlerが見つかりません");
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
    }
    
    void Update()
    {
        if (!isGameActive || voiceInputHandler == null || paintCanvas == null)
        {
            return;
        }
        
        // カリブレーション中（順次または個別）は塗り処理をスキップ（パフォーマンス維持）
        if (VoiceCalibrator.IsCalibrating || VoiceCalibrator.IsIndividualCalibrating)
        {
            return;
        }
        
        // デバッグモード時はマウス位置を直接使用
        if (voiceInputHandler.IsDebugModeActive())
        {
            Vector2 mouseScreenPos = voiceInputHandler.GetDebugMousePosition();
            if (mouseScreenPos != Vector2.zero)
            {
                // 塗り処理（デバッグモード時は固定の強度を使用）
                float debugIntensity = 0.5f * paintSpeedMultiplier; // デバッグモード時の固定強度
                paintCanvas.PaintAt(mouseScreenPos, playerId, debugIntensity);
            }
            return; // デバッグモード時は通常の処理をスキップ
        }
        
        // 通常モード：無音判定
        if (voiceInputHandler.IsSilent)
        {
            // 無音時は塗らない
            paintCanvas.NotifyPaintingSuppressed();
            return;
        }
        
        // VoiceInputHandlerから座標と音量を取得
        Vector2 screenPos = voiceInputHandler.CurrentScreenPosition;
        float volume = voiceInputHandler.CurrentVolume;
        
        // 塗り処理
        float intensity = volume * paintSpeedMultiplier;
        paintCanvas.PaintAt(screenPos, playerId, intensity);
    }
    
    /// <summary>
    /// ゲームを開始/停止
    /// </summary>
    public void SetGameActive(bool active)
    {
        isGameActive = active;
    }
}

