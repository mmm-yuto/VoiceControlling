using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// ColorDefense mode countdown UI component
/// Displays a 3-2-1-GO countdown with scale-up and fade-out animations before battle starts
/// </summary>
public class ColorDefenseCountdownUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Countdown text display")]
    [SerializeField] private TextMeshProUGUI countdownText;
    
    [Tooltip("Root GameObject to control visibility (should have CanvasGroup or be the parent)")]
    [SerializeField] private GameObject countdownRoot;
    
    [Header("Animation Settings")]
    [Tooltip("Duration for each number animation (seconds)")]
    [SerializeField] private float numberAnimationDuration = 1f;
    
    [Tooltip("Initial scale for each number")]
    [SerializeField] private Vector3 initialScale = Vector3.one;
    
    [Tooltip("Final scale for each number (scale up target)")]
    [SerializeField] private Vector3 finalScale = new Vector3(2f, 2f, 2f);
    
    [Tooltip("Duration to show 'GO!' text (seconds)")]
    [SerializeField] private float goTextDuration = 0.5f;
    
    [Header("Instruction Text Settings")]
    [Tooltip("Instruction text to display after countdown (TMP)")]
    [SerializeField] private TextMeshProUGUI instructionText;
    
    [Tooltip("Text content to display in instruction text")]
    [TextArea(2, 5)]
    [SerializeField] private string instructionTextContent = "";
    
    [Tooltip("Duration to show instruction text (seconds)")]
    [SerializeField] private float instructionTextDuration = 2f;
    
    [Tooltip("Duration for one fade out + fade in cycle (seconds)")]
    [SerializeField] private float fadeCycleDuration = 0.5f;
    
    [Header("ColorDefense UI Reference")]
    [Tooltip("ColorDefenseUI to hide after instruction text")]
    [SerializeField] private ColorDefenseUI colorDefenseUI;
    
    private Coroutine currentCountdownCoroutine;
    
    /// <summary>
    /// Start the countdown animation
    /// </summary>
    /// <param name="onCompleted">Callback invoked when countdown finishes</param>
    public void StartCountdown(System.Action onCompleted)
    {
        // Stop any existing countdown
        if (currentCountdownCoroutine != null)
        {
            StopCoroutine(currentCountdownCoroutine);
        }
        
        // Activate the countdown UI
        if (countdownRoot != null)
        {
            countdownRoot.SetActive(true);
        }
        
        // Start the countdown coroutine
        currentCountdownCoroutine = StartCoroutine(CountdownCoroutine(onCompleted));
    }
    
    /// <summary>
    /// Hide the countdown UI
    /// </summary>
    public void Hide()
    {
        if (countdownRoot != null)
        {
            countdownRoot.SetActive(false);
        }
        
        if (countdownText != null)
        {
            countdownText.text = "";
        }
    }
    
    /// <summary>
    /// Countdown coroutine: 3 -> 2 -> 1 -> GO!
    /// </summary>
    private IEnumerator CountdownCoroutine(System.Action onCompleted)
    {
        if (countdownText == null)
        {
            Debug.LogError("ColorDefenseCountdownUI: countdownText is not assigned");
            if (onCompleted != null)
            {
                onCompleted.Invoke();
            }
            yield break;
        }
        
        // Countdown: 3, 2, 1
        for (int i = 3; i >= 1; i--)
        {
            // Display the number
            countdownText.text = i.ToString();
            
            // Reset scale and alpha
            if (countdownText.rectTransform != null)
            {
                countdownText.rectTransform.localScale = initialScale;
            }
            Color textColor = countdownText.color;
            textColor.a = 1f;
            countdownText.color = textColor;
            
            // Animate: scale up and fade out
            yield return StartCoroutine(AnimateNumber(numberAnimationDuration));
            
            // Small pause between numbers (optional, can be removed if not needed)
            yield return new WaitForSeconds(0.1f);
        }
        
        // Show "GO!" text
        countdownText.text = "GO!";
        
        // Reset scale and alpha for GO
        if (countdownText.rectTransform != null)
        {
            countdownText.rectTransform.localScale = initialScale;
        }
        Color goColor = countdownText.color;
        goColor.a = 1f;
        countdownText.color = goColor;
        
        // Invoke completion callback immediately when GO! appears (battle starts now)
        if (onCompleted != null)
        {
            onCompleted.Invoke();
            onCompleted = null; // Prevent double invocation
        }
        
        // Show GO for a short duration (optional animation can be added here)
        yield return new WaitForSeconds(goTextDuration);
        
        // Hide countdown text
        if (countdownText != null)
        {
            countdownText.text = "";
        }
        
        // Show instruction text with fade in/out animation
        if (instructionText != null && instructionTextDuration > 0f)
        {
            yield return StartCoroutine(ShowInstructionTextCoroutine());
        }
        
        // Hide ColorDefenseUI before hiding countdown UI
        if (colorDefenseUI != null)
        {
            colorDefenseUI.gameObject.SetActive(false);
        }
        
        // Hide the countdown UI
        Hide();
    }
    
    /// <summary>
    /// Show instruction text with fade in/out animation
    /// </summary>
    private IEnumerator ShowInstructionTextCoroutine()
    {
        if (instructionText == null)
        {
            yield break;
        }
        
        // Set instruction text content
        if (!string.IsNullOrEmpty(instructionTextContent))
        {
            instructionText.text = instructionTextContent;
        }
        
        // Show instruction text
        instructionText.gameObject.SetActive(true);
        
        // Calculate how many fade cycles we can fit in the duration
        float elapsedTime = 0f;
        float halfCycleDuration = fadeCycleDuration * 0.5f; // Half cycle = fade out or fade in
        
        // Start with fully visible
        Color textColor = instructionText.color;
        textColor.a = 1f;
        instructionText.color = textColor;
        
        while (elapsedTime < instructionTextDuration)
        {
            float cycleTime = elapsedTime % fadeCycleDuration;
            
            if (cycleTime < halfCycleDuration)
            {
                // Fade out (first half of cycle)
                float t = cycleTime / halfCycleDuration;
                textColor.a = Mathf.Lerp(1f, 0f, t);
            }
            else
            {
                // Fade in (second half of cycle)
                float t = (cycleTime - halfCycleDuration) / halfCycleDuration;
                textColor.a = Mathf.Lerp(0f, 1f, t);
            }
            
            instructionText.color = textColor;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure text is fully visible at the end
        textColor.a = 1f;
        instructionText.color = textColor;
        
        // Hide instruction text
        instructionText.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Animate a single number: scale up and fade out
    /// </summary>
    private IEnumerator AnimateNumber(float duration)
    {
        if (countdownText == null || countdownText.rectTransform == null)
        {
            yield break;
        }
        
        float elapsedTime = 0f;
        Vector3 startScale = initialScale;
        Vector3 endScale = finalScale;
        
        Color startColor = countdownText.color;
        Color endColor = startColor;
        endColor.a = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Interpolate scale
            countdownText.rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            // Interpolate alpha
            countdownText.color = Color.Lerp(startColor, endColor, t);
            
            yield return null;
        }
        
        // Ensure final values
        countdownText.rectTransform.localScale = endScale;
        countdownText.color = endColor;
    }
    
    void Awake()
    {
        // Auto-find references if not set
        if (countdownText == null)
        {
            countdownText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (countdownRoot == null)
        {
            countdownRoot = gameObject;
        }
        
        // Auto-find instruction text if not set
        if (instructionText == null)
        {
            // Try to find by name "InstructionText"
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (text.name.Contains("InstructionText") || text.name.Contains("Instruction"))
                {
                    instructionText = text;
                    break;
                }
            }
        }
        
        // Auto-find ColorDefenseUI if not set
        if (colorDefenseUI == null)
        {
            colorDefenseUI = FindObjectOfType<ColorDefenseUI>();
        }
        
        // Hide by default
        Hide();
        
        // Hide instruction text by default
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
        }
    }
}

