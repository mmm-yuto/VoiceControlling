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
    
    [Header("Individual Calibration Buttons")]
    [SerializeField] private Button calibrateMinVolumeButton;
    [SerializeField] private Button calibrateMaxVolumeButton;
    [SerializeField] private Button calibrateMinPitchButton;
    [SerializeField] private Button calibrateMaxPitchButton;

    [Header("Step Messages")]
    [TextArea(2, 4)]
    [SerializeField] private string step1Message = "Step 1: Please remain silent (measuring minimum volume)";
    [TextArea(2, 4)]
    [SerializeField] private string step2Message = "Step 2: Please speak loudly (measuring maximum volume)";
    [TextArea(2, 4)]
    [SerializeField] private string step3Message = "Step 3: Please speak in a low voice (measuring minimum pitch)";
    [TextArea(2, 4)]
    [SerializeField] private string step4Message = "Step 4: Please speak in a high voice (measuring maximum pitch)";

    [Header("Canvas Layer Control")]
    [Tooltip("CalibrationのSettingが開かれているかどうかを監視するSettingsPanelのGameObject")]
    [SerializeField] private GameObject calibrationSettingPanel;
    
    [Tooltip("レイヤーを制御する特定のCanvas")]
    [SerializeField] private Canvas targetCanvas;
    
    [Tooltip("基準となるCalibrationCanvas")]
    [SerializeField] private Canvas calibrationCanvas;

    private int originalSortingOrder = 0;
    private bool wasSettingOpen = false;

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
        
        // 元のレイヤーを保存
        if (targetCanvas != null)
        {
            originalSortingOrder = targetCanvas.sortingOrder;
        }
        
        // 初期状態を確認
        CheckAndUpdateCanvasLayer();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        
        // 無効化時にレイヤーを元に戻す
        RestoreOriginalLayer();
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
        
        // 個別カリブレーションボタン
        if (calibrateMinVolumeButton != null)
        {
            calibrateMinVolumeButton.onClick.RemoveAllListeners();
            calibrateMinVolumeButton.onClick.AddListener(() =>
            {
                if (voiceCalibrator != null)
                {
                    voiceCalibrator.CalibrateMinVolume();
                }
            });
        }
        
        if (calibrateMaxVolumeButton != null)
        {
            calibrateMaxVolumeButton.onClick.RemoveAllListeners();
            calibrateMaxVolumeButton.onClick.AddListener(() =>
            {
                if (voiceCalibrator != null)
                {
                    voiceCalibrator.CalibrateMaxVolume();
                }
            });
        }
        
        if (calibrateMinPitchButton != null)
        {
            calibrateMinPitchButton.onClick.RemoveAllListeners();
            calibrateMinPitchButton.onClick.AddListener(() =>
            {
                if (voiceCalibrator != null)
                {
                    voiceCalibrator.CalibrateMinPitch();
                }
            });
        }
        
        if (calibrateMaxPitchButton != null)
        {
            calibrateMaxPitchButton.onClick.RemoveAllListeners();
            calibrateMaxPitchButton.onClick.AddListener(() =>
            {
                if (voiceCalibrator != null)
                {
                    voiceCalibrator.CalibrateMaxPitch();
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
        
        // カリブレーション中以外はステータスなどのテキストを非表示
        bool isCalibrating = VoiceCalibrator.IsCalibrating || VoiceCalibrator.IsIndividualCalibrating;
        
        if (statusLabel != null)
        {
            statusLabel.gameObject.SetActive(isCalibrating);
        }
        
        if (stepLabel != null)
        {
            stepLabel.gameObject.SetActive(isCalibrating);
        }
        
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(isCalibrating);
        }
    }

    private void UpdateUI()
    {
        // 初期状態ではカリブレーション中ではないので、UI要素を非表示
        bool isCalibrating = VoiceCalibrator.IsCalibrating || VoiceCalibrator.IsIndividualCalibrating;
        
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
            progressSlider.gameObject.SetActive(isCalibrating);
        }

        if (statusLabel != null)
        {
            statusLabel.text = "Please start calibration";
            statusLabel.gameObject.SetActive(isCalibrating);
        }

        if (stepLabel != null)
        {
            stepLabel.text = "";
            stepLabel.gameObject.SetActive(isCalibrating);
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

    private void Update()
    {
        // Settingの状態を監視して、変更があったときにレイヤーを更新
        CheckAndUpdateCanvasLayer();
    }

    /// <summary>
    /// CalibrationのSettingが開かれているかどうかを確認し、Canvasのレイヤーを更新
    /// </summary>
    private void CheckAndUpdateCanvasLayer()
    {
        if (targetCanvas == null || calibrationCanvas == null || calibrationSettingPanel == null)
        {
            return;
        }

        // Settingが開かれているかどうかを確認
        bool isSettingOpen = calibrationSettingPanel.activeInHierarchy;

        // 状態が変わったときのみレイヤーを更新
        if (isSettingOpen != wasSettingOpen)
        {
            if (isSettingOpen)
            {
                // Settingが開かれたとき：特定のCanvasのレイヤーをCalibratonCanvasのレイヤーの一つ下に設定
                int calibrationCanvasOrder = calibrationCanvas.sortingOrder;
                targetCanvas.sortingOrder = calibrationCanvasOrder - 1;
            }
            else
            {
                // Settingが閉じられたとき：特定のCanvasのレイヤーを元に戻す
                RestoreOriginalLayer();
            }
            
            wasSettingOpen = isSettingOpen;
        }
    }

    /// <summary>
    /// 特定のCanvasのレイヤーを元の値に戻す
    /// </summary>
    private void RestoreOriginalLayer()
    {
        if (targetCanvas != null)
        {
            targetCanvas.sortingOrder = originalSortingOrder;
        }
    }
}

