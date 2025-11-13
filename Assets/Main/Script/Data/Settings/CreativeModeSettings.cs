using UnityEngine;

/// <summary>
/// クリエイティブモードの調整用設定
/// ScriptableObjectで管理し、非エンジニアでも変更可能にする
/// </summary>
[CreateAssetMenu(fileName = "CreativeModeSettings", menuName = "Game/Creative Mode Settings")]
public class CreativeModeSettings : ScriptableObject
{
    public enum HistorySaveMode
    {
        OnOperation,
        TimeBased
    }

    [Header("Paint Settings")]
    [Tooltip("塗り設定（既存のPaintSettingsを参照）")]
    public PaintSettings paintSettings;

    [Tooltip("塗り強度の倍率（音量に掛ける）")]
    [Range(0.1f, 2f)]
    public float paintIntensity = 1f;

    [Header("Eraser Settings")]
    [Tooltip("消しゴムの半径（ピクセル）")]
    [Range(1f, 200f)]
    public float eraserRadius = 50f;

    [Tooltip("消しゴム強度（未使用だが将来的なスケーリング用）")]
    [Range(0.1f, 5f)]
    public float eraserIntensity = 1f;

    [Header("History Settings")]
    [Tooltip("履歴保存の方式")]
    public HistorySaveMode historySaveMode = HistorySaveMode.OnOperation;

    [Tooltip("履歴に保持する最大数")]
    [Range(5, 100)]
    public int maxHistorySize = 50;

    [Tooltip("時間ベース保存時のインターバル（秒）")]
    [Range(0.1f, 5f)]
    public float autoSaveHistoryInterval = 0.5f;

    [Tooltip("無音判定の音量閾値（ImprovedPitchAnalyzerを参照しない場合）")]
    [Range(0.0001f, 0.1f)]
    public float silenceVolumeThreshold = 0.01f;

    [Tooltip("無音が継続すると操作終了と見なす時間（秒）")]
    [Range(0.1f, 2f)]
    public float silenceDurationForOperationEnd = 0.3f;

    [Tooltip("ImprovedPitchAnalyzerを参照するとvolumeThresholdを自動利用")]
    public ImprovedPitchAnalyzer improvedPitchAnalyzer;

    [Header("Color Settings")]
    [Tooltip("利用可能な基本色リスト")]
    public Color[] availableColors = new Color[]
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

    [Tooltip("初期カラー")]
    public Color initialColor = Color.white;

    [Header("Player Settings")]
    [Tooltip("クリエイティブモードのプレイヤーID")]
    [Range(1, 4)]
    public int defaultPlayerId = 1;
}

