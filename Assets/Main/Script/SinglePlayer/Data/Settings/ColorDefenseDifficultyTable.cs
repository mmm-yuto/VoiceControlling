using UnityEngine;

/// <summary>
/// ColorDefense のレベル別難易度パラメータをまとめたテーブル。
/// ロビーで選択した「相手レベル」（1〜5）に応じて、敵ペンの挙動を変化させる。
/// </summary>
[CreateAssetMenu(
    fileName = "ColorDefenseDifficultyTable",
    menuName = "Game/SinglePlayer/Modes/Color Defense Difficulty Table")]
public class ColorDefenseDifficultyTable : ScriptableObject
{
    [Tooltip("レベル 1〜5 に対応するパラメータ配列（index 0 が Lv1）")]
    public ColorDefenseLevelParams[] levels = new ColorDefenseLevelParams[5];

    /// <summary>
    /// レベル番号（1〜）からパラメータを取得する。範囲外の場合はクランプ。
    /// </summary>
    public ColorDefenseLevelParams GetLevelParams(int level)
    {
        if (levels == null || levels.Length == 0)
        {
            return new ColorDefenseLevelParams(); // デフォルト値
        }

        int index = Mathf.Clamp(level - 1, 0, levels.Length - 1);
        return levels[index];
    }
}

/// <summary>
/// レベルごとの敵挙動パラメータ。
/// </summary>
[System.Serializable]
public class ColorDefenseLevelParams
{
    [Header("Move Speed")]
    [Tooltip("敵ペンの移動スピード倍率（1.0 で設定値どおり）")]
    [Range(0.2f, 5f)]
    public float moveSpeedMultiplier = 1f;

    [Header("Target Preference")]
    [Tooltip("まだ塗られていない場所をどれだけ優先するかの重み")]
    [Range(0f, 5f)]
    public float focusUnpaintedWeight = 1f;

    [Tooltip("プレイヤーが塗った場所を塗り替えようとする重み")]
    [Range(0f, 5f)]
    public float repaintPlayerWeight = 1f;

    [Header("Mistake / Randomness")]
    [Tooltip("どれだけミスするか（0=ほぼ最適、1=かなりランダムに塗る）")]
    [Range(0f, 1f)]
    public float missRate = 0.2f;
}


