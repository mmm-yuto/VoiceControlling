using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 色選択システム
/// プリセット色パレットまたはカラーピッカーで色を選択
/// </summary>
public class ColorSelectionSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ColorSelectionSettings settings;
    
    [Header("Current State")]
    [Tooltip("現在選択されている色")]
    [SerializeField] private Color currentColor;
    
    [Tooltip("現在選択されているプリセット色のインデックス")]
    [SerializeField] private int currentPresetIndex = 0;
    
    // イベント
    public event System.Action<Color> OnColorChanged;
    public UnityEvent<Color> OnColorChangedUnityEvent; // UnityEvent版（Inspectorで接続可能）
    
    void Start()
    {
        if (settings == null)
        {
            Debug.LogError("ColorSelectionSystem: ColorSelectionSettingsが設定されていません");
            return;
        }
        
        // 初期色を設定
        if (settings.presetColors != null && settings.presetColors.Length > 0)
        {
            currentColor = settings.presetColors[0];
            currentPresetIndex = 0;
        }
        else
        {
            currentColor = Color.white;
        }
        
        // イベント発火
        NotifyColorChanged();
    }
    
    /// <summary>
    /// 色を設定
    /// </summary>
    public void SetColor(Color color)
    {
        currentColor = color;
        NotifyColorChanged();
    }
    
    /// <summary>
    /// プリセット色を選択
    /// </summary>
    public void SelectPresetColor(int index)
    {
        Debug.Log($"ColorSelectionSystem.SelectPresetColor called with index={index}");
        if (settings == null || settings.presetColors == null || 
            index < 0 || index >= settings.presetColors.Length)
        {
            Debug.LogWarning($"ColorSelectionSystem: 無効なプリセット色インデックス: {index} (settings={settings}, presetColors length={settings?.presetColors?.Length ?? 0})");
            return;
        }
        
        currentPresetIndex = index;
        currentColor = settings.presetColors[index];
        Debug.Log($"ColorSelectionSystem: Color changed to {currentColor}");
        NotifyColorChanged();
    }
    
    /// <summary>
    /// 次のプリセット色に移動
    /// </summary>
    public void NextPresetColor()
    {
        if (settings == null || settings.presetColors == null || settings.presetColors.Length == 0)
        {
            return;
        }
        
        currentPresetIndex = (currentPresetIndex + 1) % settings.presetColors.Length;
        currentColor = settings.presetColors[currentPresetIndex];
        NotifyColorChanged();
    }
    
    /// <summary>
    /// 前のプリセット色に移動
    /// </summary>
    public void PreviousPresetColor()
    {
        if (settings == null || settings.presetColors == null || settings.presetColors.Length == 0)
        {
            return;
        }
        
        currentPresetIndex = (currentPresetIndex - 1 + settings.presetColors.Length) % settings.presetColors.Length;
        currentColor = settings.presetColors[currentPresetIndex];
        NotifyColorChanged();
    }
    
    /// <summary>
    /// 現在の色を取得
    /// </summary>
    public Color GetCurrentColor()
    {
        return currentColor;
    }
    
    /// <summary>
    /// プリセット色のリストを取得
    /// </summary>
    public Color[] GetPresetColors()
    {
        if (settings == null || settings.presetColors == null)
        {
            return new Color[0];
        }
        return settings.presetColors;
    }
    
    /// <summary>
    /// 現在のプリセット色インデックスを取得
    /// </summary>
    public int GetCurrentPresetIndex()
    {
        return currentPresetIndex;
    }
    
    private void NotifyColorChanged()
    {
        Debug.Log($"ColorSelectionSystem: NotifyColorChanged - Color={currentColor}, Subscribers={OnColorChanged?.GetInvocationList().Length ?? 0}");
        OnColorChanged?.Invoke(currentColor);
        OnColorChangedUnityEvent?.Invoke(currentColor);
    }
}

