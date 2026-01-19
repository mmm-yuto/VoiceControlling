using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Custom Message用のメッセージタイプ定義
/// </summary>
public enum PaintMessageType : byte
{
    PaintData = 0,           // 塗りデータ（即座に送信）
    PaintDiff = 1,           // 差分データ（定期的に送信）
    PaintSnapshot = 2,       // 初回同期データ
    PaintSnapshotRequest = 3 // スナップショットリクエスト
}

/// <summary>
/// 塗りデータメッセージ（即座に送信）
/// </summary>
public struct PaintDataMessage : INetworkSerializable
{
    public Vector2 position;
    public int playerId;
    public float intensity;
    public Color color;
    public float radius;
    public float timestamp;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref intensity);
        serializer.SerializeValue(ref color);
        serializer.SerializeValue(ref radius);
        serializer.SerializeValue(ref timestamp);
    }
}

/// <summary>
/// 差分データメッセージ（定期的に送信）
/// </summary>
public struct PaintDiffMessage : INetworkSerializable
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
/// スナップショットメッセージ（初回同期用）
/// </summary>
public struct PaintSnapshotMessage : INetworkSerializable
{
    public int width;
    public int height;
    public int chunkIndex;
    public int totalChunks;
    public int[] xCoords;
    public int[] yCoords;
    public Color[] colors;
    public int[] playerIds;
    public float[] timestamps;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref width);
        serializer.SerializeValue(ref height);
        serializer.SerializeValue(ref chunkIndex);
        serializer.SerializeValue(ref totalChunks);
        
        int count = xCoords?.Length ?? 0;
        serializer.SerializeValue(ref count);
        
        if (serializer.IsReader)
        {
            // 読み取り時：配列を初期化
            if (count > 0)
            {
                xCoords = new int[count];
                yCoords = new int[count];
                colors = new Color[count];
                playerIds = new int[count];
                timestamps = new float[count];
            }
        }
        
        // 配列をシリアライズ
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                serializer.SerializeValue(ref xCoords[i]);
                serializer.SerializeValue(ref yCoords[i]);
                serializer.SerializeValue(ref colors[i]);
                serializer.SerializeValue(ref playerIds[i]);
                serializer.SerializeValue(ref timestamps[i]);
            }
        }
    }
}

/// <summary>
/// スナップショットリクエストメッセージ
/// </summary>
public struct PaintSnapshotRequestMessage : INetworkSerializable
{
    public ulong requesterClientId;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref requesterClientId);
    }
}
