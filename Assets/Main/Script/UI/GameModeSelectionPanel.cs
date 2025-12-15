using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲームモード選択UIパネル
/// ユーザーがゲームモードを選択してからゲームを開始できるようにする
/// </summary>
public class GameModeSelectionPanel : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("選択画面のルートオブジェクト")]
    [SerializeField] private GameObject selectionPanel;
    
    [Tooltip("クリエイティブモード選択ボタン")]
    [SerializeField] private Button creativeButton;
    
    [Tooltip("カラーディフェンスモード選択ボタン")]
    [SerializeField] private Button colorDefenseButton;
    
    [Tooltip("トレーシングモード選択ボタン")]
    [SerializeField] private Button tracingButton;
    
    [Header("Settings")]
    [Tooltip("ゲームモード設定（ScriptableObject）")]
    [SerializeField] private SinglePlayerGameModeSettings gameModeSettings;
    
    [Header("Managers")]
    [Tooltip("ゲームモード切り替えマネージャー")]
    [SerializeField] private GameModeSwitcher gameModeSwitcher;
    
    [Tooltip("シングルプレイモードマネージャー")]
    [SerializeField] private SinglePlayerModeManager singlePlayerModeManager;

    [Header("Color Defense Lobby")]
    [Tooltip("ColorDefense モード開始前のロビー UI パネル")]
    [SerializeField] private ColorDefenseLobbyPanel colorDefenseLobbyPanel;

    [Header("Brush Selection")]
    [Tooltip("Pencil Brush選択ボタン")]
    [SerializeField] private Button pencilBrushButton;
    
    [Tooltip("Paint Brush選択ボタン")]
    [SerializeField] private Button paintBrushButton;
    
    [Tooltip("Spray Brush選択ボタン")]
    [SerializeField] private Button sprayBrushButton;

    [Header("Brush ScriptableObjects")]
    [Tooltip("Pencil Brush の ScriptableObject")]
    [SerializeField] private BrushStrategyBase pencilBrush;

    [Tooltip("Paint Brush の ScriptableObject")]
    [SerializeField] private BrushStrategyBase paintBrush;

    [Tooltip("Spray Brush の ScriptableObject")]
    [SerializeField] private BrushStrategyBase sprayBrush;
    
    void Start()
    {
        // シーン開始時に選択画面を表示
        Show();
        
        // ボタンのイベントを設定
        SetupButtons();
        
        // ブラシボタンのアイコンを設定
        SetupBrushButtonIcons();
        
        // ブラシボタンのイベントを設定
        SetupBrushButtons();
    }
    
    /// <summary>
    /// ブラシボタンのアイコンを設定
    /// </summary>
    private void SetupBrushButtonIcons()
    {
        // Pencil Brushボタン
        if (pencilBrushButton != null && pencilBrush != null)
        {
            Image buttonImage = pencilBrushButton.GetComponent<Image>();
            if (buttonImage != null && pencilBrush.GetIcon() != null)
            {
                buttonImage.sprite = pencilBrush.GetIcon();
            }
        }
        
        // Paint Brushボタン
        if (paintBrushButton != null && paintBrush != null)
        {
            Image buttonImage = paintBrushButton.GetComponent<Image>();
            if (buttonImage != null && paintBrush.GetIcon() != null)
            {
                buttonImage.sprite = paintBrush.GetIcon();
            }
        }
        
        // Spray Brushボタン
        if (sprayBrushButton != null && sprayBrush != null)
        {
            Image buttonImage = sprayBrushButton.GetComponent<Image>();
            if (buttonImage != null && sprayBrush.GetIcon() != null)
            {
                buttonImage.sprite = sprayBrush.GetIcon();
            }
        }
    }
    
    /// <summary>
    /// ブラシボタンのイベントを設定
    /// </summary>
    private void SetupBrushButtons()
    {
        if (pencilBrushButton != null)
        {
            pencilBrushButton.onClick.RemoveAllListeners();
            pencilBrushButton.onClick.AddListener(() => OnBrushSelected(pencilBrush));
        }
        
        if (paintBrushButton != null)
        {
            paintBrushButton.onClick.RemoveAllListeners();
            paintBrushButton.onClick.AddListener(() => OnBrushSelected(paintBrush));
        }
        
        if (sprayBrushButton != null)
        {
            sprayBrushButton.onClick.RemoveAllListeners();
            sprayBrushButton.onClick.AddListener(() => OnBrushSelected(sprayBrush));
        }
    }
    
    /// <summary>
    /// ブラシが選択された時の処理
    /// </summary>
    private void OnBrushSelected(BrushStrategyBase brush)
    {
        if (brush == null)
        {
            Debug.LogWarning("GameModeSelectionPanel.OnBrushSelected: Brush が null です。");
            return;
        }
        
        // BattleSettingsにBrushを設定
        if (BattleSettings.Instance != null)
        {
            BattleSettings.Instance.Current.brush = brush;
            BattleSettings.Instance.Current.brushKey = brush.name;
            Debug.Log($"GameModeSelectionPanel: Brush を選択しました - {brush.name}");
        }
        else
        {
            Debug.LogWarning("GameModeSelectionPanel.OnBrushSelected: BattleSettings.Instance が見つかりません。");
        }
    }
    
    /// <summary>
    /// ボタンのイベントを設定
    /// </summary>
    private void SetupButtons()
    {
        if (creativeButton != null)
        {
            creativeButton.onClick.RemoveAllListeners();
            creativeButton.onClick.AddListener(() => OnModeSelected(SinglePlayerGameModeType.Creative));
        }
        
        if (colorDefenseButton != null)
        {
            colorDefenseButton.onClick.RemoveAllListeners();
            colorDefenseButton.onClick.AddListener(() => OnModeSelected(SinglePlayerGameModeType.ColorDefense));
        }
        
        if (tracingButton != null)
        {
            tracingButton.onClick.RemoveAllListeners();
            tracingButton.onClick.AddListener(() => OnModeSelected(SinglePlayerGameModeType.Tracing));
        }
    }
    
    /// <summary>
    /// 選択画面を表示
    /// </summary>
    public void Show()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// 選択画面を非表示
    /// </summary>
    public void Hide()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// ゲームモードが選択された時の処理
    /// </summary>
    /// <param name="mode">選択されたゲームモード</param>
    public void OnModeSelected(SinglePlayerGameModeType mode)
    {
        Debug.Log($"GameModeSelectionPanel: モード選択 - {mode}");

        // ColorDefense の場合は、直接ゲームを開始せずロビー UI を開く
        if (mode == SinglePlayerGameModeType.ColorDefense && colorDefenseLobbyPanel != null)
        {
            // モード選択画面を隠し、ロビー UI を開く
            Hide();
            colorDefenseLobbyPanel.Open();
        }
        else
        {
            // それ以外のモードは従来通りすぐにゲーム開始
            StartGame(mode);
        }
    }
    
    /// <summary>
    /// ゲームを開始
    /// </summary>
    /// <param name="selectedMode">選択されたゲームモード</param>
    private void StartGame(SinglePlayerGameModeType selectedMode)
    {
        // 選択画面を非表示
        Hide();
        
        // GameModeSwitcherでモードを切り替え（直接モードを渡す）
        if (gameModeSwitcher != null)
        {
            gameModeSwitcher.ApplyMode(selectedMode);
        }
        else
        {
            Debug.LogWarning("GameModeSelectionPanel: GameModeSwitcherが設定されていません");
        }
        
        // SinglePlayerModeManagerでゲームを初期化・開始（直接モードを渡す）
        if (singlePlayerModeManager != null)
        {
            singlePlayerModeManager.InitializeMode(selectedMode);
        }
        else
        {
            Debug.LogWarning("GameModeSelectionPanel: SinglePlayerModeManagerが設定されていません");
        }
        
        Debug.Log("GameModeSelectionPanel: ゲーム開始");
    }
}

