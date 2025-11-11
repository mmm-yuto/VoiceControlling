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
    void PaintAt(Vector2 position, int playerId, float intensity);
    
    /// <summary>
    /// キャンバスをリセット（Phase 2のクリエイティブモードで使用）
    /// </summary>
    void ResetCanvas();
    
    /// <summary>
    /// 塗り完了時に発火するイベント
    /// </summary>
    event System.Action<Vector2, int, float> OnPaintCompleted;
}

