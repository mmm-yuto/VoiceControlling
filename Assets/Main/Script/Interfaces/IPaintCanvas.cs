using UnityEngine;

/// <summary>
/// 塗りシステムのインターフェース
/// Phase 1の基本機能とPhase 2のクリエイティブモードで使用
/// </summary>
public interface IPaintCanvas
{
    /// <summary>
    /// 指定位置に色を塗る
    /// </summary>
    /// <param name="position">画面座標（Screen座標系）</param>
    /// <param name="playerId">プレイヤーID（Phase 1では1で固定）</param>
    /// <param name="intensity">塗り強度（0-1）</param>
    /// <param name="color">塗り色</param>
    void PaintAt(Vector2 position, int playerId, float intensity, Color color);
    
    /// <summary>
    /// キャンバスをリセット（Phase 2のクリエイティブモードで使用）
    /// </summary>
    void ResetCanvas();
    
    /// <summary>
    /// 指定位置を消去（Phase 2のクリエイティブモードで使用）
    /// </summary>
    /// <param name="position">画面座標</param>
    /// <param name="radius">消去半径（ピクセル）</param>
    void EraseAt(Vector2 position, float radius);
    
    /// <summary>
    /// 現在のキャンバス状態を取得
    /// </summary>
    CanvasState GetState();
    
    /// <summary>
    /// 保存したキャンバス状態を復元
    /// </summary>
    /// <param name="state">復元する状態</param>
    void RestoreState(CanvasState state);
    
    /// <summary>
    /// 表示用のテクスチャを取得
    /// </summary>
    Texture2D GetTexture();
    
    /// <summary>
    /// 塗り完了時に発火するイベント
    /// </summary>
    event System.Action<Vector2, int, float> OnPaintCompleted;
}

