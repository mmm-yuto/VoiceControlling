using UnityEngine;
using TMPro;

/// <summary>
/// 音量しきい値を超えたときにカウントダウンを開始し、
/// カウントダウン終了時点の声の照準位置で爆発塗りを行うコントローラ。
/// </summary>
public class VolumeTriggeredBombController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("塗りキャンバスへの参照")]
    public PaintCanvas paintCanvas;

    [Tooltip("音声→画面座標マッピングへの参照")]
    public VoiceToScreenMapper voiceToScreenMapper;

    [Tooltip("音量解析コンポーネント（現在の音量取得用・必須）")]
    public VolumeAnalyzer volumeAnalyzer;

    [Tooltip("クリエイティブモードのマネージャー（Bombブラシ選択状態の判定に使用、任意）")]
    public CreativeModeManager creativeModeManager;

    [Tooltip("音声入力ハンドラ（現在の声の照準位置を取得するため、任意）")]
    public VoiceInputHandler voiceInputHandler;

    [Tooltip("PaintRenderer（画面座標をImageのローカル座標に変換するために使用）")]
    public PaintRenderer paintRenderer;

    [Header("Bomb Settings")]
    [Tooltip("カウントダウン時間（秒）")]
    public float countdownDuration = 3f;

    [Tooltip("一度爆発した後、次にカウントダウンを開始できるまでのクールダウン時間（秒）")]
    public float cooldownTime = 1.0f;

    [Tooltip("爆発時の塗り半径（画面座標、ピクセル単位）")]
    public float bombRadius = 150f;

    [Tooltip("爆発時の塗り強度（PaintCanvas側のしきい値に掛ける）")]
    public float bombIntensity = 1.0f;

    [Tooltip("爆発時に使用するプレイヤーID（>0でプレイヤー、-1で敵など）")]
    public int bombPlayerId = 1;

    [Tooltip("爆発時に使用する色")]
    public Color bombColor = Color.cyan;

    [Header("Countdown UI")]
    [Tooltip("カウントダウンを表示するかどうか")]
    public bool useCountdownText = true;

    [Tooltip("カウントダウン表示用テキスト（3,2,1など）")]
    public TextMeshProUGUI countdownTextUI;

    [Tooltip("カウントダウンを何段階で表示するか（例: 3なら3,2,1）")]
    public int countdownSteps = 3;

    [Tooltip("Bombブラシ選択時のみこのコントローラを有効にするか")]
    public bool onlyWhenBombBrushSelected = true;

    // 状態管理用
    private bool isCountingDown = false;
    private float countdownRemaining = 0f;
    private float cooldownRemaining = 0f;
    
    /// <summary>
    /// カウントダウン中かどうかを取得（外部から参照可能）
    /// </summary>
    public bool IsCountingDown => isCountingDown;
    
    /// <summary>
    /// 爆発中かどうかを取得（外部から参照可能）
    /// </summary>
    public bool IsExploding => isExploding;

    // 直近フレームでの声の照準位置
    private Vector2 lastAimedScreenPosition = Vector2.zero;
    
    // 爆発位置の記憶（カウントダウン開始時点の位置を記憶）
    private Vector2 explosionStartPosition = Vector2.zero;
    
    // 爆発中の状態管理
    private bool isExploding = false;
    
    // 現在の爆発位置（爆発中に固定される）
    private Vector2 currentExplosionPosition = Vector2.zero;

    // 座標変換処理のキャッシュ用
    private RectTransform cachedPaintRendererRect = null;
    private PaintSettings cachedPaintSettings = null;
    private Canvas cachedCanvas = null;
    private RectTransform cachedCountdownRectTransform = null;
    private int lastCachedFrame = -1;

    // 位置更新の間引き用
    private int frameCountForPositionUpdate = 0;
    private const int POSITION_UPDATE_INTERVAL = 5; // 5フレームに1回更新
    private Vector2 lastUpdatedTextPosition = Vector2.zero;
    private const float POSITION_UPDATE_THRESHOLD = 20f; // 20ピクセル以上動いたら更新

    // 外部から手動で現在音量を渡したい場合に使う（volumeAnalyzerが無い環境向け）
    [HideInInspector]
    public float externalCurrentVolume = 0f;

    // PaintSettings への参照（塗りシステムと同じしきい値を使うため）
    private PaintSettings paintSettings;

    void Awake()
    {
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
        }

        if (voiceToScreenMapper == null)
        {
            voiceToScreenMapper = FindObjectOfType<VoiceToScreenMapper>();
        }

        if (volumeAnalyzer == null)
        {
            volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        }

        if (creativeModeManager == null)
        {
            creativeModeManager = FindObjectOfType<CreativeModeManager>();
        }

        if (voiceInputHandler == null)
        {
            voiceInputHandler = FindObjectOfType<VoiceInputHandler>();
        }

        if (paintRenderer == null)
        {
            paintRenderer = FindObjectOfType<PaintRenderer>();
        }

        // PaintSettings を取得（塗りシステムと同じしきい値を使うため）
        if (paintCanvas != null)
        {
            paintSettings = paintCanvas.GetSettings();
        }
    }

    void Start()
    {
        // Start() でも再確認（Awake() で見つからなかった場合のフォールバック）
        if (volumeAnalyzer == null)
        {
            volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
            if (volumeAnalyzer == null)
            {
                Debug.LogError("VolumeTriggeredBombController: VolumeAnalyzer が見つかりません。シーンに VolumeAnalyzer コンポーネントを追加してください。");
            }
            else
            {
                Debug.Log("VolumeTriggeredBombController: VolumeAnalyzer を自動検出しました。");
            }
        }
        else
        {
            Debug.Log("VolumeTriggeredBombController: VolumeAnalyzer が設定されています。");
        }
    }

    void Update()
    {
        // Bombブラシ選択時のみ有効にする設定なら、状態をチェック
        if (onlyWhenBombBrushSelected && creativeModeManager != null)
        {
            BrushStrategyBase currentBrush = creativeModeManager.GetCurrentBrush();

            bool isBombBrushSelected = currentBrush is BombBrush;

            // Bombブラシ以外が選択されている場合は、カウントダウンをリセットして何もしない
            if (!isBombBrushSelected)
            {
                if (isCountingDown)
                {
                    isCountingDown = false;
                    countdownRemaining = 0f;

                    // カウントダウンUIを非表示
                    if (useCountdownText && countdownTextUI != null)
                    {
                        countdownTextUI.gameObject.SetActive(false);
                    }
                }

                // クールダウンタイマーだけは経過させておく
                if (cooldownRemaining > 0f)
                {
                    cooldownRemaining -= Time.deltaTime;
                }

                return;
            }
        }

        // クールダウン処理
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
        }

        // 現在の音量を取得
        float currentVolume = GetCurrentVolume();

        // 爆発中は新しいカウントダウンを開始しない
        if (isExploding)
        {
            return;
        }
        
        // カウントダウンしていない & クールダウン中でない場合に音量しきい値をチェック
        if (!isCountingDown && cooldownRemaining <= 0f)
        {
            // 実際に塗れる音量かどうかを判定（PaintSettingsのしきい値を使用）
            if (IsVolumeSufficientForPainting(currentVolume))
            {
                StartCountdown();
            }
        }

        // カウントダウン中の処理
        if (isCountingDown)
        {
            // 声の照準位置を更新（間引き：5フレームに1回）
            if (Time.frameCount % 5 == 0)
            {
                UpdateAimedPosition();
            }

            // 残り時間を減少
            countdownRemaining -= Time.deltaTime;

            // UI更新（位置更新は最適化して間引き）
            UpdateCountdownUI();

            // カウントダウン完了判定（一度だけ実行されるように、isCountingDownを先にfalseにする）
            // また、既に爆発中の場合は実行しない（重複実行を防ぐ）
            if (countdownRemaining <= 0f && isCountingDown && !isExploding)
            {
                // カウントダウンを先に停止（複数回実行を防ぐ）
                isCountingDown = false;
                
                // 爆発位置を確定（カウントダウン開始時点の位置を優先、無効な場合は現在位置を使用）
                Vector2 explosionPos = explosionStartPosition;
                if (explosionPos == Vector2.zero)
                {
                    explosionPos = lastAimedScreenPosition;
                    if (explosionPos == Vector2.zero && voiceToScreenMapper != null)
                    {
                        explosionPos = voiceToScreenMapper.MapToCenter();
                    }
                    Debug.LogWarning($"VolumeTriggeredBombController: 爆発開始位置が無効だったため、現在位置 ({explosionPos}) を使用します。");
                }
                
                // 爆発位置を固定（爆発中に声の位置が変わっても、この位置を使用）
                currentExplosionPosition = explosionPos;
                
                // 爆発中フラグを立てる（ExplodeAt()の前に設定して、重複実行を防ぐ）
                isExploding = true;
                
                // 記憶した位置（カウントダウン開始時点の位置）で爆発
                // ExplodeAt()は同期的に実行され、全ての粒子の塗り処理が完了するまで待つ
                ExplodeAt(currentExplosionPosition);
                
                // ここまで来た時点で、全ての塗り処理が完了している
                // （BombBrush.Paint()とcanvas.PaintAtWithRadiusForced()は同期的に実行される）

                countdownRemaining = 0f;
                cooldownRemaining = cooldownTime;

                // カウントダウンUIを非表示
                if (useCountdownText && countdownTextUI != null)
                {
                    countdownTextUI.gameObject.SetActive(false);
                }
                
                // キャッシュをクリア
                ClearCache();
                
                // 爆発完了（全ての塗り処理が完了した後、フラグを下ろす）
                isExploding = false;
                currentExplosionPosition = Vector2.zero; // 爆発位置をクリア
            }
        }
    }

    /// <summary>
    /// 現在の音量を取得する。
    /// VolumeAnalyzer から取得（必須）。
    /// </summary>
    private float GetCurrentVolume()
    {
        if (volumeAnalyzer != null)
        {
            return Mathf.Max(0f, volumeAnalyzer.CurrentVolume);
        }

        // VolumeAnalyzer が見つからない場合は警告を出して0を返す
        if (Time.frameCount % 60 == 0) // 1秒に1回だけ警告（ログが多すぎないように）
        {
            Debug.LogWarning("VolumeTriggeredBombController: VolumeAnalyzer が設定されていません。音量を取得できません。");
        }

        return 0f;
    }

    /// <summary>
    /// 現在の音量が「色が塗れる」レベルかどうかを判定する。
    /// PaintSettings の minVolumeThreshold と paintIntensityMultiplier を使用して、
    /// 実際の塗りシステムと同じ基準で判定する。
    /// </summary>
    private bool IsVolumeSufficientForPainting(float currentVolume)
    {
        if (paintSettings != null)
        {
            // PaintCanvas と同じロジック：
            // effectiveIntensity = volume * paintIntensityMultiplier
            // effectiveIntensity >= minVolumeThreshold なら塗れる
            float effectiveIntensity = currentVolume * paintSettings.paintIntensityMultiplier;
            return effectiveIntensity >= paintSettings.minVolumeThreshold;
        }
        else
        {
            // PaintSettings が取得できない場合は、デフォルトのしきい値を使用
            return currentVolume >= 0.01f;
        }
    }

    /// <summary>
    /// カウントダウンを開始する。
    /// </summary>
    private void StartCountdown()
    {
        if (paintCanvas == null || voiceToScreenMapper == null)
        {
            Debug.LogWarning("VolumeTriggeredBombController: 必要な参照 (PaintCanvas / VoiceToScreenMapper) が不足しています。");
            return;
        }

        isCountingDown = true;
        countdownRemaining = countdownDuration;

        // 開始時点でも一度照準位置を更新
        UpdateAimedPosition();

        // 有効な位置が記録されていない場合（Vector2.zero のまま）は、中央位置をセット
        if (lastAimedScreenPosition == Vector2.zero && voiceToScreenMapper != null)
        {
            lastAimedScreenPosition = voiceToScreenMapper.MapToCenter();
            Debug.Log($"VolumeTriggeredBombController: カウントダウン開始時に有効な照準位置がなかったため、中央位置 ({lastAimedScreenPosition}) をセットしました。");
        }
        
        // 爆発開始位置を記憶（カウントダウン開始時点の位置）
        explosionStartPosition = lastAimedScreenPosition;
        Debug.Log($"VolumeTriggeredBombController: カウントダウン開始 - 爆発位置を記憶: ({explosionStartPosition.x:F1}, {explosionStartPosition.y:F1})");

        // カウントダウンUI初期化
        if (useCountdownText && countdownTextUI != null)
        {
            countdownTextUI.gameObject.SetActive(true);
            frameCountForPositionUpdate = 0; // 位置更新カウンターをリセット
            lastUpdatedTextPosition = Vector2.zero; // 位置をリセット
            UpdateCache(); // キャッシュを初期化
            UpdateCountdownUI();
        }
    }

    /// <summary>
    /// 現在の声の照準位置（画面座標）を取得して保持する。
    /// 無音時（IsSilent == true または CurrentScreenPosition == Vector2.zero）は前回の有効な位置を保持する。
    /// </summary>
    private void UpdateAimedPosition()
    {
        // まずは VoiceInputHandler から「現在の声の照準位置」を取得する
        if (voiceInputHandler != null)
        {
            // 有効な位置（無音でない かつ Vector2.zero でない）の場合のみ更新
            if (!voiceInputHandler.IsSilent && voiceInputHandler.CurrentScreenPosition != Vector2.zero)
            {
                lastAimedScreenPosition = voiceInputHandler.CurrentScreenPosition;
                return;
            }
            // 無音時は前回の有効な位置を保持（上書きしない）
            return;
        }

        // VoiceInputHandler がない場合は、VoiceToScreenMapper から中心位置を使用
        // （ただし、既に有効な位置が記録されている場合は上書きしない）
        if (voiceToScreenMapper != null && lastAimedScreenPosition == Vector2.zero)
        {
            lastAimedScreenPosition = voiceToScreenMapper.MapToCenter();
        }
    }

    /// <summary>
    /// カウントダウンUIを更新する（3,2,1表示など）。
    /// テキストの内容と位置（プレイヤーの声の位置）を更新する。
    /// </summary>
    private void UpdateCountdownUI()
    {
        if (!useCountdownText || countdownTextUI == null || countdownDuration <= 0f)
        {
            return;
        }

        // テキストの内容を更新（数値が変わった時だけ更新）
        float ratio = Mathf.Clamp01(countdownRemaining / countdownDuration);

        if (countdownSteps <= 0)
        {
            countdownSteps = 3;
        }

        int step = Mathf.CeilToInt(ratio * countdownSteps);
        
        // テキストが変わった時だけ更新（パフォーマンス最適化）
        string newText = step <= 0 ? "0" : step.ToString();
        if (!countdownTextUI.text.Equals(newText))
        {
            countdownTextUI.text = newText;
        }

        // テキストの位置を「プレイヤーの声の位置（塗られている場所）」に合わせる
        // パフォーマンス最適化：数フレームに1回、または位置が大きく変わった時のみ更新
        frameCountForPositionUpdate++;
        bool shouldUpdatePosition = false;
        
        if (frameCountForPositionUpdate >= POSITION_UPDATE_INTERVAL)
        {
            // 一定フレームごとに更新
            shouldUpdatePosition = true;
            frameCountForPositionUpdate = 0;
        }
        else
        {
            // 位置が大きく変わった場合は即座に更新
            float positionDelta = Vector2.Distance(lastAimedScreenPosition, lastUpdatedTextPosition);
            if (positionDelta >= POSITION_UPDATE_THRESHOLD)
            {
                shouldUpdatePosition = true;
            }
        }
        
        if (shouldUpdatePosition)
        {
            UpdateCountdownTextPosition();
            lastUpdatedTextPosition = lastAimedScreenPosition;
        }
    }

    /// <summary>
    /// カウントダウンテキストの位置を、プレイヤーの声の位置（塗られている場所）に合わせて更新する。
    /// InkEffectと同じ処理で、PaintRendererのImageのサイズを考慮して、正しい位置に配置する。
    /// パフォーマンス最適化：参照をキャッシュして毎回取得しないようにする。
    /// </summary>
    private void UpdateCountdownTextPosition()
    {
        if (countdownTextUI == null) return;

        // キャッシュを更新（フレームごとに1回だけ）
        if (Time.frameCount != lastCachedFrame)
        {
            UpdateCache();
            lastCachedFrame = Time.frameCount;
        }

        if (cachedCountdownRectTransform == null || cachedCanvas == null)
        {
            return;
        }

        // PaintRendererが設定されている場合は、そのImageのローカル座標に変換（InkEffectと同じ処理）
        if (cachedPaintRendererRect != null && cachedPaintSettings != null)
        {
            // 画面座標をテクスチャ座標に変換（0～textureWidth, 0～textureHeight）
            float textureX = (lastAimedScreenPosition.x / Screen.width) * cachedPaintSettings.textureWidth;
            float textureY = (lastAimedScreenPosition.y / Screen.height) * cachedPaintSettings.textureHeight;

            // テクスチャ座標を0～1の範囲に正規化
            float normalizedX = textureX / cachedPaintSettings.textureWidth;
            float normalizedY = textureY / cachedPaintSettings.textureHeight;

            // PaintRendererのImageのRectTransformのサイズを取得
            Vector2 imageSize = cachedPaintRendererRect.rect.size;

            // 正規化された座標をImageのローカル座標に変換
            // pivotが(0.5, 0.5)の場合、中心が(0, 0)なので、-size/2 ～ +size/2 の範囲
            Vector2 localPosInImage = new Vector2(
                (normalizedX - 0.5f) * imageSize.x,
                (normalizedY - 0.5f) * imageSize.y
            );

            // マーカーの親のRectTransformを取得
            RectTransform parentRect = cachedCountdownRectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                // PaintRendererのImageのローカル座標を、カウントダウンテキストの親のローカル座標に変換
                Vector2 imageLocalPosInParent;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    cachedPaintRendererRect.position,
                    cachedCanvas.worldCamera,
                    out imageLocalPosInParent))
                {
                    // Imageの中心位置 + ローカル座標 = カウントダウンテキストの位置
                    cachedCountdownRectTransform.anchoredPosition = imageLocalPosInParent + localPosInImage;
                    return;
                }
            }
        }

        // PaintRendererが設定されていない場合は、従来の方法でUI座標に変換
        Vector2 uiPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cachedCanvas.transform as RectTransform,
            lastAimedScreenPosition,
            cachedCanvas.worldCamera,
            out uiPosition))
        {
            cachedCountdownRectTransform.anchoredPosition = uiPosition;
        }
        else
        {
            // 変換に失敗した場合は、画面座標をそのまま使用（Screen Space - Overlay の場合）
            if (cachedCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                cachedCountdownRectTransform.position = lastAimedScreenPosition;
            }
        }
    }
    
    /// <summary>
    /// 座標変換に使用する参照をキャッシュする（フレームごとに1回だけ）
    /// </summary>
    private void UpdateCache()
    {
        if (countdownTextUI == null)
        {
            ClearCache();
            return;
        }

        cachedCountdownRectTransform = countdownTextUI.rectTransform;
        
        // Canvasを取得
        cachedCanvas = countdownTextUI.canvas;
        if (cachedCanvas == null)
        {
            cachedCanvas = countdownTextUI.GetComponentInParent<Canvas>();
        }

        // PaintRendererのImageのRectTransformを取得
        if (paintRenderer != null)
        {
            cachedPaintRendererRect = paintRenderer.GetDisplayRectTransform();
        }
        else
        {
            cachedPaintRendererRect = null;
        }

        // PaintCanvasの設定を取得
        if (paintCanvas != null)
        {
            cachedPaintSettings = paintCanvas.GetSettings();
        }
        else
        {
            cachedPaintSettings = null;
        }
    }
    
    /// <summary>
    /// キャッシュをクリア
    /// </summary>
    private void ClearCache()
    {
        cachedPaintRendererRect = null;
        cachedPaintSettings = null;
        cachedCanvas = null;
        cachedCountdownRectTransform = null;
        lastCachedFrame = -1;
    }

    /// <summary>
    /// 指定された画面座標で爆発塗りを行う。
    /// 更新頻度チェックをスキップして、必ず塗り処理を実行する。
    /// </summary>
    /// <param name="screenPosition">爆発位置（画面座標）</param>
    private void ExplodeAt(Vector2 screenPosition)
    {
        if (paintCanvas == null)
        {
            Debug.LogWarning("VolumeTriggeredBombController: PaintCanvas が設定されていません。");
            return;
        }

        // 爆発位置が無効な場合の警告
        if (screenPosition == Vector2.zero)
        {
            Debug.LogWarning("VolumeTriggeredBombController: 爆発位置が無効です（Vector2.zero）。中央位置で爆発します。");
            if (voiceToScreenMapper != null)
            {
                screenPosition = voiceToScreenMapper.MapToCenter();
            }
        }

        // CreativeModeManagerから現在の色を取得（設定されている場合）
        Color finalBombColor = bombColor; // デフォルトは設定された色
        if (creativeModeManager != null)
        {
            // CreativeModeManagerの現在の色を取得
            finalBombColor = creativeModeManager.GetCurrentColor();
        }

        // デバッグログ：爆発位置を出力
        Debug.Log($"VolumeTriggeredBombController: 爆発実行 - 位置: ({screenPosition.x:F1}, {screenPosition.y:F1}), 強度: {bombIntensity}, 半径: {bombRadius}, 色: {finalBombColor}");

        // BombBrushを使用してグラデーション塗りを実行
        // CreativeModeManagerからBombBrushを取得するか、直接BombBrushのインスタンスを作成
        BombBrush bombBrush = null;
        if (creativeModeManager != null)
        {
            BrushStrategyBase currentBrush = creativeModeManager.GetCurrentBrush();
            if (currentBrush is BombBrush)
            {
                bombBrush = currentBrush as BombBrush;
            }
        }
        
        // BombBrushが見つからない場合は、一時的なインスタンスを作成
        if (bombBrush == null)
        {
            bombBrush = ScriptableObject.CreateInstance<BombBrush>();
            bombBrush.bombRadius = bombRadius;
        }
        
        // BombBrushのPaint()メソッドを使用してグラデーション塗りを実行
        bombBrush.Paint(paintCanvas, screenPosition, bombPlayerId, finalBombColor, bombIntensity);
    }
}


