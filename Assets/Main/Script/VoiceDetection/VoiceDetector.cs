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
    
    private bool microphoneRequested = false;
    private float microphoneRequestDelay = 1.0f; // ユーザーインタラクション後の遅延時間
    private float microphoneCheckTimeout = 5.0f; // マイクアクセスチェックのタイムアウト（秒）
    private float microphoneCheckStartTime = 0f;
    
    // パフォーマンス最適化用の変数
    private GCHandle samplesHandle;
    private bool samplesHandleAllocated = false;
    private int lastUpdateFrame = -1;
    private float[] cachedSamples = null;
#else
    private AudioClip microphoneClip;
#endif
    
    private float[] samples;
    private bool isRecording = false;
    
    void Start()
    {
        InitializeMicrophone();
        
#if UNITY_WEBGL && !UNITY_EDITOR
        // GCHandleを事前に確保（パフォーマンス最適化）
        if (samples != null && !samplesHandleAllocated)
        {
            samplesHandle = GCHandle.Alloc(samples, GCHandleType.Pinned);
            samplesHandleAllocated = true;
        }
#endif
    }
    
    void InitializeMicrophone()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL用の実装 - エラー時もゲームを続行できるようにする
        samples = new float[bufferSize]; // まずサンプル配列を初期化
        
        if (!InitializeMicrophone(sampleRate, bufferSize))
        {
            Debug.LogWarning("WebGLマイクの初期化に失敗しました。マイクなしで続行します。");
            return; // エラーでもゲームは続行
        }
        
        // ユーザーインタラクション後にマイクアクセスを要求するため、遅延させる
        // ブラウザのセキュリティ制約により、getUserMediaはユーザーインタラクション後にしか呼べない
        Invoke(nameof(RequestMicrophoneAccess), microphoneRequestDelay);
        
        Debug.Log($"WebGLマイク初期化準備完了: サンプルレート={sampleRate}, バッファサイズ={bufferSize}");
        Debug.Log("注意: マイクアクセスはユーザーインタラクション後に要求されます。");
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
    
#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL用: ユーザーインタラクション後にマイクアクセスを要求
    void RequestMicrophoneAccess()
    {
        if (microphoneRequested) return;
        
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
    }
    
    // 録音状態をチェック
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
        // WebGL用の実装 - パフォーマンス最適化版
        if (samples == null)
        {
            samples = new float[bufferSize];
        }
        
        // 同じフレーム内での複数回の呼び出しをキャッシュで対応
        int currentFrame = Time.frameCount;
        if (currentFrame == lastUpdateFrame && cachedSamples != null)
        {
            return cachedSamples;
        }
        
        // GCHandleが確保されていない場合は確保
        if (!samplesHandleAllocated)
        {
            samplesHandle = GCHandle.Alloc(samples, GCHandleType.Pinned);
            samplesHandleAllocated = true;
        }
        
        // GCHandleが無効な場合は再確保
        if (!samplesHandle.IsAllocated)
        {
            if (samplesHandleAllocated)
            {
                samplesHandle = GCHandle.Alloc(samples, GCHandleType.Pinned);
            }
        }
        
        try
        {
            System.IntPtr ptr = samplesHandle.AddrOfPinnedObject();
            if (GetAudioSamples(ptr, bufferSize))
            {
                // キャッシュを更新
                lastUpdateFrame = currentFrame;
                cachedSamples = samples;
                return samples;
            }
            else
            {
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"GetAudioSamples failed: {e.Message}");
            return null;
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
        
        // GCHandleを解放
        if (samplesHandleAllocated && samplesHandle.IsAllocated)
        {
            samplesHandle.Free();
            samplesHandleAllocated = false;
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
