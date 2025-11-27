# カラーディフェンスモード 使い方ガイド

> **注意**: このファイルは実装したカラーディフェンスモードの使い方をまとめたガイドです。実装詳細は`Implementation_ColorDefence.md`を参照してください。

---

## 📋 概要

カラーディフェンスモードは、ランダムな場所に出現する色変化領域を、声を出して色を塗ることで防ぐゲームモードです。

### ゲームの流れ

1. ゲーム開始後、一定間隔でランダムな位置に色変化領域が出現
2. 各領域は徐々に色が変わっていく（進行度: 0.0 → 1.0）
3. プレイヤーは声を出して、その領域に色を塗る
4. プレイヤーが塗った領域は色変化が阻止される
5. 領域が完全に変色した場合: ペナルティ（スコア減少）
6. 領域を完全に防げた場合: ボーナス（スコア増加）
7. 制限時間終了時に最終スコアを表示

---

## 🚀 セットアップ手順

### Step 1: ScriptableObjectアセットの作成

#### 1.1 形状アセットの作成

**円形の形状**:
1. Unityメニューから`Game/SinglePlayer/Area Shape/Circle`を選択
2. アセットを保存（例: `CircleShape_Default.asset`）
3. Inspectorで設定を確認（デフォルト設定で問題なし）

**正方形の形状**:
1. Unityメニューから`Game/SinglePlayer/Area Shape/Square`を選択
2. アセットを保存（例: `SquareShape_Default.asset`）

**長方形の形状**:
1. Unityメニューから`Game/SinglePlayer/Area Shape/Rectangle`を選択
2. アセットを保存（例: `RectangleShape_Default.asset`）
3. Inspectorで`widthRatio`と`heightRatio`を調整

#### 1.2 ColorDefenseSettingsアセットの作成

1. Unityメニューから`Game/SinglePlayer/Modes/Color Defense Settings`を選択
2. アセットを保存（例: `ColorDefenseSettings_Default.asset`）
3. Inspectorで各パラメータを設定:
   - **`areaShapeData`**: Step 1.1で作成した形状アセットを設定
   - **`colorChangeRate`**: 色変化の速度（0.1～1.0）
   - **`areaSize`**: 領域のサイズ（50～300ピクセル）
   - **`spawnInterval`**: 領域の出現間隔（1～10秒）
   - **`maxAreasOnScreen`**: 同時存在可能な領域数（1～20）
   - **`scorePerDefendedArea`**: 防げた時のスコア
   - **`penaltyPerChangedArea`**: 変色した時のペナルティ

#### 1.3 SinglePlayerGameModeSettingsアセットの作成

1. Unityメニューから`Game/SinglePlayer/Game Mode Settings`を選択
2. アセットを保存（例: `SinglePlayerGameModeSettings_Default.asset`）
3. Inspectorで設定:
   - **`selectedMode`**: `ColorDefense`を選択
   - **`gameDuration`**: ゲーム時間（30～300秒）
   - **`colorDefenseSettings`**: Step 1.2で作成した`ColorDefenseSettings`アセットを設定

---

### Step 2: シーンでの設定

#### 2.1 ゲームプレイシーンの準備

1. ゲームプレイシーン（例: `01_Gameplay.unity`）を開く
2. 以下のGameObjectを用意:
   - `PaintCanvas`（**クリエイティブモードと同じものを使用** - 推奨）
   - `ColorDefenseMode`（新規作成）
   - `ColorDefenseUI`（新規作成、オプション）

**重要**: `PaintCanvas`は**クリエイティブモードと同じものを使用することを推奨**します。

**理由**:
- ✅ **メモリ効率**: 1つのインスタンスで済むため、メモリ使用量が少ない
- ✅ **設定の統一**: 解像度などの設定を統一できる
- ✅ **自動クリア**: `ColorDefenseMode.StartGame()`で自動的に`ResetCanvas()`が呼ばれ、前のモードのデータがクリアされる
- ✅ **実装がシンプル**: 追加の設定が不要

**注意点**:
- カラーディフェンスモード開始時に、`ColorDefenseMode.StartGame()`が自動的に`paintCanvas.ResetCanvas()`を呼び出すため、クリエイティブモードの描画データは消去されます
- モードごとに異なる解像度が必要な場合は、別々の`PaintCanvas`インスタンスを作成することも可能ですが、通常は同じものを使用することを推奨します

#### 2.2 ColorDefenseModeの設定

1. 空のGameObjectを作成（名前: `ColorDefenseMode`）
2. `ColorDefenseMode`コンポーネントを追加
3. Inspectorで設定:
   - **`Settings`**: Step 1.2で作成した`ColorDefenseSettings`アセットを設定
   - **`Paint Canvas`**: シーン内の`PaintCanvas`をドラッグ&ドロップ
   - **`Area Renderer`**: （オプション）`ColorChangeAreaRenderer`コンポーネントを持つGameObjectを設定

#### 2.3 ColorDefenseUIの設定

1. Canvas内に空のGameObjectを作成（名前: `ColorDefenseUI`）
2. `ColorDefenseUI`コンポーネントを追加
3. InspectorでUI要素を設定:
   - **`Score Text`**: スコア表示用のTextMeshProUGUI
   - **`Combo Text`**: コンボ表示用のTextMeshProUGUI
   - **`Active Areas Text`**: アクティブ領域数表示用のTextMeshProUGUI
   - **`Game Over Panel`**: ゲームオーバー画面のGameObject
   - **`Final Score Text`**: 最終スコア表示用のTextMeshProUGUI
   - **`Defended Areas Text`**: 防げた領域数表示用のTextMeshProUGUI
   - **`Changed Areas Text`**: 変色した領域数表示用のTextMeshProUGUI
   - **`Retry Button`**: リトライボタン
   - **`Main Menu Button`**: メインメニューボタン

#### 2.4 ColorChangeAreaRendererの設定（オプション）

1. 空のGameObjectを作成（名前: `ColorChangeAreaRenderer`）
2. `ColorChangeAreaRenderer`コンポーネントを追加
3. Inspectorで設定:
   - **`Area Indicator Prefab`**: 領域表示用のプレハブ（Imageコンポーネントを持つGameObject）
   - **`Warning Color`**: 警告色（デフォルト: 黄色）
   - **`Danger Color`**: 危険色（デフォルト: 赤）
   - **`Safe Color`**: 安全色（デフォルト: 緑）
4. `ColorDefenseMode`の`Area Renderer`フィールドに設定

---

### Step 3: ゲームの起動

#### 3.1 SinglePlayerModeManagerの設定（推奨）

1. シーン内に`SinglePlayerModeManager`コンポーネントを持つGameObjectを作成
2. Inspectorで設定:
   - **`Settings`**: Step 1.3で作成した`SinglePlayerGameModeSettings`アセットを設定
   - **`Color Defense Mode`**: Step 2.2で作成した`ColorDefenseMode`を設定
3. `SinglePlayerModeManager`が自動的に`ColorDefenseMode`を初期化・開始します

#### 3.2 手動で起動する場合

```csharp
// ColorDefenseModeを取得
ColorDefenseMode colorDefenseMode = FindObjectOfType<ColorDefenseMode>();

// 設定を準備
SinglePlayerGameModeSettings settings = // ScriptableObjectアセットを参照

// 初期化・開始
colorDefenseMode.Initialize(settings);
colorDefenseMode.StartGame();
```

---

## ⚙️ 設定の調整方法

### 基本的な調整

#### 難易度の調整

**簡単にする**:
- `colorChangeRate`を小さくする（0.2～0.3）
- `spawnInterval`を大きくする（4～5秒）
- `maxAreasOnScreen`を小さくする（2～3個）
- `areaSize`を大きくする（150～200ピクセル）

**難しくする**:
- `colorChangeRate`を大きくする（0.7～0.8）
- `spawnInterval`を小さくする（1～2秒）
- `maxAreasOnScreen`を大きくする（6～8個）
- `areaSize`を小さくする（70～90ピクセル）

#### スコアバランスの調整

- **`scorePerDefendedArea`**: 防げた時の基本スコア（デフォルト: 50）
- **`penaltyPerChangedArea`**: 変色した時のペナルティ（デフォルト: -20）
- **`comboBonusPerDefense`**: コンボボーナス（デフォルト: 5）

### 時間ベースの難易度調整（TimeBasedモード）

1. `ColorDefenseSettings`の`scalingMode`を`TimeBased`に設定
2. `difficultyPhases`リストにフェーズを追加

**例: 3フェーズ構成**

**Phase 1（0-60秒）**: 初心者向け
- `startTime`: 0
- `endTime`: 60
- `spawnInterval`: 4秒
- `maxAreasOnScreen`: 3
- `colorChangeRate`: 0.3
- `colorChangeSpeed`: 1.0

**Phase 2（60-120秒）**: 中級者向け
- `startTime`: 60
- `endTime`: 120
- `spawnInterval`: 2.5秒
- `maxAreasOnScreen`: 5
- `colorChangeRate`: 0.5
- `colorChangeSpeed`: 1.2

**Phase 3（120秒以降）**: 上級者向け
- `startTime`: 120
- `endTime`: 0（最後まで）
- `spawnInterval`: 1.5秒
- `maxAreasOnScreen`: 7
- `colorChangeRate`: 0.7
- `colorChangeSpeed`: 1.5

### カーブベースの難易度調整（CurveBasedモード）

1. `ColorDefenseSettings`の`scalingMode`を`CurveBased`に設定
2. `difficultyCurve`を調整（時間経過による難易度の変化）
3. `maxDifficultyMultiplier`を設定（最大難易度倍率）

---

## 🎮 使用方法

### ゲームの開始

1. `SinglePlayerModeManager`が自動的にゲームを開始する場合:
   - シーンを再生するだけで開始されます

2. 手動で開始する場合:
   ```csharp
   colorDefenseMode.StartGame();
   ```

### ゲームの一時停止・再開

```csharp
// 一時停止
colorDefenseMode.Pause();

// 再開
colorDefenseMode.Resume();
```

### ゲームの終了

```csharp
// 手動で終了
colorDefenseMode.EndGame();

// ゲームオーバー判定
if (colorDefenseMode.IsGameOver())
{
    colorDefenseMode.EndGame();
}
```

### スコア・進捗の取得

```csharp
// 現在のスコア
int score = colorDefenseMode.GetScore();

// ゲームの進捗（0.0～1.0）
float progress = colorDefenseMode.GetProgress();

// アクティブな領域数
int activeAreas = colorDefenseMode.GetActiveAreasCount();
```

---

## 📡 イベントの使用

### イベントの購読

```csharp
void Start()
{
    // スコア更新イベント
    ColorDefenseMode.OnScoreUpdated += OnScoreUpdated;
    
    // コンボ更新イベント
    ColorDefenseMode.OnComboUpdated += OnComboUpdated;
    
    // 領域出現イベント
    ColorDefenseMode.OnAreaSpawned += OnAreaSpawned;
    
    // 領域防衛イベント
    ColorDefenseMode.OnAreaDefended += OnAreaDefended;
    
    // 領域変色イベント
    ColorDefenseMode.OnAreaChanged += OnAreaChanged;
}

void OnDestroy()
{
    // イベント購読解除
    ColorDefenseMode.OnScoreUpdated -= OnScoreUpdated;
    ColorDefenseMode.OnComboUpdated -= OnComboUpdated;
    ColorDefenseMode.OnAreaSpawned -= OnAreaSpawned;
    ColorDefenseMode.OnAreaDefended -= OnAreaDefended;
    ColorDefenseMode.OnAreaChanged -= OnAreaChanged;
}

void OnScoreUpdated(int score)
{
    Debug.Log($"スコア更新: {score}");
}

void OnComboUpdated(int combo)
{
    Debug.Log($"コンボ更新: {combo}");
}

void OnAreaSpawned(ColorChangeArea area)
{
    Debug.Log($"領域出現: {area.CenterPosition}");
}

void OnAreaDefended(ColorChangeArea area)
{
    Debug.Log($"領域防衛成功: {area.CenterPosition}");
}

void OnAreaChanged(ColorChangeArea area)
{
    Debug.Log($"領域変色: {area.CenterPosition}");
}
```

---

## 🔧 トラブルシューティング

### よくある問題

#### 1. 領域が出現しない

**原因**:
- `ColorDefenseSettings`が設定されていない
- `PaintCanvas`が設定されていない
- `spawnInterval`が長すぎる

**解決方法**:
- Inspectorで`ColorDefenseMode`の`Settings`と`Paint Canvas`を確認
- `spawnInterval`を短くする（例: 1秒）

#### 2. プレイヤーが塗っても防御されない

**原因**:
- `PaintCanvas`が正しく設定されていない
- `defenseThreshold`が高すぎる

**解決方法**:
- `PaintCanvas`の参照を確認
- `defenseThreshold`を下げる（例: 0.7）

#### 3. スコアが更新されない

**原因**:
- UIがイベントを購読していない
- `ColorDefenseUI`が設定されていない

**解決方法**:
- `ColorDefenseUI`の`Start()`メソッドでイベント購読を確認
- イベントが正しく発火しているかデバッグログで確認

#### 4. 形状が正しく表示されない

**原因**:
- `areaShapeData`が設定されていない
- 形状アセットが正しく作成されていない

**解決方法**:
- `ColorDefenseSettings`の`areaShapeData`を確認
- 形状アセットを再作成

---

## 📝 実装ファイル一覧

### 基盤システム
- `Assets/Main/Script/SinglePlayer/SinglePlayerGameModeType.cs`
- `Assets/Main/Script/SinglePlayer/ISinglePlayerGameMode.cs`
- `Assets/Main/Script/SinglePlayer/Data/Settings/SinglePlayerGameModeSettings.cs`

### カラーディフェンスモード
- `Assets/Main/Script/SinglePlayer/Data/Settings/ColorDefenseSettings.cs`（形状システム含む）
- `Assets/Main/Script/SinglePlayer/ColorDefense/ColorChangeArea.cs`
- `Assets/Main/Script/SinglePlayer/Modes/ColorDefenseMode.cs`
- `Assets/Main/Script/SinglePlayer/UI/ColorDefenseUI.cs`
- `Assets/Main/Script/SinglePlayer/ColorDefense/ColorChangeAreaRenderer.cs`（オプション）

---

## 🔗 関連ドキュメント

- **設計・アイデア**: `ColorDefenceIdea.md`
- **実装詳細**: `Implementation_ColorDefence.md`
- **実装チェックリスト**: `Implementation_Checklist.md`

---

## 💡 ヒント

### パフォーマンス最適化

- **領域の数**: `maxAreasOnScreen`を適切に設定（5～10個が推奨）
- **判定の最適化**: バウンディングボックスを使用してピクセル判定を最適化（既に実装済み）

### バランス調整のコツ

1. **まず基本設定で動作確認**: デフォルト設定でゲームが動作することを確認
2. **段階的に調整**: 一度に複数のパラメータを変更せず、1つずつ調整
3. **テストプレイ**: 実際にプレイして難易度を確認
4. **フェーズ設定**: TimeBasedモードで段階的に難易度を上げる

### 拡張のヒント

- **新しい形状の追加**: `IAreaShape`インターフェースを実装したクラスを作成
- **視覚表現のカスタマイズ**: `ColorChangeAreaRenderer`を拡張
- **将来の拡張**: 「炎が広がる」設定などは、`IAreaBehavior`インターフェースを実装（実装詳細を参照）

---

## 📞 サポート

問題が発生した場合は、以下の順序で確認してください：

1. Inspectorでの設定を確認
2. デバッグログを確認
3. `Implementation_ColorDefence.md`の実装詳細を確認
4. `ColorDefenceIdea.md`の設計を確認

