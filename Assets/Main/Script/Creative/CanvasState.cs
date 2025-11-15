using UnityEngine;
using System;

/// <summary>
/// キャンバスの状態を保存するためのクラス（Undo/Redo用）
/// </summary>
[Serializable]
public class CanvasState
{
    public int width;
    public int height;
    public int[,] playerIds;
    public float[,] intensities;
    public Color[,] colors;
    
    public CanvasState(int width, int height)
    {
        this.width = width;
        this.height = height;
        playerIds = new int[width, height];
        intensities = new float[width, height];
        colors = new Color[width, height];
    }
    
    /// <summary>
    /// 別のCanvasStateからコピー
    /// </summary>
    public void CopyFrom(CanvasState other)
    {
        if (other == null || other.width != width || other.height != height)
        {
            Debug.LogError("CanvasState: CopyFrom failed - size mismatch");
            return;
        }
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                playerIds[x, y] = other.playerIds[x, y];
                intensities[x, y] = other.intensities[x, y];
                colors[x, y] = other.colors[x, y];
            }
        }
    }
}

