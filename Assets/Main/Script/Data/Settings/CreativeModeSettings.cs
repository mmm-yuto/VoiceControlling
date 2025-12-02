using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// クリエイティブモードの設定
/// </summary>
[CreateAssetMenu(fileName = "CreativeModeSettings", menuName = "Game/Creative Mode Settings")]
public class CreativeModeSettings : ScriptableObject
{
    [Header("Paint Settings")]
    [Tooltip("塗り強度の倍率")]
    [Range(0.1f, 2f)] 
    public float paintIntensity = 1f;
    
    [Tooltip("初期色")]
    public Color initialColor = Color.white;
    
    [Tooltip("デフォルトプレイヤーID")]
    public int defaultPlayerId = 1;
    
    [Header("Brush Settings")]
    [Tooltip("利用可能なブラシのリスト")]
    public List<BrushStrategyBase> availableBrushes = new List<BrushStrategyBase>();
    
    [Tooltip("デフォルトで選択されるブラシ")]
    public BrushStrategyBase defaultBrush;
    
    [Header("Eraser Settings")]
    [Tooltip("消しツールの半径（ピクセル単位）")]
    [Range(10f, 100f)] 
    public float eraserRadius = 30f;
    
    [Header("History Settings")]
    [Tooltip("履歴の最大サイズ（Undo可能な回数）")]
    [Range(1, 50)] 
    public int maxHistorySize = 10;
    
    [Header("Voice Detection")]
    [Tooltip("無音判定の音量閾値")]
    [Range(0f, 0.1f)] 
    public float silenceVolumeThreshold = 0.01f;
    
    [Tooltip("操作終了とみなす無音の継続時間（秒）")]
    [Range(0.1f, 1f)] 
    public float silenceDurationForOperationEnd = 0.3f;
    
    [Header("History Settings")]
    [Tooltip("履歴保存モード")]
    public HistorySaveMode historySaveMode = HistorySaveMode.OnOperation;
    
    [Header("Effect Settings")]
    [Tooltip("インクエフェクト（CreativeModeで使用するエフェクト）")]
    public InkEffect inkEffect;
    
    public enum HistorySaveMode
    {
        OnOperation, // 操作開始/終了時に保存
        TimeBased    // 一定時間ごとに保存
    }
}

