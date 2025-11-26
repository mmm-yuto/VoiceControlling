# カラーディフェンスモード 実装チェックリスト

> **注意**: このファイルは実装すべき内容をまとめたチェックリストです。詳細な実装コードは`Implementation_ColorDefence.md`を参照してください。

---

## 📋 実装の優先順位

### Phase 1: 基本システム（最優先）
基本的なゲームプレイが動作するために必要な実装

### Phase 2: UI・視覚表現（高優先度）
プレイヤーがゲームを理解できるようにするための実装

### Phase 3: 調整・最適化（中優先度）
ゲームバランスとパフォーマンスの調整

### Phase 4: 将来の拡張（低優先度）
「炎が広がる」などの拡張機能（後から実装可能）

---

## 🔧 Phase 1: 基本システム（最優先）

### 1.1 形状システム（変更しやすい設計）

**実装ファイル**: `Assets/Main/Script/SinglePlayer/Data/Settings/ColorDefenseSettings.cs`（形状システム部分）

**実装内容**:
- [ ] `IAreaShape`インターフェースの定義
- [ ] `AreaShapeData`抽象クラスの定義
- [ ] `CircleShapeData`と`CircleShape`の実装
- [ ] `SquareShapeData`と`SquareShape`の実装
- [ ] `RectangleShapeData`と`RectangleShape`の実装

**依存関係**: なし（最初に実装）

**テスト項目**:
- [ ] 各形状で`IsPointInArea()`が正しく動作する
- [ ] `CalculateAreaInPixels()`が正しい値を返す
- [ ] `GetBoundingBox()`が正しい矩形を返す
- [ ] Unityメニューから形状アセットを作成できる

---

### 1.2 ColorDefenseSettings（ScriptableObject設定）

**実装ファイル**: `Assets/Main/Script/SinglePlayer/Data/Settings/ColorDefenseSettings.cs`

**実装内容**:
- [ ] `ColorDefenseSettings`クラスの実装
  - [ ] 色変化プロパティ（`colorChangeSpeed`, `colorChangeRate`, `targetColor`, `changeProgressCurve`）
  - [ ] 領域プロパティ（`maxAreasOnScreen`, `areaSize`, `areaShapeData`, `spawnInterval`, `spawnAwayFromPlayer`）
  - [ ] 防御プロパティ（`defenseThreshold`, `fullDefenseThreshold`, `paintSlowdownEffect`）
  - [ ] スコアプロパティ（`scorePerDefendedArea`, `penaltyPerChangedArea`, `partialDefenseScoreMultiplier`, `comboBonusPerDefense`）
  - [ ] 難易度スケーリング（`scalingMode`, `difficultyPhases`, `difficultyCurve`, `maxDifficultyMultiplier`, `minSpawnInterval`）
- [ ] `DifficultyScalingMode`enumの定義
- [ ] `DifficultyPhase`クラスの実装

**依存関係**: 形状システム（1.1）

**テスト項目**:
- [ ] Unityメニューから`ColorDefenseSettings`アセットを作成できる
- [ ] Inspectorで全てのパラメータを設定できる
- [ ] `DifficultyPhase.IsInPhase()`が正しく動作する

---

### 1.3 ColorChangeArea（色変化領域コンポーネント）

**実装ファイル**: `Assets/Main/Script/SinglePlayer/ColorDefense/ColorChangeArea.cs`

**実装内容**:
- [ ] `ColorChangeArea`クラスの実装
  - [ ] `Initialize()`メソッド（設定、位置、サイズ、形状の初期化）
  - [ ] `Update()`メソッド（色変化の進行、プレイヤーの塗り判定）
  - [ ] `CheckPlayerPaint()`メソッド（プレイヤーが塗った領域をチェック）
  - [ ] `CalculateTotalPixels()`メソッド（領域内の総ピクセル数を計算）
  - [ ] `GetPaintedPixelsInArea()`メソッド（領域内でプレイヤーが塗ったピクセル数を取得）
  - [ ] `ScreenToCanvas()`メソッド（画面座標をキャンバス座標に変換）
  - [ ] `ScreenToCanvasSize()`メソッド（画面座標のサイズをキャンバス座標のサイズに変換）
  - [ ] `IsPixelInArea()`メソッド（ピクセルが領域内にあるかチェック）
  - [ ] `IsFullyChanged()`メソッド（完全に変色したかどうか）
  - [ ] `IsFullyDefended()`メソッド（完全に防げたかどうか）
  - [ ] `IsPartiallyDefended()`メソッド（部分的に防げているかどうか）
  - [ ] イベント（`OnFullyChanged`, `OnFullyDefended`, `OnProgressChanged`）
  - [ ] プロパティ（`CenterPosition`, `ChangeProgress`, `DefendedProgress`, `AreaSize`, `Shape`）

**依存関係**: ColorDefenseSettings（1.2）、形状システム（1.1）、PaintCanvas

**テスト項目**:
- [ ] 領域が正しく初期化される
- [ ] 色変化の進行度が正しく更新される
- [ ] プレイヤーが塗った領域が正しく検出される
- [ ] 完全に変色した時にイベントが発火する
- [ ] 完全に防げた時にイベントが発火する
- [ ] 進行度が更新された時にイベントが発火する

---

### 1.4 ColorDefenseMode（メインモードクラス）

**実装ファイル**: `Assets/Main/Script/SinglePlayer/Modes/ColorDefenseMode.cs`

**実装内容**:
- [ ] `ColorDefenseMode`クラスの実装（`ISinglePlayerGameMode`インターフェースを実装）
  - [ ] `GetModeType()`メソッド
  - [ ] `Initialize()`メソッド（設定の初期化、参照の自動検索）
  - [ ] `StartGame()`メソッド（ゲーム開始処理）
  - [ ] `Update()`メソッド（ゲームループ、領域の生成・更新）
  - [ ] `SpawnColorChangeArea()`メソッド（色変化領域を生成）
  - [ ] `GetSpawnPosition()`メソッド（領域の出現位置を計算）
  - [ ] `HandleAreaChanged()`メソッド（領域が完全に変色した時の処理）
  - [ ] `HandleAreaDefended()`メソッド（領域を完全に防げた時の処理）
  - [ ] `GetCurrentPhase()`メソッド（現在のフェーズを取得）
  - [ ] `GetDifficultyMultiplier()`メソッド（難易度倍率を取得）
  - [ ] `GetEffectiveSpawnInterval()`メソッド（有効な出現間隔を取得）
  - [ ] `GetEffectiveMaxAreas()`メソッド（有効な同時存在可能な領域数を取得）
  - [ ] `GetEffectiveColorChangeRate()`メソッド（有効な色変化速度を取得）
  - [ ] `EndGame()`メソッド（ゲーム終了処理）
  - [ ] `Pause()`メソッド（一時停止）
  - [ ] `Resume()`メソッド（再開）
  - [ ] `GetScore()`メソッド（スコア取得）
  - [ ] `GetProgress()`メソッド（進捗取得）
  - [ ] `IsGameOver()`メソッド（ゲームオーバー判定）
  - [ ] イベント（`OnScoreUpdated`, `OnComboUpdated`, `OnAreaSpawned`, `OnAreaDefended`, `OnAreaChanged`）

**依存関係**: ColorDefenseSettings（1.2）、ColorChangeArea（1.3）、PaintCanvas

**テスト項目**:
- [ ] ゲームが正しく開始される
- [ ] 領域が一定間隔で出現する
- [ ] 各領域が正しく更新される
- [ ] 完全に変色した領域が削除される
- [ ] 完全に防げた領域が削除される
- [ ] スコアが正しく計算される
- [ ] コンボが正しく計算される
- [ ] 難易度スケーリングが正しく動作する
- [ ] イベントが正しく発火する

---

## 🎨 Phase 2: UI・視覚表現（高優先度）

### 2.1 ColorDefenseUI（カラーディフェンス専用UI）

**実装ファイル**: `Assets/Main/Script/SinglePlayer/UI/ColorDefenseUI.cs`

**実装内容**:
- [ ] `ColorDefenseUI`クラスの実装
  - [ ] UI要素の参照（`scoreText`, `comboText`, `activeAreasText`, `areaProgressBars`, `gameOverPanel`, `finalScoreText`, `defendedAreasText`, `changedAreasText`, `retryButton`, `mainMenuButton`）
  - [ ] `Start()`メソッド（イベント購読、ボタン設定）
  - [ ] `OnDestroy()`メソッド（イベント購読解除）
  - [ ] `UpdateScore()`メソッド（スコア表示の更新）
  - [ ] `UpdateCombo()`メソッド（コンボ表示の更新）
  - [ ] `OnAreaSpawned()`メソッド（領域が出現した時の処理）
  - [ ] `OnAreaDefended()`メソッド（領域を防げた時の処理）
  - [ ] `OnAreaChanged()`メソッド（領域が変色した時の処理）
  - [ ] `UpdateActiveAreasCount()`メソッド（アクティブな領域数の更新）
  - [ ] `ShowGameOver()`メソッド（ゲームオーバー画面の表示）
  - [ ] `OnRetryClicked()`メソッド（リトライボタンの処理）
  - [ ] `OnMainMenuClicked()`メソッド（メインメニューボタンの処理）

**依存関係**: ColorDefenseMode（1.4）

**テスト項目**:
- [ ] スコアが正しく表示される
- [ ] コンボが正しく表示される
- [ ] アクティブな領域数が正しく表示される
- [ ] ゲームオーバー画面が正しく表示される
- [ ] リトライボタンが動作する
- [ ] メインメニューボタンが動作する

---

### 2.2 ColorChangeAreaRenderer（視覚表現）

**実装ファイル**: `Assets/Main/Script/SinglePlayer/ColorDefense/ColorChangeAreaRenderer.cs`

**実装内容**:
- [ ] `ColorChangeAreaRenderer`クラスの実装
  - [ ] 視覚設定（`areaIndicatorPrefab`, `warningColor`, `dangerColor`, `safeColor`）
  - [ ] `AddArea()`メソッド（領域の視覚表現を追加）
  - [ ] `UpdateAreaVisual()`メソッド（領域の視覚表現を更新）
  - [ ] `RemoveArea()`メソッド（領域の視覚表現を削除）

**依存関係**: ColorChangeArea（1.3）

**テスト項目**:
- [ ] 領域が出現した時に視覚表現が追加される
- [ ] 進行度に応じて色が変化する
- [ ] 防御されている領域の色が変化する
- [ ] 領域が削除された時に視覚表現が削除される

**注意**: このコンポーネントはオプションです。UIレイヤーで直接表示する場合は不要です。

---

## ⚙️ Phase 3: 調整・最適化（中優先度）

### 3.1 バランス調整

**実装内容**:
- [ ] 各パラメータのバランス調整
  - [ ] 色変化速度の調整
  - [ ] 出現間隔の調整
  - [ ] スコア計算の調整
  - [ ] コンボボーナスの調整
  - [ ] 難易度フェーズの調整

**依存関係**: Phase 1, Phase 2完了後

---

### 3.2 パフォーマンス最適化

**実装内容**:
- [ ] ピクセル判定の最適化（バウンディングボックスを使用）
- [ ] 領域の更新頻度の最適化
- [ ] メモリ使用量の最適化
- [ ] フレームレートの安定化

**依存関係**: Phase 1完了後

---

## 🔥 Phase 4: 将来の拡張（低優先度）

### 4.1 動作パターンの抽象化（IAreaBehavior）

**実装内容**:
- [ ] `IAreaBehavior`インターフェースの定義
- [ ] `ColorChangeBehaviorData`と`ColorChangeBehavior`の実装（現在の実装を抽象化）
- [ ] `FireSpreadBehaviorData`と`FireSpreadBehavior`の実装（炎が広がる動作）

**依存関係**: Phase 1完了後

**使用例**: 「炎が広がる」設定を追加する場合

---

### 4.2 防御方法の抽象化（IDefenseMethod）

**実装内容**:
- [ ] `IDefenseMethod`インターフェースの定義
- [ ] `PaintDefenseMethodData`と`PaintDefenseMethod`の実装（現在の実装を抽象化）
- [ ] `WaterDefenseMethodData`と`WaterDefenseMethod`の実装（水をかける防御方法）

**依存関係**: Phase 1完了後

**使用例**: 「水をかける」防御方法を追加する場合

---

### 4.3 視覚表現の抽象化（IVisualEffect）

**実装内容**:
- [ ] `IVisualEffect`インターフェースの定義
- [ ] `ColorChangeVisualData`と`ColorChangeVisual`の実装（現在の実装を抽象化）
- [ ] `FireVisualData`と`FireVisual`の実装（炎の視覚表現）

**依存関係**: Phase 2完了後

**使用例**: 「炎」の視覚表現を追加する場合

---

## 📁 実装ファイル構成

```
Assets/Main/Script/SinglePlayer/
├── Modes/
│   └── ColorDefenseMode.cs                    // Phase 1.4
├── ColorDefense/
│   ├── ColorChangeArea.cs                     // Phase 1.3
│   └── ColorChangeAreaRenderer.cs             // Phase 2.2（オプション）
├── UI/
│   └── ColorDefenseUI.cs                      // Phase 2.1
└── Data/
    └── Settings/
        └── ColorDefenseSettings.cs            // Phase 1.1, 1.2
            ├── IAreaShape（インターフェース）
            ├── AreaShapeData（抽象クラス）
            ├── CircleShapeData, CircleShape
            ├── SquareShapeData, SquareShape
            ├── RectangleShapeData, RectangleShape
            ├── ColorDefenseSettings
            ├── DifficultyScalingMode（enum）
            └── DifficultyPhase（クラス）
```

---

## 🔗 依存関係図

```
Phase 1.1: 形状システム
    ↓
Phase 1.2: ColorDefenseSettings
    ↓
Phase 1.3: ColorChangeArea
    ↓
Phase 1.4: ColorDefenseMode
    ↓
Phase 2.1: ColorDefenseUI
    ↓
Phase 2.2: ColorChangeAreaRenderer（オプション）
    ↓
Phase 3: 調整・最適化
    ↓
Phase 4: 将来の拡張（オプション）
```

---

## ✅ 実装完了チェックリスト

### Phase 1: 基本システム
- [ ] 形状システム（1.1）
- [ ] ColorDefenseSettings（1.2）
- [ ] ColorChangeArea（1.3）
- [ ] ColorDefenseMode（1.4）

### Phase 2: UI・視覚表現
- [ ] ColorDefenseUI（2.1）
- [ ] ColorChangeAreaRenderer（2.2、オプション）

### Phase 3: 調整・最適化
- [ ] バランス調整（3.1）
- [ ] パフォーマンス最適化（3.2）

### Phase 4: 将来の拡張（オプション）
- [ ] 動作パターンの抽象化（4.1）
- [ ] 防御方法の抽象化（4.2）
- [ ] 視覚表現の抽象化（4.3）

---

## 📝 実装時の注意点

1. **実装順序**: Phase 1から順番に実装する（依存関係があるため）
2. **テスト**: 各Phase完了後にテストを実施する
3. **設定**: UnityでScriptableObjectアセットを作成して設定する
4. **拡張性**: 将来の拡張を考慮した設計になっているため、既存コードを変更せずに新しい機能を追加可能

---

## 🔗 関連ファイル

- **設計・アイデア**: `ColorDefenceIdea.md`
- **実装詳細**: `Implementation_ColorDefence.md`
- **モード管理**: `ImplementationStep.md`のPhase 3セクション
- **PaintCanvas**: `Assets/Main/Script/GameLogic/PaintCanvas.cs`

