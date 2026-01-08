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
    
    // 内部状態
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
        
        // 初期Spriteを作成（1回だけ）
        CreateSpriteOnce();
        
        // PaintCanvasのテクスチャ更新イベントを購読
        if (paintCanvas != null)
        {
            paintCanvas.OnTextureUpdated += OnTextureUpdated;
        }
    }
    
    void Update()
    {
        // テクスチャのサイズが変更された場合のみSpriteを再生成
        // 通常のテクスチャ更新（ピクセルデータの変更）は自動的に反映されるため、再生成不要
        UpdateSpriteIfNeeded();
    }
    
    /// <summary>
    /// Spriteを初期化時に1回だけ作成（パフォーマンス最適化）
    /// </summary>
    private void CreateSpriteOnce()
    {
        if (paintCanvas == null || displayImage == null) return;
        
        Texture2D texture = paintCanvas.GetTexture();
        if (texture == null) return;
        
        // Spriteを1回だけ作成
        if (canvasSprite == null)
        {
            canvasSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
            displayImage.sprite = canvasSprite;
        }
    }
    
    /// <summary>
    /// テクスチャのサイズが変更された場合のみSpriteを再生成
    /// 通常のテクスチャ更新（ピクセルデータの変更）はOnTextureUpdatedイベントで処理される
    /// </summary>
    private void UpdateSpriteIfNeeded()
    {
        if (paintCanvas == null || displayImage == null) return;
        
        Texture2D texture = paintCanvas.GetTexture();
        if (texture == null) return;
        
        // Spriteが存在しない場合は作成
        if (canvasSprite == null)
        {
            CreateSpriteOnce();
            return;
        }
        
        // テクスチャのサイズが変更された場合のみSpriteを再生成
        if (canvasSprite.texture.width != texture.width || 
            canvasSprite.texture.height != texture.height)
        {
            // サイズ変更時のみ再生成
            if (canvasSprite != null)
            {
                Destroy(canvasSprite);
            }
            canvasSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
            displayImage.sprite = canvasSprite;
        }
        // それ以外の場合は何もしない（テクスチャの更新は自動的に反映される）
    }
    
    /// <summary>
    /// 表示用のImageコンポーネントを取得（InkEffectなどから参照用）
    /// </summary>
    public Image GetDisplayImage()
    {
        return displayImage;
    }
    
    /// <summary>
    /// 表示用のImageのRectTransformを取得（InkEffectなどから参照用）
    /// </summary>
    public RectTransform GetDisplayRectTransform()
    {
        return displayImage != null ? displayImage.rectTransform : null;
    }
    
    /// <summary>
    /// テクスチャ更新イベントのハンドラ
    /// </summary>
    private void OnTextureUpdated()
    {
        if (displayImage == null || paintCanvas == null) return;
        
        Texture2D texture = paintCanvas.GetTexture();
        if (texture == null) return;
        
        // Spriteを再生成してImageを更新
        if (canvasSprite != null)
        {
            Destroy(canvasSprite);
        }
        
        canvasSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
        displayImage.sprite = canvasSprite;
        
        Debug.Log("[DEBUG] PaintRenderer: テクスチャ更新イベントを受信し、Spriteを再生成しました");
    }
    
    void OnDestroy()
    {
        // イベント購読を解除
        if (paintCanvas != null)
        {
            paintCanvas.OnTextureUpdated -= OnTextureUpdated;
        }
        
        // リソースをクリーンアップ
        if (canvasSprite != null)
        {
            Destroy(canvasSprite);
        }
    }
}

