using UnityEngine;

/// <summary>
/// ブラシタイプ（将来削除予定）
/// BrushStrategyBaseを使用してください。
/// </summary>
[System.Obsolete("BrushType enumは将来削除予定です。BrushStrategyBaseを使用してください。")]
public enum BrushType
{
    Pencil,  // 鉛筆（細い線、連続的な描画）
    Paint    // ペンキ（太い線、広範囲の塗りつぶし）- 将来的な拡張
}

