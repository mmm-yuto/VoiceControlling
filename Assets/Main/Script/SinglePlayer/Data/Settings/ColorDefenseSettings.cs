using UnityEngine;
using System.Collections.Generic;

// ============================================
// 形状システム（変更しやすい設計）
// ============================================

/// <summary>
/// 領域の形状を定義するインターフェース
/// 新しい形状を追加する際は、このインターフェースを実装する
/// </summary>
public interface IAreaShape
{
    /// <summary>
    /// 指定されたピクセルが領域内にあるかチェック
    /// </summary>
    bool IsPointInArea(Vector2 point, Vector2 center, float baseSize);
    
    /// <summary>
    /// 領域内の総ピクセル数を計算（近似値）
    /// </summary>
    int CalculateAreaInPixels(float baseSize);
    
    /// <summary>
    /// 領域のバウンディングボックスを取得（最適化用）
    /// </summary>
    Rect GetBoundingBox(Vector2 center, float baseSize);
}

/// <summary>
/// 形状の設定データ（Inspectorで設定可能）
/// </summary>
public abstract class AreaShapeData : ScriptableObject
{
    public abstract IAreaShape CreateShape();
    
    [Header("Visual Settings")]
    [Tooltip("視覚表現用のスプライト（オプション）")]
    public Sprite shapeSprite;
    
    [Tooltip("形状の色")]
    public Color shapeColor = Color.red;
}

// ============================================
// 具体的な形状実装例
// ============================================

/// <summary>
/// 円形の形状
/// </summary>
[CreateAssetMenu(fileName = "CircleShape", menuName = "Game/SinglePlayer/Area Shape/Circle")]
public class CircleShapeData : AreaShapeData
{
    public override IAreaShape CreateShape()
    {
        return new CircleShape();
    }
}

public class CircleShape : IAreaShape
{
    public bool IsPointInArea(Vector2 point, Vector2 center, float baseSize)
    {
        float radius = baseSize * 0.5f;
        return Vector2.Distance(point, center) <= radius;
    }
    
    public int CalculateAreaInPixels(float baseSize)
    {
        float radius = baseSize * 0.5f;
        return Mathf.RoundToInt(Mathf.PI * radius * radius);
    }
    
    public Rect GetBoundingBox(Vector2 center, float baseSize)
    {
        float radius = baseSize * 0.5f;
        return new Rect(center.x - radius, center.y - radius, baseSize, baseSize);
    }
}

/// <summary>
/// 正方形の形状
/// </summary>
[CreateAssetMenu(fileName = "SquareShape", menuName = "Game/SinglePlayer/Area Shape/Square")]
public class SquareShapeData : AreaShapeData
{
    public override IAreaShape CreateShape()
    {
        return new SquareShape();
    }
}

public class SquareShape : IAreaShape
{
    public bool IsPointInArea(Vector2 point, Vector2 center, float baseSize)
    {
        float halfSize = baseSize * 0.5f;
        return Mathf.Abs(point.x - center.x) <= halfSize &&
               Mathf.Abs(point.y - center.y) <= halfSize;
    }
    
    public int CalculateAreaInPixels(float baseSize)
    {
        return Mathf.RoundToInt(baseSize * baseSize);
    }
    
    public Rect GetBoundingBox(Vector2 center, float baseSize)
    {
        float halfSize = baseSize * 0.5f;
        return new Rect(center.x - halfSize, center.y - halfSize, baseSize, baseSize);
    }
}

/// <summary>
/// 長方形の形状
/// </summary>
[CreateAssetMenu(fileName = "RectangleShape", menuName = "Game/SinglePlayer/Area Shape/Rectangle")]
public class RectangleShapeData : AreaShapeData
{
    [Header("Rectangle Settings")]
    [Tooltip("幅の比率（baseSizeに対する）")]
    [Range(0.5f, 2f)]
    public float widthRatio = 1f;
    
    [Tooltip("高さの比率（baseSizeに対する）")]
    [Range(0.5f, 2f)]
    public float heightRatio = 1f;
    
    public override IAreaShape CreateShape()
    {
        return new RectangleShape(widthRatio, heightRatio);
    }
}

public class RectangleShape : IAreaShape
{
    private float widthRatio;
    private float heightRatio;
    
    public RectangleShape(float widthRatio, float heightRatio)
    {
        this.widthRatio = widthRatio;
        this.heightRatio = heightRatio;
    }
    
    public bool IsPointInArea(Vector2 point, Vector2 center, float baseSize)
    {
        float halfWidth = baseSize * widthRatio * 0.5f;
        float halfHeight = baseSize * heightRatio * 0.5f;
        return Mathf.Abs(point.x - center.x) <= halfWidth &&
               Mathf.Abs(point.y - center.y) <= halfHeight;
    }
    
    public int CalculateAreaInPixels(float baseSize)
    {
        return Mathf.RoundToInt(baseSize * widthRatio * baseSize * heightRatio);
    }
    
    public Rect GetBoundingBox(Vector2 center, float baseSize)
    {
        float halfWidth = baseSize * widthRatio * 0.5f;
        float halfHeight = baseSize * heightRatio * 0.5f;
        return new Rect(center.x - halfWidth, center.y - halfHeight, 
                       baseSize * widthRatio, baseSize * heightRatio);
    }
}

// ============================================
// ColorDefenseSettings（ScriptableObject設定）
// ============================================

/// <summary>
/// カラーディフェンスモードの全設定を管理
/// </summary>
[CreateAssetMenu(fileName = "ColorDefenseSettings", menuName = "Game/SinglePlayer/Modes/Color Defense Settings")]
public class ColorDefenseSettings : ScriptableObject
{
    [Header("Color Change Properties")]
    [Tooltip("色が変わる速度（倍率）")]
    [Range(0.1f, 5f)] 
    public float colorChangeSpeed = 1f;
    
    [Tooltip("1秒あたりの色変化率（0.0～1.0）")]
    [Range(0.1f, 1f)] 
    public float colorChangeRate = 0.5f;
    
    [Tooltip("変化する色（敵の色）")]
    public Color targetColor = Color.red;
    
    [Tooltip("色変化のアニメーションカーブ（時間経過による変化率）")]
    public AnimationCurve changeProgressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    
    [Header("Area Properties")]
    [Tooltip("画面上に同時に存在できる領域の最大数")]
    [Range(1, 20)] 
    public int maxAreasOnScreen = 5;
    
    [Tooltip("領域のサイズ（ピクセル）")]
    [Range(50f, 300f)] 
    public float areaSize = 100f;
    
    [Tooltip("領域の形状設定（ScriptableObject）")]
    public AreaShapeData areaShapeData;
    
    [Tooltip("新しい領域が出現する間隔（秒）")]
    [Range(1f, 10f)] 
    public float spawnInterval = 3f;
    
    [Tooltip("領域の出現位置のランダム性（0.0=完全ランダム、1.0=プレイヤーから離れた位置）")]
    [Range(0f, 1f)] 
    public float spawnAwayFromPlayer = 0.3f;
    
    [Header("Defense Properties")]
    [Tooltip("防げたと判定するために必要な塗り具合（0.0～1.0）")]
    [Range(0.5f, 1f)] 
    public float defenseThreshold = 0.9f;
    
    [Tooltip("色変化を完全に阻止するために必要な塗り具合（0.0～1.0）")]
    [Range(0.7f, 1f)] 
    public float fullDefenseThreshold = 0.95f;
    
    [Tooltip("プレイヤーの塗りが色変化を遅らせる効果（倍率）")]
    [Range(0f, 1f)] 
    public float paintSlowdownEffect = 0.5f;
    
    [Header("Score")]
    [Tooltip("領域を完全に防げた時のスコア")]
    public int scorePerDefendedArea = 50;
    
    [Tooltip("領域が完全に変色した時のペナルティ（負の値）")]
    public int penaltyPerChangedArea = -20;
    
    [Tooltip("部分的に防げた時のスコア（防げた割合に応じて）")]
    public int partialDefenseScoreMultiplier = 10;
    
    [Tooltip("連続で防げた時のコンボボーナス")]
    public int comboBonusPerDefense = 5;
    
    [Header("Difficulty Scaling")]
    [Tooltip("難易度上昇の設定方法")]
    public DifficultyScalingMode scalingMode = DifficultyScalingMode.TimeBased;
    
    [Header("Time-Based Difficulty (時間帯ごとの設定)")]
    [Tooltip("時間帯ごとの難易度設定（Inspectorで調整可能）")]
    public List<DifficultyPhase> difficultyPhases = new List<DifficultyPhase>();
    
    [Header("Curve-Based Difficulty (カーブベースの設定)")]
    [Tooltip("時間経過による難易度カーブ（scalingModeがCurveBasedの場合）")]
    public AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);
    
    [Tooltip("難易度が上がった時の色変化速度の倍率（scalingModeがCurveBasedの場合）")]
    [Range(1f, 3f)] 
    public float maxDifficultyMultiplier = 2f;
    
    [Tooltip("難易度が上がった時の出現間隔の短縮率（scalingModeがCurveBasedの場合）")]
    [Range(0.5f, 1f)] 
    public float minSpawnInterval = 1f;
}

/// <summary>
/// 難易度スケーリングモード
/// </summary>
public enum DifficultyScalingMode
{
    TimeBased,      // 時間帯ごとに設定（推奨：Inspectorで調整しやすい）
    CurveBased      // カーブで設定（滑らかな変化）
}

/// <summary>
/// 難易度フェーズ（時間帯ごとの設定）
/// </summary>
[System.Serializable]
public class DifficultyPhase
{
    [Header("Phase Settings")]
    [Tooltip("このフェーズの開始時間（秒）")]
    [Range(0f, 300f)]
    public float startTime = 0f;
    
    [Tooltip("このフェーズの終了時間（秒、0の場合は最後まで）")]
    [Range(0f, 300f)]
    public float endTime = 0f; // 0の場合は最後まで
    
    [Header("Spawn Settings")]
    [Tooltip("このフェーズでの領域の出現間隔（秒）")]
    [Range(0.5f, 10f)]
    public float spawnInterval = 3f;
    
    [Tooltip("このフェーズでの同時存在可能な領域の最大数")]
    [Range(1, 20)]
    public int maxAreasOnScreen = 5;
    
    [Header("Color Change Settings")]
    [Tooltip("このフェーズでの色変化速度（倍率）")]
    [Range(0.1f, 5f)]
    public float colorChangeSpeed = 1f;
    
    [Tooltip("このフェーズでの1秒あたりの色変化率")]
    [Range(0.1f, 1f)]
    public float colorChangeRate = 0.5f;
    
    [Header("Area Size (Optional)")]
    [Tooltip("このフェーズでの領域のサイズ（0の場合はデフォルトサイズを使用）")]
    [Range(0f, 300f)]
    public float areaSize = 0f; // 0の場合はデフォルトサイズ
    
    [Tooltip("このフェーズの説明（Inspectorでの識別用）")]
    public string phaseName = "Phase";
    
    /// <summary>
    /// 指定時間がこのフェーズ内かどうか
    /// </summary>
    public bool IsInPhase(float gameTime)
    {
        if (endTime <= 0f)
        {
            return gameTime >= startTime;
        }
        return gameTime >= startTime && gameTime < endTime;
    }
}

