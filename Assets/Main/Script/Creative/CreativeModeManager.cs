using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// クリエイティブモードのマネージャー
/// ツール切り替え、履歴管理、音声入力からの塗り処理を担当
/// </summary>
public class CreativeModeManager : MonoBehaviour, ISinglePlayerGameMode
{
    [Header("References")]
    [Tooltip("音声入力処理コンポーネント（Inspectorで接続）")]
    [SerializeField] private VoiceInputHandler voiceInputHandler;
    
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Tooltip("色選択システム（Inspectorで接続）")]
    [SerializeField] private ColorSelectionSystem colorSelectionSystem;
    
    [Tooltip("インクエフェクト（Inspectorで接続、またはCreativeModeSettingsから取得）")]
    [SerializeField] private InkEffect inkEffect;
    
    [Tooltip("爆弾コントローラ（カウントダウン中は塗り処理をスキップするために使用）")]
    [SerializeField] private VolumeTriggeredBombController volumeTriggeredBombController;
    
    [Header("UI Objects")]
    [Tooltip("クリエイティブモード時に非表示にするオブジェクト（OuterFrameなど、複数設定可能）")]
    [SerializeField] private GameObject[] outerFrameObjects;
    
    [Header("Settings")]
    [Tooltip("クリエイティブモード設定（Inspectorで接続）")]
    [SerializeField] private CreativeModeSettings settings;
    
    [Header("Current State")]
    [Tooltip("現在のツールモード")]
    [SerializeField] private CreativeToolMode currentToolMode = CreativeToolMode.Paint;
    
    [Tooltip("現在のブラシ")]
    [SerializeField] private BrushStrategyBase currentBrush;
    
    // 内部状態
    private int currentPlayerId;
    private Color currentColor;
    private Stack<CanvasState> historyStack = new Stack<CanvasState>();
    private float lastHistorySaveTime = 0f;
    private float silenceStartTime = 0f;
    private bool isInOperation = false; // 操作中かどうか（音声検出中）
    private Vector2 lastPaintPosition = Vector2.zero; // 前回の塗り位置
    private bool hasLastPosition = false; // 前回の位置が有効かどうか
    private CanvasState lastSavedState = null; // 前回保存した状態（変更検出用）
    private bool isInitialized = false; // ISinglePlayerGameMode経由で初期化されたかどうか
    
    // イベント
    public event System.Action<CreativeToolMode> OnToolModeChanged;
    public event System.Action<Color> OnColorChanged;
    public event System.Action<bool> OnUndoAvailabilityChanged; // bool = canUndo
    public event System.Action<BrushStrategyBase> OnBrushChanged;
    public UnityEvent<CreativeToolMode> OnToolModeChangedUnityEvent;
    public UnityEvent<Color> OnColorChangedUnityEvent;
    public UnityEvent<bool> OnUndoAvailabilityChangedUnityEvent;
    public UnityEvent<BrushStrategyBase> OnBrushChangedUnityEvent;
    
    // ISinglePlayerGameMode インターフェースの実装
    public SinglePlayerGameModeType GetModeType() => SinglePlayerGameModeType.Creative;
    
    public void Initialize(SinglePlayerGameModeSettings modeSettings)
    {
        // 参照の自動検索
        if (voiceInputHandler == null)
        {
            voiceInputHandler = FindObjectOfType<VoiceInputHandler>();
            if (voiceInputHandler == null)
            {
                Debug.LogError("CreativeModeManager: VoiceInputHandlerが見つかりません");
            }
        }
        
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
            if (paintCanvas == null)
            {
                Debug.LogError("CreativeModeManager: PaintCanvasが見つかりません");
            }
        }
        
        if (colorSelectionSystem == null)
        {
            colorSelectionSystem = FindObjectOfType<ColorSelectionSystem>();
            if (colorSelectionSystem == null)
            {
                Debug.LogError("CreativeModeManager: ColorSelectionSystemが見つかりません");
            }
        }
        
        if (volumeTriggeredBombController == null)
        {
            volumeTriggeredBombController = FindObjectOfType<VolumeTriggeredBombController>();
        }
        
        // 設定の初期化
        if (settings == null)
        {
            Debug.LogError("CreativeModeManager: CreativeModeSettingsが設定されていません");
            return;
        }
        
        currentPlayerId = settings.defaultPlayerId;
        currentColor = settings.initialColor;
        
        // デフォルトブラシの設定
        if (currentBrush == null)
        {
            currentBrush = settings.defaultBrush;
            if (currentBrush == null && settings.availableBrushes.Count > 0)
            {
                currentBrush = settings.availableBrushes[0];
            }
        }
        
        // 色選択システムのイベント購読
        if (colorSelectionSystem != null)
        {
            colorSelectionSystem.OnColorChanged += OnColorSelectionChanged;
            currentColor = colorSelectionSystem.GetCurrentColor();
        }
        
        // InkEffectの参照を取得（CreativeModeSettingsから、または自動検索）
        if (inkEffect == null && settings != null && settings.inkEffect != null)
        {
            inkEffect = settings.inkEffect;
        }
        
        if (inkEffect == null)
        {
            inkEffect = FindObjectOfType<InkEffect>();
            if (inkEffect == null)
            {
                Debug.LogWarning("CreativeModeManager: InkEffectが見つかりません。エフェクトは表示されません。");
            }
        }
        
        // InkEffectを有効化
        if (inkEffect != null)
        {
            inkEffect.gameObject.SetActive(true);
        }
        
        isInitialized = true;
    }
    
    public void StartGame()
    {
        // 初期状態を履歴に保存
        PushHistorySnapshot();
        
        // OuterFrameオブジェクトを非表示にする
        SetOuterFrameObjectsVisibility(false);
    }
    
    public void UpdateGame(float deltaTime)
    {
        if (voiceInputHandler == null || paintCanvas == null || settings == null)
        {
            return;
        }
        
        // カリブレーション中（順次または個別）は塗り処理をスキップ（パフォーマンス維持）
        if (VoiceCalibrator.IsCalibrating || VoiceCalibrator.IsIndividualCalibrating)
        {
            return;
        }
        
        // 音声入力の処理
        ProcessVoiceInput();
        
        // 履歴保存のタイミング管理
        ManageHistorySaving();
    }
    
    public void EndGame()
    {
        // クリーンアップ処理（OnDestroy()の処理を移行）
        if (colorSelectionSystem != null)
        {
            colorSelectionSystem.OnColorChanged -= OnColorSelectionChanged;
        }
        
        // OuterFrameオブジェクトを表示に戻す
        SetOuterFrameObjectsVisibility(true);
        
        // InkEffectを無効化（必要に応じて）
        // 注意: 他のモードでも使用する場合は無効化しない
        // if (inkEffect != null)
        // {
        //     inkEffect.gameObject.SetActive(false);
        // }
    }
    
    public void Pause()
    {
        // Creativeモードは一時停止機能は不要（必要に応じて実装）
    }
    
    public void Resume()
    {
        // Creativeモードは再開機能は不要（必要に応じて実装）
    }
    
    public int GetScore() => 0; // Creativeモードはスコアなし
    
    public float GetProgress() => 0f; // Creativeモードは進捗なし
    
    public bool IsGameOver() => false; // Creativeモードは終了条件なし
    
    // 後方互換性のため、Start()とUpdate()も維持（既存のコードが動作するように）
    void Start()
    {
        // ISinglePlayerGameMode経由で初期化されない場合のフォールバック
        // （既存のコードとの互換性のため）
        if (!isInitialized)
        {
            Initialize(null);
            StartGame();
        }
    }
    
    void Update()
    {
        // ISinglePlayerGameMode経由で更新されない場合のフォールバック
        // （既存のコードとの互換性のため）
        // SinglePlayerModeManagerがUpdateGame()を呼ぶため、ここでは何もしない
        // ただし、既存のコードとの互換性のために残しておく
    }
    
    void OnDestroy()
    {
        EndGame();
    }
    
    /// <summary>
    /// 音声入力の処理
    /// </summary>
    private void ProcessVoiceInput()
    {
        // デバッグモード時はマウス位置を直接使用
        if (voiceInputHandler.IsDebugModeActive())
        {
            Vector2 mouseScreenPos = voiceInputHandler.GetDebugMousePosition();
            if (mouseScreenPos != Vector2.zero)
            {
                // マウスが動いている場合は連続線を描く
                if (hasLastPosition && Vector2.Distance(mouseScreenPos, lastPaintPosition) > 0.1f)
                {
                    float debugIntensity = 0.5f * settings.paintIntensity;
                    PaintAt(mouseScreenPos, debugIntensity);
                }
                else if (!hasLastPosition)
                {
                    float debugIntensity = 0.5f * settings.paintIntensity;
                    PaintAt(mouseScreenPos, debugIntensity);
                }
            }
            return;
        }
        
        // 無音判定
        if (voiceInputHandler.IsSilent)
        {
            // 操作中だった場合は操作終了
            if (isInOperation)
            {
                isInOperation = false;
                silenceStartTime = Time.time;
                // 前回の位置をリセット（次の操作の開始時に新しい線として扱う）
                hasLastPosition = false;
            }
            paintCanvas.NotifyPaintingSuppressed();
            return;
        }
        
        // 音声が検出された場合
        if (!isInOperation)
        {
            // 操作開始
            isInOperation = true;
            // 操作開始時に履歴を保存（OnOperationモードの場合）
            if (settings.historySaveMode == CreativeModeSettings.HistorySaveMode.OnOperation)
            {
                PushHistorySnapshot();
            }
            // 前回の位置をリセット（新しい操作の開始）
            hasLastPosition = false;
        }
        
        // 座標と音量を取得
        Vector2 screenPos = voiceInputHandler.CurrentScreenPosition;
        float volume = voiceInputHandler.CurrentVolume;
        float intensity = volume * settings.paintIntensity;
        
        // カウントダウン中は塗り処理をスキップ（爆発の瞬間以外は塗らない）
        // ただし、InkEffectは表示するため、OnPaintCompletedイベントのみ発火
        if (volumeTriggeredBombController != null && volumeTriggeredBombController.IsCountingDown)
        {
            // InkEffectを表示するために、OnPaintCompletedイベントを手動で発火
            if (paintCanvas != null)
            {
                paintCanvas.InvokePaintCompletedEvent(screenPos, currentPlayerId, intensity);
            }
            return;
        }
        
        // BombBrushが選択されている場合、予約システムで処理されるため通常の塗り処理をスキップ（重複を防ぐ）
        if (currentBrush is BombBrush)
        {
            // 予約された爆発がある場合は、通常の塗り処理をスキップ
            if (volumeTriggeredBombController != null && volumeTriggeredBombController.HasPendingExplosion)
            {
                return;
            }
            // 予約がない場合でも、BombBrushはカウントダウンシステムで処理されるため通常の塗り処理はスキップ
            return;
        }
        
        // 塗り処理
        PaintAt(screenPos, intensity);
    }
    
    /// <summary>
    /// 指定位置に塗る（現在のブラシを使用）
    /// 前回の位置と現在の位置の間を補間して連続線を描く
    /// </summary>
    private void PaintAt(Vector2 position, float intensity)
    {
        if (paintCanvas == null || currentBrush == null) return;
        
        // BombBrushが選択されている場合は補間処理をスキップ（爆発はカウントダウンシステムで処理）
        // 補間処理でBombBrush.Paint()が複数回呼ばれると処理負荷が高くなるため
        if (currentBrush is BombBrush)
        {
            // BombBrushの場合は補間せず、現在位置だけ塗る（通常はカウントダウン中でない限り実行されない）
            PaintWithCurrentBrush(position, intensity);
            lastPaintPosition = position;
            hasLastPosition = true;
            return;
        }
        
        if (hasLastPosition)
        {
            // 前回の位置と現在の位置の間を補間
            PaintLineBetween(lastPaintPosition, position, intensity);
        }
        else
        {
            // 最初の点はそのまま塗る
            PaintWithCurrentBrush(position, intensity);
        }
        // 現在の位置を記録
        lastPaintPosition = position;
        hasLastPosition = true;
    }
    
    /// <summary>
    /// 現在のブラシで塗る
    /// </summary>
    private void PaintWithCurrentBrush(Vector2 position, float intensity)
    {
        if (paintCanvas == null || currentBrush == null) return;
        
        float finalIntensity = intensity * settings.paintIntensity;
        currentBrush.Paint(paintCanvas, position, currentPlayerId, currentColor, finalIntensity);
    }
    
    /// <summary>
    /// 2点間を補間して連続線を描く（塗り用）
    /// </summary>
    private void PaintLineBetween(Vector2 startPos, Vector2 endPos, float intensity)
    {
        if (paintCanvas == null || currentBrush == null) return;
        
        // 現在のブラシの半径を取得
        float radius = currentBrush.GetRadius();
        
        // 距離を計算
        float distance = Vector2.Distance(startPos, endPos);
        
        // 距離が短い場合は補間をスキップ（半径の1/4以下）
        if (distance < radius * 0.25f)
        {
            PaintWithCurrentBrush(endPos, intensity);
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
            PaintWithCurrentBrush(interpolatedPos, intensity);
        }
    }
    
    /// <summary>
    /// 履歴保存のタイミング管理
    /// </summary>
    private void ManageHistorySaving()
    {
        if (settings == null) return;
        
        if (settings.historySaveMode == CreativeModeSettings.HistorySaveMode.TimeBased)
        {
            // 一定時間ごとに履歴を保存（変更があった場合のみ）
            if (Time.time - lastHistorySaveTime >= 0.5f) // 0.5秒ごと
            {
                if (HasCanvasChanged())
                {
                    PushHistorySnapshot();
                    lastHistorySaveTime = Time.time;
                }
                else
                {
                    // 変更がない場合は時間だけ更新（次回のチェックを待つ）
                    lastHistorySaveTime = Time.time;
                }
            }
        }
        else if (settings.historySaveMode == CreativeModeSettings.HistorySaveMode.OnOperation)
        {
            // 操作終了時に履歴を保存（変更があった場合のみ）
            if (!isInOperation && Time.time - silenceStartTime >= settings.silenceDurationForOperationEnd)
            {
                if (HasCanvasChanged())
                {
                    PushHistorySnapshot();
                }
                silenceStartTime = float.MaxValue; // 重複保存を防ぐ
            }
        }
    }
    
    /// <summary>
    /// キャンバスに変更があったかどうかをチェック
    /// </summary>
    private bool HasCanvasChanged()
    {
        if (paintCanvas == null) return false;
        
        // 前回保存した状態がない場合は変更ありとみなす
        if (lastSavedState == null) return true;
        
        CanvasState currentState = paintCanvas.GetState();
        if (currentState == null) return false;
        
        // サイズが異なる場合は変更あり
        if (currentState.width != lastSavedState.width || 
            currentState.height != lastSavedState.height)
        {
            return true;
        }
        
        // 各ピクセルを比較（効率化のため、最初に見つかった違いで即座に返す）
        for (int x = 0; x < currentState.width; x++)
        {
            for (int y = 0; y < currentState.height; y++)
            {
                // playerId、intensity、colorのいずれかが異なれば変更あり
                if (currentState.playerIds[x, y] != lastSavedState.playerIds[x, y] ||
                    Mathf.Abs(currentState.intensities[x, y] - lastSavedState.intensities[x, y]) > 0.001f ||
                    currentState.colors[x, y] != lastSavedState.colors[x, y])
                {
                    return true;
                }
            }
        }
        
        return false; // 変更なし
    }
    
    /// <summary>
    /// ツールモードを設定
    /// </summary>
    public void SetToolMode(CreativeToolMode mode)
    {
        if (currentToolMode == mode) return;
        
        currentToolMode = mode;
        OnToolModeChanged?.Invoke(mode);
        OnToolModeChangedUnityEvent?.Invoke(mode);
    }
    
    /// <summary>
    /// ブラシを設定
    /// </summary>
    public void SetBrush(BrushStrategyBase brush)
    {
        if (brush == null || currentBrush == brush) return;
        
        currentBrush = brush;
        OnBrushChanged?.Invoke(brush);
        OnBrushChangedUnityEvent?.Invoke(brush);
    }
    
    /// <summary>
    /// ブラシタイプを設定（後方互換性のため、将来削除予定）
    /// </summary>
    [System.Obsolete("BrushTypeは将来削除予定です。SetBrush(BrushStrategyBase)を使用してください。")]
    public void SetBrushType(BrushType brushType)
    {
        // BrushTypeからBrushStrategyBaseへの変換
        if (settings == null) return;
        
        BrushStrategyBase brush = null;
        if (brushType == BrushType.Pencil)
        {
            brush = settings.availableBrushes.Find(b => b is PencilBrush);
        }
        else if (brushType == BrushType.Paint)
        {
            brush = settings.availableBrushes.Find(b => b is PaintBrush);
        }
        
        if (brush != null)
        {
            SetBrush(brush);
        }
    }
    
    /// <summary>
    /// 色を設定
    /// </summary>
    public void SetColor(Color color)
    {
        Debug.Log($"CreativeModeManager: SetColor - Color={color}");
        currentColor = color;
        OnColorChanged?.Invoke(color);
        OnColorChangedUnityEvent?.Invoke(color);
    }
    
    /// <summary>
    /// キャンバスをクリア
    /// </summary>
    public void ClearCanvas()
    {
        if (paintCanvas == null) return;
        
        paintCanvas.ResetCanvas();
        PushHistorySnapshot();
    }
    
    /// <summary>
    /// Undo（前の状態に戻す）
    /// </summary>
    public void Undo()
    {
        if (!CanUndo() || paintCanvas == null) return;
        
        // 現在の状態を破棄
        if (historyStack.Count > 0)
        {
            historyStack.Pop(); // 現在の状態を破棄
        }
        
        // 前の状態を復元
        if (historyStack.Count > 0)
        {
            CanvasState previousState = historyStack.Peek();
            paintCanvas.RestoreState(previousState);
            
            // 前回保存した状態も更新（Undo後の状態が新しい基準になる）
            if (lastSavedState == null || 
                lastSavedState.width != previousState.width || 
                lastSavedState.height != previousState.height)
            {
                lastSavedState = new CanvasState(previousState.width, previousState.height);
            }
            lastSavedState.CopyFrom(previousState);
        }
        else
        {
            // 履歴がない場合はクリア
            paintCanvas.ResetCanvas();
            // クリア後の状態を記録
            CanvasState clearedState = paintCanvas.GetState();
            if (clearedState != null)
            {
                if (lastSavedState == null || 
                    lastSavedState.width != clearedState.width || 
                    lastSavedState.height != clearedState.height)
                {
                    lastSavedState = new CanvasState(clearedState.width, clearedState.height);
                }
                lastSavedState.CopyFrom(clearedState);
            }
        }
        
        UpdateUndoAvailability();
    }
    
    /// <summary>
    /// Undo可能かどうか
    /// </summary>
    public bool CanUndo()
    {
        return historyStack.Count > 1; // 現在の状態 + 1つ以上の過去の状態が必要
    }
    
    /// <summary>
    /// 履歴スナップショットを保存
    /// </summary>
    private void PushHistorySnapshot()
    {
        if (paintCanvas == null) return;
        
        CanvasState state = paintCanvas.GetState();
        if (state == null) return;
        
        historyStack.Push(state);
        TrimHistory();
        UpdateUndoAvailability();
        
        // 前回保存した状態を更新（変更検出用）
        // 新しいCanvasStateを作成してコピー（参照ではなく値のコピー）
        if (lastSavedState == null || 
            lastSavedState.width != state.width || 
            lastSavedState.height != state.height)
        {
            lastSavedState = new CanvasState(state.width, state.height);
        }
        lastSavedState.CopyFrom(state);
    }
    
    /// <summary>
    /// 履歴サイズを制限
    /// </summary>
    private void TrimHistory()
    {
        if (settings == null) return;
        
        while (historyStack.Count > settings.maxHistorySize)
        {
            historyStack.Pop();
        }
    }
    
    /// <summary>
    /// Undoの可用性を更新
    /// </summary>
    private void UpdateUndoAvailability()
    {
        bool canUndo = CanUndo();
        OnUndoAvailabilityChanged?.Invoke(canUndo);
        OnUndoAvailabilityChangedUnityEvent?.Invoke(canUndo);
    }
    
    /// <summary>
    /// 色選択システムの色変更イベントハンドラ
    /// </summary>
    private void OnColorSelectionChanged(Color color)
    {
        Debug.Log($"CreativeModeManager: OnColorSelectionChanged - Color={color}");
        SetColor(color);
    }
    
    /// <summary>
    /// 現在のツールモードを取得
    /// </summary>
    public CreativeToolMode GetCurrentToolMode()
    {
        return currentToolMode;
    }
    
    /// <summary>
    /// 現在のブラシを取得
    /// </summary>
    public BrushStrategyBase GetCurrentBrush()
    {
        return currentBrush;
    }
    
    /// <summary>
    /// 現在の色を取得
    /// </summary>
    public Color GetCurrentColor()
    {
        return currentColor;
    }
    
    /// <summary>
    /// 現在のInkEffectを取得
    /// </summary>
    public InkEffect GetInkEffect()
    {
        return inkEffect;
    }
    
    /// <summary>
    /// 利用可能なブラシのリストを取得
    /// </summary>
    public List<BrushStrategyBase> GetAvailableBrushes()
    {
        if (settings == null) return new List<BrushStrategyBase>();
        return new List<BrushStrategyBase>(settings.availableBrushes);
    }
    
    /// <summary>
    /// 現在のブラシタイプを取得（後方互換性のため、将来削除予定）
    /// </summary>
    [System.Obsolete("BrushTypeは将来削除予定です。GetCurrentBrush()を使用してください。")]
    public BrushType GetCurrentBrushType()
    {
        if (currentBrush == null) return BrushType.Pencil;
        
        if (currentBrush is PencilBrush) return BrushType.Pencil;
        if (currentBrush is PaintBrush) return BrushType.Paint;
        
        return BrushType.Pencil; // デフォルト
    }
    
    /// <summary>
    /// OuterFrameオブジェクトの表示/非表示を設定
    /// </summary>
    /// <param name="visible">true = 表示、false = 非表示</param>
    private void SetOuterFrameObjectsVisibility(bool visible)
    {
        if (outerFrameObjects == null || outerFrameObjects.Length == 0)
        {
            return;
        }
        
        foreach (GameObject obj in outerFrameObjects)
        {
            if (obj != null)
            {
                obj.SetActive(visible);
            }
        }
    }
}

