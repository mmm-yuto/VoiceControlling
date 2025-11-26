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
            // イベント購読解除
            area.OnProgressChanged -= UpdateAreaVisual;
            area.OnFullyChanged -= RemoveArea;
            area.OnFullyDefended -= RemoveArea;
            
            Destroy(areaIndicators[area]);
            areaIndicators.Remove(area);
        }
    }
    
    void OnDestroy()
    {
        // 全てのイベント購読を解除
        foreach (var kvp in areaIndicators)
        {
            if (kvp.Key != null)
            {
                kvp.Key.OnProgressChanged -= UpdateAreaVisual;
                kvp.Key.OnFullyChanged -= RemoveArea;
                kvp.Key.OnFullyDefended -= RemoveArea;
            }
        }
    }
}

