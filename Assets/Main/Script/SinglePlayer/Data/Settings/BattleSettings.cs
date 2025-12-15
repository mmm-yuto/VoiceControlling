using UnityEngine;

/// <summary>
/// 1 回のバトルに関する設定値をまとめた DTO。
/// オンライン同期しやすいよう、極力プリミティブ／構造体のみを持たせる。
/// </summary>
[System.Serializable]
public class BattleSettingsData
{
    [Header("Color")]
    [Tooltip("プレイヤーのインク色インデックス（MainColorSettings などの配列に対するインデックス）")]
    public int playerColorIndex = 0;

    [Tooltip("CPU のインク色インデックス（通常は playerColorIndex とは別の色）")]
    public int cpuColorIndex = 1;

    [Tooltip("プレイヤーのインク色（利便性のためキャッシュ。MainColorSettings/BattleSettingsから自動設定されます）")]
    [HideInInspector]
    public Color playerColor = Color.blue;

    [Tooltip("CPU のインク色（利便性のためキャッシュ。MainColorSettings/BattleSettingsから自動設定されます）")]
    [HideInInspector]
    public Color cpuColor = Color.red;

    [Header("Brush")]
    [Tooltip("使用するブラシを識別するキー。ScriptableObject 名や任意の ID を想定")]
    public string brushKey = "Default";
    
    [Tooltip("使用するブラシの参照（brushKeyの代替として使用）")]
    [HideInInspector]
    public BrushStrategyBase brush;

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
            brush = this.brush,
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
        brush = other.brush;
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
    [Tooltip("メインカラー2色の設定。ColorSelectionSettingsより優先されます。")]
    [SerializeField]
    private MainColorSettings mainColorSettings;

    [Tooltip("利用可能な色の一覧（フォールバック用）。MainColorSettingsが設定されている場合は使用されません。")]
    [SerializeField]
    private ColorSelectionSettings colorSelectionSettings;

    [Header("Optional: Default Single Player Settings")]
    [Tooltip("シングルプレイ用の既存設定。必要に応じて gameDuration や difficultyLevel を上書きするために使用。")]
    [SerializeField]
    private SinglePlayerGameModeSettings singlePlayerDefaults;

    /// <summary>現在有効な設定の読み取り専用参照。</summary>
    public BattleSettingsData Current => current;

    /// <summary>ゲーム開始フラグ（ColorDefenseモードでゲームが開始されたかどうか）</summary>
    private bool isGameStarted = false;
    public bool IsGameStarted => isGameStarted;

    /// <summary>
    /// ゲーム開始フラグを設定
    /// </summary>
    /// <param name="value">ゲーム開始状態</param>
    public void SetGameStarted(bool value)
    {
        isGameStarted = value;
    }

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

        // 起動時は色を更新しない - Inspectorで設定された色または後でSetFromUI()で設定される色を保持
        // RefreshColorsFromIndices(); // 削除 - ゲーム開始前に設定した色を保持するため
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
        // RefreshColorsFromIndices()は呼ばない - 色は既に設定されている想定
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
    /// MainColorSettings または ColorSelectionSettings の配列とインデックスから色キャッシュを更新。
    /// オンライン同期時など、「index だけ受け取って色は各クライアントで再解決する」前提。
    /// </summary>
    public void RefreshColorsFromIndices()
    {
        // MainColorSettingsが設定されている場合は優先
        if (mainColorSettings != null)
        {
            // プレイヤー色の設定
            if (current.playerColorIndex == 0)
            {
                current.playerColor = mainColorSettings.mainColor1;
            }
            else if (current.playerColorIndex == 1)
            {
                current.playerColor = mainColorSettings.mainColor2;
            }
            else
            {
                // 0,1以外の場合はフォールバック
                if (colorSelectionSettings != null && colorSelectionSettings.presetColors != null && colorSelectionSettings.presetColors.Length > 0)
                {
                    int safeIndex = Mathf.Clamp(current.playerColorIndex, 0, colorSelectionSettings.presetColors.Length - 1);
                    current.playerColor = colorSelectionSettings.presetColors[safeIndex];
                }
            }
            
            // CPU色の設定
            if (current.cpuColorIndex == 0)
            {
                current.cpuColor = mainColorSettings.mainColor1;
            }
            else if (current.cpuColorIndex == 1)
            {
                current.cpuColor = mainColorSettings.mainColor2;
            }
            else
            {
                // 0,1以外の場合はフォールバック
                if (colorSelectionSettings != null && colorSelectionSettings.presetColors != null && colorSelectionSettings.presetColors.Length > 0)
                {
                    int safeIndex = Mathf.Clamp(current.cpuColorIndex, 0, colorSelectionSettings.presetColors.Length - 1);
                    current.cpuColor = colorSelectionSettings.presetColors[safeIndex];
                }
            }
            
            // プレイヤーとCPUの色が同じにならないよう、必要であればCPU側をずらす
            if (current.playerColorIndex == current.cpuColorIndex && current.playerColorIndex >= 0 && current.playerColorIndex <= 1)
            {
                current.cpuColorIndex = (current.playerColorIndex == 0) ? 1 : 0;
                current.cpuColor = (current.cpuColorIndex == 0) ? mainColorSettings.mainColor1 : mainColorSettings.mainColor2;
            }
            
            return;
        }
        
        // フォールバック: 既存のColorSelectionSettings処理
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
        // MainColorSettingsが設定されている場合は優先
        if (mainColorSettings != null)
        {
            if (index == 0)
            {
                return mainColorSettings.mainColor1;
            }
            else if (index == 1)
            {
                return mainColorSettings.mainColor2;
            }
        }
        
        // フォールバック: ColorSelectionSettingsから取得
        if (colorSelectionSettings != null && colorSelectionSettings.presetColors != null && colorSelectionSettings.presetColors.Length > 0)
        {
            int length = colorSelectionSettings.presetColors.Length;
            int safeIndex = Mathf.Clamp(index, 0, length - 1);
            return colorSelectionSettings.presetColors[safeIndex];
        }
        
        return Color.white; // 最終的なフォールバック
    }

    /// <summary>
    /// MainColorSettingsからメインカラー1を取得
    /// </summary>
    public Color GetMainColor1()
    {
        if (mainColorSettings != null)
        {
            return mainColorSettings.mainColor1;
        }
        return Color.blue; // フォールバック
    }

    /// <summary>
    /// MainColorSettingsからメインカラー2を取得
    /// </summary>
    public Color GetMainColor2()
    {
        if (mainColorSettings != null)
        {
            return mainColorSettings.mainColor2;
        }
        return Color.red; // フォールバック
    }
}

