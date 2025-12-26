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
    
    void Start()
    {
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
        
        // ColorDefense中に表示するオブジェクトを表示
        SetColorDefenseObjectsVisibility(true);
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
    }
    
    private void UpdateScore(int score)
    {
        currentScore = score;
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
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

