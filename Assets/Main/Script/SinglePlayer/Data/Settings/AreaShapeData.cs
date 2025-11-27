using UnityEngine;

// ============================================
// 形状システム（変更しやすい設計）
// ============================================

/// <summary>
/// 領域の形状を定義するインターフェース
/// 新しい形状を追加する際は、このインターフェースを実装する
/// </summary>
public interface IAreaShape
{
    /// <summary>
    /// 指定されたピクセルが領域内にあるかチェック
    /// </summary>
    bool IsPointInArea(Vector2 point, Vector2 center, float baseSize);
    
    /// <summary>
    /// 領域内の総ピクセル数を計算（近似値）
    /// </summary>
    int CalculateAreaInPixels(float baseSize);
    
    /// <summary>
    /// 領域のバウンディングボックスを取得（最適化用）
    /// </summary>
    Rect GetBoundingBox(Vector2 center, float baseSize);
}

/// <summary>
/// 形状の設定データ（Inspectorで設定可能）
/// </summary>
public abstract class AreaShapeData : ScriptableObject
{
    public abstract IAreaShape CreateShape();
    
    [Header("Visual Settings")]
    [Tooltip("視覚表現用のスプライト（オプション）")]
    public Sprite shapeSprite;
    
    [Tooltip("形状の色")]
    public Color shapeColor = Color.red;
}

