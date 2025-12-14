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
    
    [Tooltip("プレイヤーのインク色（ColorDefenseモード時に使用される色）")]
    public Color playerInkColor = Color.white;

    [Header("Brush Settings")]
    [Tooltip("声で塗るときに使用するブラシ（PaintBrush などの ScriptableObject）")]
    public BrushStrategyBase brush;
    
    [Header("Game Mode Reference")]
    [Tooltip("シングルプレイモードマネージャー（ColorDefenseモードの状態確認用）")]
    [SerializeField] private SinglePlayerModeManager singlePlayerModeManager;
    
    // 内部状態
    private bool isGameActive = true;
    private Vector2 lastPaintPosition = Vector2.zero;
    private bool hasLastPosition = false;
    
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
        
        if (singlePlayerModeManager == null)
        {
            singlePlayerModeManager = FindObjectOfType<SinglePlayerModeManager>();
        }
    }
    
    void Update()
    {
        if (!isGameActive || voiceInputHandler == null || paintCanvas == null)
        {
            return;
        }
        
        // ColorDefenseモードでゲームが終了している場合は塗り処理をスキップ
        if (IsColorDefenseModeGameOver())
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
                PaintAt(mouseScreenPos, debugIntensity);
            }
            else
            {
                // マウスが画面外などで無効な場合は線をリセット
                hasLastPosition = false;
            }
            return; // デバッグモード時は通常の処理をスキップ
        }
        
        // 通常モード：無音判定
        if (voiceInputHandler.IsSilent)
        {
            // 無音時は塗らない
            paintCanvas.NotifyPaintingSuppressed();
            // 線の継続をリセット（次の発声から新しい線として扱う）
            hasLastPosition = false;
            return;
        }
        
        // VoiceInputHandlerから座標と音量を取得
        Vector2 screenPos = voiceInputHandler.CurrentScreenPosition;
        float volume = voiceInputHandler.CurrentVolume;
        
        // 塗り処理
        float intensity = volume * paintSpeedMultiplier;
        PaintAt(screenPos, intensity);
    }
    
    /// <summary>
    /// ゲームを開始/停止
    /// </summary>
    public void SetGameActive(bool active)
    {
        isGameActive = active;
    }

    /// <summary>
    /// 指定位置に塗る（ブラシを使って線を補間しながら塗る）
    /// </summary>
    private void PaintAt(Vector2 position, float intensity)
    {
        if (paintCanvas == null)
        {
            return;
        }

        // ブラシが設定されていない場合は、従来通り1点塗り（後方互換用）
        if (brush == null)
        {
            paintCanvas.PaintAt(position, playerId, intensity, playerInkColor);
            return;
        }

        if (hasLastPosition)
        {
            // 前回の位置と現在の位置の間を補間して連続線を描く
            PaintLineBetween(lastPaintPosition, position, intensity);
        }
        else
        {
            // 最初の点はそのまま塗る
            brush.Paint(paintCanvas, position, playerId, playerInkColor, intensity);
        }

        // 現在の位置を記録
        lastPaintPosition = position;
        hasLastPosition = true;
    }

    /// <summary>
    /// 2点間を補間して連続線を描く
    /// </summary>
    private void PaintLineBetween(Vector2 startPos, Vector2 endPos, float intensity)
    {
        if (paintCanvas == null || brush == null)
        {
            return;
        }

        // ブラシの半径を取得
        float radius = brush.GetRadius();

        // 距離を計算
        float distance = Vector2.Distance(startPos, endPos);

        // 距離が短い場合は補間をスキップ（半径の1/4以下）
        if (distance < radius * 0.25f)
        {
            brush.Paint(paintCanvas, endPos, playerId, playerInkColor, intensity);
            return;
        }

        // 補間ステップ数を計算（半径の半分ごとに点を打つ）
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / (radius * 0.5f)));

        // 最大ステップ数を制限（パフォーマンス対策）
        const int maxSteps = 50;
        steps = Mathf.Min(steps, maxSteps);

        // 各ステップで塗る
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 interpolatedPos = Vector2.Lerp(startPos, endPos, t);
            brush.Paint(paintCanvas, interpolatedPos, playerId, playerInkColor, intensity);
        }
    }
    
    /// <summary>
    /// ColorDefenseモードでゲームが終了しているかどうかを確認
    /// </summary>
    private bool IsColorDefenseModeGameOver()
    {
        if (singlePlayerModeManager == null)
        {
            return false;
        }
        
        var currentMode = singlePlayerModeManager.GetCurrentMode();
        if (currentMode == null)
        {
            return false;
        }
        
        // ColorDefenseモードの場合のみチェック
        if (currentMode.GetModeType() == SinglePlayerGameModeType.ColorDefense)
        {
            // ColorDefenseModeにキャストしてIsGameActive()を確認
            if (currentMode is ColorDefenseMode colorDefenseMode)
            {
                return !colorDefenseMode.IsGameActive();
            }
        }
        
        return false;
    }
}

