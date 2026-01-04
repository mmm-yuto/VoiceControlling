using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// オフライン/オンライン選択UIパネル
/// タイトル画面から遷移し、オフライン/オンラインを選択してからゲームセレクト画面に遷移する
/// </summary>
public class OnlineOfflineSelectionPanel : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("選択画面のルートオブジェクト")]
    [SerializeField] private GameObject selectionPanel;
    
    [Tooltip("オフラインモード選択ボタン")]
    [SerializeField] private Button offlineButton;
    
    [Tooltip("オンラインモード選択ボタン")]
    [SerializeField] private Button onlineButton;
    
    [Tooltip("戻るボタン（タイトルに戻る）")]
    [SerializeField] private Button backButton;
    
    [Header("Navigation")]
    [Tooltip("ゲームモード選択パネル")]
    [SerializeField] private GameModeSelectionPanel gameModeSelectionPanel;
    
    [Tooltip("タイトルパネル（戻るボタン用）")]
    [SerializeField] private TitlePanel titlePanel;
    
    [Header("Animation")]
    [Tooltip("フェードアウトアニメーション用の Animator")]
    [SerializeField] private Animator fadeAnimator;
    
    [Tooltip("フェードアウトアニメーションのトリガー名")]
    [SerializeField] private string fadeOutTriggerName = "FadeOut";
    
    [Tooltip("アニメーション開始後、次の画面を表示するまでの遅延時間（秒）")]
    [SerializeField] private float transitionDelay = 0.3f;
    
    void Start()
    {
        // ボタンのイベントを設定
        SetupButtons();
        
        // Animator の自動検索（未設定の場合）
        if (fadeAnimator == null && selectionPanel != null)
        {
            fadeAnimator = selectionPanel.GetComponent<Animator>();
        }
        
        // GameModeSelectionPanelの自動検索（未設定の場合）
        if (gameModeSelectionPanel == null)
        {
            gameModeSelectionPanel = FindObjectOfType<GameModeSelectionPanel>();
        }
        
        // TitlePanelの自動検索（未設定の場合）
        if (titlePanel == null)
        {
            titlePanel = FindObjectOfType<TitlePanel>();
        }
    }
    
    /// <summary>
    /// ボタンのイベントを設定
    /// </summary>
    private void SetupButtons()
    {
        if (offlineButton != null)
        {
            offlineButton.onClick.RemoveAllListeners();
            offlineButton.onClick.AddListener(() => OnModeSelected(false)); // false = オフライン
        }
        
        if (onlineButton != null)
        {
            onlineButton.onClick.RemoveAllListeners();
            onlineButton.onClick.AddListener(() => OnModeSelected(true)); // true = オンライン
        }
        
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
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
    /// オフライン/オンラインが選択された時の処理
    /// </summary>
    /// <param name="isOnline">true = オンライン、false = オフライン</param>
    private void OnModeSelected(bool isOnline)
    {
        Debug.Log($"OnlineOfflineSelectionPanel: モード選択 - {(isOnline ? "オンライン" : "オフライン")}");
        
        // GameModeManagerに選択を保存
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetOnlineMode(isOnline);
        }
        else
        {
            Debug.LogWarning("OnlineOfflineSelectionPanel: GameModeManager.Instance が見つかりません");
        }
        
        // アニメーションが設定されている場合はフェードアウトを開始
        if (fadeAnimator != null && !string.IsNullOrEmpty(fadeOutTriggerName))
        {
            fadeAnimator.SetTrigger(fadeOutTriggerName);
            StartCoroutine(TransitionToGameSelectionCoroutine());
        }
        else
        {
            // アニメーションが設定されていない場合は即座に遷移
            Hide();
            ShowGameSelection();
        }
    }
    
    /// <summary>
    /// アニメーション開始後、ゲームセレクト画面に遷移するコルーチン
    /// </summary>
    private System.Collections.IEnumerator TransitionToGameSelectionCoroutine()
    {
        yield return new WaitForSeconds(transitionDelay);
        
        Hide();
        ShowGameSelection();
    }
    
    /// <summary>
    /// ゲームセレクト画面を表示
    /// </summary>
    private void ShowGameSelection()
    {
        if (gameModeSelectionPanel != null)
        {
            gameModeSelectionPanel.Show();
            Debug.Log("OnlineOfflineSelectionPanel: ゲームセレクト画面に遷移しました");
        }
        else
        {
            Debug.LogWarning("OnlineOfflineSelectionPanel: GameModeSelectionPanelが設定されていません");
        }
    }
    
    /// <summary>
    /// 戻るボタンがクリックされた時の処理
    /// </summary>
    private void OnBackButtonClicked()
    {
        Debug.Log("OnlineOfflineSelectionPanel: タイトルに戻ります");
        
        Hide();
        
        if (titlePanel != null)
        {
            titlePanel.Show();
        }
    }
}

