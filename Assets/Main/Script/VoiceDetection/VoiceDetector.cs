using UnityEngine;
using System.Runtime.InteropServices;

public class VoiceDetector : MonoBehaviour
{
    [Header("Microphone Settings")]
    public int sampleRate = 44100;
    public int bufferSize = 1024;
    
#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL用のJavaScript関数
    [DllImport("__Internal")]
    private static extern bool InitializeMicrophone(int sampleRate, int bufferSize);
    
    [DllImport("__Internal")]
    private static extern bool StartRecording();
    
    [DllImport("__Internal")]
    private static extern bool StopRecording();
    
    [DllImport("__Internal")]
    private static extern int IsRecording();
    
    [DllImport("__Internal")]
    private static extern bool GetAudioSamples(System.IntPtr samplesPtr, int bufferSize);
    
    [DllImport("__Internal")]
    private static extern int GetSampleRate();
    
    [DllImport("__Internal")]
    private static extern int GetBufferSize();
#else
    private AudioClip microphoneClip;
#endif
    
    private float[] samples;
    private bool isRecording = false;
    
#if UNITY_WEBGL && !UNITY_EDITOR
    private bool microphoneRequested = false;
    private float microphoneCheckTimeout = 5.0f; // マイクアクセスチェックのタイムアウト（秒）
    private float microphoneCheckStartTime = 0f;
#endif
    
    void Start()
    {
        InitializeMicrophone();
    }
    
    void InitializeMicrophone()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL用の実装 - 初期化のみ（マイクアクセスは要求しない）
        samples = new float[bufferSize];
        
        if (!InitializeMicrophone(sampleRate, bufferSize))
        {
            Debug.LogWarning("WebGLマイクの初期化に失敗しました。マイクなしで続行します。");
            return;
        }
        
        Debug.Log($"WebGLマイク初期化準備完了: サンプルレート={sampleRate}, バッファサイズ={bufferSize}");
        Debug.Log("注意: マイクアクセスはゲームモード選択時に要求されます。");
#else
        // 非WebGLプラットフォーム用の実装（既存のコード）
        // マイクデバイスの確認
        string[] devices = Microphone.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("マイクデバイスが見つかりません");
            return;
        }
        
        try
        {
            // マイク開始
            microphoneClip = Microphone.Start(devices[0], true, 1, sampleRate);
            
            // マイクが正常に開始されたかチェック
            if (microphoneClip == null)
            {
                Debug.LogError("マイクの開始に失敗しました");
                return;
            }
            
            // バッファサイズをマイクのサンプル数に合わせて調整
            if (bufferSize > microphoneClip.samples)
            {
                bufferSize = microphoneClip.samples;
                Debug.LogWarning($"バッファサイズを{microphoneClip.samples}に調整しました");
            }
            
            samples = new float[bufferSize];
            isRecording = true;
            
            Debug.Log($"マイク開始: {devices[0]}, サンプル数: {microphoneClip.samples}, バッファサイズ: {bufferSize}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"マイク初期化エラー: {e.Message}");
            isRecording = false;
        }
#endif
    }
    
    /// <summary>
    /// ゲームモード選択時に呼ばれる。マイクアクセスを要求する。
    /// </summary>
    public void RequestMicrophoneAccess()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (microphoneRequested)
        {
            Debug.Log("WebGLマイク: 既にマイクアクセスが要求されています。");
            return;
        }
        
        microphoneRequested = true;
        microphoneCheckStartTime = Time.time;
        
        // 録音開始（非同期で実行される）
        if (StartRecording())
        {
            Debug.Log("WebGLマイク: アクセス要求を送信しました。ユーザーの許可を待っています...");
            Debug.Log("注意: Unityroomなどの環境ではマイクアクセスが制限される場合があります。");
            // 録音状態を定期的にチェック
            InvokeRepeating(nameof(CheckRecordingStatus), 0.5f, 0.5f);
        }
        else
        {
            Debug.LogWarning("WebGLマイク: アクセス要求の送信に失敗しました。マイクなしで続行します。");
            // マイクなしでもゲームは続行
        }
#else
        // 非WebGLプラットフォームでは既にマイクが開始されているため、何もしない
        Debug.Log("VoiceDetector: 非WebGLプラットフォームではマイクは既に開始されています。");
#endif
    }
    
#if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>
    /// 録音状態をチェック
    /// </summary>
    void CheckRecordingStatus()
    {
        // タイムアウトチェック
        if (Time.time - microphoneCheckStartTime > microphoneCheckTimeout)
        {
            Debug.LogWarning($"WebGLマイク: タイムアウト（{microphoneCheckTimeout}秒）。マイクアクセスを諦めて続行します。");
            CancelInvoke(nameof(CheckRecordingStatus));
            return;
        }
        
        int recordingStatus = IsRecording();
        if (recordingStatus == 1 && !isRecording)
        {
            isRecording = true;
            Debug.Log("WebGLマイク: 録音が開始されました。");
            CancelInvoke(nameof(CheckRecordingStatus));
        }
    }
#endif
    
    public float[] GetAudioSamples()
    {
        if (!isRecording) return null;
        
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL用の実装
        if (samples == null)
        {
            samples = new float[bufferSize];
        }
        
        // アンマネージドメモリにピン留めしてJavaScriptに渡す
        System.Runtime.InteropServices.GCHandle handle = System.Runtime.InteropServices.GCHandle.Alloc(samples, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            System.IntPtr ptr = handle.AddrOfPinnedObject();
            if (GetAudioSamples(ptr, bufferSize))
            {
                return samples;
            }
            else
            {
                return null;
            }
        }
        finally
        {
            handle.Free();
        }
#else
        // 非WebGLプラットフォーム用の実装（既存のコード）
        if (microphoneClip == null) return null;
        
        int position = Microphone.GetPosition(null);
        int startPosition = position - bufferSize;
        
        // 位置が負の値にならないように調整
        if (startPosition < 0)
        {
            startPosition = microphoneClip.samples - bufferSize + position;
        }
        
        // 位置が有効な範囲内かチェック
        if (startPosition < 0 || startPosition >= microphoneClip.samples)
        {
            return null;
        }
        
        try
        {
            microphoneClip.GetData(samples, startPosition);
            return samples;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"GetData failed: {e.Message}");
            return null;
        }
#endif
    }
    
    void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL用の実装
        CancelInvoke(); // すべてのInvokeをキャンセル
        if (isRecording || microphoneRequested)
        {
            StopRecording();
            isRecording = false;
        }
#else
        // 非WebGLプラットフォーム用の実装（既存のコード）
        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }
#endif
    }
}
