# カラーディフェンスモード アイデア・設計

## 📋 概要

カラーディフェンスモードは、ランダムな場所に出現する色変化領域を、声を出して色を塗ることで防ぐゲームモードです。

### ゲームの目的
- 画面にランダムに出現する領域の色が変わるのを防ぐ
- プレイヤーが声を出して色を塗ることで、領域の色変化を阻止
- 防げた領域数に応じてスコアを獲得
- 制限時間内にできるだけ多くの領域を防ぐ

### ゲームプレイの流れ
1. ゲーム開始後、一定間隔でランダムな位置に色変化領域が出現
2. 各領域は徐々に色が変わっていく（進行度: 0.0 → 1.0）
3. プレイヤーは声を出して、その領域に色を塗る
4. プレイヤーが塗った領域は色変化が阻止される
5. 領域が完全に変色した場合: ペナルティ（スコア減少）
6. 領域を完全に防げた場合: ボーナス（スコア増加）
7. 制限時間終了時に最終スコアを表示

---

## 🎮 モード管理の概念

### モード管理の階層構造

カラーディフェンスモードは、以下の階層構造で管理されます：

1. **最上位レベル**: `GameplayManager`がゲームモード全体を管理
   - `GameMode.Creative`: クリエイティブモード（声で絵を描く）
   - `GameMode.SinglePlayer`: シングルプレイモード（ゲームモード）
   - `GameMode.OfflineMulti`: オフラインマルチプレイ
   - `GameMode.OnlineMulti`: オンラインマルチプレイ

2. **シングルプレイモード内**: `SinglePlayerModeManager`がシングルプレイゲームモードを管理
   - `SinglePlayerGameModeType.MonsterHunt`: モンスター撃破モード
   - `SinglePlayerGameModeType.ColorDefense`: **カラーディフェンスモード**
   - `SinglePlayerGameModeType.Tracing`: トレーシングモード
   - `SinglePlayerGameModeType.AIBattle`: AI対戦モード

### モード切り替えの流れ

```
メインメニュー
  ↓
GameplayManager（ゲームモード選択）
  ├─→ GameMode.Creative → CreativeModeManager
  └─→ GameMode.SinglePlayer → SinglePlayerModeManager
         ↓
         SinglePlayerGameModeType選択
         ├─→ ColorDefense → ColorDefenseMode（このモード）
         ├─→ MonsterHunt → MonsterHuntMode
         ├─→ Tracing → TracingMode
         └─→ AIBattle → AIBattleMode
```

---

## 🎯 色が変わる範囲の指定方法

### 基本的な考え方

カラーディフェンスモードでは、**色が変わる範囲**を以下の方法で指定します：

1. **座標とサイズで指定**: 中心位置（`centerPosition`）と半径（`areaRadius`）で領域を定義
2. **形状タイプで指定**: 円形、正方形、長方形から選択
3. **キャンバス座標系で管理**: 画面座標をキャンバス座標（テクスチャ座標）に変換して管理

### 位置と大きさの決定方法

#### 位置の決定方法

色が変わる領域の**位置**は、以下のパラメータで決定されます：

- `settings.areaSize`: 領域のサイズ（これより内側に出現）
- `settings.spawnAwayFromPlayer`: プレイヤーから離れた位置に出現させる度合い（0.0～1.0）
  - `0.0`: 完全ランダム
  - `1.0`: プレイヤーから離れた位置を優先
- `lastPlayerPaintPosition`: プレイヤーが最後に塗った位置

**位置決定のアルゴリズム**:
1. **基本位置**: 画面内のランダムな位置（領域が画面外に出ないようにマージンを確保）
2. **プレイヤーからの距離**: `spawnAwayFromPlayer > 0`の場合、プレイヤーから離れた位置を優先
3. **重複回避**: 既存の領域と重ならないように調整（オプション）

#### 大きさの決定方法

色が変わる領域の**大きさ**は、`ColorDefenseSettings.areaSize`で決定されます：

- `settings.areaSize`: 領域の直径（円形の場合）または一辺の長さ（正方形の場合）
- `settings.areaShape`: 形状タイプ（Circle/Square/Rectangle）
  - **Circle**: 半径 = `areaSize * 0.5f`
  - **Square**: 一辺 = `areaSize`
  - **Rectangle**: 幅と高さを別々に設定可能（拡張時）

**調整のポイント**:
- **大きさを小さくする**: より難しくなる（塗るのが大変）
- **出現間隔を短くする**: より難しくなる（頻繁に出現）
- **プレイヤーから離れた位置**: より難しくなる（移動距離が増える）
- **同時存在数を増やす**: より難しくなる（複数の領域を同時に防ぐ必要がある）

### 視覚的な表現方法

#### 推奨実装方法

**推奨**: **UIレイヤーで視覚表現 + キャンバス座標で判定**

1. **判定**: キャンバス座標系で領域内のピクセルをチェック
2. **視覚表現**: UIレイヤー（Image、SpriteRendererなど）で領域を表示
3. **色変化**: UI要素の色や透明度を進行度に応じて変更

この方法により、プレイヤーの塗りと競合せず、パフォーマンスも良好です。

#### 上塗り判定の動作について

**重要なポイント**: 推奨方法（UIレイヤーで視覚表現）を使用した場合でも、**上塗りの判定は正しく動作します**。

**動作の流れ**:

1. **色変化領域の表示**: UIレイヤー（Imageなど）で領域を表示
   - 実際のPaintCanvasのデータ（`paintData`, `colorData`）は変更されない
   - UIレイヤーは視覚的な表示のみ

2. **プレイヤーが塗る**: プレイヤーが声を出して塗る
   - `PaintCanvas.PaintAt()`が呼ばれる
   - `paintData[x, y] = playerId`が設定される
   - `colorData[x, y] = playerColor`が設定される
   - **UIレイヤーの表示はそのまま**（プレイヤーの塗りが上に表示される）

3. **上塗り判定**: `ColorChangeArea.CheckPlayerPaint()`が呼ばれる
   - `GetPaintedPixelsInArea()`で領域内のピクセルをチェック
   - `canvas.GetPlayerIdAtCanvas(x, y)`で`paintData[x, y] > 0`を確認
   - **プレイヤーが塗った領域は正しく検出される**

4. **視覚的な更新**: プレイヤーが塗った領域では、UIレイヤーの表示を更新
   - 透明度を下げる
   - 色を変更する（防げていることを示す）
   - または、プレイヤーの塗りが上に表示されるので、UIレイヤーを非表示にする

**結論**: 
- ✅ **上塗りの判定は正しく動作します**
- UIレイヤーで表示していても、実際の判定はPaintCanvasの`paintData`をチェックしているため
- プレイヤーが塗った領域は`paintData[x, y] > 0`になるため、正しく検出される
- UIレイヤーの表示は、プレイヤーが塗った領域では視覚的に更新する（透明度を下げる、色を変えるなど）

---

## 🎮 UI管理の概念

### UI管理の階層構造

カラーディフェンスモードのUIは、以下の階層構造で管理されます：

1. **最上位レベル**: `MainMenuManager`がメニュー画面のパネルを管理
   - メインメニューパネル
   - 設定パネル
   - カスタマイズパネル
   - キャリブレーションパネル

2. **ゲームプレイ中**: `GameHUD`がゲーム中のUIを管理
   - モード共通UI（タイマー、スコアなど）
   - モード固有UI（カラーディフェンス専用UI）

3. **モード固有UI**: `ColorDefenseUI`がカラーディフェンスモード専用のUIを管理
   - スコア表示
   - コンボ表示
   - 領域の状態表示
   - ゲームオーバー画面

### UI切り替えの流れ

```
メインメニュー
  ↓
MainMenuManager（パネル切り替え）
  ├─→ メインメニューパネル
  ├─→ 設定パネル
  ├─→ カスタマイズパネル
  └─→ キャリブレーションパネル
  ↓
ゲーム開始
  ↓
GameplayManager（モード切り替え）
  ├─→ GameMode.Creative → CreativeModeUI
  └─→ GameMode.SinglePlayer → SinglePlayerModeManager
         ↓
         SinglePlayerGameModeType.ColorDefense
         ↓
         ColorDefenseUI（カラーディフェンス専用UI）
```

### UI更新の仕組み

1. **イベント駆動型の更新**
   - `ColorDefenseMode`が`OnScoreUpdated`, `OnComboUpdated`などのイベントを発火
   - `ColorDefenseUI`がイベントを購読してUIを更新
   - これにより、モードとUIが疎結合になり、変更が容易

2. **毎フレーム更新が必要な要素**
   - タイマー: `GameHUD.UpdateTimer()`を`Update()`で呼び出し
   - 進捗バー: `GameHUD.UpdateProgress()`を`Update()`で呼び出し

3. **イベントベースの更新**
   - スコア: `OnScoreUpdated`イベントで更新
   - コンボ: `OnComboUpdated`イベントで更新
   - 領域の状態: `OnAreaSpawned`, `OnAreaDefended`, `OnAreaChanged`イベントで更新

---

## 🎮 UI要件

### 表示すべき情報

1. **スコア表示**
   - 現在のスコア
   - コンボ数

2. **タイマー**
   - 残り時間
   - 進捗バー

3. **領域の状態表示**
   - 現在の領域数
   - 各領域の進行度（オプション）

4. **ゲームオーバー画面**
   - 最終スコア
   - 防げた領域数
   - 変色した領域数
   - ランキング表示

---

## 📊 バランス調整のポイント

### 難易度調整

#### TimeBasedモード（推奨：Inspectorで調整しやすい）

1. **フェーズごとの難易度設定**
   - `difficultyPhases`リストに各時間帯の設定を追加
   - 例: 3フェーズ構成
     - **Phase 1（0-60秒）**: 初心者向け
       - `spawnInterval`: 4-5秒（ゆっくり出現）
       - `maxAreasOnScreen`: 2-3個（少なめ）
       - `colorChangeRate`: 0.2-0.3（遅い）
     - **Phase 2（60-120秒）**: 中級者向け
       - `spawnInterval`: 2-3秒（普通）
       - `maxAreasOnScreen`: 4-5個（普通）
       - `colorChangeRate`: 0.4-0.5（普通）
     - **Phase 3（120秒以降）**: 上級者向け
       - `spawnInterval`: 1-2秒（速い）
       - `maxAreasOnScreen`: 6-8個（多い）
       - `colorChangeRate`: 0.6-0.8（速い）

2. **調整のコツ**
   - 各フェーズの`startTime`と`endTime`を明確に設定
   - フェーズ間の難易度の差を段階的に上げる（急激な変化を避ける）
   - `spawnInterval`と`maxAreasOnScreen`のバランスを調整
   - `colorChangeRate`と`colorChangeSpeed`の組み合わせで微調整

#### CurveBasedモード（滑らかな変化）

1. **初期難易度**: `colorChangeRate`と`spawnInterval`で調整
2. **難易度カーブ**: `difficultyCurve`で時間経過による変化を調整
3. **領域の数**: `maxAreasOnScreen`で同時存在数を調整

### スコアバランス
1. **基本スコア**: `scorePerDefendedArea`で調整
2. **ペナルティ**: `penaltyPerChangedArea`で調整
3. **コンボボーナス**: `comboBonusPerDefense`で調整

### ゲーム時間
- デフォルト: 180秒（3分）
- 短時間モード: 60秒
- 長時間モード: 300秒

---

## 🔄 拡張案

### 将来的な拡張
1. **特殊領域**
   - 大きな領域（高得点）
   - 小さな領域（高速変化）
   - 移動する領域

2. **パワーアップ**
   - 一時的に色変化を停止
   - 全領域の色変化を遅らせる
   - 自動で塗るエリア

3. **マルチプレイヤー**
   - 協力モード
   - 競争モード

---

## 📝 実装チェックリスト

### Phase 1: 基本実装
- [ ] ColorDefenseSettingsの作成
- [ ] ColorChangeAreaの実装
- [ ] ColorDefenseModeの基本実装
- [ ] 領域の生成・削除
- [ ] 色変化の進行

### Phase 2: 判定システム
- [ ] プレイヤーの塗り判定
- [ ] 防げた判定
- [ ] 変色判定

### Phase 3: スコアシステム
- [ ] スコア計算
- [ ] コンボシステム
- [ ] ペナルティ処理

### Phase 4: UI実装
- [ ] スコア表示
- [ ] タイマー表示
- [ ] 領域の視覚表現
- [ ] ゲームオーバー画面

### Phase 5: 調整・最適化
- [ ] バランス調整
- [ ] パフォーマンス最適化
- [ ] エフェクト追加

---

## 🎯 実装の優先順位

1. **最優先**: 基本実装（Phase 1, 2）
2. **高優先度**: スコアシステム（Phase 3）
3. **中優先度**: UI実装（Phase 4）
4. **低優先度**: 調整・最適化（Phase 5）
