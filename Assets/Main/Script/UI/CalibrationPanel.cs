using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// キャリブレーションの UI 表示と操作を担当するパネル
/// </summary>
public class CalibrationPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoiceCalibrator voiceCalibrator;
    [SerializeField] private Button startButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI statusLabel;

    void Awake()
    {
        if (voiceCalibrator == null)
        {
            voiceCalibrator = FindObjectOfType<VoiceCalibrator>();
        }
    }

    void OnEnable()
    {
        if (voiceCalibrator == null)
        {
            Debug.LogWarning("CalibrationPanel: VoiceCalibrator が見つかりませんでした。");
            return;
        }

        voiceCalibrator.OnStatusChanged += HandleStatusChanged;
        voiceCalibrator.OnProgressChanged += HandleProgressChanged;
        voiceCalibrator.OnCalibrationRunningChanged += HandleRunningStateChanged;

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    void OnDisable()
    {
        if (voiceCalibrator != null)
        {
            voiceCalibrator.OnStatusChanged -= HandleStatusChanged;
            voiceCalibrator.OnProgressChanged -= HandleProgressChanged;
            voiceCalibrator.OnCalibrationRunningChanged -= HandleRunningStateChanged;
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelClicked);
        }
    }

    void OnStartClicked()
    {
        voiceCalibrator?.StartCalibration();
    }

    void OnCancelClicked()
    {
        voiceCalibrator?.CancelCalibration();
    }

    void HandleStatusChanged(string message)
    {
        if (statusLabel != null)
        {
            statusLabel.text = message;
        }
    }

    void HandleProgressChanged(float value)
    {
        if (progressSlider != null)
        {
            progressSlider.value = Mathf.Clamp01(value);
        }
    }

    void HandleRunningStateChanged(bool isRunning)
    {
        if (startButton != null)
        {
            startButton.interactable = !isRunning;
        }

        if (cancelButton != null)
        {
            cancelButton.interactable = isRunning;
        }
    }
}

