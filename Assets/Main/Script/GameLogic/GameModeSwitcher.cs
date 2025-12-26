using UnityEngine;

/// <summary>
/// UIの表示/非表示を管理するクラス
/// 
/// SinglePlayerGameModeSettings の selectedMode に基づいて、
/// 各モード用のUIルートの表示/非表示を切り替えます。
/// 
/// 注意: モードの初期化・管理はSinglePlayerModeManagerが担当します。
/// </summary>
public class GameModeSwitcher : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("シングルプレイゲームモードの設定（ScriptableObject）")]
    [SerializeField] private SinglePlayerGameModeSettings singlePlayerSettings;

    [Header("Mode Components")]
    [Tooltip("カラーディフェンスモードコンポーネント（親オブジェクトの有効化に使用）")]
    [SerializeField] private ColorDefenseMode colorDefenseMode;

    [Tooltip("クリエイティブモードマネージャー（親オブジェクトの有効化に使用）")]
    [SerializeField] private CreativeModeManager creativeModeManager;

    // Awake()での自動実行を停止
    // GameModeSelectionPanelから手動で呼び出される
    // private void Awake()
    // {
    //     ApplyModeFromSettings();
    // }

    /// <summary>
    /// 指定されたモードを直接適用（ボタン選択時に使用）
    /// </summary>
    public void ApplyMode(SinglePlayerGameModeType mode)
    {
        if (singlePlayerSettings != null)
        {
            singlePlayerSettings.selectedMode = mode;
        }
        ApplyModeFromSettings();
    }

    /// <summary>
    /// ScriptableObject の設定に基づいて親オブジェクトを有効化
    /// モードの初期化・UI管理はSinglePlayerModeManagerが担当
    /// </summary>
    public void ApplyModeFromSettings()
    {
        if (singlePlayerSettings == null)
        {
            Debug.LogError("GameModeSwitcher: SinglePlayerGameModeSettings が設定されていません");
            return;
        }

        // ScriptableObject の selectedMode に応じて親オブジェクトを有効化
        var mode = singlePlayerSettings.selectedMode;

        // Noneモードの場合は何も有効化しない
        if (mode == SinglePlayerGameModeType.None)
        {
            Debug.Log("GameModeSwitcher: モードが選択されていません（初期状態）");
            return;
        }

        bool useColorDefense = (mode == SinglePlayerGameModeType.ColorDefense);
        bool useCreative = (mode == SinglePlayerGameModeType.Creative);

        // 各モードで使うオブジェクトの親オブジェクトを有効化
        if (useColorDefense && colorDefenseMode != null)
        {
            // まず親オブジェクト（ColorDefence）を有効化
            // Unityでは親が無効だと子を有効化しても効果がないため、先に親を有効化する必要がある
            Transform parent = colorDefenseMode.transform.parent;
            if (parent != null)
            {
                parent.gameObject.SetActive(true);
            }
            
            // その後、ColorDefenseModeコンポーネントがアタッチされているGameObjectを有効化
            colorDefenseMode.gameObject.SetActive(true);
        }

        if (useCreative && creativeModeManager != null)
        {
            // CreativeModeManagerの親オブジェクトを有効化
            Transform parent = creativeModeManager.transform.parent;
            if (parent != null)
            {
                parent.gameObject.SetActive(true);
            }
        }

        Debug.Log($"GameModeSwitcher: 親オブジェクトを有効化しました - selectedMode={mode}");
    }
}


