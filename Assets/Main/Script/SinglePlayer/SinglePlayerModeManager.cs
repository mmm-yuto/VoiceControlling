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
    // 他のモードは後から追加
    // [SerializeField] private MonsterHuntMode monsterHuntMode;
    // [SerializeField] private TracingMode tracingMode;
    // [SerializeField] private AIBattleMode aiBattleMode;
    
    [Header("Shared Components")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    private ISinglePlayerGameMode currentMode;
    
    // イベント
    public static event Action<SinglePlayerGameModeType> OnModeChanged;
    public static event Action<int> OnScoreUpdated;
    public static event Action<float> OnProgressUpdated;
    
    void Start()
    {
        InitializeMode();
    }
    
    private void InitializeMode()
    {
        if (settings == null)
        {
            Debug.LogError("SinglePlayerModeManager: SinglePlayerGameModeSettingsが設定されていません");
            return;
        }
        
        // 全てのモードを無効化
        DisableAllModes();
        
        // 選択されたモードを有効化
        switch (settings.selectedMode)
        {
            case SinglePlayerGameModeType.ColorDefense:
                if (colorDefenseMode != null)
                {
                    currentMode = colorDefenseMode;
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
        // 他のモードは後から追加
        // if (monsterHuntMode != null) monsterHuntMode.gameObject.SetActive(false);
        // if (tracingMode != null) tracingMode.gameObject.SetActive(false);
        // if (aiBattleMode != null) aiBattleMode.gameObject.SetActive(false);
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

