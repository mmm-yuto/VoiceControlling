using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalibrationPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoiceCalibrator voiceCalibrator;

    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private TextMeshProUGUI stepLabel;

    [Header("Step Messages")]
    [TextArea(2, 4)]
    [SerializeField] private string step1Message = "Step 1: 何も声を出さないでください（音量の最小値を測定）";
    [TextArea(2, 4)]
    [SerializeField] private string step2Message = "Step 2: 大きな声を出してください（音量の最大値を測定）";
    [TextArea(2, 4)]
    [SerializeField] private string step3Message = "Step 3: 低い声を出してください（ピッチの最小値を測定）";
    [TextArea(2, 4)]
    [SerializeField] private string step4Message = "Step 4: 高い声を出してください（ピッチの最大値を測定）";

    private void Awake()
    {
        if (voiceCalibrator == null)
        {
            voiceCalibrator = FindObjectOfType<VoiceCalibrator>();
        }
    }

    private void OnEnable()
    {
        SubscribeToEvents();
        BindButtonCallbacks();
        UpdateUI();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (voiceCalibrator != null)
        {
            VoiceCalibrator.OnCalibrationStatusUpdated += UpdateStatus;
            VoiceCalibrator.OnCalibrationProgressUpdated += UpdateProgress;
            VoiceCalibrator.OnCalibrationRunningStateChanged += UpdateRunningState;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (voiceCalibrator != null)
        {
            VoiceCalibrator.OnCalibrationStatusUpdated -= UpdateStatus;
            VoiceCalibrator.OnCalibrationProgressUpdated -= UpdateProgress;
            VoiceCalibrator.OnCalibrationRunningStateChanged -= UpdateRunningState;
        }
    }

    private void BindButtonCallbacks()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                if (voiceCalibrator != null)
                {
                    voiceCalibrator.StartCalibration();
                }
            });
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() =>
            {
                if (voiceCalibrator != null)
                {
                    voiceCalibrator.CancelCalibration();
                }
            });
        }
    }

    private void UpdateStatus(string status)
    {
        if (statusLabel != null)
        {
            statusLabel.text = status;
        }
    }

    private void UpdateProgress(float progress)
    {
        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }
    }

    private void UpdateRunningState(bool isRunning)
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

    private void UpdateUI()
    {
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }

        if (statusLabel != null)
        {
            statusLabel.text = "カリブレーションを開始してください";
        }

        if (stepLabel != null)
        {
            stepLabel.text = "";
        }
    }

    public void UpdateStepLabel(int stepIndex)
    {
        if (stepLabel == null) return;

        string message = stepIndex switch
        {
            1 => step1Message,
            2 => step2Message,
            3 => step3Message,
            4 => step4Message,
            _ => ""
        };

        stepLabel.text = message;
    }
}

