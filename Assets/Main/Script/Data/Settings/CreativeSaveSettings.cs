using UnityEngine;

/// <summary>
/// クリエイティブモードの保存設定
/// </summary>
[CreateAssetMenu(fileName = "CreativeSaveSettings", menuName = "Game/Creative/Save Settings")]
public class CreativeSaveSettings : ScriptableObject
{
    [Header("Save Path")]
    [Tooltip("保存先ディレクトリ名（Application.persistentDataPath配下）")]
    public string saveDirectory = "Screenshots";
    
    [Tooltip("ファイル名のフォーマット（{0}にタイムスタンプが入る）")]
    public string fileNameFormat = "Creative_{0:yyyyMMdd_HHmmss}.png";
    
    [Tooltip("タイムスタンプを含めるか")]
    public bool includeTimestamp = true;
    
    [Tooltip("デフォルトファイル名（タイムスタンプなしの場合）")]
    public string defaultFileName = "CreativeDrawing.png";
    
    [Header("Image Properties")]
    [Tooltip("保存時の画像スケール（1.0 = 元のサイズ）")]
    [Range(0.1f, 2f)] 
    public float imageScale = 1f;
}

