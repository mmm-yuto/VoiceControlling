using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ColorDefense モード用の「バトル前設定」ロビー UI。
/// - プレイヤー色（2色のうち 1 つ）/ CPU 色（残りの 1 色）
/// - Brush（塗り方）
/// - 相手レベル
/// - バトル時間
/// を選択し、BattleSettings に反映してからゲーム開始する。
/// 
/// 将来のオンライン対応を見据え、実際のゲームロジックとは BattleSettingsData 経由で疎結合にしている。
/// </summary>
public class ColorDefenseLobbyPanel : MonoBehaviour
{
    [Header("Managers")]
    [Tooltip("シングルプレイのゲームモード管理")]
    [SerializeField] private SinglePlayerModeManager singlePlayerModeManager;

    [Tooltip("バトル設定のシングルトン。未指定の場合は BattleSettings.Instance を使用")]
    [SerializeField] private BattleSettings battleSettings;
    
    [Header("Countdown UI")]
    [Tooltip("カウントダウンUI（ColorDefense UI root内に配置。未指定の場合は自動検索）")]
    [SerializeField] private ColorDefenseCountdownUI countdownUI;

    [Header("Enemy Level UI")]
    [Tooltip("相手レベルを選択するスライダー（整数値想定）")]
    [SerializeField] private Slider enemyLevelSlider;

    [Tooltip("相手レベルを表示するテキスト (例: Lv. 3)")]
    [SerializeField] private TextMeshProUGUI enemyLevelLabel;

    [Tooltip("相手レベルの最小値")]
    [SerializeField] private int minEnemyLevel = 1;

    [Tooltip("相手レベルの最大値")]
    [SerializeField] private int maxEnemyLevel = 5;

    [Header("Battle Time UI")]
    [Tooltip("バトル時間を選択するスライダー（インデックス指定：0〜(オプション数-1)）")]
    [SerializeField] private Slider battleTimeSlider;

    [Tooltip("バトル時間を表示するテキスト (例: 60s, 2m00s)")]
    [SerializeField] private TextMeshProUGUI battleTimeLabel;

    [Tooltip("利用可能なバトル時間（秒）")]
    [SerializeField] private float[] battleTimeOptionsSeconds = new float[] { 60f, 120f, 180f };

    [Header("Start Button")]
    [Tooltip("バトル開始ボタン")]
    [SerializeField] private Button startBattleButton;

    [Header("Color Selection Buttons")]
    [Tooltip("色A選択ボタン")]
    [SerializeField] private Button colorAButton;
    
    [Tooltip("色B選択ボタン")]
    [SerializeField] private Button colorBButton;

    [Header("Brush Selection Buttons")]
    [Tooltip("Pencil Brush選択ボタン")]
    [SerializeField] private Button pencilBrushButton;
    
    [Tooltip("Paint Brush選択ボタン")]
    [SerializeField] private Button paintBrushButton;
    
    [Tooltip("Spray Brush選択ボタン")]
    [SerializeField] private Button sprayBrushButton;

    [Header("Color Selection (Indices)")]
    [Tooltip("プレイヤーが左側の色ボタンを選んだときに使用するプレイヤー色インデックス")]
    [SerializeField] private int colorAPlayerIndex = 0;

    [Tooltip("プレイヤーが右側の色ボタンを選んだときに使用するプレイヤー色インデックス")]
    [SerializeField] private int colorBPlayerIndex = 1;

    [Header("Brush ScriptableObjects")]
    [Tooltip("Pencil Brush の ScriptableObject")]
    [SerializeField] private BrushStrategyBase pencilBrush;

    [Tooltip("Paint Brush の ScriptableObject")]
    [SerializeField] private BrushStrategyBase paintBrush;

    [Tooltip("Spray Brush の ScriptableObject")]
    [SerializeField] private BrushStrategyBase sprayBrush;

    [Header("Visual Settings")]
    [Tooltip("選択中のボタンの色（通常色に対して乗算）")]
    [SerializeField] private Color selectedButtonColor = new Color(1.2f, 1.2f, 1.0f, 1f); // 明るい黄色系
    [Tooltip("選択されていないボタンの色")]
    [SerializeField] private Color normalButtonColor = Color.white;
    [Tooltip("選択中のボタンのスケール")]
    [SerializeField] private float selectedButtonScale = 1.1f; // 10%大きく
    [Tooltip("選択されていないボタンのスケール")]
    [SerializeField] private float normalButtonScale = 1.0f;

    // 作業用の設定データ。UI から編集し、StartBattle 時に BattleSettings に渡す。
    private readonly BattleSettingsData _workingData = new BattleSettingsData();

    private bool _hasColorSelection = false;
    private bool _hasBrushSelection = false;
    
    // 選択状態を追跡
    private Button selectedColorButton = null;
    private Button selectedBrushButton = null;

    void Awake()
    {
        // BattleSettings の参照が未設定ならシングルトンから取得
        if (battleSettings == null)
        {
            battleSettings = BattleSettings.Instance;
        }

        // 敵レベルスライダーの初期設定
        if (enemyLevelSlider != null)
        {
            enemyLevelSlider.minValue = minEnemyLevel;
            enemyLevelSlider.maxValue = maxEnemyLevel;
            enemyLevelSlider.wholeNumbers = true;
            enemyLevelSlider.value = minEnemyLevel;
            UpdateEnemyLevel((int)enemyLevelSlider.value);
        }

        // バトル時間スライダーの初期設定
        if (battleTimeSlider != null && battleTimeOptionsSeconds != null && battleTimeOptionsSeconds.Length > 0)
        {
            battleTimeSlider.minValue = 0;
            battleTimeSlider.maxValue = battleTimeOptionsSeconds.Length - 1;
            battleTimeSlider.wholeNumbers = true;

            int defaultIndex = 0;
            battleTimeSlider.value = defaultIndex;
            UpdateBattleTimeFromIndex(defaultIndex);
        }

        // ボタンのイベント接続
        SetupButtons();

        // ブラシボタンのアイコンを設定
        SetupBrushButtonIcons();

        UpdateStartButtonInteractable();
    }
    
    /// <summary>
    /// ブラシボタンのアイコンを設定
    /// </summary>
    private void SetupBrushButtonIcons()
    {
        // Pencil Brushボタン
        if (pencilBrushButton != null && pencilBrush != null)
        {
            Image buttonImage = pencilBrushButton.GetComponent<Image>();
            if (buttonImage != null && pencilBrush.GetIcon() != null)
            {
                buttonImage.sprite = pencilBrush.GetIcon();
            }
        }
        
        // Paint Brushボタン
        if (paintBrushButton != null && paintBrush != null)
        {
            Image buttonImage = paintBrushButton.GetComponent<Image>();
            if (buttonImage != null && paintBrush.GetIcon() != null)
            {
                buttonImage.sprite = paintBrush.GetIcon();
            }
        }
        
        // Spray Brushボタン
        if (sprayBrushButton != null && sprayBrush != null)
        {
            Image buttonImage = sprayBrushButton.GetComponent<Image>();
            if (buttonImage != null && sprayBrush.GetIcon() != null)
            {
                buttonImage.sprite = sprayBrush.GetIcon();
            }
        }
    }

    /// <summary>
    /// ボタンのイベントを設定（自動接続）
    /// </summary>
    private void SetupButtons()
    {
        // 色選択ボタン
        if (colorAButton != null)
        {
            colorAButton.onClick.RemoveAllListeners();
            colorAButton.onClick.AddListener(OnSelectColorA);
        }

        if (colorBButton != null)
        {
            colorBButton.onClick.RemoveAllListeners();
            colorBButton.onClick.AddListener(OnSelectColorB);
        }

        // ブラシ選択ボタン
        if (pencilBrushButton != null)
        {
            pencilBrushButton.onClick.RemoveAllListeners();
            pencilBrushButton.onClick.AddListener(OnSelectPencilBrush);
        }

        if (paintBrushButton != null)
        {
            paintBrushButton.onClick.RemoveAllListeners();
            paintBrushButton.onClick.AddListener(OnSelectPaintBrush);
        }

        if (sprayBrushButton != null)
        {
            sprayBrushButton.onClick.RemoveAllListeners();
            sprayBrushButton.onClick.AddListener(OnSelectSprayBrush);
        }

        // 開始ボタン
        if (startBattleButton != null)
        {
            startBattleButton.onClick.RemoveAllListeners();
            startBattleButton.onClick.AddListener(OnClickStartBattle);
        }

        // スライダーのイベント接続
        if (enemyLevelSlider != null)
        {
            enemyLevelSlider.onValueChanged.RemoveAllListeners();
            enemyLevelSlider.onValueChanged.AddListener(OnEnemyLevelSliderChanged);
        }

        // バトル時間スライダーのイベント接続
        if (battleTimeSlider != null)
        {
            battleTimeSlider.onValueChanged.RemoveAllListeners();
            battleTimeSlider.onValueChanged.AddListener(OnBattleTimeSliderChanged);
        }
    }

    /// <summary>
    /// ロビーを表示する前に呼び出して、作業データと UI を初期化する。
    /// （GameModeSelectionPanel などから呼ぶ想定。）
    /// </summary>
    public void Open()
    {
        ResetWorkingData();
        SyncUIWithWorkingData();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// ロビーを閉じる。
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 色 A ボタンが押されたとき（例: 左側の色）。
    /// プレイヤーに colorAPlayerIndex の色を割り当て、CPU には別の色を割り当てる。
    /// </summary>
    public void OnSelectColorA()
    {
        _workingData.playerColorIndex = colorAPlayerIndex;
        // CPU は別の色（単純に 0/1 を入れ替える想定だが、BattleSettings 側で安全に補正される）
        _workingData.cpuColorIndex = colorBPlayerIndex;

        // 実際の色を設定
        if (battleSettings == null)
        {
            battleSettings = BattleSettings.Instance;
        }
        if (battleSettings != null)
        {
            _workingData.playerColor = battleSettings.GetColorFromIndex(colorAPlayerIndex);
            _workingData.cpuColor = battleSettings.GetColorFromIndex(colorBPlayerIndex);
        }

        _hasColorSelection = true;
        selectedColorButton = colorAButton;
        UpdateButtonVisuals();
        UpdateStartButtonInteractable();
        
        // Immediately apply settings to BattleSettings for preview
        ApplySettingsToBattleSettings();
    }

    /// <summary>
    /// 色 B ボタンが押されたとき（例: 右側の色）。
    /// </summary>
    public void OnSelectColorB()
    {
        _workingData.playerColorIndex = colorBPlayerIndex;
        _workingData.cpuColorIndex = colorAPlayerIndex;

        // 実際の色を設定
        if (battleSettings == null)
        {
            battleSettings = BattleSettings.Instance;
        }
        if (battleSettings != null)
        {
            _workingData.playerColor = battleSettings.GetColorFromIndex(colorBPlayerIndex);
            _workingData.cpuColor = battleSettings.GetColorFromIndex(colorAPlayerIndex);
        }

        _hasColorSelection = true;
        selectedColorButton = colorBButton;
        UpdateButtonVisuals();
        UpdateStartButtonInteractable();
        
        // Immediately apply settings to BattleSettings for preview
        ApplySettingsToBattleSettings();
    }

    /// <summary>
    /// Pencil Brush ボタン用。
    /// </summary>
    public void OnSelectPencilBrush()
    {
        SetBrush(pencilBrush, pencilBrushButton);
    }

    /// <summary>
    /// Paint Brush ボタン用。
    /// </summary>
    public void OnSelectPaintBrush()
    {
        SetBrush(paintBrush, paintBrushButton);
    }

    /// <summary>
    /// Spray Brush ボタン用。
    /// </summary>
    public void OnSelectSprayBrush()
    {
        SetBrush(sprayBrush, sprayBrushButton);
    }

    /// <summary>
    /// Brush を設定。
    /// </summary>
    /// <param name="brush">Brush の ScriptableObject</param>
    /// <param name="button">選択されたボタン</param>
    private void SetBrush(BrushStrategyBase brush, Button button)
    {
        if (brush == null)
        {
            Debug.LogWarning("ColorDefenseLobbyPanel.SetBrush: Brush が null です。");
            return;
        }

        // brushKeyはScriptableObjectの名前を使用
        _workingData.brushKey = brush.name;
        // Brushの参照も保存
        _workingData.brush = brush;
        _hasBrushSelection = true;
        
        // 選択されたボタンを記録
        selectedBrushButton = button;
        
        UpdateButtonVisuals();
        UpdateStartButtonInteractable();
        
        // Immediately apply settings to BattleSettings for preview
        ApplySettingsToBattleSettings();
    }

    /// <summary>
    /// レベルスライダーの値変更イベントから呼び出す。
    /// </summary>
    /// <param name="value">スライダーの値（float だが内部的には整数に丸める）</param>
    public void OnEnemyLevelSliderChanged(float value)
    {
        int level = Mathf.RoundToInt(value);
        level = Mathf.Clamp(level, minEnemyLevel, maxEnemyLevel);
        UpdateEnemyLevel(level);
    }

    /// <summary>
    /// バトル時間スライダーの値変更イベントから呼び出す（インデックス指定）。
    /// </summary>
    /// <param name="value">スライダー値（0〜オプション数-1）</param>
    public void OnBattleTimeSliderChanged(float value)
    {
        if (battleTimeOptionsSeconds == null || battleTimeOptionsSeconds.Length == 0)
        {
            Debug.LogWarning("ColorDefenseLobbyPanel.OnBattleTimeSliderChanged: battleTimeOptionsSeconds が設定されていません。");
            return;
        }

        int index = Mathf.RoundToInt(value);
        index = Mathf.Clamp(index, 0, battleTimeOptionsSeconds.Length - 1);
        UpdateBattleTimeFromIndex(index);
    }

    /// <summary>
    /// 「バトル開始」ボタンから呼び出す。
    /// BattleSettings に作業データを渡し、SinglePlayerModeManager 経由で ColorDefense モードを開始する。
    /// </summary>
    public void OnClickStartBattle()
    {
        if (!IsReadyToStart())
        {
            Debug.LogWarning("ColorDefenseLobbyPanel.OnClickStartBattle: 必要な項目が選択されていません。");
            return;
        }

        if (battleSettings == null)
        {
            battleSettings = BattleSettings.Instance;
        }

        if (battleSettings == null)
        {
            Debug.LogError("ColorDefenseLobbyPanel.OnClickStartBattle: BattleSettings インスタンスが見つかりません。");
            return;
        }

        // BattleSettings に UI からの設定を反映
        battleSettings.SetFromUI(_workingData);

        // ゲーム開始フラグはまだ立てない（カウントダウン終了後に立てる）
        // battleSettings.SetGameStarted(true); // Moved to OnCountdownCompleted

        // 既存の SinglePlayer 設定にゲーム時間・難易度を反映
        battleSettings.ApplyToSinglePlayerSettings();

        // ColorDefense モードでゲーム開始（初期化とUIの有効化）
        if (singlePlayerModeManager != null)
        {
            singlePlayerModeManager.InitializeMode(SinglePlayerGameModeType.ColorDefense);
            
            // カウントダウン中はゲームを一時停止
            // InitializeMode()がStartGame()を呼ぶので、その直後にPause()を呼ぶ
            ColorDefenseMode colorDefenseMode = FindObjectOfType<ColorDefenseMode>();
            if (colorDefenseMode != null)
            {
                colorDefenseMode.Pause();
            }
            
            // プレイヤーの塗りを一時停止
            PaintBattleGameManager paintBattleGameManager = FindObjectOfType<PaintBattleGameManager>();
            if (paintBattleGameManager != null)
            {
                paintBattleGameManager.SetGameActive(false);
            }
        }
        else
        {
            Debug.LogWarning("ColorDefenseLobbyPanel.OnClickStartBattle: SinglePlayerModeManager が設定されていません。");
        }

        // ロビーを閉じる
        Close();
        
        // カウントダウンを開始
        if (countdownUI == null)
        {
            countdownUI = FindObjectOfType<ColorDefenseCountdownUI>();
        }
        
        if (countdownUI != null)
        {
            countdownUI.StartCountdown(OnCountdownCompleted);
        }
        else
        {
            Debug.LogWarning("ColorDefenseLobbyPanel.OnClickStartBattle: ColorDefenseCountdownUI が見つかりません。カウントダウンなしでゲームを開始します。");
            // フォールバック: カウントダウンがない場合はすぐにゲーム開始
            OnCountdownCompleted();
        }
    }

    /// <summary>
    /// カウントダウン完了時のコールバック
    /// ゲームを開始する
    /// </summary>
    private void OnCountdownCompleted()
    {
        // ゲーム開始フラグを立てる
        if (battleSettings != null)
        {
            battleSettings.SetGameStarted(true);
        }
        
        // ColorDefenseModeを再開
        ColorDefenseMode colorDefenseMode = FindObjectOfType<ColorDefenseMode>();
        if (colorDefenseMode != null)
        {
            colorDefenseMode.Resume();
        }
        
        // プレイヤーの塗りを再開
        PaintBattleGameManager paintBattleGameManager = FindObjectOfType<PaintBattleGameManager>();
        if (paintBattleGameManager != null)
        {
            paintBattleGameManager.SetGameActive(true);
        }
    }

    /// <summary>
    /// インデックスからバトル時間を更新し、スライダーとラベルに反映する。
    /// </summary>
    private void UpdateBattleTimeFromIndex(int index)
    {
        if (battleTimeOptionsSeconds == null || battleTimeOptionsSeconds.Length == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, battleTimeOptionsSeconds.Length - 1);
        float seconds = battleTimeOptionsSeconds[index];

        _workingData.battleDurationSeconds = seconds;

        if (battleTimeSlider != null)
        {
            battleTimeSlider.SetValueWithoutNotify(index);
        }

        if (battleTimeLabel != null)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);

            string text = minutes > 0
                ? $"{minutes}m {secs:00}s"
                : $"{secs}s";

            battleTimeLabel.text = text;
        }
    }

    /// <summary>
    /// スライダーとラベルにレベル値を反映。
    /// </summary>
    private void UpdateEnemyLevel(int level)
    {
        _workingData.enemyLevel = level;

        if (enemyLevelSlider != null)
        {
            enemyLevelSlider.SetValueWithoutNotify(level);
        }

        if (enemyLevelLabel != null)
        {
            enemyLevelLabel.text = $"Lv {level}";
        }
    }

    /// <summary>
    /// 「バトル開始」ボタンの活性/非活性を更新。
    /// 色と Brush が選択されていることを開始条件とする。
    /// </summary>
    private void UpdateStartButtonInteractable()
    {
        if (startBattleButton == null)
        {
            return;
        }

        startBattleButton.interactable = IsReadyToStart();
    }

    /// <summary>
    /// ボタンの視覚状態を更新（選択中のボタンをハイライト）
    /// </summary>
    private void UpdateButtonVisuals()
    {
        // 色選択ボタンの視覚更新
        UpdateButtonVisual(colorAButton, colorAButton == selectedColorButton);
        UpdateButtonVisual(colorBButton, colorBButton == selectedColorButton);
        
        // ブラシ選択ボタンの視覚更新
        UpdateButtonVisual(pencilBrushButton, pencilBrushButton == selectedBrushButton);
        UpdateButtonVisual(paintBrushButton, paintBrushButton == selectedBrushButton);
        UpdateButtonVisual(sprayBrushButton, sprayBrushButton == selectedBrushButton);
    }

    /// <summary>
    /// ボタンの視覚状態を更新（色とスケール）
    /// </summary>
    private void UpdateButtonVisual(Button button, bool isSelected)
    {
        if (button == null) return;
        
        // 色を更新（色選択ボタンの場合は実際の色を設定、それ以外は選択状態に応じた色）
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (button == colorAButton || button == colorBButton)
            {
                // 色選択ボタンの場合は、BattleSettingsから実際の色を取得
                Color buttonColor = GetButtonColor(button);
                if (isSelected)
                {
                    // 選択中は少し明るくする
                    buttonImage.color = buttonColor * 1.2f;
                }
                else
                {
                    buttonImage.color = buttonColor;
                }
            }
            else if (button == pencilBrushButton || button == paintBrushButton || button == sprayBrushButton)
            {
                // ブラシ選択ボタン: アイコンは既に設定済み、色のみ変更
                buttonImage.color = isSelected ? selectedButtonColor : normalButtonColor;
            }
            else
            {
                // その他のボタンは従来通り
                buttonImage.color = isSelected ? selectedButtonColor : normalButtonColor;
            }
        }
        
        // スケールを更新
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            float scale = isSelected ? selectedButtonScale : normalButtonScale;
            buttonRect.localScale = Vector3.one * scale;
        }
    }

    /// <summary>
    /// ボタンに対応する色を取得（BattleSettingsから取得）
    /// </summary>
    private Color GetButtonColor(Button button)
    {
        if (battleSettings == null)
        {
            battleSettings = BattleSettings.Instance;
        }

        if (button == colorAButton && battleSettings != null)
        {
            // colorAButtonは常にcolorAPlayerIndexの色を表示
            // BattleSettingsで設定された色（ColorSelectionSettingsから取得）を使用
            return battleSettings.GetColorFromIndex(colorAPlayerIndex);
        }
        else if (button == colorBButton && battleSettings != null)
        {
            // colorBButtonは常にcolorBPlayerIndexの色を表示
            // BattleSettingsで設定された色（ColorSelectionSettingsから取得）を使用
            return battleSettings.GetColorFromIndex(colorBPlayerIndex);
        }

        return Color.white; // フォールバック
    }

    private bool IsReadyToStart()
    {
        return _hasColorSelection && _hasBrushSelection;
    }

    /// <summary>
    /// 作業データを初期化し、最低限のデフォルト値を設定する。
    /// 初期状態ではButtonAとSprayBrushが選択されている。
    /// </summary>
    private void ResetWorkingData()
    {
        _workingData.playerColorIndex = colorAPlayerIndex;
        _workingData.cpuColorIndex = colorBPlayerIndex;
        _workingData.enemyLevel = minEnemyLevel;

        // 実際の色を設定
        if (battleSettings == null)
        {
            battleSettings = BattleSettings.Instance;
        }
        if (battleSettings != null)
        {
            _workingData.playerColor = battleSettings.GetColorFromIndex(colorAPlayerIndex);
            _workingData.cpuColor = battleSettings.GetColorFromIndex(colorBPlayerIndex);
        }

        // バトル時間はスライダー（オプション）から設定される想定だが、念のためデフォルトを設定
        if (battleTimeOptionsSeconds != null && battleTimeOptionsSeconds.Length > 0)
        {
            _workingData.battleDurationSeconds = battleTimeOptionsSeconds[0];
        }
        else
        {
            _workingData.battleDurationSeconds = 180f;
        }

        // 初期状態でButtonA（ColorA）を選択
        if (colorAButton != null)
        {
            _workingData.playerColorIndex = colorAPlayerIndex;
            _workingData.cpuColorIndex = colorBPlayerIndex;
            if (battleSettings != null)
            {
                _workingData.playerColor = battleSettings.GetColorFromIndex(colorAPlayerIndex);
                _workingData.cpuColor = battleSettings.GetColorFromIndex(colorBPlayerIndex);
            }
            _hasColorSelection = true;
            selectedColorButton = colorAButton;
        }
        else
        {
            _hasColorSelection = false;
            selectedColorButton = null;
        }

        // 初期状態でSprayBrushを選択
        if (sprayBrush != null)
        {
            _workingData.brushKey = sprayBrush.name;
            _workingData.brush = sprayBrush;
            _hasBrushSelection = true;
            selectedBrushButton = sprayBrushButton;
        }
        else
        {
            _workingData.brushKey = string.Empty;
            _workingData.brush = null;
            _hasBrushSelection = false;
            selectedBrushButton = null;
        }
        
        UpdateButtonVisuals();
        UpdateStartButtonInteractable();
    }

    /// <summary>
    /// Apply current working data to BattleSettings immediately for preview
    /// </summary>
    private void ApplySettingsToBattleSettings()
    {
        if (battleSettings == null)
        {
            battleSettings = BattleSettings.Instance;
        }
        
        if (battleSettings != null)
        {
            battleSettings.SetFromUI(_workingData);
        }
    }

    /// <summary>
    /// 作業データの内容を UI に反映する。
    /// Open() 時などに使用。
    /// </summary>
    private void SyncUIWithWorkingData()
    {
        UpdateEnemyLevel(_workingData.enemyLevel);

        // バトル時間スライダーの選択を反映
        if (battleTimeSlider != null && battleTimeOptionsSeconds != null && battleTimeOptionsSeconds.Length > 0)
        {
            int index = 0;
            float current = _workingData.battleDurationSeconds;
            for (int i = 0; i < battleTimeOptionsSeconds.Length; i++)
            {
                if (Mathf.Approximately(battleTimeOptionsSeconds[i], current))
                {
                    index = i;
                    break;
                }
            }

            UpdateBattleTimeFromIndex(index);
        }
    }
}

