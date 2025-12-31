using UnityEngine;
using System.IO;
using System;

/// <summary>
/// カリブレーションデータの保存・読み込みシステム
/// </summary>
[System.Serializable]
public class CalibrationData
{
    public float minVolume;
    public float maxVolume;
    public float minPitch;
    public float maxPitch;
    public string savedDateTime; // 保存日時（デバッグ用）
}

public static class CalibrationSaveSystem
{
    private const string SAVE_FOLDER_NAME = "Calibration";
    private const string SAVE_FILE_NAME = "calibration_data.json";
    
    /// <summary>
    /// カリブレーションデータを保存
    /// </summary>
    /// <param name="minVol">最小音量</param>
    /// <param name="maxVol">最大音量</param>
    /// <param name="minPit">最小ピッチ</param>
    /// <param name="maxPit">最大ピッチ</param>
    /// <returns>保存成功時true</returns>
    public static bool SaveCalibrationData(float minVol, float maxVol, float minPit, float maxPit)
    {
        try
        {
            CalibrationData data = new CalibrationData
            {
                minVolume = minVol,
                maxVolume = maxVol,
                minPitch = minPit,
                maxPitch = maxPit,
                savedDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            string json = JsonUtility.ToJson(data, true);
            string filePath = GetSaveFilePath();
            
            // ディレクトリが存在しない場合は作成
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // ファイルに書き込み
            File.WriteAllText(filePath, json);
            
            Debug.Log($"CalibrationSaveSystem: カリブレーションデータを保存しました: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"CalibrationSaveSystem: カリブレーションデータの保存に失敗しました: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// カリブレーションデータを読み込み
    /// </summary>
    /// <param name="data">読み込んだデータを格納する変数</param>
    /// <returns>読み込み成功時true</returns>
    public static bool LoadCalibrationData(out CalibrationData data)
    {
        data = null;
        
        try
        {
            string filePath = GetSaveFilePath();
            
            // ファイルが存在しない場合はfalseを返す
            if (!File.Exists(filePath))
            {
                Debug.Log($"CalibrationSaveSystem: セーブデータが見つかりません: {filePath}");
                return false;
            }
            
            // ファイルを読み込み
            string json = File.ReadAllText(filePath);
            data = JsonUtility.FromJson<CalibrationData>(json);
            
            if (data == null)
            {
                Debug.LogError("CalibrationSaveSystem: データの解析に失敗しました");
                return false;
            }
            
            Debug.Log($"CalibrationSaveSystem: カリブレーションデータを読み込みました: {filePath}");
            Debug.Log($"CalibrationSaveSystem: Volume: {data.minVolume:F3} - {data.maxVolume:F3}, Pitch: {data.minPitch:F1} - {data.maxPitch:F1} Hz (保存日時: {data.savedDateTime})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"CalibrationSaveSystem: カリブレーションデータの読み込みに失敗しました: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// セーブデータの存在確認
    /// </summary>
    /// <returns>セーブデータが存在する場合true</returns>
    public static bool HasCalibrationData()
    {
        string filePath = GetSaveFilePath();
        return File.Exists(filePath);
    }
    
    /// <summary>
    /// 保存ファイルパスを取得
    /// </summary>
    /// <returns>保存ファイルのフルパス</returns>
    public static string GetSaveFilePath()
    {
        string saveDirectory = GetSaveDirectory();
        return Path.Combine(saveDirectory, SAVE_FILE_NAME);
    }
    
    /// <summary>
    /// 保存ディレクトリを取得（.exeファイルと同じ場所の「Calibration」フォルダ）
    /// </summary>
    /// <returns>保存ディレクトリのフルパス</returns>
    private static string GetSaveDirectory()
    {
        #if UNITY_EDITOR
        // エディタ実行時は、プロジェクトフォルダ直下に保存
        // Application.dataPath = "プロジェクトフォルダ/Assets"
        // 親ディレクトリ = "プロジェクトフォルダ"
        string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
        string saveDir = Path.Combine(projectDirectory, SAVE_FOLDER_NAME);
        Debug.Log($"CalibrationSaveSystem: エディタ実行時 - 保存ディレクトリ: {saveDir}");
        return saveDir;
        #elif UNITY_STANDALONE_WIN
        // Windowsビルド時、.exeファイルと同じ場所
        // Application.dataPathは通常 "実行ファイル名_Data" フォルダなので、その親ディレクトリが.exeの場所
        string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(exeDirectory, SAVE_FOLDER_NAME);
        #elif UNITY_STANDALONE_OSX
        // macOSの場合、.appバンドルの場所
        // Application.dataPathは通常 "実行ファイル名.app/Contents" なので、その親の親が.appの場所
        string appDirectory = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        return Path.Combine(appDirectory, SAVE_FOLDER_NAME);
        #elif UNITY_STANDALONE_LINUX
        // Linuxの場合、実行ファイルと同じ場所
        string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(exeDirectory, SAVE_FOLDER_NAME);
        #else
        // その他のプラットフォームは一時キャッシュ
        return Path.Combine(Application.temporaryCachePath, SAVE_FOLDER_NAME);
        #endif
    }
}

