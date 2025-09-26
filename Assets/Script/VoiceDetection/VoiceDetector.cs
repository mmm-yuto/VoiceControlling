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
        
        // マイク開始
        microphoneClip = Microphone.Start(devices[0], true, 1, sampleRate);
        samples = new float[bufferSize];
        isRecording = true;
        
        Debug.Log($"マイク開始: {devices[0]}");
    }
    
    public float[] GetAudioSamples()
    {
        if (!isRecording) return null;
        
        int position = Microphone.GetPosition(null);
        microphoneClip.GetData(samples, position - bufferSize);
        
        return samples;
    }
    
    void OnDestroy()
    {
        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }
    }
}
