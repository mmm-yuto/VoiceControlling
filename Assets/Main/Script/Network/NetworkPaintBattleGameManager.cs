using Unity.Netcode;
using UnityEngine;
using System.Reflection;

/// <summary>
/// ネットワーク対応PaintBattleGameManager
/// ローカルプレイヤーの塗りをネットワークに送信
/// </summary>
public class NetworkPaintBattleGameManager : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("ローカルのPaintBattleGameManager（Inspectorで接続）")]
    [SerializeField] private PaintBattleGameManager localPaintManager;
    
    [Tooltip("ネットワーク対応PaintCanvas（Inspectorで接続）")]
    [SerializeField] private NetworkPaintCanvas networkPaintCanvas;
    
    [Header("Settings")]
    [Tooltip("オンラインモード時のみ動作するか")]
    [SerializeField] private bool onlyWorkInOnlineMode = true;
    
    // イベント購読フラグ
    private bool isSubscribed = false;
    
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
        
        if (networkPaintCanvas == null)
        {
            networkPaintCanvas = FindObjectOfType<NetworkPaintCanvas>();
            if (networkPaintCanvas == null)
            {
                Debug.LogWarning("NetworkPaintBattleGameManager: NetworkPaintCanvasが見つかりません。Inspectorで設定してください。");
            }
        }
    }
    
    void OnEnable()
    {
        // オンラインモードチェック
        if (onlyWorkInOnlineMode && !IsOnlineMode())
        {
            return;
        }
        
        // PaintCanvasのOnPaintCompletedイベントを購読
        SubscribeToPaintEvents();
    }
    
    void OnDisable()
    {
        // イベント購読を解除
        UnsubscribeFromPaintEvents();
    }
    
    /// <summary>
    /// オンラインモードかどうかを確認
    /// </summary>
    private bool IsOnlineMode()
    {
        // GameModeManagerが存在する場合は、その値を使用
        if (GameModeManager.Instance != null)
        {
            return GameModeManager.Instance.IsOnlineMode;
        }
        
        // GameModeManagerが存在しない場合でも、NetworkManagerが動作していればオンラインモードと判断
        if (NetworkManager.Singleton != null)
        {
            // サーバーまたはクライアントとして動作している場合、オンラインモードと判断
            return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient;
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
    /// PaintCanvasのイベントを購読
    /// </summary>
    private void SubscribeToPaintEvents()
    {
        if (isSubscribed) return;
        
        // PaintCanvasを取得
        PaintCanvas paintCanvas = null;
        if (localPaintManager != null && localPaintManager.paintCanvas != null)
        {
            paintCanvas = localPaintManager.paintCanvas;
        }
        else
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
        }
        
        if (paintCanvas != null)
        {
            paintCanvas.OnPaintCompleted += OnLocalPaintCompleted;
            isSubscribed = true;
        }
        else
        {
            Debug.LogWarning("NetworkPaintBattleGameManager: PaintCanvasが見つかりません");
        }
    }
    
    /// <summary>
    /// PaintCanvasのイベント購読を解除
    /// </summary>
    private void UnsubscribeFromPaintEvents()
    {
        if (!isSubscribed) return;
        
        PaintCanvas paintCanvas = null;
        if (localPaintManager != null && localPaintManager.paintCanvas != null)
        {
            paintCanvas = localPaintManager.paintCanvas;
        }
        else
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
        }
        
        if (paintCanvas != null)
        {
            paintCanvas.OnPaintCompleted -= OnLocalPaintCompleted;
            isSubscribed = false;
        }
    }
    
    /// <summary>
    /// ローカルプレイヤーの塗りイベントを処理
    /// </summary>
    private void OnLocalPaintCompleted(Vector2 position, int playerId, float intensity)
    {
        // 全プレイヤーで実行（ホスト/クライアントの区別なし）
        
        // プレイヤーの塗りのみ送信（playerId > 0）
        // playerId == -1 は敵（CPU）の塗りなので送信しない
        if (playerId <= 0)
        {
            return; // 敵の塗りは送信しない
        }
        
        // オンラインモードチェック
        if (onlyWorkInOnlineMode)
        {
            bool isOnlineMode = IsOnlineMode();
            if (!isOnlineMode)
            {
                return;
            }
        }
        
        // ネットワーク接続チェック
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            return;
        }
        
        // NetworkPaintCanvasが設定されているか確認
        if (networkPaintCanvas == null)
        {
            Debug.LogWarning("NetworkPaintBattleGameManager: NetworkPaintCanvasが設定されていません");
            return;
        }
        
        // プレイヤー色を取得
        Color playerColor = GetPlayerColor();
        
        // ブラシの半径を取得
        float brushRadius = GetBrushRadius();
        
        // Custom Messageで全プレイヤーに塗りデータを送信
        networkPaintCanvas.SendPaintData(position, playerId, intensity, playerColor, brushRadius);
    }
    
    /// <summary>
    /// ブラシの半径を取得
    /// </summary>
    private float GetBrushRadius()
    {
        if (localPaintManager != null)
        {
            // リフレクションでブラシを取得
            var brushField = typeof(PaintBattleGameManager).GetField("brush", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var brush = brushField?.GetValue(localPaintManager) as BrushStrategyBase;
            
            if (brush != null)
            {
                return brush.GetRadius();
            }
        }
        
        return 10f; // デフォルト半径
    }
    
    /// <summary>
    /// ネットワーク接続時の初期化
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // ローカルのPaintBattleGameManagerのplayerIdを設定
        // LocalClientIdベースでプレイヤーIDを割り当て（ホスト/クライアントの区別なし）
        if (localPaintManager != null && NetworkManager.Singleton != null)
        {
            // LocalClientIdを使用してプレイヤーIDを生成
            // ホストはLocalClientId = 0なので、プレイヤーID = 1
            // クライアントはLocalClientId + 1でプレイヤーIDを生成
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            localPaintManager.playerId = (int)localClientId + 1;
        }
        
        // ネットワーク接続時にイベントを購読（全プレイヤー）
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            SubscribeToPaintEvents();
        }
    }
    
    /// <summary>
    /// ネットワーク切断時の処理
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        // イベント購読を解除
        UnsubscribeFromPaintEvents();
    }
}

