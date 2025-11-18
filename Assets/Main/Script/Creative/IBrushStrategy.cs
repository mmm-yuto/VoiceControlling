using UnityEngine;

/// <summary>
/// ブラシ戦略のインターフェース
/// Strategyパターンを使用して、様々なブラシタイプを実装可能にする
/// </summary>
public interface IBrushStrategy
{
    /// <summary>
    /// 指定位置に色を塗る
    /// </summary>
    /// <param name="canvas">塗りキャンバス</param>
    /// <param name="position">画面座標</param>
    /// <param name="playerId">プレイヤーID</param>
    /// <param name="color">色</param>
    /// <param name="intensity">塗り強度</param>
    void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity);
    
    /// <summary>
    /// ブラシの半径を取得
    /// </summary>
    float GetRadius();
    
    /// <summary>
    /// ブラシの表示名を取得
    /// </summary>
    string GetDisplayName();
    
    /// <summary>
    /// ブラシのアイコンを取得（オプション：UI用）
    /// </summary>
    Sprite GetIcon();
}

