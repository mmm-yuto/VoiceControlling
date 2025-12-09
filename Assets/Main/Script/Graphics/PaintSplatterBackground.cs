using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ペイントスプラッター背景を管理するコンポーネント
/// シェーダーを適用したUI Imageを管理し、設定からプロパティを更新
/// </summary>
public class PaintSplatterBackground : MonoBehaviour
{
    [Header("References")]
    [Tooltip("背景表示用のUI Image（自動検索される）")]
    [SerializeField] private Image backgroundImage;
    
    [Header("Settings")]
    [Tooltip("背景の設定（ScriptableObject）")]
    [SerializeField] private PaintSplatterBackgroundSettings settings;
    
    [Header("Shader")]
    [Tooltip("使用するシェーダー（設定されていない場合は自動検索）")]
    [SerializeField] private Shader backgroundShader;
    
    private Material backgroundMaterial;
    private bool isInitialized = false;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        // Imageコンポーネントの取得
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
            if (backgroundImage == null)
            {
                Debug.LogError("PaintSplatterBackground: Imageコンポーネントが見つかりません。");
                return;
            }
        }
        
        // シェーダーの取得
        if (backgroundShader == null)
        {
            backgroundShader = Shader.Find("Custom/PaintSplatterBackground");
            if (backgroundShader == null)
            {
                Debug.LogError("PaintSplatterBackground: シェーダー 'Custom/PaintSplatterBackground' が見つかりません。");
                return;
            }
        }
        
        // マテリアルの作成
        backgroundMaterial = new Material(backgroundShader);
        backgroundImage.material = backgroundMaterial;
        
        // 設定の適用
        if (settings != null)
        {
            ApplySettings();
        }
        else
        {
            Debug.LogWarning("PaintSplatterBackground: 設定が設定されていません。デフォルト値を使用します。");
        }
        
        isInitialized = true;
    }
    
    void Update()
    {
        if (!isInitialized || backgroundMaterial == null)
        {
            return;
        }
        
        // 時間ベースのアニメーションはシェーダー内で処理されるため、
        // 設定が変更された場合のみ更新
        if (settings != null)
        {
            ApplySettings();
        }
    }
    
    /// <summary>
    /// 設定をシェーダーに適用
    /// </summary>
    private void ApplySettings()
    {
        if (backgroundMaterial == null || settings == null)
        {
            return;
        }
        
        // アニメーションプロパティ
        backgroundMaterial.SetFloat("_TimeScale", settings.timeScale);
        
        // スプラッタープロパティ
        backgroundMaterial.SetFloat("_SplatterCount", settings.splatterCount);
        backgroundMaterial.SetFloat("_SplatterSizeMin", settings.splatterSizeRange.x);
        backgroundMaterial.SetFloat("_SplatterSizeMax", settings.splatterSizeRange.y);
        backgroundMaterial.SetFloat("_BlendAmount", settings.blendAmount);
        
        // CRTエミュレーションプロパティ
        backgroundMaterial.SetFloat("_ScanlineIntensity", settings.scanlineIntensity);
        backgroundMaterial.SetFloat("_ScanlineSpeed", settings.scanlineSpeed);
        backgroundMaterial.SetFloat("_ChromaticAberration", settings.chromaticAberration);
        backgroundMaterial.SetFloat("_ScreenCurvature", settings.screenCurvature);
        backgroundMaterial.SetFloat("_Brightness", settings.brightness);
        backgroundMaterial.SetFloat("_Contrast", settings.contrast);
        
        // 色プロパティ
        if (settings.splatterColors != null && settings.splatterColors.Length >= 5)
        {
            backgroundMaterial.SetColor("_Color1", settings.splatterColors[0]);
            backgroundMaterial.SetColor("_Color2", settings.splatterColors[1]);
            backgroundMaterial.SetColor("_Color3", settings.splatterColors[2]);
            backgroundMaterial.SetColor("_Color4", settings.splatterColors[3]);
            backgroundMaterial.SetColor("_Color5", settings.splatterColors[4]);
        }
        
        // 背景色
        backgroundMaterial.SetColor("_BackgroundColor", settings.backgroundColor);
    }
    
    /// <summary>
    /// 設定を変更（実行時に動的に変更可能）
    /// </summary>
    public void SetSettings(PaintSplatterBackgroundSettings newSettings)
    {
        settings = newSettings;
        if (isInitialized)
        {
            ApplySettings();
        }
    }
    
    void OnDestroy()
    {
        // マテリアルのクリーンアップ
        if (backgroundMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(backgroundMaterial);
            }
            else
            {
                DestroyImmediate(backgroundMaterial);
            }
        }
    }
    
    void OnValidate()
    {
        // エディタで設定が変更された場合に適用
        if (isInitialized && settings != null)
        {
            ApplySettings();
        }
    }
}

