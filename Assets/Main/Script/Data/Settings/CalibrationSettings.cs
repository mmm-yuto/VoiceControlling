using UnityEngine;

[CreateAssetMenu(fileName = "CalibrationSettings", menuName = "Game/Calibration Settings")]
public class CalibrationSettings : ScriptableObject
{
    [Header("Calibration Duration")]
    [Tooltip("各ステップの収集時間（秒）")]
    [Range(1f, 10f)]
    public float stepDuration = 3f;

    [Header("Volume Margins")]
    [Tooltip("最小音量のマージン係数（収集した平均値にこの係数を掛ける）")]
    [Range(0.5f, 1f)]
    public float minVolumeMargin = 0.9f;
    
    [Tooltip("最大音量のマージン係数（収集した最大値にこの係数を掛ける）")]
    [Range(1f, 1.5f)]
    public float maxVolumeMargin = 1.1f;

    [Header("Pitch Margins")]
    [Tooltip("最小ピッチのマージン係数（収集した最小値にこの係数を掛ける）")]
    [Range(0.5f, 1f)]
    public float minPitchMargin = 0.9f;
    
    [Tooltip("最大ピッチのマージン係数（収集した最大値にこの係数を掛ける）")]
    [Range(1f, 1.5f)]
    public float maxPitchMargin = 1.1f;
}

