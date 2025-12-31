using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 設定の仕方を説明するTextMeshProコンポーネント
/// インスペクターで設定した文章を指定した間隔で順番に表示し、
/// 切り替え時にフェードイン・フェードアウトアニメーションを適用します
/// </summary>
[System.Serializable]
public class InstructionElement
{
    [Tooltip("表示する文章\n改行は \\n を使用してください\n例: \"1行目\\n2行目\"")]
    public string text = "";
    
    [Tooltip("この文章の表示時間（秒）")]
    [Min(0.1f)]
    public float displayDuration = 3f;
}

public class SettingsInstructionText : MonoBehaviour
{
    [Header("Text Reference")]
    [Tooltip("表示するTextMeshProUGUI（自動取得される）")]
    [SerializeField] private TextMeshProUGUI instructionText;
    
    [Header("Text Settings")]
    [Tooltip("表示する文章と表示時間の配列\n各要素ごとに表示時間を個別に設定できます")]
    [SerializeField] private InstructionElement[] instructionElements = new InstructionElement[0];
    
    [Header("Legacy Settings (後方互換性のため)")]
    [Tooltip("旧バージョンとの互換性のため残しています\ninstructionElementsを使用してください")]
    [SerializeField] private bool useLegacySettings = false;
    
    [Tooltip("表示する文章の配列（旧バージョン用）")]
    [SerializeField] private string[] instructionTexts = new string[0];
    
    [Tooltip("各文章の表示間隔（秒）（旧バージョン用）")]
    [SerializeField] private float displayInterval = 3f;
    
    [Tooltip("フェードイン・フェードアウトの時間（秒）")]
    [SerializeField] private float fadeDuration = 0.5f;
    
    [Tooltip("ループ表示するかどうか")]
    [SerializeField] private bool loopDisplay = true;
    
    [Header("Initial State")]
    [Tooltip("開始時に最初の文章を即座に表示するかどうか")]
    [SerializeField] private bool showFirstTextImmediately = true;
    
    private int currentTextIndex = 0;
    private Coroutine displayCoroutine;
    private Color originalColor;
    
    void Start()
    {
        Initialize();
    }
    
    /// <summary>
    /// 初期化処理
    /// </summary>
    void Initialize()
    {
        // TextMeshProUGUIの自動取得
        if (instructionText == null)
        {
            instructionText = GetComponent<TextMeshProUGUI>();
            if (instructionText == null)
            {
                Debug.LogError("SettingsInstructionText: TextMeshProUGUIコンポーネントが見つかりません。");
                return;
            }
        }
        
        // 元の色を保存
        originalColor = instructionText.color;
        
        // TextMeshProの設定を確認（改行を有効にする）
        if (instructionText != null)
        {
            // ワードラップを有効にする（長い文章の場合）
            instructionText.enableWordWrapping = true;
            // オーバーフローモードを設定（必要に応じて）
            if (instructionText.overflowMode == TextOverflowModes.Overflow)
            {
                instructionText.overflowMode = TextOverflowModes.Truncate;
            }
        }
        
        // 旧バージョンとの互換性チェック
        if (useLegacySettings && (instructionTexts == null || instructionTexts.Length == 0))
        {
            useLegacySettings = false;
        }
        
        // 新しい形式と旧形式の両方をチェック
        bool hasValidElements = instructionElements != null && instructionElements.Length > 0;
        bool hasValidTexts = useLegacySettings && instructionTexts != null && instructionTexts.Length > 0;
        
        if (!hasValidElements && !hasValidTexts)
        {
            Debug.LogWarning("SettingsInstructionText: 表示する文章が設定されていません。");
            return;
        }
        
        // 最初の文章を設定
        string firstText = "";
        if (hasValidElements)
        {
            firstText = instructionElements[0].text;
        }
        else if (hasValidTexts)
        {
            firstText = instructionTexts[0];
        }
        
        if (showFirstTextImmediately && !string.IsNullOrEmpty(firstText))
        {
            instructionText.text = firstText;
            instructionText.color = originalColor; // 完全に表示
        }
        else
        {
            // 最初は透明にする
            Color transparentColor = originalColor;
            transparentColor.a = 0f;
            instructionText.color = transparentColor;
        }
        
        // 表示コルーチンを開始
        displayCoroutine = StartCoroutine(DisplayTextsCoroutine());
    }
    
    /// <summary>
    /// 文章を順番に表示するコルーチン
    /// </summary>
    IEnumerator DisplayTextsCoroutine()
    {
        // 使用するデータを決定
        bool useElements = instructionElements != null && instructionElements.Length > 0;
        bool useTexts = useLegacySettings && instructionTexts != null && instructionTexts.Length > 0;
        
        if (!useElements && !useTexts)
        {
            yield break;
        }
        
        int elementCount = useElements ? instructionElements.Length : instructionTexts.Length;
        
        // 最初の文章を即座に表示しない場合は、最初のフェードインを待つ
        if (!showFirstTextImmediately)
        {
            yield return StartCoroutine(FadeInCoroutine());
        }
        
        // ループ表示
        while (true)
        {
            // 現在の文章を表示
            string currentText = "";
            float currentDuration = displayInterval;
            
            if (useElements && currentTextIndex < instructionElements.Length)
            {
                currentText = instructionElements[currentTextIndex].text;
                currentDuration = instructionElements[currentTextIndex].displayDuration;
            }
            else if (useTexts && currentTextIndex < instructionTexts.Length)
            {
                currentText = instructionTexts[currentTextIndex];
                currentDuration = displayInterval;
            }
            
            if (!string.IsNullOrEmpty(currentText))
            {
                instructionText.text = currentText;
            }
            
            // 表示間隔を待つ（各要素ごとの表示時間を使用）
            yield return new WaitForSeconds(currentDuration);
            
            // フェードアウト
            yield return StartCoroutine(FadeOutCoroutine());
            
            // 次の文章に進む
            currentTextIndex++;
            
            // ループ処理
            if (currentTextIndex >= elementCount)
            {
                if (loopDisplay)
                {
                    currentTextIndex = 0;
                }
                else
                {
                    // ループしない場合は終了
                    break;
                }
            }
            
            // 次の文章を設定
            string nextText = "";
            if (useElements && currentTextIndex < instructionElements.Length)
            {
                nextText = instructionElements[currentTextIndex].text;
            }
            else if (useTexts && currentTextIndex < instructionTexts.Length)
            {
                nextText = instructionTexts[currentTextIndex];
            }
            
            if (!string.IsNullOrEmpty(nextText))
            {
                instructionText.text = nextText;
            }
            
            // フェードイン
            yield return StartCoroutine(FadeInCoroutine());
        }
    }
    
    /// <summary>
    /// フェードインアニメーション
    /// </summary>
    IEnumerator FadeInCoroutine()
    {
        if (instructionText == null) yield break;
        
        float elapsedTime = 0f;
        Color startColor = instructionText.color;
        startColor.a = 0f;
        Color targetColor = originalColor;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            
            Color currentColor = Color.Lerp(startColor, targetColor, t);
            instructionText.color = currentColor;
            
            yield return null;
        }
        
        // 最終的に完全に表示
        instructionText.color = targetColor;
    }
    
    /// <summary>
    /// フェードアウトアニメーション
    /// </summary>
    IEnumerator FadeOutCoroutine()
    {
        if (instructionText == null) yield break;
        
        float elapsedTime = 0f;
        Color startColor = instructionText.color;
        Color targetColor = originalColor;
        targetColor.a = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            
            Color currentColor = Color.Lerp(startColor, targetColor, t);
            instructionText.color = currentColor;
            
            yield return null;
        }
        
        // 最終的に完全に透明
        instructionText.color = targetColor;
    }
    
    /// <summary>
    /// 表示を停止
    /// </summary>
    public void StopDisplay()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }
    }
    
    /// <summary>
    /// 表示を再開
    /// </summary>
    public void ResumeDisplay()
    {
        if (displayCoroutine == null)
        {
            displayCoroutine = StartCoroutine(DisplayTextsCoroutine());
        }
    }
    
    /// <summary>
    /// 表示をリセット（最初の文章から再開）
    /// </summary>
    public void ResetDisplay()
    {
        StopDisplay();
        currentTextIndex = 0;
        
        string firstText = "";
        bool useElements = instructionElements != null && instructionElements.Length > 0;
        bool useTexts = useLegacySettings && instructionTexts != null && instructionTexts.Length > 0;
        
        if (useElements)
        {
            firstText = instructionElements[0].text;
        }
        else if (useTexts)
        {
            firstText = instructionTexts[0];
        }
        
        if (instructionText != null && !string.IsNullOrEmpty(firstText))
        {
            instructionText.text = firstText;
            if (showFirstTextImmediately)
            {
                instructionText.color = originalColor;
            }
            else
            {
                Color transparentColor = originalColor;
                transparentColor.a = 0f;
                instructionText.color = transparentColor;
            }
        }
        
        ResumeDisplay();
    }
    
    void OnDestroy()
    {
        StopDisplay();
    }
}

