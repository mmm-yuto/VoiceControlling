using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// クリエイティブモードのマネージャー
/// ツール切り替え、履歴管理、音声入力からの塗り処理を担当
/// </summary>
public class CreativeModeManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("音声入力処理コンポーネント（Inspectorで接続）")]
    [SerializeField] private VoiceInputHandler voiceInputHandler;
    
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Tooltip("色選択システム（Inspectorで接続）")]
    [SerializeField] private ColorSelectionSystem colorSelectionSystem;
    
    [Header("Settings")]
    [Tooltip("クリエイティブモード設定（Inspectorで接続）")]
    [SerializeField] private CreativeModeSettings settings;
    
    [Header("Current State")]
    [Tooltip("現在のツールモード")]
    [SerializeField] private CreativeToolMode currentToolMode = CreativeToolMode.Paint;
    
    [Tooltip("現在のブラシタイプ")]
    [SerializeField] private BrushType currentBrushType = BrushType.Pencil;
    
    // 内部状態
    private int currentPlayerId;
    private Color currentColor;
    private Stack<CanvasState> historyStack = new Stack<CanvasState>();
    private float lastHistorySaveTime = 0f;
    private float silenceStartTime = 0f;
    private bool isInOperation = false; // 操作中かどうか（音声検出中）
    
    // イベント
    public event System.Action<CreativeToolMode> OnToolModeChanged;
    public event System.Action<Color> OnColorChanged;
    public event System.Action<bool> OnUndoAvailabilityChanged; // bool = canUndo
    public event System.Action<BrushType> OnBrushTypeChanged;
    public UnityEvent<CreativeToolMode> OnToolModeChangedUnityEvent;
    public UnityEvent<Color> OnColorChangedUnityEvent;
    public UnityEvent<bool> OnUndoAvailabilityChangedUnityEvent;
    public UnityEvent<BrushType> OnBrushTypeChangedUnityEvent;
    
    void Start()
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
        
        // 設定の初期化
        if (settings == null)
        {
            Debug.LogError("CreativeModeManager: CreativeModeSettingsが設定されていません");
            return;
        }
        
        currentPlayerId = settings.defaultPlayerId;
        currentColor = settings.initialColor;
        
        // 色選択システムのイベント購読
        if (colorSelectionSystem != null)
        {
            colorSelectionSystem.OnColorChanged += OnColorSelectionChanged;
            currentColor = colorSelectionSystem.GetCurrentColor();
        }
        
        // 初期状態を履歴に保存
        PushHistorySnapshot();
    }
    
    void OnDestroy()
    {
        if (colorSelectionSystem != null)
        {
            colorSelectionSystem.OnColorChanged -= OnColorSelectionChanged;
        }
    }
    
    void Update()
    {
        if (voiceInputHandler == null || paintCanvas == null || settings == null)
        {
            return;
        }
        
        // 音声入力の処理
        ProcessVoiceInput();
        
        // 履歴保存のタイミング管理
        ManageHistorySaving();
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
                float debugIntensity = 0.5f * settings.paintIntensity;
                PaintAt(mouseScreenPos, debugIntensity);
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
        }
        
        // 座標と音量を取得
        Vector2 screenPos = voiceInputHandler.CurrentScreenPosition;
        float volume = voiceInputHandler.CurrentVolume;
        
        // 塗り処理
        float intensity = volume * settings.paintIntensity;
        PaintAt(screenPos, intensity);
    }
    
    /// <summary>
    /// 指定位置に塗る（ツールモードに応じて処理を分岐）
    /// </summary>
    private void PaintAt(Vector2 position, float intensity)
    {
        if (paintCanvas == null) return;
        
        switch (currentToolMode)
        {
            case CreativeToolMode.Paint:
                PaintWithCurrentBrush(position, intensity);
                break;
            case CreativeToolMode.Eraser:
                EraseAt(position);
                break;
        }
    }
    
    /// <summary>
    /// 現在のブラシタイプで塗る
    /// </summary>
    private void PaintWithCurrentBrush(Vector2 position, float intensity)
    {
        if (paintCanvas == null) return;
        
        switch (currentBrushType)
        {
            case BrushType.Pencil:
                PaintWithPencil(position, intensity);
                break;
            case BrushType.Paint:
                PaintWithPaint(position, intensity);
                break;
        }
    }
    
    /// <summary>
    /// 鉛筆で塗る
    /// </summary>
    private void PaintWithPencil(Vector2 position, float intensity)
    {
        if (paintCanvas == null) return;
        
        float finalIntensity = intensity * settings.paintIntensity;
        float radius = settings.pencilRadius;
        
        paintCanvas.PaintAtWithRadius(position, currentPlayerId, finalIntensity, currentColor, radius);
    }
    
    /// <summary>
    /// ペンキブラシで塗る（将来的な拡張）
    /// </summary>
    private void PaintWithPaint(Vector2 position, float intensity)
    {
        if (paintCanvas == null) return;
        
        float finalIntensity = intensity * settings.paintIntensity;
        float radius = settings.paintBrushRadius;
        
        paintCanvas.PaintAtWithRadius(position, currentPlayerId, finalIntensity, currentColor, radius);
    }
    
    /// <summary>
    /// 消しツールで消す
    /// </summary>
    private void EraseAt(Vector2 position)
    {
        if (paintCanvas == null) return;
        
        float radius = settings.eraserRadius;
        paintCanvas.EraseAt(position, radius);
    }
    
    /// <summary>
    /// 履歴保存のタイミング管理
    /// </summary>
    private void ManageHistorySaving()
    {
        if (settings == null) return;
        
        if (settings.historySaveMode == CreativeModeSettings.HistorySaveMode.TimeBased)
        {
            // 一定時間ごとに履歴を保存
            if (Time.time - lastHistorySaveTime >= 0.5f) // 0.5秒ごと
            {
                PushHistorySnapshot();
                lastHistorySaveTime = Time.time;
            }
        }
        else if (settings.historySaveMode == CreativeModeSettings.HistorySaveMode.OnOperation)
        {
            // 操作終了時に履歴を保存
            if (!isInOperation && Time.time - silenceStartTime >= settings.silenceDurationForOperationEnd)
            {
                PushHistorySnapshot();
                silenceStartTime = float.MaxValue; // 重複保存を防ぐ
            }
        }
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
    /// ブラシタイプを設定
    /// </summary>
    public void SetBrushType(BrushType brushType)
    {
        if (currentBrushType == brushType) return;
        
        currentBrushType = brushType;
        OnBrushTypeChanged?.Invoke(brushType);
        OnBrushTypeChangedUnityEvent?.Invoke(brushType);
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
        }
        else
        {
            // 履歴がない場合はクリア
            paintCanvas.ResetCanvas();
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
    /// 現在のブラシタイプを取得
    /// </summary>
    public BrushType GetCurrentBrushType()
    {
        return currentBrushType;
    }
}

