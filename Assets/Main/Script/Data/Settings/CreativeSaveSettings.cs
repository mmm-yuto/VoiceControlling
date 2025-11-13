using UnityEngine;

/// <summary>
/// クリエイティブモードの保存設定
/// 保存先ディレクトリやファイル名形式をInspectorから調整可能にする
/// </summary>
[CreateAssetMenu(fileName = "CreativeSaveSettings", menuName = "Game/Creative/Save Settings")]
public class CreativeSaveSettings : ScriptableObject
{
    [Header("Directory")]
    [Tooltip("保存先ディレクトリ（相対パス）")]
    public string saveDirectory = "CreativeExports";

    [Header("File Naming")]
    [Tooltip("ファイル名の形式（{0}に日時が入る）")]
    public string fileNameFormat = "Creative_{0:yyyyMMdd_HHmmss}.png";

    [Tooltip("タイムスタンプを含めるか")]
    public bool includeTimestamp = true;

    [Tooltip("タイムスタンプを含めない場合の固定ファイル名")]
    public string defaultFileName = "Creative.png";

    [Header("Image Export")]
    [Tooltip("保存時のスケール倍率（1 = 原寸）")]
    [Range(0.25f, 4f)]
    public float imageScale = 1f;
}

