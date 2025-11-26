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

1. **座標とサイズで指定**: 中心位置（`centerPosition`）とサイズパラメータで領域を定義
2. **形状タイプで指定**: 円形、正方形、長方形などから選択（**変更しやすい設計**）
3. **キャンバス座標系で管理**: 画面座標をキャンバス座標（テクスチャ座標）に変換して管理

### 形状の変更しやすい設計

**重要**: 新しい形状を追加する際は、コードを大幅に変更せずに済むように、**Strategyパターン**と**ScriptableObject**を組み合わせた設計を採用します。

#### 設計方針

1. **`IAreaShape`インターフェース**: 形状判定のロジックを抽象化
2. **`AreaShapeData`（ScriptableObject）**: 形状の設定をInspectorで管理可能に
3. **形状クラスの分離**: 各形状を個別のクラスとして実装（`CircleShape`, `SquareShape`, `RectangleShape`など）

この設計により：
- ✅ 新しい形状を追加する際は、新しいクラスを1つ追加するだけ
- ✅ Inspectorで形状を選択・設定可能
- ✅ 既存のコードへの影響を最小限に
- ✅ カスタム形状（ポリゴン、スプラインなど）も簡単に追加可能

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

色が変わる領域の**大きさ**は、`ColorDefenseSettings.areaSize`と`AreaShapeData`で決定されます：

- `settings.areaSize`: 領域の基本サイズ（形状によって解釈が異なる）
- `settings.areaShapeData`: 形状の設定（ScriptableObject）
  - **CircleShape**: 半径 = `areaSize * 0.5f`
  - **SquareShape**: 一辺 = `areaSize`
  - **RectangleShape**: 幅 = `areaSize * widthRatio`, 高さ = `areaSize * heightRatio`
  - **PolygonShape**: カスタムポリゴン（頂点リストで定義）
  - **SplineShape**: スプライン曲線で囲まれた領域（将来的な拡張）

#### 形状システムの実装詳細

**インターフェース定義**:
```csharp
/// <summary>
/// 領域の形状を定義するインターフェース
/// 新しい形状を追加する際は、このインターフェースを実装する
/// </summary>
public interface IAreaShape
{
    /// <summary>
    /// 指定されたピクセルが領域内にあるかチェック
    /// </summary>
    bool IsPointInArea(Vector2 point, Vector2 center, float baseSize);
    
    /// <summary>
    /// 領域内の総ピクセル数を計算（近似値）
    /// </summary>
    int CalculateAreaInPixels(float baseSize);
    
    /// <summary>
    /// 領域のバウンディングボックスを取得（最適化用）
    /// </summary>
    Rect GetBoundingBox(Vector2 center, float baseSize);
}
```

**ScriptableObjectベースの設定**:
```csharp
/// <summary>
/// 形状の設定データ（Inspectorで設定可能）
/// </summary>
[CreateAssetMenu(fileName = "AreaShape", menuName = "Game/SinglePlayer/Area Shape")]
public abstract class AreaShapeData : ScriptableObject
{
    public abstract IAreaShape CreateShape();
    
    [Header("Visual Settings")]
    public Sprite shapeSprite; // 視覚表現用（オプション）
    public Color shapeColor = Color.red;
}
```

**具体例: 円形形状**:
```csharp
[CreateAssetMenu(fileName = "CircleShape", menuName = "Game/SinglePlayer/Area Shape/Circle")]
public class CircleShapeData : AreaShapeData
{
    public override IAreaShape CreateShape()
    {
        return new CircleShape();
    }
}

public class CircleShape : IAreaShape
{
    public bool IsPointInArea(Vector2 point, Vector2 center, float baseSize)
    {
        float radius = baseSize * 0.5f;
        return Vector2.Distance(point, center) <= radius;
    }
    
    public int CalculateAreaInPixels(float baseSize)
    {
        float radius = baseSize * 0.5f;
        return Mathf.RoundToInt(Mathf.PI * radius * radius);
    }
    
    public Rect GetBoundingBox(Vector2 center, float baseSize)
    {
        float radius = baseSize * 0.5f;
        return new Rect(center.x - radius, center.y - radius, baseSize, baseSize);
    }
}
```

**新しい形状の追加方法**:
1. `IAreaShape`を実装したクラスを作成（例: `StarShape`）
2. `AreaShapeData`を継承したScriptableObjectクラスを作成（例: `StarShapeData`）
3. Unityメニューからアセットを作成
4. `ColorDefenseSettings`の`areaShapeData`に設定

この設計により、**コードを変更せずに新しい形状を追加**できます。

#### 新しい形状の追加例（星形）

```csharp
// 1. IAreaShapeを実装
public class StarShape : IAreaShape
{
    private int points; // 星の頂点数
    private float innerRadiusRatio; // 内側の半径の比率
    
    public StarShape(int points = 5, float innerRadiusRatio = 0.5f)
    {
        this.points = points;
        this.innerRadiusRatio = innerRadiusRatio;
    }
    
    public bool IsPointInArea(Vector2 point, Vector2 center, float baseSize)
    {
        // 星形の判定ロジック
        float outerRadius = baseSize * 0.5f;
        float innerRadius = outerRadius * innerRadiusRatio;
        
        Vector2 dir = point - center;
        float angle = Mathf.Atan2(dir.y, dir.x);
        float distance = dir.magnitude;
        
        // 星形の境界を計算
        float normalizedAngle = (angle + Mathf.PI) / (2f * Mathf.PI / points);
        int segment = Mathf.FloorToInt(normalizedAngle) % points;
        float segmentAngle = (normalizedAngle % 1f) * (2f * Mathf.PI / points);
        
        float radius = Mathf.Lerp(outerRadius, innerRadius, 
            Mathf.Abs(segmentAngle - Mathf.PI / points) / (Mathf.PI / points));
        
        return distance <= radius;
    }
    
    public int CalculateAreaInPixels(float baseSize)
    {
        // 星形の面積の近似値
        float outerRadius = baseSize * 0.5f;
        float innerRadius = outerRadius * innerRadiusRatio;
        return Mathf.RoundToInt(Mathf.PI * outerRadius * outerRadius * 0.7f);
    }
    
    public Rect GetBoundingBox(Vector2 center, float baseSize)
    {
        float radius = baseSize * 0.5f;
        return new Rect(center.x - radius, center.y - radius, baseSize, baseSize);
    }
}

// 2. ScriptableObjectを作成
[CreateAssetMenu(fileName = "StarShape", menuName = "Game/SinglePlayer/Area Shape/Star")]
public class StarShapeData : AreaShapeData
{
    [Header("Star Settings")]
    [Range(3, 12)]
    public int points = 5;
    
    [Range(0.1f, 0.9f)]
    public float innerRadiusRatio = 0.5f;
    
    public override IAreaShape CreateShape()
    {
        return new StarShape(points, innerRadiusRatio);
    }
}
```

**追加のメリット**:
- ✅ 既存のコード（`ColorChangeArea`, `ColorDefenseMode`など）を変更する必要がない
- ✅ Inspectorで形状のパラメータを調整可能
- ✅ 複数の形状アセットを作成して、ゲーム中に切り替え可能
- ✅ テストが容易（各形状を個別にテスト可能）

**調整のポイント**:
- **大きさを小さくする**: より難しくなる（塗るのが大変）
- **出現間隔を短くする**: より難しくなる（頻繁に出現）
- **プレイヤーから離れた位置**: より難しくなる（移動距離が増える）
- **同時存在数を増やす**: より難しくなる（複数の領域を同時に防ぐ必要がある）

### 変更しやすい設計のメリット

**従来の設計（enumベース）の問題点**:
- ❌ 新しい形状を追加するたびに、enumとswitch文を修正する必要がある
- ❌ 既存のコード（`ColorChangeArea`など）を変更する必要がある
- ❌ 形状ごとのパラメータを設定しにくい

**新しい設計（Strategyパターン + ScriptableObject）のメリット**:
- ✅ **既存コードへの影響なし**: 新しい形状を追加しても、`ColorChangeArea`や`ColorDefenseMode`を変更する必要がない
- ✅ **Inspectorで設定可能**: 形状のパラメータをInspectorで視覚的に調整できる
- ✅ **複数の形状を切り替え可能**: 異なる形状アセットを作成して、ゲーム中に切り替え可能
- ✅ **テストが容易**: 各形状を個別にテストできる
- ✅ **拡張性**: ポリゴン、スプライン、カスタム形状なども簡単に追加可能
- ✅ **再利用性**: 同じ形状アセットを複数の設定で使用可能

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

## 🔥 将来の拡張: 「炎が広がる」設定への対応

### 拡張の要件

「炎が広がるのを声で場所を指定して水をかけることで防ぐ」という設定を後から追加するために、以下の拡張が必要です：

1. **領域の拡大機能**: 時間とともに領域が拡大する
2. **動作パターンの抽象化**: 「色が変わる」だけでなく、「広がる」「縮む」「移動する」などの動作パターン
3. **防御方法の抽象化**: 「色を塗る」だけでなく、「水をかける」などの別の防御方法
4. **視覚表現の抽象化**: 「色変化」だけでなく、「炎」「水」などの視覚表現

### 現在の設計の評価

#### ✅ 対応できている部分

1. **形状システム**: `IAreaShape`インターフェースにより、新しい形状を追加可能
2. **設定の分離**: `ColorDefenseSettings`がScriptableObjectで、Inspectorで調整可能
3. **イベント駆動**: `OnProgressChanged`などのイベントにより、視覚表現を分離可能

#### ⚠️ 拡張が必要な部分

1. **動作パターンの抽象化**: 現在は「色が変わる」という動作のみ。`IAreaBehavior`インターフェースを追加
2. **領域サイズの動的変更**: 現在は`areaSize`が固定。時間とともに拡大する機能が必要
3. **防御方法の抽象化**: 現在は「色を塗る」のみ。`IDefenseMethod`インターフェースを追加
4. **視覚表現の抽象化**: 現在はUIレイヤーで表示。`IVisualEffect`インターフェースを追加

### 拡張設計案

#### 1. 動作パターンの抽象化（`IAreaBehavior`）

```csharp
/// <summary>
/// 領域の動作パターンを定義するインターフェース
/// 「色が変わる」「広がる」「縮む」「移動する」などの動作を抽象化
/// </summary>
public interface IAreaBehavior
{
    /// <summary>
    /// 動作の進行度を更新
    /// </summary>
    void UpdateBehavior(float deltaTime, ColorChangeArea area, float defendedProgress);
    
    /// <summary>
    /// 現在の動作の進行度（0.0～1.0）
    /// </summary>
    float Progress { get; }
    
    /// <summary>
    /// 動作が完了したかどうか
    /// </summary>
    bool IsCompleted { get; }
    
    /// <summary>
    /// 現在の領域サイズを取得（拡大・縮小に対応）
    /// </summary>
    float GetCurrentSize(float baseSize);
}

/// <summary>
/// 色が変わる動作（現在の実装）
/// </summary>
public class ColorChangeBehavior : IAreaBehavior
{
    private float changeProgress = 0f;
    private ColorDefenseSettings settings;
    
    public float Progress => changeProgress;
    public bool IsCompleted => changeProgress >= 1f;
    
    public float GetCurrentSize(float baseSize)
    {
        return baseSize; // サイズは固定
    }
    
    public void UpdateBehavior(float deltaTime, ColorChangeArea area, float defendedProgress)
    {
        // 現在の実装と同じロジック
        float effectiveChangeRate = settings.colorChangeRate;
        if (defendedProgress > 0f)
        {
            effectiveChangeRate *= (1f - defendedProgress * settings.paintSlowdownEffect);
        }
        changeProgress += effectiveChangeRate * deltaTime;
        changeProgress = Mathf.Clamp01(changeProgress);
    }
}

/// <summary>
/// 炎が広がる動作（新しい実装）
/// </summary>
public class FireSpreadBehavior : IAreaBehavior
{
    private float spreadProgress = 0f;
    private float spreadRate = 0.5f; // 広がる速度
    private float maxSizeMultiplier = 3f; // 最大サイズ倍率
    
    public float Progress => spreadProgress;
    public bool IsCompleted => spreadProgress >= 1f;
    
    public float GetCurrentSize(float baseSize)
    {
        // 進行度に応じてサイズが拡大
        return baseSize * (1f + spreadProgress * (maxSizeMultiplier - 1f));
    }
    
    public void UpdateBehavior(float deltaTime, ColorChangeArea area, float defendedProgress)
    {
        // 防御されている場合は広がりを遅らせる
        float effectiveSpreadRate = spreadRate * (1f - defendedProgress * 0.5f);
        spreadProgress += effectiveSpreadRate * deltaTime;
        spreadProgress = Mathf.Clamp01(spreadProgress);
    }
}
```

#### 2. 防御方法の抽象化（`IDefenseMethod`）

```csharp
/// <summary>
/// 防御方法を定義するインターフェース
/// 「色を塗る」「水をかける」などの防御方法を抽象化
/// </summary>
public interface IDefenseMethod
{
    /// <summary>
    /// 指定位置で防御が行われているかチェック
    /// </summary>
    bool IsDefendedAt(Vector2 position, PaintCanvas canvas);
    
    /// <summary>
    /// 防御に必要な色（水の場合は青など）
    /// </summary>
    Color RequiredColor { get; }
    
    /// <summary>
    /// 防御の効果（0.0～1.0）
    /// </summary>
    float DefenseEffectiveness { get; }
}

/// <summary>
/// 色を塗る防御方法（現在の実装）
/// </summary>
public class PaintDefenseMethod : IDefenseMethod
{
    public Color RequiredColor => Color.white; // 任意の色でOK
    public float DefenseEffectiveness => 1f;
    
    public bool IsDefendedAt(Vector2 position, PaintCanvas canvas)
    {
        // プレイヤーが塗っているかチェック（現在の実装と同じ）
        int playerId = canvas.GetPlayerIdAtCanvas(
            Mathf.RoundToInt(position.x), 
            Mathf.RoundToInt(position.y)
        );
        return playerId > 0;
    }
}

/// <summary>
/// 水をかける防御方法（新しい実装）
/// </summary>
public class WaterDefenseMethod : IDefenseMethod
{
    public Color RequiredColor => Color.cyan; // 水の色（青系）
    public float DefenseEffectiveness => 1.5f; // 水は炎に対して効果的
    
    public bool IsDefendedAt(Vector2 position, PaintCanvas canvas)
    {
        // 青系の色で塗られているかチェック
        Color paintedColor = canvas.GetColorAtCanvas(
            Mathf.RoundToInt(position.x), 
            Mathf.RoundToInt(position.y)
        );
        
        // 青系の色かチェック（HSVで判定）
        float h, s, v;
        Color.RGBToHSV(paintedColor, out h, out s, out v);
        return h >= 0.45f && h <= 0.65f && s > 0.3f && v > 0.3f; // 青系
    }
}
```

#### 3. 視覚表現の抽象化（`IVisualEffect`）

```csharp
/// <summary>
/// 視覚表現を定義するインターフェース
/// 「色変化」「炎」「水」などの視覚表現を抽象化
/// </summary>
public interface IVisualEffect
{
    /// <summary>
    /// 視覚表現を更新
    /// </summary>
    void UpdateVisual(ColorChangeArea area, float progress, float defendedProgress);
    
    /// <summary>
    /// 視覚表現を初期化
    /// </summary>
    void Initialize(GameObject targetObject);
}

/// <summary>
/// 色変化の視覚表現（現在の実装）
/// </summary>
public class ColorChangeVisual : IVisualEffect
{
    private Image image;
    private Color targetColor;
    
    public void Initialize(GameObject targetObject)
    {
        image = targetObject.GetComponent<Image>();
        targetColor = Color.red;
    }
    
    public void UpdateVisual(ColorChangeArea area, float progress, float defendedProgress)
    {
        if (image != null)
        {
            Color currentColor = Color.Lerp(Color.yellow, targetColor, progress);
            if (defendedProgress > 0f)
            {
                currentColor = Color.Lerp(currentColor, Color.green, defendedProgress);
            }
            image.color = currentColor;
        }
    }
}

/// <summary>
/// 炎の視覚表現（新しい実装）
/// </summary>
public class FireVisual : IVisualEffect
{
    private ParticleSystem fireParticles;
    private SpriteRenderer fireSprite;
    
    public void Initialize(GameObject targetObject)
    {
        fireParticles = targetObject.GetComponent<ParticleSystem>();
        fireSprite = targetObject.GetComponent<SpriteRenderer>();
    }
    
    public void UpdateVisual(ColorChangeArea area, float progress, float defendedProgress)
    {
        // 炎のパーティクルエフェクトを更新
        if (fireParticles != null)
        {
            var main = fireParticles.main;
            main.startSize = area.AreaSize * (1f + progress * 0.5f);
            main.startLifetime = 1f - defendedProgress * 0.5f;
        }
        
        // 炎のスプライトを更新
        if (fireSprite != null)
        {
            fireSprite.color = new Color(1f, 0.5f - defendedProgress * 0.3f, 0f, 1f - defendedProgress);
        }
    }
}
```

### 拡張後の`ColorChangeArea`の構造

```csharp
public class ColorChangeArea : MonoBehaviour
{
    private IAreaBehavior behavior;      // 動作パターン（色変化、炎の広がりなど）
    private IDefenseMethod defenseMethod; // 防御方法（色を塗る、水をかけるなど）
    private IVisualEffect visualEffect;   // 視覚表現（色変化、炎など）
    private IAreaShape shape;             // 形状（円形、正方形など）
    
    // 既存のコードはそのまま、新しい動作パターンや防御方法を追加可能
}
```

### 拡張のメリット

- ✅ **既存コードへの影響なし**: 新しい動作パターンや防御方法を追加しても、既存のコードを変更する必要がない
- ✅ **組み合わせ可能**: 異なる動作パターンと防御方法を組み合わせ可能（例: 炎が広がる + 水をかける）
- ✅ **Inspectorで設定可能**: ScriptableObjectで動作パターンや防御方法を設定可能
- ✅ **テストが容易**: 各動作パターンや防御方法を個別にテスト可能

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
