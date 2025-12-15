using UnityEngine;

/// <summary>
/// メインカラー2色の設定
/// ゲーム全体で使用する主要な2色を管理
/// </summary>
[CreateAssetMenu(fileName = "MainColorSettings", menuName = "Game/Main Color Settings")]
public class MainColorSettings : ScriptableObject
{
    [Header("Main Colors")]
    [Tooltip("メインカラー1（プレイヤーまたはチーム1の色）")]
    public Color mainColor1 = Color.blue;
    
    [Tooltip("メインカラー2（CPUまたはチーム2の色）")]
    public Color mainColor2 = Color.red;
}

