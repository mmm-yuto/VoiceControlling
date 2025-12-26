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

    [Header("Animation")]
    [Tooltip("フェードアウトアニメーション用の Animator（selectionPanel にアタッチされている想定）")]
    [SerializeField] private Animator fadeAnimator;

    [Tooltip("フェードアウトアニメーションのトリガー名")]
    [SerializeField] private string fadeOutTriggerName = "FadeOut";

    [Tooltip("アニメーション開始後、次の画面を表示するまでの遅延時間（秒）")]
    [SerializeField] private float transitionDelay = 0.3f;
    
    [Header("Exit Button")]
    [Tooltip("ゲーム終了ボタン")]
    [SerializeField] private Button exitButton;
    
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

        // Animator の自動検索（未設定の場合）
        if (fadeAnimator == null && selectionPanel != null)
        {
            fadeAnimator = selectionPanel.GetComponent<Animator>();
        }
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
        
        // 終了ボタンのイベント設定
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitButtonClicked);
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

        // アニメーションが設定されている場合はフェードアウトを開始
        if (fadeAnimator != null && !string.IsNullOrEmpty(fadeOutTriggerName))
        {
            // フェードアウトアニメーションを開始
            fadeAnimator.SetTrigger(fadeOutTriggerName);
            
            // アニメーション開始後、指定した遅延時間後に次の画面を表示
            StartCoroutine(TransitionToNextScreen(mode));
        }
        else
        {
            // アニメーションが設定されていない場合は従来通り即座に処理
            if (mode == SinglePlayerGameModeType.ColorDefense && colorDefenseLobbyPanel != null)
            {
                Hide();
                colorDefenseLobbyPanel.Open();
            }
            else
            {
                Hide();
                StartGame(mode);
            }
        }
    }
    
    /// <summary>
    /// アニメーション開始後、次の画面に遷移するコルーチン
    /// </summary>
    private System.Collections.IEnumerator TransitionToNextScreen(SinglePlayerGameModeType mode)
    {
        // アニメーションが流れている間に指定した遅延時間を待機
        yield return new WaitForSeconds(transitionDelay);
        
        // 次の画面を表示
        if (mode == SinglePlayerGameModeType.ColorDefense && colorDefenseLobbyPanel != null)
        {
            Hide();
            colorDefenseLobbyPanel.Open();
        }
        else
        {
            Hide();
            StartGame(mode);
        }
    }
    
    /// <summary>
    /// ゲームを開始
    /// </summary>
    /// <param name="selectedMode">選択されたゲームモード</param>
    private void StartGame(SinglePlayerGameModeType selectedMode)
    {
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
    
    /// <summary>
    /// 終了ボタンがクリックされた時の処理
    /// </summary>
    private void OnExitButtonClicked()
    {
        Debug.Log("GameModeSelectionPanel: ゲームを終了します");
        
        #if UNITY_EDITOR
        // エディタの場合は再生を停止
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // ビルド版の場合はアプリケーションを終了
        Application.Quit();
        #endif
    }
}

