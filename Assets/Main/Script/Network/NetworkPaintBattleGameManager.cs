using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ネットワーク対応PaintBattleGameManager
/// ローカルプレイヤーの塗りをネットワークに送信
/// </summary>
public class NetworkPaintBattleGameManager : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("ローカルのPaintBattleGameManager（Inspectorで接続）")]
    [SerializeField] private PaintBattleGameManager localPaintManager;
    
    [Header("Settings")]
    [Tooltip("オンラインモード時のみ動作するか")]
    [SerializeField] private bool onlyWorkInOnlineMode = true;
    
    void Awake()
    {
        // 参照の自動検索（未設定の場合）
        if (localPaintManager == null)
        {
            localPaintManager = FindObjectOfType<PaintBattleGameManager>();
            if (localPaintManager == null)
            {
                Debug.LogWarning("NetworkPaintBattleGameManager: PaintBattleGameManagerが見つかりません。Inspectorで設定してください。");
            }
        }
    }
    
    /// <summary>
    /// オンラインモードかどうかを確認
    /// </summary>
    private bool IsOnlineMode()
    {
        if (GameModeManager.Instance != null)
        {
            return GameModeManager.Instance.IsOnlineMode;
        }
        return false;
    }
    
    /// <summary>
    /// プレイヤー色を取得
    /// </summary>
    private Color GetPlayerColor()
    {
        // BattleSettingsからプレイヤー色を取得
        if (BattleSettings.Instance != null && BattleSettings.Instance.Current != null)
        {
            // brushKeyが設定されている場合、設定UIで選択された色を使用
            string brushKey = BattleSettings.Instance.Current.brushKey;
            if (!string.IsNullOrEmpty(brushKey) && brushKey != "Default")
            {
                return BattleSettings.Instance.Current.playerColor;
            }
            
            // デフォルト色を使用
            return BattleSettings.Instance.GetMainColor1();
        }
        
        return Color.blue; // フォールバック値
    }
    
    /// <summary>
    /// ネットワーク接続時の初期化
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Debug.Log($"NetworkPaintBattleGameManager: ネットワーク接続 - IsServer: {IsServer}, IsClient: {IsClient}, IsOwner: {IsOwner}");
        
        // ローカルのPaintBattleGameManagerのplayerIdを設定
        if (localPaintManager != null && IsOwner)
        {
            // ホスト（サーバー）の場合は1、クライアントの場合は2以降
            if (IsServer)
            {
                localPaintManager.playerId = 1;
                Debug.Log($"NetworkPaintBattleGameManager: ホストとしてPlayerId = 1を設定しました");
            }
            else if (IsClient)
            {
                // クライアントIDをプレイヤーIDに変換
                // LocalClientIdは通常1から始まる（0はサーバー）
                // ただし、ホストもクライアントとして扱われるため、ホストの場合は既に1が設定されている
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                // クライアントID + 1 でプレイヤーIDを生成（ホストは0なので1、最初のクライアントは1なので2）
                localPaintManager.playerId = (int)localClientId + 1;
                Debug.Log($"NetworkPaintBattleGameManager: クライアントとしてPlayerId = {localPaintManager.playerId}を設定しました (LocalClientId: {localClientId})");
            }
        }
        
    }
    
    /// <summary>
    /// ネットワーク切断時の処理
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Debug.Log("NetworkPaintBattleGameManager: ネットワーク切断");
    }
}

