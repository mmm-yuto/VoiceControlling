using UnityEngine;

/// <summary>
/// シングルプレイゲームモードの共通インターフェース
/// </summary>
public interface ISinglePlayerGameMode
{
    /// <summary>
    /// ゲームモードのタイプを取得
    /// </summary>
    SinglePlayerGameModeType GetModeType();
    
    /// <summary>
    /// ゲームモードを初期化
    /// </summary>
    /// <param name="modeSettings">ゲームモード設定</param>
    void Initialize(SinglePlayerGameModeSettings modeSettings);
    
    /// <summary>
    /// ゲームを開始
    /// </summary>
    void StartGame();
    
    /// <summary>
    /// ゲームを更新（毎フレーム呼ばれる）
    /// </summary>
    /// <param name="deltaTime">経過時間</param>
    void UpdateGame(float deltaTime);
    
    /// <summary>
    /// ゲームを終了
    /// </summary>
    void EndGame();
    
    /// <summary>
    /// ゲームを一時停止
    /// </summary>
    void Pause();
    
    /// <summary>
    /// ゲームを再開
    /// </summary>
    void Resume();
    
    /// <summary>
    /// 現在のスコアを取得
    /// </summary>
    int GetScore();
    
    /// <summary>
    /// ゲームの進捗を取得（0.0～1.0）
    /// </summary>
    float GetProgress();
    
    /// <summary>
    /// ゲームオーバーかどうか
    /// </summary>
    bool IsGameOver();
}

