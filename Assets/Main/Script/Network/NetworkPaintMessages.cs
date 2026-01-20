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
/// 色インデックス変換ヘルパー
/// 10色のプリセット色とColorの変換を行う
/// </summary>
public static class PaintColorIndexHelper
{
    /// <summary>
    /// Colorから色インデックスを取得
    /// ColorSelectionSettingsのpresetColorsと比較して最も近い色のインデックスを返す
    /// </summary>
    public static byte ColorToIndex(Color color)
    {
        // ColorSelectionSettingsから取得を試みる
        ColorSelectionSettings colorSelectionSettings = FindColorSelectionSettings();
        if (colorSelectionSettings != null && colorSelectionSettings.presetColors != null && colorSelectionSettings.presetColors.Length > 0)
        {
            Color[] presetColors = colorSelectionSettings.presetColors;
            
            // 最も近い色を見つける（RGB距離で比較）
            float minDistance = float.MaxValue;
            byte closestIndex = 0;
            
            for (byte i = 0; i < presetColors.Length; i++)
            {
                float distance = ColorDistance(color, presetColors[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }
            
            return closestIndex;
        }
        
        // BattleSettingsを使用（MainColorSettingsの場合）
        if (BattleSettings.Instance != null)
        {
            // MainColorSettingsの2色と比較
            Color mainColor1 = BattleSettings.Instance.GetMainColor1();
            Color mainColor2 = BattleSettings.Instance.GetMainColor2();
            
            float dist1 = ColorDistance(color, mainColor1);
            float dist2 = ColorDistance(color, mainColor2);
            
            return (byte)(dist1 < dist2 ? 0 : 1);
        }
        
        return 0; // フォールバック
    }
    
    /// <summary>
    /// 色インデックスからColorを取得
    /// </summary>
    public static Color IndexToColor(byte colorIndex)
    {
        if (BattleSettings.Instance != null)
        {
            return BattleSettings.Instance.GetColorFromIndex(colorIndex);
        }
        
        // フォールバック: ColorSelectionSettingsから直接取得
        ColorSelectionSettings colorSelectionSettings = FindColorSelectionSettings();
        if (colorSelectionSettings != null && colorSelectionSettings.presetColors != null && colorSelectionSettings.presetColors.Length > 0)
        {
            int safeIndex = Mathf.Clamp(colorIndex, 0, colorSelectionSettings.presetColors.Length - 1);
            return colorSelectionSettings.presetColors[safeIndex];
        }
        
        return Color.white; // 最終的なフォールバック
    }
    
    /// <summary>
    /// ColorSelectionSettingsを検索（キャッシュなし）
    /// </summary>
    private static ColorSelectionSettings FindColorSelectionSettings()
    {
        // BattleSettingsから取得を試みる
        if (BattleSettings.Instance != null)
        {
            // BattleSettingsはcolorSelectionSettingsフィールドを持っているが、privateのため、
            // FindObjectOfTypeを使用する
            ColorSelectionSettings settings = Object.FindObjectOfType<ColorSelectionSettings>();
            if (settings != null)
            {
                return settings;
            }
        }
        
        // FindObjectOfTypeで検索（シーン内）
        return Object.FindObjectOfType<ColorSelectionSettings>();
    }
    
    /// <summary>
    /// 2つのColor間のRGB距離（二乗）を計算
    /// </summary>
    private static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        // アルファ値は考慮しない（塗りでは不透明度は使用されない）
        return dr * dr + dg * dg + db * db;
    }
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
/// 最適化版：座標をushort、Colorを色インデックスbyte、playerIdをbyte、タイムスタンプをushort相対値
/// </summary>
public struct PaintDiffMessage : INetworkSerializable
{
    public int pixelCount;
    public ushort[] xCoords;           // 最適化: int → ushort (2 bytes)
    public ushort[] yCoords;           // 最適化: int → ushort (2 bytes)
    public byte[] colorIndices;        // 最適化: Color → byte (1 byte, 0-9の色インデックス)
    public byte[] playerIds;           // 最適化: int → byte (1 byte)
    public ushort[] timestamps;        // 最適化: float → ushort (2 bytes, 相対値)
    public float baseTimestamp;        // 基準タイムスタンプ（相対値の基準）
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref pixelCount);
        serializer.SerializeValue(ref baseTimestamp);
        
        if (serializer.IsReader)
        {
            // 読み取り時：配列を初期化
            xCoords = new ushort[pixelCount];
            yCoords = new ushort[pixelCount];
            colorIndices = new byte[pixelCount];
            playerIds = new byte[pixelCount];
            timestamps = new ushort[pixelCount];
        }
        
        // 配列をシリアライズ
        for (int i = 0; i < pixelCount; i++)
        {
            serializer.SerializeValue(ref xCoords[i]);
            serializer.SerializeValue(ref yCoords[i]);
            serializer.SerializeValue(ref colorIndices[i]);
            serializer.SerializeValue(ref playerIds[i]);
            serializer.SerializeValue(ref timestamps[i]);
        }
    }
}

/// <summary>
/// スナップショットメッセージ（初回同期用）
/// 最適化版：座標をushort、Colorを色インデックスbyte、playerIdをbyte、タイムスタンプをushort相対値
/// </summary>
public struct PaintSnapshotMessage : INetworkSerializable
{
    public int width;
    public int height;
    public int chunkIndex;
    public int totalChunks;
    public ushort[] xCoords;           // 最適化: int → ushort (2 bytes)
    public ushort[] yCoords;           // 最適化: int → ushort (2 bytes)
    public byte[] colorIndices;        // 最適化: Color → byte (1 byte, 0-9の色インデックス)
    public byte[] playerIds;           // 最適化: int → byte (1 byte)
    public ushort[] timestamps;        // 最適化: float → ushort (2 bytes, 相対値)
    public float baseTimestamp;        // 基準タイムスタンプ（相対値の基準）
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref width);
        serializer.SerializeValue(ref height);
        serializer.SerializeValue(ref chunkIndex);
        serializer.SerializeValue(ref totalChunks);
        serializer.SerializeValue(ref baseTimestamp);
        
        int count = xCoords?.Length ?? 0;
        serializer.SerializeValue(ref count);
        
        if (serializer.IsReader)
        {
            // 読み取り時：配列を初期化
            if (count > 0)
            {
                xCoords = new ushort[count];
                yCoords = new ushort[count];
                colorIndices = new byte[count];
                playerIds = new byte[count];
                timestamps = new ushort[count];
            }
        }
        
        // 配列をシリアライズ
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                serializer.SerializeValue(ref xCoords[i]);
                serializer.SerializeValue(ref yCoords[i]);
                serializer.SerializeValue(ref colorIndices[i]);
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
