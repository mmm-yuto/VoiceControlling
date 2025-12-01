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
    
    // 敵ペン（キャンバス上を動きながら塗る敵）のリスト
    private readonly List<EnemyPainter> enemyPainters = new List<EnemyPainter>();
    
    private List<ColorChangeArea> activeAreas = new List<ColorChangeArea>();
    private float spawnTimer = 0f;
    private int currentScore = 0;
    private int currentCombo = 0;
    private float gameTime = 0f;
    private float gameDuration = 180f;
    private bool isGameActive = false;
    private Vector2 lastPlayerPaintPosition = Vector2.zero;
    
    public enum GameResult
    {
        PlayerWin,
        EnemyWin,
        Draw
    }
    
    // イベント
    public static event System.Action<int> OnScoreUpdated;
    public static event System.Action<int> OnComboUpdated;
    public static event System.Action<ColorChangeArea> OnAreaSpawned;
    public static event System.Action<ColorChangeArea> OnAreaDefended;
    public static event System.Action<ColorChangeArea> OnAreaChanged;
    public static event System.Action<GameResult, float, float> OnGameEnded; // (結果, playerRatio, enemyRatio)
    
    public SinglePlayerGameModeType GetModeType() => SinglePlayerGameModeType.ColorDefense;
    
    public void Initialize(SinglePlayerGameModeSettings modeSettings)
    {
        gameDuration = modeSettings.gameDuration;
        
        // 設定を取得
        if (modeSettings.colorDefenseSettings != null)
        {
            settings = modeSettings.colorDefenseSettings;
        }
        
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
        enemyPainters.Clear();
        
        // キャンバスをクリア
        if (paintCanvas != null)
        {
            paintCanvas.ResetCanvas();
        }
        
        // 敵ペンを初期化（モードがGlobalPaintersのときのみ）
        if (settings != null && settings.enemyPaintMode == EnemyPaintMode.GlobalPainters)
        {
            InitializeEnemyPainters();
        }
        
        // イベント発火
        OnScoreUpdated?.Invoke(currentScore);
        OnComboUpdated?.Invoke(currentCombo);
        
        Debug.Log("ColorDefenseMode: ゲーム開始");
    }
    
    void Update()
    {
        if (!isGameActive || settings == null) return;
        
        UpdateGame(Time.deltaTime);
    }
    
    public void UpdateGame(float deltaTime)
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
            // インデックスが有効かチェック
            if (i < 0 || i >= activeAreas.Count)
            {
                continue;
            }
            
            ColorChangeArea area = activeAreas[i];
            
            // 領域が既に削除されている場合はリストから削除してスキップ
            if (area == null || area.gameObject == null)
            {
                if (i >= 0 && i < activeAreas.Count)
                {
                    activeAreas.RemoveAt(i);
                }
                continue;
            }
            
            // 塗り終わるまでの時間を取得（TimeBasedモードの場合はフェーズから）
            float effectiveTimeToComplete = GetEffectiveTimeToComplete(currentPhase);
            
            // 領域の更新（塗り終わるまでの時間を渡す）
            area.UpdateArea(deltaTime, paintCanvas, effectiveTimeToComplete);
            
            // イベント経由で処理されるため、ここでは削除処理は行わない
            // （HandleAreaChanged()とHandleAreaDefended()内で削除される）
        }
        
        // 敵ペンの更新（キャンバス全体を人間のように塗る）
        if (paintCanvas != null && settings.enemyPaintMode == EnemyPaintMode.GlobalPainters && enemyPainters.Count > 0)
        {
            for (int i = 0; i < enemyPainters.Count; i++)
            {
                enemyPainters[i]?.Update(deltaTime);
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
        
        // PaintCanvasのイベントを購読（プレイヤーが塗った直後にdefendedProgressを更新）
        if (paintCanvas != null)
        {
            area.SubscribeToPaintCanvas(paintCanvas);
        }
        
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
        // activeAreasから削除（重複チェック）
        if (activeAreas.Contains(area))
        {
            activeAreas.Remove(area);
        }
        
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
        
        // 領域を削除
        if (area != null)
        {
            Destroy(area.gameObject);
        }
    }
    
    /// <summary>
    /// 領域を完全に防げた時の処理
    /// </summary>
    private void HandleAreaDefended(ColorChangeArea area)
    {
        Debug.Log($"[ColorDefenseMode] HandleAreaDefended - 呼び出されました: defendedProgress={area.DefendedProgress:F4}, defenseThreshold={settings.defenseThreshold:F4}");
        
        // activeAreasから削除（重複チェック）
        if (activeAreas.Contains(area))
        {
            activeAreas.Remove(area);
        }
        
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
        
        int previousScore = currentScore;
        currentScore += baseScore + comboBonus;
        
        // イベント発火
        OnAreaDefended?.Invoke(area);
        OnScoreUpdated?.Invoke(currentScore);
        OnComboUpdated?.Invoke(currentCombo);
        
        Debug.Log($"[ColorDefenseMode] HandleAreaDefended - スコア加算: 前回={previousScore}, 加算={baseScore + comboBonus} (基本={baseScore}, コンボ={comboBonus}), 現在={currentScore}, コンボ={currentCombo}");
        
        // 領域を削除
        if (area != null)
        {
            Destroy(area.gameObject);
        }
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
    /// 有効な塗り終わるまでの時間を取得
    /// </summary>
    private float GetEffectiveTimeToComplete(DifficultyPhase phase)
    {
        if (settings == null) return 10f; // デフォルト値
        
        if (settings.scalingMode == DifficultyScalingMode.TimeBased && phase != null)
        {
            // フェーズでtimeToCompleteが指定されている場合はそれを使用
            if (phase.timeToComplete > 0f)
            {
                return phase.timeToComplete;
            }
        }
        
        // デフォルト値を使用
        return settings.timeToComplete;
    }
    
    /// <summary>
    /// 有効な色変化速度を取得（後方互換性のため残しています）
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
        
        // 終了時のプレイヤー／敵ピクセル数を集計して勝敗判定
        GameResult result = GameResult.Draw;
        float playerRatio = 0f;
        float enemyRatio = 0f;
        
        if (paintCanvas != null)
        {
            paintCanvas.GetPlayerAndEnemyPixelCounts(out int playerPixels, out int enemyPixels);
            int total = playerPixels + enemyPixels;
            if (total > 0)
            {
                playerRatio = (float)playerPixels / total;
                enemyRatio = (float)enemyPixels / total;
            }
            
            if (playerPixels > enemyPixels)
            {
                result = GameResult.PlayerWin;
            }
            else if (playerPixels < enemyPixels)
            {
                result = GameResult.EnemyWin;
            }
            else
            {
                result = GameResult.Draw;
            }
        }
        
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
        
        // 敵ペンをクリア
        enemyPainters.Clear();
        
        Debug.Log($"ColorDefenseMode: ゲーム終了 - 最終スコア: {currentScore}, 結果={result}, PlayerRatio={playerRatio:P1}, EnemyRatio={enemyRatio:P1}");
        
        // 勝敗結果イベントを発火
        OnGameEnded?.Invoke(result, playerRatio, enemyRatio);
    }
    
    /// <summary>
    /// 敵ペンを設定に基づいて初期化
    /// </summary>
    private void InitializeEnemyPainters()
    {
        enemyPainters.Clear();
        
        if (paintCanvas == null || settings == null)
        {
            return;
        }
        
        int count = Mathf.Max(1, settings.enemyPainterCount);
        for (int i = 0; i < count; i++)
        {
            var painter = new EnemyPainter(paintCanvas, settings);
            enemyPainters.Add(painter);
        }
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
    
    /// <summary>
    /// 現在アクティブな領域数を取得
    /// </summary>
    public int GetActiveAreasCount() => activeAreas.Count;
    
    void OnDestroy()
    {
        EndGame();
    }
}

