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
    private float elapsedTime = 0f;         // 経過時間（秒）
    private float effectiveTimeToComplete = 10f; // 有効な塗り終わるまでの時間（防御による減速を考慮）
    private PaintCanvas subscribedCanvas = null; // 購読しているPaintCanvas
    private bool isAutoPaintCancelled = false; // 自動塗りがキャンセルされたか
    private bool hasFullyDefendedEventFired = false; // OnFullyDefendedイベントが発火済みか（重複発火防止）
    private bool hasEnemyColorErased = false; // 敵の色を消したか（重複実行防止）
    private float lastPaintProgress = 0f; // 前回塗り更新時の進行度（0.1刻み）
    
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
        this.elapsedTime = 0f;
        this.effectiveTimeToComplete = settings.timeToComplete;
        this.isAutoPaintCancelled = false; // フラグをリセット
        this.hasFullyDefendedEventFired = false; // イベント発火フラグをリセット
        this.hasEnemyColorErased = false; // 敵の色を消したフラグをリセット
        this.lastPaintProgress = 0f; // 前回塗り更新時の進行度をリセット
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
    /// <param name="effectiveTimeToComplete">有効な塗り終わるまでの時間（フェーズから取得した値、0の場合は設定から取得）</param>
    public void UpdateArea(float deltaTime, PaintCanvas canvas, float effectiveTimeToComplete = 0f)
    {
        if (!isInitialized || settings == null) return;
        
        // 自動塗りがキャンセルされている場合は、敵の色を塗らない
        if (isAutoPaintCancelled)
        {
            // プレイヤーが塗った領域をチェック（スコア判定のため）
            CheckPlayerPaint(canvas);
            
            // 完全に防げたかチェック（念のため）
            if (IsFullyDefended() && !hasFullyDefendedEventFired)
            {
                hasFullyDefendedEventFired = true;
                Debug.Log($"[ColorChangeArea] UpdateArea - 自動塗りキャンセル中に完全に防げました: defendedProgress={defendedProgress:F4} >= defenseThreshold={settings.defenseThreshold:F4}");
                OnFullyDefended?.Invoke(this);
            }
            return; // 自動塗りを停止
        }
        
        // プレイヤーが塗った領域をチェック
        CheckPlayerPaint(canvas);
        
        // 有効な塗り終わるまでの時間を取得（フェーズから指定されていない場合は設定から取得）
        float baseTimeToComplete = effectiveTimeToComplete > 0f ? effectiveTimeToComplete : settings.timeToComplete;
        float currentTimeToComplete = baseTimeToComplete;
        
        // プレイヤーが塗った分だけ色変化を遅らせる（elapsedTimeの増加を遅らせる）
        if (defendedProgress > 0f)
        {
            float slowdownFactor = Mathf.Max(0.1f, 1f - defendedProgress * settings.paintSlowdownEffect); // 最小値を0.1に設定して完全停止を防ぐ
            // elapsedTimeの増加を遅らせる
            elapsedTime += deltaTime * slowdownFactor;
        }
        else
        {
            elapsedTime += deltaTime;
        }
        
        // 時間ベースで進行度を計算
        float previousProgress = changeProgress;
        
        if (settings.useProgressCurve)
        {
            // アニメーションカーブを使用する場合
            float normalizedTime = Mathf.Clamp01(elapsedTime / currentTimeToComplete);
            changeProgress = settings.changeProgressCurve.Evaluate(normalizedTime);
        }
        else
        {
            // 時間ベースで均等に進行
            changeProgress = Mathf.Clamp01(elapsedTime / currentTimeToComplete);
        }
        
        Debug.Log($"[ColorChangeArea] UpdateArea - 進行度計算: elapsedTime={elapsedTime:F4}, currentTimeToComplete={currentTimeToComplete:F4}, パラメータeffectiveTimeToComplete={effectiveTimeToComplete:F4}, settings.timeToComplete={settings.timeToComplete:F4}, 前回={previousProgress:F4}, 現在={changeProgress:F4}, defendedProgress={defendedProgress:F4}");
        
        // 敵の色を自動的に塗る処理（自動塗りがキャンセルされていない場合のみ）
        // 敵ペンモードのときは、ここでの自動塗りは無効化する
        bool useAreaAutoPaint = settings.enemyPaintMode == EnemyPaintMode.AreaAuto;
        if (canvas != null && changeProgress > previousProgress && !isAutoPaintCancelled && useAreaAutoPaint)
        {
            float progressDelta = changeProgress - previousProgress;
            float progressRate = 1.0f / effectiveTimeToComplete; // 1秒あたりの進行率
            Debug.Log($"[ColorChangeArea] 進行度更新 - 前回: {previousProgress:F4}, 現在: {changeProgress:F4}, 差分: {progressDelta:F4}, 残り時間: {effectiveTimeToComplete - elapsedTime:F2}秒");
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
        
        // 完全に防げたかチェック（UpdateArea内でもチェック）
        if (IsFullyDefended() && !isAutoPaintCancelled && !hasFullyDefendedEventFired)
        {
            // 敵の色を消す（まだ消していない場合）
            if (!hasEnemyColorErased && canvas != null)
            {
                hasEnemyColorErased = true;
                canvas.ErasePlayerIdInArea(centerPosition, areaSize, -1, settings.areaShapeData);
                Debug.Log($"[ColorChangeArea] UpdateArea - 敵の色を消しました: centerPosition={centerPosition}, areaSize={areaSize}");
            }
            
            hasFullyDefendedEventFired = true;
            isAutoPaintCancelled = true; // 自動塗りを停止
            Debug.Log($"[ColorChangeArea] UpdateArea - 完全に防げました！自動塗りをキャンセル: defendedProgress={defendedProgress:F4} >= defenseThreshold={settings.defenseThreshold:F4}, changeProgress={changeProgress:F4}");
            OnFullyDefended?.Invoke(this);
            Debug.Log($"[ColorChangeArea] UpdateArea - OnFullyDefendedイベント発火完了");
        }
    }
    
    /// <summary>
    /// プレイヤーが塗った領域をチェック
    /// </summary>
    private void CheckPlayerPaint(PaintCanvas canvas)
    {
        if (canvas == null) return;
        
        int paintedPixels = GetPaintedPixelsInArea(canvas);
        float previousDefendedProgress = defendedProgress;
        defendedProgress = (float)paintedPixels / totalPixelsInArea;
        defendedProgress = Mathf.Clamp01(defendedProgress);
        
        // デバッグログ（変化があった場合のみ）
        if (Mathf.Abs(defendedProgress - previousDefendedProgress) > 0.001f)
        {
            Debug.Log($"[ColorChangeArea] CheckPlayerPaint - 更新: paintedPixels={paintedPixels}, totalPixelsInArea={totalPixelsInArea}, defendedProgress={defendedProgress:F4} (前回={previousDefendedProgress:F4})");
        }
    }
    
    /// <summary>
    /// 領域内の総ピクセル数を計算
    /// </summary>
    private void CalculateTotalPixels(PaintCanvas canvas = null)
    {
        // PaintCanvasが利用可能な場合は、実際のピクセル数をカウント
        if (canvas != null)
        {
            PaintSettings paintSettings = canvas.GetSettings();
            if (paintSettings != null)
            {
                // キャンバス座標系で実際のピクセル数をカウント
                Vector2 canvasCenter = ScreenToCanvas(centerPosition, canvas);
                float canvasSize = ScreenToCanvasSize(areaSize, canvas);
                
                Rect boundingBox = shape != null 
                    ? shape.GetBoundingBox(canvasCenter, canvasSize)
                    : new Rect(canvasCenter.x - canvasSize * 0.5f, canvasCenter.y - canvasSize * 0.5f, 
                              canvasSize, canvasSize);
                
                int minX = Mathf.Max(0, Mathf.RoundToInt(boundingBox.xMin));
                int maxX = Mathf.Min(paintSettings.textureWidth - 1, Mathf.RoundToInt(boundingBox.xMax));
                int minY = Mathf.Max(0, Mathf.RoundToInt(boundingBox.yMin));
                int maxY = Mathf.Min(paintSettings.textureHeight - 1, Mathf.RoundToInt(boundingBox.yMax));
                
                totalPixelsInArea = 0;
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        Vector2 pixelPos = new Vector2(x, y);
                        if (IsPixelInArea(pixelPos, canvasCenter, canvasSize))
                        {
                            totalPixelsInArea++;
                        }
                    }
                }
                
                Debug.Log($"[ColorChangeArea] CalculateTotalPixels - 実際のピクセル数をカウント: totalPixelsInArea={totalPixelsInArea}, キャンバス座標系でのサイズ={canvasSize:F2}");
                return;
            }
        }
        
        // フォールバック: 理論値を使用（PaintCanvasが利用できない場合）
        if (shape != null)
        {
            totalPixelsInArea = shape.CalculateAreaInPixels(areaSize);
            Debug.Log($"[ColorChangeArea] CalculateTotalPixels - 理論値を使用: totalPixelsInArea={totalPixelsInArea}, areaSize={areaSize:F2}");
        }
        else
        {
            float radius = areaSize * 0.5f;
            totalPixelsInArea = Mathf.RoundToInt(Mathf.PI * radius * radius);
            Debug.Log($"[ColorChangeArea] CalculateTotalPixels - 理論値を使用（円形）: totalPixelsInArea={totalPixelsInArea}, areaSize={areaSize:F2}");
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
        return defendedProgress >= settings.defenseThreshold;
    }
    
    /// <summary>
    /// 部分的に防げているかどうか
    /// </summary>
    public bool IsPartiallyDefended()
    {
        return defendedProgress > 0f && defendedProgress < settings.defenseThreshold;
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
    /// 敵の色を領域内に自動的に塗る（領域全体を透明度で塗る）
    /// </summary>
    private void PaintEnemyColor(PaintCanvas canvas, float previousProgress, float currentProgress)
    {
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
        
        // 進行度を0.1刻みに丸める
        float currentStep = Mathf.Floor(currentProgress * 10f) / 10f;
        float previousStep = lastPaintProgress;
        
        // 0.1刻みで増えていない場合は更新しない
        if (currentStep <= previousStep)
        {
            return;
        }
        
        Debug.Log($"[ColorChangeArea] PaintEnemyColor - 更新: 前回進行度={previousStep:F1}, 現在進行度={currentStep:F1}, アルファ値={currentStep:F1}");
        
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
        
        // アルファ値を現在の進行度に設定
        float alpha = currentStep;
        
        int paintedCount = 0;
        
        // 領域内の全ピクセルを処理
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 pixelPos = new Vector2(x, y);
                
                // 領域内かチェック
                if (!IsPixelInArea(pixelPos, canvasCenter, canvasSize))
                    continue;
                
                // プレイヤーが既に塗っている場合はスキップ（上塗りしない）
                int playerId = canvas.GetPlayerIdAtCanvas(x, y);
                if (playerId > 0)
                    continue;
                
                // アルファ値を指定して塗る
                Vector2 screenPos = CanvasToScreen(pixelPos, canvas);
                Color enemyColor = GetEnemyColor();
                canvas.PaintAtWithAlpha(screenPos, -1, 1f, enemyColor, alpha);
                paintedCount++;
            }
        }
        
        // テクスチャの更新をフラッシュ（全ピクセル更新後に一度だけ）
        canvas.FlushTextureUpdates();
        
        // 前回の更新進行度を記録
        lastPaintProgress = currentStep;
        
        Debug.Log($"[ColorChangeArea] PaintEnemyColor - 完了: {paintedCount}ピクセルをアルファ値{alpha:F1}で塗りました");
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
    
    /// <summary>
    /// PaintCanvasのイベントを購読
    /// </summary>
    public void SubscribeToPaintCanvas(PaintCanvas canvas)
    {
        // 既存の購読を解除
        if (subscribedCanvas != null)
        {
            subscribedCanvas.OnPaintCompleted -= OnPaintCanvasCompleted;
            Debug.Log($"[ColorChangeArea] SubscribeToPaintCanvas - 既存の購読を解除: {subscribedCanvas.name}");
        }
        
        subscribedCanvas = canvas;
        if (canvas != null)
        {
            canvas.OnPaintCompleted += OnPaintCanvasCompleted;
            
            // PaintCanvasが利用可能になったので、実際のピクセル数を再計算
            CalculateTotalPixels(canvas);
            
            Debug.Log($"[ColorChangeArea] SubscribeToPaintCanvas - イベント購読完了: {canvas.name}, 領域中心={centerPosition}, サイズ={areaSize}, totalPixelsInArea={totalPixelsInArea}");
        }
        else
        {
            Debug.LogWarning("[ColorChangeArea] SubscribeToPaintCanvas - canvasがnullです");
        }
    }
    
    /// <summary>
    /// PaintCanvasの塗り完了イベントハンドラー
    /// </summary>
    private void OnPaintCanvasCompleted(Vector2 screenPosition, int playerId, float intensity)
    {
        // プレイヤーが塗った場合のみ処理
        if (playerId > 0 && subscribedCanvas != null)
        {
            Debug.Log($"[ColorChangeArea] OnPaintCanvasCompleted - イベント受信: 画面座標({screenPosition.x:F1}, {screenPosition.y:F1}), playerId={playerId}, intensity={intensity:F4}");
            
            // 領域外チェックを削除（PaintAtWithRadiusは半径で塗るため、中心が領域外でも領域内のピクセルが塗られる可能性がある）
            // 常にCheckPlayerPaintを呼んで、実際に塗られたピクセル数を確認する
            
            // 更新前のdefendedProgressを保存
            float previousDefendedProgress = defendedProgress;
            
            // 即座にdefendedProgressを更新
            CheckPlayerPaint(subscribedCanvas);
            
            Debug.Log($"[ColorChangeArea] OnPaintCanvasCompleted - defendedProgress更新: 前回={previousDefendedProgress:F4}, 現在={defendedProgress:F4}, defenseThreshold={settings.defenseThreshold:F4}, IsFullyDefended={IsFullyDefended()}");
            
            // 完全に防げた場合、自動塗りをキャンセル
            if (IsFullyDefended() && !hasFullyDefendedEventFired)
            {
                // 敵の色を消す（まだ消していない場合）
                if (!hasEnemyColorErased && subscribedCanvas != null)
                {
                    hasEnemyColorErased = true;
                    subscribedCanvas.ErasePlayerIdInArea(centerPosition, areaSize, -1, settings.areaShapeData);
                    Debug.Log($"[ColorChangeArea] OnPaintCanvasCompleted - 敵の色を消しました: centerPosition={centerPosition}, areaSize={areaSize}");
                }
                
                hasFullyDefendedEventFired = true;
                isAutoPaintCancelled = true; // 自動塗りを停止
                Debug.Log($"[ColorChangeArea] OnPaintCanvasCompleted - 完全に防げました！自動塗りをキャンセル: defendedProgress={defendedProgress:F4} >= defenseThreshold={settings.defenseThreshold:F4}, changeProgress={changeProgress:F4}");
                OnFullyDefended?.Invoke(this);
                Debug.Log($"[ColorChangeArea] OnPaintCanvasCompleted - OnFullyDefendedイベント発火完了");
            }
        }
        else
        {
            if (playerId <= 0)
            {
                Debug.Log($"[ColorChangeArea] OnPaintCanvasCompleted - スキップ: playerId={playerId} (プレイヤーではない)");
            }
            if (subscribedCanvas == null)
            {
                Debug.LogWarning($"[ColorChangeArea] OnPaintCanvasCompleted - スキップ: subscribedCanvasがnull");
            }
        }
    }
    
    void OnDestroy()
    {
        // イベント購読を解除
        if (subscribedCanvas != null)
        {
            subscribedCanvas.OnPaintCompleted -= OnPaintCanvasCompleted;
            subscribedCanvas = null;
        }
    }
    
    // プロパティ
    public Vector2 CenterPosition => centerPosition;
    public float ChangeProgress => changeProgress;
    public float DefendedProgress => defendedProgress;
    public float AreaSize => areaSize;
    public IAreaShape Shape => shape;
}

