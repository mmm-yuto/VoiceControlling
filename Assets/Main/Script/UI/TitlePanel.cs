using UnityEngine;

/// <summary>
/// タイトル画面UIパネル
/// マウスクリックまたはキーボード入力で次のパネルに遷移する
/// </summary>
public class TitlePanel : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("タイトル画面のルートオブジェクト")]
    [SerializeField] private GameObject titlePanel;
    
    [Tooltip("次のパネル（表示するオブジェクト、通常は設定画面）")]
    [SerializeField] private GameObject nextPanel;
    
    [Tooltip("ゲームモード選択パネル（セーブデータがある場合に直接遷移）")]
    [SerializeField] private GameModeSelectionPanel gameModeSelectionPanel;
    
    [Header("Objects to Hide During Title")]
    [Tooltip("タイトル画面中に非表示にしておきたいオブジェクトのリスト")]
    [SerializeField] private GameObject[] objectsToHideDuringTitle;
    
    [Header("Animation")]
    [Tooltip("フェードアウトアニメーション用の Animator（titlePanel にアタッチされている想定）")]
    [SerializeField] private Animator fadeAnimator;
    
    [Tooltip("フェードアウトアニメーションのトリガー名")]
    [SerializeField] private string fadeOutTriggerName = "FadeOut";
    
    [Tooltip("アニメーション開始後、次の画面を表示するまでの遅延時間（秒）")]
    [SerializeField] private float transitionDelay = 0.3f;
    
    private bool hasTransitioned = false;
    
    void Start()
    {
        // タイトル画面を表示
        Show();
        
        // タイトル画面中に非表示にするオブジェクトを非表示にする
        HideObjectsDuringTitle();
        
        // Animator の自動検索（未設定の場合）
        if (fadeAnimator == null && titlePanel != null)
        {
            fadeAnimator = titlePanel.GetComponent<Animator>();
        }
        
        // GameModeSelectionPanelの自動検索（未設定の場合）
        if (gameModeSelectionPanel == null)
        {
            gameModeSelectionPanel = FindObjectOfType<GameModeSelectionPanel>();
        }
        
        // セーブデータがある場合は、nextPanelをGameModeSelectionPanelに設定
        if (CalibrationSaveSystem.HasCalibrationData())
        {
            Debug.Log("TitlePanel: セーブデータが見つかりました。ゲームモード選択画面に直接遷移します。");
            // nextPanelをGameModeSelectionPanelに変更（一時的に）
            if (gameModeSelectionPanel != null)
            {
                // 一時的にnextPanelをGameModeSelectionPanelのGameObjectに設定
                // ただし、元のnextPanelは保持しておく（設定画面に戻る場合に備えて）
                // ここでは直接GameModeSelectionPanelを表示するロジックを使用
            }
        }
        else
        {
            Debug.Log("TitlePanel: セーブデータが見つかりませんでした。設定画面に遷移します。");
        }
    }
    
    void Update()
    {
        // 既に遷移済みの場合は処理しない
        if (hasTransitioned)
            return;
        
        // マウスクリックまたはキーボード入力を検知
        if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
        {
            TransitionToNextPanel();
        }
    }
    
    /// <summary>
    /// タイトル画面を表示
    /// </summary>
    public void Show()
    {
        if (titlePanel != null)
        {
            titlePanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// タイトル画面を非表示
    /// </summary>
    public void Hide()
    {
        if (titlePanel != null)
        {
            titlePanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// タイトル画面中に非表示にするオブジェクトを非表示にする
    /// </summary>
    private void HideObjectsDuringTitle()
    {
        if (objectsToHideDuringTitle == null)
            return;
        
        foreach (GameObject obj in objectsToHideDuringTitle)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// タイトル画面中に非表示にしていたオブジェクトを表示する
    /// </summary>
    private void ShowObjectsAfterTitle()
    {
        if (objectsToHideDuringTitle == null)
            return;
        
        foreach (GameObject obj in objectsToHideDuringTitle)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// 次のパネルに遷移する
    /// </summary>
    private void TransitionToNextPanel()
    {
        if (hasTransitioned)
            return;
        
        hasTransitioned = true;
        
        // アニメーションが設定されている場合はフェードアウトを開始
        if (fadeAnimator != null && !string.IsNullOrEmpty(fadeOutTriggerName))
        {
            // フェードアウトアニメーションを開始
            fadeAnimator.SetTrigger(fadeOutTriggerName);
            
            // アニメーション開始後、指定した遅延時間後に次の画面を表示
            StartCoroutine(TransitionToNextPanelCoroutine());
        }
        else
        {
            // アニメーションが設定されていない場合は即座に遷移
            Hide();
            ShowObjectsAfterTitle();
            
            ShowNextPanel();
        }
    }
    
    /// <summary>
    /// アニメーション開始後、次の画面に遷移するコルーチン
    /// </summary>
    private System.Collections.IEnumerator TransitionToNextPanelCoroutine()
    {
        // アニメーションが流れている間に指定した遅延時間を待機
        yield return new WaitForSeconds(transitionDelay);
        
        // タイトル画面を非表示
        Hide();
        
        // タイトル画面中に非表示にしていたオブジェクトを表示
        ShowObjectsAfterTitle();
        
        // 次のパネルを表示
        ShowNextPanel();
    }
    
    /// <summary>
    /// 次のパネルを表示
    /// </summary>
    private void ShowNextPanel()
    {
        // セーブデータがある場合は、GameModeSelectionPanelに直接遷移
        if (CalibrationSaveSystem.HasCalibrationData() && gameModeSelectionPanel != null)
        {
            // ゲームモード選択画面を表示
            gameModeSelectionPanel.Show();
            Debug.Log("TitlePanel: ゲームモード選択画面に遷移しました（セーブデータあり）");
        }
        else if (nextPanel != null)
        {
            // セーブデータがない場合は、従来通り設定画面に遷移
            // オブジェクトを表示
            nextPanel.SetActive(true);
            
            // SettingsPanelコンポーネントがある場合はShow()も呼び出す
            SettingsPanel settingsPanel = nextPanel.GetComponent<SettingsPanel>();
            if (settingsPanel != null)
            {
                settingsPanel.Show();
            }
            Debug.Log("TitlePanel: 設定画面に遷移しました（セーブデータなし）");
        }
        else
        {
            Debug.LogWarning("TitlePanel: nextPanel が設定されていません");
        }
    }
}

