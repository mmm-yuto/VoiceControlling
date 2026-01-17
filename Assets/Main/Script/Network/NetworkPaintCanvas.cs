using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ネットワーク対応PaintCanvas
/// 塗りコマンドベースの同期方式でネットワーク経由で同期（オフラインと同じ軽量な方法）
/// </summary>
public class NetworkPaintCanvas : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Header("Network Settings")]
    [Tooltip("初回同期時の最大ピクセル数（これを超える場合は分割送信）")]
    [SerializeField] private int maxPixelsPerMessage = 5000; // 約160KB（5000ピクセル × 32バイト、安全マージンを考慮）
    
    [Header("Debug")]
    [Tooltip("デバッグログを出力するか")]
    [SerializeField] private bool enableDebugLog = false;
    
    void Awake()
    {
        // PaintCanvasの自動検索（未設定の場合）
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
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
    }
    
    
    /// <summary>
    /// クライアント側の塗りをサーバーに送信（ServerRpc）
    /// position: 正規化座標（0-1の範囲）
    /// radius: 正規化された半径（画面幅を基準とした0-1の範囲）
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SendClientPaintServerRpc(Vector2 normalizedPosition, int playerId, float intensity, Color color, float normalizedRadius, ServerRpcParams rpcParams = default)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintCanvas] SendClientPaintServerRpc受信 - normalizedPosition: {normalizedPosition}, playerId: {playerId}, color: {color}, normalizedRadius: {normalizedRadius}");
        }
        
        if (paintCanvas == null)
        {
            if (enableDebugLog)
            {
                Debug.LogError($"[NetworkPaintCanvas] paintCanvasがnullです");
            }
            return;
        }
        
        // 正規化座標を画面座標に変換（サーバー側の画面サイズを使用）
        Vector2 screenPosition = new Vector2(
            normalizedPosition.x * Screen.width,
            normalizedPosition.y * Screen.height
        );
        float screenRadius = normalizedRadius * Screen.width;
        
        // サーバー側のPaintCanvasに塗りを適用
        paintCanvas.PaintAtWithRadius(screenPosition, playerId, intensity, color, screenRadius);
        
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintCanvas] サーバー側に塗りを適用し、全クライアントに転送します - screenPosition: {screenPosition}, screenRadius: {screenRadius}");
        }
        
        // 全クライアントに同じ塗りコマンドを転送（正規化座標を送信）
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintCanvas] ApplyPaintCommandClientRpcを呼び出します - normalizedPosition: {normalizedPosition}, playerId: {playerId}, normalizedRadius: {normalizedRadius}, IsServer: {IsServer}, IsClient: {IsClient}, IsSpawned: {IsSpawned}, NetworkObjectId: {NetworkObjectId}");
        }
        
        ApplyPaintCommandClientRpc(normalizedPosition, playerId, intensity, color, normalizedRadius);
        
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintCanvas] ApplyPaintCommandClientRpc呼び出し完了");
        }
    }
    
    /// <summary>
    /// 塗りコマンドを受信して適用（ClientRpc）
    /// normalizedPosition: 正規化座標（0-1の範囲）
    /// normalizedRadius: 正規化された半径（画面幅を基準とした0-1の範囲）
    /// オフラインと同じように直接塗り処理を実行する軽量な方法
    /// </summary>
    [ClientRpc]
    private void ApplyPaintCommandClientRpc(Vector2 normalizedPosition, int playerId, float intensity, Color color, float normalizedRadius, ClientRpcParams rpcParams = default)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintCanvas] ApplyPaintCommandClientRpc受信 - normalizedPosition: {normalizedPosition}, playerId: {playerId}, normalizedRadius: {normalizedRadius}, IsServer: {IsServer}");
        }
        
        // サーバー側（ホスト）は既に塗り済みなのでスキップ
        if (IsServer)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[NetworkPaintCanvas] ホスト側のため、クライアント側のPaintCanvasへの描画をスキップ");
            }
            return;
        }
        
        if (paintCanvas == null)
        {
            if (enableDebugLog)
            {
                Debug.LogError($"[NetworkPaintCanvas] paintCanvasがnullです");
            }
            return;
        }
        
        // 正規化座標を画面座標に変換（各クライアントの画面サイズを使用）
        Vector2 screenPosition = new Vector2(
            normalizedPosition.x * Screen.width,
            normalizedPosition.y * Screen.height
        );
        float screenRadius = normalizedRadius * Screen.width;
        
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintCanvas] クライアント側のPaintCanvasに塗りを適用します - screenPosition: {screenPosition}, screenRadius: {screenRadius}, Screen.width: {Screen.width}, Screen.height: {Screen.height}");
        }
        
        // オフラインと同じように直接塗り処理を実行（軽量）
        paintCanvas.PaintAtWithRadius(screenPosition, playerId, intensity, color, screenRadius);
        
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintCanvas] PaintAtWithRadius呼び出し完了 - screenPosition: {screenPosition}, playerId: {playerId}, intensity: {intensity}, color: {color}, radius: {screenRadius}");
        }
    }
    
    /// <summary>
    /// サーバー時刻を取得
    /// </summary>
    private float GetServerTime()
    {
        if (IsServer)
        {
            return Time.time;
        }
        else
        {
            // クライアントはサーバー時刻を取得
            return (float)NetworkManager.Singleton.ServerTime.Time;
        }
    }
    
    /// <summary>
    /// ネットワーク接続時の初期化
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintCanvas] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsSpawned: {IsSpawned}, NetworkObjectId: {NetworkObjectId}");
        }
        
        if (paintCanvas == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[NetworkPaintCanvas] OnNetworkSpawn - paintCanvasがnullです");
            }
            return;
        }
        
        // PaintCanvasにタイムスタンプ取得コールバックを設定
        paintCanvas.SetTimestampCallback(GetServerTime);
        
        // サーバー側でクライアント接続時に初回同期を送信
        if (IsServer)
        {
            StartCoroutine(SendInitialSnapshotDelayed());
        }
    }
    
    /// <summary>
    /// 初回同期を遅延送信（接続確立を待つ）
    /// </summary>
    private IEnumerator SendInitialSnapshotDelayed()
    {
        yield return new WaitForSeconds(0.5f); // 接続確立を待つ
        
        SendInitialSnapshot();
    }
    
    /// <summary>
    /// 初回同期（フルスナップショット）を送信
    /// メッセージサイズ制限を回避するため、分割送信を使用
    /// </summary>
    private void SendInitialSnapshot()
    {
        if (paintCanvas == null) return;
        
        var settings = paintCanvas.GetSettings();
        if (settings == null) return;
        
        int width = settings.textureWidth;
        int height = settings.textureHeight;
        
        // 色データとタイムスタンプ、プレイヤーIDを直接送信（テクスチャ圧縮は使わない）
        Color[,] colorData = paintCanvas.GetColorData();
        float[,] timestamps = paintCanvas.GetPaintTimestamps();
        int[,] playerIds = paintCanvas.GetPaintData();
        
        // ピクセルごとのデータをリストに変換
        List<SnapshotPixelData> pixelDataList = new List<SnapshotPixelData>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (timestamps[x, y] > 0f) // 塗られたピクセルのみ
                {
                    pixelDataList.Add(new SnapshotPixelData
                    {
                        x = x,
                        y = y,
                        color = colorData[x, y],
                        playerId = playerIds[x, y],
                        timestamp = timestamps[x, y]
                    });
                }
            }
        }
        
        // 分割送信
        int totalPixels = pixelDataList.Count;
        int pixelsPerChunk = maxPixelsPerMessage;
        int chunkCount = Mathf.CeilToInt((float)totalPixels / pixelsPerChunk);
        
        // 各チャンクを送信
        for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            int startIndex = chunkIndex * pixelsPerChunk;
            int endIndex = Mathf.Min(startIndex + pixelsPerChunk, totalPixels);
            int chunkPixelCount = endIndex - startIndex;
            
            // チャンクデータを作成
            int[] xCoords = new int[chunkPixelCount];
            int[] yCoords = new int[chunkPixelCount];
            Color[] colors = new Color[chunkPixelCount];
            int[] playerIdArray = new int[chunkPixelCount];
            float[] timestampArray = new float[chunkPixelCount];
            
            for (int i = 0; i < chunkPixelCount; i++)
            {
                var pixel = pixelDataList[startIndex + i];
                xCoords[i] = pixel.x;
                yCoords[i] = pixel.y;
                colors[i] = pixel.color;
                playerIdArray[i] = pixel.playerId;
                timestampArray[i] = pixel.timestamp;
            }
            
            // チャンクを送信
            SendSnapshotChunkClientRpc(width, height, chunkIndex, chunkCount, xCoords, yCoords, colors, playerIdArray, timestampArray);
        }
    }
    
    /// <summary>
    /// スナップショットのピクセルデータ
    /// </summary>
    private struct SnapshotPixelData
    {
        public int x;
        public int y;
        public Color color;
        public int playerId;
        public float timestamp;
    }
    
    // 初回同期用のバッファ
    private Dictionary<int, SnapshotChunkBuffer> snapshotBuffers = new Dictionary<int, SnapshotChunkBuffer>();
    
    /// <summary>
    /// スナップショットチャンクのバッファ
    /// </summary>
    private class SnapshotChunkBuffer
    {
        public int width;
        public int height;
        public int totalChunks;
        public Dictionary<int, SnapshotChunkData> chunks = new Dictionary<int, SnapshotChunkData>();
        
        public bool IsComplete()
        {
            return chunks.Count == totalChunks;
        }
    }
    
    /// <summary>
    /// スナップショットチャンクデータ
    /// </summary>
    private struct SnapshotChunkData
    {
        public int[] xCoords;
        public int[] yCoords;
        public Color[] colors;
        public int[] playerIds;
        public float[] timestamps;
    }
    
    /// <summary>
    /// スナップショットチャンクを受信（ClientRpc）
    /// </summary>
    [ClientRpc]
    private void SendSnapshotChunkClientRpc(int width, int height, int chunkIndex, int totalChunks, int[] xCoords, int[] yCoords, Color[] colors, int[] playerIds, float[] timestamps, ClientRpcParams rpcParams = default)
    {
        if (IsServer) return; // サーバー側では実行しない
        
        if (paintCanvas == null)
        {
            return;
        }
        
        // バッファキー（クライアントIDを使用）
        int bufferKey = (int)NetworkManager.Singleton.LocalClientId;
        
        // バッファを取得または作成
        if (!snapshotBuffers.ContainsKey(bufferKey))
        {
            snapshotBuffers[bufferKey] = new SnapshotChunkBuffer
            {
                width = width,
                height = height,
                totalChunks = totalChunks
            };
        }
        
        var buffer = snapshotBuffers[bufferKey];
        
        // チャンクを保存
        buffer.chunks[chunkIndex] = new SnapshotChunkData
        {
            xCoords = xCoords,
            yCoords = yCoords,
            colors = colors,
            playerIds = playerIds,
            timestamps = timestamps
        };
        
        // 全てのチャンクが揃ったら適用
        if (buffer.IsComplete())
        {
            ApplySnapshot(buffer);
            
            // バッファをクリア
            snapshotBuffers.Remove(bufferKey);
        }
    }
    
    /// <summary>
    /// スナップショットを適用
    /// </summary>
    private void ApplySnapshot(SnapshotChunkBuffer buffer)
    {
        if (paintCanvas == null) return;
        
        int width = buffer.width;
        int height = buffer.height;
        
        // 全てのチャンクからデータを復元して適用
        foreach (var chunk in buffer.chunks.Values)
        {
            for (int i = 0; i < chunk.xCoords.Length; i++)
            {
                int x = chunk.xCoords[i];
                int y = chunk.yCoords[i];
                
                // ピクセルを適用
                paintCanvas.PaintAtWithTimestamp(x, y, chunk.playerIds[i], chunk.colors[i], chunk.timestamps[i]);
            }
        }
    }
    
    /// <summary>
    /// ネットワーク切断時の処理
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
}

