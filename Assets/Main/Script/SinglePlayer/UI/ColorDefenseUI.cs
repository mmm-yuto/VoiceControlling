using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// カラーディフェンスモード専用UI
/// </summary>
public class ColorDefenseUI : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    
    [Header("Area Status")]
    [SerializeField] private TextMeshProUGUI activeAreasText;
    [SerializeField] private Slider[] areaProgressBars; // 各領域の進行度（オプション）
    
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI defendedAreasText;
    [SerializeField] private TextMeshProUGUI changedAreasText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI ratioText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Navigation")]
    [Tooltip("タイトルに戻るボタン")]
    [SerializeField] private Button backToTitleButton;
    
    [Tooltip("タイトルシーン名（インスペクターで設定）")]
    [SerializeField] private string titleSceneName = "00_MainMenu";
    
    [Header("ColorDefense Objects")]
    [Tooltip("ColorDefense中に常に表示するオブジェクト（インスペクターで設定）")]
    [SerializeField] private GameObject[] colorDefenseObjects;
    
    private int currentScore = 0;
    private int currentCombo = 0;
    private int defendedAreasCount = 0;
    private int changedAreasCount = 0;
    private ColorDefenseMode colorDefenseMode;
    private bool objectsVisible = false; // オブジェクトが表示されているかどうか
    
    void Start()
    {
        // 初期状態ではオブジェクトを非表示
        SetColorDefenseObjectsVisibility(false);
        
        // ColorDefenseModeを検索
        colorDefenseMode = FindObjectOfType<ColorDefenseMode>();
        
        // イベント購読
        ColorDefenseMode.OnScoreUpdated += UpdateScore;
        ColorDefenseMode.OnComboUpdated += UpdateCombo;
        ColorDefenseMode.OnAreaSpawned += OnAreaSpawned;
        ColorDefenseMode.OnAreaDefended += OnAreaDefended;
        ColorDefenseMode.OnAreaChanged += OnAreaChanged;
        ColorDefenseMode.OnGameEnded += OnGameEnded;
        
        // ボタン設定
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (backToTitleButton != null)
            backToTitleButton.onClick.AddListener(OnBackToTitleClicked);
        
        // ゲームオーバーパネルを非表示
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    
    void OnDisable()
    {
        // ColorDefenseモードが無効化されたときにオブジェクトを非表示
        SetColorDefenseObjectsVisibility(false);
        objectsVisible = false;
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        ColorDefenseMode.OnScoreUpdated -= UpdateScore;
        ColorDefenseMode.OnComboUpdated -= UpdateCombo;
        ColorDefenseMode.OnAreaSpawned -= OnAreaSpawned;
        ColorDefenseMode.OnAreaDefended -= OnAreaDefended;
        ColorDefenseMode.OnAreaChanged -= OnAreaChanged;
        ColorDefenseMode.OnGameEnded -= OnGameEnded;
        
        // ColorDefense中に表示していたオブジェクトを非表示
        SetColorDefenseObjectsVisibility(false);
        objectsVisible = false;
    }
    
    void Update()
    {
        // ColorDefenseModeが存在して、ゲームが開始されたときにオブジェクトを表示
        if (!objectsVisible && colorDefenseMode != null && colorDefenseMode.IsGameActive())
        {
            SetColorDefenseObjectsVisibility(true);
            objectsVisible = true;
        }
        // ゲームが終了したらオブジェクトを非表示（ゲーム終了時のみ）
        else if (objectsVisible && colorDefenseMode != null && !colorDefenseMode.IsGameActive())
        {
            // ゲームが一時停止されているだけかもしれないので、ここでは非表示にしない
            // EndGame()が呼ばれたときのみ非表示にする（OnGameEndedイベントで処理）
        }
    }
    
    private void UpdateScore(int score)
    {
        currentScore = score;
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
        
        // 最初のスコア更新（ゲーム開始時）にオブジェクトを表示
        if (!objectsVisible)
        {
            SetColorDefenseObjectsVisibility(true);
            objectsVisible = true;
        }
    }
    
    private void UpdateCombo(int combo)
    {
        currentCombo = combo;
        if (comboText != null)
        {
            comboText.text = $"Combo: {combo}";
        }
    }
    
    private void OnAreaSpawned(ColorChangeArea area)
    {
        UpdateActiveAreasCount();
    }
    
    private void OnAreaDefended(ColorChangeArea area)
    {
        defendedAreasCount++;
        UpdateActiveAreasCount();
    }
    
    private void OnAreaChanged(ColorChangeArea area)
    {
        changedAreasCount++;
        UpdateActiveAreasCount();
    }
    
    private void UpdateActiveAreasCount()
    {
        // 現在のアクティブな領域数を表示
        if (activeAreasText != null && colorDefenseMode != null)
        {
            int activeCount = colorDefenseMode.GetActiveAreasCount();
            activeAreasText.text = $"Active Areas: {activeCount}";
        }
    }
    
    public void ShowGameOver(int finalScore, int defendedCount, int changedCount)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (finalScoreText != null)
                finalScoreText.text = $"Final Score: {finalScore}";
            if (defendedAreasText != null)
                defendedAreasText.text = $"Defended: {defendedCount}";
            if (changedAreasText != null)
                changedAreasText.text = $"Changed: {changedCount}";
        }
    }
    
    private void OnGameEnded(ColorDefenseMode.GameResult result, float playerRatio, float enemyRatio)
    {
        // 既存のスコア情報と合わせてゲームオーバーパネルを表示
        ShowGameOver(currentScore, defendedAreasCount, changedAreasCount);
        
        if (resultText != null)
        {
                switch (result)
                {
                    case ColorDefenseMode.GameResult.PlayerWin:
                        resultText.text = "Result: WIN";
                        break;
                    case ColorDefenseMode.GameResult.EnemyWin:
                        resultText.text = "Result: LOSE";
                        break;
                    default:
                        resultText.text = "Result: DRAW";
                        break;
                }
        }
        
        if (ratioText != null)
        {
            int playerPercent = Mathf.RoundToInt(playerRatio * 100f);
            int enemyPercent = Mathf.RoundToInt(enemyRatio * 100f);
            ratioText.text = $"Player {playerPercent}% : Enemy {enemyPercent}%";
        }
        
        // GameOverPanelの色を勝利したプレイヤーの色に設定
        SetGameOverPanelColor(result);
        
        // ゲーム終了時にオブジェクトを非表示（オプション：必要に応じてコメントアウト）
        // SetColorDefenseObjectsVisibility(false);
        // objectsVisible = false;
    }
    
    /// <summary>
    /// GameOverPanelの背景色を勝利したプレイヤーの色に設定
    /// </summary>
    private void SetGameOverPanelColor(ColorDefenseMode.GameResult result)
    {
        if (gameOverPanel == null) return;
        
        // BattleSettingsから色を取得
        BattleSettings battleSettings = BattleSettings.Instance;
        if (battleSettings == null || battleSettings.Current == null)
        {
            return;
        }
        
        Color winnerColor;
        switch (result)
        {
            case ColorDefenseMode.GameResult.PlayerWin:
                // プレイヤーが勝利した場合、プレイヤーの色を使用
                winnerColor = battleSettings.Current.playerColor;
                break;
            case ColorDefenseMode.GameResult.EnemyWin:
                // 敵が勝利した場合、敵の色を使用
                winnerColor = battleSettings.Current.cpuColor;
                break;
            default:
                // 引き分けの場合はデフォルト色（白または半透明）を使用
                winnerColor = Color.white;
                break;
        }
        
        // GameOverPanelのImageコンポーネントを取得して色を設定
        UnityEngine.UI.Image panelImage = gameOverPanel.GetComponent<UnityEngine.UI.Image>();
        if (panelImage != null)
        {
            // 透明度を保持しながら色を設定（アルファ値を保持）
            Color currentColor = panelImage.color;
            winnerColor.a = currentColor.a; // 既存の透明度を保持
            panelImage.color = winnerColor;
        }
    }
    
    private void OnRetryClicked()
    {
        // ゲームを再開（SinglePlayerModeManagerに通知）
        UnityEngine.SceneManagement.SceneManager.LoadScene("01_Gameplay");
    }
    
    private void OnMainMenuClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("00_MainMenu");
    }
    
    /// <summary>
    /// タイトルに戻るボタンがクリックされた時の処理
    /// </summary>
    private void OnBackToTitleClicked()
    {
        if (!string.IsNullOrEmpty(titleSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
        }
        else
        {
            Debug.LogWarning("ColorDefenseUI: タイトルシーン名が設定されていません");
        }
    }
    
    /// <summary>
    /// ColorDefense中に表示するオブジェクトの表示/非表示を設定
    /// </summary>
    /// <param name="visible">true = 表示、false = 非表示</param>
    private void SetColorDefenseObjectsVisibility(bool visible)
    {
        if (colorDefenseObjects == null || colorDefenseObjects.Length == 0)
        {
            return;
        }
        
        foreach (GameObject obj in colorDefenseObjects)
        {
            if (obj != null)
            {
                obj.SetActive(visible);
            }
        }
    }
}

