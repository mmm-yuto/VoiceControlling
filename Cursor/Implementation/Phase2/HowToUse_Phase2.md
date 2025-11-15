# Phase 2: クリエイティブモード 使い方ガイド

## 概要

Phase 2では、声で自由に絵を描けるクリエイティブモードを実装しました。このガイドでは、実装したコンポーネントの使い方とセットアップ手順を説明します。

## 実装されたコンポーネント

### 1. VoiceInputHandler（音声入力処理の共通化）
- **ファイル**: `Assets/Main/Script/GameLogic/VoiceInputHandler.cs`
- **説明**: 音声入力処理を共通化し、`PaintBattleGameManager`と`CreativeModeManager`で使用
- **用途**: 音声検出から座標変換までの処理を統合管理

### 2. CreativeModeSettings（ScriptableObject）
- **ファイル**: `Assets/Main/Script/Data/Settings/CreativeModeSettings.cs`
- **説明**: クリエイティブモードの設定を管理
- **用途**: Inspectorで調整可能な設定アセット

### 3. ColorSelectionSettings（ScriptableObject）
- **ファイル**: `Assets/Main/Script/Data/Settings/ColorSelectionSettings.cs`
- **説明**: 色選択システムの設定を管理
- **用途**: プリセット色の定義など

### 4. CreativeSaveSettings（ScriptableObject、オプション）
- **ファイル**: `Assets/Main/Script/Data/Settings/CreativeSaveSettings.cs`
- **説明**: 保存機能の設定を管理
- **用途**: 保存先ディレクトリ、ファイル名フォーマットなど

### 5. CreativeModeManager（クリエイティブモードマネージャー）
- **ファイル**: `Assets/Main/Script/Creative/CreativeModeManager.cs`
- **説明**: クリエイティブモードの統合管理（ツール切り替え、履歴管理、音声入力処理）
- **用途**: クリエイティブモードのメインロジック

### 6. ColorSelectionSystem（色選択システム）
- **ファイル**: `Assets/Main/Script/Creative/ColorSelectionSystem.cs`
- **説明**: 色の選択・変更を管理
- **用途**: プリセット色の選択、色変更イベントの発火

### 7. PaintRenderer（描画システム）
- **ファイル**: `Assets/Main/Script/Graphics/PaintRenderer.cs`
- **説明**: 塗りキャンバスをTexture2Dに変換してUI Imageに表示
- **用途**: 画面上に色が塗られている様子を可視化

### 8. CreativeModeUI（UI管理）
- **ファイル**: `Assets/Main/Script/UI/CreativeModeUI.cs`
- **説明**: クリエイティブモードのUI管理（ボタン、ラベル、イベント処理）
- **用途**: UI要素の制御とイベント処理

### 9. CreativeModeSaveSystem（保存・共有システム、オプション）
- **ファイル**: `Assets/Main/Script/Creative/CreativeModeSaveSystem.cs`
- **説明**: 描いた絵を画像として保存・共有
- **用途**: 画像の保存・共有機能

### 10. 基本enumとデータクラス
- **CreativeToolMode.cs**: ツールモード（Paint, Eraser）
- **BrushType.cs**: ブラシタイプ（Pencil, Paint）
- **CanvasState.cs**: 履歴管理用の状態クラス

---

## セットアップ手順

### Step 1: ScriptableObjectアセットの作成

#### 1.1: CreativeModeSettingsの作成

1. Unityエディタで、`Assets`フォルダを右クリック → `Create` → `Folder`
2. フォルダ名を `ScriptableObjects` に変更（既に存在する場合はスキップ）
3. `ScriptableObjects`フォルダを右クリック → `Create` → `Folder`
4. フォルダ名を `Settings` に変更（既に存在する場合はスキップ）
5. `Assets/ScriptableObjects/Settings/` フォルダを開く
6. 右クリック → `Create` → `Game` → `Creative Mode Settings`
7. アセット名を `CreativeModeSettings` に変更
8. Inspectorで設定を調整：
   - **Paint Intensity**: 塗り強度の倍率（デフォルト: 1.0）
   - **Initial Color**: 初期色（デフォルト: White）
   - **Default Player Id**: デフォルトプレイヤーID（デフォルト: 1）
   - **Pencil Radius**: 鉛筆の半径（デフォルト: 5）
   - **Paint Brush Radius**: ペンキブラシの半径（デフォルト: 50）
   - **Eraser Radius**: 消しツールの半径（デフォルト: 30）
   - **Max History Size**: 履歴の最大サイズ（Undo可能な回数、デフォルト: 10）
   - **Silence Volume Threshold**: 無音判定の音量閾値（デフォルト: 0.01）
   - **Silence Duration For Operation End**: 操作終了とみなす無音の継続時間（デフォルト: 0.3秒）
   - **History Save Mode**: 履歴保存モード（OnOperation / TimeBased）

#### 1.2: ColorSelectionSettingsの作成

1. `Assets/ScriptableObjects/Settings/` フォルダを開く
2. 右クリック → `Create` → `Game` → `Color Selection Settings`
3. アセット名を `ColorSelectionSettings` に変更
4. Inspectorで設定を調整：
   - **Method**: 色選択方法（PresetPalette / ColorPicker / VoiceSelection）
   - **Preset Colors**: プリセット色のリスト（デフォルト: Red, Blue, Green, Yellow, Cyan, Magenta, White, Black）
   - **Color Picker Visible By Default**: カラーピッカーをデフォルトで表示するか（デフォルト: false）

#### 1.3: CreativeSaveSettingsの作成（オプション）

1. `Assets/ScriptableObjects/Settings/` フォルダを開く
2. 右クリック → `Create` → `Game` → `Creative/Save Settings`
3. アセット名を `CreativeSaveSettings` に変更
4. Inspectorで設定を調整：
   - **Save Directory**: 保存先ディレクトリ名（デフォルト: "CreativeExports"）
   - **File Name Format**: ファイル名のフォーマット（デフォルト: "Creative_{0:yyyyMMdd_HHmmss}.png"）
   - **Include Timestamp**: タイムスタンプを含めるか（デフォルト: true）
   - **Default File Name**: デフォルトファイル名（タイムスタンプなしの場合、デフォルト: "CreativeDrawing.png"）
   - **Image Scale**: 保存時の画像スケール（デフォルト: 1.0）

**注意**: アセットの配置場所は機能に影響しませんが、プロジェクトの整理のため、専用フォルダ（`Assets/ScriptableObjects/Settings/`）に作成することを推奨します。

### Step 2: シーンにコンポーネントを追加

**重要**: Phase 2のコンポーネントを使用するには、Phase 1のコンポーネント（`PaintCanvas`、`VoiceToScreenMapper`など）と既存の音声検出システム（`VoiceDetector`、`VolumeAnalyzer`、`ImprovedPitchAnalyzer`）がシーンに存在する必要があります。

#### 2.0: Phase 1のコンポーネントの確認（必須）

Phase 2はPhase 1のコンポーネントに依存しています。以下のコンポーネントがシーンに存在することを確認してください：

1. **PaintCanvas**（必須）
   - Phase 1で実装された塗りキャンバス
   - `PaintSettings`アセットが設定されていること

2. **VoiceToScreenMapper**（必須）
   - Phase 1で実装された座標変換コンポーネント

3. **VoiceDetector**（必須）
   - マイク入力の取得

4. **VolumeAnalyzer**（必須）
   - 音量の検出

5. **ImprovedPitchAnalyzer**（必須）
   - ピッチの検出

**これらのコンポーネントが存在しない場合**: Phase 1のセットアップ手順を参照して、まずPhase 1のコンポーネントを追加してください。

#### 2.1: VoiceInputHandlerの追加（新規・必須）

1. シーンに空のGameObjectを作成（例: `VoiceInputHandler`）
2. `VoiceInputHandler`コンポーネントを追加
3. Inspectorで以下を設定：
   - **Volume Analyzer**: Hierarchyで`VolumeAnalyzer`コンポーネントがアタッチされているGameObjectをドラッグ&ドロップ
   - **Improved Pitch Analyzer**: Hierarchyで`ImprovedPitchAnalyzer`コンポーネントがアタッチされているGameObjectをドラッグ&ドロップ
   - **Voice Debug Simulator**: デバッグモードを使用する場合、`VoiceDebugSimulator`のGameObjectをドラッグ&ドロップ（オプション）
   - **Voice To Screen Mapper**: Step 2.0で確認した`VoiceToScreenMapper`のGameObjectをドラッグ&ドロップ
   - **Silence Volume Threshold**: 無音判定の音量閾値（デフォルト: 0.01）

**注意**: `VoiceInputHandler`は、`PaintBattleGameManager`と`CreativeModeManager`の両方で使用されます。1つのシーンに1つ作成してください。

#### 2.2: PaintBattleGameManagerの更新（既存・必須）

Phase 1で作成した`PaintBattleGameManager`を更新します：

1. 既存の`PaintBattleGameManager`のGameObjectを選択
2. Inspectorで`PaintBattleGameManager`コンポーネントを確認
3. 以下のフィールドを設定：
   - **Voice Input Handler**: Step 2.1で作成した`VoiceInputHandler`のGameObjectをドラッグ&ドロップ
   - **Paint Canvas**: Phase 1で作成した`PaintCanvas`のGameObjectをドラッグ&ドロップ
   - **Player Id**: プレイヤーID（デフォルト: 1）
   - **Paint Speed Multiplier**: 塗り速度の倍率（デフォルト: 1.0）

**注意**: Phase 1で設定していた`Volume Analyzer`、`Improved Pitch Analyzer`、`Voice To Screen Mapper`、`Voice Debug Simulator`のフィールドは削除され、代わりに`Voice Input Handler`を使用します。

#### 2.3: ColorSelectionSystemの追加

1. シーンに空のGameObjectを作成（例: `ColorSelectionSystem`）
2. `ColorSelectionSystem`コンポーネントを追加
3. Inspectorで以下を設定：
   - **Settings**: Step 1.2で作成した`ColorSelectionSettings`アセットをProjectウィンドウからInspectorのフィールドにドラッグ&ドロップ

#### 2.4: CreativeModeManagerの追加

1. シーンに空のGameObjectを作成（例: `CreativeModeManager`）
2. `CreativeModeManager`コンポーネントを追加
3. Inspectorで以下を設定：
   - **Voice Input Handler**: Step 2.1で作成した`VoiceInputHandler`のGameObjectをドラッグ&ドロップ
   - **Paint Canvas**: Phase 1で作成した`PaintCanvas`のGameObjectをドラッグ&ドロップ
   - **Color Selection System**: Step 2.3で作成した`ColorSelectionSystem`のGameObjectをドラッグ&ドロップ
   - **Settings**: Step 1.1で作成した`CreativeModeSettings`アセットをProjectウィンドウからInspectorのフィールドにドラッグ&ドロップ

#### 2.5: PaintRendererの追加（描画システム）

1. UI Canvasを作成（既に存在する場合はスキップ）
   - Hierarchyで右クリック → `UI` → `Canvas`
2. Canvasの子として空のGameObjectを作成（例: `PaintRenderer`）
3. `PaintRenderer`コンポーネントを追加
4. 同じGameObjectに`Image`コンポーネントを追加（`PaintRenderer`が自動検出します）
5. Inspectorで以下を設定：
   - **Paint Canvas**: Phase 1で作成した`PaintCanvas`のGameObjectをドラッグ&ドロップ
   - **Display Image**: 同じGameObjectの`Image`コンポーネントが自動的に設定されます（手動で設定することも可能）
   - **Update Frequency**: テクスチャ更新頻度（デフォルト: 1）

**注意**: `PaintRenderer`は、`PaintCanvas`の内部データをTexture2Dに変換してUI Imageに表示します。Canvasのサイズや位置を調整して、描画領域を設定してください。

#### 2.6: CreativeModeUIの追加

1. Canvasの子として空のGameObjectを作成（例: `CreativeModeUI`）
2. `CreativeModeUI`コンポーネントを追加
3. Inspectorで以下を設定：

   **Manager References**:
   - **Creative Mode Manager**: Step 2.4で作成した`CreativeModeManager`のGameObjectをドラッグ&ドロップ
   - **Color Selection System**: Step 2.3で作成した`ColorSelectionSystem`のGameObjectをドラッグ&ドロップ
   - **Save System**: Step 2.7で作成した`CreativeModeSaveSystem`のGameObjectをドラッグ&ドロップ（オプション）

   **Tool Buttons**（UI Buttonを作成して接続）:
   - **Paint Tool Button**: 塗りツールボタン（`Button`コンポーネント付きのUI要素）
   - **Eraser Tool Button**: 消しツールボタン（`Button`コンポーネント付きのUI要素）

   **Brush Type Buttons**（UI Buttonを作成して接続）:
   - **Pencil Brush Button**: 鉛筆ブラシボタン（`Button`コンポーネント付きのUI要素）
   - **Paint Brush Button**: ペンキブラシボタン（`Button`コンポーネント付きのUI要素、将来的な拡張）

   **Action Buttons**（UI Buttonを作成して接続）:
   - **Clear Button**: クリアボタン（`Button`コンポーネント付きのUI要素）
   - **Undo Button**: Undoボタン（`Button`コンポーネント付きのUI要素）

   **Color Buttons**（UI Buttonを作成して接続、オプション）:
   - **Next Color Button**: 次の色ボタン（`Button`コンポーネント付きのUI要素）
   - **Previous Color Button**: 前の色ボタン（`Button`コンポーネント付きのUI要素）

   **Preset Color Buttons**:
   - **Preset Color Container**: プリセット色ボタンを並べるための`RectTransform`（例: Horizontal Layout Groupを持つ空のGameObject）
   - **Preset Color Button Prefab**: 単色ボタンのプレハブ（`Button` + `Image`）。プレイ時に複製され、`Preset Color Container`の子として生成されます（オプション、プレハブがない場合は動的に作成されます）

   **Display Labels**（TextMeshProUGUIを作成して接続）:
   - **Current Color Preview**: 現在の色プレビュー（`Image`コンポーネント付きのUI要素）
   - **Tool State Label**: ツール状態ラベル（`TextMeshProUGUI`コンポーネント付きのUI要素）
   - **Brush Type Label**: ブラシタイプラベル（`TextMeshProUGUI`コンポーネント付きのUI要素）
   - **Undo State Label**: Undo状態ラベル（`TextMeshProUGUI`コンポーネント付きのUI要素）

   **Save/Share Buttons**（UI Buttonを作成して接続、オプション）:
   - **Save Button**: 保存ボタン（`Button`コンポーネント付きのUI要素）
   - **Share Button**: 共有ボタン（`Button`コンポーネント付きのUI要素）
   - **Save Status Label**: 保存状態ラベル（`TextMeshProUGUI`コンポーネント付きのUI要素）

**UI要素の作成方法**:
- Hierarchyで右クリック → `UI` → `Button` または `Text - TextMeshPro` または `Image`
- 作成したUI要素をInspectorの対応するフィールドにドラッグ&ドロップ

#### 2.7: CreativeModeSaveSystemの追加（オプション）

1. シーンに空のGameObjectを作成（例: `CreativeModeSaveSystem`）
2. `CreativeModeSaveSystem`コンポーネントを追加
3. Inspectorで以下を設定：
   - **Paint Canvas**: Phase 1で作成した`PaintCanvas`のGameObjectをドラッグ&ドロップ
   - **Save Settings**: Step 1.3で作成した`CreativeSaveSettings`アセットをProjectウィンドウからInspectorのフィールドにドラッグ&ドロップ（オプション）

---

## 使用方法

### 基本的な使い方

1. **キャリブレーションの実行**（推奨）
   - 既存の`VoiceCalibrator`でキャリブレーションを実行
   - キャリブレーション結果が`VoiceToScreenMapper`に自動的に反映されます

2. **ツールの選択**
   - **塗りツール**: `Paint Tool Button`をクリック
   - **消しツール**: `Eraser Tool Button`をクリック

3. **ブラシタイプの選択**
   - **鉛筆**: `Pencil Brush Button`をクリック（細い線、連続的な描画）
   - **ペンキ**: `Paint Brush Button`をクリック（太い線、広範囲の塗りつぶし、将来的な拡張）

4. **色の選択**
   - **プリセット色**: `Preset Color Container`内の色ボタンをクリック
   - **次の色**: `Next Color Button`をクリック
   - **前の色**: `Previous Color Button`をクリック

5. **声を出して描く**
   - マイクに向かって声を出すと、音量とピッチに応じて画面の位置が決定されます
   - 塗りツール時は選択した色で描画されます
   - 消しツール時は描画が消去されます
   - 無音時は描画処理が停止します

6. **操作の取り消し**
   - `Undo Button`をクリックして、前の状態に戻すことができます
   - 履歴の最大サイズは`CreativeModeSettings`の`Max History Size`で設定できます

7. **キャンバスのクリア**
   - `Clear Button`をクリックして、キャンバスをクリアできます

8. **画像の保存**（オプション）
   - `Save Button`をクリックして、描いた絵を画像として保存できます
   - 保存先は`CreativeSaveSettings`の`Save Directory`で設定できます

9. **画像の共有**（オプション）
   - `Share Button`をクリックして、保存した画像を共有できます
   - プラットフォーム固有の実装が必要です（現在は基本的な実装のみ）

### 動作確認

#### 確認項目

1. **音声検出の確認**
   - `VoiceInputHandler`が正常に動作していることを確認
   - `VolumeAnalyzer`と`ImprovedPitchAnalyzer`が正常に動作していることを確認

2. **座標変換の確認**
   - `VoiceToScreenMapper`がキャリブレーション結果を正しく取得していることを確認
   - 音声値が画面座標に正しく変換されていることを確認

3. **塗り処理の確認**
   - `PaintCanvas`が`PaintSettings`を正しく読み込んでいることを確認
   - `CreativeModeManager`が音声入力を受信していることを確認
   - 塗り処理が実行されていることを確認（`PaintRenderer`で可視化）

4. **描画システムの確認**
   - `PaintRenderer`が`PaintCanvas`の内部データを正しく表示していることを確認
   - UI Imageに描画が表示されていることを確認

5. **UIの確認**
   - ツール切り替えボタンが正常に動作していることを確認
   - 色選択ボタンが正常に動作していることを確認
   - Undoボタンが正常に動作していることを確認
   - ラベルが正しく更新されていることを確認

6. **履歴管理の確認**
   - Undo機能が正常に動作していることを確認
   - 履歴の最大サイズが正しく制限されていることを確認

7. **保存機能の確認**（オプション）
   - 画像が正しく保存されていることを確認
   - 保存先ディレクトリが正しく作成されていることを確認

### デバッグ方法

#### ログ出力の確認

- `PaintCanvas`の`Show Debug Gizmos`をONにすると、塗り位置がログに出力されます
- `CreativeModeManager`は、参照が見つからない場合にエラーログを出力します
- `ColorSelectionSystem`は、色変更時にログを出力します（オプション）

#### デバッグモードの活用

- `VoiceDebugSimulator`を使用すると、声を出さずにテストできます
- `VoiceInputHandler`の`Voice Debug Simulator`フィールドに`VoiceDebugSimulator`を接続すると、デバッグモードが有効になります
- マウスの左クリックを押したまま動かすことで、マウス位置から音量・ピッチへの逆変換が正しく動作していることを確認できます

---

## トラブルシューティング

### 問題: NullReferenceException（VoiceInputHandlerが見つからない）

**エラーメッセージ例**:
```
NullReferenceException: Object reference not set to an instance of an object
CreativeModeManager.Update () (at Assets/Main/Script/Creative/CreativeModeManager.cs:XX)
```

**原因と対処法**:
1. **VoiceInputHandlerがシーンに存在しない**
   - Hierarchyで`VoiceInputHandler`コンポーネントがアタッチされているGameObjectが存在するか確認
   - 存在しない場合は、Step 2.1を参照して追加してください

2. **参照が設定されていない**
   - `CreativeModeManager`のInspectorで、`Voice Input Handler`フィールドに`VoiceInputHandler`のGameObjectが接続されているか確認
   - `PaintBattleGameManager`のInspectorで、`Voice Input Handler`フィールドに`VoiceInputHandler`のGameObjectが接続されているか確認

### 問題: 塗り処理が実行されない

**原因と対処法**:
1. **参照が設定されていない**
   - `CreativeModeManager`のInspectorで、すべての参照が正しく設定されているか確認
   - `Voice Input Handler`、`Paint Canvas`、`Color Selection System`がすべて接続されているか確認

2. **音量が閾値以下**
   - `CreativeModeSettings`の`Silence Volume Threshold`を確認
   - 音量が閾値以上になっているか確認

3. **キャリブレーションが実行されていない**
   - `VoiceCalibrator`でキャリブレーションを実行
   - `VoiceToScreenMapper`がキャリブレーション結果を取得しているか確認

### 問題: 描画が表示されない

**原因と対処法**:
1. **PaintRendererが設定されていない**
   - `PaintRenderer`コンポーネントがシーンに存在するか確認
   - `Paint Canvas`参照が正しく設定されているか確認
   - `Display Image`参照が正しく設定されているか確認

2. **UI Imageの設定**
   - `PaintRenderer`がアタッチされているGameObjectに`Image`コンポーネントが存在するか確認
   - Canvasのサイズや位置が正しく設定されているか確認

3. **テクスチャの更新**
   - `PaintRenderer`の`Update Frequency`を確認
   - `PaintCanvas`の内部データが正しく更新されているか確認

### 問題: 色が変更されない

**原因と対処法**:
1. **ColorSelectionSystemが設定されていない**
   - `ColorSelectionSystem`コンポーネントがシーンに存在するか確認
   - `Settings`参照が正しく設定されているか確認

2. **CreativeModeManagerとの連携**
   - `CreativeModeManager`の`Color Selection System`参照が正しく設定されているか確認
   - `ColorSelectionSystem.OnColorChanged`イベントが`CreativeModeManager`に届いているか確認

### 問題: Undoが動作しない

**原因と対処法**:
1. **履歴が保存されていない**
   - `CreativeModeSettings`の`History Save Mode`を確認
   - `Max History Size`が1以上に設定されているか確認

2. **操作が実行されていない**
   - 実際に描画操作が実行されているか確認
   - 履歴スタックに状態が保存されているか確認

### 問題: 保存機能が動作しない

**原因と対処法**:
1. **CreativeModeSaveSystemが設定されていない**
   - `CreativeModeSaveSystem`コンポーネントがシーンに存在するか確認
   - `Paint Canvas`参照が正しく設定されているか確認

2. **保存先ディレクトリの権限**
   - 保存先ディレクトリに書き込み権限があるか確認
   - `Application.persistentDataPath`が正しく取得できているか確認

3. **CreativeModeUIとの連携**
   - `CreativeModeUI`の`Save System`参照が正しく設定されているか確認
   - 保存ボタンのイベントが正しく接続されているか確認

---

## 次のステップ

Phase 2が完成したら、以下のステップに進むことができます：

1. **Phase 3: シングルプレイモード**
   - モンスターの実装
   - スコアシステム
   - タイマー
   - 勝利条件

2. **攻撃タイプシステムの追加**
   - インパクトショットとストリームペイントの実装
   - 塗り方のバリエーション追加

3. **上塗り機能の追加**
   - 既存の色を上書きする機能
   - 陣地奪い合いの実装

4. **可視化の改善**
   - より高度なエフェクトの追加
   - パーティクルエフェクトの改善

---

## 参考情報

### 既存コンポーネントとの連携

- **VoiceDetector**: マイク入力の取得（必須、`VolumeAnalyzer`と`ImprovedPitchAnalyzer`が依存）
- **VoiceCalibrator**: キャリブレーション結果を提供
- **VolumeAnalyzer**: 音量検出イベントを提供（`VoiceDetector`に依存）
- **ImprovedPitchAnalyzer**: ピッチ検出イベントを提供（`VoiceDetector`に依存）
- **VoiceToScreenMapper**: 座標変換を提供
- **PaintCanvas**: 塗り状態を管理（Phase 1で実装）

**依存関係**:
```
VoiceDetector（必須）
  ↓
VolumeAnalyzer ──→ VoiceInputHandler ──→ CreativeModeManager
ImprovedPitchAnalyzer ──→ VoiceInputHandler ──→ CreativeModeManager
VoiceToScreenMapper ──→ VoiceInputHandler ──→ CreativeModeManager
PaintCanvas ──→ CreativeModeManager
ColorSelectionSystem ──→ CreativeModeManager
```

### イベントフロー

```
1. VolumeAnalyzer.OnVolumeDetected
   ↓
2. ImprovedPitchAnalyzer.OnPitchDetected
   ↓
3. VoiceInputHandler.Update()
   ↓
4. VoiceToScreenMapper.MapVoiceToScreen()
   ↓
5. CreativeModeManager.ProcessVoiceInput()
   ↓
6. PaintCanvas.PaintAtWithRadius() / EraseAt()
   ↓
7. PaintCanvas.OnPaintCompleted
   ↓
8. PaintRenderer.UpdateTexture()
   ↓
9. UI Imageに描画が表示される
```

### 設定の推奨値

- **CreativeModeSettings**:
  - Pencil Radius: 5（細い線）
  - Paint Brush Radius: 50（太い線）
  - Eraser Radius: 30（消しツール）
  - Max History Size: 10（Undo可能な回数）
  - Silence Volume Threshold: 0.01（デフォルト）
  - History Save Mode: OnOperation（操作開始/終了時に保存）

- **ColorSelectionSettings**:
  - Method: PresetPalette（プリセット色パレット）
  - Preset Colors: Red, Blue, Green, Yellow, Cyan, Magenta, White, Black

- **CreativeSaveSettings**:
  - Save Directory: "CreativeExports"
  - Image Scale: 1.0（元のサイズ）

---

## まとめ

Phase 2では、声で自由に絵を描けるクリエイティブモードを実装しました。このシステムは、Phase 1の塗りシステムを基盤として、ツール切り替え、色選択、履歴管理、描画システム、保存機能を追加しています。

問題が発生した場合は、このガイドのトラブルシューティングセクションを参照してください。

