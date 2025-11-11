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

### 8. UIコンポーネント（既存・オプション）

Phase 1では、塗りデータの可視化は**パーティクルエフェクト（InkEffect）のみ**です。以下の既存UIコンポーネントは、Phase 1でも使用できますが、必須ではありません：

- **VoiceScatterPlot**: 音声入力の可視化（オプション・推奨）
- **VoiceDisplay**: 音量・ピッチの数値表示（オプション）
- **VoiceCalibrator**: キャリブレーションUI（推奨）

**注意**: 塗りデータ自体を画面に表示するUI（Texture2D + UI Image）は、Phase 1では実装していません。これはPhase 1後の実装となります。

---

## セットアップ手順

### Step 1: PaintSettingsアセットの作成

#### 推奨：専用フォルダに作成（整理しやすい）

1. Unityエディタで、`Assets`フォルダを右クリック → `Create` → `Folder`
2. フォルダ名を `ScriptableObjects` に変更
3. `ScriptableObjects`フォルダを右クリック → `Create` → `Folder`
4. フォルダ名を `Settings` に変更
5. `Assets/ScriptableObjects/Settings/` フォルダを開く
6. 右クリック → `Create` → `Game` → `Paint Settings`
7. アセット名を `PaintSettings` に変更
8. Inspectorで設定を調整：
   - **Paint Intensity Multiplier**: 塗り強度の倍率（デフォルト: 1.0）
   - **Update Frequency**: 更新頻度（1=毎フレーム、デフォルト: 1）
   - **Texture Width/Height**: キャンバス解像度（デフォルト: 960x540）
   - **Min Volume Threshold**: 塗りの最小音量閾値（デフォルト: 0.01）

#### 代替：スクリプトと同じ場所に作成（簡易）

スクリプトと同じ場所に作成することも可能です：
1. Unityエディタで、`Assets/Main/Script/Data/Settings/` フォルダを開く
2. 右クリック → `Create` → `Game` → `Paint Settings`
3. アセット名を `PaintSettings` に変更
4. Inspectorで設定を調整（上記と同じ）

**注意**: アセットの配置場所は機能に影響しませんが、プロジェクトの整理のため、専用フォルダ（`Assets/ScriptableObjects/Settings/`）に作成することを推奨します。

### Step 2: シーンにコンポーネントを追加

**重要**: Phase 1のコンポーネントを使用するには、既存の音声検出システム（`VoiceDetector`、`VolumeAnalyzer`、`ImprovedPitchAnalyzer`）がシーンに存在する必要があります。これらが存在しない場合は、まず追加してください。

#### 2.0: 既存の音声検出システムの確認（必須）

Phase 1のコンポーネントは、既存の音声検出システムに依存しています。以下のコンポーネントがシーンに存在することを確認してください：

1. **VoiceDetector**（必須）
   - マイク入力の取得
   - シーンに`VoiceDetector`コンポーネントがアタッチされているGameObjectが必要
   - マイクの権限が許可されていることを確認

2. **VolumeAnalyzer**（必須）
   - 音量の検出
   - `VoiceDetector`に依存
   - `OnVolumeDetected`イベントを発火

3. **ImprovedPitchAnalyzer**（必須）
   - ピッチの検出
   - `VoiceDetector`に依存
   - `OnPitchDetected`イベントを発火

**これらのコンポーネントが存在しない場合**:
- 既存のシーン（例: `VoiceTestScene`）を確認して、これらのコンポーネントがどのように設定されているか参考にしてください
- または、新しいGameObjectを作成して、それぞれのコンポーネントを追加してください

#### 2.1: PaintCanvasの追加

1. シーンに空のGameObjectを作成（例: `PaintCanvas`）
2. `PaintCanvas`コンポーネントを追加
3. Inspectorで以下を設定：
   - **Settings**: Step 1で作成した`PaintSettings`アセットをProjectウィンドウからInspectorのフィールドにドラッグ&ドロップ
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
3. **Inspectorウィンドウで設定**：
   - `PaintBattleGameManager`がアタッチされているGameObjectを選択
   - Inspectorウィンドウに`PaintBattleGameManager`コンポーネントが表示されます
   - 以下のフィールドに、**Hierarchyウィンドウから対応するGameObjectをドラッグ&ドロップ**：
     - **Volume Analyzer**: Hierarchyで`VolumeAnalyzer`コンポーネントがアタッチされているGameObjectをドラッグ&ドロップ
     - **Improved Pitch Analyzer**: Hierarchyで`ImprovedPitchAnalyzer`コンポーネントがアタッチされているGameObjectをドラッグ&ドロップ
     - **Voice To Screen Mapper**: Step 2.2で作成した`VoiceToScreenMapper`のGameObjectをドラッグ&ドロップ
     - **Paint Canvas**: Step 2.1で作成した`PaintCanvas`のGameObjectをドラッグ&ドロップ
     - **Voice Debug Simulator**: Step 2.5で作成した`VoiceDebugSimulator`のGameObjectをドラッグ&ドロップ（デバッグモードを使用する場合）
   - 数値フィールドは直接入力：
     - **Player Id**: プレイヤーID（Phase 1では1で固定）
     - **Paint Speed Multiplier**: 塗り速度の倍率（デフォルト: 1.0）
     - **Silence Volume Threshold**: 無音判定の音量閾値（デフォルト: 0.01）

**デバッグモードの動作**:
- `VoiceDebugSimulator`の`Enable Debug Mode`がONで、`PaintBattleGameManager`の`Voice Debug Simulator`フィールドに接続されている場合、デバッグモードが有効になります
- デバッグモード時は、`VolumeAnalyzer`と`ImprovedPitchAnalyzer`の代わりに`VoiceDebugSimulator`のイベントを使用します
- マウスの左クリックを押したまま動かすと、パーティクルがマウス位置に応じて表示されます

**通常モードとデバッグモードの違い**:
- **入力方法**: 通常モードは音声入力、デバッグモードはマウス入力
- **処理の流れ**: どちらも同じイベント（`OnVolumeDetected`、`OnPitchDetected`）を発火し、`PaintBattleGameManager`で同じ処理が実行されます
- **音量閾値**: 
  - 通常モード: `VolumeAnalyzer`が`volumeThreshold`をチェックし、閾値以上の場合のみイベントを発火
  - デバッグモード: 閾値チェックなし（常にイベントを発火）
- **無音判定**: `PaintBattleGameManager`の`Update()`で両方とも同じ無音判定が適用されます
- **座標変換**: どちらも`VoiceToScreenMapper.MapVoiceToScreen()`で同じ座標変換が実行されます

**結論**: 入力方法以外は基本的に同じ処理が実行されますが、音量閾値のチェックタイミングが異なります（通常モードは`VolumeAnalyzer`で、デバッグモードは`PaintBattleGameManager`で）。

**注意**: 
- GameObject全体をドラッグ&ドロップする必要があります（コンポーネント単体ではありません）
- または、フィールド右側の**円形アイコン（⚙️）**をクリックして、シーン内のGameObjectを選択することもできます
- 参照が正しく設定されると、フィールドにGameObject名が表示されます

#### 2.4: InkEffectの追加（オプション）

1. シーンに空のGameObjectを作成（例: `InkEffect`）
2. `InkEffect`コンポーネントを追加
3. Inspectorで以下を設定：
   - **Paint Canvas**: Step 2.1で作成した`PaintCanvas`のGameObjectをHierarchyからドラッグ&ドロップ
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
   - **Scatter Plot**: 既存の`VoiceScatterPlot`コンポーネントがアタッチされているGameObjectをHierarchyからドラッグ&ドロップ
   - **Max Volume/Min/Max Pitch**: 範囲設定（自動同期されるが、手動調整も可能）

#### 2.6: UIコンポーネントの確認（既存のUIを使用）

Phase 1では、塗りデータの可視化は**パーティクルエフェクト（InkEffect）のみ**です。Texture2D + UI Imageでの可視化はPhase 1後（将来の実装）となります。

ただし、既存の音声検出システムのUIコンポーネントは、Phase 1でも使用できます：

1. **VoiceScatterPlot**（オプション・推奨）
   - 音声入力（音量・ピッチ）の可視化
   - マーカーが動くことで、音声入力が正しく検出されているか確認できます
   - デバッグモード（VoiceDebugSimulator）と組み合わせて使用すると便利です

2. **VoiceDisplay**（オプション）
   - 音量・ピッチの数値表示
   - スライダーで音量・ピッチの範囲を調整可能

3. **VoiceCalibrator**（推奨）
   - キャリブレーションの実行
   - Phase 1では、キャリブレーション平均を原点として使用するため、実行を推奨します

**これらのUIコンポーネントは既存のシーンに存在する可能性があります**。存在しない場合は、必要に応じて追加してください。

**注意**: Phase 1では、塗りデータ自体を画面に表示するUIは実装していません。塗り位置の可視化は`InkEffect`のパーティクルエフェクトのみです。塗りデータの可視化（Texture2D + UI Image）は、Phase 1後の実装となります。

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
   - **マウスの左クリックを押したまま動かす**ことで音声入力をシミュレート
   - `VoiceScatterPlot`のplotArea内でクリックを押したままマウスを動かすと、その位置に対応する音量・ピッチが計算されます
   - クリックを離すと無音状態になります
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
   - **注意**: Phase 1では、塗りデータ自体を画面に表示するUIはありません。パーティクルエフェクトのみで可視化されます
   
5. **UIコンポーネントの確認（オプション）**
   - `VoiceScatterPlot`のマーカーが動いていることを確認（音声入力の可視化）
   - `VoiceDisplay`で音量・ピッチの数値が表示されていることを確認（オプション）
   - `VoiceCalibrator`でキャリブレーションが実行できることを確認（推奨）

### デバッグ方法

#### ログ出力の確認

- `PaintCanvas`の`Show Debug Gizmos`をONにすると、塗り位置がログに出力されます
- `PaintBattleGameManager`は、参照が見つからない場合にエラーログを出力します

#### デバッグモードの活用

- `VoiceDebugSimulator`を使用すると、声を出さずにテストできます
- **マウスの左クリックを押したまま動かす**ことで、マウス位置から音量・ピッチへの逆変換が正しく動作していることを確認できます
- `VoiceScatterPlot`のplotArea内でクリックを押したままマウスを動かすと、マーカーが追従します

---

## トラブルシューティング

### 問題: NullReferenceException（VoiceDetectorが見つからない）

**エラーメッセージ例**:
```
NullReferenceException: Object reference not set to an instance of an object
VolumeAnalyzer.Update () (at Assets/Main/Script/VoiceDetection/VolumeAnalyzer.cs:18)
```

**原因と対処法**:
1. **VoiceDetectorがシーンに存在しない**
   - Hierarchyで`VoiceDetector`コンポーネントがアタッチされているGameObjectが存在するか確認
   - 存在しない場合は、新しいGameObjectを作成して`VoiceDetector`コンポーネントを追加
   - `VoiceDetector`は`VolumeAnalyzer`と`ImprovedPitchAnalyzer`が依存しているため、必須です

2. **マイクの権限が許可されていない**
   - Unityの設定でマイクの権限が許可されているか確認
   - 実行時にマイクの権限を要求するダイアログが表示される場合は、許可してください

3. **マイクデバイスが接続されていない**
   - マイクが正しく接続されているか確認
   - `VoiceDetector`の初期化時にエラーログが表示されていないか確認

### 問題: 塗り処理が実行されない

**原因と対処法**:
1. **参照が設定されていない**
   - `PaintBattleGameManager`のInspectorで、すべての参照が正しく設定されているか確認
   - `VolumeAnalyzer`、`ImprovedPitchAnalyzer`、`VoiceToScreenMapper`、`PaintCanvas`がすべて接続されているか確認
   - **重要**: `VoiceDetector`がシーンに存在し、`VolumeAnalyzer`と`ImprovedPitchAnalyzer`が正常に動作していることを確認

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

### 問題: デバッグモードが動作しない、パーティクルが表示されない

**原因と対処法**:
1. **デバッグモードが有効になっていない**
   - `VoiceDebugSimulator`の`Enable Debug Mode`がONになっているか確認
   - **重要**: `PaintBattleGameManager`の`Voice Debug Simulator`フィールドに`VoiceDebugSimulator`のGameObjectが接続されているか確認
   - 接続されていない場合、デバッグモードのイベントが`PaintBattleGameManager`に届きません

2. **参照の設定**
   - `VoiceDebugSimulator`の`Scatter Plot`参照は**オプション**です。接続されていない場合は画面全体が入力エリアとして使用されます
   - `VoiceScatterPlot`の`plotArea`が設定されているか確認（`Scatter Plot`を接続している場合のみ）
   - **重要**: `PaintBattleGameManager`の`Voice Debug Simulator`フィールドに`VoiceDebugSimulator`が接続されているか確認
   - UnityのConsoleで警告メッセージを確認（"OnVolumeDetectedイベントが購読されていません"など）

3. **マウス位置の変換**
   - **マウスの左クリックを押したまま動かす**必要があります（クリックだけでは不十分）
   - `Scatter Plot`が接続されていない場合は、画面全体が入力エリアとして使用されます
   - 画面外をクリックしている場合でも、画面座標として処理されます

4. **パーティクルが表示されない場合**
   - `InkEffect`が`PaintCanvas`に接続されているか確認
   - カメラ（`Main Camera`）がシーンに存在するか確認
   - `PaintCanvas.OnPaintCompleted`イベントが発火しているか確認（デバッグログで確認可能）

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

- **VoiceDetector**: マイク入力の取得（必須、`VolumeAnalyzer`と`ImprovedPitchAnalyzer`が依存）
- **VoiceCalibrator**: キャリブレーション平均を提供
- **VolumeAnalyzer**: 音量検出イベントを提供（`VoiceDetector`に依存）
- **ImprovedPitchAnalyzer**: ピッチ検出イベントを提供（`VoiceDetector`に依存）
- **VoiceScatterPlot**: 座標変換ロジックの参考（UI座標系）
- **VoiceDisplay**: 範囲設定の同期

**依存関係**:
```
VoiceDetector（必須）
  ↓
VolumeAnalyzer ──→ PaintBattleGameManager
ImprovedPitchAnalyzer ──→ PaintBattleGameManager
```

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

