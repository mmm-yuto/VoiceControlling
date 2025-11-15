using UnityEngine;

/// <summary>
/// 色選択の設定
/// </summary>
[CreateAssetMenu(fileName = "ColorSelectionSettings", menuName = "Game/Color Selection Settings")]
public class ColorSelectionSettings : ScriptableObject
{
    [Header("Selection Method")]
    [Tooltip("色選択方法")]
    public ColorSelectionMethod method = ColorSelectionMethod.PresetPalette;
    
    [Header("Preset Colors")]
    [Tooltip("プリセット色のリスト")]
    public Color[] presetColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        Color.white,
        Color.black
    };
    
    [Header("Color Picker")]
    [Tooltip("カラーピッカーをデフォルトで表示するか")]
    public bool colorPickerVisibleByDefault = false;
}

/// <summary>
/// 色選択方法
/// </summary>
public enum ColorSelectionMethod
{
    PresetPalette,  // プリセット色パレット
    ColorPicker,    // カラーピッカー（Unity標準）
    VoiceSelection  // 音声による色選択（将来的な拡張）
}

