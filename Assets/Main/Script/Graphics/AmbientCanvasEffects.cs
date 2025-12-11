using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// キャンバスの縁にアンビエントな波形エフェクトを追加するコンポーネント
/// 指定したRectTransformのサイズに合わせて、上下左右の縁全体に波形が流れる
/// </summary>
public class AmbientCanvasEffects : MonoBehaviour
{
    [Header("Target Canvas")]
    [Tooltip("エフェクトを配置する対象のRectTransform（キャンバスなど）")]
    [SerializeField] private RectTransform targetRectTransform;
    
    [Header("Waveform Settings")]
    [Tooltip("波形の基本強度（目立たせる場合は0.5-2.0程度）")]
    [Range(0f, 2f)]
    [SerializeField] private float waveformIntensity = 0.05f;
    
    [Tooltip("波形の移動速度")]
    [Range(0f, 10f)]
    [SerializeField] private float waveformSpeed = 1.0f;
    
    [Tooltip("波形の色")]
    [SerializeField] private Color waveformColor = new Color(0.5f, 1f, 1f, 1f);
    
    [Tooltip("縁の幅（ピクセル単位）")]
    [Range(1f, 100f)]
    [SerializeField] private float borderWidth = 3f;
    
    [Header("Audio Reactive")]
    [Tooltip("音声反応の強度倍率")]
    [Range(0f, 10f)]
    [SerializeField] private float audioReactiveIntensity = 1.5f;
    
    [Header("Bar Pattern")]
    [Tooltip("放射状に伸びるバーの数")]
    [Range(8f, 128f)]
    [SerializeField] private float barCount = 32f;
    
    [Tooltip("バーの幅（角度単位）")]
    [Range(0.5f, 10f)]
    [SerializeField] private float barWidth = 2f;
    
    [Tooltip("バーの最大長さ（正規化値、0-1）")]
    [Range(0.1f, 1f)]
    [SerializeField] private float maxBarLength = 0.3f;
    
    [Header("References")]
    [Tooltip("VolumeAnalyzer（自動検索される）")]
    [SerializeField] private VolumeAnalyzer volumeAnalyzer;
    
    [Tooltip("使用するシェーダー（設定されていない場合は自動検索）")]
    [SerializeField] private Shader waveformShader;
    
    private Image waveformImage;
    private Material waveformMaterial;
    private RectTransform waveformRectTransform;
    private Canvas parentCanvas;
    private bool isInitialized = false;
    private Vector2 lastTargetSize = Vector2.zero;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        // targetRectTransformの取得
        if (targetRectTransform == null)
        {
            // VoiceToScreenMapperから取得を試みる
            VoiceToScreenMapper mapper = FindObjectOfType<VoiceToScreenMapper>();
            if (mapper != null)
            {
                targetRectTransform = mapper.targetRectTransform;
            }
            
            if (targetRectTransform == null)
            {
                Debug.LogError("AmbientCanvasEffects: targetRectTransformが設定されていません。");
                return;
            }
        }
        
        // VolumeAnalyzerの取得
        if (volumeAnalyzer == null)
        {
            volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
            if (volumeAnalyzer == null)
            {
                Debug.LogWarning("AmbientCanvasEffects: VolumeAnalyzerが見つかりません。音声反応機能は無効になります。");
            }
        }
        
        // シェーダーの取得
        if (waveformShader == null)
        {
            waveformShader = Shader.Find("Custom/AmbientWaveform");
            if (waveformShader == null)
            {
                Debug.LogError("AmbientCanvasEffects: シェーダー 'Custom/AmbientWaveform' が見つかりません。");
                return;
            }
        }
        
        // 親Canvasの取得
        parentCanvas = targetRectTransform.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("AmbientCanvasEffects: 親Canvasが見つかりません。");
            return;
        }
        
        // 波形Imageの作成
        CreateWaveformImage();
        
        // マテリアルの作成
        waveformMaterial = new Material(waveformShader);
        waveformImage.material = waveformMaterial;
        
        // 初期設定の適用
        ApplySettings();
        
        // 初期サイズを記録
        lastTargetSize = targetRectTransform.sizeDelta;
        
        isInitialized = true;
    }
    
    void CreateWaveformImage()
    {
        // 波形Image用のGameObjectを作成
        GameObject waveformObj = new GameObject("AmbientWaveform", typeof(RectTransform), typeof(Image));
        
        // targetRectTransformの親の下に配置
        waveformObj.transform.SetParent(targetRectTransform.parent, false);
        
        waveformImage = waveformObj.GetComponent<Image>();
        waveformRectTransform = waveformObj.GetComponent<RectTransform>();
        
        // Imageの設定
        waveformImage.color = Color.white;
        waveformImage.raycastTarget = false; // クリック判定を無効化
        
        // RectTransformの設定（targetRectTransformと同じサイズ+縁の幅分）
        UpdateWaveformSize();
        
        // targetRectTransformの直前に配置（Hierarchy上で上に配置すると、画面では下に描画される）
        // これにより、波形がキャンバスの下に表示され、外側の縁が見えるようになる
        int targetIndex = targetRectTransform.GetSiblingIndex();
        waveformRectTransform.SetSiblingIndex(targetIndex);
    }
    
    void UpdateWaveformSize()
    {
        if (waveformRectTransform == null || targetRectTransform == null)
        {
            return;
        }
        
        // targetRectTransformのサイズを取得
        Vector2 targetSize = targetRectTransform.sizeDelta;
        
        // 縁の幅分を追加（両側なので2倍）
        // Imageをキャンバスより大きくして、外側の縁を表示できるようにする
        float borderWidthPixels = borderWidth * 2f;
        Vector2 waveformSize = targetSize + new Vector2(borderWidthPixels, borderWidthPixels);
        
        // RectTransformの設定
        waveformRectTransform.anchorMin = targetRectTransform.anchorMin;
        waveformRectTransform.anchorMax = targetRectTransform.anchorMax;
        waveformRectTransform.pivot = targetRectTransform.pivot;
        // 位置はキャンバスと同じ（外側に配置するため）
        waveformRectTransform.anchoredPosition = targetRectTransform.anchoredPosition;
        waveformRectTransform.sizeDelta = waveformSize;
        
        // レイヤー順序を調整（targetRectTransformの直前に配置）
        // UIのレンダリング順序では、Hierarchy上で上にある要素が下に描画される
        // これにより、波形がキャンバスの下に表示され、外側の縁が見えるようになる
        int targetIndex = targetRectTransform.GetSiblingIndex();
        waveformRectTransform.SetSiblingIndex(targetIndex);
        
        // シェーダーにキャンバスのサイズ情報を渡す（外側の縁を判定するため）
        if (waveformMaterial != null)
        {
            // Imageのサイズに対するキャンバスのサイズの比率を計算
            float canvasWidthRatio = targetSize.x / waveformSize.x;
            float canvasHeightRatio = targetSize.y / waveformSize.y;
            waveformMaterial.SetVector("_CanvasSizeRatio", new Vector4(canvasWidthRatio, canvasHeightRatio, 0, 0));
        }
    }
    
    void Update()
    {
        if (!isInitialized || waveformMaterial == null)
        {
            return;
        }
        
        // RectTransformのサイズ変更を検知
        Vector2 currentSize = targetRectTransform.sizeDelta;
        if (currentSize != lastTargetSize)
        {
            UpdateWaveformSize();
            lastTargetSize = currentSize;
        }
        
        // 設定の適用（Inspectorで変更された場合に対応）
        ApplySettings();
        
        // 音声入力の音量を取得してシェーダーに反映
        if (volumeAnalyzer != null)
        {
            float volume = volumeAnalyzer.CurrentVolume;
            waveformMaterial.SetFloat("_AudioVolume", volume);
        }
    }
    
    /// <summary>
    /// 設定をシェーダーに適用
    /// </summary>
    void ApplySettings()
    {
        if (waveformMaterial == null)
        {
            return;
        }
        
        waveformMaterial.SetFloat("_WaveformIntensity", waveformIntensity);
        waveformMaterial.SetFloat("_WaveformSpeed", waveformSpeed);
        waveformMaterial.SetColor("_WaveformColor", waveformColor);
        waveformMaterial.SetFloat("_BorderWidth", borderWidth);
        waveformMaterial.SetFloat("_AudioReactiveIntensity", audioReactiveIntensity);
        waveformMaterial.SetFloat("_BarCount", barCount);
        waveformMaterial.SetFloat("_BarWidth", barWidth);
        waveformMaterial.SetFloat("_MaxBarLength", maxBarLength);
        
        // キャンバスのサイズ比率を更新（UpdateWaveformSizeで設定されるが、念のため）
        if (targetRectTransform != null && waveformRectTransform != null)
        {
            Vector2 targetSize = targetRectTransform.sizeDelta;
            Vector2 waveformSize = waveformRectTransform.sizeDelta;
            if (waveformSize.x > 0 && waveformSize.y > 0)
            {
                float canvasWidthRatio = targetSize.x / waveformSize.x;
                float canvasHeightRatio = targetSize.y / waveformSize.y;
                waveformMaterial.SetVector("_CanvasSizeRatio", new Vector4(canvasWidthRatio, canvasHeightRatio, 0, 0));
            }
        }
    }
    
    /// <summary>
    /// 設定を変更（実行時に動的に変更可能）
    /// </summary>
    public void SetTargetRectTransform(RectTransform rectTransform)
    {
        targetRectTransform = rectTransform;
        if (isInitialized)
        {
            UpdateWaveformSize();
        }
    }
    
    void OnDestroy()
    {
        // マテリアルのクリーンアップ
        if (waveformMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(waveformMaterial);
            }
            else
            {
                DestroyImmediate(waveformMaterial);
            }
        }
    }
    
    void OnValidate()
    {
        // エディタで設定が変更された場合に適用
        if (isInitialized && waveformMaterial != null)
        {
            ApplySettings();
            UpdateWaveformSize();
        }
    }
}

