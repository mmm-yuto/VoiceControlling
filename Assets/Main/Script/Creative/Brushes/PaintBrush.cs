using UnityEngine;

/// <summary>
/// ペンキブラシの実装
/// PaintBrushとSprayBrushの中間的なBrush
/// 中心部分はPaintBrushのように固定半径で塗り、その周りにSprayBrushのように粒子を散布する
/// </summary>
[CreateAssetMenu(fileName = "PaintBrush", menuName = "Game/Brushes/Paint Brush")]
public class PaintBrush : BrushStrategyBase
{
    [Header("Core Paint Properties")]
    [Tooltip("中心部分の半径（PaintBrush風の固定半径）")]
    [Range(1f, 100f)]
    public float coreRadius = 10f;
    
    [Header("Spray Properties")]
    [Tooltip("散布粒子数")]
    [Range(5, 50)]
    public int sprayParticleCount = 15;
    
    [Tooltip("各散布粒子の半径")]
    [Range(1f, 10f)]
    public float sprayParticleRadius = 2f;
    
    [Tooltip("散布範囲（中心半径からの追加距離）")]
    [Range(0f, 100f)]
    public float spraySpreadRadius = 20f;
    
    [Tooltip("散布密度（0 = 均等、1 = 中心に近いほど密集）")]
    [Range(0f, 1f)]
    public float sprayDensity = 0.3f;
    
    [Tooltip("散布の更新頻度（フレーム単位、1=毎フレーム）")]
    [Range(1, 10)]
    public int sprayUpdateFrequency = 2;
    
    [Tooltip("最小粒子半径の閾値（この値以下の粒子はスキップして処理負荷を削減）")]
    [Range(0.5f, 2f)]
    public float minParticleRadiusThreshold = 0.8f;
    
    // フレームカウント（各インスタンスごとに管理するため、静的ではない）
    private int sprayFrameCount = 0;
    
    public override void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity)
    {
        if (canvas == null) return;
        
        // 中心部分の塗り（PaintBrush風の固定半径で塗る）
        canvas.PaintAtWithRadius(position, playerId, intensity, color, coreRadius);
        
        // 周辺散布の塗り（SprayBrush風の粒子散布）
        // 更新頻度チェック（処理を間引く）
        sprayFrameCount++;
        if (sprayFrameCount % sprayUpdateFrequency != 0)
        {
            return; // 散布処理をスキップ（中心部分は既に塗られている）
        }
        
        // 散布範囲: coreRadiusからcoreRadius + spraySpreadRadiusの間
        float minSpreadDistance = coreRadius;
        float maxSpreadDistance = coreRadius + spraySpreadRadius;
        
        for (int i = 0; i < sprayParticleCount; i++)
        {
            // ランダムな方向と距離を生成（中心半径から外側に向かって散布）
            Vector2 offset = GetSprayOffset(minSpreadDistance, maxSpreadDistance, sprayDensity);
            Vector2 particlePosition = position + offset;
            
            // 各粒子を塗る
            canvas.PaintAtWithRadius(particlePosition, playerId, intensity, color, sprayParticleRadius);
        }
    }
    
    /// <summary>
    /// 散布用のランダムなオフセットを生成（中心半径から外側に向かって散布）
    /// </summary>
    /// <param name="minDistance">最小距離（中心半径）</param>
    /// <param name="maxDistance">最大距離（中心半径 + 散布範囲）</param>
    /// <param name="densityFactor">密度分布（0 = 均等、1 = 中心に近いほど密集）</param>
    /// <returns>ランダムなオフセットベクトル</returns>
    private Vector2 GetSprayOffset(float minDistance, float maxDistance, float densityFactor)
    {
        // 距離を計算（最小距離から最大距離の間）
        float distance = minDistance + (maxDistance - minDistance) * Random.Range(0f, 1f);
        
        // 密度分布を適用（中心に近いほど密集）
        if (densityFactor > 0f)
        {
            // densityFactorが大きいほど、中心（minDistance）により集中する
            float randomValue = Random.Range(0f, 1f);
            distance = minDistance + (maxDistance - minDistance) * Mathf.Pow(randomValue, 1f + densityFactor);
        }
        
        // ランダムな角度（360度全方向）
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
    }
    
    /// <summary>
    /// ブラシの有効半径を取得（UI表示用）
    /// </summary>
    public override float GetRadius()
    {
        // 有効半径 = 中心半径 + 散布範囲
        return coreRadius + spraySpreadRadius;
    }
}
