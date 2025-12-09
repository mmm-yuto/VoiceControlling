using UnityEngine;

/// <summary>
/// ペイントスプラッター背景の設定を管理するScriptableObject
/// Inspectorで調整可能
/// </summary>
[CreateAssetMenu(fileName = "PaintSplatterBackgroundSettings", menuName = "Game/Graphics/Paint Splatter Background Settings")]
public class PaintSplatterBackgroundSettings : ScriptableObject
{
    [Header("Animation")]
    [Tooltip("アニメーション速度の倍率")]
    [Range(0f, 5f)]
    public float timeScale = 1f;
    
    [Tooltip("スクロール速度（X, Y）")]
    public Vector2 scrollSpeed = new Vector2(0.1f, 0.1f);
    
    [Tooltip("回転速度")]
    [Range(0f, 2f)]
    public float rotationSpeed = 0.5f;
    
    [Header("Fade Animation")]
    [Tooltip("フェードイン/フェードアウトの速度")]
    [Range(0.1f, 2f)]
    public float fadeSpeed = 0.5f;
    
    [Tooltip("フェードの周期（秒）")]
    [Range(2f, 20f)]
    public float fadeCycle = 8f;
    
    [Header("Splatter Properties")]
    [Tooltip("スプラッターの数")]
    [Range(1, 20)]
    public int splatterCount = 8;
    
    [Tooltip("スプラッターの色（最大5色）")]
    public Color[] splatterColors = new Color[] 
    { 
        new Color(1f, 0f, 1f, 1f),      // Magenta
        new Color(1f, 1f, 0f, 1f),      // Yellow
        new Color(0f, 0.5f, 1f, 1f),    // Bright Blue
        new Color(1f, 0.5f, 0f, 1f),    // Orange
        new Color(0.5f, 1f, 0f, 1f)     // Bright Green
    };
    
    [Tooltip("スプラッターのサイズ範囲（最小、最大）")]
    public Vector2 splatterSizeRange = new Vector2(0.2f, 0.8f);
    
    [Tooltip("スプラッター同士のブレンド量（0=不透明、1=透明）")]
    [Range(0f, 1f)]
    public float blendAmount = 0.3f;
    
    [Header("Background")]
    [Tooltip("背景色")]
    public Color backgroundColor = Color.black; // Pure black
    
    void OnValidate()
    {
        // 色配列のサイズを5に固定
        if (splatterColors == null || splatterColors.Length != 5)
        {
            Color[] newColors = new Color[5];
            if (splatterColors != null)
            {
                for (int i = 0; i < Mathf.Min(splatterColors.Length, 5); i++)
                {
                    newColors[i] = splatterColors[i];
                }
            }
            // デフォルト色を設定（明るい鮮やかな色）
            if (splatterColors == null || splatterColors.Length == 0)
            {
                newColors[0] = new Color(1f, 0f, 1f, 1f);      // Magenta
                newColors[1] = new Color(1f, 1f, 0f, 1f);      // Yellow
                newColors[2] = new Color(0f, 0.5f, 1f, 1f);    // Bright Blue
                newColors[3] = new Color(1f, 0.5f, 0f, 1f);    // Orange
                newColors[4] = new Color(0.5f, 1f, 0f, 1f);     // Bright Green
            }
            splatterColors = newColors;
        }
        
        // サイズ範囲の検証
        if (splatterSizeRange.x > splatterSizeRange.y)
        {
            float temp = splatterSizeRange.x;
            splatterSizeRange.x = splatterSizeRange.y;
            splatterSizeRange.y = temp;
        }
    }
}

