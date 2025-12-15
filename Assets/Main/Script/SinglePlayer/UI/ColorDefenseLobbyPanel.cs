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

    [Header("Enemy Level UI")]
    [Tooltip("相手レベルを選択するスライダー（整数値想定）")]
    [SerializeField] private Slider enemyLevelSlider;

    [Tooltip("相手レベルを表示するテキスト (例: Lv. 3)")]
    [SerializeField] private TextMeshProUGUI enemyLevelLabel;

    [Tooltip("相手レベルの最小値")]
    [SerializeField] private int minEnemyLevel = 1;

    [Tooltip("相手レベルの最大値")]
    [SerializeField] private int maxEnemyLevel = 10;

    [Header("Battle Time UI")]
    [Tooltip("バトル時間を選択するドロップダウン (TMP)")]
    [SerializeField] private TMP_Dropdown battleTimeDropdown;

    [Tooltip("ドロップダウンの各オプションに対応するバトル時間（秒）")]
    [SerializeField] private float[] battleTimeOptionsSeconds = new float[] { 60f, 120f, 180f };

    [Header("Start Button")]
    [Tooltip("バトル開始ボタン")]
    [SerializeField] private Button startBattleButton;

    [Header("Color Selection (Indices)")]
    [Tooltip("プレイヤーが左側の色ボタンを選んだときに使用するプレイヤー色インデックス")]
    [SerializeField] private int colorAPlayerIndex = 0;

    [Tooltip("プレイヤーが右側の色ボタンを選んだときに使用するプレイヤー色インデックス")]
    [SerializeField] private int colorBPlayerIndex = 1;

    [Header("Brush Keys (for Buttons)")]
    [Tooltip("Pencil Brush ボタンが設定する brushKey")]
    [SerializeField] private string pencilBrushKey = "PencilBrush";

    [Tooltip("Paint Brush ボタンが設定する brushKey")]
    [SerializeField] private string paintBrushKey = "PaintBrush";

    [Tooltip("Spray Brush ボタンが設定する brushKey")]
    [SerializeField] private string sprayBrushKey = "SprayBrush";

    // 作業用の設定データ。UI から編集し、StartBattle 時に BattleSettings に渡す。
    private readonly BattleSettingsData _workingData = new BattleSettingsData();

    private bool _hasColorSelection = false;
    private bool _hasBrushSelection = false;

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

        // バトル時間ドロップダウンの初期値
        if (battleTimeDropdown != null && battleTimeOptionsSeconds != null && battleTimeOptionsSeconds.Length > 0)
        {
            battleTimeDropdown.value = 0;
            OnBattleTimeDropdownChanged(0);
        }

        UpdateStartButtonInteractable();
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

        _hasColorSelection = true;
        UpdateStartButtonInteractable();
    }

    /// <summary>
    /// 色 B ボタンが押されたとき（例: 右側の色）。
    /// </summary>
    public void OnSelectColorB()
    {
        _workingData.playerColorIndex = colorBPlayerIndex;
        _workingData.cpuColorIndex = colorAPlayerIndex;

        _hasColorSelection = true;
        UpdateStartButtonInteractable();
    }

    /// <summary>
    /// Pencil Brush ボタン用。
    /// </summary>
    public void OnSelectPencilBrush()
    {
        SetBrushKey(pencilBrushKey);
    }

    /// <summary>
    /// Paint Brush ボタン用。
    /// </summary>
    public void OnSelectPaintBrush()
    {
        SetBrushKey(paintBrushKey);
    }

    /// <summary>
    /// Spray Brush ボタン用。
    /// </summary>
    public void OnSelectSprayBrush()
    {
        SetBrushKey(sprayBrushKey);
    }

    /// <summary>
    /// 汎用的な Brush キー設定。
    /// </summary>
    /// <param name="key">Brush を識別するキー</param>
    private void SetBrushKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("ColorDefenseLobbyPanel.SetBrushKey: 空の brushKey は設定できません。");
            return;
        }

        _workingData.brushKey = key;
        _hasBrushSelection = true;
        UpdateStartButtonInteractable();
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
    /// バトル時間ドロップダウンの値変更イベントから呼び出す。
    /// </summary>
    /// <param name="optionIndex">選択されたオプションのインデックス</param>
    public void OnBattleTimeDropdownChanged(int optionIndex)
    {
        if (battleTimeOptionsSeconds == null || battleTimeOptionsSeconds.Length == 0)
        {
            Debug.LogWarning("ColorDefenseLobbyPanel.OnBattleTimeDropdownChanged: battleTimeOptionsSeconds が設定されていません。");
            return;
        }

        if (optionIndex < 0 || optionIndex >= battleTimeOptionsSeconds.Length)
        {
            Debug.LogWarning($"ColorDefenseLobbyPanel.OnBattleTimeDropdownChanged: optionIndex {optionIndex} が範囲外です。");
            optionIndex = Mathf.Clamp(optionIndex, 0, battleTimeOptionsSeconds.Length - 1);
        }

        _workingData.battleDurationSeconds = battleTimeOptionsSeconds[optionIndex];
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

        // 既存の SinglePlayer 設定にゲーム時間・難易度を反映
        battleSettings.ApplyToSinglePlayerSettings();

        // ColorDefense モードでゲーム開始
        if (singlePlayerModeManager != null)
        {
            singlePlayerModeManager.InitializeMode(SinglePlayerGameModeType.ColorDefense);
        }
        else
        {
            Debug.LogWarning("ColorDefenseLobbyPanel.OnClickStartBattle: SinglePlayerModeManager が設定されていません。");
        }

        // ロビーを閉じる
        Close();
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
            enemyLevelLabel.text = $"Lv. {level}";
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

    private bool IsReadyToStart()
    {
        return _hasColorSelection && _hasBrushSelection;
    }

    /// <summary>
    /// 作業データを初期化し、最低限のデフォルト値を設定する。
    /// </summary>
    private void ResetWorkingData()
    {
        _workingData.playerColorIndex = colorAPlayerIndex;
        _workingData.cpuColorIndex = colorBPlayerIndex;
        _workingData.brushKey = string.Empty;
        _workingData.enemyLevel = minEnemyLevel;

        // バトル時間はドロップダウンから設定される想定だが、念のためデフォルトを設定
        if (battleTimeOptionsSeconds != null && battleTimeOptionsSeconds.Length > 0)
        {
            _workingData.battleDurationSeconds = battleTimeOptionsSeconds[0];
        }
        else
        {
            _workingData.battleDurationSeconds = 180f;
        }

        _hasColorSelection = false;
        _hasBrushSelection = false;
        UpdateStartButtonInteractable();
    }

    /// <summary>
    /// 作業データの内容を UI に反映する。
    /// Open() 時などに使用。
    /// </summary>
    private void SyncUIWithWorkingData()
    {
        UpdateEnemyLevel(_workingData.enemyLevel);

        // バトル時間ドロップダウンの選択を反映
        if (battleTimeDropdown != null && battleTimeOptionsSeconds != null && battleTimeOptionsSeconds.Length > 0)
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

            battleTimeDropdown.SetValueWithoutNotify(index);
        }
    }
}

