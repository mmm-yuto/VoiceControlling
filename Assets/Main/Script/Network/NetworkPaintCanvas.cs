using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ネットワーク対応PaintCanvas
/// 塗りコマンドをネットワーク経由で同期
/// ホストとクライアント間で塗りつぶしを同期する
/// </summary>
public class NetworkPaintCanvas : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Header("Debug Settings")]
    [Tooltip("デバッグログを有効にする")]
    [SerializeField] private bool enableDebugLog = false;
    
    // プレイヤーIDの管理
    // ホスト: playerId = 1
    // クライアント: playerId = 2
    private int localPlayerId = 1; // ローカルプレイヤーのID
    
    // 送信頻度制限（パフォーマンス最適化）
    private float lastSendTime = 0f;
    private const float SEND_INTERVAL = 0.033f; // 約30fps
    
    // 重複送信防止のためのバッファ
    private Dictionary<ulong, PaintCommand> pendingCommands = new Dictionary<ulong, PaintCommand>();
    
    // 塗りコマンドのデータ構造
    private struct PaintCommand
    {
        public Vector2 position;
        public int playerId;
        public float intensity;
        public Color color;
        public float timestamp;
    }
    
    // デバッグログ用のparallelSink（複数の出力先に同時に出力）
    private System.Action<string> parallelSink;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // プレイヤーIDを決定
        if (IsServer && IsHost)
        {
            localPlayerId = 1; // ホストは1
        }
        else if (IsClient && !IsServer)
        {
            localPlayerId = 2; // クライアントは2
        }
        
        // parallelSinkの初期化（複数の出力先に同時に出力）
        parallelSink = (message) =>
        {
            // 標準のDebug.Log
            Debug.Log($"[NetworkPaintCanvas] {message}");
            
            // 将来的に追加のログ出力先を追加可能
            // 例: ファイル出力、ネットワークログサーバーなど
        };
        
        if (enableDebugLog)
        {
            LogDebug($"NetworkPaintCanvas: ネットワーク生成完了 - LocalPlayerId: {localPlayerId}, IsHost: {IsHost}, IsClient: {IsClient}");
        }
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (enableDebugLog)
        {
            LogDebug("NetworkPaintCanvas: ネットワーク破棄");
        }
        
        pendingCommands.Clear();
    }
    
    /// <summary>
    /// 塗りコマンドを送信（ローカルプレイヤーの塗りをネットワークに送信）
    /// </summary>
    /// <param name="position">画面座標</param>
    /// <param name="intensity">塗り強度</param>
    /// <param name="color">色</param>
    public void SendPaintCommand(Vector2 position, float intensity, Color color)
    {
        // オンラインモードかどうかを確認
        if (GameModeManager.Instance == null || !GameModeManager.Instance.IsOnlineMode)
        {
            return; // オフラインモードの場合は送信しない
        }
        
        // ネットワークが接続されていない場合は送信しない
        if (!IsSpawned)
        {
            return;
        }
        
        // 送信頻度制限
        float currentTime = Time.time;
        if (currentTime - lastSendTime < SEND_INTERVAL)
        {
            return;
        }
        lastSendTime = currentTime;
        
        // サーバーに塗りコマンドを送信
        SendPaintCommandServerRpc(position, localPlayerId, intensity, color);
        
        if (enableDebugLog)
        {
            LogDebug($"SendPaintCommand: Position=({position.x:F1}, {position.y:F1}), PlayerId={localPlayerId}, Intensity={intensity:F3}, Color={color}");
        }
    }
    
    /// <summary>
    /// サーバーに塗りコマンドを送信（ServerRpc）
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendPaintCommandServerRpc(Vector2 position, int playerId, float intensity, Color color)
    {
        // サーバーで塗りを実行し、全クライアントに同期
        // 送信元のクライアント以外に配信（送信元は既にローカルで塗っているため）
        ApplyPaintCommandClientRpc(position, playerId, intensity, color);
        
        if (enableDebugLog)
        {
            LogDebug($"SendPaintCommandServerRpc: Position=({position.x:F1}, {position.y:F1}), PlayerId={playerId}, Intensity={intensity:F3}, Color={color}");
        }
    }
    
    /// <summary>
    /// クライアント側で塗りを適用（ClientRpc）
    /// </summary>
    [ClientRpc]
    private void ApplyPaintCommandClientRpc(Vector2 position, int playerId, float intensity, Color color)
    {
        // ローカルプレイヤーの塗りは既にローカルで処理済みなので、スキップ
        if (playerId == localPlayerId)
        {
            if (enableDebugLog)
            {
                LogDebug($"ApplyPaintCommandClientRpc: ローカルプレイヤーの塗りをスキップ - PlayerId={playerId}");
            }
            return;
        }
        
        // リモートプレイヤーの塗りを適用
        if (paintCanvas != null)
        {
            paintCanvas.PaintAt(position, playerId, intensity, color);
            
            if (enableDebugLog)
            {
                LogDebug($"ApplyPaintCommandClientRpc: リモートプレイヤーの塗りを適用 - Position=({position.x:F1}, {position.y:F1}), PlayerId={playerId}, Intensity={intensity:F3}, Color={color}");
            }
        }
        else
        {
            LogDebug($"ApplyPaintCommandClientRpc: PaintCanvasが設定されていません");
        }
    }
    
    /// <summary>
    /// デバッグログを出力（parallelSinkを使用）
    /// </summary>
    private void LogDebug(string message)
    {
        if (parallelSink != null)
        {
            parallelSink(message);
        }
        else
        {
            Debug.Log($"[NetworkPaintCanvas] {message}");
        }
    }
    
    /// <summary>
    /// ローカルプレイヤーIDを取得
    /// </summary>
    public int GetLocalPlayerId()
    {
        return localPlayerId;
    }
    
    /// <summary>
    /// リモートプレイヤーIDを取得（相手のプレイヤーID）
    /// </summary>
    public int GetRemotePlayerId()
    {
        return localPlayerId == 1 ? 2 : 1;
    }
}
