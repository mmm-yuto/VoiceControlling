using UnityEngine;
using System.IO;
using System;
using UnityEngine.Events;

/// <summary>
/// クリエイティブモードの保存・共有機能
/// </summary>
public class CreativeModeSaveSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("塗りキャンバス（Inspectorで接続）")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Header("Settings")]
    [Tooltip("保存設定（Inspectorで接続）")]
    [SerializeField] private CreativeSaveSettings saveSettings;
    
    // イベント
    public event System.Action<string> OnImageSaved; // filePath
    public event System.Action<bool> OnShareCompleted; // success
    public UnityEvent<string> OnImageSavedUnityEvent;
    public UnityEvent<bool> OnShareCompletedUnityEvent;
    
    void Start()
    {
        // 参照の自動検索
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
            if (paintCanvas == null)
            {
                Debug.LogError("CreativeModeSaveSystem: PaintCanvasが見つかりません");
            }
        }
        
        // 設定の初期化
        if (saveSettings == null)
        {
            Debug.LogWarning("CreativeModeSaveSystem: CreativeSaveSettingsが設定されていません。デフォルト設定を使用します。");
            saveSettings = ScriptableObject.CreateInstance<CreativeSaveSettings>();
        }
    }
    
    /// <summary>
    /// 画像を保存
    /// </summary>
    public void SaveImage()
    {
        if (paintCanvas == null)
        {
            Debug.LogError("CreativeModeSaveSystem: PaintCanvasが設定されていません");
            return;
        }
        
        Texture2D texture = paintCanvas.GetTexture();
        if (texture == null)
        {
            Debug.LogError("CreativeModeSaveSystem: テクスチャを取得できませんでした");
            return;
        }
        
        // 保存先ディレクトリを取得（.exeファイルと同じ場所の「写真」フォルダ）
        string saveDirectory = GetSaveImageFolder();
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
        
        // ファイル名を生成（ゲーム名とタイムスタンプ付き）
        string fileName;
        if (saveSettings.includeTimestamp)
        {
            fileName = $"ShoutInk_Drawing_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        }
        else
        {
            fileName = "ShoutInk_Drawing.png";
        }
        
        string filePath = Path.Combine(saveDirectory, fileName);
        
        // テクスチャをPNG形式で保存
        try
        {
            // スケールを適用する場合は新しいテクスチャを作成
            Texture2D saveTexture = texture;
            if (saveSettings.imageScale != 1f)
            {
                int newWidth = Mathf.RoundToInt(texture.width * saveSettings.imageScale);
                int newHeight = Mathf.RoundToInt(texture.height * saveSettings.imageScale);
                saveTexture = ScaleTexture(texture, newWidth, newHeight);
            }
            
            byte[] pngData = saveTexture.EncodeToPNG();
            File.WriteAllBytes(filePath, pngData);
            
            // スケール用テクスチャを破棄
            if (saveTexture != texture)
            {
                Destroy(saveTexture);
            }
            
            Debug.Log($"CreativeModeSaveSystem: 画像を保存しました: {filePath}");
            
            // 保存したフォルダをエクスプローラーで開く（ShareImageと同じ動作）
            OpenFileInExplorer(filePath);
            
            // イベント発火
            OnImageSaved?.Invoke(filePath);
            OnImageSavedUnityEvent?.Invoke(filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"CreativeModeSaveSystem: 画像の保存に失敗しました: {e.Message}");
        }
    }
    
    /// <summary>
    /// 画像を共有（プラットフォーム固有の実装が必要）
    /// </summary>
    /// <param name="title">描画のタイトル（オプション）</param>
    public void ShareImage(string title = "")
    {
        if (paintCanvas == null)
        {
            Debug.LogError("CreativeModeSaveSystem: PaintCanvasが設定されていません");
            return;
        }
        
        Texture2D texture = paintCanvas.GetTexture();
        if (texture == null)
        {
            Debug.LogError("CreativeModeSaveSystem: テクスチャを取得できませんでした");
            return;
        }
        
        // .exeファイルと同じ場所の「写真」フォルダに保存
        string saveFolder = GetSaveImageFolder();
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }
        
        // ファイル名を生成（ゲーム名とタイムスタンプ付き）
        string fileName = $"ShoutInk_Drawing_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        string savePath = Path.Combine(saveFolder, fileName);
        
        try
        {
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(savePath, pngData);
            
            // プラットフォーム固有の共有処理
            bool success = false;
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Android用の共有処理（Native Share Pluginなどが必要）
            success = ShareImageAndroid(savePath);
            #elif UNITY_IOS && !UNITY_EDITOR
            // iOS用の共有処理（Native Share Pluginなどが必要）
            success = ShareImageIOS(savePath);
            #else
            // Windows/その他のプラットフォームではTwitter共有を実装
            success = ShareToTwitter(savePath, title);
            #endif
            
            // イベント発火
            OnShareCompleted?.Invoke(success);
            OnShareCompletedUnityEvent?.Invoke(success);
        }
        catch (Exception e)
        {
            Debug.LogError($"CreativeModeSaveSystem: 画像の共有に失敗しました: {e.Message}");
            OnShareCompleted?.Invoke(false);
            OnShareCompletedUnityEvent?.Invoke(false);
        }
    }
    
    /// <summary>
    /// テクスチャをスケール
    /// </summary>
    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);
        Color[] pixels = new Color[targetWidth * targetHeight];
        
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                float u = (float)x / targetWidth;
                float v = (float)y / targetHeight;
                pixels[y * targetWidth + x] = source.GetPixelBilinear(u, v);
            }
        }
        
        result.SetPixels(pixels);
        result.Apply();
        return result;
    }
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    private bool ShareImageAndroid(string filePath)
    {
        // Android用の共有処理（Native Share Pluginなどが必要）
        // ここでは実装例のみ
        Debug.Log($"CreativeModeSaveSystem: Android共有機能は未実装です。ファイルパス: {filePath}");
        return false;
    }
    #endif
    
    #if UNITY_IOS && !UNITY_EDITOR
    private bool ShareImageIOS(string filePath)
    {
        // iOS用の共有処理（Native Share Pluginなどが必要）
        // ここでは実装例のみ
        Debug.Log($"CreativeModeSaveSystem: iOS共有機能は未実装です。ファイルパス: {filePath}");
        return false;
    }
    #endif
    
    /// <summary>
    /// 画像の保存フォルダを取得（.exeファイルと同じ場所の「Screenshots」フォルダ）
    /// </summary>
    private string GetSaveImageFolder()
    {
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Windowsの場合、.exeファイルと同じ場所
        // Application.dataPathは通常 "実行ファイル名_Data" フォルダなので、その親ディレクトリが.exeの場所
        string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(exeDirectory, "Screenshots");
        #elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        // macOSの場合、.appバンドルの場所
        // Application.dataPathは通常 "実行ファイル名.app/Contents" なので、その親の親が.appの場所
        string appDirectory = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        return Path.Combine(appDirectory, "Screenshots");
        #elif UNITY_STANDALONE_LINUX
        // Linuxの場合、実行ファイルと同じ場所
        string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(exeDirectory, "Screenshots");
        #else
        // その他のプラットフォームは一時キャッシュ
        return Application.temporaryCachePath;
        #endif
    }
    
    /// <summary>
    /// ファイルをエクスプローラーで開く（Windows用）
    /// </summary>
    private void OpenFileInExplorer(string filePath)
    {
        try
        {
            #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Windows: エクスプローラーでファイルを選択状態で開く
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            #elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            // macOS: Finderでファイルを選択状態で開く
            System.Diagnostics.Process.Start("open", $"-R \"{filePath}\"");
            #elif UNITY_STANDALONE_LINUX
            // Linux: ファイルマネージャーで開く（xdg-openを使用）
            System.Diagnostics.Process.Start("xdg-open", Path.GetDirectoryName(filePath));
            #else
            // その他のプラットフォームは何もしない
            Debug.Log($"CreativeModeSaveSystem: このプラットフォームではファイルを自動的に開けません。パス: {filePath}");
            #endif
        }
        catch (Exception e)
        {
            Debug.LogWarning($"CreativeModeSaveSystem: ファイルを開くのに失敗しました: {e.Message}");
        }
    }
    
    /// <summary>
    /// Twitterに共有（Windows/その他のプラットフォーム用）
    /// </summary>
    /// <param name="imagePath">画像ファイルのパス</param>
    /// <param name="title">描画のタイトル</param>
    private bool ShareToTwitter(string imagePath, string title)
    {
        try
        {
            // ゲームストアURLを取得
            string storeUrl = saveSettings != null ? saveSettings.gameStoreUrl : "";
            
            // Twitter投稿用のテキストを構築
            string tweetText = "I drew this with my voice!";
            
            // タイトルが入力されている場合は追加
            if (!string.IsNullOrWhiteSpace(title))
            {
                tweetText += $"\nThe title of this drawing is 「{title}」";
            }
            
            // ゲームストアURLが設定されている場合は追加
            if (!string.IsNullOrWhiteSpace(storeUrl))
            {
                tweetText += $"\n{storeUrl}";
            }
            
            // URLエンコード
            string encodedText = Uri.EscapeDataString(tweetText);
            
            // Twitter Web Intent URLを構築
            // 注意: Twitter Web Intentは画像を直接アタッチできないため、
            // テキストのみで投稿画面を開き、ユーザーが手動で画像をアップロードする必要があります
            string twitterUrl = $"https://twitter.com/intent/tweet?text={encodedText}";
            
            // ブラウザでTwitter投稿画面を開く
            Application.OpenURL(twitterUrl);
            
            // 画像ファイルをエクスプローラーで開く（ユーザーが簡単にアクセスできるように）
            OpenFileInExplorer(imagePath);
            
            // 画像ファイルのパスをログに出力
            UnityEngine.Debug.Log($"CreativeModeSaveSystem: Twitter投稿画面を開きました。");
            UnityEngine.Debug.Log($"CreativeModeSaveSystem: 画像ファイルをエクスプローラーで開きました。パス: {imagePath}");
            UnityEngine.Debug.Log($"CreativeModeSaveSystem: エクスプローラーで表示された画像をTwitterにドラッグ&ドロップしてください。");
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"CreativeModeSaveSystem: Twitter共有に失敗しました: {e.Message}");
            return false;
        }
    }
}

