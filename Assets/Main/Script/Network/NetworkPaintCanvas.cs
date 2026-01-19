using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ネットワーク対応PaintCanvas
/// 差分同期方式でテクスチャデータをネットワーク経由で同期
/// </summary>
public class NetworkPaintCanvas : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Header("Network Settings")]
    [Tooltip("差分送信の間隔（秒）")]
    [SerializeField] private float sendInterval = 0.2f; // 0.2秒ごと
    
    [Tooltip("分割送信のチャンクサイズ（バイト）")]
    [SerializeField] private int chunkSize = 500000; // 500KB（Unity Netcodeの制限は約1MB）
    
    [Tooltip("差分送信の最大ピクセル数（これを超える場合は分割送信）")]
    [SerializeField] private int maxPixelsPerMessage = 30000; // 約480KB（30000ピクセル × 16バイト）
    
    // 差分検出マネージャー
    private PaintDiffManager diffManager;
    
    // 送信タイマー
    private float lastSendTime = 0f;
    
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
        
        
        // 差分検出マネージャーを初期化
        diffManager = new PaintDiffManager();
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
    
    void Update()
    {
        // 全プレイヤーで定期的に差分を送信
        if (paintCanvas != null && diffManager != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            if (Time.time - lastSendTime >= sendInterval)
            {
                SendPaintDiff();
                lastSendTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// 差分を検出して送信
    /// </summary>
    private void SendPaintDiff()
    {
        if (paintCanvas == null || diffManager == null) return;
        
        // 差分を検出
        var changes = diffManager.DetectChanges(
            paintCanvas.GetColorData(),
            paintCanvas.GetPaintTimestamps(),
            paintCanvas.GetPaintData()
        );
        
        if (changes.Count > 0)
        {
            // 大きすぎる場合は分割送信
            if (changes.Count > maxPixelsPerMessage)
            {
                SendPaintDiffSplit(changes);
            }
            else
            {
                // データをパック
                PaintDiffMessage diffMessage = PackDiffMessage(changes);
                
                // Custom Messageで全プレイヤーに送信
                SendPaintDiffMessage(diffMessage);
            }
        }
    }
    
    /// <summary>
    /// 差分を分割して送信
    /// </summary>
    private void SendPaintDiffSplit(List<PaintDiffManager.PixelChange> changes)
    {
        int totalPixels = changes.Count;
        int pixelsPerChunk = maxPixelsPerMessage;
        int chunkCount = Mathf.CeilToInt((float)totalPixels / pixelsPerChunk);
        
        // 各チャンクを送信
        for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            int startIndex = chunkIndex * pixelsPerChunk;
            int endIndex = Mathf.Min(startIndex + pixelsPerChunk, totalPixels);
            int chunkPixelCount = endIndex - startIndex;
            
            // チャンクデータを作成
            List<PaintDiffManager.PixelChange> chunkChanges = new List<PaintDiffManager.PixelChange>();
            for (int i = startIndex; i < endIndex; i++)
            {
                chunkChanges.Add(changes[i]);
            }
            
            // データをパック
            PaintDiffMessage diffMessage = PackDiffMessage(chunkChanges);
            
            // Custom Messageで全プレイヤーに送信
            SendPaintDiffMessage(diffMessage);
        }
    }
    
    /// <summary>
    /// 差分データをパック（Custom Message用）
    /// </summary>
    private PaintDiffMessage PackDiffMessage(List<PaintDiffManager.PixelChange> changes)
    {
        int count = changes.Count;
        int[] xCoords = new int[count];
        int[] yCoords = new int[count];
        Color[] colors = new Color[count];
        int[] playerIds = new int[count];
        float[] timestamps = new float[count];
        
        for (int i = 0; i < count; i++)
        {
            xCoords[i] = changes[i].x;
            yCoords[i] = changes[i].y;
            colors[i] = changes[i].color;
            playerIds[i] = changes[i].playerId;
            timestamps[i] = changes[i].timestamp;
        }
        
        return new PaintDiffMessage
        {
            pixelCount = count,
            xCoords = xCoords,
            yCoords = yCoords,
            colors = colors,
            playerIds = playerIds,
            timestamps = timestamps
        };
    }
    
    /// <summary>
    /// 差分データメッセージを送信（Custom Message）
    /// クライアントはサーバーに送信し、サーバーが全クライアントに転送
    /// </summary>
    private void SendPaintDiffMessage(PaintDiffMessage message)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            return;
        }
        
        // INetworkSerializableの場合はWriteValueSafeを使用
        // 大きめのサイズを確保（最大ピクセル数 × 16バイト + オーバーヘッド）
        int estimatedSize = message.pixelCount * 64 + 256; // 余裕を持たせたサイズ
        FastBufferWriter writer = new FastBufferWriter(estimatedSize, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(message);
        
        // サーバーに送信（サーバーが全クライアントに転送）
        if (NetworkManager.Singleton.IsServer)
        {
            // サーバー（ホスト）の場合は直接全クライアントに送信
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
                nameof(PaintMessageType.PaintDiff),
                writer,
                NetworkDelivery.ReliableSequenced
            );
        }
        else
        {
            // クライアントの場合はサーバーに送信
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                "ClientToServer_" + nameof(PaintMessageType.PaintDiff),
                NetworkManager.ServerClientId,
                writer,
                NetworkDelivery.ReliableSequenced
            );
        }
        
        writer.Dispose();
    }
    
    /// <summary>
    /// 塗りデータを全プレイヤーに送信（Custom Message）
    /// </summary>
    public void SendPaintData(Vector2 position, int playerId, float intensity, Color color, float radius)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.LogWarning("NetworkPaintCanvas: ネットワークに接続されていません");
            return;
        }
        
        if (paintCanvas == null)
        {
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        // タイムスタンプを取得
        float timestamp = GetServerTime();
        
        // メッセージを作成
        PaintDataMessage message = new PaintDataMessage
        {
            position = position,
            playerId = playerId,
            intensity = intensity,
            color = color,
            radius = radius,
            timestamp = timestamp
        };
        
        // サーバーに送信（サーバーが全クライアントに転送）
        // INetworkSerializableの場合はWriteValueSafeを使用
        FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(message);
        
        // サーバーに送信（サーバーが全クライアントに転送）
        if (NetworkManager.Singleton.IsServer)
        {
            // サーバー（ホスト）の場合は直接全クライアントに送信
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
                nameof(PaintMessageType.PaintData),
                writer,
                NetworkDelivery.ReliableSequenced
            );
        }
        else
        {
            // クライアントの場合はサーバーに送信
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                "ClientToServer_" + nameof(PaintMessageType.PaintData),
                NetworkManager.ServerClientId,
                writer,
                NetworkDelivery.ReliableSequenced
            );
        }
        
        writer.Dispose();
        
        // 送信者側も即座に適用（受信ハンドラでも処理されるが、即座に反映）
        paintCanvas.PaintAtWithRadius(position, playerId, intensity, color, radius);
    }
    
    /// <summary>
    /// 塗りデータを受信（Custom Messageハンドラ）
    /// </summary>
    private void OnPaintDataReceived(ulong clientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out PaintDataMessage message);
        
        if (paintCanvas == null)
        {
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        // タイムスタンプベースで適用（重複チェックはPaintCanvas側で行う）
        // ただし、送信者側は既に適用済みなので、タイムスタンプチェックで重複を回避
        paintCanvas.PaintAtWithRadius(message.position, message.playerId, message.intensity, message.color, message.radius);
    }
    
    /// <summary>
    /// 差分データを受信（Custom Messageハンドラ）
    /// </summary>
    private void OnPaintDiffReceived(ulong clientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out PaintDiffMessage message);
        
        if (paintCanvas == null)
        {
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        // 各ピクセルを更新（タイムスタンプベースで重複チェック）
        for (int i = 0; i < message.pixelCount; i++)
        {
            int x = message.xCoords[i];
            int y = message.yCoords[i];
            
            // タイムスタンプを比較して適用
            paintCanvas.PaintAtWithTimestamp(
                x, y,
                message.playerIds[i],
                message.colors[i],
                message.timestamps[i]
            );
        }
    }
    
    /// <summary>
    /// サーバー時刻を取得（全プレイヤーで同期）
    /// </summary>
    private float GetServerTime()
    {
        // NetworkManager.ServerTimeを使用して全プレイヤーで同期
        if (NetworkManager.Singleton != null)
        {
            return (float)NetworkManager.Singleton.ServerTime.Time;
        }
        return Time.time; // フォールバック
    }
    
    void Start()
    {
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
            return;
        }
        
        // PaintCanvasにタイムスタンプ取得コールバックを設定
        paintCanvas.SetTimestampCallback(GetServerTime);
        
        // 全プレイヤーで差分検出マネージャーを初期化
        if (diffManager != null)
        {
            var settings = paintCanvas.GetSettings();
            if (settings != null)
            {
                diffManager.Initialize(settings.textureWidth, settings.textureHeight);
            }
            else
            {
                Debug.LogError("NetworkPaintCanvas: PaintSettingsが取得できません");
            }
        }
        
        // Custom Messageハンドラを登録
        if (NetworkManager.Singleton != null)
        {
            // クライアント側：サーバーから送られてくるメッセージを受信
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                nameof(PaintMessageType.PaintData),
                OnPaintDataReceived
            );
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                nameof(PaintMessageType.PaintDiff),
                OnPaintDiffReceived
            );
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                nameof(PaintMessageType.PaintSnapshot),
                OnPaintSnapshotReceived
            );
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                nameof(PaintMessageType.PaintSnapshotRequest),
                OnPaintSnapshotRequestReceived
            );
            
            // サーバー側：クライアントから送られてくるメッセージを受信して全クライアントに転送
            if (IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    "ClientToServer_" + nameof(PaintMessageType.PaintData),
                    OnClientPaintDataReceived
                );
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    "ClientToServer_" + nameof(PaintMessageType.PaintDiff),
                    OnClientPaintDiffReceived
                );
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    "ClientToServer_" + nameof(PaintMessageType.PaintSnapshot),
                    OnClientPaintSnapshotReceived
                );
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    "ClientToServer_" + nameof(PaintMessageType.PaintSnapshotRequest),
                    OnClientPaintSnapshotRequestReceived
                );
            }
        }
        
        // 接続時に初回同期をリクエスト
        StartCoroutine(RequestInitialSnapshotDelayed());
    }
    
    /// <summary>
    /// 初回同期を遅延リクエスト（接続確立を待つ）
    /// </summary>
    private IEnumerator RequestInitialSnapshotDelayed()
    {
        yield return new WaitForSeconds(0.5f); // 接続確立を待つ
        
        // 他のプレイヤーにスナップショットをリクエスト
        RequestSnapshotFromOtherPlayers();
    }
    
    /// <summary>
    /// 他のプレイヤーにスナップショットをリクエスト
    /// </summary>
    private void RequestSnapshotFromOtherPlayers()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            return;
        }
        
        // 既に塗りがある場合はスナップショットをリクエスト
        if (paintCanvas != null)
        {
            var timestamps = paintCanvas.GetPaintTimestamps();
            bool hasPaint = false;
            if (timestamps != null)
            {
                for (int x = 0; x < timestamps.GetLength(0) && !hasPaint; x++)
                {
                    for (int y = 0; y < timestamps.GetLength(1) && !hasPaint; y++)
                    {
                        if (timestamps[x, y] > 0f)
                        {
                            hasPaint = true;
                        }
                    }
                }
            }
            
            // 既に塗りがある場合は、自分がスナップショットを送信
            if (hasPaint)
            {
                SendInitialSnapshot();
            }
            else
            {
                // 他のプレイヤーにスナップショットをリクエスト
                PaintSnapshotRequestMessage request = new PaintSnapshotRequestMessage
                {
                    requesterClientId = NetworkManager.Singleton.LocalClientId
                };
                
                // INetworkSerializableの場合はWriteValueSafeを使用
                FastBufferWriter writer = new FastBufferWriter(64, Unity.Collections.Allocator.Temp);
                writer.WriteValueSafe(request);
                
                // サーバーに送信（サーバーが全クライアントに転送）
                if (NetworkManager.Singleton.IsServer)
                {
                    // サーバー（ホスト）の場合は直接全クライアントに送信
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
                        nameof(PaintMessageType.PaintSnapshotRequest),
                        writer,
                        NetworkDelivery.ReliableSequenced
                    );
                }
            else
            {
                // クライアントの場合はサーバーに送信
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                    "ClientToServer_" + nameof(PaintMessageType.PaintSnapshotRequest),
                    NetworkManager.ServerClientId,
                    writer,
                    NetworkDelivery.ReliableSequenced
                );
            }
                
                writer.Dispose();
            }
        }
    }
    
    /// <summary>
    /// 初回同期（フルスナップショット）を送信（Custom Message）
    /// メッセージサイズ制限を回避するため、分割送信を使用
    /// </summary>
    private void SendInitialSnapshot()
    {
        if (paintCanvas == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;
        
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
            
            // Custom Messageでチャンクを送信
            PaintSnapshotMessage snapshotMessage = new PaintSnapshotMessage
            {
                width = width,
                height = height,
                chunkIndex = chunkIndex,
                totalChunks = chunkCount,
                xCoords = xCoords,
                yCoords = yCoords,
                colors = colors,
                playerIds = playerIdArray,
                timestamps = timestampArray
            };
            
            SendSnapshotMessage(snapshotMessage);
        }
    }
    
    /// <summary>
    /// スナップショットメッセージを送信（Custom Message）
    /// </summary>
    private void SendSnapshotMessage(PaintSnapshotMessage message)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            return;
        }
        
        // INetworkSerializableの場合はWriteValueSafeを使用
        int estimatedSize = (message.xCoords?.Length ?? 0) * 64 + 256; // 余裕を持たせたサイズ
        FastBufferWriter writer = new FastBufferWriter(estimatedSize, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(message);
        
        // サーバーに送信（サーバーが全クライアントに転送）
        if (NetworkManager.Singleton.IsServer)
        {
            // サーバー（ホスト）の場合は直接全クライアントに送信
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
                nameof(PaintMessageType.PaintSnapshot),
                writer,
                NetworkDelivery.ReliableSequenced
            );
        }
        else
        {
            // クライアントの場合はサーバーに送信
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                "ClientToServer_" + nameof(PaintMessageType.PaintSnapshot),
                NetworkManager.ServerClientId,
                writer,
                NetworkDelivery.ReliableSequenced
            );
        }
        
        writer.Dispose();
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
    /// スナップショットを受信（Custom Messageハンドラ）
    /// </summary>
    private void OnPaintSnapshotReceived(ulong clientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out PaintSnapshotMessage message);
        
        if (paintCanvas == null)
        {
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        // バッファキー（送信者のクライアントIDを使用）
        int bufferKey = (int)clientId;
        
        // バッファを取得または作成
        if (!snapshotBuffers.ContainsKey(bufferKey))
        {
            snapshotBuffers[bufferKey] = new SnapshotChunkBuffer
            {
                width = message.width,
                height = message.height,
                totalChunks = message.totalChunks
            };
        }
        
        var buffer = snapshotBuffers[bufferKey];
        
        // チャンクを保存
        buffer.chunks[message.chunkIndex] = new SnapshotChunkData
        {
            xCoords = message.xCoords,
            yCoords = message.yCoords,
            colors = message.colors,
            playerIds = message.playerIds,
            timestamps = message.timestamps
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
    /// スナップショットリクエストを受信（Custom Messageハンドラ）
    /// </summary>
    private void OnPaintSnapshotRequestReceived(ulong clientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out PaintSnapshotRequestMessage request);
        
        // リクエストを送ったプレイヤーに自分のスナップショットを送信
        // ただし、自分へのリクエストは無視
        if (request.requesterClientId != NetworkManager.Singleton.LocalClientId)
        {
            // 既に塗りがある場合のみ送信
            if (paintCanvas != null)
            {
                var timestamps = paintCanvas.GetPaintTimestamps();
                bool hasPaint = false;
                if (timestamps != null)
                {
                    for (int x = 0; x < timestamps.GetLength(0) && !hasPaint; x++)
                    {
                        for (int y = 0; y < timestamps.GetLength(1) && !hasPaint; y++)
                        {
                            if (timestamps[x, y] > 0f)
                            {
                                hasPaint = true;
                            }
                        }
                    }
                }
                
                if (hasPaint)
                {
                    SendInitialSnapshotToClient(request.requesterClientId);
                }
            }
        }
    }
    
    /// <summary>
    /// クライアントからの塗りデータを受信して全クライアントに転送（サーバー側ハンドラ）
    /// </summary>
    private void OnClientPaintDataReceived(ulong clientId, FastBufferReader reader)
    {
        // クライアントから送られてきたメッセージを全クライアントに転送
        reader.ReadValueSafe(out PaintDataMessage message);
        
        // 全クライアントに転送（送信者も含む）
        FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(message);
        
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
            nameof(PaintMessageType.PaintData),
            writer,
            NetworkDelivery.ReliableSequenced
        );
        
        writer.Dispose();
    }
    
    /// <summary>
    /// クライアントからの差分データを受信して全クライアントに転送（サーバー側ハンドラ）
    /// </summary>
    private void OnClientPaintDiffReceived(ulong clientId, FastBufferReader reader)
    {
        // クライアントから送られてきたメッセージを全クライアントに転送
        reader.ReadValueSafe(out PaintDiffMessage message);
        
        // 全クライアントに転送（送信者も含む）
        int estimatedSize = message.pixelCount * 64 + 256;
        FastBufferWriter writer = new FastBufferWriter(estimatedSize, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(message);
        
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
            nameof(PaintMessageType.PaintDiff),
            writer,
            NetworkDelivery.ReliableSequenced
        );
        
        writer.Dispose();
    }
    
    /// <summary>
    /// クライアントからのスナップショットを受信して全クライアントに転送（サーバー側ハンドラ）
    /// </summary>
    private void OnClientPaintSnapshotReceived(ulong clientId, FastBufferReader reader)
    {
        // クライアントから送られてきたメッセージを全クライアントに転送
        reader.ReadValueSafe(out PaintSnapshotMessage message);
        
        // 全クライアントに転送（送信者も含む）
        int estimatedSize = (message.xCoords?.Length ?? 0) * 64 + 256;
        FastBufferWriter writer = new FastBufferWriter(estimatedSize, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(message);
        
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
            nameof(PaintMessageType.PaintSnapshot),
            writer,
            NetworkDelivery.ReliableSequenced
        );
        
        writer.Dispose();
    }
    
    /// <summary>
    /// クライアントからのスナップショットリクエストを受信して全クライアントに転送（サーバー側ハンドラ）
    /// </summary>
    private void OnClientPaintSnapshotRequestReceived(ulong clientId, FastBufferReader reader)
    {
        // クライアントから送られてきたメッセージを全クライアントに転送
        reader.ReadValueSafe(out PaintSnapshotRequestMessage request);
        
        // 全クライアントに転送（送信者も含む）
        FastBufferWriter writer = new FastBufferWriter(64, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(request);
        
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
            nameof(PaintMessageType.PaintSnapshotRequest),
            writer,
            NetworkDelivery.ReliableSequenced
        );
        
        writer.Dispose();
    }
    
    /// <summary>
    /// 特定のクライアントにスナップショットを送信
    /// </summary>
    private void SendInitialSnapshotToClient(ulong targetClientId)
    {
        if (paintCanvas == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;
        
        var settings = paintCanvas.GetSettings();
        if (settings == null) return;
        
        int width = settings.textureWidth;
        int height = settings.textureHeight;
        
        Color[,] colorData = paintCanvas.GetColorData();
        float[,] timestamps = paintCanvas.GetPaintTimestamps();
        int[,] playerIds = paintCanvas.GetPaintData();
        
        List<SnapshotPixelData> pixelDataList = new List<SnapshotPixelData>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (timestamps[x, y] > 0f)
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
        
        int totalPixels = pixelDataList.Count;
        int pixelsPerChunk = maxPixelsPerMessage;
        int chunkCount = Mathf.CeilToInt((float)totalPixels / pixelsPerChunk);
        
        for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            int startIndex = chunkIndex * pixelsPerChunk;
            int endIndex = Mathf.Min(startIndex + pixelsPerChunk, totalPixels);
            int chunkPixelCount = endIndex - startIndex;
            
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
            
            PaintSnapshotMessage snapshotMessage = new PaintSnapshotMessage
            {
                width = width,
                height = height,
                chunkIndex = chunkIndex,
                totalChunks = chunkCount,
                xCoords = xCoords,
                yCoords = yCoords,
                colors = colors,
                playerIds = playerIdArray,
                timestamps = timestampArray
            };
            
            // INetworkSerializableの場合はWriteValueSafeを使用
            int estimatedSize = chunkPixelCount * 64 + 256; // 余裕を持たせたサイズ
            FastBufferWriter writer = new FastBufferWriter(estimatedSize, Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(snapshotMessage);
            
            // サーバー経由で送信
            if (NetworkManager.Singleton.IsServer)
            {
                // サーバー（ホスト）の場合は直接特定のクライアントに送信
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                    nameof(PaintMessageType.PaintSnapshot),
                    targetClientId,
                    writer,
                    NetworkDelivery.ReliableSequenced
                );
            }
            else
            {
                // クライアントの場合はサーバー経由で送信（サーバーが転送）
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                    "ClientToServer_" + nameof(PaintMessageType.PaintSnapshot),
                    NetworkManager.ServerClientId,
                    writer,
                    NetworkDelivery.ReliableSequenced
                );
            }
            
            writer.Dispose();
        }
    }
    
    /// <summary>
    /// スナップショットを適用
    /// </summary>
    private void ApplySnapshot(SnapshotChunkBuffer buffer)
    {
        if (paintCanvas == null || diffManager == null) return;
        
        int width = buffer.width;
        int height = buffer.height;
        
        // 色データとタイムスタンプ配列を作成
        Color[,] colorData = new Color[width, height];
        float[,] timestamps = new float[width, height];
        
        // 全てのチャンクからデータを復元
        foreach (var chunk in buffer.chunks.Values)
        {
            for (int i = 0; i < chunk.xCoords.Length; i++)
            {
                int x = chunk.xCoords[i];
                int y = chunk.yCoords[i];
                
                colorData[x, y] = chunk.colors[i];
                timestamps[x, y] = chunk.timestamps[i];
                
                // ピクセルを適用
                paintCanvas.PaintAtWithTimestamp(x, y, chunk.playerIds[i], chunk.colors[i], chunk.timestamps[i]);
            }
        }
        
        // 差分検出マネージャーの状態をリセット
        diffManager.ResetAfterSnapshot(colorData, timestamps);
    }
    
    /// <summary>
    /// ネットワーク切断時の処理
    /// </summary>
    public override void OnNetworkDespawn()
    {
        // Custom Messageハンドラを解除
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
        {
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(nameof(PaintMessageType.PaintData));
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(nameof(PaintMessageType.PaintDiff));
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(nameof(PaintMessageType.PaintSnapshot));
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(nameof(PaintMessageType.PaintSnapshotRequest));
            
            if (IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("ClientToServer_" + nameof(PaintMessageType.PaintData));
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("ClientToServer_" + nameof(PaintMessageType.PaintDiff));
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("ClientToServer_" + nameof(PaintMessageType.PaintSnapshot));
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("ClientToServer_" + nameof(PaintMessageType.PaintSnapshotRequest));
            }
        }
        
        base.OnNetworkDespawn();
    }
}

