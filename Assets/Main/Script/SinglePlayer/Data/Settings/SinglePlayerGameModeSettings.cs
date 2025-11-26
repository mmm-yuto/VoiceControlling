using UnityEngine;

/// <summary>
/// シングルプレイゲームモードの設定
/// </summary>
[CreateAssetMenu(fileName = "SinglePlayerGameModeSettings", menuName = "Game/SinglePlayer/Game Mode Settings")]
public class SinglePlayerGameModeSettings : ScriptableObject
{
    [Header("Mode Selection")]
    [Tooltip("選択されたゲームモード")]
    public SinglePlayerGameModeType selectedMode = SinglePlayerGameModeType.ColorDefense;
    
    [Header("Common Settings")]
    [Tooltip("ゲーム時間（秒）")]
    [Range(30f, 300f)]
    public float gameDuration = 180f;
    
    [Tooltip("難易度レベル")]
    [Range(1, 10)]
    public int difficultyLevel = 1;
    
    [Header("Mode-Specific Settings")]
    [Tooltip("カラーディフェンスモードの設定")]
    public ColorDefenseSettings colorDefenseSettings;
    
    // 他のモードの設定は後から追加
    // public MonsterHuntSettings monsterHuntSettings;
    // public TracingSettings tracingSettings;
    // public AIBattleSettings aiBattleSettings;
}

