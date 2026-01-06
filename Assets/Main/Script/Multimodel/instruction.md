# Multimodel System - 音声とタッチによる色変更システム

## 概要

既存のシステムとは別の新しいシーンとコードで実装する、音声とタッチを組み合わせた色変更システム。

## アプリケーションの動作フロー

```
1. ユーザーのNeutral Soundを検出
   ↓
2. 画面上にあるオブジェクトをタッチする
   ↓
3. 声を出す
   ↓
4. 声のピッチ・ボリュームをNeutral Soundと比較する
   ↓
5. その比率によってタッチされたオブジェクトの色を変更する
```

## 機能要件

### 1. Neutral Sound検出
- ユーザーの基準となる音声（Neutral Sound）を記録・保存
- ピッチとボリュームの基準値を取得
- カリブレーション機能

### 2. オブジェクトタッチ検出
- 画面上のオブジェクトをタッチ/クリックで選択
- タッチされたオブジェクトを識別

### 3. 音声検出（タッチ中）
- タッチされている間、音声を検出
- ピッチとボリュームをリアルタイムで取得

### 4. 比率計算
- 現在の音声のピッチ・ボリュームをNeutral Soundと比較
- 比率を計算（例: 現在のピッチ / Neutral Soundのピッチ）
- 比率を0.0～1.0の範囲に正規化

### 5. 色変更
- 計算した比率に基づいてオブジェクトの色を変更
- ピッチ比率とボリューム比率を色の要素（RGB、HSV等）にマッピング

## 技術要件

### 使用する既存コンポーネント（参照のみ）
- `VoiceDetector`: マイク入力の取得
- `VolumeAnalyzer`: 音量の検出
- `ImprovedPitchAnalyzer`: ピッチの検出

### 新規実装コンポーネント
全て `Assets/Main/Script/Multimodel/` フォルダに配置

1. **NeutralSoundDetector.cs**
   - Neutral Soundの検出・記録
   - 基準値（ピッチ、ボリューム）の保存

2. **TouchableObject.cs**
   - タッチ可能なオブジェクトのコンポーネント
   - タッチ検出
   - 色変更処理

3. **ColorCalculator.cs**
   - ピッチ・ボリューム比率から色を計算
   - 色のマッピングロジック

4. **MultimodelManager.cs**
   - 全体の制御
   - Neutral Sound検出とオブジェクトタッチの連携

5. **MultimodelSceneManager.cs**（オプション）
   - シーン管理
   - UI管理

## 実装方針

### 色の計算方法
- **ピッチ比率**: 色相（Hue）にマッピング
- **ボリューム比率**: 明度（Brightness）または彩度（Saturation）にマッピング
- または、RGBの各要素にマッピング

### Neutral Soundの基準値
- デフォルト値: ピッチ = 400Hz, ボリューム = 0.5
- ユーザーがカリブレーションで設定可能

### タッチ検出
- UnityのInputシステムを使用（マウス/タッチ）
- Raycastでオブジェクトを検出

## ファイル構成

```
Assets/Main/Script/Multimodel/
├── instruction.md (このファイル)
├── NeutralSoundDetector.cs
├── TouchableObject.cs
├── ColorCalculator.cs
├── MultimodelManager.cs
└── MultimodelSceneManager.cs (オプション)
```

## 実装順序

1. ✅ NeutralSoundDetector.cs - Neutral Sound検出機能（実装完了）
2. ✅ TouchableObject.cs - タッチ検出と色変更（実装完了）
3. ✅ ColorCalculator.cs - 色計算ロジック（実装完了）
4. ✅ MultimodelManager.cs - 全体制御（実装完了）
5. ⏳ 新しいシーンを作成し、コンポーネントを配置・テスト

## 実装済みコンポーネント

### NeutralSoundDetector.cs
- Neutral Soundの検出・記録機能
- 基準値（ピッチ、ボリューム）の保存・読み込み
- 検出開始/停止/キャンセル機能
- イベント通知

### ColorCalculator.cs
- ピッチ比率に基づいて4種類の色から選択
- 4つの区分:
  1. NeutralSound（比率 ≈ 1.0）
  2. ピッチがNeutralSound以下（比率 < 1.0）
  3. 1.0 <= 比率 < 1.5
  4. 比率 >= 1.5
- 各区分の色はInspectorで設定可能

### TouchableObject.cs
- タッチ/マウスクリック検出
- タッチ中の音声検出とリアルタイム色変更
- 色のスムージング機能
- Collider2D必須

### MultimodelManager.cs
- 全体制御と連携管理
- UIボタンとの連携
- 状態管理とイベント処理

## 使用方法

### 1. シーン設定
1. 新しいシーンを作成（例: `MultimodelScene`）
2. 必要なコンポーネントを配置:
   - `VoiceDetector`
   - `VolumeAnalyzer`
   - `ImprovedPitchAnalyzer`
   - `NeutralSoundDetector`
   - `ColorCalculator`
   - `MultimodelManager`
   - `TouchableObject`（色を変更したいオブジェクトにアタッチ）

### 2. Neutral Sound検出
1. `MultimodelManager`の「検出開始」ボタンをクリック
2. 声を出す（約1秒間）
3. 自動的に検出が完了

### 3. オブジェクトの色変更
1. 画面上のオブジェクト（`TouchableObject`がアタッチされている）をタッチ/クリック
2. タッチ中に声を出す
3. 声のピッチ・ボリュームに応じて色が変化

## 設定項目

### NeutralSoundDetector
- `Sample Count`: 検出サンプル数（デフォルト: 30）
- `Min Volume Threshold`: 最小音量閾値
- `Max Volume Threshold`: 最大音量閾値

### ColorCalculator
- `Neutral Sound Color`: NeutralSound（比率 ≈ 1.0）の色
- `Low Pitch Color`: ピッチがNeutralSound以下（比率 < 1.0）の色
- `Medium Pitch Color`: 1.0 <= 比率 < 1.5 の色
- `High Pitch Color`: 比率 >= 1.5 の色
- `Neutral Sound Threshold`: NeutralSoundの判定範囲（デフォルト: 0.05）

### TouchableObject
- `Color Smoothing`: 色変更のスムージング係数
- `Audio Update Frequency`: 音声検出の更新頻度

