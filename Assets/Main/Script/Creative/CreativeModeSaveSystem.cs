using System;
using System.IO;
using UnityEngine;

/// <summary>
/// クリエイティブモードの保存・共有処理を担当
/// </summary>
public class CreativeModeSaveSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CreativeSaveSettings saveSettings;

    [Header("References")]
    [SerializeField] private PaintCanvas paintCanvas;

    public static event Action<string> OnImageSaved;
    public static event Action<bool> OnShareCompleted;

    void Awake()
    {
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
        }

        if (saveSettings == null)
        {
            Debug.LogWarning("CreativeModeSaveSystem: CreativeSaveSettingsが設定されていません。デフォルト設定を使用します。");
            saveSettings = ScriptableObject.CreateInstance<CreativeSaveSettings>();
        }
    }

    public void SaveCanvas()
    {
        if (paintCanvas == null)
        {
            Debug.LogError("CreativeModeSaveSystem: PaintCanvasが見つかりません。");
            return;
        }

        Texture2D texture = paintCanvas.GetTexture();
        if (texture == null)
        {
            Debug.LogError("CreativeModeSaveSystem: テクスチャの取得に失敗しました。");
            return;
        }

        Texture2D exportTexture = texture;
        if (!Mathf.Approximately(saveSettings.imageScale, 1f))
        {
            exportTexture = ScaleTexture(texture, saveSettings.imageScale);
        }

        byte[] pngData = exportTexture.EncodeToPNG();
        if (pngData == null || pngData.Length == 0)
        {
            Debug.LogError("CreativeModeSaveSystem: PNGエンコードに失敗しました。");
            return;
        }

        string directory = GetSaveDirectory();
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string fileName = GetFileName();
        string filePath = Path.Combine(directory, fileName);
        File.WriteAllBytes(filePath, pngData);

        Debug.Log($"CreativeModeSaveSystem: 画像を保存しました -> {filePath}");
        OnImageSaved?.Invoke(filePath);

        if (exportTexture != texture)
        {
            Destroy(exportTexture);
        }
    }

    public void ShareImage()
    {
        if (paintCanvas == null)
        {
            Debug.LogError("CreativeModeSaveSystem: PaintCanvasが見つかりません。");
            OnShareCompleted?.Invoke(false);
            return;
        }

        Texture2D texture = paintCanvas.GetTexture();
        if (texture == null)
        {
            Debug.LogError("CreativeModeSaveSystem: テクスチャの取得に失敗しました。");
            OnShareCompleted?.Invoke(false);
            return;
        }

        string tempPath = Path.Combine(Application.temporaryCachePath, "creative_share.png");
        File.WriteAllBytes(tempPath, texture.EncodeToPNG());

#if UNITY_ANDROID
        // Androidの共有処理はネイティブプラグインで実装する想定。ここでは成功とする。
        OnShareCompleted?.Invoke(true);
#elif UNITY_IOS
        // iOSの共有処理もネイティブ連携が必要。ここでは成功とする。
        OnShareCompleted?.Invoke(true);
#else
        GUIUtility.systemCopyBuffer = tempPath;
        Debug.Log($"CreativeModeSaveSystem: 共有用パスをクリップボードにコピーしました -> {tempPath}");
        OnShareCompleted?.Invoke(true);
#endif
    }

    string GetSaveDirectory()
    {
        string basePath = Application.persistentDataPath;
        if (Application.isEditor)
        {
            basePath = Path.Combine(Application.dataPath, "..", "Exports");
        }

        string directoryName = string.IsNullOrWhiteSpace(saveSettings.saveDirectory)
            ? "CreativeExports"
            : saveSettings.saveDirectory;

        return Path.GetFullPath(Path.Combine(basePath, directoryName));
    }

    string GetFileName()
    {
        if (saveSettings.includeTimestamp)
        {
            return string.Format(saveSettings.fileNameFormat, DateTime.Now);
        }

        return saveSettings.defaultFileName;
    }

    Texture2D ScaleTexture(Texture2D source, float scale)
    {
        int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));

        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}

