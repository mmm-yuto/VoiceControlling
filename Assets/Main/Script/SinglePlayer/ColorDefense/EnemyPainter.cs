using UnityEngine;

/// <summary>
/// キャンバス上を移動しながら敵色を塗るペン
/// ColorDefenseMode から更新される軽量クラス（MonoBehaviourではない）
/// </summary>
public class EnemyPainter
{
    private readonly PaintCanvas canvas;
    private readonly ColorDefenseSettings settings;
    private readonly BrushStrategyBase brush;  // 使用するブラシ

    // 移動関連
    private Vector2 currentScreenPos;
    private Vector2 targetScreenPos;
    private float timeUntilNextTarget;

    // パラメータ（設定から取得）
    private readonly float moveSpeed;          // 画面座標系での移動速度（px/sec想定）
    private readonly float strokeInterval;     // 次の目標点を決める間隔（秒）
    private readonly float paintRadius;        // ペン先の半径（画面座標系）

    // 線をなめらかにするための状態
    private Vector2 lastPaintScreenPos;
    private bool hasLastPaintPos = false;

    public EnemyPainter(PaintCanvas canvas, ColorDefenseSettings settings, BrushStrategyBase brush = null)
    {
        this.canvas = canvas;
        this.settings = settings;
        this.brush = brush;

        if (canvas == null || settings == null)
        {
            Debug.LogWarning("EnemyPainter: canvas または settings が null です");
            return;
        }

        moveSpeed = settings.enemyMoveSpeed;
        strokeInterval = settings.enemyStrokeInterval;
        
        // ブラシが指定されている場合はブラシの半径を使用、そうでない場合は設定から取得
        if (brush != null)
        {
            paintRadius = brush.GetRadius();
        }
        else
        {
            paintRadius = settings.enemyPaintRadius;
        }

        // 初期位置と目標位置をランダムに設定
        currentScreenPos = GetRandomScreenPosition();
        targetScreenPos = GetRandomScreenPosition();
        timeUntilNextTarget = strokeInterval;
    }

    /// <summary>
    /// 毎フレームの更新処理
    /// </summary>
    public void Update(float deltaTime)
    {
        if (canvas == null || settings == null)
        {
            return;
        }

        // 目標点の更新タイミング
        timeUntilNextTarget -= deltaTime;
        if (timeUntilNextTarget <= 0f || Vector2.Distance(currentScreenPos, targetScreenPos) < 1f)
        {
            targetScreenPos = GetRandomScreenPositionNear(currentScreenPos);
            timeUntilNextTarget = strokeInterval;
        }

        // 目標点に向かって移動
        Vector2 toTarget = targetScreenPos - currentScreenPos;
        float distanceToTarget = toTarget.magnitude;
        if (distanceToTarget > 0.01f)
        {
            float maxMove = moveSpeed * deltaTime;
            Vector2 move = toTarget.normalized * Mathf.Min(maxMove, distanceToTarget);
            Vector2 newPos = currentScreenPos + move;

            // 線をなめらかに描く
            PaintAlongSegment(currentScreenPos, newPos);

            currentScreenPos = newPos;
        }
        else
        {
            // ほぼ到達している場合は、次のターゲットを早めに設定
            timeUntilNextTarget = 0f;
        }
    }

    /// <summary>
    /// BattleSettingsから敵色を取得
    /// </summary>
    private Color GetEnemyColor()
    {
        if (BattleSettings.Instance != null && BattleSettings.Instance.Current != null)
        {
            return BattleSettings.Instance.Current.cpuColor;
        }
        // フォールバック: 既存のsettings.targetColorを使用
        if (settings != null)
        {
            return settings.targetColor;
        }
        return Color.red; // デフォルト値
    }

    /// <summary>
    /// 2点間を補間して連続線を描画
    /// </summary>
    private void PaintAlongSegment(Vector2 startPos, Vector2 endPos)
    {
        if (canvas == null)
        {
            return;
        }

        Color enemyColor = GetEnemyColor();

        // ブラシ半径に基づいてステップ数を決定
        float effectiveRadius = Mathf.Max(paintRadius, 1f);
        float distance = Vector2.Distance(startPos, endPos);

        if (!hasLastPaintPos)
        {
            lastPaintScreenPos = startPos;
            hasLastPaintPos = true;
        }

        // 前回描画位置から今回終点までを補間
        Vector2 segmentStart = lastPaintScreenPos;
        Vector2 segmentEnd = endPos;
        float segmentDistance = Vector2.Distance(segmentStart, segmentEnd);

        // ブラシが指定されている場合はブラシを使用、そうでない場合は従来の方法を使用
        if (brush != null)
        {
            // ブラシを使用して塗る
            if (segmentDistance < effectiveRadius * 0.25f)
            {
                // ほとんど動いていない場合は終点だけ塗る
                brush.Paint(canvas, segmentEnd, -1, enemyColor, 1f);
                lastPaintScreenPos = segmentEnd;
                return;
            }

            int steps = Mathf.Max(1, Mathf.CeilToInt(segmentDistance / (effectiveRadius * 0.5f)));
            const int maxSteps = 20;
            steps = Mathf.Min(steps, maxSteps);

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 pos = Vector2.Lerp(segmentStart, segmentEnd, t);
                brush.Paint(canvas, pos, -1, enemyColor, 1f);
            }
        }
        else
        {
            // 従来の方法（PaintAtWithRadiusを使用）
            if (segmentDistance < effectiveRadius * 0.25f)
            {
                // ほとんど動いていない場合は終点だけ塗る
                canvas.PaintAtWithRadius(segmentEnd, -1, 1f, enemyColor, paintRadius);
                lastPaintScreenPos = segmentEnd;
                return;
            }

            int steps = Mathf.Max(1, Mathf.CeilToInt(segmentDistance / (effectiveRadius * 0.5f)));
            const int maxSteps = 20;
            steps = Mathf.Min(steps, maxSteps);

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 pos = Vector2.Lerp(segmentStart, segmentEnd, t);
                canvas.PaintAtWithRadius(pos, -1, 1f, enemyColor, paintRadius);
            }
        }

        lastPaintScreenPos = segmentEnd;
    }

    /// <summary>
    /// 画面全体からランダムな位置を取得
    /// </summary>
    private Vector2 GetRandomScreenPosition()
    {
        float x = Random.Range(0f, Screen.width);
        float y = Random.Range(0f, Screen.height);
        return new Vector2(x, y);
    }

    /// <summary>
    /// 現在位置付近のランダムな位置を取得（急激な方向転換を避ける）
    /// </summary>
    private Vector2 GetRandomScreenPositionNear(Vector2 basePos)
    {
        // 半径の数倍の範囲でランダムに揺らす
        float range = Mathf.Max(paintRadius * 5f, 50f);
        Vector2 offset = Random.insideUnitCircle * range;
        Vector2 pos = basePos + offset;

        // 画面内にクランプ
        pos.x = Mathf.Clamp(pos.x, 0f, Screen.width);
        pos.y = Mathf.Clamp(pos.y, 0f, Screen.height);
        return pos;
    }
}


