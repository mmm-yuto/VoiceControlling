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
    
    /// <summary>
    /// ピッチ比率に基づいて色を決定
    /// </summary>
    /// <param name="pitchRatio">ピッチ比率（現在のピッチ / Neutral Soundのピッチ）</param>
    /// <param name="volumeRatio">ボリューム比率（現在は使用しないが、将来の拡張用）</param>
    /// <returns>決定された色</returns>
    public Color CalculateColor(float pitchRatio, float volumeRatio)
    {
        // 区分を判定
        PitchCategory category = GetPitchCategory(pitchRatio);
        
        // 区分に応じた色を返す
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
}

