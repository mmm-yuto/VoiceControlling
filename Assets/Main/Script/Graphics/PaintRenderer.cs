using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 塗りキャンバスをTexture2Dに変換してUI Imageに表示
/// </summary>
public class PaintRenderer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Tooltip("表示用のUI Image（Inspectorで接続）")]
    [SerializeField] private Image displayImage;
    
    [Header("Settings")]
    [Tooltip("テクスチャ更新頻度（フレーム数）")]
    [Range(1, 10)]
    [SerializeField] private int updateFrequency = 1;
    
    // 内部状態
    private int frameCount = 0;
    private Sprite canvasSprite;
    
    void Start()
    {
        // 参照の自動検索
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
            if (paintCanvas == null)
            {
                Debug.LogError("PaintRenderer: PaintCanvasが見つかりません");
            }
        }
        
        if (displayImage == null)
        {
            displayImage = GetComponent<Image>();
            if (displayImage == null)
            {
                Debug.LogError("PaintRenderer: Imageコンポーネントが見つかりません");
            }
        }
        
        // 初期テクスチャを作成
        UpdateTexture();
    }
    
    void Update()
    {
        // 更新頻度チェック
        frameCount++;
        if (frameCount % updateFrequency != 0)
        {
            return;
        }
        
        // テクスチャを更新
        UpdateTexture();
    }
    
    /// <summary>
    /// テクスチャを更新
    /// </summary>
    private void UpdateTexture()
    {
        if (paintCanvas == null || displayImage == null) return;
        
        Texture2D texture = paintCanvas.GetTexture();
        if (texture == null) return;
        
        // 既存のSpriteを破棄
        if (canvasSprite != null)
        {
            Destroy(canvasSprite);
        }
        
        // 新しいSpriteを作成
        canvasSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
        
        // Imageに設定
        displayImage.sprite = canvasSprite;
    }
    
    void OnDestroy()
    {
        // リソースをクリーンアップ
        if (canvasSprite != null)
        {
            Destroy(canvasSprite);
        }
    }
}

