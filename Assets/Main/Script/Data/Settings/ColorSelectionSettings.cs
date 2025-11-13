using UnityEngine;

/// <summary>
/// 色選択システムの設定
/// 選択方式やプリセット色をScriptableObjectで管理
/// </summary>
[CreateAssetMenu(fileName = "ColorSelectionSettings", menuName = "Game/Creative/Color Selection Settings")]
public class ColorSelectionSettings : ScriptableObject
{
    public enum ColorSelectionMethod
    {
        ButtonSelection,
        ColorPicker,
        VoiceSelection,
        PresetPalette
    }

    [Header("General")]
    [Tooltip("色選択の方式")]
    public ColorSelectionMethod method = ColorSelectionMethod.ButtonSelection;

    [Header("Preset Colors")]
    [Tooltip("プリセットとして利用する色")]
    public Color[] presetColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        new Color(1f, 0.5f, 0f), // Orange
        Color.white
    };

    [Header("Color Picker")]
    [Tooltip("カラーピッカーを初期状態で開くか")]
    public bool colorPickerVisibleByDefault = false;

    [Header("Voice Selection")]
    [Tooltip("ピッチによる色選択を行う区間（下限Hz）")]
    public float[] voicePitchThresholds = new float[] { 200f, 300f, 400f, 500f };
}

