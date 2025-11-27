using UnityEngine;

/// <summary>
/// 色が変わる領域のコンポーネント
/// 色変化の進行度と、プレイヤーによる防御の進行度を管理
/// </summary>
public class ColorChangeArea : MonoBehaviour
{
    private ColorDefenseSettings settings;
    private Vector2 centerPosition;
    private float changeProgress = 0f;      // 色変化の進行度（0.0～1.0）
    private float defendedProgress = 0f;   // プレイヤーが防いだ進行度（0.0～1.0）
    private float areaSize;
    private IAreaShape shape;              // 形状判定ロジック（変更しやすい設計）
    private int totalPixelsInArea = 0;
    private bool isInitialized = false;
    
    // イベント
    public event System.Action<ColorChangeArea> OnFullyChanged;
    public event System.Action<ColorChangeArea> OnFullyDefended;
    public event System.Action<ColorChangeArea, float> OnProgressChanged; // (area, changeProgress)
    
    /// <summary>
    /// 領域を初期化
    /// </summary>
    /// <param name="settings">設定</param>
    /// <param name="position">中心位置</param>
    /// <param name="areaSize">領域のサイズ（0の場合はsettings.areaSizeを使用）</param>
    public void Initialize(ColorDefenseSettings settings, Vector2 position, float areaSize = 0f)
    {
        this.settings = settings;
        this.centerPosition = position;
        
        // 領域サイズが指定されている場合はそれを使用、そうでなければ設定から取得
        this.areaSize = areaSize > 0f ? areaSize : settings.areaSize;
        
        // 形状を初期化（変更しやすい設計）
        if (settings.areaShapeData != null)
        {
            this.shape = settings.areaShapeData.CreateShape();
        }
        else
        {
            // デフォルトは円形
            Debug.LogWarning("ColorChangeArea: areaShapeDataが設定されていません。デフォルトの円形を使用します。");
            this.shape = new CircleShape();
        }
        
        this.changeProgress = 0f;
        this.defendedProgress = 0f;
        this.isInitialized = true;
        
        // 領域内の総ピクセル数を計算
        CalculateTotalPixels();
        
        // 位置を設定
        transform.position = new Vector3(position.x, position.y, 0f);
    }
    
    /// <summary>
    /// 領域を更新（毎フレーム呼ばれる）
    /// </summary>
    /// <param name="deltaTime">経過時間</param>
    /// <param name="canvas">ペイントキャンバス</param>
    /// <param name="effectiveColorChangeRate">有効な色変化速度（フェーズから取得した値、nullの場合は設定から取得）</param>
    public void UpdateArea(float deltaTime, PaintCanvas canvas, float? effectiveColorChangeRate = null)
    {
        if (!isInitialized || settings == null) return;
        
        // プレイヤーが塗った領域をチェック
        CheckPlayerPaint(canvas);
        
        // 色変化の進行
        // フェーズから取得した色変化速度を使用、そうでなければ設定から取得
        float baseChangeRate = effectiveColorChangeRate ?? settings.colorChangeRate;
        float effectiveChangeRate = baseChangeRate;
        
        Debug.Log($"[ColorChangeArea] UpdateArea - 初期値: baseChangeRate={baseChangeRate:F4}, effectiveColorChangeRate={effectiveColorChangeRate}, settings.colorChangeRate={settings.colorChangeRate:F4}");
        
        // プレイヤーが塗った分だけ色変化を遅らせる
        if (defendedProgress > 0f)
        {
            float slowdownFactor = (1f - defendedProgress * settings.paintSlowdownEffect);
            effectiveChangeRate *= slowdownFactor;
            Debug.Log($"[ColorChangeArea] UpdateArea - 防御による減速: defendedProgress={defendedProgress:F4}, slowdownFactor={slowdownFactor:F4}, effectiveChangeRate={effectiveChangeRate:F4}");
        }
        
        // アニメーションカーブを適用
        float curveValue = settings.changeProgressCurve.Evaluate(changeProgress);
        
        // カーブが0の場合、デフォルトで1.0を使用（カーブが設定されていない場合のフォールバック）
        if (curveValue <= 0f && changeProgress < 0.01f)
        {
            Debug.LogWarning($"[ColorChangeArea] UpdateArea - アニメーションカーブが0を返しています。デフォルト値1.0を使用します。changeProgress={changeProgress:F4}");
            curveValue = 1f;
        }
        
        float previousProgress = changeProgress; // 前回の進行度を保存
        float progressIncrement = effectiveChangeRate * deltaTime * curveValue;
        changeProgress += progressIncrement;
        changeProgress = Mathf.Clamp01(changeProgress);
        
        Debug.Log($"[ColorChangeArea] UpdateArea - 進行度計算: deltaTime={deltaTime:F4}, curveValue={curveValue:F4}, progressIncrement={progressIncrement:F4}, 前回={previousProgress:F4}, 現在={changeProgress:F4}");
        
        // 敵の色を自動的に塗る処理
        if (canvas != null && changeProgress > previousProgress)
        {
            float progressDelta = changeProgress - previousProgress;
            Debug.Log($"[ColorChangeArea] 進行度更新 - 前回: {previousProgress:F4}, 現在: {changeProgress:F4}, 差分: {progressDelta:F4}, 変化速度: {effectiveChangeRate:F4}");
            PaintEnemyColor(canvas, previousProgress, changeProgress);
        }
        else if (canvas == null)
        {
            Debug.LogWarning("[ColorChangeArea] canvasがnullです");
        }
        else if (changeProgress <= previousProgress)
        {
            Debug.Log($"[ColorChangeArea] 進行度が増加していません - 前回: {previousProgress:F4}, 現在: {changeProgress:F4}");
        }
        
        // イベント発火
        OnProgressChanged?.Invoke(this, changeProgress);
        
        // 完全に変色したかチェック
        if (IsFullyChanged())
        {
            OnFullyChanged?.Invoke(this);
        }
        
        // 完全に防げたかチェック
        if (IsFullyDefended())
        {
            OnFullyDefended?.Invoke(this);
        }
    }
    
    /// <summary>
    /// プレイヤーが塗った領域をチェック
    /// </summary>
    private void CheckPlayerPaint(PaintCanvas canvas)
    {
        if (canvas == null) return;
        
        int paintedPixels = GetPaintedPixelsInArea(canvas);
        defendedProgress = (float)paintedPixels / totalPixelsInArea;
        defendedProgress = Mathf.Clamp01(defendedProgress);
    }
    
    /// <summary>
    /// 領域内の総ピクセル数を計算
    /// </summary>
    private void CalculateTotalPixels()
    {
        if (shape != null)
        {
            // 形状クラスから総ピクセル数を取得（変更しやすい設計）
            totalPixelsInArea = shape.CalculateAreaInPixels(areaSize);
        }
        else
        {
            // フォールバック: 円形として計算
            float radius = areaSize * 0.5f;
            totalPixelsInArea = Mathf.RoundToInt(Mathf.PI * radius * radius);
        }
    }
    
    /// <summary>
    /// 領域内でプレイヤーが塗ったピクセル数を取得
    /// </summary>
    private int GetPaintedPixelsInArea(PaintCanvas canvas)
    {
        if (canvas == null) return 0;
        
        int paintedCount = 0;
        PaintSettings paintSettings = canvas.GetSettings();
        if (paintSettings == null) return 0;
        
        // 画面座標をキャンバス座標に変換
        Vector2 canvasCenter = ScreenToCanvas(centerPosition, canvas);
        float canvasSize = ScreenToCanvasSize(areaSize, canvas);
        
        // バウンディングボックスを取得して最適化（変更しやすい設計）
        Rect boundingBox = shape != null 
            ? shape.GetBoundingBox(canvasCenter, canvasSize)
            : new Rect(canvasCenter.x - canvasSize * 0.5f, canvasCenter.y - canvasSize * 0.5f, 
                      canvasSize, canvasSize);
        
        // 領域の範囲を計算
        int minX = Mathf.Max(0, Mathf.RoundToInt(boundingBox.xMin));
        int maxX = Mathf.Min(paintSettings.textureWidth - 1, Mathf.RoundToInt(boundingBox.xMax));
        int minY = Mathf.Max(0, Mathf.RoundToInt(boundingBox.yMin));
        int maxY = Mathf.Min(paintSettings.textureHeight - 1, Mathf.RoundToInt(boundingBox.yMax));
        
        // 領域内の各ピクセルをチェック
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 pixelPos = new Vector2(x, y);
                
                // 領域の形状に応じて判定（変更しやすい設計）
                if (IsPixelInArea(pixelPos, canvasCenter, canvasSize))
                {
                    // プレイヤーが塗っているかチェック（playerId > 0）
                    int playerId = canvas.GetPlayerIdAtCanvas(x, y);
                    if (playerId > 0)
                    {
                        paintedCount++;
                    }
                }
            }
        }
        
        return paintedCount;
    }
    
    /// <summary>
    /// 画面座標をキャンバス座標に変換
    /// </summary>
    private Vector2 ScreenToCanvas(Vector2 screenPos, PaintCanvas canvas)
    {
        PaintSettings paintSettings = canvas.GetSettings();
        if (paintSettings == null) return Vector2.zero;
        
        int canvasX = Mathf.RoundToInt((screenPos.x / Screen.width) * paintSettings.textureWidth);
        int canvasY = Mathf.RoundToInt((screenPos.y / Screen.height) * paintSettings.textureHeight);
        
        return new Vector2(canvasX, canvasY);
    }
    
    /// <summary>
    /// 画面座標のサイズをキャンバス座標のサイズに変換
    /// </summary>
    private float ScreenToCanvasSize(float screenSize, PaintCanvas canvas)
    {
        PaintSettings paintSettings = canvas.GetSettings();
        if (paintSettings == null) return 0f;
        
        return (screenSize / Screen.width) * paintSettings.textureWidth;
    }
    
    /// <summary>
    /// ピクセルが領域内にあるかチェック（変更しやすい設計）
    /// </summary>
    private bool IsPixelInArea(Vector2 pixelPos, Vector2 centerPos, float baseSize)
    {
        if (shape != null)
        {
            // 形状クラスに判定を委譲（変更しやすい設計）
            return shape.IsPointInArea(pixelPos, centerPos, baseSize);
        }
        
        // フォールバック: 円形として判定
        float radius = baseSize * 0.5f;
        return Vector2.Distance(pixelPos, centerPos) <= radius;
    }
    
    /// <summary>
    /// 完全に変色したかどうか
    /// </summary>
    public bool IsFullyChanged()
    {
        return changeProgress >= 1f && defendedProgress < settings.defenseThreshold;
    }
    
    /// <summary>
    /// 完全に防げたかどうか
    /// </summary>
    public bool IsFullyDefended()
    {
        return defendedProgress >= settings.fullDefenseThreshold;
    }
    
    /// <summary>
    /// 部分的に防げているかどうか
    /// </summary>
    public bool IsPartiallyDefended()
    {
        return defendedProgress > 0f && defendedProgress < settings.fullDefenseThreshold;
    }
    
    /// <summary>
    /// 敵の色を領域内に自動的に塗る
    /// </summary>
    private void PaintEnemyColor(PaintCanvas canvas, float previousProgress, float currentProgress)
    {
        Debug.Log($"[ColorChangeArea] PaintEnemyColor 呼び出し - 前回: {previousProgress:F4}, 現在: {currentProgress:F4}");
        
        if (canvas == null || settings == null)
        {
            Debug.LogWarning($"[ColorChangeArea] PaintEnemyColor - canvasまたはsettingsがnullです (canvas: {canvas != null}, settings: {settings != null})");
            return;
        }
        
        PaintSettings paintSettings = canvas.GetSettings();
        if (paintSettings == null)
        {
            Debug.LogWarning("[ColorChangeArea] PaintEnemyColor - paintSettingsがnullです");
            return;
        }
        
        Debug.Log($"[ColorChangeArea] PaintEnemyColor - 領域中心: {centerPosition}, サイズ: {areaSize}");
        
        // 画面座標をキャンバス座標に変換
        Vector2 canvasCenter = ScreenToCanvas(centerPosition, canvas);
        float canvasSize = ScreenToCanvasSize(areaSize, canvas);
        
        // バウンディングボックスを取得
        Rect boundingBox = shape != null 
            ? shape.GetBoundingBox(canvasCenter, canvasSize)
            : new Rect(canvasCenter.x - canvasSize * 0.5f, canvasCenter.y - canvasSize * 0.5f, 
                      canvasSize, canvasSize);
        
        // 領域の範囲を計算
        int minX = Mathf.Max(0, Mathf.RoundToInt(boundingBox.xMin));
        int maxX = Mathf.Min(paintSettings.textureWidth - 1, Mathf.RoundToInt(boundingBox.xMax));
        int minY = Mathf.Max(0, Mathf.RoundToInt(boundingBox.yMin));
        int maxY = Mathf.Min(paintSettings.textureHeight - 1, Mathf.RoundToInt(boundingBox.yMax));
        
        // 進行度の差分に応じて塗るピクセル数を計算
        float progressDelta = currentProgress - previousProgress;
        if (progressDelta <= 0f)
        {
            Debug.LogWarning($"[ColorChangeArea] PaintEnemyColor - progressDeltaが0以下です: {progressDelta:F4}");
            return;
        }
        
        Debug.Log($"[ColorChangeArea] PaintEnemyColor - バウンディングボックス: ({minX}, {minY}) ～ ({maxX}, {maxY})");
        
        // 領域内の総ピクセル数を計算
        int totalPixelsInBounds = 0;
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 pixelPos = new Vector2(x, y);
                if (IsPixelInArea(pixelPos, canvasCenter, canvasSize))
                {
                    totalPixelsInBounds++;
                }
            }
        }
        
        if (totalPixelsInBounds == 0)
        {
            Debug.LogWarning($"[ColorChangeArea] PaintEnemyColor - 領域内のピクセル数が0です (canvasCenter: {canvasCenter}, canvasSize: {canvasSize})");
            return;
        }
        
        // 塗るべきピクセル数を計算（進行度に応じて）
        int pixelsToPaint = Mathf.Max(1, Mathf.RoundToInt(totalPixelsInBounds * progressDelta));
        
        Debug.Log($"[ColorChangeArea] PaintEnemyColor - 総ピクセル数: {totalPixelsInBounds}, 進行度差分: {progressDelta:F4}, 塗るべきピクセル数: {pixelsToPaint}");
        
        // ランダムにピクセルを選択して塗る（パフォーマンス最適化）
        int paintedCount = 0;
        int attempts = 0;
        const int maxAttemptsMultiplier = 3; // 無限ループ防止のための倍率
        int maxAttempts = pixelsToPaint * maxAttemptsMultiplier;
        
        while (paintedCount < pixelsToPaint && attempts < maxAttempts)
        {
            int x = Random.Range(minX, maxX + 1);
            int y = Random.Range(minY, maxY + 1);
            Vector2 pixelPos = new Vector2(x, y);
            
            // 領域内かチェック
            if (IsPixelInArea(pixelPos, canvasCenter, canvasSize))
            {
                // プレイヤーが既に塗っている場合はスキップ（上塗りしない）
                int playerId = canvas.GetPlayerIdAtCanvas(x, y);
                if (playerId <= 0) // 未塗り（0）または敵の色（負の値）のみ塗る
                {
                    // 敵の色を塗る（playerId = -1 で敵を表現）
                    Vector2 screenPos = CanvasToScreen(pixelPos, canvas);
                    Debug.Log($"[ColorChangeArea] PaintEnemyColor - 塗り実行: 画面座標({screenPos.x:F1}, {screenPos.y:F1}), キャンバス座標({x}, {y}), 色: {settings.targetColor}");
                    canvas.PaintAt(screenPos, -1, 1f, settings.targetColor);
                    paintedCount++;
                }
                else
                {
                    Debug.Log($"[ColorChangeArea] PaintEnemyColor - スキップ: プレイヤーが既に塗っています (playerId: {playerId})");
                }
            }
            attempts++;
        }
        
        Debug.Log($"[ColorChangeArea] PaintEnemyColor - 完了: {paintedCount}/{pixelsToPaint}ピクセルを塗りました (試行回数: {attempts}/{maxAttempts})");
    }
    
    /// <summary>
    /// キャンバス座標を画面座標に変換
    /// </summary>
    private Vector2 CanvasToScreen(Vector2 canvasPos, PaintCanvas canvas)
    {
        PaintSettings paintSettings = canvas.GetSettings();
        if (paintSettings == null) return Vector2.zero;
        
        float screenX = (canvasPos.x / paintSettings.textureWidth) * Screen.width;
        float screenY = (canvasPos.y / paintSettings.textureHeight) * Screen.height;
        
        return new Vector2(screenX, screenY);
    }
    
    // プロパティ
    public Vector2 CenterPosition => centerPosition;
    public float ChangeProgress => changeProgress;
    public float DefendedProgress => defendedProgress;
    public float AreaSize => areaSize;
    public IAreaShape Shape => shape;
}

