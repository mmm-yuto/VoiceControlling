using UnityEngine;

/// <summary>
/// 長方形の形状
/// </summary>
[CreateAssetMenu(fileName = "RectangleShape", menuName = "Game/SinglePlayer/Area Shape/Rectangle")]
public class RectangleShapeData : AreaShapeData
{
    [Header("Rectangle Settings")]
    [Tooltip("幅の比率（baseSizeに対する）")]
    [Range(0.5f, 2f)]
    public float widthRatio = 1f;
    
    [Tooltip("高さの比率（baseSizeに対する）")]
    [Range(0.5f, 2f)]
    public float heightRatio = 1f;
    
    public override IAreaShape CreateShape()
    {
        return new RectangleShape(widthRatio, heightRatio);
    }
}

/// <summary>
/// 長方形の形状実装
/// </summary>
public class RectangleShape : IAreaShape
{
    private float widthRatio;
    private float heightRatio;
    
    public RectangleShape(float widthRatio, float heightRatio)
    {
        this.widthRatio = widthRatio;
        this.heightRatio = heightRatio;
    }
    
    public bool IsPointInArea(Vector2 point, Vector2 center, float baseSize)
    {
        float halfWidth = baseSize * widthRatio * 0.5f;
        float halfHeight = baseSize * heightRatio * 0.5f;
        return Mathf.Abs(point.x - center.x) <= halfWidth &&
               Mathf.Abs(point.y - center.y) <= halfHeight;
    }
    
    public int CalculateAreaInPixels(float baseSize)
    {
        return Mathf.RoundToInt(baseSize * widthRatio * baseSize * heightRatio);
    }
    
    public Rect GetBoundingBox(Vector2 center, float baseSize)
    {
        float halfWidth = baseSize * widthRatio * 0.5f;
        float halfHeight = baseSize * heightRatio * 0.5f;
        return new Rect(center.x - halfWidth, center.y - halfHeight, 
                       baseSize * widthRatio, baseSize * heightRatio);
    }
}

