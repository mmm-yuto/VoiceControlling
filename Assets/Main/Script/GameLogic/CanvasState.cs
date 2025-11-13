using UnityEngine;

/// <summary>
/// キャンバス状態のスナップショット
/// CreativeModeのUndo/Redoや保存で使用
/// </summary>
[System.Serializable]
public class CanvasState
{
    public int width;
    public int height;
    public int[,] playerIds;
    public float[,] intensities;
    public Color32[,] colors;

    public CanvasState(int width, int height)
    {
        this.width = width;
        this.height = height;
        playerIds = new int[width, height];
        intensities = new float[width, height];
        colors = new Color32[width, height];
    }
}

