using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// クリエイティブモードのUI管理
/// ボタン、ラベル、イベント処理を担当
/// </summary>
public class CreativeModeUI : MonoBehaviour
{
    [Header("Manager References")]
    [Tooltip("クリエイティブモードマネージャー（Inspectorで接続）")]
    [SerializeField] private CreativeModeManager creativeModeManager;
    
    [Tooltip("色選択システム（Inspectorで接続）")]
    [SerializeField] private ColorSelectionSystem colorSelectionSystem;
    
    [Tooltip("保存システム（Inspectorで接続、オプション）")]
    [SerializeField] private CreativeModeSaveSystem saveSystem;
    
    [Header("Tool Buttons")]
    [Tooltip("塗りツールボタン")]
    [SerializeField] private Button paintToolButton;
    
    [Tooltip("消しツールボタン")]
    [SerializeField] private Button eraserToolButton;
    
    [Header("Brush Buttons")]
    [Tooltip("ブラシボタンのコンテナ（Horizontal Layout Groupなど）")]
    [SerializeField] private Transform brushButtonContainer;
    
    [Tooltip("ブラシボタンのプレハブ（オプション）")]
    [SerializeField] private GameObject brushButtonPrefab;
    
    [Header("Action Buttons")]
    [Tooltip("クリアボタン")]
    [SerializeField] private Button clearButton;
    
    [Tooltip("Undoボタン")]
    [SerializeField] private Button undoButton;
    
    [Header("Color Buttons")]
    [Tooltip("次の色ボタン")]
    [SerializeField] private Button nextColorButton;
    
    [Tooltip("前の色ボタン")]
    [SerializeField] private Button previousColorButton;
    
    [Header("Preset Color Buttons")]
    [Tooltip("プリセット色ボタンのコンテナ（Horizontal Layout Groupなど）")]
    [SerializeField] private Transform presetColorContainer;
    
    [Tooltip("プリセット色ボタンのプレハブ")]
    [SerializeField] private GameObject presetColorButtonPrefab;
    
    [Header("Display Labels")]
    [Tooltip("現在の色プレビュー（Image）")]
    [SerializeField] private Image currentColorPreview;
    
    [Tooltip("ツール状態ラベル")]
    [SerializeField] private TextMeshProUGUI toolStateLabel;
    
    [Tooltip("ブラシタイプラベル")]
    [SerializeField] private TextMeshProUGUI brushTypeLabel;
    
    [Tooltip("Undo状態ラベル")]
    [SerializeField] private TextMeshProUGUI undoStateLabel;
    
    [Header("Save/Share Buttons (Optional)")]
    [Tooltip("保存ボタン")]
    [SerializeField] private Button saveButton;
    
    [Tooltip("共有ボタン")]
    [SerializeField] private Button shareButton;
    
    [Tooltip("保存状態ラベル")]
    [SerializeField] private TextMeshProUGUI saveStatusLabel;
    
    // 動的生成されたブラシボタンの辞書
    private Dictionary<BrushStrategyBase, Button> brushButtons = new Dictionary<BrushStrategyBase, Button>();
    
    void Start()
    {
        // 参照の自動検索
        if (creativeModeManager == null)
        {
            creativeModeManager = FindObjectOfType<CreativeModeManager>();
        }
        
        if (colorSelectionSystem == null)
        {
            colorSelectionSystem = FindObjectOfType<ColorSelectionSystem>();
        }
        
        // イベント購読
        SubscribeToEvents();
        
        // ボタンイベント設定
        SetupButtons();
        
        // プリセット色ボタンの生成
        BuildPresetButtons();
        
        // ブラシボタンの生成
        BuildBrushButtons();
        
        // 初期UI更新
        UpdateToolUI(creativeModeManager != null ? creativeModeManager.GetCurrentToolMode() : CreativeToolMode.Paint);
        UpdateColorUI(colorSelectionSystem != null ? colorSelectionSystem.GetCurrentColor() : Color.white);
        UpdateUndoUI(creativeModeManager != null && creativeModeManager.CanUndo());
        UpdateBrushUI(creativeModeManager != null ? creativeModeManager.GetCurrentBrush() : null);
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    void SubscribeToEvents()
    {
        if (creativeModeManager != null)
        {
            creativeModeManager.OnToolModeChanged += UpdateToolUI;
            creativeModeManager.OnColorChanged += UpdateColorUI;
            creativeModeManager.OnUndoAvailabilityChanged += UpdateUndoUI;
            creativeModeManager.OnBrushChanged += UpdateBrushUI;
        }
        
        if (colorSelectionSystem != null)
        {
            colorSelectionSystem.OnColorChanged += UpdateColorUI;
        }
    }
    
    void UnsubscribeFromEvents()
    {
        if (creativeModeManager != null)
        {
            creativeModeManager.OnToolModeChanged -= UpdateToolUI;
            creativeModeManager.OnColorChanged -= UpdateColorUI;
            creativeModeManager.OnUndoAvailabilityChanged -= UpdateUndoUI;
            creativeModeManager.OnBrushChanged -= UpdateBrushUI;
        }
        
        if (colorSelectionSystem != null)
        {
            colorSelectionSystem.OnColorChanged -= UpdateColorUI;
        }
        
        if (saveSystem != null)
        {
            saveSystem.OnImageSaved -= HandleImageSaved;
            saveSystem.OnShareCompleted -= HandleShareCompleted;
        }
    }
    
    void SetupButtons()
    {
        // ツールボタン
        if (paintToolButton != null)
        {
            paintToolButton.onClick.AddListener(() => {
                Debug.Log("CreativeModeUI: Paint Tool Button clicked");
                if (creativeModeManager != null)
                {
                    creativeModeManager.SetToolMode(CreativeToolMode.Paint);
                }
                else
                {
                    Debug.LogError("CreativeModeUI: creativeModeManager is null when Paint button clicked!");
                }
            });
        }
        else
        {
            Debug.LogError("CreativeModeUI: paintToolButton is null!");
        }
        
        if (eraserToolButton != null)
        {
            Debug.Log($"CreativeModeUI: eraserToolButton found, setting up listener. interactable={eraserToolButton.interactable}");
            
            // 既存のリスナーをクリア（重複を防ぐ）
            eraserToolButton.onClick.RemoveAllListeners();
            
            // ImageコンポーネントのRaycast Targetを確認
            Image buttonImage = eraserToolButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Debug.Log($"CreativeModeUI: eraserToolButton Image found, raycastTarget={buttonImage.raycastTarget}");
                if (!buttonImage.raycastTarget)
                {
                    Debug.LogWarning("CreativeModeUI: eraserToolButton Image raycastTarget is false! Setting to true.");
                    buttonImage.raycastTarget = true;
                }
            }
            else
            {
                Debug.LogWarning("CreativeModeUI: eraserToolButton has no Image component!");
            }
            
            // Canvas Groupの確認（自身）
            CanvasGroup canvasGroup = eraserToolButton.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log($"CreativeModeUI: eraserToolButton CanvasGroup found, interactable={canvasGroup.interactable}, blocksRaycasts={canvasGroup.blocksRaycasts}");
                if (!canvasGroup.interactable || !canvasGroup.blocksRaycasts)
                {
                    Debug.LogWarning("CreativeModeUI: eraserToolButton CanvasGroup is blocking interaction! Fixing...");
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }
            
            // 親オブジェクトのCanvasGroupも確認
            CanvasGroup parentCanvasGroup = eraserToolButton.GetComponentInParent<CanvasGroup>();
            if (parentCanvasGroup != null && parentCanvasGroup != canvasGroup)
            {
                Debug.Log($"CreativeModeUI: eraserToolButton parent CanvasGroup found, interactable={parentCanvasGroup.interactable}, blocksRaycasts={parentCanvasGroup.blocksRaycasts}");
                if (!parentCanvasGroup.interactable || !parentCanvasGroup.blocksRaycasts)
                {
                    Debug.LogWarning("CreativeModeUI: eraserToolButton parent CanvasGroup is blocking interaction! This may prevent the button from working.");
                }
            }
            
            // Paint Tool Button と比較（同じ親を持つ場合）
            if (paintToolButton != null)
            {
                CanvasGroup paintCanvasGroup = paintToolButton.GetComponent<CanvasGroup>();
                CanvasGroup paintParentCanvasGroup = paintToolButton.GetComponentInParent<CanvasGroup>();
                Debug.Log($"CreativeModeUI: paintToolButton CanvasGroup: self={paintCanvasGroup != null}, parent={paintParentCanvasGroup != null}");
                if (paintCanvasGroup != null)
                {
                    Debug.Log($"CreativeModeUI: paintToolButton CanvasGroup: interactable={paintCanvasGroup.interactable}, blocksRaycasts={paintCanvasGroup.blocksRaycasts}");
                }
                if (paintParentCanvasGroup != null)
                {
                    Debug.Log($"CreativeModeUI: paintToolButton parent CanvasGroup: interactable={paintParentCanvasGroup.interactable}, blocksRaycasts={paintParentCanvasGroup.blocksRaycasts}");
                }
            }
            
            // ButtonコンポーネントのNavigation設定を確認
            Navigation nav = eraserToolButton.navigation;
            Debug.Log($"CreativeModeUI: eraserToolButton navigation mode = {nav.mode}");
            
            // ButtonコンポーネントのTransition設定を確認
            Debug.Log($"CreativeModeUI: eraserToolButton transition = {eraserToolButton.transition}");
            
            // ButtonコンポーネントのTarget Graphicを確認
            if (eraserToolButton.targetGraphic != null)
            {
                Debug.Log($"CreativeModeUI: eraserToolButton targetGraphic = {eraserToolButton.targetGraphic.name}, enabled={eraserToolButton.targetGraphic.enabled}");
            }
            else
            {
                Debug.LogWarning("CreativeModeUI: eraserToolButton targetGraphic is null!");
                if (buttonImage != null)
                {
                    eraserToolButton.targetGraphic = buttonImage;
                    Debug.Log("CreativeModeUI: Set eraserToolButton targetGraphic to Image component.");
                }
            }
            
            // RectTransformのサイズと位置を確認
            RectTransform eraserRect = eraserToolButton.GetComponent<RectTransform>();
            if (eraserRect != null)
            {
                Debug.Log($"CreativeModeUI: eraserToolButton RectTransform: sizeDelta={eraserRect.sizeDelta}, anchoredPosition={eraserRect.anchoredPosition}, activeInHierarchy={eraserToolButton.gameObject.activeInHierarchy}");
                if (eraserRect.sizeDelta.x <= 0 || eraserRect.sizeDelta.y <= 0)
                {
                    Debug.LogWarning("CreativeModeUI: eraserToolButton RectTransform size is zero or negative! This may prevent clicking.");
                }
            }
            
            // Paint Tool Buttonと比較（同じ親を持つ場合）
            if (paintToolButton != null)
            {
                RectTransform paintRect = paintToolButton.GetComponent<RectTransform>();
                if (paintRect != null)
                {
                    Debug.Log($"CreativeModeUI: paintToolButton RectTransform: sizeDelta={paintRect.sizeDelta}, anchoredPosition={paintRect.anchoredPosition}");
                }
                
                // Buttonコンポーネントの設定を比較
                Debug.Log($"CreativeModeUI: Comparison - paintToolButton.interactable={paintToolButton.interactable}, eraserToolButton.interactable={eraserToolButton.interactable}");
                Debug.Log($"CreativeModeUI: Comparison - paintToolButton.enabled={paintToolButton.enabled}, eraserToolButton.enabled={eraserToolButton.enabled}");
            }
            
            // イベントシステムの確認
            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                Debug.Log($"CreativeModeUI: EventSystem found, enabled={eventSystem.enabled}");
            }
            else
            {
                Debug.LogWarning("CreativeModeUI: EventSystem not found! Buttons may not work.");
            }
            
            eraserToolButton.onClick.AddListener(() => {
                Debug.Log("CreativeModeUI: Eraser Tool Button clicked!");
                if (creativeModeManager != null)
                {
                    Debug.Log("CreativeModeUI: Calling SetToolMode(Eraser)");
                    creativeModeManager.SetToolMode(CreativeToolMode.Eraser);
                }
                else
                {
                    Debug.LogError("CreativeModeUI: creativeModeManager is null when Eraser button clicked!");
                }
            });
            
            // リスナーが正しく設定されたか確認
            int listenerCount = eraserToolButton.onClick.GetPersistentEventCount();
            Debug.Log($"CreativeModeUI: eraserToolButton onClick listener count = {listenerCount}");
        }
        else
        {
            Debug.LogError("CreativeModeUI: eraserToolButton is null! Please assign it in the Inspector.");
        }
        
        // アクションボタン
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(() => {
                if (creativeModeManager != null)
                {
                    creativeModeManager.ClearCanvas();
                }
            });
        }
        
        if (undoButton != null)
        {
            undoButton.onClick.AddListener(() => {
                if (creativeModeManager != null)
                {
                    creativeModeManager.Undo();
                }
            });
        }
        
        // 色ボタン
        if (nextColorButton != null)
        {
            nextColorButton.onClick.AddListener(() => {
                if (colorSelectionSystem != null)
                {
                    colorSelectionSystem.NextPresetColor();
                }
            });
        }
        
        if (previousColorButton != null)
        {
            previousColorButton.onClick.AddListener(() => {
                if (colorSelectionSystem != null)
                {
                    colorSelectionSystem.PreviousPresetColor();
                }
            });
        }
        
        // 保存/共有ボタン（オプション）
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(() => {
                if (saveSystem != null)
                {
                    saveSystem.SaveImage();
                }
                else
                {
                    Debug.LogWarning("CreativeModeUI: CreativeModeSaveSystemが設定されていません");
                }
            });
        }
        
        if (shareButton != null)
        {
            shareButton.onClick.AddListener(() => {
                if (saveSystem != null)
                {
                    saveSystem.ShareImage();
                }
                else
                {
                    Debug.LogWarning("CreativeModeUI: CreativeModeSaveSystemが設定されていません");
                }
            });
        }
        
        // 保存システムのイベント購読
        if (saveSystem != null)
        {
            saveSystem.OnImageSaved += HandleImageSaved;
            saveSystem.OnShareCompleted += HandleShareCompleted;
        }
    }
    
    /// <summary>
    /// プリセット色ボタンを動的に生成
    /// </summary>
    void BuildPresetButtons()
    {
        Debug.Log($"BuildPresetButtons called: colorSelectionSystem={colorSelectionSystem}, presetColorContainer={presetColorContainer}");
        if (colorSelectionSystem == null || presetColorContainer == null)
        {
            Debug.LogWarning("BuildPresetButtons: colorSelectionSystem or presetColorContainer is null");
            return;
        }
        
        Color[] presetColors = colorSelectionSystem.GetPresetColors();
        Debug.Log($"BuildPresetButtons: presetColors length={presetColors?.Length ?? 0}");
        if (presetColors == null || presetColors.Length == 0)
        {
            Debug.LogWarning("BuildPresetButtons: No preset colors available");
            return;
        }
        
        // 既存のボタンを削除
        foreach (Transform child in presetColorContainer)
        {
            Destroy(child.gameObject);
        }
        
        // プリセット色ボタンを生成
        for (int i = 0; i < presetColors.Length; i++)
        {
            int index = i; // クロージャ用
            
            GameObject buttonObj;
            if (presetColorButtonPrefab != null)
            {
                buttonObj = Instantiate(presetColorButtonPrefab, presetColorContainer);
            }
            else
            {
                // プレハブがない場合は動的に作成
                buttonObj = new GameObject($"PresetColorButton_{i}");
                buttonObj.transform.SetParent(presetColorContainer);
                
                Image image = buttonObj.AddComponent<Image>();
                image.color = presetColors[i];
                
                Button newButton = buttonObj.AddComponent<Button>();
                newButton.targetGraphic = image;
            }
            
            // ボタンの色を設定
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = presetColors[i];
            }
            
            // ボタンイベント設定
            // TextMeshProのButtonプレハブにも対応（親、自身、子の順で検索）
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.GetComponentInParent<Button>();
            }
            if (button == null)
            {
                button = buttonObj.GetComponentInChildren<Button>();
            }
            
            if (button != null)
            {
                Debug.Log($"CreativeModeUI: Button component found on {buttonObj.name}, adding listener for index={index}");
                button.onClick.AddListener(() => {
                    Debug.Log($"PresetColorButton clicked: index={index}");
                    if (colorSelectionSystem != null)
                    {
                        Debug.Log($"Calling SelectPresetColor({index})");
                        colorSelectionSystem.SelectPresetColor(index);
                    }
                    else
                    {
                        Debug.LogError("CreativeModeUI: colorSelectionSystem is null!");
                    }
                });
            }
            else
            {
                Debug.LogError($"CreativeModeUI: Button component not found on buttonObj {buttonObj.name}. Please ensure the prefab has a Button component.");
            }
        }
    }
    
    /// <summary>
    /// ブラシボタンを動的に生成
    /// </summary>
    void BuildBrushButtons()
    {
        if (creativeModeManager == null || brushButtonContainer == null)
        {
            Debug.LogWarning("BuildBrushButtons: creativeModeManager or brushButtonContainer is null");
            return;
        }
        
        List<BrushStrategyBase> availableBrushes = creativeModeManager.GetAvailableBrushes();
        if (availableBrushes == null || availableBrushes.Count == 0)
        {
            Debug.LogWarning("BuildBrushButtons: 利用可能なブラシがありません。CreativeModeSettingsで設定してください。");
            return;
        }
        
        // 既存のボタンを削除
        foreach (Transform child in brushButtonContainer)
        {
            Destroy(child.gameObject);
        }
        brushButtons.Clear();
        
        // ブラシボタンを生成
        foreach (BrushStrategyBase brush in availableBrushes)
        {
            if (brush == null) continue;
            
            GameObject buttonObj;
            if (brushButtonPrefab != null)
            {
                buttonObj = Instantiate(brushButtonPrefab, brushButtonContainer);
            }
            else
            {
                // プレハブがない場合は動的に作成
                buttonObj = new GameObject($"BrushButton_{brush.name}");
                buttonObj.transform.SetParent(brushButtonContainer);
                
                Image image = buttonObj.AddComponent<Image>();
                Button newButton = buttonObj.AddComponent<Button>();
                newButton.targetGraphic = image;
                
                // テキストを追加
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform);
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = brush.GetDisplayName();
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.black;
                
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
            }
            
            // ボタンのテキストを設定
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = brush.GetDisplayName();
            }
            
            // アイコンを設定（オプション）
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null && brush.GetIcon() != null)
            {
                buttonImage.sprite = brush.GetIcon();
            }
            
            // ボタンイベント設定
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.GetComponentInParent<Button>();
            }
            if (button == null)
            {
                button = buttonObj.GetComponentInChildren<Button>();
            }
            
            if (button != null)
            {
                BrushStrategyBase brushRef = brush; // クロージャ用
                button.onClick.AddListener(() => {
                    if (creativeModeManager != null)
                    {
                        creativeModeManager.SetBrush(brushRef);
                    }
                });
                
                brushButtons[brush] = button;
            }
        }
    }
    
    /// <summary>
    /// ツールUIを更新
    /// </summary>
    void UpdateToolUI(CreativeToolMode mode)
    {
        if (toolStateLabel != null)
        {
            toolStateLabel.text = mode == CreativeToolMode.Paint ? "Paint" : "Eraser";
        }
        
        // ボタンの視覚的フィードバック
        if (paintToolButton != null)
        {
            paintToolButton.interactable = (mode != CreativeToolMode.Paint);
            Debug.Log($"UpdateToolUI: paintToolButton.interactable = {paintToolButton.interactable} (mode={mode})");
        }
        
        if (eraserToolButton != null)
        {
            bool shouldBeInteractable = (mode != CreativeToolMode.Eraser);
            eraserToolButton.interactable = shouldBeInteractable;
            Debug.Log($"UpdateToolUI: eraserToolButton.interactable = {shouldBeInteractable} (mode={mode})");
        }
        else
        {
            Debug.LogError("UpdateToolUI: eraserToolButton is null!");
        }
    }
    
    /// <summary>
    /// 色UIを更新
    /// </summary>
    void UpdateColorUI(Color color)
    {
        if (currentColorPreview != null)
        {
            currentColorPreview.color = color;
        }
    }
    
    /// <summary>
    /// Undo UIを更新
    /// </summary>
    void UpdateUndoUI(bool canUndo)
    {
        if (undoButton != null)
        {
            undoButton.interactable = canUndo;
        }
        
        if (undoStateLabel != null)
        {
            undoStateLabel.text = canUndo ? "Undo Available" : "No Undo";
        }
    }
    
    /// <summary>
    /// ブラシUIを更新
    /// </summary>
    void UpdateBrushUI(BrushStrategyBase brush)
    {
        if (brushTypeLabel != null)
        {
            brushTypeLabel.text = brush != null ? brush.GetDisplayName() : "None";
        }
        
        // 動的生成されたボタンの視覚的フィードバック
        foreach (var kvp in brushButtons)
        {
            if (kvp.Value != null)
            {
                kvp.Value.interactable = (kvp.Key != brush);
            }
        }
    }
    
    /// <summary>
    /// 保存完了時のハンドラ（CreativeModeSaveSystemから呼ばれる）
    /// </summary>
    public void HandleImageSaved(string filePath)
    {
        if (saveStatusLabel != null)
        {
            saveStatusLabel.text = $"Saved: {filePath}";
        }
    }
    
    /// <summary>
    /// 共有完了時のハンドラ（CreativeModeSaveSystemから呼ばれる）
    /// </summary>
    public void HandleShareCompleted(bool success)
    {
        if (saveStatusLabel != null)
        {
            saveStatusLabel.text = success ? "Share completed" : "Share failed";
        }
    }
}

