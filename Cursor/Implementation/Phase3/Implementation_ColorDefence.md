# カラーディフェンスモード 実装詳細

> **注意**: このファイルは実装詳細のみを記載しています。設計・アイデアについては`ColorDefenceIdea.md`を参照してください。

---

## 🎯 実装ファイル構成

```
Assets/Main/Script/SinglePlayer/
├── Modes/
│   └── ColorDefenseMode.cs          // メインモードクラス
├── ColorDefense/
│   ├── ColorChangeArea.cs            // 色変化領域コンポーネント
│   └── ColorChangeAreaRenderer.cs    // 視覚表現用（オプション）
├── UI/
│   └── ColorDefenseUI.cs             // カラーディフェンス専用UI
└── Data/
    └── Settings/
        └── ColorDefenseSettings.cs   // ScriptableObject設定
```

---

## 🔧 実装詳細

### Step 1: ColorDefenseSettings（ScriptableObject設定）

**ファイル**: `Assets/Main/Script/SinglePlayer/Data/Settings/ColorDefenseSettings.cs`

**役割**: カラーディフェンスモードの全設定を管理

**実装コード**:

```csharp
using UnityEngine;
using System.Collections.Generic;

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
    
    [Tooltip("領域の形状タイプ")]
    public AreaShape areaShape = AreaShape.Circle;
    
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

public enum DifficultyScalingMode
{
    TimeBased,      // 時間帯ごとに設定（推奨：Inspectorで調整しやすい）
    CurveBased      // カーブで設定（滑らかな変化）
}

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

public enum AreaShape
{
    Circle,     // 円形
    Square,     // 正方形
    Rectangle   // 長方形
}
```

**Inspectorでの難易度設定方法（TimeBasedモード）**:

1. **`scalingMode`を`TimeBased`に設定**
2. **`difficultyPhases`リストにフェーズを追加**
   - 例: 3つのフェーズを追加
     - **Phase 1（0-60秒）**: 初期難易度
       - `startTime`: 0
       - `endTime`: 60
       - `spawnInterval`: 4秒
       - `maxAreasOnScreen`: 3
       - `colorChangeRate`: 0.3
       - `colorChangeSpeed`: 1.0
     - **Phase 2（60-120秒）**: 中期難易度
       - `startTime`: 60
       - `endTime`: 120
       - `spawnInterval`: 2.5秒
       - `maxAreasOnScreen`: 5
       - `colorChangeRate`: 0.5
       - `colorChangeSpeed`: 1.2
     - **Phase 3（120秒以降）**: 後期難易度
       - `startTime`: 120
       - `endTime`: 0（最後まで）
       - `spawnInterval`: 1.5秒
       - `maxAreasOnScreen`: 7
       - `colorChangeRate`: 0.7
       - `colorChangeSpeed`: 1.5

3. **各フェーズのパラメータを調整**
   - `startTime`/`endTime`: このフェーズが適用される時間帯（秒）
   - `spawnInterval`: このフェーズでの領域の出現間隔
   - `maxAreasOnScreen`: このフェーズでの同時存在可能な領域数
   - `colorChangeRate`: このフェーズでの色変化速度
   - `colorChangeSpeed`: このフェーズでの色変化速度の倍率
   - `areaSize`: このフェーズでの領域のサイズ（0の場合はデフォルトサイズを使用）

**注意点**:
- フェーズは時間順に並べる必要はありませんが、`startTime`と`endTime`が重複しないように注意してください
- `endTime`が0の場合は、そのフェーズが最後まで適用されます
- フェーズが設定されていない時間帯では、最後のフェーズの設定が適用されます

---

### Step 2: ColorChangeArea（色変化領域コンポーネント）

**ファイル**: `Assets/Main/Script/SinglePlayer/ColorDefense/ColorChangeArea.cs`

**役割**: 個々の色変化領域の状態管理と更新

**実装コード**:

```csharp
using UnityEngine;
using System.Collections.Generic;

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
    private float areaRadius;
    private AreaShape shape;
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
        float size = areaSize > 0f ? areaSize : settings.areaSize;
        this.areaRadius = size * 0.5f;
        this.shape = settings.areaShape;
        this.changeProgress = 0f;
        this.defendedProgress = 0f;
        this.isInitialized = true;
        
        // 領域内の総ピクセル数を計算
        CalculateTotalPixels();
        
        // 位置を設定
        transform.position = new Vector3(position.x, position.y, 0f);
    }
    
    /// <summary>
    /// 毎フレーム更新
    /// </summary>
    /// <param name="deltaTime">経過時間</param>
    /// <param name="canvas">ペイントキャンバス</param>
    /// <param name="effectiveColorChangeRate">有効な色変化速度（フェーズから取得した値、nullの場合は設定から取得）</param>
    public void Update(float deltaTime, PaintCanvas canvas, float? effectiveColorChangeRate = null)
    {
        if (!isInitialized || settings == null) return;
        
        // プレイヤーが塗った領域をチェック
        CheckPlayerPaint(canvas);
        
        // 色変化の進行
        // フェーズから取得した色変化速度を使用、そうでなければ設定から取得
        float baseChangeRate = effectiveColorChangeRate ?? settings.colorChangeRate;
        float effectiveChangeRate = baseChangeRate;
        
        // プレイヤーが塗った分だけ色変化を遅らせる
        if (defendedProgress > 0f)
        {
            effectiveChangeRate *= (1f - defendedProgress * settings.paintSlowdownEffect);
        }
        
        // アニメーションカーブを適用
        float curveValue = settings.changeProgressCurve.Evaluate(changeProgress);
        changeProgress += effectiveChangeRate * deltaTime * curveValue;
        changeProgress = Mathf.Clamp01(changeProgress);
        
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
        PaintSettings paintSettings = null;
        if (settings != null)
        {
            // PaintCanvasから設定を取得する必要がある場合は、後で設定
            // ここでは簡易計算
            float area = shape == AreaShape.Circle 
                ? Mathf.PI * areaRadius * areaRadius 
                : areaRadius * areaRadius * 4f; // 正方形の場合
            
            // ピクセル密度を仮定（実際のPaintCanvasの解像度に応じて調整）
            totalPixelsInArea = Mathf.RoundToInt(area);
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
        float canvasRadius = ScreenToCanvasRadius(areaRadius, canvas);
        
        // 領域の範囲を計算
        int minX = Mathf.Max(0, Mathf.RoundToInt(canvasCenter.x - canvasRadius));
        int maxX = Mathf.Min(paintSettings.textureWidth - 1, Mathf.RoundToInt(canvasCenter.x + canvasRadius));
        int minY = Mathf.Max(0, Mathf.RoundToInt(canvasCenter.y - canvasRadius));
        int maxY = Mathf.Min(paintSettings.textureHeight - 1, Mathf.RoundToInt(canvasCenter.y + canvasRadius));
        
        // 領域内の各ピクセルをチェック
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 pixelPos = new Vector2(x, y);
                
                // 領域の形状に応じて判定
                if (IsPixelInArea(pixelPos, canvasCenter, canvasRadius))
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
    /// 画面座標の半径をキャンバス座標の半径に変換
    /// </summary>
    private float ScreenToCanvasRadius(float screenRadius, PaintCanvas canvas)
    {
        PaintSettings paintSettings = canvas.GetSettings();
        if (paintSettings == null) return 0f;
        
        return (screenRadius / Screen.width) * paintSettings.textureWidth;
    }
    
    /// <summary>
    /// ピクセルが領域内にあるかチェック
    /// </summary>
    private bool IsPixelInArea(Vector2 pixelPos, Vector2 centerPos, float radius)
    {
        switch (shape)
        {
            case AreaShape.Circle:
                float distance = Vector2.Distance(pixelPos, centerPos);
                return distance <= radius;
                
            case AreaShape.Square:
                return Mathf.Abs(pixelPos.x - centerPos.x) <= radius &&
                       Mathf.Abs(pixelPos.y - centerPos.y) <= radius;
                
            case AreaShape.Rectangle:
                // 長方形の場合（幅と高さを別々に設定可能にする場合は拡張）
                return Mathf.Abs(pixelPos.x - centerPos.x) <= radius &&
                       Mathf.Abs(pixelPos.y - centerPos.y) <= radius;
                
            default:
                return false;
        }
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
    
    // プロパティ
    public Vector2 CenterPosition => centerPosition;
    public float ChangeProgress => changeProgress;
    public float DefendedProgress => defendedProgress;
    public float AreaRadius => areaRadius;
    public AreaShape Shape => shape;
}
```

**重要なポイント**:
- `changeProgress`: 色変化の進行度を管理
- `defendedProgress`: プレイヤーが塗った割合を管理
- `GetPaintedPixelsInArea()`: PaintCanvasから実際の塗り状態を取得
- `IsPixelInArea()`: 領域の形状に応じた判定

---

### Step 3: ColorDefenseMode（メインモードクラス）

**ファイル**: `Assets/Main/Script/SinglePlayer/Modes/ColorDefenseMode.cs`

**役割**: ゲームモード全体の管理とゲームループ

**実装コード**:

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// カラーディフェンスモード
/// ランダムな場所の色が変わるのを防ぐゲームモード
/// </summary>
public class ColorDefenseMode : MonoBehaviour, ISinglePlayerGameMode
{
    [Header("Settings")]
    [SerializeField] private ColorDefenseSettings settings;
    
    [Header("References")]
    [SerializeField] private PaintCanvas paintCanvas;
    [SerializeField] private ColorChangeAreaRenderer areaRenderer; // 視覚表現用（オプション）
    
    private List<ColorChangeArea> activeAreas = new List<ColorChangeArea>();
    private float spawnTimer = 0f;
    private int currentScore = 0;
    private int currentCombo = 0;
    private float gameTime = 0f;
    private float gameDuration = 180f;
    private bool isGameActive = false;
    private Vector2 lastPlayerPaintPosition = Vector2.zero;
    
    // イベント
    public static event System.Action<int> OnScoreUpdated;
    public static event System.Action<int> OnComboUpdated;
    public static event System.Action<ColorChangeArea> OnAreaSpawned;
    public static event System.Action<ColorChangeArea> OnAreaDefended;
    public static event System.Action<ColorChangeArea> OnAreaChanged;
    
    public SinglePlayerGameModeType GetModeType() => SinglePlayerGameModeType.ColorDefense;
    
    public void Initialize(SinglePlayerGameModeSettings modeSettings)
    {
        gameDuration = modeSettings.gameDuration;
        
        // 参照の自動検索
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
        }
        
        if (settings == null)
        {
            Debug.LogError("ColorDefenseMode: ColorDefenseSettingsが設定されていません");
        }
    }
    
    public void StartGame()
    {
        isGameActive = true;
        gameTime = 0f;
        currentScore = 0;
        currentCombo = 0;
        activeAreas.Clear();
        spawnTimer = 0f;
        
        // キャンバスをクリア
        if (paintCanvas != null)
        {
            paintCanvas.ResetCanvas();
        }
        
        // イベント発火
        OnScoreUpdated?.Invoke(currentScore);
        OnComboUpdated?.Invoke(currentCombo);
        
        Debug.Log("ColorDefenseMode: ゲーム開始");
    }
    
    public void Update(float deltaTime)
    {
        if (!isGameActive || settings == null) return;
        
        gameTime += deltaTime;
        spawnTimer += deltaTime;
        
        // 現在のフェーズを取得
        DifficultyPhase currentPhase = GetCurrentPhase();
        
        // 難易度スケーリング
        float difficultyMultiplier = GetDifficultyMultiplier();
        
        // 出現間隔を取得（TimeBasedモードの場合はフェーズから、CurveBasedの場合は計算）
        float effectiveSpawnInterval = GetEffectiveSpawnInterval(currentPhase);
        
        // 同時存在可能な領域数を取得
        int effectiveMaxAreas = GetEffectiveMaxAreas(currentPhase);
        
        // 新しい領域を生成
        if (spawnTimer >= effectiveSpawnInterval && activeAreas.Count < effectiveMaxAreas)
        {
            SpawnColorChangeArea(currentPhase);
            spawnTimer = 0f;
        }
        
        // 各領域の更新
        for (int i = activeAreas.Count - 1; i >= 0; i--)
        {
            ColorChangeArea area = activeAreas[i];
            
            // 色変化速度を取得（TimeBasedモードの場合はフェーズから）
            float effectiveColorChangeRate = GetEffectiveColorChangeRate(currentPhase);
            
            // 領域の更新（色変化速度を渡す）
            area.Update(deltaTime, paintCanvas, effectiveColorChangeRate);
            
            // 完全に変色した場合
            if (area.IsFullyChanged())
            {
                HandleAreaChanged(area);
                activeAreas.RemoveAt(i);
                Destroy(area.gameObject);
            }
            // 完全に防げた場合
            else if (area.IsFullyDefended())
            {
                HandleAreaDefended(area);
                activeAreas.RemoveAt(i);
                Destroy(area.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 色変化領域を生成
    /// </summary>
    private void SpawnColorChangeArea(DifficultyPhase phase = null)
    {
        Vector2 spawnPosition = GetSpawnPosition();
        
        GameObject areaObj = new GameObject($"ColorChangeArea_{activeAreas.Count}");
        areaObj.transform.SetParent(transform);
        
        ColorChangeArea area = areaObj.AddComponent<ColorChangeArea>();
        
        // フェーズで領域サイズが指定されている場合はそれを使用
        float areaSize = settings.areaSize;
        if (phase != null && phase.areaSize > 0f)
        {
            areaSize = phase.areaSize;
        }
        
        area.Initialize(settings, spawnPosition, areaSize);
        
        // イベント購読
        area.OnFullyChanged += HandleAreaChanged;
        area.OnFullyDefended += HandleAreaDefended;
        
        activeAreas.Add(area);
        OnAreaSpawned?.Invoke(area);
        
        // 視覚表現の設定（オプション）
        if (areaRenderer != null)
        {
            areaRenderer.AddArea(area);
        }
    }
    
    /// <summary>
    /// 領域の出現位置を計算
    /// </summary>
    private Vector2 GetSpawnPosition()
    {
        Vector2 basePosition = new Vector2(
            Random.Range(settings.areaSize, Screen.width - settings.areaSize),
            Random.Range(settings.areaSize, Screen.height - settings.areaSize)
        );
        
        // プレイヤーから離れた位置に出現させる設定がある場合
        if (settings.spawnAwayFromPlayer > 0f && paintCanvas != null)
        {
            // 最後に塗った位置から離れた位置を優先
            Vector2 awayFromPlayer = basePosition;
            int attempts = 0;
            const int maxAttempts = 10;
            
            while (attempts < maxAttempts)
            {
                float distance = Vector2.Distance(awayFromPlayer, lastPlayerPaintPosition);
                float minDistance = settings.areaSize * 2f;
                
                if (distance >= minDistance)
                {
                    break;
                }
                
                // 再計算
                awayFromPlayer = new Vector2(
                    Random.Range(settings.areaSize, Screen.width - settings.areaSize),
                    Random.Range(settings.areaSize, Screen.height - settings.areaSize)
                );
                attempts++;
            }
            
            basePosition = Vector2.Lerp(basePosition, awayFromPlayer, settings.spawnAwayFromPlayer);
        }
        
        return basePosition;
    }
    
    /// <summary>
    /// 領域が完全に変色した時の処理
    /// </summary>
    private void HandleAreaChanged(ColorChangeArea area)
    {
        // ペナルティ
        currentScore += settings.penaltyPerChangedArea;
        currentScore = Mathf.Max(0, currentScore); // スコアが負にならないように
        
        // コンボリセット
        currentCombo = 0;
        
        // イベント発火
        OnAreaChanged?.Invoke(area);
        OnScoreUpdated?.Invoke(currentScore);
        OnComboUpdated?.Invoke(currentCombo);
        
        Debug.Log($"ColorDefenseMode: 領域が変色 - スコア: {currentScore}");
    }
    
    /// <summary>
    /// 領域を完全に防げた時の処理
    /// </summary>
    private void HandleAreaDefended(ColorChangeArea area)
    {
        // スコア計算
        int baseScore = settings.scorePerDefendedArea;
        
        // 部分的に防げた場合の追加スコア
        if (area.DefendedProgress < 1f)
        {
            baseScore += Mathf.RoundToInt(
                (area.DefendedProgress - settings.defenseThreshold) * 
                settings.partialDefenseScoreMultiplier
            );
        }
        
        // コンボボーナス
        currentCombo++;
        int comboBonus = currentCombo * settings.comboBonusPerDefense;
        
        currentScore += baseScore + comboBonus;
        
        // イベント発火
        OnAreaDefended?.Invoke(area);
        OnScoreUpdated?.Invoke(currentScore);
        OnComboUpdated?.Invoke(currentCombo);
        
        Debug.Log($"ColorDefenseMode: 領域を防衛 - スコア: {currentScore}, コンボ: {currentCombo}");
    }
    
    /// <summary>
    /// 現在のフェーズを取得
    /// </summary>
    private DifficultyPhase GetCurrentPhase()
    {
        if (settings == null || settings.scalingMode != DifficultyScalingMode.TimeBased)
        {
            return null;
        }
        
        if (settings.difficultyPhases == null || settings.difficultyPhases.Count == 0)
        {
            return null;
        }
        
        // 現在の時間に該当するフェーズを検索
        foreach (var phase in settings.difficultyPhases)
        {
            if (phase.IsInPhase(gameTime))
            {
                return phase;
            }
        }
        
        // 該当するフェーズがない場合は、最後のフェーズを返す
        return settings.difficultyPhases[settings.difficultyPhases.Count - 1];
    }
    
    /// <summary>
    /// 難易度倍率を取得（CurveBasedモード用）
    /// </summary>
    private float GetDifficultyMultiplier()
    {
        if (settings == null) return 1f;
        
        if (settings.scalingMode == DifficultyScalingMode.TimeBased)
        {
            // TimeBasedモードの場合は、フェーズの設定から直接値を取得するため、倍率は1.0
            return 1f;
        }
        
        // CurveBasedモードの場合
        float normalizedTime = gameTime / gameDuration;
        float curveValue = settings.difficultyCurve.Evaluate(normalizedTime);
        return 1f + (curveValue - 1f) * (settings.maxDifficultyMultiplier - 1f);
    }
    
    /// <summary>
    /// 有効な出現間隔を取得
    /// </summary>
    private float GetEffectiveSpawnInterval(DifficultyPhase phase)
    {
        if (settings == null) return settings.spawnInterval;
        
        if (settings.scalingMode == DifficultyScalingMode.TimeBased && phase != null)
        {
            return phase.spawnInterval;
        }
        
        // CurveBasedモードの場合
        float difficultyMultiplier = GetDifficultyMultiplier();
        return Mathf.Lerp(
            settings.spawnInterval, 
            settings.minSpawnInterval, 
            1f - (1f / difficultyMultiplier)
        );
    }
    
    /// <summary>
    /// 有効な同時存在可能な領域数を取得
    /// </summary>
    private int GetEffectiveMaxAreas(DifficultyPhase phase)
    {
        if (settings == null) return settings.maxAreasOnScreen;
        
        if (settings.scalingMode == DifficultyScalingMode.TimeBased && phase != null)
        {
            return phase.maxAreasOnScreen;
        }
        
        // CurveBasedモードの場合はデフォルト値を使用
        return settings.maxAreasOnScreen;
    }
    
    /// <summary>
    /// 有効な色変化速度を取得
    /// </summary>
    private float GetEffectiveColorChangeRate(DifficultyPhase phase)
    {
        if (settings == null) return settings.colorChangeRate;
        
        if (settings.scalingMode == DifficultyScalingMode.TimeBased && phase != null)
        {
            return phase.colorChangeRate * phase.colorChangeSpeed;
        }
        
        // CurveBasedモードの場合
        float difficultyMultiplier = GetDifficultyMultiplier();
        return settings.colorChangeRate * difficultyMultiplier;
    }
    
    public void EndGame()
    {
        isGameActive = false;
        
        // 全ての領域をクリーンアップ
        foreach (var area in activeAreas)
        {
            if (area != null)
            {
                area.OnFullyChanged -= HandleAreaChanged;
                area.OnFullyDefended -= HandleAreaDefended;
                Destroy(area.gameObject);
            }
        }
        activeAreas.Clear();
        
        Debug.Log($"ColorDefenseMode: ゲーム終了 - 最終スコア: {currentScore}");
    }
    
    public void Pause() 
    { 
        isGameActive = false; 
    }
    
    public void Resume() 
    { 
        isGameActive = true; 
    }
    
    public int GetScore() => currentScore;
    
    public float GetProgress() => Mathf.Clamp01(gameTime / gameDuration);
    
    public bool IsGameOver() => gameTime >= gameDuration;
    
    void OnDestroy()
    {
        EndGame();
    }
}
```

**重要なポイント**:
- 難易度スケーリング: 時間経過で難易度が上がる
- コンボシステム: 連続で防げるとボーナス
- 出現位置の最適化: プレイヤーから離れた位置に出現させる設定
- イベント駆動: UIやエフェクトが反応できるようにイベントを発火

---

### Step 4: ColorChangeAreaRenderer（視覚表現）

**ファイル**: `Assets/Main/Script/SinglePlayer/ColorDefense/ColorChangeAreaRenderer.cs`

**役割**: 色変化領域の視覚表現

**実装コード**:

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 色変化領域の視覚表現
/// </summary>
public class ColorChangeAreaRenderer : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private GameObject areaIndicatorPrefab;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private Color safeColor = Color.green;
    
    private Dictionary<ColorChangeArea, GameObject> areaIndicators = new Dictionary<ColorChangeArea, GameObject>();
    
    public void AddArea(ColorChangeArea area)
    {
        if (areaIndicatorPrefab == null) return;
        
        GameObject indicator = Instantiate(areaIndicatorPrefab, transform);
        indicator.transform.position = new Vector3(area.CenterPosition.x, area.CenterPosition.y, 0f);
        
        areaIndicators[area] = indicator;
        
        // イベント購読
        area.OnProgressChanged += UpdateAreaVisual;
        area.OnFullyChanged += RemoveArea;
        area.OnFullyDefended += RemoveArea;
    }
    
    private void UpdateAreaVisual(ColorChangeArea area, float progress)
    {
        if (!areaIndicators.ContainsKey(area)) return;
        
        GameObject indicator = areaIndicators[area];
        Image image = indicator.GetComponent<Image>();
        if (image != null)
        {
            // 進行度に応じて色を変更
            Color currentColor;
            if (area.DefendedProgress > 0.5f)
            {
                currentColor = Color.Lerp(warningColor, safeColor, area.DefendedProgress);
            }
            else
            {
                currentColor = Color.Lerp(warningColor, dangerColor, progress);
            }
            
            image.color = currentColor;
            
            // スケールも変更（進行度に応じて）
            float scale = 1f + progress * 0.2f;
            indicator.transform.localScale = Vector3.one * scale;
        }
    }
    
    private void RemoveArea(ColorChangeArea area)
    {
        if (areaIndicators.ContainsKey(area))
        {
            Destroy(areaIndicators[area]);
            areaIndicators.Remove(area);
        }
    }
}
```

---

### Step 5: ColorDefenseUI（UI管理）

**ファイル**: `Assets/Main/Script/SinglePlayer/UI/ColorDefenseUI.cs`

**役割**: カラーディフェンスモード専用のUI要素を管理

**実装コード**:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// カラーディフェンスモード専用UI
/// </summary>
public class ColorDefenseUI : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    
    [Header("Area Status")]
    [SerializeField] private TextMeshProUGUI activeAreasText;
    [SerializeField] private Slider[] areaProgressBars; // 各領域の進行度（オプション）
    
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI defendedAreasText;
    [SerializeField] private TextMeshProUGUI changedAreasText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    
    private int currentScore = 0;
    private int currentCombo = 0;
    private int defendedAreasCount = 0;
    private int changedAreasCount = 0;
    
    void Start()
    {
        // イベント購読
        ColorDefenseMode.OnScoreUpdated += UpdateScore;
        ColorDefenseMode.OnComboUpdated += UpdateCombo;
        ColorDefenseMode.OnAreaSpawned += OnAreaSpawned;
        ColorDefenseMode.OnAreaDefended += OnAreaDefended;
        ColorDefenseMode.OnAreaChanged += OnAreaChanged;
        
        // ボタン設定
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        
        // ゲームオーバーパネルを非表示
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        ColorDefenseMode.OnScoreUpdated -= UpdateScore;
        ColorDefenseMode.OnComboUpdated -= UpdateCombo;
        ColorDefenseMode.OnAreaSpawned -= OnAreaSpawned;
        ColorDefenseMode.OnAreaDefended -= OnAreaDefended;
        ColorDefenseMode.OnAreaChanged -= OnAreaChanged;
    }
    
    private void UpdateScore(int score)
    {
        currentScore = score;
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }
    
    private void UpdateCombo(int combo)
    {
        currentCombo = combo;
        if (comboText != null)
        {
            comboText.text = $"Combo: {combo}";
        }
    }
    
    private void OnAreaSpawned(ColorChangeArea area)
    {
        UpdateActiveAreasCount();
    }
    
    private void OnAreaDefended(ColorChangeArea area)
    {
        defendedAreasCount++;
        UpdateActiveAreasCount();
    }
    
    private void OnAreaChanged(ColorChangeArea area)
    {
        changedAreasCount++;
        UpdateActiveAreasCount();
    }
    
    private void UpdateActiveAreasCount()
    {
        // 現在のアクティブな領域数を表示（ColorDefenseModeから取得）
        // 実装はColorDefenseModeにアクティブ領域数を取得するメソッドを追加する必要がある
        if (activeAreasText != null)
        {
            // TODO: ColorDefenseModeからアクティブ領域数を取得
            // activeAreasText.text = $"Active Areas: {activeCount}";
        }
    }
    
    public void ShowGameOver(int finalScore, int defendedCount, int changedCount)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (finalScoreText != null)
                finalScoreText.text = $"Final Score: {finalScore}";
            if (defendedAreasText != null)
                defendedAreasText.text = $"Defended: {defendedCount}";
            if (changedAreasText != null)
                changedAreasText.text = $"Changed: {changedCount}";
        }
    }
    
    private void OnRetryClicked()
    {
        // ゲームを再開（SinglePlayerModeManagerに通知）
        UnityEngine.SceneManagement.SceneManager.LoadScene("01_Gameplay");
    }
    
    private void OnMainMenuClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("00_MainMenu");
    }
}
```

---

## 🎮 モード管理の実装

### GameplayManagerとの連携

**ファイル**: `Assets/Main/Script/Gameplay/GameplayManager.cs`

**実装例**:

```csharp
public enum GameMode
{
    Creative,       // クリエイティブモード
    SinglePlayer,   // シングルプレイモード
    OfflineMulti,   // オフラインマルチ
    OnlineMulti     // オンラインマルチ
}

public class GameplayManager : MonoBehaviour
{
    [SerializeField] private GameMode currentMode = GameMode.SinglePlayer;
    [SerializeField] private CreativeModeManager creativeModeManager;
    [SerializeField] private SinglePlayerModeManager singlePlayerModeManager;
    
    private void InitializeGameMode()
    {
        switch (currentMode)
        {
            case GameMode.Creative:
                // クリエイティブモードを有効化
                creativeModeManager.gameObject.SetActive(true);
                break;
            case GameMode.SinglePlayer:
                // シングルプレイモードを有効化
                singlePlayerModeManager.gameObject.SetActive(true);
                break;
        }
    }
}
```

### SinglePlayerModeManagerとの連携

**ファイル**: `Assets/Main/Script/SinglePlayer/SinglePlayerModeManager.cs`

**実装例**:

```csharp
public enum SinglePlayerGameModeType
{
    MonsterHunt,
    ColorDefense,  // カラーディフェンスモード
    Tracing,
    AIBattle
}

public class SinglePlayerModeManager : MonoBehaviour
{
    [SerializeField] private SinglePlayerGameModeSettings settings;
    [SerializeField] private ColorDefenseMode colorDefenseMode;
    [SerializeField] private MonsterHuntMode monsterHuntMode;
    // ... 他のモード
    
    private ISinglePlayerGameMode currentMode;
    
    private void InitializeMode()
    {
        switch (settings.selectedMode)
        {
            case SinglePlayerGameModeType.ColorDefense:
                currentMode = colorDefenseMode;
                currentMode.Initialize(settings);
                currentMode.StartGame();
                break;
            // ... 他のモード
        }
    }
}
```

---

## 🎮 UI管理の実装

### MainMenuManager

**ファイル**: `Assets/Main/Script/UI/MainMenuManager.cs`

**実装例**:

```csharp
public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject customizationPanel;
    [SerializeField] private GameObject calibrationPanel;
    
    void Start()
    {
        ShowMainMenu();
    }
    
    public void ShowMainMenu() => ShowPanel(mainMenuPanel);
    public void ShowSettings() => ShowPanel(settingsPanel);
    public void ShowCustomization() => ShowPanel(customizationPanel);
    public void ShowCalibration() => ShowPanel(calibrationPanel);
    
    private void ShowPanel(GameObject panel)
    {
        // 全てのパネルを非表示にしてから、指定パネルのみ表示
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        customizationPanel.SetActive(false);
        calibrationPanel.SetActive(false);
        panel.SetActive(true);
    }
    
    public void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("01_Gameplay");
    }
}
```

### GameHUD

**ファイル**: `Assets/Main/Script/UI/GameHUD.cs`

**実装例**:

```csharp
public class GameHUD : MonoBehaviour
{
    [Header("Common UI Elements")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Slider progressBar;
    
    [Header("Mode-Specific UI")]
    [SerializeField] private GameObject creativeModeUI;
    [SerializeField] private GameObject singlePlayerModeUI;
    [SerializeField] private ColorDefenseUI colorDefenseUI; // カラーディフェンス専用UI
    
    private GameMode currentMode;
    
    public void Initialize(GameMode mode)
    {
        currentMode = mode;
        
        // 全てのモード固有UIを非表示
        if (creativeModeUI != null) creativeModeUI.SetActive(false);
        if (singlePlayerModeUI != null) singlePlayerModeUI.SetActive(false);
        
        // 選択されたモードのUIを表示
        switch (mode)
        {
            case GameMode.Creative:
                if (creativeModeUI != null) creativeModeUI.SetActive(true);
                break;
            case GameMode.SinglePlayer:
                if (singlePlayerModeUI != null) singlePlayerModeUI.SetActive(true);
                break;
        }
    }
    
    public void UpdateTimer(float remainingTime)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }
    
    public void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }
}
```

---

## 🧪 テスト項目

### 基本動作
- [ ] 領域が一定間隔で出現する
- [ ] 領域の色が徐々に変わる
- [ ] プレイヤーが塗ると色変化が遅くなる
- [ ] 完全に防げた時にスコアが加算される
- [ ] 完全に変色した時にペナルティが発生する

### ゲームフロー
- [ ] ゲーム開始時にキャンバスがクリアされる
- [ ] 制限時間終了時にゲームが終了する
- [ ] 一時停止・再開が動作する

### 難易度調整
- [ ] 時間経過で難易度が上がる
- [ ] 出現間隔が短くなる
- [ ] 色変化速度が速くなる
- [ ] TimeBasedモードでフェーズが正しく切り替わる
- [ ] 各フェーズの設定（出現間隔、同時存在数、色変化速度）が正しく適用される
- [ ] CurveBasedモードで難易度カーブが正しく適用される

### スコアシステム
- [ ] コンボボーナスが正しく計算される
- [ ] 部分的に防げた時のスコアが計算される
- [ ] スコアが負にならない

### パフォーマンス
- [ ] 多数の領域が同時に存在しても動作する
- [ ] フレームレートが安定している

---

## 📝 Inspectorでの設定手順

### 1. ColorDefenseSettingsアセットの作成

1. Unityメニューから`Game/SinglePlayer/Modes/Color Defense Settings`を選択
2. アセットを保存（例: `ColorDefenseSettings_Default.asset`）
3. Inspectorで各パラメータを調整

### 2. SinglePlayerGameModeSettingsアセットの設定

1. Unityメニューから`Game/SinglePlayer/Game Mode Settings`を選択
2. `selectedMode`を`ColorDefense`に設定
3. `colorDefenseSettings`に作成した`ColorDefenseSettings`アセットを設定

### 3. シーンでの設定

1. `GameplayManager`の`currentMode`を`GameMode.SinglePlayer`に設定
2. `SinglePlayerModeManager`の`settings`に`SinglePlayerGameModeSettings`アセットを設定
3. `ColorDefenseMode`コンポーネントを追加し、`settings`に`ColorDefenseSettings`アセットを設定
4. `ColorDefenseUI`コンポーネントを追加し、各UI要素を接続

---

## 🔗 関連ファイル

- **設計・アイデア**: `ColorDefenceIdea.md`
- **モード管理**: `ImplementationStep.md`のPhase 3セクション
- **PaintCanvas**: `Assets/Main/Script/GameLogic/PaintCanvas.cs`

