using UnityEngine;

/// <summary>
/// ゲームモード（オフライン/オンライン）を管理するシングルトン
/// シングルプレイ用のスクリプトを変更せずに、オンライン/オフラインの状態を管理
/// </summary>
public class GameModeManager : MonoBehaviour
{
    /// <summary>ランタイム唯一のインスタンス</summary>
    public static GameModeManager Instance { get; private set; }
    
    [Header("Debug (Inspector Only)")]
    [Tooltip("デバッグ用: インスペクタ上でオンラインモードを切り替える（実行時のみ反映）")]
    [SerializeField] private bool debugIsOnlineMode = false;
    
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
        #if UNITY_EDITOR
        debugIsOnlineMode = isOnline;
        #endif
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
    
    void Start()
    {
        // デバッグ用: インスペクタの設定を反映（実行時のみ）
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            isOnlineMode = debugIsOnlineMode;
            Debug.Log($"GameModeManager: [DEBUG] インスペクタの設定を反映 - {(isOnlineMode ? "オンライン" : "オフライン")}");
        }
        #endif
    }
    
    void OnValidate()
    {
        // エディタ上でインスペクタの値を変更した時に実行時の状態を更新
        #if UNITY_EDITOR
        if (Application.isPlaying && Instance == this)
        {
            isOnlineMode = debugIsOnlineMode;
            Debug.Log($"GameModeManager: [DEBUG] インスペクタからモードを変更 - {(isOnlineMode ? "オンライン" : "オフライン")}");
        }
        #endif
    }
    
    /// <summary>
    /// モードをリセット（タイトルに戻る時など）
    /// </summary>
    public void ResetMode()
    {
        isOnlineMode = false;
        #if UNITY_EDITOR
        debugIsOnlineMode = false;
        #endif
        Debug.Log("GameModeManager: モードをリセットしました");
    }
    
    /// <summary>
    /// デバッグ用: インスペクタからオンラインモードを有効にする（Context Menu）
    /// </summary>
    [ContextMenu("Debug: Enable Online Mode")]
    private void DebugEnableOnlineMode()
    {
        SetOnlineMode(true);
        #if UNITY_EDITOR
        debugIsOnlineMode = true;
        #endif
    }
    
    /// <summary>
    /// デバッグ用: インスペクタからオフラインモードに切り替える（Context Menu）
    /// </summary>
    [ContextMenu("Debug: Enable Offline Mode")]
    private void DebugEnableOfflineMode()
    {
        SetOnlineMode(false);
        #if UNITY_EDITOR
        debugIsOnlineMode = false;
        #endif
    }
}

