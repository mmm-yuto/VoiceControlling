# 音声検知ゲーム実装ステップ

## プロジェクト概要
音量と音程を使用したゲームコントロールシステムの実装手順

## Phase 1: 環境構築と基本設定

### Step 1.1: Unity プロジェクト設定
- [ ] Unity 2022.3 LTS以上を使用
- [ ] プラットフォーム設定（PC、Mac、Android、iOS）
- [ ] マイク権限の設定確認

### Step 1.2: 必要なパッケージのインストール
- [ ] **Audio** パッケージ（標準）
- [ ] **Mathematics** パッケージ（FFT計算用）
- [ ] **Input System** パッケージ（既にインストール済み）

### Step 1.3: プロジェクト構造の作成
```
Assets/
├── Scripts/
│   ├── VoiceDetection/
│   │   ├── VoiceDetector.cs
│   │   ├── VolumeAnalyzer.cs
│   │   └── PitchAnalyzer.cs
│   ├── GameLogic/
│   │   ├── VoiceController.cs
│   │   └── GameManager.cs
│   └── UI/
│       ├── VoiceVisualizer.cs
│       └── VolumePitchDisplay.cs
├── Prefabs/
│   └── VoiceControlPrefab.prefab
└── Scenes/
    ├── VoiceTestScene.unity
    └── GameScene.unity
```

## Phase 2: 基本音声検知の実装

### Step 2.1: マイク入力の実装
**ファイル**: `Scripts/VoiceDetection/VoiceDetector.cs`

```csharp
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
```

### Step 2.2: 音量検知の実装
**ファイル**: `Scripts/VoiceDetection/VolumeAnalyzer.cs`

```csharp
using UnityEngine;

public class VolumeAnalyzer : MonoBehaviour
{
    [Header("Volume Settings")]
    public float volumeSensitivity = 1.0f;
    public float volumeThreshold = 0.01f;
    
    private VoiceDetector voiceDetector;
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
    }
    
    void Update()
    {
        float[] samples = voiceDetector.GetAudioSamples();
        if (samples != null)
        {
            float volume = CalculateVolume(samples);
            ProcessVolume(volume);
        }
    }
    
    float CalculateVolume(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / samples.Length) * volumeSensitivity;
    }
    
    void ProcessVolume(float volume)
    {
        // 音量が閾値以上の場合のみ処理
        if (volume > volumeThreshold)
        {
            // 音量に基づく処理をここに実装
            Debug.Log($"Volume: {volume:F3}");
            
            // イベント発火
            OnVolumeDetected?.Invoke(volume);
        }
    }
    
    public System.Action<float> OnVolumeDetected;
}
```

### Step 2.3: 基本音程検知の実装
**ファイル**: `Scripts/VoiceDetection/PitchAnalyzer.cs`

```csharp
using UnityEngine;
using Unity.Mathematics;

public class PitchAnalyzer : MonoBehaviour
{
    [Header("Pitch Settings")]
    public float pitchSensitivity = 1.0f;
    public float minFrequency = 80f;  // 最低周波数
    public float maxFrequency = 1000f; // 最高周波数
    
    private VoiceDetector voiceDetector;
    private float[] fftBuffer;
    private Complex[] fftResult;
    
    void Start()
    {
        voiceDetector = FindObjectOfType<VoiceDetector>();
        int bufferSize = voiceDetector.bufferSize;
        fftBuffer = new float[bufferSize];
        fftResult = new Complex[bufferSize];
    }
    
    void Update()
    {
        float[] samples = voiceDetector.GetAudioSamples();
        if (samples != null)
        {
            float pitch = CalculatePitch(samples);
            ProcessPitch(pitch);
        }
    }
    
    float CalculatePitch(float[] samples)
    {
        // ハミング窓を適用
        ApplyHammingWindow(samples, fftBuffer);
        
        // FFT実行（簡易版）
        PerformFFT(fftBuffer, fftResult);
        
        // 最大振幅の周波数を検出
        float maxMagnitude = 0;
        int maxIndex = 0;
        
        for (int i = 1; i < fftResult.Length / 2; i++)
        {
            float magnitude = math.sqrt(fftResult[i].real * fftResult[i].real + 
                                      fftResult[i].imag * fftResult[i].imag);
            
            if (magnitude > maxMagnitude)
            {
                maxMagnitude = magnitude;
                maxIndex = i;
            }
        }
        
        // 周波数に変換
        float frequency = (float)maxIndex * voiceDetector.sampleRate / fftResult.Length;
        
        // 範囲内の周波数のみ返す
        if (frequency >= minFrequency && frequency <= maxFrequency)
        {
            return frequency;
        }
        
        return 0f;
    }
    
    void ApplyHammingWindow(float[] input, float[] output)
    {
        for (int i = 0; i < input.Length; i++)
        {
            float window = 0.54f - 0.46f * math.cos(2 * math.PI * i / (input.Length - 1));
            output[i] = input[i] * window;
        }
    }
    
    void PerformFFT(float[] input, Complex[] output)
    {
        // 簡易FFT実装（実際のプロジェクトでは外部ライブラリを使用推奨）
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = new Complex(input[i], 0);
        }
        
        // ここでFFTアルゴリズムを実装
        // 実際にはUnity.MathematicsのFFT関数を使用
    }
    
    void ProcessPitch(float pitch)
    {
        if (pitch > 0)
        {
            Debug.Log($"Pitch: {pitch:F1} Hz");
            
            // イベント発火
            OnPitchDetected?.Invoke(pitch);
        }
    }
    
    public System.Action<float> OnPitchDetected;
}
```

## Phase 3: ゲームロジックの実装

### Step 3.1: 音声コントローラーの実装
**ファイル**: `Scripts/GameLogic/VoiceController.cs`

```csharp
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
        Vector3 currentVelocity = rb.velocity;
        if (currentVelocity.magnitude > 0)
        {
            rb.velocity = currentVelocity.normalized * speed;
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
```

### Step 3.2: ゲームマネージャーの実装
**ファイル**: `Scripts/GameLogic/GameManager.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider volumeSlider;
    public Slider pitchSlider;
    public Text volumeText;
    public Text pitchText;
    
    [Header("Game Settings")]
    public float maxVolume = 1f;
    public float maxPitch = 1000f;
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    
    void Start()
    {
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        
        // UI初期化
        InitializeUI();
        
        // イベント購読
        volumeAnalyzer.OnVolumeDetected += UpdateVolumeUI;
        pitchAnalyzer.OnPitchDetected += UpdatePitchUI;
    }
    
    void InitializeUI()
    {
        if (volumeSlider != null)
        {
            volumeSlider.maxValue = maxVolume;
            volumeSlider.value = 0;
        }
        
        if (pitchSlider != null)
        {
            pitchSlider.maxValue = maxPitch;
            pitchSlider.value = 0;
        }
    }
    
    void UpdateVolumeUI(float volume)
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = Mathf.Clamp(volume, 0, maxVolume);
        }
        
        if (volumeText != null)
        {
            volumeText.text = $"Volume: {volume:F3}";
        }
    }
    
    void UpdatePitchUI(float pitch)
    {
        if (pitchSlider != null)
        {
            pitchSlider.value = Mathf.Clamp(pitch, 0, maxPitch);
        }
        
        if (pitchText != null)
        {
            pitchText.text = $"Pitch: {pitch:F1} Hz";
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= UpdateVolumeUI;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected -= UpdatePitchUI;
    }
}
```

## Phase 4: UI実装

### Step 4.1: 音声可視化の実装
**ファイル**: `Scripts/UI/VoiceVisualizer.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;

public class VoiceVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    public Image volumeBar;
    public Image pitchBar;
    public Color lowVolumeColor = Color.green;
    public Color highVolumeColor = Color.red;
    public Color lowPitchColor = Color.blue;
    public Color highPitchColor = Color.yellow;
    
    private VolumeAnalyzer volumeAnalyzer;
    private PitchAnalyzer pitchAnalyzer;
    
    void Start()
    {
        volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
        pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
        
        // イベント購読
        volumeAnalyzer.OnVolumeDetected += UpdateVolumeVisualization;
        pitchAnalyzer.OnPitchDetected += UpdatePitchVisualization;
    }
    
    void UpdateVolumeVisualization(float volume)
    {
        if (volumeBar != null)
        {
            volumeBar.fillAmount = Mathf.Clamp01(volume);
            
            // 音量に応じて色を変更
            volumeBar.color = Color.Lerp(lowVolumeColor, highVolumeColor, volume);
        }
    }
    
    void UpdatePitchVisualization(float pitch)
    {
        if (pitchBar != null)
        {
            float normalizedPitch = Mathf.Clamp01((pitch - 200f) / 800f);
            pitchBar.fillAmount = normalizedPitch;
            
            // 音程に応じて色を変更
            pitchBar.color = Color.Lerp(lowPitchColor, highPitchColor, normalizedPitch);
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected -= UpdateVolumeVisualization;
        if (pitchAnalyzer != null)
            pitchAnalyzer.OnPitchDetected -= UpdatePitchVisualization;
    }
}
```

## Phase 5: テストとデバッグ

### Step 5.1: テストシーンの作成
- [ ] 基本的な3Dシーンを作成
- [ ] プレイヤーオブジェクト（Cube）を配置
- [ ] 音声検知スクリプトをアタッチ
- [ ] UI要素を配置

### Step 5.2: デバッグ機能の実装
- [ ] コンソールログの出力
- [ ] リアルタイムでの音量・音程表示
- [ ] エラーハンドリングの実装

### Step 5.3: パフォーマンステスト
- [ ] フレームレートの監視
- [ ] メモリ使用量の確認
- [ ] 音声処理の遅延測定

## Phase 6: 最適化と改善

### Step 6.1: パフォーマンス最適化
- [ ] バッファサイズの調整
- [ ] サンプリングレートの最適化
- [ ] 不要な計算の削減

### Step 6.2: 精度向上
- [ ] ノイズ除去フィルタの実装
- [ ] 複数フレームでの平均化
- [ ] 適切な窓関数の使用

### Step 6.3: 外部ライブラリの統合（必要に応じて）
- [ ] NAudioライブラリの統合
- [ ] 高精度FFTの実装
- [ ] リアルタイム音声処理の改善

## 実装チェックリスト

### Phase 1: 環境構築
- [ ] Unity プロジェクト設定
- [ ] 必要なパッケージのインストール
- [ ] プロジェクト構造の作成

### Phase 2: 基本音声検知
- [ ] マイク入力の実装
- [ ] 音量検知の実装
- [ ] 基本音程検知の実装

### Phase 3: ゲームロジック
- [ ] 音声コントローラーの実装
- [ ] ゲームマネージャーの実装

### Phase 4: UI実装
- [ ] 音声可視化の実装
- [ ] リアルタイム表示の実装

### Phase 5: テストとデバッグ
- [ ] テストシーンの作成
- [ ] デバッグ機能の実装
- [ ] パフォーマンステスト

### Phase 6: 最適化と改善
- [ ] パフォーマンス最適化
- [ ] 精度向上
- [ ] 外部ライブラリの統合（必要に応じて）

## 次のステップ
1. **Phase 1**から順番に実装を開始
2. 各ステップで動作確認を行う
3. 問題があれば前のステップに戻って修正
4. 段階的に機能を追加していく

この実装ステップに従って進めることで、段階的に音声検知ゲームを構築できます。
