using UnityEngine;

/// <summary>
/// ピッチ比率に基づいて4種類の色から選択するコンポーネント
/// Neutral Soundとの比較結果に基づいて色を決定
/// </summary>
public class ColorCalculator : MonoBehaviour
{
    [Header("Color Settings - 4 Categories")]
    [Tooltip("1. NeutralSound（比率 = 1.0）の色")]
    [SerializeField] private Color neutralSoundColor = Color.white;
    
    [Tooltip("2. ピッチがNeutralSound以下（比率 < 1.0）の色")]
    [SerializeField] private Color lowPitchColor = Color.blue;
    
    [Tooltip("3. 1.0 <= 比率 < 1.5 の色")]
    [SerializeField] private Color mediumPitchColor = Color.yellow;
    
    [Tooltip("4. 比率 >= 1.5 の色")]
    [SerializeField] private Color highPitchColor = Color.red;
    
    [Header("Threshold Settings")]
    [Tooltip("NeutralSoundの判定範囲（比率がこの範囲内ならNeutralSoundとみなす）")]
    [Range(0.01f, 0.1f)]
    [SerializeField] private float neutralSoundThreshold = 0.05f;
    
    [Header("Volume Saturation Settings")]
    [Tooltip("音量比率に基づいて彩度を調整するか")]
    [SerializeField] private bool useVolumeForSaturation = true;
    
    [Tooltip("音量比率の範囲（最小値）")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeRatioMin = 0.5f;
    
    [Tooltip("音量比率の範囲（最大値）")]
    [Range(0f, 2f)]
    [SerializeField] private float volumeRatioMax = 1.5f;
    
    [Tooltip("最小彩度（音量が小さい場合）")]
    [Range(0f, 1f)]
    [SerializeField] private float minSaturation = 0.3f;
    
    [Tooltip("最大彩度（音量が大きい場合）")]
    [Range(0f, 1f)]
    [SerializeField] private float maxSaturation = 1.0f;
    
    /// <summary>
    /// ピッチ比率と音量比率に基づいて色を決定
    /// </summary>
    /// <param name="pitchRatio">ピッチ比率（現在のピッチ / Neutral Soundのピッチ）</param>
    /// <param name="volumeRatio">ボリューム比率（現在のボリューム / Neutral Soundのボリューム）</param>
    /// <returns>決定された色</returns>
    public Color CalculateColor(float pitchRatio, float volumeRatio)
    {
        // 区分を判定
        PitchCategory category = GetPitchCategory(pitchRatio);
        
        // 区分に応じた基本色を取得
        Color baseColor;
        switch (category)
        {
            case PitchCategory.NeutralSound:
                baseColor = neutralSoundColor;
                break;
            
            case PitchCategory.LowPitch:
                baseColor = lowPitchColor;
                break;
            
            case PitchCategory.MediumPitch:
                baseColor = mediumPitchColor;
                break;
            
            case PitchCategory.HighPitch:
                baseColor = highPitchColor;
                break;
            
            default:
                baseColor = neutralSoundColor;
                break;
        }
        
        // 音量比率に基づいて彩度を調整
        if (useVolumeForSaturation)
        {
            return ApplyVolumeSaturation(baseColor, volumeRatio);
        }
        
        return baseColor;
    }
    
    /// <summary>
    /// 音量比率に基づいて彩度を適用
    /// </summary>
    private Color ApplyVolumeSaturation(Color baseColor, float volumeRatio)
    {
        // 音量比率を0.0～1.0の範囲に正規化
        float normalizedVolumeRatio = NormalizeVolumeRatio(volumeRatio);
        
        // 彩度を計算（音量が大きいほど彩度が高い）
        float saturation = Mathf.Lerp(minSaturation, maxSaturation, normalizedVolumeRatio);
        
        // 基本色をHSVに変換
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        
        // 彩度を音量比率に基づいて調整
        s = saturation;
        
        // HSVからRGBに戻す
        return Color.HSVToRGB(h, s, v);
    }
    
    /// <summary>
    /// 音量比率を0.0～1.0の範囲に正規化
    /// </summary>
    private float NormalizeVolumeRatio(float volumeRatio)
    {
        // 音量比率をmin～maxの範囲にクランプ
        float clampedRatio = Mathf.Clamp(volumeRatio, volumeRatioMin, volumeRatioMax);
        
        // 0.0～1.0の範囲に正規化
        if (volumeRatioMax > volumeRatioMin)
        {
            return (clampedRatio - volumeRatioMin) / (volumeRatioMax - volumeRatioMin);
        }
        
        return 0.5f; // min == max の場合は0.5を返す
    }
    
    /// <summary>
    /// ピッチ比率から区分を判定
    /// </summary>
    /// <param name="pitchRatio">ピッチ比率</param>
    /// <returns>区分</returns>
    private PitchCategory GetPitchCategory(float pitchRatio)
    {
        // 1. NeutralSound（比率 ≈ 1.0）
        if (Mathf.Abs(pitchRatio - 1.0f) <= neutralSoundThreshold)
        {
            return PitchCategory.NeutralSound;
        }
        
        // 2. ピッチがNeutralSound以下（比率 < 1.0）
        if (pitchRatio < 1.0f)
        {
            return PitchCategory.LowPitch;
        }
        
        // 3. 1.0 <= 比率 < 1.5
        if (pitchRatio >= 1.0f && pitchRatio < 1.5f)
        {
            return PitchCategory.MediumPitch;
        }
        
        // 4. 比率 >= 1.5
        return PitchCategory.HighPitch;
    }
    
    /// <summary>
    /// ピッチ区分
    /// </summary>
    public enum PitchCategory
    {
        NeutralSound,  // 比率 ≈ 1.0
        LowPitch,      // 比率 < 1.0
        MediumPitch,   // 1.0 <= 比率 < 1.5
        HighPitch      // 比率 >= 1.5
    }
    
    /// <summary>
    /// ピッチ比率から区分を取得（外部から使用可能）
    /// </summary>
    public PitchCategory GetCategory(float pitchRatio)
    {
        return GetPitchCategory(pitchRatio);
    }
    
    /// <summary>
    /// ピッチとボリュームから比率を計算
    /// </summary>
    /// <param name="currentPitch">現在のピッチ</param>
    /// <param name="currentVolume">現在のボリューム</param>
    /// <param name="neutralPitch">Neutral Soundのピッチ</param>
    /// <param name="neutralVolume">Neutral Soundのボリューム</param>
    /// <returns>(pitchRatio, volumeRatio)</returns>
    public Vector2 CalculateRatios(float currentPitch, float currentVolume, float neutralPitch, float neutralVolume)
    {
        float pitchRatio = neutralPitch > 0f ? currentPitch / neutralPitch : 1f;
        float volumeRatio = neutralVolume > 0f ? currentVolume / neutralVolume : 1f;
        
        return new Vector2(pitchRatio, volumeRatio);
    }
    
    /// <summary>
    /// 区分名を取得（デバッグ用）
    /// </summary>
    public string GetCategoryName(PitchCategory category)
    {
        switch (category)
        {
            case PitchCategory.NeutralSound:
                return "NeutralSound";
            case PitchCategory.LowPitch:
                return "LowPitch";
            case PitchCategory.MediumPitch:
                return "MediumPitch";
            case PitchCategory.HighPitch:
                return "HighPitch";
            default:
                return "Unknown";
        }
    }
    
    /// <summary>
    /// ピッチ比率のみに基づいた基本色を取得（ボリューム比率による彩度調整なし）
    /// </summary>
    /// <param name="pitchRatio">ピッチ比率（現在のピッチ / Neutral Soundのピッチ）</param>
    /// <returns>基本色</returns>
    public Color GetBaseColor(float pitchRatio)
    {
        // 区分を判定
        PitchCategory category = GetPitchCategory(pitchRatio);
        
        // 区分に応じた基本色を取得
        switch (category)
        {
            case PitchCategory.NeutralSound:
                return neutralSoundColor;
            
            case PitchCategory.LowPitch:
                return lowPitchColor;
            
            case PitchCategory.MediumPitch:
                return mediumPitchColor;
            
            case PitchCategory.HighPitch:
                return highPitchColor;
            
            default:
                return neutralSoundColor;
        }
    }
}

