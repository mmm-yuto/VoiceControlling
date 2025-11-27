using UnityEngine;

/// <summary>
/// 正方形の形状
/// </summary>
[CreateAssetMenu(fileName = "SquareShape", menuName = "Game/SinglePlayer/Area Shape/Square")]
public class SquareShapeData : AreaShapeData
{
    public override IAreaShape CreateShape()
    {
        return new SquareShape();
    }
}

/// <summary>
/// 正方形の形状実装
/// </summary>
public class SquareShape : IAreaShape
{
    public bool IsPointInArea(Vector2 point, Vector2 center, float baseSize)
    {
        float halfSize = baseSize * 0.5f;
        return Mathf.Abs(point.x - center.x) <= halfSize &&
               Mathf.Abs(point.y - center.y) <= halfSize;
    }
    
    public int CalculateAreaInPixels(float baseSize)
    {
        return Mathf.RoundToInt(baseSize * baseSize);
    }
    
    public Rect GetBoundingBox(Vector2 center, float baseSize)
    {
        float halfSize = baseSize * 0.5f;
        return new Rect(center.x - halfSize, center.y - halfSize, baseSize, baseSize);
    }
}

