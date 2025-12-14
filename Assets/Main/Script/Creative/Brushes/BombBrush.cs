using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 爆弾ブラシの実装
/// 一点を指定して、その周囲を一気に塗るためのブラシ。
/// ペンキ爆弾が爆発しているような散らばり効果を実現。
/// </summary>
[CreateAssetMenu(fileName = "BombBrush", menuName = "Game/Brushes/Bomb Brush")]
public class BombBrush : BrushStrategyBase
{
    [Header("Bomb Brush Settings")]
    [Tooltip("爆弾の有効半径（ピクセル単位）。0以下なら基底クラスの radius を使用します。")]
    public float bombRadius = 100f;

    [Tooltip("塗り強度に掛ける倍率（PaintCanvas 側のしきい値に影響）")]
    public float intensityMultiplier = 1.0f;

    [Header("Center Circle")]
    [Tooltip("中心の円形の半径（0 = 円形塗りをスキップ、パーティクルのみ）")]
    [Range(0f, 200f)]
    public float centerCircleRadius = 50f;

    [Header("Explosion Properties")]
    [Tooltip("爆発時のパーティクル数")]
    [Range(50, 1000)]
    public int particleCount = 100;
    
    [Tooltip("各パーティクルの半径")]
    [Range(1f, 50f)]
    public float particleRadius = 3f;
    
    [Tooltip("拡散半径（0 = bombRadiusを使用）")]
    [Range(0f, 200f)]
    public float spreadRadius = 0f; // 0の場合はbombRadiusを使用
    
    [Tooltip("密度分布（0 = 均等、1 = 中心に集中）")]
    [Range(0f, 1f)]
    public float density = 0.3f;
    
    [Tooltip("距離による半径の減衰（0 = 減衰なし、1 = 中心から離れるほど半径が小さくなる）")]
    [Range(0f, 1f)]
    public float radiusFalloff = 0.5f;
    
    [Tooltip("最小粒子半径の閾値（この値以下の粒子はスキップして処理負荷を削減）")]
    [Range(0.5f, 2f)]
    public float minParticleRadiusThreshold = 0.8f;

    /// <summary>
    /// 爆弾ブラシによる塗り処理。
    /// 中心に円形の塗りを配置し、その外側にパーティクルが飛び散る爆発の見た目を実現。
    /// 全ての円形領域を一度に塗ることで、パフォーマンスを最適化。
    /// </summary>
    public override void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity)
    {
        if (canvas == null) return;

        // 実際に使用する拡散半径を決定
        float effectiveSpreadRadius = spreadRadius > 0f ? spreadRadius : (bombRadius > 0f ? bombRadius : radius);

        // 強度に倍率を掛ける（しきい値を超えやすくするため）
        float finalIntensity = intensity * intensityMultiplier;

        // 全ての円形領域をリストに追加
        List<PaintCanvas.CirclePaintData> circles = new List<PaintCanvas.CirclePaintData>();

        // 中心の円形を追加（centerCircleRadius > 0の場合）
        if (centerCircleRadius > 0f)
        {
            circles.Add(new PaintCanvas.CirclePaintData(position, centerCircleRadius));
        }

        // centerCircleRadiusがeffectiveSpreadRadiusより大きい場合は警告
        if (centerCircleRadius >= effectiveSpreadRadius)
        {
            Debug.LogWarning($"BombBrush: centerCircleRadius ({centerCircleRadius}) が effectiveSpreadRadius ({effectiveSpreadRadius}) 以上です。パーティクルが生成されません。");
            // 中心円形だけ塗る
            if (circles.Count > 0)
            {
                canvas.PaintMultipleCircles(circles, playerId, finalIntensity, color);
            }
            return;
        }

        // パーティクルを生成してリストに追加（centerCircleRadiusより外側にのみ生成）
        float minParticleRadius = centerCircleRadius; // パーティクルの最小半径（中心円形の外側）
        for (int i = 0; i < particleCount; i++)
        {
            // ランダムな方向と距離を生成（minParticleRadiusからeffectiveSpreadRadiusの範囲）
            Vector2 offset = GetRandomOffset(minParticleRadius, effectiveSpreadRadius, density);
            Vector2 particlePosition = position + offset;
            
            // 中心からの距離を計算（パーティクル生成範囲を基準に正規化）
            float distanceFromCenter = offset.magnitude;
            float particleRange = effectiveSpreadRadius - minParticleRadius;
            float normalizedDistance = particleRange > 0f ? ((distanceFromCenter - minParticleRadius) / particleRange) : 0f;
            
            // 距離に応じて粒子の半径を減衰させる
            // 中心円形の外側: 100%、境界: (1 - radiusFalloff)%
            float effectiveParticleRadius = particleRadius * (1f - normalizedDistance * radiusFalloff);
            
            // 最小半径を確保（0にならないように）
            effectiveParticleRadius = Mathf.Max(effectiveParticleRadius, 0.5f);
            
            // 小さすぎる粒子はスキップ（処理負荷を削減）
            if (effectiveParticleRadius < minParticleRadiusThreshold)
            {
                continue; // この粒子をスキップ
            }
            
            // パーティクルをリストに追加
            circles.Add(new PaintCanvas.CirclePaintData(particlePosition, effectiveParticleRadius));
        }
        
        // 全ての円形領域を一度に塗る（1回の呼び出し、テクスチャ更新も1回だけ）
        if (circles.Count > 0)
        {
            canvas.PaintMultipleCircles(circles, playerId, finalIntensity, color);
        }
    }

    /// <summary>
    /// ランダムなオフセットを生成（密度分布を考慮、最小半径から最大半径の範囲）
    /// </summary>
    private Vector2 GetRandomOffset(float minRadius, float maxRadius, float densityFactor)
    {
        // 範囲を計算
        float range = maxRadius - minRadius;
        if (range <= 0f)
        {
            // 範囲が無効な場合は、最小半径の位置を返す
            float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(fallbackAngle) * minRadius, Mathf.Sin(fallbackAngle) * minRadius);
        }

        // 密度に応じてランダム分布を調整
        // 0から1の範囲で生成し、minRadiusからmaxRadiusにマッピング
        float normalizedDistance = Random.Range(0f, 1f);
        if (densityFactor > 0f)
        {
            // 最小半径に近いほど密度が高い（平方根で調整）
            // densityFactorが大きいほど、最小半径により集中する
            normalizedDistance = Mathf.Pow(Random.Range(0f, 1f), 1f + densityFactor);
        }
        
        // 正規化された距離を実際の距離に変換（minRadiusからmaxRadiusの範囲）
        float distance = minRadius + normalizedDistance * range;
        
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
    }

    /// <summary>
    /// UIなどから参照される半径は bombRadius を優先する。
    /// </summary>
    public override float GetRadius()
    {
        return bombRadius > 0f ? bombRadius : base.GetRadius();
    }
}


