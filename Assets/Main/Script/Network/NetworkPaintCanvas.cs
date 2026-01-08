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
    [SerializeField] private float sendInterval = 0.1f; // 0.1秒ごと（より頻繁に送信して、1回あたりのデータ量を減らす）
    
    [Tooltip("分割送信のチャンクサイズ（バイト）")]
    [SerializeField] private int chunkSize = 500000; // 500KB（Unity Netcodeの制限は約1MB）
    
    [Tooltip("差分送信の最大ピクセル数（これを超える場合は分割送信）")]
    [SerializeField] private int maxPixelsPerMessage = 5000; // 約160KB（5000ピクセル × 32バイト、安全マージンを考慮）
    
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
        Debug.Log("NetworkPaintCanvas: SyncInitialState - 実装は将来の拡張用");
    }
    
    void Update()
    {
        // サーバー側で定期的に差分を送信
        if (IsServer && paintCanvas != null && diffManager != null)
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
            // 実際のデータサイズを計算（1ピクセルあたり32バイト）
            // int x(4) + int y(4) + Color(16) + int playerId(4) + float timestamp(4) = 32バイト
            // RPCのオーバーヘッド（ヘッダー、メタデータなど）を考慮して、より小さな制限を設定
            const int bytesPerPixel = 32;
            const int maxBytesPerMessage = 150000; // 150KB（Unity Netcodeの制限は約1MBだが、RPCオーバーヘッドを考慮して安全マージンを大きく取る）
            int estimatedBytes = changes.Count * bytesPerPixel;
            
            if (Application.isEditor)
            {
                Debug.Log($"[DEBUG] SendPaintDiff: {changes.Count}ピクセル変更、推定サイズ: {estimatedBytes}バイト ({estimatedBytes / 1024}KB)");
            }
            
            // ピクセル数またはデータサイズが制限を超える場合は分割送信
            if (changes.Count > maxPixelsPerMessage || estimatedBytes > maxBytesPerMessage)
            {
                // データサイズに基づいて適切なチャンクサイズを計算
                // より安全なサイズ（maxBytesPerMessageの半分）を使用
                int safePixelsPerChunk = Mathf.Min(maxPixelsPerMessage, (maxBytesPerMessage / 2) / bytesPerPixel);
                if (Application.isEditor)
                {
                    Debug.Log($"[DEBUG] SendPaintDiff: 分割送信が必要 - {changes.Count}ピクセル、チャンクサイズ: {safePixelsPerChunk}ピクセル");
                }
                SendPaintDiffSplit(changes, safePixelsPerChunk);
            }
            else
            {
            try
            {
                // データをパック
                PaintDiffData diffData = PackDiffData(changes);
                
                // 全クライアントに送信
                ApplyPaintDiffClientRpc(diffData);
                
                if (Application.isEditor)
                {
                    Debug.Log($"NetworkPaintCanvas: 差分を送信 - {changes.Count}ピクセル変更 ({estimatedBytes / 1024}KB)");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DEBUG] SendPaintDiff: 送信エラーが発生しました - {ex.GetType().Name}: {ex.Message}");
                // エラーが発生した場合は、より小さなチャンクに分割して再送信
                // より安全なサイズ（1000ピクセル）に分割
                int safePixelsPerChunk = Mathf.Max(100, Mathf.Min(1000, maxPixelsPerMessage / 5));
                Debug.LogWarning($"[DEBUG] SendPaintDiff: より小さなチャンク ({safePixelsPerChunk}ピクセル/チャンク) に分割して再送信します");
                SendPaintDiffSplit(changes, safePixelsPerChunk);
            }
            }
        }
    }
    
    /// <summary>
    /// 差分を分割して送信
    /// </summary>
    private void SendPaintDiffSplit(List<PaintDiffManager.PixelChange> changes, int pixelsPerChunk)
    {
        int totalPixels = changes.Count;
        int chunkCount = Mathf.CeilToInt((float)totalPixels / pixelsPerChunk);
        
        const int bytesPerPixel = 32;
        int estimatedTotalBytes = totalPixels * bytesPerPixel;
        
        if (Application.isEditor)
        {
            Debug.Log($"[DEBUG] SendPaintDiffSplit: {totalPixels}ピクセル（推定{estimatedTotalBytes / 1024}KB）を{pixelsPerChunk}ピクセル/チャンクで{chunkCount}チャンクに分割");
        }
        
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
            
            try
            {
                // データをパック
                PaintDiffData diffData = PackDiffData(chunkChanges);
                
                int chunkBytes = chunkPixelCount * bytesPerPixel;
                if (Application.isEditor)
                {
                    Debug.Log($"[DEBUG] SendPaintDiffSplit: チャンク {chunkIndex + 1}/{chunkCount} を送信 - {chunkPixelCount}ピクセル（推定{chunkBytes / 1024}KB）");
                }
                
                // チャンクを送信
                ApplyPaintDiffClientRpc(diffData);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DEBUG] SendPaintDiffSplit: チャンク {chunkIndex + 1}/{chunkCount} の送信でエラーが発生しました - {ex.GetType().Name}: {ex.Message}");
                // エラーが発生した場合は、このチャンクをさらに小さく分割
                if (chunkPixelCount > 100)
                {
                    // より安全なサイズ（500ピクセル以下）に分割
                    int smallerChunkSize = Mathf.Max(100, Mathf.Min(500, pixelsPerChunk / 10));
                    Debug.LogWarning($"[DEBUG] SendPaintDiffSplit: チャンクをさらに小さく分割 ({smallerChunkSize}ピクセル/チャンク) して再送信します");
                    SendPaintDiffSplit(chunkChanges, smallerChunkSize);
                }
                else
                {
                    Debug.LogError($"[DEBUG] SendPaintDiffSplit: チャンクサイズが既に最小 ({chunkPixelCount}ピクセル) です。送信をスキップします。");
                }
            }
        }
    }
    
    /// <summary>
    /// 差分データをパック
    /// </summary>
    private PaintDiffData PackDiffData(List<PaintDiffManager.PixelChange> changes)
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
        
        return new PaintDiffData
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
    /// 差分データの構造体（Unity Netcodeで送信可能な形式）
    /// </summary>
    public struct PaintDiffData : INetworkSerializable
    {
        public int pixelCount;
        public int[] xCoords;
        public int[] yCoords;
        public Color[] colors;
        public int[] playerIds;
        public float[] timestamps;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref pixelCount);
            
            if (serializer.IsReader)
            {
                // 読み取り時：配列を初期化
                xCoords = new int[pixelCount];
                yCoords = new int[pixelCount];
                colors = new Color[pixelCount];
                playerIds = new int[pixelCount];
                timestamps = new float[pixelCount];
            }
            
            // 配列をシリアライズ
            for (int i = 0; i < pixelCount; i++)
            {
                serializer.SerializeValue(ref xCoords[i]);
                serializer.SerializeValue(ref yCoords[i]);
                serializer.SerializeValue(ref colors[i]);
                serializer.SerializeValue(ref playerIds[i]);
                serializer.SerializeValue(ref timestamps[i]);
            }
        }
    }
    
    /// <summary>
    /// クライアント側の塗りをサーバーに送信（ServerRpc）
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SendClientPaintServerRpc(Vector2 position, int playerId, float intensity, Color color, float radius, ServerRpcParams rpcParams = default)
    {
        if (paintCanvas == null)
        {
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        // サーバー側のPaintCanvasに塗りを適用
        // これにより、サーバー側の差分検出が変更を検出できる
        paintCanvas.PaintAtWithRadius(position, playerId, intensity, color, radius);
        
        if (Application.isEditor)
        {
            Debug.Log($"NetworkPaintCanvas: クライアント塗りをサーバー側に適用 - Position: {position}, PlayerId: {playerId}, Intensity: {intensity}, Radius: {radius}");
        }
    }
    
    /// <summary>
    /// 差分データを受信して適用（ClientRpc）
    /// </summary>
    [ClientRpc]
    private void ApplyPaintDiffClientRpc(PaintDiffData diffData, ClientRpcParams rpcParams = default)
    {
        // サーバー側では実行しない（サーバーは既に最新の状態を持っている）
        if (IsServer) return;
        
        if (paintCanvas == null)
        {
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        try
        {
            // データの妥当性チェック
            if (diffData.pixelCount <= 0)
            {
                Debug.LogWarning($"[DEBUG] ApplyPaintDiffClientRpc: 無効なpixelCount ({diffData.pixelCount})");
                return;
            }
            
            if (diffData.xCoords == null || diffData.yCoords == null || 
                diffData.colors == null || diffData.playerIds == null || 
                diffData.timestamps == null)
            {
                Debug.LogError("[DEBUG] ApplyPaintDiffClientRpc: データ配列がnullです");
                return;
            }
            
            if (diffData.xCoords.Length < diffData.pixelCount ||
                diffData.yCoords.Length < diffData.pixelCount ||
                diffData.colors.Length < diffData.pixelCount ||
                diffData.playerIds.Length < diffData.pixelCount ||
                diffData.timestamps.Length < diffData.pixelCount)
            {
                Debug.LogError($"[DEBUG] ApplyPaintDiffClientRpc: データ配列のサイズが不足しています (pixelCount: {diffData.pixelCount})");
                return;
            }
            
            // 各ピクセルを更新
            for (int i = 0; i < diffData.pixelCount; i++)
            {
                int x = diffData.xCoords[i];
                int y = diffData.yCoords[i];
                
                // タイムスタンプを比較して適用
                paintCanvas.PaintAtWithTimestamp(
                    x, y,
                    diffData.playerIds[i],
                    diffData.colors[i],
                    diffData.timestamps[i]
                );
            }
            
            if (Application.isEditor)
            {
                Debug.Log($"[DEBUG] NetworkPaintCanvas: 差分を受信して適用 - {diffData.pixelCount}ピクセル");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DEBUG] ApplyPaintDiffClientRpc: エラーが発生しました - {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
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
        
        if (paintCanvas == null)
        {
            Debug.LogError("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        // PaintCanvasにタイムスタンプ取得コールバックを設定
        paintCanvas.SetTimestampCallback(GetServerTime);
        
        // サーバー側で差分検出マネージャーを初期化
        if (IsServer && diffManager != null)
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
        
        Debug.Log($"NetworkPaintCanvas: ネットワーク接続 - IsServer: {IsServer}, IsClient: {IsClient}, IsOwner: {IsOwner}");
        
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
        
        if (Application.isEditor)
        {
            Debug.Log($"NetworkPaintCanvas: 初回同期を送信 - {totalPixels}ピクセル、{chunkCount}チャンクに分割");
        }
        
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
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
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
            
            if (Application.isEditor)
            {
                Debug.Log($"NetworkPaintCanvas: 初回同期を受信して適用 - {width}x{height}, {totalChunks}チャンク");
            }
        }
        else
        {
            if (Application.isEditor)
            {
                Debug.Log($"NetworkPaintCanvas: スナップショットチャンク受信 - {chunkIndex + 1}/{totalChunks}");
            }
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
        base.OnNetworkDespawn();
        Debug.Log("NetworkPaintCanvas: ネットワーク切断");
    }
}

