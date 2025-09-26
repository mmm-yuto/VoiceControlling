# 音量・音程検知の実装方法

## 概要
Unityでマイクからの音声をリアルタイムで分析し、音量と音程（ピッチ）を検知する方法について説明します。

## 必要な技術・ライブラリ

### 1. Unity Audio System
- **Microphone** クラス：マイクからの音声データを取得
- **AudioSource** コンポーネント：音声の再生・録音
- **AudioClip** クラス：音声データの格納

### 2. 音声解析ライブラリ
- **Unity Audio Analysis**：Unity公式の音声解析ツール
- **NAudio**：.NET用の音声処理ライブラリ（Unityで使用可能）
- **FFT（Fast Fourier Transform）**：周波数解析のためのアルゴリズム

## 実装手順

### Step 1: マイクからの音声取得
```csharp
// マイクデバイスの確認
string[] devices = Microphone.devices;

// マイクからの録音開始
AudioClip clip = Microphone.Start(deviceName, true, 1, sampleRate);
```

### Step 2: 音量検知の実装
```csharp
// 音量（RMS - Root Mean Square）の計算
float CalculateVolume(float[] samples)
{
    float sum = 0;
    for (int i = 0; i < samples.Length; i++)
    {
        sum += samples[i] * samples[i];
    }
    return Mathf.Sqrt(sum / samples.Length);
}
```

### Step 3: 音程検知の実装
```csharp
// FFTを使用した周波数解析
float DetectPitch(float[] samples, int sampleRate)
{
    // FFTを実行して周波数スペクトラムを取得
    Complex[] fft = FFT(samples);
    
    // 最大振幅を持つ周波数を検出
    float maxMagnitude = 0;
    int maxIndex = 0;
    
    for (int i = 0; i < fft.Length / 2; i++)
    {
        float magnitude = fft[i].Magnitude;
        if (magnitude > maxMagnitude)
        {
            maxMagnitude = magnitude;
            maxIndex = i;
        }
    }
    
    // インデックスを周波数に変換
    float frequency = (float)maxIndex * sampleRate / fft.Length;
    return frequency;
}
```

## 必要なパッケージ・アセット

### Unity Package Manager
1. **Audio** パッケージ（標準で含まれている）
2. **Mathematics** パッケージ（FFT計算用）

### 外部ライブラリ
1. **Unity Audio Analysis**（GitHubからダウンロード）
2. **NAudio**（NuGetから取得、Unity用に調整が必要）

## 実装時の注意点

### 1. パフォーマンス
- リアルタイム処理のため、計算量を最小限に抑える
- 適切なサンプリングレート（44.1kHz推奨）の設定
- バッファサイズの最適化

### 2. 精度
- ノイズ除去フィルタの実装
- 複数フレームでの平均化
- 適切な窓関数（ハミング窓など）の使用

### 3. プラットフォーム対応
- Windows、Mac、Android、iOSでの動作確認
- 各プラットフォームでのマイク権限の取得

## 推奨実装順序

1. **基本的なマイク入力**の実装
2. **音量検知**の実装とテスト
3. **FFTライブラリ**の統合
4. **音程検知**の実装
5. **ノイズ除去**と精度向上
6. **UI表示**の実装
7. **ゲームロジック**との統合

## 参考リソース

- [Unity Microphone Documentation](https://docs.unity3d.com/ScriptReference/Microphone.html)
- [Audio Analysis in Unity](https://github.com/keijiro/AudioAnalysis)
- [FFT Implementation for Unity](https://github.com/keijiro/KvantSpray)
- [Real-time Audio Processing](https://docs.unity3d.com/Manual/AudioMixer.html)

## 次のステップ
1. 基本的なマイク入力スクリプトの作成
2. 音量検知のテスト実装
3. FFTライブラリの調査と選択
