using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クリエイティブモードの中核ロジック
/// 音声入力から座標を算出し、ツールモードに応じて塗り／消去を行う
/// </summary>
public class CreativeModeManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CreativeModeSettings settings;
    [SerializeField] private CreativeToolMode initialToolMode = CreativeToolMode.Paint;
    
    [Header("References")]
    [SerializeField] private PaintCanvas paintCanvas;
    [SerializeField] private VoiceToScreenMapper voiceToScreenMapper;
    [SerializeField] private VolumeAnalyzer volumeAnalyzer;
    [SerializeField] private ImprovedPitchAnalyzer improvedPitchAnalyzer;
    
    [Header("Runtime State (Read Only)")]
    [SerializeField] private CreativeToolMode currentToolMode;
    [SerializeField] private Color currentColor = Color.white;
    [SerializeField] private int currentPlayerId = 1;
    [SerializeField] private bool isActive = true;
    
    public CreativeToolMode CurrentToolMode => currentToolMode;
    public Color CurrentColor => currentColor;
    public bool IsActive => isActive;
    
    public static event System.Action<CreativeToolMode> OnToolModeChanged;
    public static event System.Action<Color> OnColorChanged;
    public static event System.Action<bool> OnUndoAvailabilityChanged;
    
    private readonly Stack<CanvasState> historyStack = new Stack<CanvasState>();
    private float lastHistorySaveTime = 0f;
    private float latestVolume = 0f;
    private float latestPitch = 0f;
    private bool isOperationInProgress = false;
    private bool wasSilent = false;
    private float silenceStartTime = 0f;
    private float lastVolumeDetectedTime = float.NegativeInfinity;
    
    void Awake()
    {
        ResolveReferences();
        InitializeState();
    }
    
    void OnEnable()
    {
        SubscribeToVoiceEvents();
    }
    
    void OnDisable()
    {
        UnsubscribeFromVoiceEvents();
    }
    
    void Update()
    {
        if (!isActive || paintCanvas == null || voiceToScreenMapper == null || settings == null)
        {
            return;
        }
        
        if (settings.historySaveMode == CreativeModeSettings.HistorySaveMode.TimeBased)
        {
            float interval = Mathf.Max(0.01f, settings.autoSaveHistoryInterval);
            if (Time.time - lastHistorySaveTime >= interval)
            {
                PushHistorySnapshot();
                lastHistorySaveTime = Time.time;
            }
        }
        
        float silenceThreshold = GetSilenceThreshold();
        float timeSinceVolume = Time.time - lastVolumeDetectedTime;
        bool timeoutSilent = false;
        if (settings != null)
        {
            timeoutSilent = timeSinceVolume >= settings.silenceDurationForOperationEnd;
        }
        else
        {
            timeoutSilent = timeSinceVolume >= 0.3f;
        }
        
        bool isSilent = latestVolume <= silenceThreshold || latestPitch <= 0f || timeoutSilent;
        
        if (isSilent)
        {
            HandleSilencePhase();
            return;
        }
        
        HandleActiveVoice();
    }
    
    void ResolveReferences()
    {
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
        }
        
        if (voiceToScreenMapper == null)
        {
            voiceToScreenMapper = FindObjectOfType<VoiceToScreenMapper>();
        }
        
        if (volumeAnalyzer == null)
        {
            volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        }
        
        if (improvedPitchAnalyzer == null)
        {
            improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
        }
        
        if (settings != null && settings.improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer = settings.improvedPitchAnalyzer;
        }
    }
    
    void InitializeState()
    {
        if (settings == null)
        {
            Debug.LogError("CreativeModeManager: CreativeModeSettingsが設定されていません");
            enabled = false;
            return;
        }
        
        currentToolMode = initialToolMode;
        currentColor = settings.initialColor;
        currentPlayerId = Mathf.Max(1, settings.defaultPlayerId);
        
        PushHistorySnapshot(force: true);
        NotifyToolChanged();
        NotifyColorChanged();
        NotifyUndoAvailability();
    }
    
    void SubscribeToVoiceEvents()
    {
        UnsubscribeFromVoiceEvents();
        
        if (volumeAnalyzer != null)
        {
            volumeAnalyzer.OnVolumeDetected += OnVolumeDetected;
        }
        
        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.OnPitchDetected += OnPitchDetected;
        }
    }
    
    void UnsubscribeFromVoiceEvents()
    {
        if (volumeAnalyzer != null)
        {
            volumeAnalyzer.OnVolumeDetected -= OnVolumeDetected;
        }
        
        if (improvedPitchAnalyzer != null)
        {
            improvedPitchAnalyzer.OnPitchDetected -= OnPitchDetected;
        }
    }
    
    float GetSilenceThreshold()
    {
        if (settings != null && settings.improvedPitchAnalyzer != null)
        {
            return settings.improvedPitchAnalyzer.volumeThreshold;
        }
        
        if (improvedPitchAnalyzer != null)
        {
            return improvedPitchAnalyzer.volumeThreshold;
        }
        
        return settings != null ? settings.silenceVolumeThreshold : 0.01f;
    }
    
    void HandleSilencePhase()
    {
        if (settings.historySaveMode != CreativeModeSettings.HistorySaveMode.OnOperation || !isOperationInProgress)
        {
            wasSilent = true;
            return;
        }
        
        if (!wasSilent)
        {
            silenceStartTime = Time.time;
            wasSilent = true;
            return;
        }
        
        if (Time.time - silenceStartTime >= settings.silenceDurationForOperationEnd)
        {
            PushHistorySnapshot();
            isOperationInProgress = false;
            wasSilent = false;
        }
    }
    
    void HandleActiveVoice()
    {
        wasSilent = false;
        
        if (settings.historySaveMode == CreativeModeSettings.HistorySaveMode.OnOperation && !isOperationInProgress)
        {
            PushHistorySnapshot();
            isOperationInProgress = true;
        }
        
        if (voiceToScreenMapper == null || paintCanvas == null)
        {
            return;
        }
        
        Vector2 screenPos = voiceToScreenMapper.MapVoiceToScreen(latestVolume, latestPitch);
        float intensity = latestVolume * (settings != null ? settings.paintIntensity : 1f);
        
        if (currentToolMode == CreativeToolMode.Paint)
        {
            paintCanvas.PaintAt(screenPos, currentPlayerId, intensity, currentColor);
        }
        else
        {
            float radius = settings != null ? settings.eraserRadius : 30f;
            paintCanvas.EraseAt(screenPos, radius);
        }
    }
    
    void PushHistorySnapshot(bool force = false)
    {
        if (paintCanvas == null)
        {
            return;
        }
        
        CanvasState state = paintCanvas.GetState();
        if (state == null)
        {
            return;
        }
        
        if (!force && historyStack.Count > 0)
        {
            CanvasState current = historyStack.Peek();
            if (ReferenceEquals(current, state))
            {
                return;
            }
        }
        
        historyStack.Push(state);
        TrimHistory();
        NotifyUndoAvailability();
    }
    
    void TrimHistory()
    {
        if (settings == null)
        {
            return;
        }
        
        int maxHistory = Mathf.Max(1, settings.maxHistorySize);
        if (historyStack.Count <= maxHistory)
        {
            return;
        }
        
        var buffer = new Stack<CanvasState>(historyStack.Count);
        while (historyStack.Count > 0)
        {
            buffer.Push(historyStack.Pop());
        }
        
        bool skippedOldest = false;
        while (buffer.Count > 0)
        {
            var state = buffer.Pop();
            if (!skippedOldest)
            {
                skippedOldest = true;
                continue;
            }
            historyStack.Push(state);
        }
    }
    
    void NotifyToolChanged()
    {
        OnToolModeChanged?.Invoke(currentToolMode);
    }
    
    void NotifyColorChanged()
    {
        OnColorChanged?.Invoke(currentColor);
    }
    
    void NotifyUndoAvailability()
    {
        OnUndoAvailabilityChanged?.Invoke(CanUndo());
    }
    
    public void SetToolMode(CreativeToolMode mode)
    {
        if (currentToolMode == mode)
        {
            return;
        }
        
        currentToolMode = mode;
        NotifyToolChanged();
    }
    
    public void SetColor(Color color)
    {
        currentColor = color;
        NotifyColorChanged();
    }
    
    public void ClearCanvas()
    {
        if (paintCanvas == null)
        {
            return;
        }
        
        paintCanvas.ResetCanvas();
        PushHistorySnapshot();
    }
    
    public void Undo()
    {
        if (!CanUndo() || paintCanvas == null)
        {
            return;
        }
        
        // 現在の状態を破棄
        historyStack.Pop();
        CanvasState previous = historyStack.Peek();
        paintCanvas.RestoreState(previous);
        NotifyUndoAvailability();
    }
    
    public bool CanUndo()
    {
        return historyStack.Count > 1;
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
    }
    
    void OnVolumeDetected(float volume)
    {
        latestVolume = volume;
        lastHistorySaveTime = Time.time;
        lastVolumeDetectedTime = Time.time;
    }
    
    void OnPitchDetected(float pitch)
    {
        latestPitch = pitch;
    }
}

