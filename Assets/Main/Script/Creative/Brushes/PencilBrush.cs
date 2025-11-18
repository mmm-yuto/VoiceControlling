using UnityEngine;

/// <summary>
/// 鉛筆ブラシの実装
/// 細い線、連続的な描画用
/// </summary>
[CreateAssetMenu(fileName = "PencilBrush", menuName = "Game/Brushes/Pencil Brush")]
public class PencilBrush : BrushStrategyBase
{
    public override void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity)
    {
        if (canvas == null) return;
        canvas.PaintAtWithRadius(position, playerId, intensity, color, radius);
    }
}

