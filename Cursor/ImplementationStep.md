# VO!CE Paint Battle 実装手順書

## 🎯 設計原則：変更しやすい実装

### 基本方針
全ての実装において、**設定変更・機能追加・バランス調整を容易にする**ことを最優先とします。

### 1. ScriptableObjectによる設定管理
**目的**: コードを変更せずにInspectorで設定を調整可能にする

**実装方針**:
- ゲームパラメータ（塗り強度、攻撃判定閾値、マッチ時間など）は全てScriptableObjectで管理
- プレハブやシーンに直接値を書かない
- 設定ファイルを複数作成し、簡単に切り替え可能にする

**例**:
```
Assets/ScriptableObjects/
├── GameSettings.asset          // 基本ゲーム設定
├── PaintSettings.asset         // 塗りシステム設定
├── AttackSettings.asset        // 攻撃タイプ設定
└── BalancePresets/             // バランスプリセット
    ├── FastPaced.asset
    ├── Strategic.asset
    └── Casual.asset
```

### 2. イベント駆動アーキテクチャ
**目的**: コンポーネント間の結合を緩くし、機能追加を容易にする

**実装方針**:
- UnityEventやC#のAction/Eventを使用
- 直接参照ではなく、イベントで通信
- イベントリスナーはInspectorで接続可能にする

**例**:
```csharp
// 塗りイベント
public static event Action<Vector2, int, float> OnPaint;
// 攻撃タイプ変更イベント
public static event Action<AttackType> OnAttackTypeChanged;
// プレイヤースコア更新イベント
public static event Action<int, float> OnScoreUpdated;
```

### 3. インターフェース/抽象クラスの活用
**目的**: 実装を差し替え可能にし、拡張性を高める

**実装方針**:
- 攻撃タイプ判定、塗りシステム、AI行動などはインターフェース化
- 新しい攻撃タイプやAI行動を追加する際は、インターフェースを実装するだけ

**例**:
```csharp
// 攻撃タイプ判定のインターフェース
public interface IAttackTypeDetector
{
    AttackType DetectAttackType(float volume, float pitch, float deltaTime);
}

// 塗りシステムのインターフェース
public interface IPaintStrategy
{
    void Paint(Vector2 position, int playerId, float intensity);
}
```

### 4. データとロジックの分離
**目的**: バランス調整時にロジックを触らずに済むようにする

**実装方針**:
- 計算式の係数や閾値は全てScriptableObjectに
- ロジックはデータを参照するだけ
- 計算式自体も設定可能にする（例: AnimationCurve）

### 5. Inspectorで調整可能な設計
**目的**: プログラマーでなくても調整できるようにする

**実装方針**:
- 全てのパラメータに`[Header]`、`[Tooltip]`を付ける
- 範囲制限`[Range(min, max)]`を使用
- デフォルト値を適切に設定

### 6. モジュール化と依存性の最小化
**目的**: 一部の変更が他に影響しないようにする

**実装方針**:
- 各システムは独立したコンポーネントとして実装
- 依存関係は明示的に（Inspectorで接続、またはService Locatorパターン）
- シングルトンは最小限に

### 7. グラフィックシステムの柔軟性
**目的**: 見た目を簡単に変更・カスタマイズできるようにする

**実装方針**:
- **エフェクトの抽象化**: `IInkEffect`インターフェースでエフェクト実装を差し替え可能に
- **マテリアル管理**: `InkMaterialData`（ScriptableObject）でマテリアル、テクスチャ、色を管理
- **プレハブベース**: パーティクルエフェクトはプレハブ化し、Inspectorで差し替え可能に
- **動的変更**: 実行中にマテリアルや色を変更できるAPIを提供
- **レンダリングパイプライン対応**: Built-in/URP/HDRPに対応できる設計
- **テーマシステム**: UIテーマやインクテーマをScriptableObjectで管理

**グラフィック設定の構造**:
```
Assets/ScriptableObjects/Graphics/
├── InkEffectSettings.asset        // エフェクト設定
├── InkMaterialData.asset          // マテリアルデータ
├── PlayerColorTheme.asset         // プレイヤー色テーマ
├── UISettings.asset               // UI設定
└── Themes/                        // テーマプリセット
    ├── DefaultTheme.asset
    ├── NeonTheme.asset
    └── PaintTheme.asset
```

---

## 📁 プロジェクト構造とスクリプト分割

### スクリプト分割の基本方針

**分割の原則**:
1. **機能ごとに分割**: 各スクリプトは1つの責任を持つ
2. **ディレクトリで整理**: 機能ごとにディレクトリを分ける
3. **インターフェースで抽象化**: 差し替え可能な機能はインターフェース化
4. **設定はScriptableObject**: パラメータはScriptableObjectで管理

### 推奨ディレクトリ構造

```
Assets/
├── Scripts/
│   ├── VoiceDetection/          // 音声検出システム（既存）
│   │   ├── VoiceDetector.cs
│   │   ├── VolumeAnalyzer.cs
│   │   ├── ImprovedPitchAnalyzer.cs
│   │   └── VoiceCalibrator.cs
│   │
│   ├── GameLogic/               // ゲームコアロジック
│   │   ├── PaintCanvas.cs       // 塗りキャンバス
│   │   ├── PaintSystem.cs       // 塗りシステム
│   │   ├── PaintBattleGameManager.cs  // ゲームマネージャー
│   │   ├── VoiceToScreenMapper.cs      // 座標変換
│   │   ├── AttackTypeManager.cs       // 攻撃タイプ選択・管理
│   │   ├── AttackTypeDetector.cs      // 攻撃タイプ判定（自動判定用）
│   │   └── AttackTypeSelectors/       // 選択モード実装
│   │       ├── AutoAttackTypeSelector.cs
│   │       ├── ManualAttackTypeSelector.cs
│   │       ├── RandomTimedAttackTypeSelector.cs
│   │       └── RandomOnPaintAttackTypeSelector.cs
│   │   ├── PlayerManager.cs            // プレイヤー管理
│   │   ├── LocalPlayerManager.cs       // オフライン用
│   │   ├── VictoryCondition.cs         // 勝利条件
│   │   └── GameplayManager.cs          // ゲームプレイ管理
│   │
│   ├── Graphics/                 // グラフィックシステム
│   │   ├── InkEffect.cs          // インクエフェクト
│   │   ├── PaintRenderer.cs      // 塗り描画
│   │   └── EffectPool.cs         // エフェクトプール
│   │
│   ├── Network/                   // ネットワーク（オンラインモード用）
│   │   ├── NetworkManager.cs     // ネットワーク管理
│   │   ├── NetworkPaintSync.cs   // 塗りデータ同期
│   │   └── NetworkPlayerSync.cs  // プレイヤー同期
│   │
│   ├── SinglePlayer/              // シングルモード（モンスター撃破）
│   │   ├── Monster.cs            // モンスター
│   │   ├── MonsterSpawner.cs       // モンスター生成管理
│   │   ├── MonsterHitDetector.cs // 当たり判定
│   │   ├── ScoreManager.cs       // スコア管理
│   │   └── MovementPatterns/     // 移動パターン実装
│   │       ├── LinearMovement.cs
│   │       ├── CurveMovement.cs
│   │       └── RandomMovement.cs
│   │
│   ├── UI/                        // UIシステム
│   │   ├── VoiceDisplay.cs       // 音声表示（既存）
│   │   ├── VoiceScatterPlot.cs   // グラフ表示（既存）
│   │   ├── GameHUD.cs            // ゲームHUD
│   │   ├── AttackTypeSelectionUI.cs // 攻撃タイプ選択UI
│   │   ├── MainMenuManager.cs    // メインメニュー
│   │   ├── SettingsPanel.cs     // 設定パネル
│   │   ├── CustomizationPanel.cs // カスタマイズパネル
│   │   └── CalibrationPanel.cs  // キャリブレーションパネル
│   │
│   ├── Customization/             // カスタマイズシステム
│   │   ├── InkCustomizer.cs     // インクカスタマイズ
│   │   ├── SoundCustomizer.cs   // サウンドカスタマイズ
│   │   └── ThemeManager.cs      // テーマ管理
│   │
│   ├── SceneManagement/          // シーン管理
│   │   ├── SimpleSceneManager.cs
│   │   └── GameDataManager.cs
│   │
│   ├── Interfaces/               // インターフェース定義
│   │   ├── IPaintCanvas.cs
│   │   ├── IPaintStrategy.cs
│   │   ├── IAttackTypeDetector.cs
│   │   ├── IInkEffect.cs
│   │   └── IPlayerManager.cs
│   │
│   └── Data/                     // データクラス（ScriptableObject）
│       ├── Settings/
│       │   ├── GameSettings.cs
│       │   ├── PaintSettings.cs
│       │   ├── AttackSettings.cs
│       │   └── AttackTypeSelectionSettings.cs
│       ├── Graphics/
│       │   ├── InkEffectSettings.cs
│       │   ├── InkMaterialData.cs
│       │   └── InkTheme.cs
│       ├── Scene/
│       │   ├── SceneReference.cs
│       │   └── GameData.cs
│       └── SinglePlayer/
│           ├── MonsterSettings.cs
│           ├── SpawnSettings.cs
│           └── ScoreSettings.cs
│
├── ScriptableObjects/            // ScriptableObjectアセット
│   ├── Settings/
│   ├── Graphics/
│   └── Themes/
│
├── Prefabs/                      // プレハブ
│   ├── Effects/
│   │   ├── ImpactShotEffect.prefab
│   │   └── StreamPaintEffect.prefab
│   └── UI/
│
└── Scenes/                       // シーン
    ├── 00_MainMenu.unity
    ├── 01_Gameplay.unity
    └── 99_Test.unity
```

### スクリプト分割の詳細

#### 1. VoiceDetection/（音声検出）
**役割**: マイク入力から音量・ピッチを検出

**分割方針**:
- 各機能を独立したコンポーネントに
- 既存の実装をそのまま使用

#### 2. GameLogic/（ゲームコアロジック）
**役割**: ゲームの核となるロジック

**分割方針**:
- **PaintCanvas.cs**: 塗りデータの管理のみ
- **PaintSystem.cs**: 塗り処理の実行（Strategy パターン）
- **PaintBattleGameManager.cs**: 全体の統合・ゲームループ
- **AttackTypeManager.cs**: 攻撃タイプの選択・管理（複数の選択モードに対応）
- **AttackTypeDetector.cs**: 攻撃タイプ判定（自動判定モード用）
- **PlayerManager.cs**: プレイヤー管理のインターフェース実装
- **LocalPlayerManager.cs**: オフライン用の実装

**理由**: 各機能を独立させることで、テスト・修正・拡張が容易

#### 3. Graphics/（グラフィック）
**役割**: 視覚的表現

**分割方針**:
- **InkEffect.cs**: エフェクトの再生・管理
- **PaintRenderer.cs**: 塗りデータの描画
- **EffectPool.cs**: パフォーマンス最適化

**理由**: グラフィック処理を分離し、レンダリングパイプラインの変更に対応しやすく

#### 4. Network/（ネットワーク）
**役割**: オンラインマルチプレイ

**分割方針**:
- **NetworkManager.cs**: ネットワーク接続管理
- **NetworkPaintSync.cs**: 塗りデータの同期
- **NetworkPlayerSync.cs**: プレイヤー情報の同期

**理由**: ネットワーク機能を分離し、オフラインモードに影響を与えない

#### 5. SinglePlayer/（シングルモード）
**役割**: モンスター撃破モード

**分割方針**:
- **Monster.cs**: モンスターの基本動作、HP管理
- **MonsterSpawner.cs**: モンスターの生成・管理
- **MonsterHitDetector.cs**: インクとモンスターの当たり判定
- **ScoreManager.cs**: スコア計算、コンボ管理
- **MovementPatterns/**: 移動パターンの実装（インターフェース化）

**理由**: 各機能を独立させ、モンスターの種類や移動パターンを追加しやすく

#### 6. UI/（UIシステム）
**役割**: ユーザーインターフェース

**分割方針**:
- 各画面（パネル）ごとにスクリプトを分割
- 既存の`VoiceDisplay`、`VoiceScatterPlot`はそのまま使用

**理由**: UIの変更が他のシステムに影響しないように

#### 7. Customization/（カスタマイズ）
**役割**: ユーザーカスタマイズ

**分割方針**:
- **InkCustomizer.cs**: インク関連のカスタマイズ
- **SoundCustomizer.cs**: サウンド関連のカスタマイズ
- **ThemeManager.cs**: テーマの管理

**理由**: カスタマイズ機能を独立させ、拡張しやすく

#### 8. Interfaces/（インターフェース）
**役割**: 抽象化定義

**分割方針**:
- 各インターフェースを1ファイルに1つ
- 実装は別ディレクトリに

**理由**: インターフェースの定義を明確にし、依存関係を整理

#### 9. Data/（ScriptableObject）
**役割**: 設定データの定義

**分割方針**:
- 機能ごとにディレクトリを分ける
- 1つのScriptableObjectクラス = 1ファイル

**理由**: 設定データの管理を明確に

### スクリプト分割の判断基準

**1つのスクリプトにまとめるべき場合**:
- ✅ 密接に関連する機能（例: `PaintCanvas`とその描画処理）
- ✅ 常に一緒に使われる機能
- ✅ 小規模なヘルパークラス

**別のスクリプトに分けるべき場合**:
- ✅ **異なる責任**: 塗り処理とプレイヤー管理は別
- ✅ **独立してテストしたい**: AIとネットワークは別
- ✅ **差し替え可能にしたい**: Strategy パターンで実装を分ける
- ✅ **開発者が異なる**: UIとゲームロジックは別

### 依存関係の管理

**依存関係の原則**:
- **上位層から下位層へ**: UI → GameLogic → VoiceDetection
- **インターフェース経由**: 直接参照ではなく、インターフェースで接続
- **Inspectorで接続**: 依存関係はInspectorで明示的に接続

**依存関係の例**:
```
UI/
  └─→ GameLogic/ (インターフェース経由)
        └─→ VoiceDetection/
        └─→ Graphics/
        └─→ Network/ (条件付き)
        └─→ AI/ (条件付き)
```

### 実装の優先順位

1. **Phase 1（プロトタイプ）**: 
   - `GameLogic/` - コアゲームロジック
   - `Graphics/` - 基本的なエフェクト
   - `Interfaces/` - 主要なインターフェース

2. **Phase 2（ブラッシュアップ）**:
   - `UI/` - ゲームHUD、メニュー
   - `Network/` - オンラインマルチ
   - `SinglePlayer/` - モンスター撃破モード

3. **Phase 3（完成）**:
   - `Customization/` - カスタマイズ機能
   - `SceneManagement/` - シーン管理
   - 最適化とリファクタリング

---

## 📋 現在の実装状況

### ✅ 実装済み
- **音声検出システム**
  - `VoiceDetector.cs` - マイク入力の取得
  - `VolumeAnalyzer.cs` - 音量の検出
  - `ImprovedPitchAnalyzer.cs` - ピッチの高精度検出（プリエンファシス、DC除去、放物線補間）
  - `VoiceCalibrator.cs` - キャリブレーション機能（ノイズ計測、動的閾値設定）

- **UIシステム**
  - `VoiceDisplay.cs` - 音量・ピッチのスライダー表示
  - `VoiceScatterPlot.cs` - 2Dグラフ表示（Volume×Pitchの可視化、軸の選択可能）

---

## 🎬 シーン構成と管理システム

### シーン分割の方針（小規模ゲーム向け）

**基本方針**: 最小限のシーン分割で、開発効率と保守性のバランスを取る

**シーンを分ける理由**:
1. **開発効率**: メニューとゲームプレイを分けることで、テストが容易
2. **保守性**: 機能ごとにシーンが分かれていると、バグ修正や機能追加が容易
3. **メモリ管理**: ゲームプレイ中に不要なメニューUIをアンロード

**注意**: 小規模ゲームでは、過度な分割は避ける。シーン遷移のオーバーヘッドと開発の複雑さを考慮。

### 推奨シーン構成（最小構成）

```
Assets/Scenes/
├── 00_MainMenu.unity           // メインメニュー + 設定 + カスタマイズ（統合）
├── 01_Gameplay.unity           // ゲームプレイ（全モード共通）
└── 99_Test.unity               // テスト用シーン（開発中のみ）
```

**構成の考え方**:
- **メインメニューシーン**: メニュー、設定、カスタマイズ、キャリブレーションを1つのシーンに統合
  - UIパネルで画面を切り替え（シーン遷移不要）
  - メモリ使用量が小さいため、統合しても問題なし
- **ゲームプレイシーン**: シングル、オフライン、オンライン全てを1つのシーンで対応
  - ゲームモードはコンポーネントの有無や設定で切り替え
  - ネットワークコンポーネントの追加/削除でオンライン/オフラインを切り替え

### 各シーンの役割

#### 00_MainMenu.unity（メインメニューシーン）
**目的**: メニュー、設定、カスタマイズ、キャリブレーションを統合

**含まれる要素**:
- **メインメニューUI**: ゲームモード選択ボタン
- **設定パネル**: 音量、グラフィック品質などの設定（UIパネルで表示/非表示）
- **カスタマイズパネル**: インク色、テクスチャ選択（UIパネルで表示/非表示）
- **キャリブレーションパネル**: 音声入力のキャリブレーション（UIパネルで表示/非表示）
- **シーン管理**: ゲームプレイシーンへの遷移
- **永続オブジェクト**: 設定データ、カスタマイズデータを保持（DontDestroyOnLoad）

**実装方針**:
```csharp
public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject customizationPanel;
    [SerializeField] private GameObject calibrationPanel;
    
    [Header("Scene Management")]
    [SerializeField] private string gameplaySceneName = "01_Gameplay";
    
    void Start()
    {
        // 初期化処理
        LoadSettings();
        ShowMainMenu();
    }
    
    public void ShowMainMenu() => ShowPanel(mainMenuPanel);
    public void ShowSettings() => ShowPanel(settingsPanel);
    public void ShowCustomization() => ShowPanel(customizationPanel);
    public void ShowCalibration() => ShowPanel(calibrationPanel);
    
    private void ShowPanel(GameObject panel)
    {
        // 全てのパネルを非表示にしてから、指定パネルのみ表示
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        customizationPanel.SetActive(false);
        calibrationPanel.SetActive(false);
        panel.SetActive(true);
    }
    
    public void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
    }
}
```

#### 01_Gameplay.unity（ゲームプレイシーン）
**目的**: 全てのゲームモード（シングル、オフライン、オンライン）を1つのシーンで対応

**設計方針**: 
- **同じシーンを使用**: 小規模ゲームでは、オンライン/オフラインで別シーンにする必要はない
- **初期化処理で分岐**: ゲームモードに応じて、異なる初期化処理を実行
- **コンポーネントの有効/無効**: ネットワーク関連コンポーネントは、オンラインモード時のみ有効化

**含まれる要素**:
- **ゲームマネージャー**: ゲームループの管理、モード初期化
- **塗りシステム**: インク塗りつぶし（オンライン/オフライン共通）
- **プレイヤー管理**: プレイヤー情報の管理（モードに応じて実装を切り替え）
- **ゲームUI**: HUD、タイマー、スコア表示（共通）
- **ネットワークコンポーネント**: オンラインモード時のみ有効化

**実装方針**:
```csharp
public enum GameMode
{
    SinglePlayer,   // CPU対戦
    OfflineMulti,   // オフラインマルチ（ローカル）
    OnlineMulti     // オンラインマルチ（ネットワーク）
}

public class GameplayManager : MonoBehaviour
{
    [Header("Game Mode")]
    [SerializeField] private GameMode currentMode = GameMode.OfflineMulti;
    
    [Header("Mode-Specific Components")]
    [SerializeField] private MonsterSpawner monsterSpawner; // シングルモード用
    [SerializeField] private NetworkManager networkManager; // オンラインモード用
    [SerializeField] private LocalPlayerManager localPlayerManager; // オフラインモード用
    
    [Header("Shared Components")]
    [SerializeField] private PaintSystem paintSystem;
    [SerializeField] private PaintCanvas canvas;
    [SerializeField] private GameHUD hud;
    
    void Start()
    {
        InitializeGameMode();
    }
    
    private void InitializeGameMode()
    {
        // 全てのモード固有コンポーネントを無効化
        if (monsterSpawner != null) monsterSpawner.gameObject.SetActive(false);
        if (networkManager != null) networkManager.gameObject.SetActive(false);
        if (localPlayerManager != null) localPlayerManager.gameObject.SetActive(false);
        
        // モードに応じて初期化
        switch (currentMode)
        {
            case GameMode.SinglePlayer:
                InitializeSinglePlayer();
                break;
            case GameMode.OfflineMulti:
                InitializeOfflineMulti();
                break;
            case GameMode.OnlineMulti:
                InitializeOnlineMulti();
                break;
        }
    }
    
    private void InitializeSinglePlayer()
    {
        // モンスター生成システムを有効化
        if (monsterSpawner != null)
        {
            monsterSpawner.gameObject.SetActive(true);
            monsterSpawner.Initialize();
        }
        // ローカルプレイヤー管理を使用（プレイヤー1人）
        if (localPlayerManager != null)
        {
            localPlayerManager.gameObject.SetActive(true);
            localPlayerManager.Initialize(1);
        }
    }
    
    private void InitializeOfflineMulti()
    {
        // ネットワークコンポーネントは無効
        if (networkManager != null)
            networkManager.gameObject.SetActive(false);
            
        // ローカルプレイヤー管理を使用
        if (localPlayerManager != null)
        {
            localPlayerManager.gameObject.SetActive(true);
            localPlayerManager.Initialize(2); // プレイヤー数2（オフラインマルチ）
        }
    }
    
    private void InitializeOnlineMulti()
    {
        // ネットワークコンポーネントを有効化
        if (networkManager != null)
        {
            networkManager.gameObject.SetActive(true);
            networkManager.Initialize();
        }
        
        // ローカルプレイヤー管理は無効（ネットワーク管理を使用）
        if (localPlayerManager != null)
            localPlayerManager.gameObject.SetActive(false);
    }
}
```

**オンライン/オフラインの違い**:

| 項目 | オフラインマルチ | オンラインマルチ |
|------|------------------|------------------|
| **プレイヤー管理** | `LocalPlayerManager`（ローカル） | `NetworkManager`（ネットワーク同期） |
| **塗りデータ同期** | 不要（同一端末） | 必要（ネットワーク経由） |
| **初期化処理** | シンプル（ローカル初期化のみ） | 複雑（ネットワーク接続、マッチメイキング） |
| **エラーハンドリング** | 不要 | 必要（ネットワーク切断など） |

**メリット（同じシーンを使う設計）**:
- ✅ **開発効率**: コードの重複が少ない
- ✅ **保守性**: 塗りシステムやUIは共通で管理
- ✅ **テスト容易性**: モードを切り替えてテスト可能
- ✅ **小規模ゲームに適している**: シーン遷移のオーバーヘッドがない

**注意点**:
- ネットワーク初期化処理は、オンラインモード時のみ実行
- プレイヤー管理は、インターフェース化してモードに応じて実装を切り替え
- エラーハンドリング（ネットワーク切断など）は、オンラインモード時のみ必要

#### 99_Test.unity（テスト用シーン）
**目的**: 開発中の機能テスト

**含まれる要素**:
- テスト用UI
- デバッグツール
- 各種システムの個別テスト（音声検出、塗りシステムなど）

**注意**: 本番ビルドには含めない

### シーン管理システムの実装（簡易版）

**ファイル**: `Assets/Script/SceneManagement/SimpleSceneManager.cs`

**実装内容**:
- 基本的なシーン遷移（メニュー ↔ ゲームプレイ）
- 必要に応じてローディング画面（オプション）

**変更しやすさの考慮事項**:
- **シンプルな実装**: 小規模ゲームでは複雑なシステムは不要
- **イベント発火**: 必要に応じてイベントを発火（オプション）
- **直接的な遷移**: `SceneManager.LoadScene`で十分な場合はそれを使用

**主要メソッド**:
```csharp
public class SimpleSceneManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "00_MainMenu";
    [SerializeField] private string gameplaySceneName = "01_Gameplay";
    
    [Header("Optional Loading Screen")]
    [SerializeField] private GameObject loadingScreenPrefab; // 必要に応じて
    
    public static event Action OnSceneChanged; // オプション
    
    // メニューからゲームプレイへ
    public void LoadGameplay()
    {
        if (loadingScreenPrefab != null)
        {
            StartCoroutine(LoadSceneWithLoading(gameplaySceneName));
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
            OnSceneChanged?.Invoke();
        }
    }
    
    // ゲームプレイからメニューへ
    public void LoadMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        OnSceneChanged?.Invoke();
    }
    
    // ローディング画面付き（必要に応じて）
    private IEnumerator LoadSceneWithLoading(string sceneName)
    {
        GameObject loadingScreen = Instantiate(loadingScreenPrefab);
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        if (loadingScreen != null)
            Destroy(loadingScreen);
            
        OnSceneChanged?.Invoke();
    }
}
```

**注意**: 
- 小規模ゲームでは、この程度の実装で十分
- 必要に応じて、後から機能を追加可能
- ローディング画面は、シーン遷移が遅い場合のみ実装

### シーン間のデータ受け渡し

**実装方針**: ScriptableObjectを使用した共有データ（DontDestroyOnLoadで保持）

**ファイル**: `Assets/Script/SceneManagement/GameData.cs`

```csharp
[CreateAssetMenu(fileName = "GameData", menuName = "Game/Game Data")]
public class GameData : ScriptableObject
{
    // カスタマイズデータ
    public InkTheme selectedTheme;
    public Color selectedColor;
    
    // ゲーム設定
    public GameSettings gameSettings;
    
    // キャリブレーションデータ
    public float calibratedVolume;
    public float calibratedPitch;
    
    // ゲームモード
    public GameMode selectedGameMode = GameMode.OfflineMulti;
}

// シーン間でデータを保持するマネージャー
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    
    [SerializeField] private GameData gameData;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public GameData GetGameData() => gameData;
}
```

**使用方法**:
- メニューシーンで設定・カスタマイズを変更
- `GameDataManager.Instance.GetGameData()`でデータにアクセス
- ゲームプレイシーンでデータを読み込んで使用

### シーン分割の判断基準（小規模ゲーム向け）

**シーンを分けるべき場合**:
- ✅ **メニューとゲームプレイ**: 機能が明確に分離されており、テストが容易
- ✅ **リソースの分離**: ゲームプレイ中にメニューUIが不要

**シーンを分けない方が良い場合**:
- ❌ **設定・カスタマイズ**: UIパネルで切り替え可能なため、別シーン不要
- ❌ **キャリブレーション**: メニューシーン内のパネルで十分
- ❌ **ゲームモード**: コンポーネントの有効/無効で対応可能

**結論**: 小規模ゲームでは、**メニューとゲームプレイの2シーン**で十分

### 実装の優先順位

1. **Phase 1（プロトタイプ）**: 
   - `01_Gameplay.unity` - ゲームプレイのみ（メニューなしで直接起動）
   - `99_Test.unity` - テスト用

2. **Phase 2（ブラッシュアップ）**:
   - `00_MainMenu.unity` - メニューシーン追加（設定・カスタマイズ・キャリブレーション統合）
   - `SimpleSceneManager.cs` - シーン遷移の実装

3. **Phase 3（完成）**:
   - `GameDataManager.cs` - データ受け渡しの実装
   - ローディング画面（必要に応じて）

---

## ✅ 企画書との整合性チェック

### 確認結果

#### ✅ 一致している要素

1. **操作方法（Volume/Pitch）**
   - 企画書: 縦軸=Volume、横軸=Pitch
   - 実装: `VoiceToScreenMapper`で実装済み（`volumeOnYAxis = true`）

2. **攻撃メカニズム**
   - 企画書: インパクトショット、ストリームペイントの2種類
   - 実装: `AttackTypeManager`で実装済み、複数の選択方法に対応（自動判定、手動選択、時間制ランダム）
   - **選択タイミング**: バトル前選択、バトル中変更、時間制ランダムなど、柔軟に対応可能

3. **上塗りメカニズム**
   - 企画書: 「すでに色が塗られている位置に上塗りして陣地を奪い合う」
   - 実装: `PaintCanvas.PaintAt()`で上塗り判定を実装（強度ベース）

4. **ゲームモード**
   - 企画書: オンラインマルチ、オフラインマルチ、シングルモード
   - 実装: 全て実装済み（`GameplayManager`でモード切り替え）

5. **勝利条件**
   - 企画書: 「制限時間終了時に、最終的に最も広範囲の陣地（色）を塗れたプレイヤーの勝利」
   - 実装: `VictoryCondition`で実装済み（`GetPaintedArea()`で面積計算）

6. **シングルモード**
   - 企画書: 「敵キャラクターを色を塗ることで倒していく」（更新: 移動するモンスターを狙う）
   - 実装: モンスター撃破モードとして実装済み

7. **カスタマイズ要素**
   - 企画書: インク（色）のカスタマイズ、サウンドエフェクト
   - 実装: `InkCustomizer`、`SoundCustomizer`で実装済み

#### 📝 実装の詳細

**コアゲームサイクル（企画書）**:
1. プレイヤーは声を出して、自分が色を塗りたい位置を音量とピッチで指定し、攻撃タイプを選択する。
   → ✅ 実装: `PaintBattleGameManager.Update()`で実装

2. プレイヤーが指定した位置にインクが噴出され、色が塗られる。
   → ✅ 実装: `PaintSystem`で実装

3. 相手プレイヤーは、声とインクタイプを使い分け、すでに色が塗られている位置に上塗りして陣地を奪い合う。
   → ✅ 実装: `PaintCanvas.PaintAt()`で上塗り判定を実装

**結論**: 企画書の内容は実装手順書に反映されています。

---

## 🎯 Phase 1: コアゲームシステム（～12月：プロトタイプ開発）

### Step 1.1: 画面着色システムの実装

**目標**: 声のピッチとボリュームで画面位置を指定し、その位置に色を塗る

#### 1.1.1: PaintCanvas システムの作成
**ファイル**: `Assets/Script/GameLogic/PaintCanvas.cs`

**実装内容**:
- 画面全体をテクスチャとして管理（RenderTexture使用推奨）
- 各ピクセルに「プレイヤーID」と「塗り強度」を記録
- 2D配列またはTexture2Dで塗り状態を管理
- **上塗りメカニズム**: 既存の色を上書きして陣地を奪い合う（企画書のコア機能）

**変更しやすさの考慮事項**:
- **ScriptableObject設定**: `PaintSettings.asset`で塗り強度、更新頻度、テクスチャ解像度を管理
- **イベント発火**: 塗り完了時に`OnPaintCompleted`イベントを発火（UI更新、エフェクト再生など）
- **インターフェース化**: `IPaintCanvas`インターフェースを定義し、実装を差し替え可能に

**主要メソッド**:
```csharp
[CreateAssetMenu(fileName = "PaintSettings", menuName = "Game/Paint Settings")]
public class PaintSettings : ScriptableObject
{
    [Header("Paint Properties")]
    [Range(0.1f, 5f)] public float baseIntensity = 1f;
    [Range(0.1f, 10f)] public float maxIntensity = 3f;
    [Range(1, 10)] public int updateRate = 1; // フレームごとの更新頻度
    
    [Header("Overpaint (上塗り) Properties")]
    [Range(0.5f, 3f)] public float overpaintThreshold = 1.5f; // 上塗りに必要な強度倍率
    [Tooltip("インパクトショットの上塗り倍率（企画書: 上塗りしやすい）")]
    [Range(1f, 5f)] public float impactOverpaintMultiplier = 2f;
    
    [Header("Canvas Properties")]
    public int textureWidth = 1920;
    public int textureHeight = 1080;
}

public interface IPaintCanvas
{
    void PaintAt(Vector2 screenPos, int playerId, float intensity, PaintType type);
    float GetPaintedArea(int playerId);
    void ResetCanvas();
}

public class PaintCanvas : MonoBehaviour, IPaintCanvas
{
    [SerializeField] private PaintSettings settings;
    
    public static event Action<Vector2, int, float> OnPaintCompleted;
    public static event Action<Vector2, int, int> OnTerritoryCaptured; // (position, newOwner, oldOwner)
    
    private int[,] playerIdData; // 各ピクセルのプレイヤーID
    private float[,] intensityData; // 各ピクセルの塗り強度
    
    public void PaintAt(Vector2 screenPos, int playerId, float intensity, PaintType type)
    {
        // 設定から強度を取得
        float baseIntensity = intensity * settings.baseIntensity;
        
        // インパクトショットは上塗り倍率を適用（企画書: 上塗りしやすい）
        if (type == AttackType.ImpactShot)
        {
            baseIntensity *= settings.impactOverpaintMultiplier;
        }
        
        // 画面座標をピクセル座標に変換
        int x = Mathf.RoundToInt(screenPos.x);
        int y = Mathf.RoundToInt(screenPos.y);
        
        // 範囲チェック
        if (x < 0 || x >= settings.textureWidth || y < 0 || y >= settings.textureHeight)
            return;
        
        // 既存の塗り状態を確認
        int existingPlayerId = playerIdData[x, y];
        float existingIntensity = intensityData[x, y];
        
        // 上塗り判定（企画書: すでに色が塗られている位置に上塗り）
        if (existingPlayerId != 0 && existingPlayerId != playerId)
        {
            // 上塗りに必要な強度をチェック
            float requiredIntensity = existingIntensity * settings.overpaintThreshold;
            if (baseIntensity >= requiredIntensity)
            {
                // 上塗り成功：陣地を奪取
                playerIdData[x, y] = playerId;
                intensityData[x, y] = baseIntensity;
                OnTerritoryCaptured?.Invoke(screenPos, playerId, existingPlayerId);
            }
            else
            {
                // 上塗り失敗：強度が足りない
                return;
            }
        }
        else
        {
            // 空白または自分の色：通常の塗り
            playerIdData[x, y] = playerId;
            intensityData[x, y] = Mathf.Max(intensityData[x, y], baseIntensity);
        }
        
        OnPaintCompleted?.Invoke(screenPos, playerId, baseIntensity);
    }
    
    public float GetPaintedArea(int playerId) 
    {
        int count = 0;
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                if (playerIdData[x, y] == playerId)
                    count++;
            }
        }
        return (float)count / (settings.textureWidth * settings.textureHeight);
    }
    
    public void ResetCanvas() 
    {
        // 塗りデータをリセット
        for (int x = 0; x < settings.textureWidth; x++)
        {
            for (int y = 0; y < settings.textureHeight; y++)
            {
                playerIdData[x, y] = 0;
                intensityData[x, y] = 0f;
            }
        }
    }
}
```

#### 1.1.2: 座標変換システム
**ファイル**: `Assets/Script/GameLogic/VoiceToScreenMapper.cs`

**実装内容**:
- ピッチ・ボリューム値 → 画面座標への変換
- `VoiceScatterPlot`のマッピングロジックを再利用可能にする

**主要メソッド**:
```csharp
public class VoiceToScreenMapper : MonoBehaviour
{
    public bool volumeOnYAxis = true; // 企画書: 縦軸=Volume
    
    // ピッチ・ボリューム → 画面座標
    public Vector2 MapToScreenPosition(float pitch, float volume, 
                                       float minPitch, float maxPitch,
                                       float minVolume, float maxVolume);
}
```

#### 1.1.3: インクエフェクトの実装
**ファイル**: `Assets/Script/GameLogic/InkEffect.cs`

**実装内容**:
- インクの視覚的表現（パーティクル、スプラッシュエフェクト）
- 塗りつぶし時のアニメーション

**変更しやすさの考慮事項**:
- **エフェクトシステムの抽象化**: `IInkEffect`インターフェースでエフェクトを差し替え可能に
- **ScriptableObject設定**: パーティクル設定、マテリアル、テクスチャを設定ファイルで管理
- **プレハブベース**: エフェクトはプレハブ化し、Inspectorで差し替え可能に
- **マテリアルシステム**: インクの見た目（色、テクスチャ、シェーダー）を動的に変更可能に
- **エフェクトプール**: パフォーマンスと柔軟性の両立

**主要メソッド**:
```csharp
public interface IInkEffect
{
    void PlayEffect(Vector2 position, int playerId, AttackType type, float intensity);
    void StopEffect();
    void SetMaterial(Material material);
    void SetColor(Color color);
}

[CreateAssetMenu(fileName = "InkEffectSettings", menuName = "Game/Graphics/Ink Effect Settings")]
public class InkEffectSettings : ScriptableObject
{
    [Header("Impact Shot Effect")]
    public GameObject impactShotPrefab;
    public ParticleSystem impactParticles;
    [Range(0.1f, 5f)] public float impactScale = 1f;
    [Range(0f, 2f)] public float impactDuration = 0.5f;
    
    [Header("Stream Paint Effect")]
    public GameObject streamPrefab;
    public ParticleSystem streamParticles;
    [Range(0.1f, 3f)] public float streamWidth = 1f;
    
    [Header("Materials")]
    public Material defaultInkMaterial;
    public Texture2D defaultInkTexture;
    public Shader inkShader;
}

[CreateAssetMenu(fileName = "InkMaterialData", menuName = "Game/Graphics/Ink Material")]
public class InkMaterialData : ScriptableObject
{
    [Header("Visual Properties")]
    public Material material;
    public Texture2D texture;
    public Color tintColor = Color.white;
    [Range(0f, 1f)] public float metallic = 0f;
    [Range(0f, 1f)] public float smoothness = 0.5f;
    
    [Header("Animation")]
    public AnimationCurve intensityCurve; // 強度による見た目の変化
    public bool useFlowAnimation = true;
    [Range(0f, 5f)] public float flowSpeed = 1f;
}

public class InkEffect : MonoBehaviour, IInkEffect
{
    [SerializeField] private InkEffectSettings settings;
    [SerializeField] private InkMaterialData materialData;
    
    private Dictionary<int, ParticleSystem> activeEffects = new Dictionary<int, ParticleSystem>();
    private Material currentMaterial;
    
    public void PlayEffect(Vector2 position, int playerId, AttackType type, float intensity)
    {
        GameObject prefab = type == AttackType.ImpactShot 
            ? settings.impactShotPrefab 
            : settings.streamPrefab;
            
        if (prefab != null)
        {
            GameObject effect = Instantiate(prefab, position, Quaternion.identity);
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            
            // マテリアルと色を適用
            if (materialData != null)
            {
                ApplyMaterial(ps, materialData, playerId);
            }
            
            // 強度に応じてスケール調整
            var main = ps.main;
            main.startSize = main.startSize.constant * intensity;
            
            activeEffects[playerId] = ps;
        }
    }
    
    private void ApplyMaterial(ParticleSystem ps, InkMaterialData data, int playerId)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && data.material != null)
        {
            // マテリアルのインスタンスを作成（各プレイヤー用）
            Material instance = new Material(data.material);
            instance.SetTexture("_MainTex", data.texture);
            instance.SetColor("_Color", data.tintColor);
            renderer.material = instance;
        }
    }
    
    public void SetMaterial(Material material)
    {
        currentMaterial = material;
        // アクティブなエフェクトに適用
        foreach (var effect in activeEffects.Values)
        {
            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.material = material;
        }
    }
    
    public void SetColor(Color color)
    {
        if (materialData != null)
            materialData.tintColor = color;
        // アクティブなエフェクトに即座に反映
        foreach (var effect in activeEffects.Values)
        {
            var main = effect.main;
            main.startColor = color;
        }
    }
    
    public void StopEffect() { /* エフェクト停止処理 */ }
}
```

---

### Step 1.2: 攻撃タイプシステムの実装

#### 1.2.1: 攻撃タイプ選択・管理システム
**ファイル**: `Assets/Script/GameLogic/AttackTypeManager.cs`

**実装内容**:
- 攻撃タイプの選択方法を柔軟に対応（手動選択、自動判定、時間制ランダム）
- 選択タイミングの設定（バトル前、バトル中、時間制など）
- 攻撃タイプの状態管理

**変更しやすさの考慮事項**:
- **選択モードの抽象化**: `IAttackTypeSelector`インターフェースで選択方法を差し替え可能に
- **ScriptableObject設定**: 選択モード、タイミング、ランダム間隔などを設定ファイルで管理
- **イベント発火**: 攻撃タイプ変更時にイベントを発火
- **拡張性**: 新しい選択方法を追加する際は、インターフェースを実装するだけ

**主要メソッド**:
```csharp
public enum AttackTypeSelectionMode
{
    Auto,           // 音声による自動判定（既存）
    Manual,         // 手動選択（バトル前またはバトル中）
    RandomTimed,    // 時間制でランダムに変更
    RandomOnPaint   // 塗るたびにランダム
}

[CreateAssetMenu(fileName = "AttackTypeSelectionSettings", menuName = "Game/Attack Type Selection Settings")]
public class AttackTypeSelectionSettings : ScriptableObject
{
    [Header("Selection Mode")]
    public AttackTypeSelectionMode mode = AttackTypeSelectionMode.Auto;
    
    [Header("Manual Selection")]
    [Tooltip("バトル開始前に選択するか")]
    public bool selectBeforeBattle = true;
    [Tooltip("バトル中に変更可能か")]
    public bool allowChangeDuringBattle = false;
    
    [Header("Random Timed Mode")]
    [Range(1f, 30f)] public float randomChangeInterval = 5f; // ランダム変更の間隔（秒）
    [Range(0f, 1f)] public float impactShotProbability = 0.5f; // インパクトショットの確率
    
    [Header("Auto Detection (既存の音声判定)")]
    public AttackSettings autoDetectionSettings; // 既存のAttackSettingsを参照
}

public interface IAttackTypeSelector
{
    AttackType GetCurrentType();
    void Initialize(AttackTypeSelectionSettings settings);
    void Update(float deltaTime);
    void SetManualType(AttackType type); // 手動選択用
}

public class AttackTypeManager : MonoBehaviour
{
    [SerializeField] private AttackTypeSelectionSettings settings;
    [SerializeField] private IAttackTypeSelector selector;
    
    private AttackType currentType = AttackType.None;
    
    public static event Action<AttackType> OnAttackTypeChanged;
    public static event Action<AttackType> OnAttackTypeSelected; // 手動選択時
    
    void Start()
    {
        InitializeSelector();
    }
    
    void Update()
    {
        if (selector != null)
        {
            selector.Update(Time.deltaTime);
            AttackType newType = selector.GetCurrentType();
            if (newType != currentType)
            {
                currentType = newType;
                OnAttackTypeChanged?.Invoke(currentType);
            }
        }
    }
    
    private void InitializeSelector()
    {
        // 選択モードに応じてセレクターを初期化
        switch (settings.mode)
        {
            case AttackTypeSelectionMode.Auto:
                selector = gameObject.AddComponent<AutoAttackTypeSelector>();
                break;
            case AttackTypeSelectionMode.Manual:
                selector = gameObject.AddComponent<ManualAttackTypeSelector>();
                break;
            case AttackTypeSelectionMode.RandomTimed:
                selector = gameObject.AddComponent<RandomTimedAttackTypeSelector>();
                break;
            case AttackTypeSelectionMode.RandomOnPaint:
                selector = gameObject.AddComponent<RandomOnPaintAttackTypeSelector>();
                break;
        }
        
        if (selector != null)
        {
            selector.Initialize(settings);
        }
    }
    
    // 手動選択用のメソッド（UIから呼び出し）
    public void SelectAttackType(AttackType type)
    {
        if (selector is ManualAttackTypeSelector manualSelector)
        {
            manualSelector.SetManualType(type);
            OnAttackTypeSelected?.Invoke(type);
        }
    }
    
    public AttackType GetCurrentType() => currentType;
}
```

#### 1.2.2: 各選択モードの実装

**ファイル**: `Assets/Script/GameLogic/AttackTypeSelectors/`

**実装内容**:
- 自動判定モード（既存の音声判定）
- 手動選択モード（バトル前/バトル中）
- 時間制ランダムモード
- 塗るたびランダムモード

**主要メソッド**:
```csharp
// 自動判定モード（既存の実装を再利用）
public class AutoAttackTypeSelector : MonoBehaviour, IAttackTypeSelector
{
    private IAttackTypeDetector detector;
    private AttackTypeSelectionSettings settings;
    
    public void Initialize(AttackTypeSelectionSettings settings)
    {
        this.settings = settings;
        detector = gameObject.AddComponent<AttackTypeDetector>();
        // 既存のAttackSettingsを適用
    }
    
    public AttackType GetCurrentType()
    {
        return detector?.CurrentType ?? AttackType.None;
    }
    
    public void Update(float deltaTime) { /* 自動判定はDetectorが処理 */ }
    public void SetManualType(AttackType type) { /* 無効 */ }
}

// 手動選択モード
public class ManualAttackTypeSelector : MonoBehaviour, IAttackTypeSelector
{
    private AttackType selectedType = AttackType.ImpactShot;
    private AttackTypeSelectionSettings settings;
    
    public void Initialize(AttackTypeSelectionSettings settings)
    {
        this.settings = settings;
        // バトル開始前の選択を待つ
        if (settings.selectBeforeBattle)
        {
            selectedType = AttackType.None; // 未選択状態
        }
    }
    
    public AttackType GetCurrentType() => selectedType;
    
    public void Update(float deltaTime) { /* 手動選択なので更新不要 */ }
    
    public void SetManualType(AttackType type)
    {
        if (settings.allowChangeDuringBattle || selectedType == AttackType.None)
        {
            selectedType = type;
        }
    }
}

// 時間制ランダムモード
public class RandomTimedAttackTypeSelector : MonoBehaviour, IAttackTypeSelector
{
    private AttackType currentType = AttackType.ImpactShot;
    private AttackTypeSelectionSettings settings;
    private float timer = 0f;
    
    public void Initialize(AttackTypeSelectionSettings settings)
    {
        this.settings = settings;
        RandomizeType();
    }
    
    public AttackType GetCurrentType() => currentType;
    
    public void Update(float deltaTime)
    {
        timer += deltaTime;
        if (timer >= settings.randomChangeInterval)
        {
            RandomizeType();
            timer = 0f;
        }
    }
    
    private void RandomizeType()
    {
        currentType = Random.value < settings.impactShotProbability 
            ? AttackType.ImpactShot 
            : AttackType.StreamPaint;
    }
    
    public void SetManualType(AttackType type) { /* 無効 */ }
}

// 塗るたびランダムモード
public class RandomOnPaintAttackTypeSelector : MonoBehaviour, IAttackTypeSelector
{
    private AttackType currentType = AttackType.ImpactShot;
    private AttackTypeSelectionSettings settings;
    
    public void Initialize(AttackTypeSelectionSettings settings)
    {
        this.settings = settings;
        RandomizeType();
        
        // 塗りイベントを購読
        PaintCanvas.OnPaintCompleted += OnPaint;
    }
    
    void OnDestroy()
    {
        PaintCanvas.OnPaintCompleted -= OnPaint;
    }
    
    private void OnPaint(Vector2 pos, int playerId, float intensity)
    {
        RandomizeType();
    }
    
    public AttackType GetCurrentType() => currentType;
    
    public void Update(float deltaTime) { /* 不要 */ }
    
    private void RandomizeType()
    {
        currentType = Random.value < settings.impactShotProbability 
            ? AttackType.ImpactShot 
            : AttackType.StreamPaint;
    }
    
    public void SetManualType(AttackType type) { /* 無効 */ }
}
```

#### 1.2.3: 攻撃タイプ判定システム（自動判定用、既存）
**ファイル**: `Assets/Script/GameLogic/AttackTypeDetector.cs`

**実装内容**:
- **インパクトショット**: 音量の急激な変化（微分値が閾値超）を検出
- **ストリームペイント**: 一定時間以上、音量が安定して継続している状態を検出
- **注意**: 自動判定モード時のみ使用される

**変更しやすさの考慮事項**:
- **インターフェース化**: `IAttackTypeDetector`を実装し、判定ロジックを差し替え可能に
- **ScriptableObject設定**: 閾値、判定時間、履歴サイズを全て設定ファイルで管理
- **イベント発火**: 攻撃タイプ変更時にイベントを発火
- **拡張性**: 新しい攻撃タイプを追加する際は、インターフェースを実装するだけ

**主要メソッド**:
```csharp
[CreateAssetMenu(fileName = "AttackSettings", menuName = "Game/Attack Settings")]
public class AttackSettings : ScriptableObject
{
    [Header("Impact Shot Detection")]
    [Range(0.01f, 1f)] public float impactVolumeThreshold = 0.3f; // 音量急上昇の閾値
    [Range(0.01f, 0.5f)] public float impactTimeWindow = 0.1f; // 判定時間窓
    
    [Header("Stream Paint Detection")]
    [Range(0.1f, 3f)] public float streamMinDuration = 0.5f; // 最小継続時間
    [Range(0.01f, 0.2f)] public float streamVolumeVariance = 0.05f; // 許容音量変動
    [Range(5, 30)] public int streamHistorySize = 10; // 履歴サイズ
    
    [Header("Smoothing")]
    [Range(0f, 1f)] public float typeChangeSmoothing = 0.2f; // タイプ変更のスムージング
}

public interface IAttackTypeDetector
{
    AttackType DetectAttackType(float volume, float pitch, float deltaTime);
    AttackType CurrentType { get; }
}

public enum AttackType
{
    None,
    ImpactShot,
    StreamPaint
}

public class AttackTypeDetector : MonoBehaviour, IAttackTypeDetector
{
    [SerializeField] private AttackSettings settings;
    
    public static event Action<AttackType> OnAttackTypeChanged;
    
    private Queue<float> volumeHistory = new Queue<float>();
    private float lastVolume = 0f;
    private AttackType currentType = AttackType.None;
    
    public AttackType CurrentType => currentType;
    
    public AttackType DetectAttackType(float currentVolume, float pitch, float deltaTime)
    {
        AttackType detectedType = AttackType.None;
        
        if (IsImpactShot(currentVolume))
            detectedType = AttackType.ImpactShot;
        else if (IsStreamPaint())
            detectedType = AttackType.StreamPaint;
        
        // スムージング適用
        if (detectedType != currentType)
        {
            currentType = detectedType;
            OnAttackTypeChanged?.Invoke(currentType);
        }
        
        return currentType;
    }
    
    private bool IsImpactShot(float currentVolume)
    {
        float volumeDelta = currentVolume - lastVolume;
        return volumeDelta > settings.impactVolumeThreshold;
    }
    
    private bool IsStreamPaint()
    {
        if (volumeHistory.Count < settings.streamHistorySize) return false;
        // 安定性チェック（設定から閾値を取得）
        // ...
        return true;
    }
}
```

#### 1.2.2: 攻撃タイプ別の塗りロジック
**ファイル**: `Assets/Script/GameLogic/PaintSystem.cs`

**実装内容**:
- **インパクトショット**: 指定位置に高密度の円状インクを塗る（半径固定、強度高）
- **ストリームペイント**: 前フレームからの軌跡に沿って連続的にインクを塗る（線状/面状）

**変更しやすさの考慮事項**:
- **Strategy パターン**: 各攻撃タイプを`IPaintStrategy`インターフェースで実装
- **ScriptableObject設定**: 半径、強度、軌跡の長さなどを設定ファイルで管理
- **Inspectorで差し替え**: 塗り戦略をInspectorで選択可能に

**主要メソッド**:
```csharp
public interface IPaintStrategy
{
    void Paint(Vector2 position, int playerId, float intensity, IPaintCanvas canvas);
}

[CreateAssetMenu(fileName = "ImpactShotSettings", menuName = "Game/Paint/Impact Shot")]
public class ImpactShotSettings : ScriptableObject
{
    [Range(10f, 200f)] public float radius = 50f;
    [Range(1f, 5f)] public float intensityMultiplier = 2f;
}

public class ImpactShotStrategy : ScriptableObject, IPaintStrategy
{
    [SerializeField] private ImpactShotSettings settings;
    
    public void Paint(Vector2 position, int playerId, float intensity, IPaintCanvas canvas)
    {
        // 設定から半径と強度を取得して塗る
        float finalIntensity = intensity * settings.intensityMultiplier;
        // 円状に塗る処理
    }
}

public class PaintSystem : MonoBehaviour
{
    [SerializeField] private IPaintStrategy impactShotStrategy;
    [SerializeField] private IPaintStrategy streamPaintStrategy;
    [SerializeField] private IPaintCanvas canvas;
    
    private Vector2 lastPaintPosition;
    private Queue<Vector2> paintTrail = new Queue<Vector2>();
    
    public void PaintImpactShot(Vector2 position, int playerId, float intensity)
    {
        impactShotStrategy?.Paint(position, playerId, intensity, canvas);
    }
    
    public void PaintStream(Vector2 currentPosition, int playerId, float intensity)
    {
        streamPaintStrategy?.Paint(currentPosition, playerId, intensity, canvas);
        UpdatePaintTrail(currentPosition);
    }
    
    private void UpdatePaintTrail(Vector2 position) { /* ... */ }
}
```

---

### Step 1.3: ゲームマネージャーの統合

**ファイル**: `Assets/Script/GameLogic/PaintBattleGameManager.cs`

**実装内容**:
- 音声検出 → 座標変換 → 攻撃タイプ判定 → 塗り処理の統合
- ゲームループの管理

**変更しやすさの考慮事項**:
- **Inspectorで接続**: 全ての依存コンポーネントをInspectorで接続（FindObjectOfTypeを避ける）
- **ScriptableObject設定**: ゲーム全体の設定を`GameSettings.asset`で管理
- **イベント駆動**: 各ステップでイベントを発火し、他のシステムが反応可能に
- **状態管理**: ゲーム状態（Playing, Paused, Ended）を明確に管理

**主要メソッド**:
```csharp
[CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Game Flow")]
    public float matchDuration = 180f;
    public int maxPlayers = 2;
    
    [Header("Paint Settings")]
    public PaintSettings paintSettings;
    public AttackSettings attackSettings;
    
    [Header("Balance")]
    [Range(0.5f, 2f)] public float paintSpeedMultiplier = 1f;
}

public class PaintBattleGameManager : MonoBehaviour
{
    [Header("Dependencies")] // Inspectorで接続
    [SerializeField] private VoiceToScreenMapper mapper;
    [SerializeField] private AttackTypeManager attackTypeManager; // 攻撃タイプ管理
    [SerializeField] private PaintSystem paintSystem;
    [SerializeField] private IPaintCanvas canvas;
    [SerializeField] private ImprovedPitchAnalyzer pitchAnalyzer;
    [SerializeField] private VolumeAnalyzer volumeAnalyzer;
    
    [Header("Settings")]
    [SerializeField] private GameSettings settings;
    
    private int currentPlayerId = 0;
    
    public static event Action<float, float> OnVoiceInput; // pitch, volume
    public static event Action<Vector2> OnPositionMapped;
    public static event Action<AttackType> OnAttackDetected;
    
    void Start()
    {
        // 攻撃タイプ変更イベントを購読
        if (attackTypeManager != null)
        {
            AttackTypeManager.OnAttackTypeChanged += OnAttackTypeChanged;
        }
    }
    
    void OnDestroy()
    {
        if (attackTypeManager != null)
        {
            AttackTypeManager.OnAttackTypeChanged -= OnAttackTypeChanged;
        }
    }
    
    void Update()
    {
        if (!IsGameActive()) return;
        
        // 1. 音声データ取得
        float pitch = pitchAnalyzer.lastDetectedPitch;
        float volume = volumeAnalyzer.lastDetectedVolume;
        OnVoiceInput?.Invoke(pitch, volume);
        
        // 2. 座標変換
        Vector2 screenPos = mapper.MapToScreenPosition(
            pitch, volume, 
            pitchAnalyzer.minFrequency, pitchAnalyzer.maxFrequency,
            0f, volumeAnalyzer.maxVolume
        );
        OnPositionMapped?.Invoke(screenPos);
        
        // 3. 攻撃タイプ取得（選択モードに応じて自動/手動/ランダム）
        AttackType type = attackTypeManager != null 
            ? attackTypeManager.GetCurrentType() 
            : AttackType.None;
        OnAttackDetected?.Invoke(type);
        
        // 4. 塗り処理（Strategy パターンで分岐不要）
        if (type != AttackType.None)
        {
            float intensity = volume * settings.paintSpeedMultiplier;
            paintSystem.Paint(screenPos, currentPlayerId, intensity, type);
        }
    }
    
    private void OnAttackTypeChanged(AttackType newType)
    {
        OnAttackDetected?.Invoke(newType);
    }
    
    private bool IsGameActive() { /* 状態チェック */ }
}
```

---

## 🎮 Phase 2: 対戦システム（1～2月：ブラッシュアップ）

### Step 2.1: プレイヤー管理システム

**ファイル**: `Assets/Script/GameLogic/PlayerManager.cs`

**実装内容**:
- 複数プレイヤーの管理（ID、色、スコア）
- プレイヤーごとの塗り面積の追跡

```csharp
public class PlayerManager : MonoBehaviour
{
    public class Player
    {
        public int playerId;
        public Color playerColor;
        public float paintedArea;
        public string playerName;
    }
    
    private List<Player> players = new List<Player>();
    
    public void RegisterPlayer(int id, Color color);
    public Player GetPlayer(int id);
    public Player GetWinner(); // 塗り面積最大のプレイヤー
}
```

---

### Step 2.2: 勝利条件判定システム

**ファイル**: `Assets/Script/GameLogic/VictoryCondition.cs`

**実装内容**:
- 制限時間の管理
- 塗り面積の計算と比較
- 勝利判定と結果表示

```csharp
public class VictoryCondition : MonoBehaviour
{
    public float matchDuration = 180f; // 3分
    private float remainingTime;
    
    void Update()
    {
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            EndMatch();
        }
    }
    
    private void EndMatch()
    {
        Player winner = playerManager.GetWinner();
        // 結果表示UIを表示
    }
}
```

---

### Step 2.3: オフラインマルチプレイの実装

**実装内容**:
- 同一端末での複数プレイヤー管理
- プレイヤー切り替え（キー入力または自動ローテーション）
- 各プレイヤーの音声入力を個別に処理

**考慮事項**:
- マイク入力の切り替え（複数マイク対応）
- または、音声の特徴（ピッチ/ボリュームのパターン）でプレイヤーを識別

---

### Step 2.4: オンラインマルチプレイの実装

**実装内容**:
- ネットワーク同期（Unity Netcode for GameObjects または Mirror を使用）
- 塗りデータの同期
- プレイヤー位置・攻撃タイプの同期

**主要コンポーネント**:
- `NetworkPaintCanvas.cs` - 塗りデータの同期
- `NetworkPlayerManager.cs` - プレイヤー情報の同期
- `MatchmakingSystem.cs` - マッチメイキング

---

## 🎨 Phase 3: UI/UX実装（1～2月）

### Step 3.1: ゲーム画面UI

**ファイル**: `Assets/Script/UI/GameHUD.cs`

**実装内容**:
- タイマー表示
- 各プレイヤーの塗り面積（パーセンテージ）表示
- 現在の攻撃タイプ表示（インパクト/ストリーム）
- ミニマップ（塗り状況の可視化）

---

### Step 3.1.1: 攻撃タイプ選択UI

**ファイル**: `Assets/Script/UI/AttackTypeSelectionUI.cs`

**実装内容**:
- バトル開始前の攻撃タイプ選択画面
- バトル中の攻撃タイプ変更UI（設定で有効化時）
- 現在の攻撃タイプの視覚的表示
- ランダムモード時のタイマー表示

**変更しやすさの考慮事項**:
- **設定連動**: `AttackTypeSelectionSettings`の設定に応じてUIを表示/非表示
- **イベント駆動**: `AttackTypeManager`のイベントを購読してUIを更新
- **Inspector設定**: UI要素はInspectorで接続可能に

**主要メソッド**:
```csharp
public class AttackTypeSelectionUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject selectionPanel; // バトル前選択パネル
    [SerializeField] private Button impactShotButton;
    [SerializeField] private Button streamPaintButton;
    [SerializeField] private TextMeshProUGUI currentTypeText;
    [SerializeField] private TextMeshProUGUI randomTimerText; // ランダムモード用
    
    [Header("References")]
    [SerializeField] private AttackTypeManager attackTypeManager;
    [SerializeField] private AttackTypeSelectionSettings settings;
    
    void Start()
    {
        // イベント購読
        AttackTypeManager.OnAttackTypeChanged += UpdateUI;
        AttackTypeManager.OnAttackTypeSelected += OnTypeSelected;
        
        // ボタン設定
        if (impactShotButton != null)
            impactShotButton.onClick.AddListener(() => SelectType(AttackType.ImpactShot));
        if (streamPaintButton != null)
            streamPaintButton.onClick.AddListener(() => SelectType(AttackType.StreamPaint));
        
        // 初期表示
        UpdateUI(AttackType.None);
        
        // バトル前選択が必要な場合
        if (settings != null && settings.mode == AttackTypeSelectionMode.Manual && settings.selectBeforeBattle)
        {
            ShowSelectionPanel();
        }
    }
    
    void OnDestroy()
    {
        AttackTypeManager.OnAttackTypeChanged -= UpdateUI;
        AttackTypeManager.OnAttackTypeSelected -= OnTypeSelected;
    }
    
    void Update()
    {
        // ランダムモード時のタイマー表示
        if (settings != null && settings.mode == AttackTypeSelectionMode.RandomTimed)
        {
            UpdateRandomTimer();
        }
    }
    
    private void SelectType(AttackType type)
    {
        if (attackTypeManager != null)
        {
            attackTypeManager.SelectAttackType(type);
        }
    }
    
    private void UpdateUI(AttackType type)
    {
        if (currentTypeText != null)
        {
            currentTypeText.text = type == AttackType.ImpactShot ? "インパクトショット" : "ストリームペイント";
        }
        
        // バトル中の変更が許可されている場合のみ、変更ボタンを表示
        if (settings != null && settings.allowChangeDuringBattle)
        {
            // バトル中の変更UIを表示
        }
    }
    
    private void OnTypeSelected(AttackType type)
    {
        // 選択完了時の処理
        if (selectionPanel != null && settings != null && settings.selectBeforeBattle)
        {
            selectionPanel.SetActive(false);
            // バトル開始を通知
        }
    }
    
    private void ShowSelectionPanel()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }
    }
    
    private void UpdateRandomTimer()
    {
        // ランダム変更までの残り時間を表示（実装はAttackTypeManagerから取得）
        if (randomTimerText != null)
        {
            // タイマー表示の実装
        }
    }
}
```

---

### Step 3.2: メニューシステム

**ファイル**: `Assets/Script/UI/MenuSystem.cs`

**実装内容**:
- メインメニュー（シングル/マルチ/設定）
- ゲーム設定（マッチ時間、塗り強度など）
- カスタマイズ画面（色、エフェクト選択）

---

## 🎨 Phase 4: カスタマイズシステム（1～2月：高優先度）

### Step 4.1: インクカスタマイズ

**ファイル**: `Assets/Script/Customization/InkCustomizer.cs`

**実装内容**:
- プレイヤー色の選択（カラーピッカー）
- テクスチャの適用
- 設定の保存（PlayerPrefs）

**変更しやすさの考慮事項**:
- **テーマシステム**: `InkTheme`（ScriptableObject）で色、テクスチャ、エフェクトをまとめて管理
- **プレハブ化**: カスタマイズ可能な要素は全てプレハブ化
- **リアルタイムプレビュー**: 設定変更時に即座にプレビュー表示
- **プリセット管理**: 複数のテーマプリセットを簡単に切り替え可能に

**主要メソッド**:
```csharp
[CreateAssetMenu(fileName = "InkTheme", menuName = "Game/Customization/Ink Theme")]
public class InkTheme : ScriptableObject
{
    [Header("Colors")]
    public Color primaryColor = Color.red;
    public Color secondaryColor = Color.blue;
    public Gradient colorGradient;
    
    [Header("Materials")]
    public InkMaterialData materialData;
    public Texture2D customTexture;
    
    [Header("Effects")]
    public InkEffectSettings effectSettings;
    
    [Header("Visual Style")]
    [Range(0f, 1f)] public float glossiness = 0.5f;
    [Range(0f, 1f)] public float transparency = 0.8f;
    public bool useGlow = true;
    public Color glowColor = Color.white;
}

public class InkCustomizer : MonoBehaviour
{
    [SerializeField] private List<InkTheme> availableThemes;
    [SerializeField] private InkTheme currentTheme;
    [SerializeField] private IInkEffect inkEffect;
    
    public static event Action<InkTheme> OnThemeChanged;
    
    public void ApplyTheme(InkTheme theme)
    {
        currentTheme = theme;
        
        // マテリアルを適用
        if (inkEffect != null && theme.materialData != null)
        {
            inkEffect.SetMaterial(theme.materialData.material);
            inkEffect.SetColor(theme.primaryColor);
        }
        
        // イベント発火
        OnThemeChanged?.Invoke(theme);
        
        // 保存
        SaveTheme(theme);
    }
    
    public void SetColor(Color color)
    {
        if (currentTheme != null)
        {
            currentTheme.primaryColor = color;
            inkEffect?.SetColor(color);
        }
    }
    
    public void SetTexture(Texture2D texture)
    {
        if (currentTheme != null && currentTheme.materialData != null)
        {
            currentTheme.materialData.texture = texture;
            // マテリアルに即座に反映
        }
    }
    
    private void SaveTheme(InkTheme theme)
    {
        // PlayerPrefsまたはJSONで保存
        string themeName = theme.name;
        PlayerPrefs.SetString("SelectedInkTheme", themeName);
    }
    
    public InkTheme LoadTheme()
    {
        string themeName = PlayerPrefs.GetString("SelectedInkTheme", "DefaultTheme");
        return availableThemes.Find(t => t.name == themeName);
    }
}
```

---

### Step 4.2: サウンドエフェクトカスタマイズ

**ファイル**: `Assets/Script/Customization/SoundCustomizer.cs`

**実装内容**:
- 噴射音の変更
- 上塗り音の変更
- BGM設定

---

## 🎯 Phase 5: シングルモード（モンスター撃破モード）（1～2月）

### Step 5.1: モンスターシステムの実装

**ファイル**: `Assets/Script/SinglePlayer/Monster.cs`

**実装内容**:
- 画面を移動するモンスターの実装
- モンスターの移動パターン（直線、曲線、ランダムなど）
- モンスターのHP/耐久値管理
- モンスターの視覚的表現

**変更しやすさの考慮事項**:
- **ScriptableObject設定**: モンスターの種類、移動速度、HPなどを設定ファイルで管理
- **移動パターンの抽象化**: `IMonsterMovement`インターフェースで移動パターンを差し替え可能に
- **イベント発火**: モンスター撃破時にイベントを発火

**主要メソッド**:
```csharp
[CreateAssetMenu(fileName = "MonsterSettings", menuName = "Game/SinglePlayer/Monster Settings")]
public class MonsterSettings : ScriptableObject
{
    [Header("Monster Properties")]
    [Range(1, 10)] public int maxHP = 3;
    [Range(10f, 500f)] public float moveSpeed = 100f;
    [Range(0.5f, 5f)] public float spawnInterval = 2f;
    
    [Header("Visual")]
    public GameObject monsterPrefab;
    public Color monsterColor = Color.red;
    [Range(20f, 200f)] public float monsterSize = 50f;
}

public interface IMonsterMovement
{
    Vector2 GetNextPosition(Vector2 currentPos, float deltaTime);
    void Initialize(Vector2 startPos, Vector2 targetPos);
}

public class Monster : MonoBehaviour
{
    [SerializeField] private MonsterSettings settings;
    [SerializeField] private IMonsterMovement movementPattern;
    
    private int currentHP;
    private Vector2 currentPosition;
    
    public static event Action<Monster> OnMonsterDefeated;
    public static event Action<Monster> OnMonsterSpawned;
    
    void Start()
    {
        currentHP = settings.maxHP;
        OnMonsterSpawned?.Invoke(this);
    }
    
    void Update()
    {
        // 移動パターンに従って移動
        if (movementPattern != null)
        {
            currentPosition = movementPattern.GetNextPosition(currentPosition, Time.deltaTime);
            transform.position = currentPosition;
        }
    }
    
    // インクが当たった時の処理
    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP <= 0)
        {
            Defeat();
        }
    }
    
    private void Defeat()
    {
        OnMonsterDefeated?.Invoke(this);
        // スコア加算、エフェクト再生など
        Destroy(gameObject);
    }
    
    public Vector2 GetPosition() => currentPosition;
    public int GetHP() => currentHP;
}
```

---

### Step 5.2: モンスター生成・管理システム

**ファイル**: `Assets/Script/SinglePlayer/MonsterSpawner.cs`

**実装内容**:
- モンスターの生成タイミング管理
- モンスターの移動パターン選択
- 画面外への移動判定と削除
- 難易度の段階的増加

**実装方針**:
```csharp
[CreateAssetMenu(fileName = "SpawnSettings", menuName = "Game/SinglePlayer/Spawn Settings")]
public class SpawnSettings : ScriptableObject
{
    [Header("Spawn Timing")]
    [Range(0.5f, 10f)] public float spawnInterval = 2f;
    [Range(1, 10)] public int maxMonstersOnScreen = 5;
    
    [Header("Difficulty")]
    public AnimationCurve difficultyCurve; // 時間経過による難易度変化
    [Range(1f, 5f)] public float speedMultiplierMax = 3f;
}

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private SpawnSettings settings;
    [SerializeField] private MonsterSettings monsterSettings;
    [SerializeField] private List<IMonsterMovement> movementPatterns;
    
    private float spawnTimer = 0f;
    private List<Monster> activeMonsters = new List<Monster>();
    private float gameTime = 0f;
    
    void Update()
    {
        gameTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;
        
        // 生成タイミングチェック
        float currentInterval = settings.spawnInterval / GetDifficultyMultiplier();
        if (spawnTimer >= currentInterval && activeMonsters.Count < settings.maxMonstersOnScreen)
        {
            SpawnMonster();
            spawnTimer = 0f;
        }
        
        // 画面外のモンスターを削除
        RemoveOffScreenMonsters();
    }
    
    private void SpawnMonster()
    {
        GameObject monsterObj = Instantiate(monsterSettings.monsterPrefab);
        Monster monster = monsterObj.GetComponent<Monster>();
        
        // 移動パターンをランダムに選択
        IMonsterMovement pattern = movementPatterns[Random.Range(0, movementPatterns.Count)];
        monster.SetMovementPattern(pattern);
        
        // 画面端から出現
        Vector2 spawnPos = GetRandomSpawnPosition();
        monster.transform.position = spawnPos;
        
        activeMonsters.Add(monster);
        Monster.OnMonsterDefeated += OnMonsterDefeated;
    }
    
    private float GetDifficultyMultiplier()
    {
        return 1f + (settings.difficultyCurve.Evaluate(gameTime / 60f) * (settings.speedMultiplierMax - 1f));
    }
    
    private void OnMonsterDefeated(Monster monster)
    {
        activeMonsters.Remove(monster);
    }
}
```

---

### Step 5.3: 当たり判定システム

**ファイル**: `Assets/Script/SinglePlayer/MonsterHitDetector.cs`

**実装内容**:
- インクがモンスターに当たったかの判定
- 当たった時のダメージ処理
- ヒットエフェクトの再生

**実装方針**:
```csharp
public class MonsterHitDetector : MonoBehaviour
{
    [SerializeField] private PaintSystem paintSystem;
    [SerializeField] private List<Monster> monsters;
    
    public static event Action<Vector2, Monster> OnMonsterHit;
    
    void Update()
    {
        // 塗りシステムから最新の塗り位置を取得
        Vector2 lastPaintPos = paintSystem.GetLastPaintPosition();
        
        // 各モンスターとの距離をチェック
        foreach (Monster monster in monsters)
        {
            float distance = Vector2.Distance(lastPaintPos, monster.GetPosition());
            float hitRadius = monster.GetHitRadius();
            
            if (distance < hitRadius)
            {
                // ヒット処理
                monster.TakeDamage(1);
                OnMonsterHit?.Invoke(lastPaintPos, monster);
            }
        }
    }
}
```

---

### Step 5.4: スコアシステム

**ファイル**: `Assets/Script/SinglePlayer/ScoreManager.cs`

**実装内容**:
- モンスター撃破時のスコア計算
- コンボシステム（連続撃破ボーナス）
- スコア表示
- ランキング保存

**実装方針**:
```csharp
[CreateAssetMenu(fileName = "ScoreSettings", menuName = "Game/SinglePlayer/Score Settings")]
public class ScoreSettings : ScriptableObject
{
    [Header("Score Values")]
    public int baseScorePerMonster = 100;
    public int comboBonusMultiplier = 10;
    [Range(2, 10)] public int maxCombo = 5;
}

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private ScoreSettings settings;
    
    private int currentScore = 0;
    private int currentCombo = 0;
    
    public static event Action<int> OnScoreUpdated;
    public static event Action<int> OnComboUpdated;
    
    void Start()
    {
        Monster.OnMonsterDefeated += OnMonsterDefeated;
    }
    
    private void OnMonsterDefeated(Monster monster)
    {
        // コンボ加算
        currentCombo = Mathf.Min(currentCombo + 1, settings.maxCombo);
        
        // スコア計算
        int scoreGain = settings.baseScorePerMonster + (currentCombo * settings.comboBonusMultiplier);
        currentScore += scoreGain;
        
        OnScoreUpdated?.Invoke(currentScore);
        OnComboUpdated?.Invoke(currentCombo);
    }
    
    public void ResetCombo()
    {
        currentCombo = 0;
        OnComboUpdated?.Invoke(0);
    }
}
```

---

## 🐛 Phase 6: 最適化とバグ修正（3月）

### Step 6.1: パフォーマンス最適化
- 塗りデータの更新頻度の最適化
- テクスチャ更新の効率化
- メモリ使用量の削減

### Step 6.2: バランス調整
- 攻撃タイプの強度調整
- 塗り速度の調整
- マッチ時間の調整

### Step 6.3: アクセシビリティ改善
- UIの文字サイズ調整
- 色覚多様性への配慮（色だけでなく形状でも区別）
- 操作説明の明確化

---

## 📝 実装の優先順位

### 🔴 最優先（プロトタイプ完成に必須）
1. PaintCanvas システム（Step 1.1）
2. 座標変換システム（Step 1.1.2）
3. 攻撃タイプ判定（Step 1.2.1）
4. 攻撃タイプ別塗りロジック（Step 1.2.2）
5. ゲームマネージャー統合（Step 1.3）

### 🟡 高優先度（対戦機能に必須）
6. プレイヤー管理システム（Step 2.1）
7. 勝利条件判定（Step 2.2）
8. オフラインマルチ（Step 2.3）
9. ゲーム画面UI（Step 3.1）

### 🟢 中優先度（完成度向上）
10. オンラインマルチ（Step 2.4）
11. カスタマイズシステム（Phase 4）
12. シングルモード（Phase 5）- モンスター撃破モード
13. メニューシステム（Step 3.2）

---

## 🔧 技術的な考慮事項

### 塗りデータの管理方法
- **Option 1**: Texture2D（各ピクセルにプレイヤーIDを記録）
  - メリット: 視覚的、Unity標準
  - デメリット: メモリ使用量が大きい
  
- **Option 2**: 2D配列（int[,]）
  - メリット: 軽量、高速アクセス
  - デメリット: 可視化に追加処理が必要

- **推奨**: ハイブリッド（内部は配列、表示はTexture2D）

### ネットワーク同期
- 塗りデータは頻繁に更新されるため、全ピクセルを同期するのは非効率
- 差分更新（変更された領域のみ送信）を実装
- または、塗りコマンド（位置、タイプ、強度）のみを同期

### パフォーマンス
- 塗り処理は毎フレーム実行されるため、最適化が重要
- 塗り範囲を制限（画面外は処理しない）
- 更新頻度を下げる（例: 60fps → 30fpsで塗り更新）

---

## 📚 参考リソース

- Unity RenderTexture ドキュメント
- Unity Netcode for GameObjects（オンラインマルチ用）
- Unity Particle System（インクエフェクト用）
- Unity UI Toolkit（UI実装用）

---

## ✅ 変更しやすさチェックリスト

各実装ステップで、以下の項目を確認してください：

### 設計チェック
- [ ] **ScriptableObject設定**: 調整が必要な値は全てScriptableObjectで管理されているか？
- [ ] **Inspector接続**: 依存関係はInspectorで接続可能か？（FindObjectOfTypeを避ける）
- [ ] **インターフェース化**: 差し替えが必要な機能はインターフェース化されているか？
- [ ] **イベント発火**: 他のシステムが反応すべきタイミングでイベントを発火しているか？
- [ ] **Tooltip/Header**: 全てのパラメータに説明とヘッダーが付いているか？
- [ ] **Range制限**: 数値パラメータに適切な範囲制限が設定されているか？

### コード品質チェック
- [ ] **単一責任**: 各クラスは1つの責任のみを持っているか？
- [ ] **依存性注入**: 依存関係は外部から注入可能か？
- [ ] **拡張性**: 新しい機能を追加する際、既存コードの変更が最小限か？
- [ ] **テスト容易性**: ユニットテストが書きやすい構造か？

### バランス調整チェック
- [ ] **設定ファイル**: バランス調整にコード変更は不要か？
- [ ] **プリセット**: 複数のバランスプリセットを簡単に切り替えられるか？
- [ ] **リアルタイム調整**: 実行中に設定を変更してテストできるか？（Editor拡張）

### グラフィック柔軟性チェック
- [ ] **エフェクト抽象化**: エフェクト実装はインターフェース化されているか？
- [ ] **マテリアル管理**: マテリアル、テクスチャ、色はScriptableObjectで管理されているか？
- [ ] **プレハブ化**: エフェクトはプレハブ化され、Inspectorで差し替え可能か？
- [ ] **動的変更**: 実行中にマテリアルや色を変更できるAPIがあるか？
- [ ] **テーマシステム**: テーマ（色、テクスチャ、エフェクト）をまとめて管理できるか？
- [ ] **レンダリングパイプライン**: Built-in/URP/HDRPに対応できる設計か？
- [ ] **カスタマイズ保存**: ユーザーのカスタマイズ設定が保存・読み込み可能か？

---

## ✅ チェックリスト

### Phase 1 完了条件
- [ ] 声を出すと画面の指定位置に色が塗られる
- [ ] インパクトショットとストリームペイントが正しく判定される
- [ ] 2種類の攻撃タイプで異なる塗り方が実装されている
- [ ] 基本的なゲームループが動作する

### Phase 2 完了条件
- [ ] 複数プレイヤーで対戦できる
- [ ] 塗り面積が正しく計算される
- [ ] 制限時間終了時に勝利判定が行われる
- [ ] オフラインマルチが動作する

### Phase 3 完了条件
- [ ] ゲーム画面に必要なUIが表示される
- [ ] メニューから各モードに遷移できる
- [ ] 設定が保存・読み込まれる


