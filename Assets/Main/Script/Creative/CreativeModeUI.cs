using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// クリエイティブモード用のUI制御
/// ツール切り替え、色選択、履歴操作、表示更新を担当
/// </summary>
public class CreativeModeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CreativeModeManager creativeModeManager;
    [SerializeField] private ColorSelectionSystem colorSelectionSystem;
    [SerializeField] private ColorSelectionSettings colorSelectionSettings;

    [Header("Tool Buttons")]
    [SerializeField] private Button paintToolButton;
    [SerializeField] private Button eraserToolButton;

    [Header("General Actions")]
    [SerializeField] private Button clearButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button nextColorButton;
    [SerializeField] private Button previousColorButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button shareButton;

    [Header("Color UI")]
    [SerializeField] private Button colorPickerToggleButton;
    [SerializeField] private GameObject colorPickerPanel;
    [SerializeField] private Transform presetColorContainer;
    [SerializeField] private GameObject presetColorButtonPrefab;

    [Header("Display")]
    [SerializeField] private Image currentColorPreview;
    [SerializeField] private TextMeshProUGUI toolStateLabel;
    [SerializeField] private TextMeshProUGUI undoStateLabel;
    [SerializeField] private TextMeshProUGUI saveStatusLabel;

    readonly List<Button> presetButtons = new List<Button>();
    CreativeModeSaveSystem saveSystem;

    void Awake()
    {
        ResolveReferences();
    }

    void OnEnable()
    {
        SubscribeToEvents();
        BindButtonCallbacks();
        BuildPresetButtons();
        UpdateToolUI(creativeModeManager != null ? creativeModeManager.CurrentToolMode : CreativeToolMode.Paint);
        UpdateColorUI(colorSelectionSystem != null ? colorSelectionSystem.GetCurrentColor() : Color.white);
        UpdateUndoUI(creativeModeManager != null && creativeModeManager.CanUndo());
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
        UnbindButtonCallbacks();
    }

    void ResolveReferences()
    {
        if (creativeModeManager == null)
        {
            creativeModeManager = FindObjectOfType<CreativeModeManager>();
        }

        if (colorSelectionSystem == null)
        {
            colorSelectionSystem = FindObjectOfType<ColorSelectionSystem>();
        }

        if (colorSelectionSystem != null)
        {
            colorSelectionSettings = colorSelectionSystem.Settings;
        }

        if (saveSystem == null)
        {
            saveSystem = FindObjectOfType<CreativeModeSaveSystem>();
        }

        if (colorPickerPanel != null)
        {
            bool initialActive = colorSelectionSettings != null
                ? colorSelectionSettings.colorPickerVisibleByDefault
                : colorPickerPanel.activeSelf;
            colorPickerPanel.SetActive(initialActive);
        }
    }

    void SubscribeToEvents()
    {
        CreativeModeManager.OnToolModeChanged += UpdateToolUI;
        CreativeModeManager.OnColorChanged += UpdateColorUI;
        CreativeModeManager.OnUndoAvailabilityChanged += UpdateUndoUI;

        if (colorSelectionSystem != null)
        {
            colorSelectionSystem.OnColorSelected += UpdateColorUI;
            colorSelectionSystem.OnColorIndexChanged += HighlightPresetButton;
        }

        CreativeModeSaveSystem.OnImageSaved += HandleImageSaved;
        CreativeModeSaveSystem.OnShareCompleted += HandleShareCompleted;
    }

    void UnsubscribeFromEvents()
    {
        CreativeModeManager.OnToolModeChanged -= UpdateToolUI;
        CreativeModeManager.OnColorChanged -= UpdateColorUI;
        CreativeModeManager.OnUndoAvailabilityChanged -= UpdateUndoUI;

        if (colorSelectionSystem != null)
        {
            colorSelectionSystem.OnColorSelected -= UpdateColorUI;
            colorSelectionSystem.OnColorIndexChanged -= HighlightPresetButton;
        }

        CreativeModeSaveSystem.OnImageSaved -= HandleImageSaved;
        CreativeModeSaveSystem.OnShareCompleted -= HandleShareCompleted;
    }

    void BindButtonCallbacks()
    {
        if (paintToolButton != null)
        {
            paintToolButton.onClick.AddListener(() => creativeModeManager?.SetToolMode(CreativeToolMode.Paint));
        }

        if (eraserToolButton != null)
        {
            eraserToolButton.onClick.AddListener(() => creativeModeManager?.SetToolMode(CreativeToolMode.Eraser));
        }

        if (clearButton != null)
        {
            clearButton.onClick.AddListener(() => creativeModeManager?.ClearCanvas());
        }

        if (undoButton != null)
        {
            undoButton.onClick.AddListener(() => creativeModeManager?.Undo());
        }

        if (nextColorButton != null)
        {
            nextColorButton.onClick.AddListener(() => colorSelectionSystem?.SelectNextColor());
        }

        if (previousColorButton != null)
        {
            previousColorButton.onClick.AddListener(() => colorSelectionSystem?.SelectPreviousColor());
        }

        if (colorPickerToggleButton != null && colorPickerPanel != null)
        {
            colorPickerToggleButton.onClick.AddListener(() =>
            {
                bool newState = !colorPickerPanel.activeSelf;
                colorPickerPanel.SetActive(newState);
            });
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(() => saveSystem?.SaveCanvas());
        }

        if (shareButton != null)
        {
            shareButton.onClick.AddListener(() => saveSystem?.ShareImage());
        }
    }

    void UnbindButtonCallbacks()
    {
        paintToolButton?.onClick.RemoveAllListeners();
        eraserToolButton?.onClick.RemoveAllListeners();
        clearButton?.onClick.RemoveAllListeners();
        undoButton?.onClick.RemoveAllListeners();
        nextColorButton?.onClick.RemoveAllListeners();
        previousColorButton?.onClick.RemoveAllListeners();
        colorPickerToggleButton?.onClick.RemoveAllListeners();
        saveButton?.onClick.RemoveAllListeners();
        shareButton?.onClick.RemoveAllListeners();
    }

    void BuildPresetButtons()
    {
        if (presetColorContainer == null || presetColorButtonPrefab == null || colorSelectionSystem == null)
        {
            return;
        }

        foreach (Transform child in presetColorContainer)
        {
            Destroy(child.gameObject);
        }
        presetButtons.Clear();

        var palette = colorSelectionSystem.GetAvailableColors();
        for (int i = 0; i < palette.Count; i++)
        {
            int index = i;
            GameObject buttonObj = Instantiate(presetColorButtonPrefab, presetColorContainer);
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => colorSelectionSystem.SelectColorByIndex(index));
                presetButtons.Add(button);
            }

            var image = buttonObj.GetComponent<Image>();
            if (image != null)
            {
                image.color = palette[index];
            }
        }

        HighlightPresetButton(colorSelectionSystem.GetCurrentIndex());
    }

    void HighlightPresetButton(int index)
    {
        for (int i = 0; i < presetButtons.Count; i++)
        {
            Transform buttonTransform = presetButtons[i].transform;
            if (buttonTransform != null)
            {
                buttonTransform.localScale = i == index ? Vector3.one * 1.05f : Vector3.one;
            }
        }
    }

    void UpdateToolUI(CreativeToolMode mode)
    {
        toolStateLabel?.SetText(mode == CreativeToolMode.Paint ? "塗りモード" : "消しゴムモード");

        if (paintToolButton != null)
        {
            paintToolButton.interactable = mode != CreativeToolMode.Paint;
        }

        if (eraserToolButton != null)
        {
            eraserToolButton.interactable = mode != CreativeToolMode.Eraser;
        }
    }

    void UpdateColorUI(Color color)
    {
        if (currentColorPreview != null)
        {
            currentColorPreview.color = color;
        }
    }

    void UpdateUndoUI(bool canUndo)
    {
        if (undoButton != null)
        {
            undoButton.interactable = canUndo;
        }

        undoStateLabel?.SetText(canUndo ? "Undo可能" : "Undo不可");
    }

    void HandleImageSaved(string path)
    {
        if (saveStatusLabel != null)
        {
            saveStatusLabel.SetText($"保存: {path}");
        }
    }

    void HandleShareCompleted(bool success)
    {
        if (saveStatusLabel != null)
        {
            saveStatusLabel.SetText(success ? "共有成功" : "共有失敗");
        }
    }
}

