using UnityEngine;

public class VoiceDetector : MonoBehaviour
{
    [Header("Microphone Settings")]
    public int sampleRate = 44100;
    public int bufferSize = 1024;
    
    private AudioClip microphoneClip;
    private float[] samples;
    private bool isRecording = false;
    
    void Start()
    {
        InitializeMicrophone();
    }
    
    void InitializeMicrophone()
    {
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
    }
    
    public float[] GetAudioSamples()
    {
        if (!isRecording || microphoneClip == null) return null;
        
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
    }
    
    void OnDestroy()
    {
        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }
    }
}
