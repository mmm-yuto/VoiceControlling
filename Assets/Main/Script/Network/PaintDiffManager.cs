using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 塗りキャンバスの差分検出を管理
/// 前回送信時からの変更を検出して、変更されたピクセルのみを返す
/// </summary>
public class PaintDiffManager
{
    /// <summary>
    /// 変更されたピクセルの情報
    /// </summary>
    public struct PixelChange
    {
        public int x;
        public int y;
        public Color color;
        public int playerId;
        public float timestamp;
        
        public PixelChange(int x, int y, Color color, int playerId, float timestamp)
        {
            this.x = x;
            this.y = y;
            this.color = color;
            this.playerId = playerId;
            this.timestamp = timestamp;
        }
    }
    
    // 前回送信時の状態を保持
    private Color[,] lastSentColorData;
    private float[,] lastSentTimestamp;
    private int width;
    private int height;
    private bool isInitialized = false;
    
    /// <summary>
    /// 差分検出マネージャーを初期化
    /// </summary>
    public void Initialize(int width, int height)
    {
        this.width = width;
        this.height = height;
        
        // 前回送信時の状態を初期化
        lastSentColorData = new Color[width, height];
        lastSentTimestamp = new float[width, height];
        
        // 初期状態を0で初期化
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                lastSentColorData[x, y] = Color.clear;
                lastSentTimestamp[x, y] = 0f;
            }
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// 変更されたピクセルを検出
    /// </summary>
    /// <param name="currentColorData">現在の色データ</param>
    /// <param name="currentTimestamp">現在のタイムスタンプデータ</param>
    /// <param name="currentPlayerId">現在のプレイヤーIDデータ</param>
    /// <returns>変更されたピクセルのリスト</returns>
    public List<PixelChange> DetectChanges(Color[,] currentColorData, float[,] currentTimestamp, int[,] currentPlayerId)
    {
        if (!isInitialized)
        {
            Debug.LogError("PaintDiffManager: 初期化されていません。Initialize()を呼び出してください。");
            return new List<PixelChange>();
        }
        
        List<PixelChange> changes = new List<PixelChange>();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 色が変更された、またはタイムスタンプが新しい場合
                if (currentColorData[x, y] != lastSentColorData[x, y] || 
                    currentTimestamp[x, y] > lastSentTimestamp[x, y])
                {
                    changes.Add(new PixelChange(
                        x,
                        y,
                        currentColorData[x, y],
                        currentPlayerId[x, y],
                        currentTimestamp[x, y]
                    ));
                }
            }
        }
        
        // 前回状態を更新
        UpdateLastSentState(currentColorData, currentTimestamp);
        
        return changes;
    }
    
    /// <summary>
    /// 前回送信時の状態を更新
    /// </summary>
    private void UpdateLastSentState(Color[,] currentColorData, float[,] currentTimestamp)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                lastSentColorData[x, y] = currentColorData[x, y];
                lastSentTimestamp[x, y] = currentTimestamp[x, y];
            }
        }
    }
    
    /// <summary>
    /// フルスナップショットを送信した後に状態をリセット
    /// </summary>
    public void ResetAfterSnapshot(Color[,] snapshotColorData, float[,] snapshotTimestamp)
    {
        UpdateLastSentState(snapshotColorData, snapshotTimestamp);
    }
}

