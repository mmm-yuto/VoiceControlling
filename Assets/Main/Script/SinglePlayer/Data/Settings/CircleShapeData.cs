using UnityEngine;

/// <summary>
/// 円形の形状
/// </summary>
[CreateAssetMenu(fileName = "CircleShape", menuName = "Game/SinglePlayer/Area Shape/Circle")]
public class CircleShapeData : AreaShapeData
{
    public override IAreaShape CreateShape()
    {
        return new CircleShape();
    }
}

/// <summary>
/// 円形の形状実装
/// </summary>
public class CircleShape : IAreaShape
{
    public bool IsPointInArea(Vector2 point, Vector2 center, float baseSize)
    {
        float radius = baseSize * 0.5f;
        return Vector2.Distance(point, center) <= radius;
    }
    
    public int CalculateAreaInPixels(float baseSize)
    {
        float radius = baseSize * 0.5f;
        return Mathf.RoundToInt(Mathf.PI * radius * radius);
    }
    
    public Rect GetBoundingBox(Vector2 center, float baseSize)
    {
        float radius = baseSize * 0.5f;
        return new Rect(center.x - radius, center.y - radius, baseSize, baseSize);
    }
}

