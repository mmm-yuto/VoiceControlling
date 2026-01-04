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
        // Check microphone device
        string[] devices = Microphone.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("VoiceDetector: No microphone device found");
            return;
        }
        
        try
        {
            // Start microphone
            microphoneClip = Microphone.Start(devices[0], true, 1, sampleRate);
            
            // Check if microphone started successfully
            if (microphoneClip == null)
            {
                Debug.LogError("VoiceDetector: Failed to start microphone");
                return;
            }
            
            // Adjust buffer size to match microphone samples
            if (bufferSize > microphoneClip.samples)
            {
                bufferSize = microphoneClip.samples;
                Debug.LogWarning($"VoiceDetector: Buffer size adjusted to {microphoneClip.samples}");
            }
            
            samples = new float[bufferSize];
            isRecording = true;
            
            Debug.Log($"VoiceDetector: Microphone started - Device: {devices[0]}, Samples: {microphoneClip.samples}, Buffer Size: {bufferSize}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"VoiceDetector: Microphone initialization error: {e.Message}");
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
    
    /// <summary>
    /// Get voice detection state
    /// </summary>
    public bool IsDetectionEnabled => isRecording;
    
    /// <summary>
    /// Enable voice detection
    /// </summary>
    public void EnableDetection()
    {
        if (isRecording)
        {
            Debug.Log("VoiceDetector: Voice detection is already enabled");
            return;
        }
        
        isRecording = true;
        Debug.Log("VoiceDetector: Voice detection enabled");
    }
    
    /// <summary>
    /// Disable voice detection
    /// </summary>
    public void DisableDetection()
    {
        if (!isRecording)
        {
            Debug.Log("VoiceDetector: Voice detection is already disabled");
            return;
        }
        
        isRecording = false;
        Debug.Log("VoiceDetector: Voice detection disabled");
    }
    
    void OnDestroy()
    {
        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }
    }
}
