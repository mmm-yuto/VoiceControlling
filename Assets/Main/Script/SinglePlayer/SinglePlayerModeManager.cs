using UnityEngine;
using System;

/// <summary>
/// シングルプレイゲームモードの管理
/// 複数のゲームモードを管理・切り替え
/// </summary>
public class SinglePlayerModeManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private SinglePlayerGameModeSettings settings;
    
    [Header("Mode Components")]
    [SerializeField] private ColorDefenseMode colorDefenseMode;
    [SerializeField] private CreativeModeManager creativeModeManager;
    // 他のモードは後から追加
    // [SerializeField] private MonsterHuntMode monsterHuntMode;
    // [SerializeField] private TracingMode tracingMode;
    // [SerializeField] private AIBattleMode aiBattleMode;
    
    [Header("Mode UI Roots")]
    [Tooltip("カラーディフェンス用UIのルート（インスペクターで直接指定）")]
    [SerializeField] private GameObject colorDefenseUIRoot;
    
    [Tooltip("クリエイティブモード用UIのルート（インスペクターで直接指定）")]
    [SerializeField] private GameObject creativeModeUIRoot;
    
    [Header("Shared Components")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    private ISinglePlayerGameMode currentMode;
    
    // イベント
    public static event Action<SinglePlayerGameModeType> OnModeChanged;
    public static event Action<int> OnScoreUpdated;
    public static event Action<float> OnProgressUpdated;
    
    // Start()での自動実行を停止
    // GameModeSelectionPanelから手動で呼び出される
    // void Start()
    // {
    //     InitializeMode();
    // }
    
    /// <summary>
    /// 指定されたモードで初期化（ボタン選択時に使用）
    /// </summary>
    public void InitializeMode(SinglePlayerGameModeType mode)
    {
        if (settings != null)
        {
            settings.selectedMode = mode;
        }
        InitializeMode(); // 既存のメソッドを呼び出し
    }
    
    /// <summary>
    /// ゲームモードを初期化（外部から呼び出し可能）
    /// </summary>
    public void InitializeMode()
    {
        if (settings == null)
        {
            Debug.LogError("SinglePlayerModeManager: SinglePlayerGameModeSettingsが設定されていません");
            return;
        }
        
        // 全てのモードとUIルートを無効化
        DisableAllModes();
        DisableAllUIRoots();
        
        // 選択されたモードを有効化
        switch (settings.selectedMode)
        {
            case SinglePlayerGameModeType.Creative:
                if (creativeModeManager != null)
                {
                    currentMode = creativeModeManager;
                    // UIルートを有効化（インスペクターで指定されたオブジェクト）
                    if (creativeModeUIRoot != null)
                    {
                        creativeModeUIRoot.SetActive(true);
                    }
                    // 子オブジェクトを有効化
                    creativeModeManager.gameObject.SetActive(true);
                }
                break;
            case SinglePlayerGameModeType.ColorDefense:
                if (colorDefenseMode != null)
                {
                    currentMode = colorDefenseMode;
                    // UIルートを有効化（インスペクターで指定されたオブジェクト）
                    if (colorDefenseUIRoot != null)
                    {
                        colorDefenseUIRoot.SetActive(true);
                    }
                    // 子オブジェクトを有効化
                    colorDefenseMode.gameObject.SetActive(true);
                }
                break;
            // 他のモードは後から追加
            // case SinglePlayerGameModeType.MonsterHunt:
            //     if (monsterHuntMode != null)
            //     {
            //         currentMode = monsterHuntMode;
            //         monsterHuntMode.gameObject.SetActive(true);
            //     }
            //     break;
        }
        
        if (currentMode != null)
        {
            currentMode.Initialize(settings);
            currentMode.StartGame();
            OnModeChanged?.Invoke(settings.selectedMode);
        }
        else
        {
            Debug.LogError($"SinglePlayerModeManager: {settings.selectedMode}モードのコンポーネントが設定されていません");
        }
    }
    
    void Update()
    {
        if (currentMode != null)
        {
            currentMode.UpdateGame(Time.deltaTime);
            
            // スコアと進捗を更新
            OnScoreUpdated?.Invoke(currentMode.GetScore());
            OnProgressUpdated?.Invoke(currentMode.GetProgress());
            
            // ゲームオーバー判定
            if (currentMode.IsGameOver())
            {
                currentMode.EndGame();
            }
        }
    }
    
    private void DisableAllModes()
    {
        if (colorDefenseMode != null) colorDefenseMode.gameObject.SetActive(false);
        if (creativeModeManager != null) creativeModeManager.gameObject.SetActive(false);
        // 他のモードは後から追加
        // if (monsterHuntMode != null) monsterHuntMode.gameObject.SetActive(false);
        // if (tracingMode != null) tracingMode.gameObject.SetActive(false);
        // if (aiBattleMode != null) aiBattleMode.gameObject.SetActive(false);
    }
    
    private void DisableAllUIRoots()
    {
        if (colorDefenseUIRoot != null) colorDefenseUIRoot.SetActive(false);
        if (creativeModeUIRoot != null) creativeModeUIRoot.SetActive(false);
    }
    
    /// <summary>
    /// ゲームモードを変更
    /// </summary>
    public void ChangeMode(SinglePlayerGameModeType newMode)
    {
        if (currentMode != null)
        {
            currentMode.EndGame();
        }
        
        if (settings != null)
        {
            settings.selectedMode = newMode;
        }
        
        InitializeMode();
    }
    
    /// <summary>
    /// 現在のゲームモードを取得
    /// </summary>
    public ISinglePlayerGameMode GetCurrentMode()
    {
        return currentMode;
    }
    
    void OnDestroy()
    {
        if (currentMode != null)
        {
            currentMode.EndGame();
        }
    }
}

