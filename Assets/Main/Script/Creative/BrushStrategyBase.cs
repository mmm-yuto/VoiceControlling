using UnityEngine;

/// <summary>
/// ブラシ戦略の基底クラス
/// ScriptableObjectとして実装し、Inspectorで設定可能にする
/// 各ブラシタイプはこのクラスを継承して実装する
/// </summary>
[CreateAssetMenu(fileName = "Brush", menuName = "Game/Brushes/Brush")]
public abstract class BrushStrategyBase : ScriptableObject, IBrushStrategy
{
    [Header("Brush Properties")]
    [Tooltip("ブラシの半径（ピクセル単位）")]
    [Range(1f, 200f)] 
    public float radius = 10f;
    
    [Tooltip("ブラシの表示名")]
    public string displayName = "Brush";
    
    [Tooltip("UI用アイコン（オプション）")]
    public Sprite icon;
    
    /// <summary>
    /// 塗り処理を実装（各ブラシでオーバーライド）
    /// </summary>
    public abstract void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity);
    
    /// <summary>
    /// ブラシの半径を取得
    /// </summary>
    public virtual float GetRadius() => radius;
    
    /// <summary>
    /// ブラシの表示名を取得
    /// </summary>
    public virtual string GetDisplayName() => displayName;
    
    /// <summary>
    /// ブラシのアイコンを取得
    /// </summary>
    public virtual Sprite GetIcon() => icon;
}

