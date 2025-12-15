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
        
        // Show GO for a short duration (optional animation can be added here)
        yield return new WaitForSeconds(goTextDuration);
        
        // Hide the UI
        Hide();
        
        // Invoke completion callback
        if (onCompleted != null)
        {
            onCompleted.Invoke();
        }
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
        
        // Hide by default
        Hide();
    }
}

