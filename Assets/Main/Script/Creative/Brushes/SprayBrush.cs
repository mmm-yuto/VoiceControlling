using UnityEngine;

/// <summary>
/// スプレーブラシの実装
/// 粒子が拡散するようなエアブラシ風の塗り方
/// </summary>
[CreateAssetMenu(fileName = "SprayBrush", menuName = "Game/Brushes/Spray Brush")]
public class SprayBrush : BrushStrategyBase
{
    [Header("Spray Properties")]
    [Tooltip("スプレーの粒子数")]
    [Range(5, 100)]
    public int particleCount = 15;
    
    [Tooltip("各粒子の半径")]
    [Range(1f, 10f)]
    public float particleRadius = 2f;
    
    [Tooltip("拡散半径（0 = 基本radiusを使用）")]
    [Range(0f, 200f)]
    public float spreadRadius = 0f; // 0の場合はradiusを使用
    
    [Tooltip("密度分布（0 = 均等、1 = 中心に集中）")]
    [Range(0f, 1f)]
    public float density = 0.5f;
    
    [Tooltip("距離による半径の減衰（0 = 減衰なし、1 = 中心から離れるほど半径が小さくなる）")]
    [Range(0f, 1f)]
    public float radiusFalloff = 0.5f;
    
    [Tooltip("スプレーの更新頻度（フレーム単位、1=毎フレーム、2=2フレームに1回）")]
    [Range(1, 50)]
    public int sprayUpdateFrequency = 2;
    
    [Tooltip("最小粒子半径の閾値（この値以下の粒子はスキップして処理負荷を削減）")]
    [Range(0.5f, 2f)]
    public float minParticleRadiusThreshold = 0.8f;
    
    // フレームカウント（各インスタンスごとに管理するため、静的ではない）
    private int sprayFrameCount = 0;
    
    public override void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity)
    {
        if (canvas == null) return;
        
        // 更新頻度チェック（処理を間引く）
        sprayFrameCount++;
        if (sprayFrameCount % sprayUpdateFrequency != 0)
        {
            return; // 処理をスキップ
        }
        
        float effectiveSpreadRadius = spreadRadius > 0f ? spreadRadius : radius;
        
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
            
            // 各粒子を塗る
            canvas.PaintAtWithRadius(particlePosition, playerId, intensity, color, effectiveParticleRadius);
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
}

