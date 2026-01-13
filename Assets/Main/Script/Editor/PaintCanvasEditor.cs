using UnityEngine;
using UnityEditor;

/// <summary>
/// PaintCanvasのカスタムエディタ
/// 現在使用中の設定をInspector上で確認できるようにする
/// </summary>
[CustomEditor(typeof(PaintCanvas))]
public class PaintCanvasEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // デフォルトのInspectorを描画
        DrawDefaultInspector();
        
        PaintCanvas paintCanvas = (PaintCanvas)target;
        
        // 現在の状態を表示するセクション
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("現在の状態", EditorStyles.boldLabel);
        
        // 現在のモードを表示
        string modeStatus = GetCurrentModeStatus(paintCanvas);
        EditorGUILayout.LabelField("モード:", modeStatus);
        
        // 現在使用中の設定を表示
        PaintSettings activeSettings = GetCurrentActiveSettings(paintCanvas);
        if (activeSettings != null)
        {
            EditorGUILayout.LabelField("使用中の設定:", activeSettings.name);
            
            // 設定の詳細を表示
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Update Frequency:", activeSettings.updateFrequency.ToString());
            EditorGUILayout.LabelField("解像度:", $"{activeSettings.textureWidth} x {activeSettings.textureHeight}");
            EditorGUILayout.LabelField("Texture Update Frequency:", activeSettings.textureUpdateFrequency.ToString());
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox("設定が未設定です。Settingsセクションで設定を指定してください。", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
        
        // 設定を再適用するボタン（実行中のみ表示）
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("設定を再適用", GUILayout.Height(30)))
            {
                // リフレクションでSelectSettings()を呼び出す
                var method = typeof(PaintCanvas).GetMethod("SelectSettings", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(paintCanvas, null);
                    Debug.Log("PaintCanvas: 設定を再適用しました");
                }
            }
        }
    }
    
    /// <summary>
    /// 現在のモード状態を取得（リフレクション使用）
    /// </summary>
    private string GetCurrentModeStatus(PaintCanvas paintCanvas)
    {
        var field = typeof(PaintCanvas).GetField("currentModeStatus", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return field.GetValue(paintCanvas) as string ?? "未判定";
        }
        return "未判定";
    }
    
    /// <summary>
    /// 現在使用中の設定を取得（リフレクション使用）
    /// </summary>
    private PaintSettings GetCurrentActiveSettings(PaintCanvas paintCanvas)
    {
        var field = typeof(PaintCanvas).GetField("currentActiveSettings", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return field.GetValue(paintCanvas) as PaintSettings;
        }
        return null;
    }
}
