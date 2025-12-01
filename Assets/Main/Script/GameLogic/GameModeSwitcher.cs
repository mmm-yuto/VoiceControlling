using UnityEngine;

/// <summary>
/// ゲーム全体のモード切り替え（ColorDefense / Creative）を担当するクラス
/// 
/// SinglePlayerGameModeSettings の selectedMode を見て、
/// - ColorDefense の場合: シングルプレイ（ColorDefense）関連を有効化
/// - Creative の場合: クリエイティブモード関連を有効化
/// します。
/// </summary>
public class GameModeSwitcher : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("シングルプレイゲームモードの設定（ScriptableObject）")]
    [SerializeField] private SinglePlayerGameModeSettings singlePlayerSettings;

    [Header("Mode Managers")]
    [Tooltip("シングルプレイ用モードマネージャー（ColorDefense など）")]
    [SerializeField] private SinglePlayerModeManager singlePlayerModeManager;

    [Tooltip("クリエイティブモードマネージャー")]
    [SerializeField] private CreativeModeManager creativeModeManager;

    [Header("Mode UI Roots")]
    [Tooltip("カラーディフェンス用UIのルート（Canvas配下のルートオブジェクトなど）")]
    [SerializeField] private GameObject colorDefenseUIRoot;

    [Tooltip("クリエイティブモード用UIのルート（Canvas配下のルートオブジェクトなど）")]
    [SerializeField] private GameObject creativeModeUIRoot;

    private void Awake()
    {
        ApplyModeFromSettings();
    }

    /// <summary>
    /// ScriptableObject の設定に基づいてモードを切り替える
    /// </summary>
    public void ApplyModeFromSettings()
    {
        if (singlePlayerSettings == null)
        {
            Debug.LogError("GameModeSwitcher: SinglePlayerGameModeSettings が設定されていません");
            return;
        }

        // ScriptableObject の selectedMode に応じて有効化するモードを決定
        var mode = singlePlayerSettings.selectedMode;

        bool useColorDefense = (mode == SinglePlayerGameModeType.ColorDefense);
        bool useCreative = (mode == SinglePlayerGameModeType.Creative);

        // シングルプレイ（ColorDefense）側の有効/無効
        if (singlePlayerModeManager != null)
        {
            singlePlayerModeManager.gameObject.SetActive(useColorDefense);
        }

        if (colorDefenseUIRoot != null)
        {
            colorDefenseUIRoot.SetActive(useColorDefense);
        }

        // クリエイティブモード側の有効/無効
        if (creativeModeManager != null)
        {
            creativeModeManager.gameObject.SetActive(useCreative);
        }

        if (creativeModeUIRoot != null)
        {
            creativeModeUIRoot.SetActive(useCreative);
        }

        Debug.Log($"GameModeSwitcher: モード切り替え完了 - selectedMode={mode}, useColorDefense={useColorDefense}, useCreative={useCreative}");
    }
}


