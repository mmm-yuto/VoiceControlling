# Phase 1: コアゲームシステム 使い方ガイド

## 概要

Phase 1では、声で画面に色を塗れる基本機能を実装しました。このガイドでは、実装したコンポーネントの使い方とセットアップ手順を説明します。

## 実装されたコンポーネント

### 1. IPaintCanvas（インターフェース）
- **ファイル**: `Assets/Main/Script/Interfaces/IPaintCanvas.cs`
- **説明**: 塗りシステムのインターフェース定義
- **用途**: 直接使用する必要はありません（`PaintCanvas`が実装）

### 2. PaintSettings（ScriptableObject）
- **ファイル**: `Assets/Main/Script/Data/Settings/PaintSettings.cs`
- **説明**: 塗りシステムの設定を管理
- **用途**: Inspectorで調整可能な設定アセット

### 3. PaintCanvas（塗りキャンバス）
- **ファイル**: `Assets/Main/Script/GameLogic/PaintCanvas.cs`
- **説明**: 画面に色を塗る機能を実装
- **用途**: 塗り状態を管理し、塗り処理を実行

### 4. VoiceToScreenMapper（座標変換）
- **ファイル**: `Assets/Main/Script/GameLogic/VoiceToScreenMapper.cs`
- **説明**: 音声値（音量・ピッチ）を画面座標に変換
- **用途**: 音声入力から画面位置への変換

### 5. PaintBattleGameManager（ゲームマネージャー）
- **ファイル**: `Assets/Main/Script/GameLogic/PaintBattleGameManager.cs`
- **説明**: 音声検出 → 座標変換 → 塗り処理の統合
- **用途**: ゲームのメインロジック

### 6. InkEffect（インクエフェクト）
- **ファイル**: `Assets/Main/Script/Graphics/InkEffect.cs`
- **説明**: 塗り位置にパーティクルエフェクトを表示
- **用途**: 視覚的なフィードバック（オプション）

### 7. VoiceDebugSimulator（デバッグモード）
- **ファイル**: `Assets/Main/Script/Debug/VoiceDebugSimulator.cs`
- **説明**: マウス操作で音声入力をシミュレート
- **用途**: 開発・テスト効率向上（オプション）

---

## セットアップ手順

### Step 1: PaintSettingsアセットの作成

1. Unityエディタで、`Assets/Main/Script/Data/Settings/` フォルダを開く
2. 右クリック → `Create` → `Game` → `Paint Settings`
3. アセット名を `PaintSettings` に変更
4. Inspectorで設定を調整：
   - **Paint Intensity Multiplier**: 塗り強度の倍率（デフォルト: 1.0）
   - **Update Frequency**: 更新頻度（1=毎フレーム、デフォルト: 1）
   - **Texture Width/Height**: キャンバス解像度（デフォルト: 960x540）
   - **Min Volume Threshold**: 塗りの最小音量閾値（デフォルト: 0.01）

### Step 2: シーンにコンポーネントを追加

#### 2.1: PaintCanvasの追加

1. シーンに空のGameObjectを作成（例: `PaintCanvas`）
2. `PaintCanvas`コンポーネントを追加
3. Inspectorで以下を設定：
   - **Settings**: Step 1で作成した`PaintSettings`アセットをドラッグ&ドロップ
   - **Show Debug Gizmos**: デバッグ表示のON/OFF（オプション）

#### 2.2: VoiceToScreenMapperの追加

1. シーンに空のGameObjectを作成（例: `VoiceToScreenMapper`）
2. `VoiceToScreenMapper`コンポーネントを追加
3. Inspectorで以下を設定（自動同期されるが、手動調整も可能）：
   - **Max Volume**: ボリュームの最大値（`VoiceDisplay`と同期）
   - **Min/Max Pitch**: ピッチの範囲（`ImprovedPitchAnalyzer`と同期）
   - **Match Slider Y Axis**: Y軸の正規化方法（デフォルト: true）

#### 2.3: PaintBattleGameManagerの追加

1. シーンに空のGameObjectを作成（例: `PaintBattleGameManager`）
2. `PaintBattleGameManager`コンポーネントを追加
3. Inspectorで以下を設定：
   - **Volume Analyzer**: 既存の`VolumeAnalyzer`コンポーネントをドラッグ&ドロップ
   - **Improved Pitch Analyzer**: 既存の`ImprovedPitchAnalyzer`コンポーネントをドラッグ&ドロップ
   - **Voice To Screen Mapper**: Step 2.2で作成した`VoiceToScreenMapper`をドラッグ&ドロップ
   - **Paint Canvas**: Step 2.1で作成した`PaintCanvas`をドラッグ&ドロップ
   - **Player Id**: プレイヤーID（Phase 1では1で固定）
   - **Paint Speed Multiplier**: 塗り速度の倍率（デフォルト: 1.0）
   - **Silence Volume Threshold**: 無音判定の音量閾値（デフォルト: 0.01）

#### 2.4: InkEffectの追加（オプション）

1. シーンに空のGameObjectを作成（例: `InkEffect`）
2. `InkEffect`コンポーネントを追加
3. Inspectorで以下を設定：
   - **Paint Canvas**: Step 2.1で作成した`PaintCanvas`をドラッグ&ドロップ
   - **Particle Color**: パーティクルの色（デフォルト: Cyan）
   - **Particle Size**: パーティクルのサイズ（デフォルト: 0.5）
   - **Particle Lifetime**: パーティクルの生存時間（デフォルト: 1秒）
   - **Particle Count**: パーティクルの数（デフォルト: 10）

   **注意**: パーティクルシステムは自動生成されますが、手動で設定することも可能です。

#### 2.5: VoiceDebugSimulatorの追加（オプション・推奨）

1. シーンに空のGameObjectを作成（例: `VoiceDebugSimulator`）
2. `VoiceDebugSimulator`コンポーネントを追加
3. Inspectorで以下を設定：
   - **Enable Debug Mode**: デバッグモードを有効にする（チェック）
   - **Scatter Plot**: 既存の`VoiceScatterPlot`コンポーネントをドラッグ&ドロップ
   - **Max Volume/Min/Max Pitch**: 範囲設定（自動同期されるが、手動調整も可能）

---

## 使用方法

### 基本的な使い方

1. **キャリブレーションの実行**
   - 既存の`VoiceCalibrator`でキャリブレーションを実行
   - キャリブレーション平均が`VoiceToScreenMapper`に自動的に反映されます

2. **声を出して塗る**
   - マイクに向かって声を出すと、音量とピッチに応じて画面の位置が決定されます
   - キャリブレーション平均を原点として使用します
   - 無音時は塗り処理が停止します

3. **デバッグモードの使用（オプション）**
   - `VoiceDebugSimulator`の`Enable Debug Mode`をONにする
   - マウスの左クリックで音声入力をシミュレート
   - `VoiceScatterPlot`のplotArea内をクリックすると、その位置に対応する音量・ピッチが計算されます
   - 声を出さずにテストできます

### 動作確認

#### 確認項目

1. **音声検出の確認**
   - `VolumeAnalyzer`と`ImprovedPitchAnalyzer`が正常に動作していることを確認
   - `VoiceScatterPlot`のマーカーが動いていることを確認

2. **座標変換の確認**
   - `VoiceToScreenMapper`がキャリブレーション平均を正しく取得していることを確認
   - 音声値が画面座標に正しく変換されていることを確認

3. **塗り処理の確認**
   - `PaintCanvas`が`PaintSettings`を正しく読み込んでいることを確認
   - `PaintBattleGameManager`が音声検出イベントを受信していることを確認
   - 塗り処理が実行されていることを確認（デバッグログで確認可能）

4. **エフェクトの確認（オプション）**
   - `InkEffect`が`PaintCanvas.OnPaintCompleted`イベントを受信していることを確認
   - パーティクルが表示されていることを確認

### デバッグ方法

#### ログ出力の確認

- `PaintCanvas`の`Show Debug Gizmos`をONにすると、塗り位置がログに出力されます
- `PaintBattleGameManager`は、参照が見つからない場合にエラーログを出力します

#### デバッグモードの活用

- `VoiceDebugSimulator`を使用すると、声を出さずにテストできます
- マウス位置から音量・ピッチへの逆変換が正しく動作していることを確認できます

---

## トラブルシューティング

### 問題: 塗り処理が実行されない

**原因と対処法**:
1. **参照が設定されていない**
   - `PaintBattleGameManager`のInspectorで、すべての参照が正しく設定されているか確認
   - `VolumeAnalyzer`、`ImprovedPitchAnalyzer`、`VoiceToScreenMapper`、`PaintCanvas`がすべて接続されているか確認

2. **音量が閾値以下**
   - `PaintSettings`の`Min Volume Threshold`を確認
   - `PaintBattleGameManager`の`Silence Volume Threshold`を確認
   - 音量が閾値以上になっているか確認

3. **キャリブレーションが実行されていない**
   - `VoiceCalibrator`でキャリブレーションを実行
   - `VoiceToScreenMapper`がキャリブレーション平均を取得しているか確認

### 問題: 座標変換が正しく動作しない

**原因と対処法**:
1. **範囲設定の不一致**
   - `VoiceToScreenMapper`の範囲設定が`VoiceDisplay`や`ImprovedPitchAnalyzer`と一致しているか確認
   - 自動同期が機能していない場合は、手動で設定を合わせる

2. **キャリブレーション平均の取得失敗**
   - `VoiceCalibrator.LastAverageVolume`と`LastAveragePitch`が正しく設定されているか確認
   - キャリブレーションを再実行

### 問題: パーティクルエフェクトが表示されない

**原因と対処法**:
1. **カメラの設定**
   - シーンに`Camera`（`Main Camera`）が存在するか確認
   - カメラの`Tag`が`MainCamera`に設定されているか確認

2. **参照の設定**
   - `InkEffect`の`Paint Canvas`参照が正しく設定されているか確認
   - `PaintCanvas.OnPaintCompleted`イベントが発火しているか確認

3. **パーティクルシステムの生成**
   - `InkEffect`が自動的にパーティクルシステムを生成するか確認
   - 手動でパーティクルシステムを設定する場合は、Inspectorで接続

### 問題: デバッグモードが動作しない

**原因と対処法**:
1. **デバッグモードが有効になっていない**
   - `VoiceDebugSimulator`の`Enable Debug Mode`がONになっているか確認

2. **参照の設定**
   - `VoiceDebugSimulator`の`Scatter Plot`参照が正しく設定されているか確認
   - `VoiceScatterPlot`の`plotArea`が設定されているか確認

3. **マウス位置の変換**
   - `VoiceScatterPlot`の`plotArea`内をクリックしているか確認
   - 画面外をクリックしている場合は動作しません

---

## 次のステップ

Phase 1が完成したら、以下のステップに進むことができます：

1. **Phase 2: クリエイティブモード**
   - 声で自由に絵を描くモード
   - `PaintCanvas.ResetCanvas()`を使用してクリアボタンを実装

2. **攻撃タイプシステムの追加**
   - インパクトショットとストリームペイントの実装
   - 塗り方のバリエーション追加

3. **上塗り機能の追加**
   - 既存の色を上書きする機能
   - 陣地奪い合いの実装

4. **可視化の改善**
   - Texture2D + UI Imageで塗りデータを可視化
   - より高度なエフェクトの追加

---

## 参考情報

### 既存コンポーネントとの連携

- **VoiceCalibrator**: キャリブレーション平均を提供
- **VolumeAnalyzer**: 音量検出イベントを提供
- **ImprovedPitchAnalyzer**: ピッチ検出イベントを提供
- **VoiceScatterPlot**: 座標変換ロジックの参考（UI座標系）
- **VoiceDisplay**: 範囲設定の同期

### イベントフロー

```
1. VolumeAnalyzer.OnVolumeDetected
   ↓
2. ImprovedPitchAnalyzer.OnPitchDetected
   ↓
3. PaintBattleGameManager.Update()
   ↓
4. VoiceToScreenMapper.MapVoiceToScreen()
   ↓
5. PaintCanvas.PaintAt()
   ↓
6. PaintCanvas.OnPaintCompleted
   ↓
7. InkEffect.OnPaintCompleted（オプション）
```

### 設定の推奨値

- **PaintSettings**:
  - Texture Width/Height: 960x540（軽量）または 1920x1080（高解像度）
  - Update Frequency: 1（毎フレーム）または 2-3（パフォーマンス重視）
  - Min Volume Threshold: 0.01（デフォルト）

- **PaintBattleGameManager**:
  - Paint Speed Multiplier: 1.0（デフォルト）
  - Silence Volume Threshold: 0.01（デフォルト）

---

## まとめ

Phase 1では、声で画面に色を塗れる基本機能を実装しました。このシステムは、Phase 2のクリエイティブモードや、その後のゲームモードの基盤となります。

問題が発生した場合は、このガイドのトラブルシューティングセクションを参照してください。

