using UnityEngine;

/// <summary>
/// 爆弾ブラシの実装
/// 一点を指定して、その周囲を一気に塗るためのブラシ。
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
    
    [Header("Gradient Paint Settings")]
    [Tooltip("グラデーション塗りの粒子数（中心から遠くなるにつれて塗りが少なくなる）")]
    [Range(20, 200)]
    public int particleCount = 80;
    
    [Tooltip("各粒子の半径")]
    [Range(1f, 10f)]
    public float particleRadius = 3f;
    
    [Tooltip("密度分布（0 = 均等、1 = 中心に集中）")]
    [Range(0f, 1f)]
    public float density = 0.7f;
    
    [Tooltip("距離による半径の減衰（0 = 減衰なし、1 = 中心から離れるほど半径が小さくなる）")]
    [Range(0f, 1f)]
    public float radiusFalloff = 0.6f;
    
    [Tooltip("距離による強度の減衰（0 = 減衰なし、1 = 中心から離れるほど強度が小さくなる）")]
    [Range(0f, 1f)]
    public float intensityFalloff = 0.5f;
    
    [Header("Splatter Settings")]
    [Tooltip("スプラッターの数")]
    [Range(5, 30)]
    public int splatterCount = 15;
    
    [Tooltip("スプラッターのサイズ（基本半径に対する倍率）")]
    [Range(0.1f, 0.5f)]
    public float splatterSizeRatio = 0.2f;
    
    [Tooltip("スプラッターの最大飛距離（基本半径に対する倍率）")]
    [Range(0.5f, 1.5f)]
    public float splatterDistanceRatio = 1.2f;
    
    [Tooltip("スプラッターの強度（基本強度に対する倍率）")]
    [Range(0.5f, 1.0f)]
    public float splatterIntensityRatio = 0.8f;

    /// <summary>
    /// 爆弾ブラシによる塗り処理。
    /// 線を引くのではなく、指定位置を中心にグラデーション状に塗る。
    /// 中心から遠くなるにつれて塗りが少なくなる（SprayBrushと同様の塗り方）。
    /// 基本のグラデーション塗りに加えて、ペンキが飛び散るスプラッターエフェクトを追加。
    /// </summary>
    public override void Paint(PaintCanvas canvas, Vector2 position, int playerId, Color color, float intensity)
    {
        if (canvas == null) return;

        // 実際に使用する半径を決定
        float effectiveRadius = bombRadius > 0f ? bombRadius : radius;

        // 強度に倍率を掛ける（しきい値を超えやすくするため）
        float finalIntensity = intensity * intensityMultiplier;

        // 1. グラデーション塗りを実行（中心から遠くなるにつれて塗りが少なくなる）
        PaintGradient(canvas, position, playerId, color, finalIntensity, effectiveRadius);
        
        // 2. スプラッターエフェクトを追加
        PaintSplatters(canvas, position, playerId, color, finalIntensity, effectiveRadius);
    }
    
    /// <summary>
    /// グラデーション塗りを実行（中心から遠くなるにつれて塗りが少なくなる）
    /// </summary>
    private void PaintGradient(PaintCanvas canvas, Vector2 centerPosition, int playerId, Color color, float baseIntensity, float maxRadius)
    {
        if (particleCount <= 0) return;
        
        for (int i = 0; i < particleCount; i++)
        {
            // ランダムな方向と距離を生成（密度分布を考慮）
            Vector2 offset = GetRandomOffset(maxRadius, density);
            Vector2 particlePosition = centerPosition + offset;
            
            // 中心からの距離を計算
            float distanceFromCenter = offset.magnitude;
            float normalizedDistance = maxRadius > 0f ? (distanceFromCenter / maxRadius) : 0f;
            
            // 距離に応じて粒子の半径を減衰させる
            // 中心: 100%、境界: (1 - radiusFalloff)%
            float effectiveParticleRadius = particleRadius * (1f - normalizedDistance * radiusFalloff);
            
            // 最小半径を確保（0にならないように）
            effectiveParticleRadius = Mathf.Max(effectiveParticleRadius, 0.5f);
            
            // 距離に応じて強度を減衰させる
            float effectiveIntensity = baseIntensity * (1f - normalizedDistance * intensityFalloff);
            
            // 各粒子を塗る
            canvas.PaintAtWithRadius(particlePosition, playerId, effectiveIntensity, color, effectiveParticleRadius);
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
    /// スプラッターエフェクトを描画（ペンキが飛び散る様子を表現）
    /// </summary>
    private void PaintSplatters(PaintCanvas canvas, Vector2 centerPosition, int playerId, Color color, float baseIntensity, float baseRadius)
    {
        if (splatterCount <= 0) return;
        
        // スプラッターの強度を計算
        float splatterIntensity = baseIntensity * splatterIntensityRatio;
        
        // 各スプラッターを生成
        for (int i = 0; i < splatterCount; i++)
        {
            // ランダムな方向を決定（0-360度）
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            
            // ランダムな距離を決定（基本半径の50%-120%の範囲、splatterDistanceRatioで調整）
            float minDistance = baseRadius * 0.5f;
            float maxDistance = baseRadius * splatterDistanceRatio;
            float distance = Random.Range(minDistance, maxDistance);
            
            // スプラッターの位置を計算（中心 + 方向 × 距離）
            Vector2 splatterPosition = centerPosition + new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );
            
            // スプラッターのサイズをランダムに決定（基本半径の10%-30%の範囲、splatterSizeRatioで調整）
            float minSize = baseRadius * 0.1f;
            float maxSize = baseRadius * splatterSizeRatio;
            float splatterSize = Random.Range(minSize, maxSize);
            
            // スプラッターを小さな円形として塗る
            canvas.PaintAtWithRadius(splatterPosition, playerId, splatterIntensity, color, splatterSize);
        }
    }

    /// <summary>
    /// UIなどから参照される半径は bombRadius を優先する。
    /// </summary>
    public override float GetRadius()
    {
        return bombRadius > 0f ? bombRadius : base.GetRadius();
    }
}


