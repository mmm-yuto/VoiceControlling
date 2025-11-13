using UnityEngine;

/// <summary>
/// キャリブレーション時に使用するパラメータをまとめた ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "CalibrationSettings", menuName = "Game/Calibration Settings")]
public class CalibrationSettings : ScriptableObject
{
    [Header("Durations")]
    [Tooltip("キャリブレーションを継続する秒数")]
    [Range(1f, 10f)] public float calibrationDuration = 3f;

    [Tooltip("開始直後にノイズをサンプリングして平均化する秒数")]
    [Range(0f, 2f)] public float noiseSampleDuration = 0.5f;

    [Header("Range Multipliers")]
    [Tooltip("検出した平均音量に掛ける倍率。これが最大音量設定に使用される")]
    [Range(1f, 3f)] public float volumeMultiplier = 1.5f;

    [Tooltip("検出した平均ピッチに掛ける倍率。これが最大ピッチ設定に使用される")]
    [Range(1f, 2f)] public float pitchMultiplier = 1.2f;

    [Tooltip("最小ピッチを求める際の係数（平均ピッチに掛ける）")]
    [Range(0.1f, 1f)] public float minPitchFactor = 0.5f;

    [Header("Silence Threshold")]
    [Tooltip("ノイズ平均に掛ける倍率で音量閾値を算出")]
    [Range(1f, 5f)] public float dynamicThresholdMultiplier = 2f;

    [Tooltip("音量閾値の下限値")]
    [Range(0f, 0.1f)] public float minDynamicThreshold = 0.005f;

    [Tooltip("音量閾値の上限値")]
    [Range(0f, 0.2f)] public float maxDynamicThreshold = 0.05f;
}

