using UnityEngine;

/// <summary>
/// ゲームモード（オフライン/オンライン）を管理するシングルトン
/// シングルプレイ用のスクリプトを変更せずに、オンライン/オフラインの状態を管理
/// </summary>
public class GameModeManager : MonoBehaviour
{
    /// <summary>ランタイム唯一のインスタンス</summary>
    public static GameModeManager Instance { get; private set; }
    
    /// <summary>現在のモード（true = オンライン、false = オフライン）</summary>
    private bool isOnlineMode = false;
    
    /// <summary>現在のモードを取得</summary>
    public bool IsOnlineMode => isOnlineMode;
    
    /// <summary>現在のモードを取得（オフライン/オンライン）</summary>
    public bool GetIsOnlineMode() => isOnlineMode;
    
    /// <summary>
    /// オンラインモードを設定
    /// </summary>
    /// <param name="isOnline">true = オンライン、false = オフライン</param>
    public void SetOnlineMode(bool isOnline)
    {
        isOnlineMode = isOnline;
        Debug.Log($"GameModeManager: モードを設定 - {(isOnline ? "オンライン" : "オフライン")}");
    }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameModeManager: 複数インスタンスが存在します。既存のインスタンスを使用します。");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // デフォルトはオフラインモード
        isOnlineMode = false;
    }
    
    /// <summary>
    /// モードをリセット（タイトルに戻る時など）
    /// </summary>
    public void ResetMode()
    {
        isOnlineMode = false;
        Debug.Log("GameModeManager: モードをリセットしました");
    }
}

