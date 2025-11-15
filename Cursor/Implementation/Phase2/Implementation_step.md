# Phase 2: クリエイティブモード実装仕様書

## 📋 概要

**目標**: 声で自由に絵を描ける状態にする

**実装難易度**: 低（Phase 1の塗りシステムが完成していれば実装可能）

**特徴**:
- Phase 1の塗りシステム（`PaintCanvas`、`PaintBattleGameManager`）をそのまま使用
- ゲーム要素（モンスター、スコア、タイマー、勝利条件）は不要
- シンプルなUI（クリアボタン、色選択、ツール切り替えなど）
- 保存・共有機能（オプション）

---

## 🎯 実装範囲

### 必須実装
1. **クリエイティブモードマネージャー** - 塗りシステムの統合管理
2. **色選択システム** - インクの色を選択・変更
3. **ツールシステム** - 塗りツールと消しツールの切り替え
4. **クリエイティブモードUI** - 基本的なUI要素（クリア、色選択、ツール切り替え）
5. **履歴管理（Undo/Redo）** - 操作の取り消し・やり直し

### オプション実装
6. **描画システム** - 画面上に色が塗られている様子を表示
7. **保存機能** - 描いた絵を画像として保存
8. **共有機能** - 保存した画像を共有（プラットフォーム依存）

---

## 📁 ファイル構成

```
Assets/Main/Script/
├── Creative/
│   ├── CreativeModeManager.cs          // クリエイティブモードの統合管理
│   ├── CreativeToolMode.cs            // ツールモードのenum定義
│   ├── ColorSelectionSystem.cs        // 色選択システム
│   └── CreativeModeSaveSystem.cs     // 保存・共有システム（オプション）
│
├── Graphics/
│   └── PaintRenderer.cs               // 塗りキャンバスの描画システム
│
├── Data/Settings/
│   ├── CreativeModeSettings.cs        // クリエイティブモード設定
│   ├── ColorSelectionSettings.cs      // 色選択設定
│   └── CreativeSaveSettings.cs        // 保存設定（オプション）
│
└── UI/
    └── CreativeModeUI.cs              // クリエイティブモードUI
```

---

## 🔧 Step 2.1: クリエイティブモードマネージャー

### ファイル
`Assets/Main/Script/Creative/CreativeModeManager.cs`

### 役割
- Phase 1の`PaintCanvas`と`PaintBattleGameManager`を統合管理
- ツールモード（塗り/消し）の切り替え
- 色の管理
- 履歴管理（Undo/Redo）
- 音声入力の処理

### 実装内容

#### 1. 基本構造

```csharp
public enum CreativeToolMode
{
    Paint,   // 塗りツール
    Eraser   // 消しツール
}

public enum BrushType
{
    Pencil,  // 鉛筆（細い線、連続的な描画）
    Paint    // ペンキ（太い線、広範囲の塗りつぶし）- 将来的な拡張
}

public class CreativeModeManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CreativeModeSettings settings;
    
    [Header("References")]
    [SerializeField] private PaintCanvas paintCanvas;
    [SerializeField] private PaintBattleGameManager paintGameManager;
    [SerializeField] private VoiceToScreenMapper voiceMapper;
    [SerializeField] private ImprovedPitchAnalyzer pitchAnalyzer;
    [SerializeField] private VolumeAnalyzer volumeAnalyzer;
    
    [Header("Color Selection")]
    [SerializeField] private ColorSelectionSystem colorSelectionSystem;
    
    // 現在の状態
    private CreativeToolMode currentToolMode = CreativeToolMode.Paint;
    private BrushType currentBrushType = BrushType.Pencil; // 現在のブラシタイプ
    private Color currentColor = Color.white;
    private int currentPlayerId = 1; // クリエイティブモードは1プレイヤーのみ
    
    // 履歴管理
    private Stack<CanvasState> historyStack = new Stack<CanvasState>();
    private int maxHistorySize = 10;
}
```

#### 2. 初期化処理

```csharp
void Start()
{
    InitializeCreativeMode();
}

private void InitializeCreativeMode()
{
    // 塗りシステムを有効化
    if (paintCanvas != null)
        paintCanvas.gameObject.SetActive(true);
    
    if (paintGameManager != null)
        paintGameManager.gameObject.SetActive(true);
    
    // 色選択システムの初期化
    if (colorSelectionSystem != null)
    {
        colorSelectionSystem.Initialize(settings.colorSelectionSettings);
        currentColor = colorSelectionSystem.GetCurrentColor();
    }
    
    // 履歴の初期状態を保存
    SaveHistorySnapshot();
    
    // イベント購読
    SubscribeToEvents();
}
```

#### 3. ツールモード切り替え

```csharp
public void SetToolMode(CreativeToolMode mode)
{
    currentToolMode = mode;
    OnToolModeChanged?.Invoke(mode);
}

public CreativeToolMode GetToolMode() => currentToolMode;
```

#### 3-2. ブラシタイプ切り替え

```csharp
public void SetBrushType(BrushType brushType)
{
    currentBrushType = brushType;
    OnBrushTypeChanged?.Invoke(brushType);
}

public BrushType GetBrushType() => currentBrushType;
```

#### 4. 色の設定

```csharp
public void SetColor(Color color)
{
    currentColor = color;
    OnColorChanged?.Invoke(color);
    
    // InkEffectの色も更新
    InkEffect inkEffect = FindObjectOfType<InkEffect>();
    if (inkEffect != null)
    {
        inkEffect.SetColor(color);
    }
}

public Color GetCurrentColor() => currentColor;
```

#### 5. 塗り・消し処理（音声入力から）

**注意**: 塗りツールと消しツールの両方が音声入力で動作します。ツールモードに応じて、音声の音量とピッチから座標を取得し、塗りまたは消し処理を実行します。

```csharp
void Update()
{
    if (!IsActive()) return;
    
    // 音声データ取得
    float pitch = pitchAnalyzer != null ? pitchAnalyzer.lastDetectedPitch : 0f;
    float volume = volumeAnalyzer != null ? volumeAnalyzer.lastDetectedVolume : 0f;
    
    // 音量が閾値以下なら処理しない
    if (volume < settings.silenceVolumeThreshold) return;
    
    // 座標変換（音量とピッチから画面座標を取得）
    Vector2 screenPos = voiceMapper != null 
        ? voiceMapper.MapVoiceToScreen(volume, pitch)
        : Vector2.zero;
    
    // ツールモードに応じて処理
    // - Paintモード: 音声で指定した位置に色を塗る
    // - Eraserモード: 音声で指定した位置を消す
    if (currentToolMode == CreativeToolMode.Paint)
    {
        PaintAt(screenPos, volume);
    }
    else // CreativeToolMode.Eraser
    {
        EraseAt(screenPos, volume);
    }
}

private void PaintAt(Vector2 position, float intensity)
{
    if (paintCanvas == null) return;
    
    // ブラシタイプに応じて処理を分岐
    switch (currentBrushType)
    {
        case BrushType.Pencil:
            PaintWithPencil(position, intensity);
            break;
        case BrushType.Paint:
            PaintWithPaint(position, intensity);
            break;
    }
}

/// <summary>
/// 鉛筆で塗る（細い線、連続的な描画）
/// </summary>
private void PaintWithPencil(Vector2 position, float intensity)
{
    if (paintCanvas == null) return;
    
    float finalIntensity = intensity * settings.paintIntensity;
    float radius = settings.pencilRadius; // 鉛筆の半径（設定から取得）
    
    // 鉛筆は円形のブラシで塗る
    paintCanvas.PaintAtWithRadius(position, currentPlayerId, finalIntensity, currentColor, radius);
}

/// <summary>
/// ペンキで塗る（太い線、広範囲の塗りつぶし）- 将来的な実装
/// </summary>
private void PaintWithPaint(Vector2 position, float intensity)
{
    if (paintCanvas == null) return;
    
    float finalIntensity = intensity * settings.paintIntensity;
    float radius = settings.paintBrushRadius; // ペンキブラシの半径（将来的な設定）
    
    // ペンキはより大きな円形のブラシで塗る
    paintCanvas.PaintAtWithRadius(position, currentPlayerId, finalIntensity, currentColor, radius);
}

private void EraseAt(Vector2 position, float intensity)
{
    if (paintCanvas != null)
    {
        // 消しツール: 音声で指定した位置から指定半径内を消す
        // 音量（intensity）は現在使用していないが、将来的に音量で消しサイズを調整可能
        float radius = settings.eraserRadius;
        paintCanvas.EraseAt(position, radius);
    }
}
```

#### 6. 履歴管理（Undo/Redo）

```csharp
public void Undo()
{
    if (historyStack.Count <= 1) return; // 初期状態は残す
    
    // 現在の状態を破棄
    historyStack.Pop();
    
    // 前の状態を復元
    if (historyStack.Count > 0)
    {
        CanvasState previousState = historyStack.Peek();
        paintCanvas.RestoreState(previousState);
        OnUndoAvailabilityChanged?.Invoke(CanUndo());
    }
}

public bool CanUndo() => historyStack.Count > 1;

public void SaveHistorySnapshot()
{
    if (paintCanvas != null)
    {
        CanvasState state = paintCanvas.GetState();
        historyStack.Push(state);
        
        // 履歴サイズ制限
        if (historyStack.Count > maxHistorySize)
        {
            // 古い履歴を削除（実装は簡略化）
        }
    }
}
```

#### 7. キャンバスクリア

```csharp
public void ClearCanvas()
{
    if (paintCanvas != null)
    {
        paintCanvas.ResetCanvas();
        SaveHistorySnapshot(); // クリア後の状態を履歴に保存
    }
}
```

#### 8. イベント

```csharp
public static event System.Action<CreativeToolMode> OnToolModeChanged;
public static event System.Action<BrushType> OnBrushTypeChanged;
public static event System.Action<Color> OnColorChanged;
public static event System.Action<bool> OnUndoAvailabilityChanged;
```

---

## 🎨 Step 2.2: 色選択システム

### ファイル
`Assets/Main/Script/Creative/ColorSelectionSystem.cs`

### 役割
- 色の選択・管理
- プリセット色の管理
- カラーピッカーの統合（オプション）

### 実装内容

```csharp
public enum ColorSelectionMethod
{
    PresetPalette,  // プリセット色パレット
    ColorPicker,    // カラーピッカー（Unity標準）
    VoiceSelection  // 音声による色選択（将来的な拡張）
}

public class ColorSelectionSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ColorSelectionSettings settings;
    
    [Header("References")]
    [SerializeField] private CreativeModeManager creativeModeManager;
    
    private Color currentColor;
    private int currentColorIndex = 0;
    
    void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        if (settings != null && settings.presetColors != null && settings.presetColors.Length > 0)
        {
            currentColor = settings.presetColors[0];
        }
        else
        {
            currentColor = Color.white;
        }
        
        // CreativeModeManagerに色を通知
        if (creativeModeManager != null)
        {
            creativeModeManager.SetColor(currentColor);
        }
    }
    
    public void SelectColorByIndex(int index)
    {
        if (settings != null && settings.presetColors != null)
        {
            if (index >= 0 && index < settings.presetColors.Length)
            {
                currentColorIndex = index;
                currentColor = settings.presetColors[index];
                NotifyColorChanged();
            }
        }
    }
    
    public void SelectColor(Color color)
    {
        currentColor = color;
        NotifyColorChanged();
    }
    
    public Color GetCurrentColor() => currentColor;
    
    public IReadOnlyList<Color> GetAvailableColors()
    {
        return settings != null && settings.presetColors != null
            ? settings.presetColors
            : new List<Color> { Color.white };
    }
    
    private void NotifyColorChanged()
    {
        if (creativeModeManager != null)
        {
            creativeModeManager.SetColor(currentColor);
        }
    }
}
```

---

## ⚙️ Step 2.3: 設定ファイル（ScriptableObject）

### ファイル1: CreativeModeSettings.cs

```csharp
[CreateAssetMenu(fileName = "CreativeModeSettings", menuName = "Game/Creative Mode Settings")]
public class CreativeModeSettings : ScriptableObject
{
    [Header("Paint Settings")]
    [Range(0.1f, 2f)] public float paintIntensity = 1f;
    public Color initialColor = Color.white;
    public int defaultPlayerId = 1;
    
    [Header("Brush Settings")]
    [Tooltip("鉛筆の半径（ピクセル単位）")]
    [Range(1f, 50f)] public float pencilRadius = 5f;
    
    [Tooltip("ペンキブラシの半径（ピクセル単位）- 将来的な拡張")]
    [Range(10f, 200f)] public float paintBrushRadius = 50f;
    
    [Header("Eraser Settings")]
    [Range(10f, 100f)] public float eraserRadius = 30f;
    
    [Header("History Settings")]
    [Range(1, 50)] public int maxHistorySize = 10;
    
    [Header("Voice Detection")]
    [Range(0f, 0.1f)] public float silenceVolumeThreshold = 0.01f;
    
    [Header("Color Selection")]
    public ColorSelectionSettings colorSelectionSettings;
}
```

### ファイル2: ColorSelectionSettings.cs

```csharp
[CreateAssetMenu(fileName = "ColorSelectionSettings", menuName = "Game/Color Selection Settings")]
public class ColorSelectionSettings : ScriptableObject
{
    public ColorSelectionMethod method = ColorSelectionMethod.PresetPalette;
    
    [Header("Preset Colors")]
    public Color[] presetColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        Color.white,
        Color.black
    };
    
    [Header("Color Picker")]
    public bool colorPickerVisibleByDefault = false;
}
```

---

## 🖼️ Step 2.4: クリエイティブモードUI

### ファイル
`Assets/Main/Script/UI/CreativeModeUI.cs`

### 役割
- UI要素の管理
- ボタンイベントの処理
- UI状態の更新

### 実装内容

```csharp
public class CreativeModeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CreativeModeManager creativeModeManager;
    [SerializeField] private ColorSelectionSystem colorSelectionSystem;
    
    [Header("Tool Buttons")]
    [SerializeField] private Button paintToolButton;
    [SerializeField] private Button eraserToolButton;
    
    [Header("Brush Type Buttons")]
    [SerializeField] private Button pencilBrushButton;
    [SerializeField] private Button paintBrushButton; // 将来的な拡張用
    
    [Header("Color Selection")]
    [SerializeField] private Transform colorButtonParent;
    [SerializeField] private GameObject colorButtonPrefab;
    [SerializeField] private Image currentColorPreview;
    
    [Header("Action Buttons")]
    [SerializeField] private Button clearButton;
    [SerializeField] private Button undoButton;
    
    [Header("Status Labels")]
    [SerializeField] private TextMeshProUGUI toolStateLabel;
    [SerializeField] private TextMeshProUGUI brushTypeLabel;
    [SerializeField] private TextMeshProUGUI undoStateLabel;
    
    void Start()
    {
        InitializeUI();
        BindButtonCallbacks();
        SubscribeToEvents();
    }
    
    private void InitializeUI()
    {
        // 色選択ボタンの生成
        BuildPresetColorButtons();
        
        // 初期状態の更新
        UpdateToolUI(CreativeToolMode.Paint);
        UpdateBrushTypeUI(BrushType.Pencil);
        UpdateColorUI(colorSelectionSystem != null ? colorSelectionSystem.GetCurrentColor() : Color.white);
        UpdateUndoUI(creativeModeManager != null ? creativeModeManager.CanUndo() : false);
    }
    
    private void BuildPresetColorButtons()
    {
        if (colorSelectionSystem == null || colorButtonPrefab == null || colorButtonParent == null)
            return;
        
        var colors = colorSelectionSystem.GetAvailableColors();
        foreach (var color in colors)
        {
            GameObject buttonObj = Instantiate(colorButtonPrefab, colorButtonParent);
            Button button = buttonObj.GetComponent<Button>();
            
            // ボタンの色を設定
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = color;
            }
            
            // クリックイベント
            int colorIndex = System.Array.IndexOf(colors.ToArray(), color);
            button.onClick.AddListener(() => SelectColor(colorIndex));
        }
    }
    
    private void BindButtonCallbacks()
    {
        // ツール切り替え
        if (paintToolButton != null)
            paintToolButton.onClick.AddListener(() => SetToolMode(CreativeToolMode.Paint));
        
        if (eraserToolButton != null)
            eraserToolButton.onClick.AddListener(() => SetToolMode(CreativeToolMode.Eraser));
        
        // ブラシタイプ切り替え
        if (pencilBrushButton != null)
            pencilBrushButton.onClick.AddListener(() => SetBrushType(BrushType.Pencil));
        
        if (paintBrushButton != null)
            paintBrushButton.onClick.AddListener(() => SetBrushType(BrushType.Paint));
        
        // アクション
        if (clearButton != null && creativeModeManager != null)
            clearButton.onClick.AddListener(() => creativeModeManager.ClearCanvas());
        
        if (undoButton != null && creativeModeManager != null)
            undoButton.onClick.AddListener(() => creativeModeManager.Undo());
    }
    
    private void SetToolMode(CreativeToolMode mode)
    {
        if (creativeModeManager != null)
        {
            creativeModeManager.SetToolMode(mode);
        }
    }
    
    private void SetBrushType(BrushType brushType)
    {
        if (creativeModeManager != null)
        {
            creativeModeManager.SetBrushType(brushType);
        }
    }
    
    private void SelectColor(int colorIndex)
    {
        if (colorSelectionSystem != null)
        {
            colorSelectionSystem.SelectColorByIndex(colorIndex);
        }
    }
    
    private void SubscribeToEvents()
    {
        if (creativeModeManager != null)
        {
            CreativeModeManager.OnToolModeChanged += UpdateToolUI;
            CreativeModeManager.OnBrushTypeChanged += UpdateBrushTypeUI;
            CreativeModeManager.OnColorChanged += UpdateColorUI;
            CreativeModeManager.OnUndoAvailabilityChanged += UpdateUndoUI;
        }
    }
    
    private void UpdateToolUI(CreativeToolMode mode)
    {
        if (toolStateLabel != null)
        {
            toolStateLabel.text = mode == CreativeToolMode.Paint ? "Paint Tool" : "Eraser Tool";
        }
        
        // ボタンの視覚的フィードバック
        if (paintToolButton != null)
            paintToolButton.interactable = (mode != CreativeToolMode.Paint);
        
        if (eraserToolButton != null)
            eraserToolButton.interactable = (mode != CreativeToolMode.Eraser);
    }
    
    private void UpdateBrushTypeUI(BrushType brushType)
    {
        if (brushTypeLabel != null)
        {
            brushTypeLabel.text = brushType == BrushType.Pencil ? "Pencil" : "Paint";
        }
        
        // ボタンの視覚的フィードバック
        if (pencilBrushButton != null)
            pencilBrushButton.interactable = (brushType != BrushType.Pencil);
        
        if (paintBrushButton != null)
            paintBrushButton.interactable = (brushType != BrushType.Paint);
    }
    
    private void UpdateColorUI(Color color)
    {
        if (currentColorPreview != null)
        {
            currentColorPreview.color = color;
        }
    }
    
    private void UpdateUndoUI(bool canUndo)
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
    
    void OnDestroy()
    {
        if (creativeModeManager != null)
        {
            CreativeModeManager.OnToolModeChanged -= UpdateToolUI;
            CreativeModeManager.OnBrushTypeChanged -= UpdateBrushTypeUI;
            CreativeModeManager.OnColorChanged -= UpdateColorUI;
            CreativeModeManager.OnUndoAvailabilityChanged -= UpdateUndoUI;
        }
    }
}
```

---

## 🎨 Step 2.5: 描画システム（画面上への表示）

### ファイル
`Assets/Main/Script/Graphics/PaintRenderer.cs`

### 役割
- `PaintCanvas`の内部データを`Texture2D`に変換
- UI Imageに表示して、画面上に色が塗られている様子を可視化
- リアルタイムで塗り状態を更新

### 実装内容

```csharp
public class PaintRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Header("Display Settings")]
    [Tooltip("塗りキャンバスを表示するUI Image")]
    [SerializeField] private UnityEngine.UI.Image canvasDisplayImage;
    
    [Tooltip("キャンバスの表示サイズ（画面全体 or 指定サイズ）")]
    [SerializeField] private RectTransform canvasDisplayRect;
    
    [Header("Rendering Settings")]
    [Tooltip("テクスチャの更新頻度（フレーム単位、1=毎フレーム）")]
    [Range(1, 10)] public int updateFrequency = 1;
    
    [Tooltip("背景色（未塗り部分の色）")]
    public Color backgroundColor = Color.clear;
    
    // 内部状態
    private Texture2D canvasTexture;
    private int frameCount = 0;
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeRenderer();
    }
    
    private void InitializeRenderer()
    {
        if (paintCanvas == null)
        {
            Debug.LogError("PaintRenderer: PaintCanvasが設定されていません");
            return;
        }
        
        // PaintCanvasからサイズを取得
        PaintSettings settings = paintCanvas.GetSettings();
        if (settings == null)
        {
            Debug.LogError("PaintRenderer: PaintSettingsが取得できません");
            return;
        }
        
        // Texture2Dを作成
        canvasTexture = new Texture2D(settings.textureWidth, settings.textureHeight, TextureFormat.RGBA32, false);
        canvasTexture.filterMode = FilterMode.Bilinear;
        canvasTexture.wrapMode = TextureWrapMode.Clamp;
        
        // 初期化：背景色で塗りつぶし
        Color[] pixels = new Color[settings.textureWidth * settings.textureHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }
        canvasTexture.SetPixels(pixels);
        canvasTexture.Apply();
        
        // UI Imageに設定
        if (canvasDisplayImage != null)
        {
            canvasDisplayImage.sprite = Sprite.Create(
                canvasTexture,
                new Rect(0, 0, settings.textureWidth, settings.textureHeight),
                new Vector2(0.5f, 0.5f),
                100f // pixelsPerUnit
            );
        }
        
        // イベント購読
        if (paintCanvas != null)
        {
            paintCanvas.OnPaintCompleted += OnPaintCompleted;
            paintCanvas.OnPaintingSuppressed += OnPaintingSuppressed;
        }
        
        isInitialized = true;
        Debug.Log("PaintRenderer: 初期化完了");
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        // 更新頻度チェック
        frameCount++;
        if (frameCount % updateFrequency != 0)
        {
            return;
        }
        
        // テクスチャを更新
        UpdateTexture();
    }
    
    private void UpdateTexture()
    {
        if (paintCanvas == null || canvasTexture == null) return;
        
        PaintSettings settings = paintCanvas.GetSettings();
        if (settings == null) return;
        
        // PaintCanvasからデータを取得してテクスチャに反映
        // 注意: PaintCanvasに色データを取得するメソッドが必要
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                Color pixelColor = GetPixelColor(x, y);
                canvasTexture.SetPixel(x, y, pixelColor);
            }
        }
        
        canvasTexture.Apply();
    }
    
    private Color GetPixelColor(int x, int y)
    {
        if (paintCanvas == null) return backgroundColor;
        
        // PaintCanvasから色データを取得
        // 注意: PaintCanvasに色データを取得するメソッドが必要
        int playerId = paintCanvas.GetPlayerIdAtCanvas(x, y);
        Color color = paintCanvas.GetColorAtCanvas(x, y);
        
        if (playerId == 0)
        {
            return backgroundColor;
        }
        
        return color;
    }
    
    private void OnPaintCompleted(Vector2 position, int playerId, float intensity)
    {
        // 塗り完了時に該当ピクセルのみ更新（最適化）
        if (paintCanvas == null || canvasTexture == null) return;
        
        PaintSettings settings = paintCanvas.GetSettings();
        if (settings == null) return;
        
        // 画面座標をキャンバス座標に変換
        int canvasX = Mathf.RoundToInt((position.x / Screen.width) * settings.textureWidth);
        int canvasY = Mathf.RoundToInt((position.y / Screen.height) * settings.textureHeight);
        
        if (canvasX >= 0 && canvasX < settings.textureWidth &&
            canvasY >= 0 && canvasY < settings.textureHeight)
        {
            Color pixelColor = GetPixelColor(canvasX, canvasY);
            canvasTexture.SetPixel(canvasX, canvasY, pixelColor);
            canvasTexture.Apply();
        }
    }
    
    private void OnPaintingSuppressed()
    {
        // 無音時は何もしない（既存のテクスチャを維持）
    }
    
    /// <summary>
    /// テクスチャを取得（保存機能などで使用）
    /// </summary>
    public Texture2D GetTexture()
    {
        return canvasTexture;
    }
    
    /// <summary>
    /// テクスチャを強制的に更新（Undo/Redo時など）
    /// </summary>
    public void ForceUpdate()
    {
        UpdateTexture();
    }
    
    void OnDestroy()
    {
        if (paintCanvas != null)
        {
            paintCanvas.OnPaintCompleted -= OnPaintCompleted;
            paintCanvas.OnPaintingSuppressed -= OnPaintingSuppressed;
        }
        
        if (canvasTexture != null)
        {
            Destroy(canvasTexture);
        }
    }
}
```

### PaintCanvasへの拡張（色データ管理）

`PaintCanvas.cs`に以下のメソッドとデータを追加する必要があります：

```csharp
public class PaintCanvas : MonoBehaviour, IPaintCanvas
{
    // 既存のコード...
    
    // 色データを管理する2D配列（Phase 2で追加）
    private Color[,] colorData;
    
    // 強度データを管理する2D配列（Phase 2で追加）
    private float[,] intensityData;
    
    void InitializeCanvas()
    {
        paintData = new int[settings.textureWidth, settings.textureHeight];
        colorData = new Color[settings.textureWidth, settings.textureHeight]; // 追加
        intensityData = new float[settings.textureWidth, settings.textureHeight]; // 追加
        
        // 初期化...
    }
    
    // PaintAtメソッドを拡張（色パラメータを追加）
    public void PaintAt(Vector2 screenPosition, int playerId, float intensity, Color color)
    {
        // 既存の処理（単一ピクセル塗り）...
        
        // 色データと強度データを保存
        colorData[canvasX, canvasY] = color;
        intensityData[canvasX, canvasY] = effectiveIntensity;
        
        // イベント発火（色情報も含める）
        OnPaintCompleted?.Invoke(screenPosition, playerId, effectiveIntensity);
    }
    
    // 半径指定で塗る（ブラシタイプ用）
    public void PaintAtWithRadius(Vector2 screenPosition, int playerId, float intensity, Color color, float radius)
    {
        if (!isInitialized || settings == null) return;
        
        // 画面座標をキャンバス座標に変換
        int centerX = Mathf.RoundToInt((screenPosition.x / Screen.width) * settings.textureWidth);
        int centerY = Mathf.RoundToInt((screenPosition.y / Screen.height) * settings.textureHeight);
        
        // 半径をキャンバス座標系に変換
        float radiusInCanvas = (radius / Screen.width) * settings.textureWidth;
        int radiusPixels = Mathf.RoundToInt(radiusInCanvas);
        
        // 円形のブラシで塗る
        float effectiveIntensity = intensity * settings.paintIntensityMultiplier;
        
        for (int x = centerX - radiusPixels; x <= centerX + radiusPixels; x++)
        {
            for (int y = centerY - radiusPixels; y <= centerY + radiusPixels; y++)
            {
                if (x < 0 || x >= settings.textureWidth || y < 0 || y >= settings.textureHeight)
                    continue;
                
                // 円形の範囲内かチェック
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (distance <= radiusPixels)
                {
                    // 塗り処理
                    paintData[x, y] = playerId;
                    colorData[x, y] = color;
                    intensityData[x, y] = effectiveIntensity;
                }
            }
        }
        
        // イベント発火
        OnPaintCompleted?.Invoke(screenPosition, playerId, effectiveIntensity);
    }
    
    // キャンバス座標での色取得（PaintRenderer用）
    public Color GetColorAtCanvas(int x, int y)
    {
        if (colorData == null || x < 0 || x >= settings.textureWidth ||
            y < 0 || y >= settings.textureHeight)
        {
            return Color.clear;
        }
        return colorData[x, y];
    }
    
    // キャンバス座標でのプレイヤーID取得（PaintRenderer用）
    public int GetPlayerIdAtCanvas(int x, int y)
    {
        if (paintData == null || x < 0 || x >= settings.textureWidth ||
            y < 0 || y >= settings.textureHeight)
        {
            return 0;
        }
        return paintData[x, y];
    }
    
    // 設定を取得（PaintRenderer用）
    public PaintSettings GetSettings()
    {
        return settings;
    }
    
    // EraseAtメソッド（消しツール用）
    public void EraseAt(Vector2 position, float radius)
    {
        // 実装は仕様書の「PaintCanvas への拡張」セクションを参照
    }
}
```

### UI設定

1. **Canvas Display Imageの設定**:
   - UnityエディタでUI Canvasを作成
   - `Image`コンポーネントを持つGameObjectを作成
   - `PaintRenderer`の`Canvas Display Image`フィールドに設定

2. **表示サイズの調整**:
   - `RectTransform`で表示サイズを調整
   - 画面全体に表示する場合は、`Anchor Presets`で`Stretch`を選択

3. **レイヤー順序**:
   - 塗りキャンバスは背景レイヤーに配置
   - UI要素（ボタンなど）は前面レイヤーに配置

---

## 💾 Step 2.6: 保存機能（オプション）

### ファイル
`Assets/Main/Script/Creative/CreativeModeSaveSystem.cs`

### 役割
- 描いた絵を画像として保存
- 保存した画像の共有（プラットフォーム依存）

### 実装内容

```csharp
[CreateAssetMenu(fileName = "CreativeSaveSettings", menuName = "Game/Creative/Save Settings")]
public class CreativeSaveSettings : ScriptableObject
{
    [Header("Save Path")]
    public string saveDirectory = "CreativeExports";
    public string fileNameFormat = "Creative_{0:yyyyMMdd_HHmmss}.png";
    public bool includeTimestamp = true;
    
    [Header("Image Properties")]
    [Range(0.1f, 2f)] public float imageScale = 1f; // スケールファクター
}

public class CreativeModeSaveSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CreativeSaveSettings settings;
    
    [Header("References")]
    [SerializeField] private PaintCanvas paintCanvas;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button shareButton;
    
    public static event System.Action<string> OnImageSaved;
    public static event System.Action<bool> OnShareCompleted;
    
    void Start()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveCanvas);
        
        if (shareButton != null)
            shareButton.onClick.AddListener(ShareImage);
    }
    
    public void SaveCanvas()
    {
        if (paintCanvas == null) return;
        
        // PaintCanvasからテクスチャを取得
        Texture2D texture = paintCanvas.GetTexture();
        if (texture == null) return;
        
        // スケール適用
        int width = Mathf.RoundToInt(texture.width * settings.imageScale);
        int height = Mathf.RoundToInt(texture.height * settings.imageScale);
        
        // リサイズ
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(texture, rt);
        RenderTexture.active = rt;
        
        Texture2D resizedTexture = new Texture2D(width, height);
        resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resizedTexture.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        // PNGにエンコード
        byte[] pngData = resizedTexture.EncodeToPNG();
        Destroy(resizedTexture);
        
        // ファイルパス生成
        string fileName = settings.includeTimestamp
            ? string.Format(settings.fileNameFormat, System.DateTime.Now)
            : "CreativeDrawing.png";
        
        string directory = System.IO.Path.Combine(Application.persistentDataPath, settings.saveDirectory);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        string filePath = System.IO.Path.Combine(directory, fileName);
        
        // 保存
        System.IO.File.WriteAllBytes(filePath, pngData);
        
        Debug.Log($"Creative mode: Image saved to {filePath}");
        OnImageSaved?.Invoke(filePath);
    }
    
    public void ShareImage()
    {
        // まず保存
        SaveCanvas();
        
        // プラットフォーム依存の共有処理
        // 実装はプラットフォームごとに異なる（Native Share Pluginなど使用）
        // ここでは簡易的な実装
        
        #if UNITY_ANDROID || UNITY_IOS
        // モバイルプラットフォームでの共有処理
        // Native Share Pluginなどを使用
        #else
        // PCプラットフォームではファイルパスを表示
        Debug.Log("Share functionality is platform-specific. Please implement for your target platform.");
        #endif
        
        OnShareCompleted?.Invoke(true);
    }
}
```

---

## 🔗 既存システムとの統合

### PaintCanvas への追加機能

`PaintCanvas.cs`に以下のメソッドを追加する必要があります：

```csharp
// 消しツール用
public void EraseAt(Vector2 position, float radius)
{
    // 指定位置から半径内のピクセルをクリア
    int centerX = Mathf.RoundToInt(position.x);
    int centerY = Mathf.RoundToInt(position.y);
    int radiusPixels = Mathf.RoundToInt(radius);
    
    for (int x = centerX - radiusPixels; x <= centerX + radiusPixels; x++)
    {
        for (int y = centerY - radiusPixels; y <= centerY + radiusPixels; y++)
        {
            if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
            {
                float distance = Vector2.Distance(new Vector2(x, y), position);
                if (distance <= radius)
                {
                    playerIdData[x, y] = 0;
                    intensityData[x, y] = 0f;
                    colors[x, y] = Color.clear;
                }
            }
        }
    }
    
    // テクスチャを更新
    UpdateTexture();
}

// 履歴管理用
public CanvasState GetState()
{
    CanvasState state = new CanvasState(textureWidth, textureHeight);
    // データをコピー
    // ...
    return state;
}

public void RestoreState(CanvasState state)
{
    // 状態を復元
    // ...
    UpdateTexture();
}

// テクスチャ取得用
public Texture2D GetTexture()
{
    return canvasTexture; // 内部のTexture2Dを返す
}
```

### CanvasState クラス

```csharp
[System.Serializable]
public class CanvasState
{
    public int width;
    public int height;
    public int[,] playerIds;
    public float[,] intensities;
    public Color[,] colors;
    
    public CanvasState(int width, int height)
    {
        this.width = width;
        this.height = height;
        playerIds = new int[width, height];
        intensities = new float[width, height];
        colors = new Color[width, height];
    }
}
```

---

## ✅ 実装チェックリスト

### Step 2.1: クリエイティブモードマネージャー
- [ ] `CreativeModeManager.cs`を作成
- [ ] `CreativeToolMode` enumを定義
- [ ] `BrushType` enumを定義（Pencil, Paint）
- [ ] ツールモード切り替え機能を実装
- [ ] ブラシタイプ切り替え機能を実装
- [ ] 色設定機能を実装
- [ ] 音声入力からの塗り処理を実装
- [ ] 鉛筆ブラシの実装（`PaintWithPencil`）
- [ ] ペンキブラシの実装（`PaintWithPaint`）- 将来的な拡張
- [ ] 履歴管理（Undo/Redo）を実装
- [ ] キャンバスクリア機能を実装
- [ ] イベント発火を実装

### Step 2.2: 色選択システム
- [ ] `ColorSelectionSystem.cs`を作成
- [ ] プリセット色の管理を実装
- [ ] 色選択機能を実装
- [ ] `CreativeModeManager`との連携を実装

### Step 2.3: 設定ファイル
- [ ] `CreativeModeSettings.cs`を作成
- [ ] `ColorSelectionSettings.cs`を作成
- [ ] ScriptableObjectアセットを作成

### Step 2.4: UI
- [ ] `CreativeModeUI.cs`を作成
- [ ] ツール切り替えボタンを実装
- [ ] ブラシタイプ切り替えボタンを実装（鉛筆、ペンキ）
- [ ] 色選択ボタンを実装
- [ ] クリアボタンを実装
- [ ] Undoボタンを実装
- [ ] UI状態の更新を実装（ツール、ブラシタイプ、色、Undo）

### Step 2.5: 描画システム
- [ ] `PaintRenderer.cs`を作成
- [ ] `Texture2D`の生成・更新機能を実装
- [ ] UI Imageへの表示機能を実装
- [ ] リアルタイム更新機能を実装
- [ ] `PaintCanvas`に色データ管理機能を追加
- [ ] `PaintCanvas.GetColorAtCanvas`メソッドを追加
- [ ] `PaintCanvas.GetPlayerIdAtCanvas`メソッドを追加
- [ ] `PaintCanvas.GetSettings`メソッドを追加

### Step 2.6: 保存機能（オプション）
- [ ] `CreativeModeSaveSystem.cs`を作成
- [ ] `CreativeSaveSettings.cs`を作成
- [ ] 画像保存機能を実装
- [ ] 共有機能を実装（プラットフォーム依存）

### PaintCanvas 拡張
- [ ] `EraseAt`メソッドを追加
- [ ] `PaintAtWithRadius`メソッドを追加（ブラシタイプ用）
- [ ] `GetState`メソッドを追加
- [ ] `RestoreState`メソッドを追加
- [ ] `GetTexture`メソッドを追加
- [ ] `CanvasState`クラスを追加
- [ ] 色データ管理（`Color[,] colorData`）を追加
- [ ] 強度データ管理（`float[,] intensityData`）を追加

---

## 🎮 使用方法

### 1. シーン設定

1. 新しいシーンを作成（例: `CreativeModeScene.unity`）
2. 以下のGameObjectを作成：
   - `CreativeModeManager` - `CreativeModeManager`コンポーネントをアタッチ
   - `ColorSelectionSystem` - `ColorSelectionSystem`コンポーネントをアタッチ
   - `CreativeModeUI` - `CreativeModeUI`コンポーネントをアタッチ
   - `PaintCanvas` - 既存の`PaintCanvas`コンポーネント
   - `PaintBattleGameManager` - 既存の`PaintBattleGameManager`コンポーネント
   - `VoiceToScreenMapper` - 既存の`VoiceToScreenMapper`コンポーネント
   - `InkEffect` - 既存の`InkEffect`コンポーネント
   - `PaintRenderer` - `PaintRenderer`コンポーネント（新規作成）

### 2. Inspector設定

#### CreativeModeManager
- `Settings`: `CreativeModeSettings`アセットを設定
- `Paint Canvas`: `PaintCanvas`コンポーネントを設定
- `Paint Game Manager`: `PaintBattleGameManager`コンポーネントを設定
- `Voice Mapper`: `VoiceToScreenMapper`コンポーネントを設定
- `Pitch Analyzer`: `ImprovedPitchAnalyzer`コンポーネントを設定
- `Volume Analyzer`: `VolumeAnalyzer`コンポーネントを設定
- `Color Selection System`: `ColorSelectionSystem`コンポーネントを設定

#### ColorSelectionSystem
- `Settings`: `ColorSelectionSettings`アセットを設定
- `Creative Mode Manager`: `CreativeModeManager`コンポーネントを設定

#### CreativeModeUI
- `Creative Mode Manager`: `CreativeModeManager`コンポーネントを設定
- `Color Selection System`: `ColorSelectionSystem`コンポーネントを設定
- UI要素（ボタン、ラベルなど）を設定

#### PaintRenderer
- `Paint Canvas`: `PaintCanvas`コンポーネントを設定
- `Canvas Display Image`: 塗りキャンバスを表示するUI Imageを設定
- `Canvas Display Rect`: 表示領域のRectTransformを設定（オプション）

### 3. ScriptableObject作成

1. `Assets/ScriptableObjects/Creative/`フォルダを作成
2. `Create > Game > Creative Mode Settings`で設定アセットを作成
3. `Create > Game > Color Selection Settings`で設定アセットを作成
4. 必要に応じて`Create > Game > Creative > Save Settings`で保存設定アセットを作成

---

## 📝 注意事項

1. **Phase 1の依存**: Phase 1の`PaintCanvas`、`PaintBattleGameManager`が完成している必要があります
2. **描画システム**: `PaintRenderer`は`PaintCanvas`の色データに依存します。`PaintCanvas`に色データ管理機能を追加する必要があります
3. **パフォーマンス**: テクスチャの更新は重い処理になる可能性があります。`updateFrequency`を調整してパフォーマンスを最適化してください
4. **履歴管理**: 履歴サイズが大きすぎるとメモリ使用量が増加します。適切なサイズに設定してください
5. **保存機能**: プラットフォーム依存の共有機能は、Native Share Pluginなどの外部アセットを使用することを推奨します
6. **パフォーマンス**: 履歴の保存・復元は重い処理になる可能性があります。必要に応じて最適化してください

---

## 🔄 将来の拡張

- **レイヤーシステム**: 複数のレイヤーで描画
- **ブラシサイズ調整**: 音声の音量でブラシサイズを変更
- **テクスチャブラシ**: 異なるテクスチャのブラシ
- **アニメーション記録**: 描画過程をアニメーションとして記録
- **クラウド保存**: 描いた絵をクラウドに保存

