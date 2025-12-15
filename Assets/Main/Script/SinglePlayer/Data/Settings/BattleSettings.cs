using UnityEngine;

/// <summary>
/// 1 回のバトルに関する設定値をまとめた DTO。
/// オンライン同期しやすいよう、極力プリミティブ／構造体のみを持たせる。
/// </summary>
[System.Serializable]
public class BattleSettingsData
{
    [Header("Color")]
    [Tooltip("プレイヤーのインク色インデックス（ColorSelectionSettings などの配列に対するインデックス）")]
    public int playerColorIndex = 0;

    [Tooltip("CPU のインク色インデックス（通常は playerColorIndex とは別の色）")]
    public int cpuColorIndex = 1;

    [Tooltip("プレイヤーのインク色（利便性のためキャッシュ。オンライン同期時は index を優先し、色は再計算する想定）")]
    public Color playerColor = Color.blue;

    [Tooltip("CPU のインク色（利便性のためキャッシュ。オンライン同期時は index を優先し、色は再計算する想定）")]
    public Color cpuColor = Color.red;

    [Header("Brush")]
    [Tooltip("使用するブラシを識別するキー。ScriptableObject 名や任意の ID を想定")]
    public string brushKey = "Default";

    [Header("Difficulty / Time")]
    [Tooltip("相手のレベル（1 以上の整数想定）")]
    public int enemyLevel = 1;

    [Tooltip("バトル時間（秒）")]
    public float battleDurationSeconds = 180f;

    /// <summary>
    /// コピーを生成（オンライン同期時などで安全に渡すため）。
    /// </summary>
    public BattleSettingsData Clone()
    {
        return new BattleSettingsData
        {
            playerColorIndex = this.playerColorIndex,
            cpuColorIndex = this.cpuColorIndex,
            playerColor = this.playerColor,
            cpuColor = this.cpuColor,
            brushKey = this.brushKey,
            enemyLevel = this.enemyLevel,
            battleDurationSeconds = this.battleDurationSeconds
        };
    }

    /// <summary>
    /// 別インスタンスの内容をこのインスタンスへコピー。
    /// </summary>
    public void CopyFrom(BattleSettingsData other)
    {
        if (other == null) return;

        playerColorIndex = other.playerColorIndex;
        cpuColorIndex = other.cpuColorIndex;
        playerColor = other.playerColor;
        cpuColor = other.cpuColor;
        brushKey = other.brushKey;
        enemyLevel = other.enemyLevel;
        battleDurationSeconds = other.battleDurationSeconds;
    }
}

/// <summary>
/// 現在のバトル設定を 1 箇所で管理するシングルトン。
/// ローカル／オンラインの両方で同じデータ構造を使う前提。
/// </summary>
public class BattleSettings : MonoBehaviour
{
    /// <summary>ランタイム唯一のインスタンス。</summary>
    public static BattleSettings Instance { get; private set; }

    [Header("Current Settings")]
    [Tooltip("現在有効なバトル設定。UI から編集され、バトル開始時に確定される。")]
    [SerializeField]
    private BattleSettingsData current = new BattleSettingsData();

    [Header("Optional: Color Source")]
    [Tooltip("利用可能な色の一覧。存在しない場合は current.playerColor / cpuColor をそのまま利用する。")]
    [SerializeField]
    private ColorSelectionSettings colorSelectionSettings;

    [Header("Optional: Default Single Player Settings")]
    [Tooltip("シングルプレイ用の既存設定。必要に応じて gameDuration や difficultyLevel を上書きするために使用。")]
    [SerializeField]
    private SinglePlayerGameModeSettings singlePlayerDefaults;

    /// <summary>現在有効な設定の読み取り専用参照。</summary>
    public BattleSettingsData Current => current;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("BattleSettings: 複数インスタンスが存在します。既存のインスタンスを使用します。");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 起動時にインデックスから色キャッシュを更新しておく
        RefreshColorsFromIndices();
    }

    /// <summary>
    /// UI などから設定を反映する。内部では値をコピーして保持する。
    /// </summary>
    public void SetFromUI(BattleSettingsData uiData)
    {
        if (uiData == null)
        {
            Debug.LogWarning("BattleSettings.SetFromUI: uiData が null です。");
            return;
        }

        if (current == null)
        {
            current = new BattleSettingsData();
        }

        current.CopyFrom(uiData);
        RefreshColorsFromIndices();
    }

    /// <summary>
    /// 現在の設定から SinglePlayerGameModeSettings へ値を適用する。
    /// 既存の ColorDefense 実装と連携するためのブリッジ。
    /// </summary>
    public void ApplyToSinglePlayerSettings()
    {
        if (singlePlayerDefaults == null)
        {
            Debug.LogWarning("BattleSettings.ApplyToSinglePlayerSettings: singlePlayerDefaults が設定されていません。");
            return;
        }

        // ゲーム時間と難易度レベルを既存の設定に反映
        singlePlayerDefaults.gameDuration = Mathf.Max(10f, current.battleDurationSeconds);
        singlePlayerDefaults.difficultyLevel = Mathf.Max(1, current.enemyLevel);
    }

    /// <summary>
    /// ColorSelectionSettings の配列とインデックスから色キャッシュを更新。
    /// オンライン同期時など、「index だけ受け取って色は各クライアントで再解決する」前提。
    /// </summary>
    public void RefreshColorsFromIndices()
    {
        if (colorSelectionSettings == null || colorSelectionSettings.presetColors == null || colorSelectionSettings.presetColors.Length == 0)
        {
            // 設定が無い場合は、既存の色をそのまま使用
            return;
        }

        int length = colorSelectionSettings.presetColors.Length;

        int safePlayerIndex = Mathf.Clamp(current.playerColorIndex, 0, length - 1);
        int safeCpuIndex = Mathf.Clamp(current.cpuColorIndex, 0, length - 1);

        // プレイヤーと CPU の色が同じにならないよう、必要であれば CPU 側をずらす
        if (safeCpuIndex == safePlayerIndex && length >= 2)
        {
            safeCpuIndex = (safePlayerIndex + 1) % length;
        }

        current.playerColorIndex = safePlayerIndex;
        current.cpuColorIndex = safeCpuIndex;
        current.playerColor = colorSelectionSettings.presetColors[safePlayerIndex];
        current.cpuColor = colorSelectionSettings.presetColors[safeCpuIndex];
    }

    /// <summary>
    /// インデックスから色を取得（UI表示用など）
    /// </summary>
    public Color GetColorFromIndex(int index)
    {
        if (colorSelectionSettings == null || colorSelectionSettings.presetColors == null || colorSelectionSettings.presetColors.Length == 0)
        {
            return Color.white; // フォールバック
        }

        int length = colorSelectionSettings.presetColors.Length;
        int safeIndex = Mathf.Clamp(index, 0, length - 1);
        return colorSelectionSettings.presetColors[safeIndex];
    }
}

