using UnityEngine;

public class VoiceController : MonoBehaviour
{
    [Header("Control Settings")]
    public float volumeToSpeed = 10f;
    public float pitchToDirection = 1f;
    public float deadZone = 0.1f;
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    private Rigidbody rb;
    
    void Start()
    {
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        rb = GetComponent<Rigidbody>();
        
        // イベント購読
        volumeAnalyzer.OnVolumeDetected += OnVolumeDetected;
        pitchAnalyzer.OnPitchDetected += OnPitchDetected;
    }
    
    void OnVolumeDetected(float volume)
    {
        // 音量に基づく移動速度の調整
        float speed = volume * volumeToSpeed;
        
        // 現在の移動方向を維持しつつ速度を調整
        Vector3 currentVelocity = rb.linearVelocity;
        if (currentVelocity.magnitude > 0)
        {
            rb.linearVelocity = currentVelocity.normalized * speed;
        }
    }
    
    void OnPitchDetected(float pitch)
    {
        // 音程に基づく方向の変更
        float normalizedPitch = (pitch - 200f) / 800f; // 200-1000Hzを-1〜1に正規化
        normalizedPitch = Mathf.Clamp(normalizedPitch, -1f, 1f);
        
        if (Mathf.Abs(normalizedPitch) > deadZone)
        {
            Vector3 direction = new Vector3(normalizedPitch, 0, 0);
            rb.AddForce(direction * pitchToDirection, ForceMode.Force);
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= OnVolumeDetected;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected -= OnPitchDetected;
    }
}
