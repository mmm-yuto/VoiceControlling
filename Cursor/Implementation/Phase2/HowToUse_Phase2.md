# Phase 2: クリエイティブモード 使い方ガイド

## 概要
Phase 2 では、Phase 1 で構築した音声入力ベースの塗り機能を拡張し、声で自由に絵を描くクリエイティブモードを実現しました。このガイドでは、Unity 上でのセットアップ手順と主要コンポーネントの使い方を説明します。

---

## 実装済みコンポーネント

| 種別 | ファイル | 役割 |
| --- | --- | --- |
| ScriptableObject | `Assets/Main/Script/Data/Settings/CreativeModeSettings.cs` | クリエイティブモード全体の調整値（塗り・消しゴム・履歴など） |
| ScriptableObject | `Assets/Main/Script/Data/Settings/ColorSelectionSettings.cs` | 色選択方式・プリセット色の定義 |
| ScriptableObject | `Assets/Main/Script/Data/Settings/CreativeSaveSettings.cs` | PNG 保存時のパスやファイル名形式 |
| コンポーネント | `Assets/Main/Script/Creative/CreativeModeManager.cs` | クリエイティブモードのゲームループ／Undo 管理 |
| コンポーネント | `Assets/Main/Script/GameLogic/PaintCanvas.cs` | 色・強度・プレイヤー ID を記録し、消去/履歴/テクスチャ取得に対応 |
| コンポーネント | `Assets/Main/Script/Creative/ColorSelectionSystem.cs` | 色選択イベントをまとめて `CreativeModeManager` に伝達 |
| コンポーネント | `Assets/Main/Script/Creative/CreativeModeUI.cs` | ツール切替・Undo・色選択・保存 UI の制御 |
| コンポーネント | `Assets/Main/Script/Creative/CreativeModeSaveSystem.cs` | テクスチャの保存・共有処理ラッパー |
| 定義 | `Assets/Main/Script/Creative/CreativeToolMode.cs` | ツール種別の列挙体（Paint / Eraser） |
| 定義 | `Assets/Main/Script/GameLogic/CanvasState.cs` | Undo 用キャンバススナップショット |

> **前提**  
> Phase 1 の音声入力基盤（`VoiceDetector` / `VolumeAnalyzer` / `ImprovedPitchAnalyzer` / `VoiceToScreenMapper` / `PaintCanvas`）がシーンに存在し、正常に動作している必要があります。

---

## 1. ScriptableObject の準備

### 1-1. CreativeModeSettings
1. `Assets/ScriptableObjects`（なければ作成）に `Creative` フォルダを作成  
2. `Create > Game > Creative Mode Settings` でアセットを作成（例：`CreativeModeSettings.asset`）  
3. Inspector 設定例  
   - **Paint Settings**: Phase 1 で作成した `PaintSettings` を参照  
   - **Paint Intensity**: 声量に掛ける塗り強度倍率（例: 1.0）  
   - **Eraser Radius / Eraser Intensity**: 消しゴムの半径と強度  
   - **History Save Mode**: `OnOperation`（操作単位）／`TimeBased`（時間単位）  
   - **Max History Size / Auto Save History Interval**: 履歴数上限と自動保存間隔  
   - **Silence Volume Threshold / Silence Duration For Operation End**: 無音判定に使う閾値と時間  
   - **Improved Pitch Analyzer**: シーン上の `ImprovedPitchAnalyzer` を割り当て（未設定でも自動検索）  
   - **Available Colors / Initial Color**: 選択可能な色と初期色  
   - **Default Player Id**: クリエイティブモード利用時のプレイヤー ID（通常 1）  

### 1-2. ColorSelectionSettings（必要に応じて複数パターン作成可）
1. `Create > Game > Creative > Color Selection Settings`  
2. Inspector 設定例  
   - **Method**: `ButtonSelection` / `ColorPicker` / `VoiceSelection` / `PresetPalette`  
   - **Preset Colors**: プリセットボタンで表示する色リスト  
   - **Color Picker Visible By Default**: カラーピッカーパネルの初期表示状態  
   - **Voice Pitch Thresholds**: ピッチによる色切替境界（`VoiceSelection` 使用時）  

### 1-3. CreativeSaveSettings
1. `Create > Game > Creative > Save Settings`  
2. Inspector 設定例  
   - **Save Directory**: `CreativeExports` など任意のフォルダ名  
   - **File Name Format**: `Creative_{0:yyyyMMdd_HHmmss}.png` など  
   - **Include Timestamp**: タイムスタンプ付与の ON/OFF  
   - **Default File Name**: タイムスタンプを使わない場合の固定名  
   - **Image Scale**: 保存時の拡大率（1=原寸）  

---

## 2. シーンセットアップ

### 2-0. 前提確認
- Phase 1 の音声コンポーネント・`PaintCanvas` がシーンに存在  
- UI Canvas（ボタンやラベルを配置するため）が用意されている  

### 2-1. CreativeModeManager
1. 空の GameObject（例：`CreativeModeManager`）を作成し `CreativeModeManager` コンポーネントを追加  
2. Inspector で以下を割り当て  
   - **Settings**: `CreativeModeSettings`  
   - **Initial Tool Mode**: 起動時のツール（`Paint` または `Eraser`）  
   - **Paint Canvas**: シーン上の `PaintCanvas`  
   - **Voice To Screen Mapper**: `VoiceToScreenMapper`  
   - **Volume Analyzer / Improved Pitch Analyzer**: それぞれのコンポーネント  
   - 参照を設定しない場合は自動検索も行われますが、Inspector で明示的に接続するのが確実です  

### 2-2. ColorSelectionSystem
1. 空の GameObject に `ColorSelectionSystem` を追加  
2. Inspector  
   - **Settings**: `ColorSelectionSettings`  
   - **Creative Mode Manager**: 上記で配置した `CreativeModeManager`  
3. これにより UI（または音声ピッチ）からの色変更が `CreativeModeManager` に反映されます  

### 2-3. CreativeModeUI
1. UI Canvas 内に UI 管理用 GameObject（例：`CreativeModeUI`）を作成し `CreativeModeUI` を追加  
2. Inspector で以下の参照を接続  
   - **Creative Mode Manager** / **Color Selection System**  
   - **Paint Tool Button / Eraser Tool Button**  
   - **Clear Button / Undo Button**  
   - **Next Color Button / Previous Color Button**（不要なら未設定でも可）  
   - **Save Button / Share Button**（保存・共有機能を使う場合）  
   - **Color Picker Toggle Button / Color Picker Panel**  
   - **Preset Color Container / Preset Color Button Prefab**（色ボタンを自動生成）  
   - **Current Color Preview** (`Image`)  
   - **Tool State Label / Undo State Label / Save Status Label** (`TextMeshProUGUI`)  
3. プレイ時に `ColorSelectionSystem` が持つプリセット色からボタンが生成され、選択状態に応じてハイライト（スケール変更）されます  

### 2-4. CreativeModeSaveSystem
1. 空の GameObject に `CreativeModeSaveSystem` を追加  
2. Inspector で  
   - **Save Settings**: `CreativeSaveSettings`  
   - **Paint Canvas**: `PaintCanvas`  
3. `CreativeModeUI` の保存・共有ボタンに対応するメソッドが紐づいているため、これで保存／共有機能が有効になります（共有はプラットフォーム別にダミー実装あり）  

---

## 3. 実行フロー
1. **キャリブレーション（任意）**: Phase 1 と同様に `VoiceCalibrator` で平均値を取得すると描画が安定します  
2. **塗り**: 声量とピッチが `VoiceToScreenMapper` で画面座標に変換され、`CreativeModeManager` が `PaintCanvas` に描画  
3. **ツール切替**: UI ボタンで `Paint` ↔ `Eraser` を切り替え。`CreativeModeSettings` の値に合わせて強度が変わります  
4. **色変更**: プリセットボタン／Next/Prev ボタン／カラーピッカー／音声ピッチ（`VoiceSelection`）でカラーを変更  
5. **Undo / Clear**:  
   - `Undo` は履歴スタックから直前の状態を復元  
   - `Clear` はキャンバスを初期化し新たな履歴を積む  
6. **保存・共有**:  
   - Save ボタンで PNG をディスクに出力  
   - Share ボタンはプラットフォームごとにスタブ実装。デスクトップではファイルパスをクリップボードにコピー  

---

## 4. 調整ポイント
- **筆圧（塗り強度）**: `CreativeModeSettings.PaintIntensity` や `PaintSettings.PaintIntensityMultiplier` を調整  
- **Undo の頻度**: `History Save Mode` / `Max History Size` / `Auto Save History Interval` を調整  
- **消しゴム範囲**: `CreativeModeSettings.EraserRadius` を変更  
- **プリセット色の編集**: `ColorSelectionSettings.PresetColors` を編集  
- **保存先変更**: `CreativeSaveSettings.SaveDirectory` と `FileNameFormat` を変更  

---

## 5. トラブルシューティング

| 症状 | 原因 / 対処 |
| --- | --- |
| 描画されない | Phase 1 のコンポーネント参照漏れ／音量が閾値未満／`PaintCanvas` の設定漏れ |
| Undo が効かない | 履歴モードが `TimeBased` で間隔が長すぎる／`CreativeModeManager` が `SetActive(false)` 状態 |
| 色が変わらない | `ColorSelectionSystem` が未接続／プリセット色が空／複数の ColorSelectionSystem が競合 |
| 保存でエラー | `CreativeSaveSettings` 未割り当て／保存先ディレクトリの作成権限不足 |
| 共有がうまくいかない | モバイル向け共有処理はダミー実装。必要に応じてネイティブ連携を追加してください |

---

## 6. 次のステップ
- 描画テクスチャを RawImage 等で画面表示するビジュアル化  
- ブラシバリエーション（太さ、スプレー等）の追加  
- 保存した PNG のギャラリー UI、SNS 連携など共有機能の強化  

---

これで Phase 2 クリエイティブモードの Unity 上での利用手順は完了です。参照設定を確実に行い、`CreativeModeSettings` を中心に調整しながら用途に合わせたパラメータを設定してください。***

