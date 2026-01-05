using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ネットワーク対応PaintCanvas
/// 塗りコマンドをネットワーク経由で同期
/// </summary>
public class NetworkPaintCanvas : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Header("Network Settings")]
    [Tooltip("塗りコマンドの送信頻度制限（フレーム単位、0で制限なし）")]
    [SerializeField] private int sendRateLimit = 2; // 2フレームに1回送信（30fps想定）
    
    [Tooltip("送信頻度制限を有効にするか")]
    [SerializeField] private bool enableSendRateLimit = true;
    
    // 送信頻度制限用のカウンター
    private int frameCount = 0;
    
    // バッファリング用（将来の拡張用）
    private System.Collections.Generic.Queue<PaintCommand> commandBuffer = new System.Collections.Generic.Queue<PaintCommand>();
    
    /// <summary>
    /// 塗りコマンドのデータ構造
    /// </summary>
    private struct PaintCommand
    {
        public Vector2 position;
        public int playerId;
        public float intensity;
        public Color color;
        
        public PaintCommand(Vector2 position, int playerId, float intensity, Color color)
        {
            this.position = position;
            this.playerId = playerId;
            this.intensity = intensity;
            this.color = color;
        }
    }
    
    void Awake()
    {
        // PaintCanvasの自動検索（未設定の場合）
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
            if (paintCanvas == null)
            {
                Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが見つかりません。Inspectorで設定してください。");
            }
        }
    }
    
    /// <summary>
    /// 塗りコマンドをネットワークに送信
    /// </summary>
    /// <param name="position">塗り位置（画面座標）</param>
    /// <param name="playerId">プレイヤーID</param>
    /// <param name="intensity">塗り強度</param>
    /// <param name="color">塗り色</param>
    public void SendPaintCommand(Vector2 position, int playerId, float intensity, Color color)
    {
        // 送信頻度制限チェック
        if (enableSendRateLimit && sendRateLimit > 0)
        {
            frameCount++;
            if (frameCount % sendRateLimit != 0)
            {
                // 送信をスキップ（バッファリングは将来の拡張用）
                return;
            }
        }
        
        // サーバーに塗りコマンドを送信
        if (IsClient)
        {
            SendPaintCommandServerRpc(position, playerId, intensity, color);
        }
    }
    
    /// <summary>
    /// サーバーに塗りコマンドを送信（ServerRpc）
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendPaintCommandServerRpc(Vector2 position, int playerId, float intensity, Color color, ServerRpcParams rpcParams = default)
    {
        // サーバーで塗りを実行し、全クライアントに同期
        // 送信元のクライアントIDを取得して、そのクライアントには送信しない（既にローカルで塗られているため）
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        
        // 送信元のクライアントIDを除外したリストを作成
        var targetClients = new System.Collections.Generic.List<ulong>();
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId != senderClientId)
            {
                targetClients.Add(clientId);
            }
        }
        
        // 送信先が存在する場合のみ送信
        if (targetClients.Count > 0)
        {
            ApplyPaintCommandClientRpc(position, playerId, intensity, color, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = targetClients
                }
            });
        }
    }
    
    /// <summary>
    /// 塗りコマンドを受信して適用（ClientRpc）
    /// </summary>
    [ClientRpc]
    private void ApplyPaintCommandClientRpc(Vector2 position, int playerId, float intensity, Color color, ClientRpcParams rpcParams = default)
    {
        // ローカルで塗りを実行
        if (paintCanvas != null)
        {
            paintCanvas.PaintAt(position, playerId, intensity, color);
            
            if (Application.isEditor)
            {
                Debug.Log($"NetworkPaintCanvas: リモート塗りを受信 - Position: {position}, PlayerId: {playerId}, Intensity: {intensity}");
            }
        }
        else
        {
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
        }
    }
    
    /// <summary>
    /// 初期状態の同期（ゲーム開始時）
    /// 必要に応じて実装
    /// </summary>
    public void SyncInitialState()
    {
        // キャンバスの初期状態を全クライアントに送信
        // 実装は必要に応じて追加
        Debug.Log("NetworkPaintCanvas: SyncInitialState - 実装は将来の拡張用");
    }
    
    /// <summary>
    /// ネットワーク接続時の初期化
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (paintCanvas == null)
        {
            Debug.LogError("NetworkPaintCanvas: PaintCanvasが設定されていません");
        }
        
        Debug.Log($"NetworkPaintCanvas: ネットワーク接続 - IsServer: {IsServer}, IsClient: {IsClient}, IsOwner: {IsOwner}");
    }
    
    /// <summary>
    /// ネットワーク切断時の処理
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Debug.Log("NetworkPaintCanvas: ネットワーク切断");
    }
}

