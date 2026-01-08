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
            // データをパック
            PaintDiffData diffData = PackDiffData(changes);
            
            // 全クライアントに送信
            ApplyPaintDiffClientRpc(diffData);
            
            if (Application.isEditor)
            {
                Debug.Log($"NetworkPaintCanvas: 差分を送信 - {changes.Count}ピクセル変更");
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
            Debug.Log($"NetworkPaintCanvas: 差分を受信して適用 - {diffData.pixelCount}ピクセル");
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
    /// </summary>
    private void SendInitialSnapshot()
    {
        if (paintCanvas == null) return;
        
        // フルテクスチャデータを圧縮
        byte[] textureData = paintCanvas.GetTexture().EncodeToPNG();
        
        // タイムスタンプ配列をシリアライズ
        byte[] timestampData = SerializeTimestamps(paintCanvas.GetPaintTimestamps());
        
        // プレイヤーID配列をシリアライズ
        byte[] playerIdData = SerializePlayerIds(paintCanvas.GetPaintData());
        
        // 全クライアントに送信
        SendFullSnapshotClientRpc(textureData, timestampData, playerIdData);
        
        if (Application.isEditor)
        {
            Debug.Log($"NetworkPaintCanvas: 初回同期を送信 - Texture: {textureData.Length} bytes, Timestamps: {timestampData.Length} bytes, PlayerIds: {playerIdData.Length} bytes");
        }
    }
    
    /// <summary>
    /// プレイヤーID配列をシリアライズ
    /// </summary>
    private byte[] SerializePlayerIds(int[,] playerIds)
    {
        if (playerIds == null) return new byte[0];
        
        int width = playerIds.GetLength(0);
        int height = playerIds.GetLength(1);
        
        // int配列に変換
        int[] flatPlayerIds = new int[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                flatPlayerIds[y * width + x] = playerIds[x, y];
            }
        }
        
        // byte配列に変換
        byte[] result = new byte[flatPlayerIds.Length * sizeof(int)];
        System.Buffer.BlockCopy(flatPlayerIds, 0, result, 0, result.Length);
        
        return result;
    }
    
    /// <summary>
    /// プレイヤーID配列をデシリアライズ
    /// </summary>
    private int[,] DeserializePlayerIds(byte[] data, int width, int height)
    {
        if (data == null || data.Length == 0) return new int[width, height];
        
        // byte配列からint配列に変換
        int[] flatPlayerIds = new int[data.Length / sizeof(int)];
        System.Buffer.BlockCopy(data, 0, flatPlayerIds, 0, data.Length);
        
        // 2D配列に変換
        int[,] result = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x, y] = flatPlayerIds[y * width + x];
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// タイムスタンプ配列をシリアライズ
    /// </summary>
    private byte[] SerializeTimestamps(float[,] timestamps)
    {
        if (timestamps == null) return new byte[0];
        
        int width = timestamps.GetLength(0);
        int height = timestamps.GetLength(1);
        
        // float配列に変換
        float[] flatTimestamps = new float[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                flatTimestamps[y * width + x] = timestamps[x, y];
            }
        }
        
        // byte配列に変換
        byte[] result = new byte[flatTimestamps.Length * sizeof(float)];
        System.Buffer.BlockCopy(flatTimestamps, 0, result, 0, result.Length);
        
        return result;
    }
    
    /// <summary>
    /// タイムスタンプ配列をデシリアライズ
    /// </summary>
    private float[,] DeserializeTimestamps(byte[] data, int width, int height)
    {
        if (data == null || data.Length == 0) return new float[width, height];
        
        // byte配列からfloat配列に変換
        float[] flatTimestamps = new float[data.Length / sizeof(float)];
        System.Buffer.BlockCopy(data, 0, flatTimestamps, 0, data.Length);
        
        // 2D配列に変換
        float[,] result = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x, y] = flatTimestamps[y * width + x];
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// フルスナップショットを受信して適用（ClientRpc）
    /// </summary>
    [ClientRpc]
    private void SendFullSnapshotClientRpc(byte[] textureData, byte[] timestampData, byte[] playerIdData, ClientRpcParams rpcParams = default)
    {
        if (IsServer) return; // サーバー側では実行しない
        
        if (paintCanvas == null)
        {
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        // テクスチャデータを復元
        Texture2D snapshotTexture = new Texture2D(2, 2);
        if (snapshotTexture.LoadImage(textureData))
        {
            // テクスチャのサイズを確認
            int width = snapshotTexture.width;
            int height = snapshotTexture.height;
            
            // タイムスタンプ配列をデシリアライズ
            float[,] timestamps = DeserializeTimestamps(timestampData, width, height);
            
            // プレイヤーID配列をデシリアライズ
            int[,] playerIds = DeserializePlayerIds(playerIdData, width, height);
            
            // テクスチャから色データを取得
            Color[] pixels = snapshotTexture.GetPixels();
            Color[,] colorData = new Color[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int index = y * width + x;
                    colorData[x, y] = pixels[index];
                }
            }
            
            // 各ピクセルをタイムスタンプ付きで適用
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (timestamps[x, y] > 0f) // タイムスタンプが0より大きい場合のみ適用
                    {
                        paintCanvas.PaintAtWithTimestamp(x, y, playerIds[x, y], colorData[x, y], timestamps[x, y]);
                    }
                }
            }
            
            // 差分検出マネージャーの状態をリセット
            if (diffManager != null)
            {
                diffManager.ResetAfterSnapshot(colorData, timestamps);
            }
            
            if (Application.isEditor)
            {
                Debug.Log($"NetworkPaintCanvas: 初回同期を受信して適用 - {width}x{height}");
            }
        }
        else
        {
            Debug.LogError("NetworkPaintCanvas: テクスチャデータの復元に失敗しました");
        }
        
        // テクスチャを破棄
        Destroy(snapshotTexture);
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

