using UnityEngine;

/// <summary>
/// 爆弾ブラシの実装
/// 一点を指定して、その周囲を一気に塗るためのブラシ。
/// （連続線ではなく「ドカン」と広がる塗りに向いている）
/// </summary>
[CreateAssetMenu(fileName = "BombBrush", menuName = "Game/Brushes/Bomb Brush")]
public class BombBrush : BrushStrategyBase
{
    [Header("Bomb Brush Settings")]
    [Tooltip("爆弾の有効半径（ピクセル単位）。0以下なら基底クラスの radius を使用します。")]
    public float bombRadius = 100f;

    [Tooltip("塗り強度に掛ける倍率（PaintCanvas 側のしきい値に影響）")]
    public float intensityMultiplier = 1.0f;

    /// <summary>
    /// 爆弾ブラシによる塗り処理。
    /// 線を引くのではなく、指定位置を中心に円形に塗る。
    /// 過去の軽い実装を参考に、1回のPaintAtWithRadius呼び出しのみで処理を最適化。
    /// </summary>
    public override void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity)
    {
        if (canvas == null) return;

        // 実際に使用する半径を決定
        float effectiveRadius = bombRadius > 0f ? bombRadius : radius;

        // 強度に倍率を掛ける（しきい値を超えやすくするため）
        float finalIntensity = intensity * intensityMultiplier;

        // PaintCanvas の円ブラシ機能をそのまま利用（1回の呼び出しのみで軽量）
        canvas.PaintAtWithRadius(position, playerId, finalIntensity, color, effectiveRadius);
    }

    /// <summary>
    /// UIなどから参照される半径は bombRadius を優先する。
    /// </summary>
    public override float GetRadius()
    {
        return bombRadius > 0f ? bombRadius : base.GetRadius();
    }
}


