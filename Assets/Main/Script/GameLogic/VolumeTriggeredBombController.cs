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

    // 直近フレームでの声の照準位置
    private Vector2 lastAimedScreenPosition = Vector2.zero;

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
            // 声の照準位置を更新
            UpdateAimedPosition();

            // 残り時間を減少
            countdownRemaining -= Time.deltaTime;

            // UI更新
            UpdateCountdownUI();

            // カウントダウン完了判定
            if (countdownRemaining <= 0f)
            {
                // 最終的な照準位置で爆発
                ExplodeAt(lastAimedScreenPosition);

                isCountingDown = false;
                countdownRemaining = 0f;
                cooldownRemaining = cooldownTime;

                // カウントダウンUIを非表示
                if (useCountdownText && countdownTextUI != null)
                {
                    countdownTextUI.gameObject.SetActive(false);
                }
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

        // カウントダウンUI初期化
        if (useCountdownText && countdownTextUI != null)
        {
            countdownTextUI.gameObject.SetActive(true);
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

        // テキストの内容を更新
        float ratio = Mathf.Clamp01(countdownRemaining / countdownDuration);

        if (countdownSteps <= 0)
        {
            countdownSteps = 3;
        }

        int step = Mathf.CeilToInt(ratio * countdownSteps);

        if (step <= 0)
        {
            // 最後の瞬間は空白か「0」にしても良い
            countdownTextUI.text = "0";
        }
        else
        {
            countdownTextUI.text = step.ToString();
        }

        // テキストの位置を「プレイヤーの声の位置（塗られている場所）」に合わせる
        UpdateCountdownTextPosition();
    }

    /// <summary>
    /// カウントダウンテキストの位置を、プレイヤーの声の位置（塗られている場所）に合わせて更新する。
    /// </summary>
    private void UpdateCountdownTextPosition()
    {
        if (countdownTextUI == null) return;

        RectTransform rectTransform = countdownTextUI.rectTransform;
        if (rectTransform == null) return;

        // 画面座標を UI 座標に変換するために Canvas を取得
        Canvas canvas = countdownTextUI.canvas;
        if (canvas == null)
        {
            // Canvas が見つからない場合は、親から探す
            canvas = countdownTextUI.GetComponentInParent<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning("VolumeTriggeredBombController: カウントダウンテキストの Canvas が見つかりません。位置を更新できません。");
            return;
        }

        // 画面座標（lastAimedScreenPosition）を UI 座標に変換
        Vector2 uiPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            lastAimedScreenPosition,
            canvas.worldCamera,
            out uiPosition))
        {
            rectTransform.anchoredPosition = uiPosition;
        }
        else
        {
            // 変換に失敗した場合は、画面座標をそのまま使用（Screen Space - Overlay の場合）
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                rectTransform.position = lastAimedScreenPosition;
            }
        }
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

        // PaintCanvas の PaintAtWithRadiusForced を使用して円形塗りを実行
        // 更新頻度チェックをスキップして、必ず塗り処理を実行する
        paintCanvas.PaintAtWithRadiusForced(screenPosition, bombPlayerId, bombIntensity, finalBombColor, bombRadius);
    }
}


