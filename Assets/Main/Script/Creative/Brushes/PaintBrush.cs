using UnityEngine;

/// <summary>
/// ペンキブラシの実装
/// 太い線、広範囲の塗りつぶし用
/// </summary>
[CreateAssetMenu(fileName = "PaintBrush", menuName = "Game/Brushes/Paint Brush")]
public class PaintBrush : BrushStrategyBase
{
    public override void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity)
    {
        if (canvas == null) return;
        canvas.PaintAtWithRadius(position, playerId, intensity, color, radius);
    }
}

