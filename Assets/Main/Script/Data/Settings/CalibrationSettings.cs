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
    
    [Header("Initial Calibration Values")]
    [Tooltip("最小音量（デシベル、0 = 無音、90 = 叫び声レベル）")]
    [Range(0f, 90f)]
    public float initialMinVolumeDb = 0f;
    
    [Tooltip("最大音量（デシベル、0 = 無音、90 = 叫び声レベル）")]
    [Range(0f, 90f)]
    public float initialMaxVolumeDb = 90f;
    
    [Tooltip("最小ピッチ（Hz）")]
    [Range(50f, 200f)]
    public float initialMinPitch = 80f;
    
    [Tooltip("最大ピッチ（Hz）")]
    [Range(200f, 1000f)]
    public float initialMaxPitch = 1000f;
    
    /// <summary>
    /// デシベル値を振幅値（0-1）に変換
    /// VoiceToScreenMapper.ConvertAmplitudeToSPL()の逆変換
    /// </summary>
    public static float ConvertDbToAmplitude(float db)
    {
        if (db <= 0f)
        {
            return 0.0001f; // 無音（最小振幅）
        }
        
        // 変換式: amplitude = 10^(dB / 22.5 - 4)
        // 0 dB = 0.0001, 90 dB = 1.0
        float amplitude = Mathf.Pow(10f, db / 22.5f - 4f);
        return Mathf.Clamp(amplitude, 0.0001f, 1f);
    }
    
    /// <summary>
    /// 初期最小音量を振幅値として取得
    /// </summary>
    public float GetInitialMinVolume()
    {
        return ConvertDbToAmplitude(initialMinVolumeDb);
    }
    
    /// <summary>
    /// 初期最大音量を振幅値として取得
    /// </summary>
    public float GetInitialMaxVolume()
    {
        return ConvertDbToAmplitude(initialMaxVolumeDb);
    }
}

