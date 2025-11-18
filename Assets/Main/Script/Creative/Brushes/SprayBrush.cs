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
    public int particleCount = 20;
    
    [Tooltip("各粒子の半径")]
    [Range(1f, 10f)]
    public float particleRadius = 2f;
    
    [Tooltip("拡散半径（0 = 基本radiusを使用）")]
    [Range(0f, 200f)]
    public float spreadRadius = 0f; // 0の場合はradiusを使用
    
    [Tooltip("密度分布（0 = 均等、1 = 中心に集中）")]
    [Range(0f, 1f)]
    public float density = 0.5f;
    
    public override void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity)
    {
        if (canvas == null) return;
        
        float effectiveSpreadRadius = spreadRadius > 0f ? spreadRadius : radius;
        
        for (int i = 0; i < particleCount; i++)
        {
            // ランダムな方向と距離を生成
            Vector2 offset = GetRandomOffset(effectiveSpreadRadius, density);
            Vector2 particlePosition = position + offset;
            
            // 各粒子を塗る
            canvas.PaintAtWithRadius(particlePosition, playerId, intensity, color, particleRadius);
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

