using UnityEngine;

/// <summary>
/// 爆弾ブラシの実装
/// 一点を指定して、その周囲を一気に塗るためのブラシ。
/// SprayBrushと同じ粒子拡散方式で散らばった演出を実現。
/// （連続線ではなく「ドカン」と広がる塗りに向いている）
/// </summary>
[CreateAssetMenu(fileName = "BombBrush", menuName = "Game/Brushes/Bomb Brush")]
public class BombBrush : BrushStrategyBase
{
    [Header("Bomb Brush Settings")]
    [Tooltip("爆弾の有効半径（ピクセル単位）。0以下なら基底クラスの radius を使用します。")]
    public float bombRadius = 100f;

    [Tooltip("塗り強度に掛ける倍率（PaintCanvas 側のしきい値に影響）")]
    public float intensityMultiplier = 1.0f;
    
    [Header("Spray Properties")]
    [Tooltip("スプレーの粒子数")]
    [Range(5, 100)]
    public int particleCount = 15;
    
    [Tooltip("各粒子の半径")]
    [Range(1f, 10f)]
    public float particleRadius = 2f;
    
    [Tooltip("密度分布（0 = 均等、1 = 中心に集中）")]
    [Range(0f, 1f)]
    public float density = 0.5f;
    
    [Tooltip("距離による半径の減衰（0 = 減衰なし、1 = 中心から離れるほど半径が小さくなる）")]
    [Range(0f, 1f)]
    public float radiusFalloff = 0.5f;
    
    [Tooltip("最小粒子半径の閾値（この値以下の粒子はスキップして処理負荷を削減）")]
    [Range(0.5f, 2f)]
    public float minParticleRadiusThreshold = 0.8f;

    /// <summary>
    /// 爆弾ブラシによる塗り処理。
    /// SprayBrushと同じ粒子拡散方式で散らばった演出を実現。
    /// 爆発の瞬間のみ呼ばれるため、更新頻度チェックは不要。
    /// </summary>
    public override void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity)
    {
        if (canvas == null) return;
        
        // 強度に倍率を掛ける（しきい値を超えやすくするため）
        float finalIntensity = intensity * intensityMultiplier;
        
        // 実際に使用する拡散半径を決定
        float effectiveSpreadRadius = bombRadius > 0f ? bombRadius : radius;
        
        for (int i = 0; i < particleCount; i++)
        {
            // ランダムな方向と距離を生成
            Vector2 offset = GetRandomOffset(effectiveSpreadRadius, density);
            Vector2 particlePosition = position + offset;
            
            // 中心からの距離を計算
            float distanceFromCenter = offset.magnitude;
            float normalizedDistance = effectiveSpreadRadius > 0f ? (distanceFromCenter / effectiveSpreadRadius) : 0f;
            
            // 距離に応じて粒子の半径を減衰させる
            // 中心: 100%、境界: (1 - radiusFalloff)%
            float effectiveParticleRadius = particleRadius * (1f - normalizedDistance * radiusFalloff);
            
            // 最小半径を確保（0にならないように）
            effectiveParticleRadius = Mathf.Max(effectiveParticleRadius, 0.5f);
            
            // 小さすぎる粒子はスキップ（処理負荷を削減）
            if (effectiveParticleRadius < minParticleRadiusThreshold)
            {
                continue; // この粒子をスキップ
            }
            
            // 各粒子を塗る（更新頻度チェックをスキップして一度に全て塗る）
            canvas.PaintAtWithRadiusForced(particlePosition, playerId, finalIntensity, color, effectiveParticleRadius);
        }
    }
    
    /// <summary>
    /// ランダムなオフセットを生成（密度分布を考慮）
    /// </summary>
    private Vector2 GetRandomOffset(float maxRadius, float densityFactor)
    {
        // 密度に応じてランダム分布を調整
        float distance = maxRadius * Random.Range(0f, 1f);
        if (densityFactor > 0f)
        {
            // 中心に近いほど密度が高い（平方根で調整）
            // densityFactorが大きいほど、中心により集中する
            distance = maxRadius * Mathf.Pow(Random.Range(0f, 1f), 1f + densityFactor);
        }
        
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


