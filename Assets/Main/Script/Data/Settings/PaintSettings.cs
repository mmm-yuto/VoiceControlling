using UnityEngine;

/// <summary>
/// 塗りシステムの設定を管理するScriptableObject
/// Inspectorで調整可能
/// </summary>
[CreateAssetMenu(fileName = "PaintSettings", menuName = "Game/Paint Settings")]
public class PaintSettings : ScriptableObject
{
    [Header("Paint Properties")]
    [Tooltip("塗り強度の倍率（音量に掛けられる）")]
    [Range(0.1f, 5f)]
    public float paintIntensityMultiplier = 1f;
    
    [Tooltip("塗りの更新頻度（フレーム単位、1=毎フレーム）")]
    [Range(1, 10)]
    public int updateFrequency = 1;
    
    [Header("Canvas Resolution")]
    [Tooltip("キャンバスの幅（ピクセル）")]
    [Range(320, 1920)]
    public int textureWidth = 960;
    
    [Tooltip("キャンバスの高さ（ピクセル）")]
    [Range(240, 1080)]
    public int textureHeight = 540;
    
    [Header("Paint Behavior")]
    [Tooltip("無音時の塗りを停止するか")]
    public bool stopPaintingOnSilence = true;
    
    [Tooltip("塗りの最小音量閾値（これ以下は塗らない）")]
    [Range(0f, 0.1f)]
    public float minVolumeThreshold = 0.01f;
}

