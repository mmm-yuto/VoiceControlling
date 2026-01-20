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
    // メッセージサイズ制限定数
    private const int MAX_MESSAGE_SIZE = 1000; // 安全マージンを含む制限（バイト） - ReliableSequencedの制限は1264バイト
    private const int BYTES_PER_PIXEL = 8; // 最適化後のサイズ（バイト/ピクセル）：座標ushort×2 + 色インデックスbyte + playerId byte + タイムスタンプushort
    private const int MESSAGE_OVERHEAD = 100; // メッセージオーバーヘッド（バイト）：pixelCount, baseTimestamp, 配列の長さなど
    
    [Header("References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Header("Network Settings")]
    [Tooltip("差分送信の間隔（秒）")]
    [SerializeField] private float sendInterval = 0.2f; // 0.2秒ごと
    
    [Tooltip("分割送信のチャンクサイズ（バイト）")]
    [SerializeField] private int chunkSize = 500000; // 500KB（Unity Netcodeの制限は約1MB）
    
    [Tooltip("差分送信の最大ピクセル数（これを超える場合は分割送信）")]
    [SerializeField] private int maxPixelsPerMessage = 10; // 最適化後: 10ピクセル × 8バイト = 約80バイト + オーバーヘッド ≈ 200バイト（制限内）
    
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
    /// メッセージサイズ制限を考慮し、maxPixelsPerMessage以下のピクセル数で分割
    /// 実際のメッセージサイズはSendPaintDiffMessage()で検証され、必要に応じてさらに分割される
    /// </summary>
    private void SendPaintDiffSplit(List<PaintDiffManager.PixelChange> changes)
    {
        int totalPixels = changes.Count;
        
        // 動的に最大ピクセル数を計算（メッセージサイズ制限を考慮）
        int maxAllowedPixels = Mathf.Max(1, (MAX_MESSAGE_SIZE - MESSAGE_OVERHEAD) / BYTES_PER_PIXEL);
        // ただし、maxPixelsPerMessageがより小さい場合はそれを使用（安全マージン）
        int pixelsPerChunk = Mathf.Min(maxPixelsPerMessage, maxAllowedPixels);
        
        // ピクセル数が制限を超えている場合は警告を出して、さらに小さく分割
        if (totalPixels > pixelsPerChunk * 100) // 100チャンク以上になる場合は警告
        {
            Debug.LogWarning($"NetworkPaintCanvas: 大量のピクセルを送信します。ピクセル数: {totalPixels}、チャンクサイズ: {pixelsPerChunk}、チャンク数: {Mathf.CeilToInt((float)totalPixels / pixelsPerChunk)}");
        }
        
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
            
            // Custom Messageで全プレイヤーに送信（サイズ検証が実行される）
            SendPaintDiffMessage(diffMessage);
        }
    }
    
    /// <summary>
    /// 差分データをパック（Custom Message用）
    /// 最適化版：座標をushort、Colorを色インデックスbyte、playerIdをbyte、タイムスタンプをushort相対値化
    /// メッセージサイズ制限: MAX_MESSAGE_SIZE = 1000バイト
    /// 最大ピクセル数（動的計算）: (MAX_MESSAGE_SIZE - MESSAGE_OVERHEAD) / BYTES_PER_PIXEL ≈ 112ピクセル
    /// ただし、実際にはより小さい値（maxPixelsPerMessage = 10）を使用して安全マージンを確保
    /// </summary>
    private PaintDiffMessage PackDiffMessage(List<PaintDiffManager.PixelChange> changes)
    {
        int count = changes.Count;
        ushort[] xCoords = new ushort[count];
        ushort[] yCoords = new ushort[count];
        byte[] colorIndices = new byte[count];
        byte[] playerIds = new byte[count];
        ushort[] timestamps = new ushort[count];
        
        // 基準タイムスタンプを取得（最初のタイムスタンプまたは現在時刻）
        float baseTimestamp = count > 0 ? changes[0].timestamp : GetServerTime();
        if (count > 0)
        {
            // 最小のタイムスタンプを基準にする
            foreach (var change in changes)
            {
                if (change.timestamp < baseTimestamp)
                {
                    baseTimestamp = change.timestamp;
                }
            }
        }
        
        for (int i = 0; i < count; i++)
        {
            var change = changes[i];
            
            // 座標をushortに変換（範囲チェック）
            xCoords[i] = (ushort)Mathf.Clamp(change.x, 0, ushort.MaxValue);
            yCoords[i] = (ushort)Mathf.Clamp(change.y, 0, ushort.MaxValue);
            
            // Colorを色インデックスに変換
            colorIndices[i] = PaintColorIndexHelper.ColorToIndex(change.color);
            
            // playerIdをbyteに変換（範囲チェック）
            playerIds[i] = (byte)Mathf.Clamp(change.playerId, 0, byte.MaxValue);
            
            // タイムスタンプを相対値（ushort、ミリ秒単位）に変換
            float relativeTime = (change.timestamp - baseTimestamp) * 1000f; // ミリ秒単位
            timestamps[i] = (ushort)Mathf.Clamp(Mathf.RoundToInt(relativeTime), 0, ushort.MaxValue);
        }
        
        return new PaintDiffMessage
        {
            pixelCount = count,
            baseTimestamp = baseTimestamp,
            xCoords = xCoords,
            yCoords = yCoords,
            colorIndices = colorIndices,
            playerIds = playerIds,
            timestamps = timestamps
        };
    }
    
    /// <summary>
    /// 差分データメッセージを送信（Custom Message）
    /// クライアントはサーバーに送信し、サーバーが全クライアントに転送
    /// メッセージサイズを検証し、制限を超える場合はさらに分割
    /// </summary>
    private void SendPaintDiffMessage(PaintDiffMessage message)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            return;
        }
        
        // INetworkSerializableの場合はWriteValueSafeを使用
        // 最適化後: 約8 bytes/ピクセル（座標ushort×2 + 色インデックスbyte + playerId byte + タイムスタンプushort）
        int estimatedSize = message.pixelCount * BYTES_PER_PIXEL + MESSAGE_OVERHEAD + 256; // 余裕を持たせたサイズ
        FastBufferWriter writer = new FastBufferWriter(estimatedSize, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(message);
        
        // 実際のメッセージサイズを検証
        int actualSize = (int)writer.Position;
        if (actualSize > MAX_MESSAGE_SIZE)
        {
            // メッセージサイズが制限を超えている場合、さらに小さく分割
            Debug.LogWarning($"NetworkPaintCanvas: メッセージサイズが制限を超えています。サイズ: {actualSize}バイト、制限: {MAX_MESSAGE_SIZE}バイト、ピクセル数: {message.pixelCount}。さらに分割します。");
            writer.Dispose();
            
            // ピクセル数を半分に減らして再分割
            int newMaxPixels = Mathf.Max(1, message.pixelCount / 2);
            SendPaintDiffMessageSplit(message, newMaxPixels);
            return;
        }
        
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
    /// メッセージをさらに小さく分割して送信（再帰的分割）
    /// </summary>
    private void SendPaintDiffMessageSplit(PaintDiffMessage originalMessage, int maxPixelsPerChunk)
    {
        // メッセージのピクセルデータをリストに変換
        List<PaintDiffManager.PixelChange> allChanges = new List<PaintDiffManager.PixelChange>();
        for (int i = 0; i < originalMessage.pixelCount; i++)
        {
            // 色インデックスからColorに変換（一時的に）
            Color color = PaintColorIndexHelper.IndexToColor(originalMessage.colorIndices[i]);
            
            // 相対タイムスタンプを絶対値に変換
            float absoluteTimestamp = originalMessage.baseTimestamp + (originalMessage.timestamps[i] / 1000f);
            
            allChanges.Add(new PaintDiffManager.PixelChange
            {
                x = originalMessage.xCoords[i],
                y = originalMessage.yCoords[i],
                color = color,
                playerId = originalMessage.playerIds[i],
                timestamp = absoluteTimestamp
            });
        }
        
        // 小さなチャンクに分割して送信
        int chunkCount = Mathf.CeilToInt((float)originalMessage.pixelCount / maxPixelsPerChunk);
        for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            int startIndex = chunkIndex * maxPixelsPerChunk;
            int endIndex = Mathf.Min(startIndex + maxPixelsPerChunk, originalMessage.pixelCount);
            int chunkPixelCount = endIndex - startIndex;
            
            List<PaintDiffManager.PixelChange> chunkChanges = new List<PaintDiffManager.PixelChange>();
            for (int i = startIndex; i < endIndex; i++)
            {
                chunkChanges.Add(allChanges[i]);
            }
            
            // 新しいメッセージを作成して送信
            PaintDiffMessage chunkMessage = PackDiffMessage(chunkChanges);
            SendPaintDiffMessage(chunkMessage); // 再帰的に呼び出し（サイズ検証が再度実行される）
        }
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
    /// 最適化版：色インデックスからColorに変換、相対タイムスタンプを絶対値に変換
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
            // 座標をushortからintに変換（範囲チェック済みなので安全）
            int x = message.xCoords[i];
            int y = message.yCoords[i];
            
            // 色インデックスからColorに変換
            Color color = PaintColorIndexHelper.IndexToColor(message.colorIndices[i]);
            
            // playerIdをbyteからintに変換
            int playerId = message.playerIds[i];
            
            // 相対タイムスタンプを絶対値に変換（ミリ秒から秒に戻す）
            float absoluteTimestamp = message.baseTimestamp + (message.timestamps[i] / 1000f);
            
            // タイムスタンプを比較して適用
            paintCanvas.PaintAtWithTimestamp(
                x, y,
                playerId,
                color,
                absoluteTimestamp
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
            
            // チャンクデータを作成（最適化版）
            ushort[] xCoords = new ushort[chunkPixelCount];
            ushort[] yCoords = new ushort[chunkPixelCount];
            byte[] colorIndices = new byte[chunkPixelCount];
            byte[] playerIdArray = new byte[chunkPixelCount];
            ushort[] timestampArray = new ushort[chunkPixelCount];
            
            // 基準タイムスタンプを取得（チャンク内の最小タイムスタンプ）
            float baseTimestamp = chunkPixelCount > 0 ? pixelDataList[startIndex].timestamp : GetServerTime();
            for (int i = 0; i < chunkPixelCount; i++)
            {
                float timestamp = pixelDataList[startIndex + i].timestamp;
                if (timestamp < baseTimestamp)
                {
                    baseTimestamp = timestamp;
                }
            }
            
            for (int i = 0; i < chunkPixelCount; i++)
            {
                var pixel = pixelDataList[startIndex + i];
                
                // 座標をushortに変換
                xCoords[i] = (ushort)Mathf.Clamp(pixel.x, 0, ushort.MaxValue);
                yCoords[i] = (ushort)Mathf.Clamp(pixel.y, 0, ushort.MaxValue);
                
                // Colorを色インデックスに変換
                colorIndices[i] = PaintColorIndexHelper.ColorToIndex(pixel.color);
                
                // playerIdをbyteに変換
                playerIdArray[i] = (byte)Mathf.Clamp(pixel.playerId, 0, byte.MaxValue);
                
                // タイムスタンプを相対値（ushort、ミリ秒単位）に変換
                float relativeTime = (pixel.timestamp - baseTimestamp) * 1000f;
                timestampArray[i] = (ushort)Mathf.Clamp(Mathf.RoundToInt(relativeTime), 0, ushort.MaxValue);
            }
            
            // Custom Messageでチャンクを送信
            PaintSnapshotMessage snapshotMessage = new PaintSnapshotMessage
            {
                width = width,
                height = height,
                chunkIndex = chunkIndex,
                totalChunks = chunkCount,
                baseTimestamp = baseTimestamp,
                xCoords = xCoords,
                yCoords = yCoords,
                colorIndices = colorIndices,
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
        
        // チャンクを保存（最適化版：逆変換を適用）
        // 色インデックスからColorに変換、相対タイムスタンプを絶対値に変換
        int count = message.xCoords?.Length ?? 0;
        int[] xCoords = new int[count];
        int[] yCoords = new int[count];
        Color[] colors = new Color[count];
        int[] playerIds = new int[count];
        float[] timestamps = new float[count];
        
        for (int i = 0; i < count; i++)
        {
            // 座標をushortからintに変換
            xCoords[i] = message.xCoords[i];
            yCoords[i] = message.yCoords[i];
            
            // 色インデックスからColorに変換
            colors[i] = PaintColorIndexHelper.IndexToColor(message.colorIndices[i]);
            
            // playerIdをbyteからintに変換
            playerIds[i] = message.playerIds[i];
            
            // 相対タイムスタンプを絶対値に変換（ミリ秒から秒に戻す）
            timestamps[i] = message.baseTimestamp + (message.timestamps[i] / 1000f);
        }
        
        buffer.chunks[message.chunkIndex] = new SnapshotChunkData
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
        // 最適化後: 約8 bytes/ピクセル（座標ushort×2 + 色インデックスbyte + playerId byte + タイムスタンプushort）
        int estimatedSize = message.pixelCount * 12 + 256; // 余裕を持たせたサイズ
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
        // 最適化後: 約8 bytes/ピクセル
        int pixelCount = message.xCoords?.Length ?? 0;
        int estimatedSize = pixelCount * 12 + 256; // 余裕を持たせたサイズ
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
            
            // チャンクデータを作成（最適化版）
            ushort[] xCoords = new ushort[chunkPixelCount];
            ushort[] yCoords = new ushort[chunkPixelCount];
            byte[] colorIndices = new byte[chunkPixelCount];
            byte[] playerIdArray = new byte[chunkPixelCount];
            ushort[] timestampArray = new ushort[chunkPixelCount];
            
            // 基準タイムスタンプを取得（チャンク内の最小タイムスタンプ）
            float baseTimestamp = chunkPixelCount > 0 ? pixelDataList[startIndex].timestamp : GetServerTime();
            for (int i = 0; i < chunkPixelCount; i++)
            {
                float timestamp = pixelDataList[startIndex + i].timestamp;
                if (timestamp < baseTimestamp)
                {
                    baseTimestamp = timestamp;
                }
            }
            
            for (int i = 0; i < chunkPixelCount; i++)
            {
                var pixel = pixelDataList[startIndex + i];
                
                // 座標をushortに変換
                xCoords[i] = (ushort)Mathf.Clamp(pixel.x, 0, ushort.MaxValue);
                yCoords[i] = (ushort)Mathf.Clamp(pixel.y, 0, ushort.MaxValue);
                
                // Colorを色インデックスに変換
                colorIndices[i] = PaintColorIndexHelper.ColorToIndex(pixel.color);
                
                // playerIdをbyteに変換
                playerIdArray[i] = (byte)Mathf.Clamp(pixel.playerId, 0, byte.MaxValue);
                
                // タイムスタンプを相対値（ushort、ミリ秒単位）に変換
                float relativeTime = (pixel.timestamp - baseTimestamp) * 1000f;
                timestampArray[i] = (ushort)Mathf.Clamp(Mathf.RoundToInt(relativeTime), 0, ushort.MaxValue);
            }
            
            PaintSnapshotMessage snapshotMessage = new PaintSnapshotMessage
            {
                width = width,
                height = height,
                chunkIndex = chunkIndex,
                totalChunks = chunkCount,
                baseTimestamp = baseTimestamp,
                xCoords = xCoords,
                yCoords = yCoords,
                colorIndices = colorIndices,
                playerIds = playerIdArray,
                timestamps = timestampArray
            };
            
            // INetworkSerializableの場合はWriteValueSafeを使用
            // 最適化後: 約8 bytes/ピクセル
            int estimatedSize = chunkPixelCount * 12 + 256; // 余裕を持たせたサイズ
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

