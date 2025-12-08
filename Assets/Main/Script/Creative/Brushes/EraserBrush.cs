using UnityEngine;

/// <summary>
/// 消しブラシの実装
/// 指定位置の色を消すためのブラシ。
/// 他のブラシとは異なり、色を塗るのではなく消す動作を行う。
/// </summary>
[CreateAssetMenu(fileName = "EraserBrush", menuName = "Game/Brushes/Eraser Brush")]
public class EraserBrush : BrushStrategyBase
{
    [Header("Eraser Brush Settings")]
    [Tooltip("消しブラシの有効半径（ピクセル単位）。0以下なら基底クラスの radius を使用します。")]
    public float eraserRadius = 30f;

    /// <summary>
    /// 消しブラシによる処理。
    /// 色を塗るのではなく、指定位置の色を消す。
    /// </summary>
    public override void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity)
    {
        if (canvas == null) return;

        // 実際に使用する半径を決定
        float effectiveRadius = eraserRadius > 0f ? eraserRadius : radius;

        // PaintCanvas の消し機能を使用
        // playerId と color は無視（すべての色を消すため）
        canvas.EraseAt(position, effectiveRadius);
    }

    /// <summary>
    /// UIなどから参照される半径は eraserRadius を優先する。
    /// </summary>
    public override float GetRadius()
    {
        return eraserRadius > 0f ? eraserRadius : base.GetRadius();
    }
}

