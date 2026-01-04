# Phase 4: オンライン対戦実装（ColorDefenseモード対応）

## 🎯 目標

ColorDefenseモードのCPU部分をネットワーク上の他のプレイヤーに置き換え、オンライン対戦を実現する。

## 💰 費用と技術選定の概要

### 使用する技術
- **推奨**: **Unity Netcode for GameObjects**（Unity公式、**完全無料**）
- **代替案**: Mirror Networking（オープンソース、無料）

### 費用
- **最小構成（P2P方式）**: **完全無料**
- **専用サーバー使用**: 月額 **$5～$20程度**（小規模インスタンス）

### 同時接続可能人数
- **ColorDefenseモード**: **2人対戦**（どのフレームワークでも問題なく動作）
- **理論上の上限**: 数十～数百人（サーバー性能とネットワーク設計に依存）

詳細は「[Step 4.1: ネットワークフレームワークの選択とセットアップ](#step-41-ネットワークフレームワークの選択とセットアップ)」を参照してください。

## 🎬 シーン構成

### 設計方針: 1つのシーンで全てを管理

**重要**: **メインメニュー + 設定 + カスタマイズ + ゲームプレイ全てを1つのシーンで管理**する設計を採用します。

#### シーン構成

```
Assets/Scenes/
└── GameScene.unity             // 全ての機能を1つのシーンに統合
    ├── メインメニュー（TitlePanel）
    ├── 設定（SettingsPanel）
    ├── カスタマイズ
    ├── キャリブレーション
    ├── オフライン/オンライン選択（OnlineOfflineSelectionPanel）
    ├── ゲームセレクト（GameModeSelectionPanel）
    └── ゲームプレイ（シングル、オフライン、オンライン全モード共通）
```

#### 設計の考え方

1. **1つのシーンで全てを管理**
   - タイトル、設定、カスタマイズ、キャリブレーション、ゲームプレイを1つのシーンに統合
   - UIパネルで画面を切り替え（シーン遷移不要）
   - メモリ使用量が小さいため、統合しても問題なし

2. **ゲームモードの切り替え**
   - **シングル、オフライン、オンライン全てを同じシーンで対応**
   - ゲームモードはコンポーネントの有無や設定で切り替え
   - ネットワークコンポーネントの追加/削除でオンライン/オフラインを切り替え
   - UIパネルの表示/非表示でメニューとゲームプレイを切り替え

#### 同じシーンを使う設計のメリット

| 項目 | メリット |
|------|---------|
| **コードの重複** | なし（`PaintCanvas`、`ColorDefenseMode`、UI等を共通化） |
| **保守性** | 高い（1箇所の修正で全モード対応） |
| **テスト容易性** | 高い（モード切り替えでテスト可能） |
| **実装の複雑さ** | 低い（コンポーネントの有効/無効のみ） |
| **既存スクリプトへの影響** | 最小限（`GameModeManager`を使用） |

#### 実装方針

```csharp
// GameplayManager.cs の実装例

public class GameplayManager : MonoBehaviour
{
    [Header("Mode-Specific Components")]
    [SerializeField] private NetworkManager networkManager; // オンラインモード用
    [SerializeField] private EnemyPainter enemyPainter; // オフラインモード用（CPU）
    
    [Header("Shared Components")]
    [SerializeField] private PaintCanvas paintCanvas; // 全モード共通
    [SerializeField] private ColorDefenseMode colorDefenseMode; // 全モード共通
    
    void Start()
    {
        // GameModeManagerからオンライン/オフラインを取得
        bool isOnline = GameModeManager.Instance != null && 
                       GameModeManager.Instance.CurrentGameModeType == GameType.Online;
        
        if (isOnline)
        {
            // オンライン: ネットワークコンポーネントを有効化、CPUを無効化
            InitializeOnline();
        }
        else
        {
            // オフライン: ローカルコンポーネントを有効化、ネットワークを無効化
            InitializeOffline();
        }
    }
    
    private void InitializeOnline()
    {
        // NetworkManagerを有効化
        if (networkManager != null)
        {
            networkManager.gameObject.SetActive(true);
        }
        
        // EnemyPainter（CPU）を無効化
        if (enemyPainter != null)
        {
            enemyPainter.gameObject.SetActive(false);
        }
    }
    
    private void InitializeOffline()
    {
        // NetworkManagerを無効化
        if (networkManager != null)
        {
            networkManager.gameObject.SetActive(false);
        }
        
        // EnemyPainter（CPU）を有効化
        if (enemyPainter != null)
        {
            enemyPainter.gameObject.SetActive(true);
        }
    }
}
```

#### UIパネル切り替えの流れ（シーン遷移なし）

1. **メニュー画面（UIパネル）**
   - タイトル → 設定 → オフライン/オンライン選択 → ゲームセレクト
   - 全てUIパネルの表示/非表示で切り替え（シーン遷移不要）
   - マッチメイキング（オンラインの場合）
   - ネットワーク接続開始（オンラインの場合）

2. **ゲームプレイ画面（UIパネル）への切り替え**
   - 全クライアントが接続完了を確認（オンラインの場合）
   - メニューUIパネルを非表示、ゲームプレイUIパネルを表示
   - **重要**: シーン遷移は不要（同じシーン内でUIパネルを切り替え）

3. **ゲームプレイ中**
   - `GameModeManager`からオンライン/オフライン状態を取得
   - モードに応じてコンポーネントを有効/無効化
   - ネットワーク接続を維持（オンラインの場合）
   - ゲーム終了後はメニューUIパネルに戻る（シーン遷移不要）

#### 注意点

- **シーン遷移は不要**
  - 全ての機能を1つのシーンで管理
  - UIパネルの表示/非表示で画面を切り替え
  - ネットワーク接続を維持（オンラインの場合）

- **NetworkManagerは`DontDestroyOnLoad`で保持（オプション）**
  - シーンが1つなので、通常は不要
  - 将来的にシーン分割する場合に備えて実装可能

- **UIパネルの管理**
  - メニューUIパネルとゲームプレイUIパネルを適切に切り替え
  - ゲーム開始時: メニューUIを非表示、ゲームプレイUIを表示
  - ゲーム終了時: ゲームプレイUIを非表示、メニューUIを表示

#### 実装例（UIパネル切り替え）

```csharp
// ゲーム開始時のUIパネル切り替え

public class GameplayManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject menuPanel; // メニューUIパネル
    [SerializeField] private GameObject gameplayPanel; // ゲームプレイUIパネル
    
    public void StartGame()
    {
        bool isOnline = GameModeManager.Instance != null && 
                       GameModeManager.Instance.CurrentGameModeType == GameType.Online;
        
        // オンライン: 全クライアントが接続完了を確認
        if (isOnline)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // サーバーが全クライアントにゲーム開始を通知
                StartGameClientRpc();
            }
        }
        else
        {
            // オフライン: 直接ゲーム開始
            ShowGameplayPanel();
        }
    }
    
    [ClientRpc]
    private void StartGameClientRpc()
    {
        ShowGameplayPanel();
    }
    
    private void ShowGameplayPanel()
    {
        // メニューUIを非表示
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        
        // ゲームプレイUIを表示
        if (gameplayPanel != null)
        {
            gameplayPanel.SetActive(true);
        }
        
        // ゲーム開始処理
        InitializeGameMode();
    }
    
    public void ReturnToMenu()
    {
        // ゲームプレイUIを非表示
        if (gameplayPanel != null)
        {
            gameplayPanel.SetActive(false);
        }
        
        // メニューUIを表示
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
    }
}
```

---

## 📋 UIフロー

### 正しいフロー
```
タイトル 
  ↓（セーブデータがない場合）
音に関する設定（SettingsPanel）
  ↓
オフライン/オンライン選択（OnlineOfflineSelectionPanel）
  ↓
ゲームセレクト（GameModeSelectionPanel）
  ↓
ゲーム開始
```

**注意**: セーブデータがある場合は、タイトルから直接オフライン/オンライン選択画面に遷移します。

### 実装済みコンポーネント
- **TitlePanel**: タイトル画面（修正済み）
  - セーブデータがない場合: `SettingsPanel`（音に関する設定）に遷移
  - セーブデータがある場合: `OnlineOfflineSelectionPanel`に直接遷移
- **SettingsPanel**: 音に関する設定画面（修正済み）
  - 「次へ」ボタンで`OnlineOfflineSelectionPanel`に遷移
- **OnlineOfflineSelectionPanel**: オフライン/オンライン選択画面（新規作成）
  - オフライン/オンラインを選択
  - `GameModeManager`に選択を保存
  - `GameModeSelectionPanel`に遷移
- **GameModeSelectionPanel**: ゲームモード選択画面（既存、変更なし）
  - 既存のシングルプレイ用スクリプトは変更なし
  - `GameModeManager`からオンライン/オフライン状態を取得可能
- **GameModeManager**: オフライン/オンライン状態管理（新規作成）
  - シングルトンで状態を管理
  - 既存スクリプトから状態を取得可能

### 既存スクリプトへの影響
- **GameModeSelectionPanel**: 変更なし（既存のまま使用可能）
- **ColorDefenseMode**: 変更なし（GameModeManagerから状態を取得）
- **SinglePlayerModeManager**: 変更なし（GameModeManagerから状態を取得）

## 📋 実装概要

### シーン構成（重要）

**設計方針**: **1つのシーン（GameScene）で全てを管理**

- **1つのシーン（GameScene）**: メニュー、設定、カスタマイズ、ゲームプレイ全てを統合
  - UIパネルで画面を切り替え（シーン遷移不要）
  - シングル、オフライン、オンライン全モード共通
  - `GameModeManager`からオンライン/オフライン状態を取得
  - モードに応じてコンポーネントを有効/無効化
  - ネットワークコンポーネントはオンラインモード時のみ有効化

**メリット**:
- コードの重複なし（`PaintCanvas`、`ColorDefenseMode`等を共通化）
- 保守性が高い（1箇所の修正で全モード対応）
- 既存スクリプトへの影響が最小限
- シーン遷移のオーバーヘッドなし
- ネットワーク接続を維持（オンライン時）

詳細は「[シーン構成](#-シーン構成)」セクションを参照してください。

### 現在の実装（シングルプレイ）
- **CPU**: `EnemyPainter`クラスが自動で塗りを実行
- **プレイヤーID**: プレイヤー = `1`, CPU = `-1`
- **塗り処理**: `PaintCanvas.PaintAt()`でローカル処理
- **シーン**: `01_Gameplay`（オフラインモード）

### オンライン対戦での変更点
- **CPU → ネットワークプレイヤー**: `EnemyPainter`の代わりに、ネットワーク経由で他のプレイヤーの塗りデータを受信
- **塗りデータの同期**: 各プレイヤーの塗りコマンドをネットワーク経由で送受信
- **プレイヤー管理**: ネットワーク接続されたプレイヤーを管理
- **シーン**: `GameScene`（**1つのシーン**、UIパネルとコンポーネントの有効/無効で切り替え）
- **既存スクリプトへの影響**: 最小限（`GameModeManager`を使用）

---

## 🔧 実装手順

### Step 4.0: UIフローの実装（実装済み）

#### 4.0.1: OnlineOfflineSelectionPanelの実装
**ファイル**: `Assets/Main/Script/UI/OnlineOfflineSelectionPanel.cs`（実装済み）

**実装内容**:
- オフライン/オンライン選択画面
- 選択を`GameModeManager`に保存
- `GameModeSelectionPanel`に遷移

#### 4.0.2: GameModeManagerの実装
**ファイル**: `Assets/Main/Script/Data/GameModeManager.cs`（実装済み）

**実装内容**:
- オフライン/オンライン状態を管理するシングルトン
- 既存スクリプトから状態を取得可能
- `DontDestroyOnLoad`でシーン間で状態を保持

#### 4.0.3: TitlePanelの修正
**ファイル**: `Assets/Main/Script/UI/TitlePanel.cs`（修正済み）

**変更内容**:
- セーブデータがない場合: `SettingsPanel`（音に関する設定）に遷移
- セーブデータがある場合: `OnlineOfflineSelectionPanel`に直接遷移

#### 4.0.4: SettingsPanelの修正
**ファイル**: `Assets/Main/Script/UI/SettingsPanel.cs`（修正済み）

**変更内容**:
- 「次へ」ボタンで`OnlineOfflineSelectionPanel`に遷移（従来は`GameModeSelectionPanel`に直接遷移）
- 後方互換性のため、`OnlineOfflineSelectionPanel`がない場合は`GameModeSelectionPanel`に直接遷移

**実装方針**:
```csharp
// SettingsPanel.cs の変更点

[Header("Navigation")]
[Tooltip("オフライン/オンライン選択パネル")]
[SerializeField] private OnlineOfflineSelectionPanel onlineOfflineSelectionPanel;

private void TransitionToOnlineOfflineSelection()
{
    Hide();
    if (gameObject != null)
    {
        gameObject.SetActive(false);
    }
    
    // オフライン/オンライン選択画面を表示
    if (onlineOfflineSelectionPanel != null)
    {
        onlineOfflineSelectionPanel.Show();
    }
    else if (gameModeSelectionPanel != null)
    {
        // 後方互換性: 直接GameModeSelectionPanelに遷移
        gameModeSelectionPanel.Show();
    }
}
```

---

### Step 4.1: ネットワークフレームワークの選択とセットアップ

#### 推奨フレームワーク: Unity Netcode for GameObjects

**理由**:
- Unity公式サポート
- **完全無料**（Unityの一部として提供）
- ホスト/クライアントアーキテクチャがシンプル
- スムーズな統合が可能
- 公式ドキュメントが充実

**代替案**: 
- **Mirror Networking**（オープンソース、無料、コミュニティサポート、機能豊富）
- **Photon Realtime**（有料、無料プランあり、専用サーバー不要）

#### 費用について

| フレームワーク | 費用 | 備考 |
|--------------|------|------|
| **Unity Netcode for GameObjects** | **無料** | Unityの一部として提供。追加費用なし |
| **Mirror Networking** | **無料** | オープンソース。完全無料 |
| **Photon Realtime** | 無料プランあり<br>有料プラン: $95/月～ | 無料プラン: 同時接続20人まで<br>有料プラン: 同時接続数に応じて |

**推奨**: ColorDefenseモードは2人対戦なので、**Unity Netcode for GameObjects**（無料）で十分です。

#### 同時接続可能人数

| フレームワーク | 同時接続可能人数 | 備考 |
|--------------|----------------|------|
| **Unity Netcode for GameObjects** | **理論上無制限**<br>実用的には**数十～数百人** | サーバーの性能とネットワーク設計に依存<br>ColorDefense（2人対戦）には十分 |
| **Mirror Networking** | **理論上無制限**<br>実用的には**数十～数百人** | サーバーの性能とネットワーク設計に依存 |
| **Photon Realtime** | 無料プラン: **20人**<br>有料プラン: **数百～数千人** | プランによって異なる |

**ColorDefenseモードの場合**:
- **2人対戦**なので、どのフレームワークでも問題なく動作
- **Unity Netcode for GameObjects（無料）**で十分対応可能
- サーバー費用も不要（P2Pまたはホスト/クライアント方式）

#### サーバー費用について

**Unity Netcode for GameObjects / Mirror Networking**:
- **P2P方式**: サーバー費用なし（プレイヤー間で直接接続）
- **専用サーバー方式**: サーバー費用が必要（AWS、Azure、GCPなど）
  - 小規模（2人対戦）: 月額 **$5～$20程度**（t2.micro等の小規模インスタンス）
  - 中規模: 月額 **$50～$200程度**

**Photon Realtime**:
- 専用サーバー不要（Photonのサーバーを使用）
- 無料プラン: 同時接続20人まで
- 有料プラン: 月額 **$95～**（同時接続数に応じて）

**推奨構成（ColorDefense 2人対戦）**:
- **Unity Netcode for GameObjects + P2P方式**: **完全無料**
- または **Unity Netcode for GameObjects + 専用サーバー**: 月額 **$5～$20程度**

#### セットアップ手順

1. **Package ManagerでNetcode for GameObjectsをインストール**
   ```
   Window > Package Manager > Unity Registry > Netcode for GameObjects
   ```
   **費用**: 無料

2. **NetworkManagerの作成**
   - **GameScene**にNetworkManagerオブジェクトを配置
   - ネットワーク設定を構成
   - P2P方式または専用サーバー方式を選択
   - **重要**: シーンが1つなので、`DontDestroyOnLoad`は不要（将来的にシーン分割する場合に備えて実装可能）

3. **シーン構成の確認**
   - **GameScene.unity**: メニュー、設定、カスタマイズ、ゲームプレイ全てを統合
   - **重要**: 1つのシーンで全てを管理、UIパネルで画面を切り替え

4. **GameplayManagerの実装**
   - `GameModeManager`からオンライン/オフライン状態を取得
   - モードに応じてNetworkManagerとEnemyPainterを有効/無効化
   - 詳細は「[シーン構成](#-シーン構成)」セクションの実装例を参照

---

### Step 4.2: ネットワーク対応PaintCanvasの実装

**ファイル**: `Assets/Main/Script/Network/NetworkPaintCanvas.cs`

**実装内容**:
- 塗りコマンドの送信（ローカルプレイヤーの塗りをネットワークに送信）
- 塗りコマンドの受信（他のプレイヤーの塗りを適用）
- 塗りデータの同期（初期状態の同期）

**主要メソッド**:
```csharp
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ネットワーク対応PaintCanvas
/// 塗りコマンドをネットワーク経由で同期
/// </summary>
public class NetworkPaintCanvas : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    // 塗りコマンドのネットワーク変数
    private NetworkVariable<int> lastPaintPlayerId = new NetworkVariable<int>();
    private NetworkVariable<Vector2> lastPaintPosition = new NetworkVariable<Vector2>();
    private NetworkVariable<float> lastPaintIntensity = new NetworkVariable<float>();
    private NetworkVariable<Color> lastPaintColor = new NetworkVariable<Color>();
    
    // 塗りコマンド送信用のRPC
    public void SendPaintCommand(Vector2 position, int playerId, float intensity, Color color)
    {
        // サーバーに塗りコマンドを送信
        SendPaintCommandServerRpc(position, playerId, intensity, color);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SendPaintCommandServerRpc(Vector2 position, int playerId, float intensity, Color color)
    {
        // サーバーで塗りを実行し、全クライアントに同期
        ApplyPaintCommandClientRpc(position, playerId, intensity, color);
    }
    
    [ClientRpc]
    private void ApplyPaintCommandClientRpc(Vector2 position, int playerId, float intensity, Color color)
    {
        // ローカルで塗りを実行（送信元以外のクライアント）
        if (paintCanvas != null && !IsOwner)
        {
            paintCanvas.PaintAt(position, playerId, intensity, color);
        }
    }
    
    // 初期状態の同期（ゲーム開始時）
    public void SyncInitialState()
    {
        // キャンバスの初期状態を全クライアントに送信
        // 実装は必要に応じて追加
    }
}
```

**注意点**:
- 塗りコマンドは頻繁に送信されるため、送信頻度を制限する必要がある
- バッファリングやバッチ送信を検討

---

### Step 4.3: ネットワーク対応ColorDefenseModeの実装

**ファイル**: `Assets/Main/Script/Network/NetworkColorDefenseMode.cs`

**実装内容**:
- `ColorDefenseMode`を継承またはラップ
- CPU（`EnemyPainter`）の代わりにネットワークプレイヤーを使用
- ゲーム状態の同期（開始、終了、スコアなど）
- **重要**: `GameModeManager`からオンライン/オフライン状態を取得

**実装方針**:
```csharp
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ネットワーク対応ColorDefenseMode
/// CPUの代わりにネットワークプレイヤーを使用
/// </summary>
public class NetworkColorDefenseMode : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private ColorDefenseMode colorDefenseMode;
    [SerializeField] private NetworkPaintCanvas networkPaintCanvas;
    
    // ネットワークプレイヤー管理
    private Dictionary<ulong, int> networkPlayerIds = new Dictionary<ulong, int>();
    private int localPlayerId = 1; // ローカルプレイヤーのID
    private int enemyPlayerId = -1; // 敵プレイヤーのID（ネットワーク経由）
    
    // ゲーム状態の同期
    private NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false);
    private NetworkVariable<float> gameTime = new NetworkVariable<float>(0f);
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // GameModeManagerからオンラインモードかどうかを確認
        bool isOnlineMode = GameModeManager.Instance != null && GameModeManager.Instance.IsOnlineMode;
        
        if (!isOnlineMode)
        {
            // オフラインモードの場合は、このコンポーネントを無効化
            Debug.Log("NetworkColorDefenseMode: オフラインモードのため、ネットワーク機能を無効化します");
            enabled = false;
            return;
        }
        
        // ネットワークプレイヤーのIDを割り当て
        if (IsServer)
        {
            AssignPlayerIds();
        }
        
        // ゲーム開始
        if (IsServer)
        {
            StartGameServer();
        }
    }
    
    private void AssignPlayerIds()
    {
        // サーバーでプレイヤーIDを割り当て
        int playerIdCounter = 1;
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                localPlayerId = playerIdCounter;
            }
            else
            {
                networkPlayerIds[clientId] = -playerIdCounter; // 敵プレイヤーは負のID
            }
            playerIdCounter++;
        }
        
        // クライアントにIDを通知
        AssignPlayerIdsClientRpc(localPlayerId);
    }
    
    [ClientRpc]
    private void AssignPlayerIdsClientRpc(int assignedPlayerId)
    {
        localPlayerId = assignedPlayerId;
        // 敵プレイヤーのIDを設定（他のプレイヤー）
        foreach (var kvp in networkPlayerIds)
        {
            if (kvp.Key != NetworkManager.Singleton.LocalClientId)
            {
                enemyPlayerId = kvp.Value;
                break;
            }
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void StartGameServer()
    {
        if (colorDefenseMode != null)
        {
            // CPUの代わりにネットワークプレイヤーを使用する設定
            // EnemyPainterを無効化し、ネットワークプレイヤーの塗りを有効化
            colorDefenseMode.StartGame();
            
            // ゲーム状態を同期
            isGameActive.Value = true;
            gameTime.Value = 0f;
        }
    }
    
    void Update()
    {
        if (!IsServer) return;
        
        if (isGameActive.Value)
        {
            // ゲーム時間を更新
            gameTime.Value += Time.deltaTime;
            
            // ColorDefenseModeの更新（CPUの代わりにネットワークプレイヤーが塗る）
            if (colorDefenseMode != null)
            {
                colorDefenseMode.UpdateGame(Time.deltaTime);
            }
        }
    }
    
    // ネットワークプレイヤーの塗りを処理
    public void OnNetworkPlayerPaint(Vector2 position, float intensity, Color color)
    {
        // ネットワーク経由で塗りコマンドを送信
        if (networkPaintCanvas != null)
        {
            networkPaintCanvas.SendPaintCommand(position, enemyPlayerId, intensity, color);
        }
    }
}
```

---

### Step 4.4: PaintBattleGameManagerのネットワーク対応

**ファイル**: `Assets/Main/Script/Network/NetworkPaintBattleGameManager.cs`

**実装内容**:
- ローカルプレイヤーの塗りをネットワークに送信
- ネットワークプレイヤーの塗りを受信して適用

**実装方針**:
```csharp
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ネットワーク対応PaintBattleGameManager
/// ローカルプレイヤーの塗りをネットワークに送信
/// </summary>
public class NetworkPaintBattleGameManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PaintBattleGameManager localPaintManager;
    [SerializeField] private NetworkPaintCanvas networkPaintCanvas;
    
    void Update()
    {
        if (!IsOwner) return; // オーナーのみ実行
        
        // ローカルプレイヤーの塗りをネットワークに送信
        // PaintBattleGameManagerの塗り処理を監視し、ネットワークに送信
        // 実装はPaintBattleGameManagerのイベントを購読
    }
    
    // PaintBattleGameManagerのOnPaintCompletedイベントを購読
    void OnEnable()
    {
        if (PaintCanvas.OnPaintCompleted != null)
        {
            PaintCanvas.OnPaintCompleted += OnLocalPaintCompleted;
        }
    }
    
    void OnDisable()
    {
        if (PaintCanvas.OnPaintCompleted != null)
        {
            PaintCanvas.OnPaintCompleted -= OnLocalPaintCompleted;
        }
    }
    
    private void OnLocalPaintCompleted(Vector2 position, int playerId, float intensity)
    {
        // ローカルプレイヤーの塗りをネットワークに送信
        if (networkPaintCanvas != null && IsOwner)
        {
            Color playerColor = GetPlayerColor();
            networkPaintCanvas.SendPaintCommand(position, playerId, intensity, playerColor);
        }
    }
    
    private Color GetPlayerColor()
    {
        // BattleSettingsからプレイヤー色を取得
        if (BattleSettings.Instance != null && BattleSettings.Instance.Current != null)
        {
            return BattleSettings.Instance.Current.playerColor;
        }
        return Color.blue; // デフォルト値
    }
}
```

---

### Step 4.5: マッチメイキングシステム

**ファイル**: `Assets/Main/Script/Network/MatchmakingSystem.cs`

**実装内容**:
- プレイヤーのマッチング
- ルーム作成・参加
- ゲーム開始の同期

**実装方針**:
```csharp
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// マッチメイキングシステム
/// プレイヤーをマッチングし、ゲームを開始
/// </summary>
public class MatchmakingSystem : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxPlayers = 2; // ColorDefenseは2人対戦
    
    public static MatchmakingSystem Instance { get; private set; }
    
    // マッチメイキングイベント
    public static event System.Action OnMatchFound;
    public static event System.Action OnMatchCancelled;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // ホストとしてゲームを開始
    public void StartHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("MatchmakingSystem: ホストとしてゲームを開始");
            OnMatchFound?.Invoke();
        }
    }
    
    // クライアントとしてゲームに参加
    public void JoinGame(string ipAddress = "localhost")
    {
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log($"MatchmakingSystem: ゲームに参加 - {ipAddress}");
        }
    }
    
    // ゲームを終了
    public void LeaveGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        OnMatchCancelled?.Invoke();
    }
    
    // プレイヤー数が揃ったらゲーム開始
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // プレイヤー数が揃ったらゲーム開始
            CheckAndStartGame();
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"MatchmakingSystem: クライアント接続 - {clientId}");
        CheckAndStartGame();
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"MatchmakingSystem: クライアント切断 - {clientId}");
        // ゲーム終了処理
    }
    
    private void CheckAndStartGame()
    {
        if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= maxPlayers)
        {
            // ゲーム開始を全クライアントに通知
            StartGameClientRpc();
        }
    }
    
    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("MatchmakingSystem: ゲーム開始");
        // ゲーム開始処理
        if (NetworkColorDefenseMode.Instance != null)
        {
            NetworkColorDefenseMode.Instance.StartGame();
        }
    }
}
```

---

### Step 4.6: ColorDefenseModeのCPU無効化

**ファイル**: `Assets/Main/Script/SinglePlayer/Modes/ColorDefenseMode.cs`（最小限の修正）

**変更内容**:
- `GameModeManager`からオンラインモードかどうかを取得
- オンラインモード時は`EnemyPainter`を無効化
- **重要**: 既存のシングルプレイ用スクリプトは最小限の変更のみ
- **重要**: オンライン/オフラインで**同じシーン（01_Gameplay）**を使用するため、コンポーネントの有効/無効で切り替え

**実装方針**:
```csharp
// ColorDefenseMode.cs に追加（最小限の変更）

public void StartGame()
{
    // 既存の処理...
    
    // GameModeManagerからオンラインモードかどうかを取得
    bool isOnlineMode = GameModeManager.Instance != null && GameModeManager.Instance.IsOnlineMode;
    
    // オンラインモード時はCPU（EnemyPainter）を無効化
    if (isOnlineMode)
    {
        // EnemyPainterを初期化しない
        Debug.Log("ColorDefenseMode: オンラインモードのため、CPU（EnemyPainter）を無効化します");
        // enemyPainters.Clear()は既に実行されているので、追加処理不要
    }
    else
    {
        // オフラインモード（シングルプレイ）: 従来通りCPUを初期化
        if (settings != null && settings.enemyPaintMode == EnemyPaintMode.GlobalPainters)
        {
            InitializeEnemyPainters();
        }
    }
}

public void UpdateGame(float deltaTime)
{
    // 既存の処理...
    
    // GameModeManagerからオンラインモードかどうかを取得
    bool isOnlineMode = GameModeManager.Instance != null && GameModeManager.Instance.IsOnlineMode;
    
    // オフラインモード時のみCPUの更新を実行
    if (!isOnlineMode && paintCanvas != null && settings.enemyPaintMode == EnemyPaintMode.GlobalPainters && enemyPainters.Count > 0)
    {
        for (int i = 0; i < enemyPainters.Count; i++)
        {
            enemyPainters[i]?.Update(deltaTime);
        }
    }
    // オンラインモード時は、ネットワークプレイヤーの塗りがNetworkPaintCanvas経由で処理される
}
```

**注意点**:
- `GameModeManager`を使用することで、既存のシングルプレイ用スクリプトへの影響を最小限に
- `ColorDefenseMode`の既存の機能は全て保持
- オンラインモード時のみCPUを無効化する条件分岐を追加

---

### Step 4.7: UI実装（マッチメイキング画面）

**ファイル**: `Assets/Main/Script/UI/OnlineMatchmakingPanel.cs`

**実装内容**:
- ホスト/クライアント選択
- IPアドレス入力
- 接続状態表示
- **重要**: `GameModeSelectionPanel`から呼び出される（既存のフローを維持）

**実装方針**:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// オンラインマッチメイキングUI
/// GameModeSelectionPanelから呼び出される
/// </summary>
public class OnlineMatchmakingPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject matchmakingPanel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField ipAddressInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button backButton;
    
    [Header("References")]
    [SerializeField] private MatchmakingSystem matchmakingSystem;
    [SerializeField] private GameModeSelectionPanel gameModeSelectionPanel;
    
    void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClicked);
        
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinClicked);
        
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
        
        if (ipAddressInput != null)
            ipAddressInput.text = "localhost"; // デフォルト値
        
        // イベント購読
        MatchmakingSystem.OnMatchFound += OnMatchFound;
        MatchmakingSystem.OnMatchCancelled += OnMatchCancelled;
        
        // 初期状態では非表示
        Hide();
    }
    
    void OnDestroy()
    {
        MatchmakingSystem.OnMatchFound -= OnMatchFound;
        MatchmakingSystem.OnMatchCancelled -= OnMatchCancelled;
    }
    
    /// <summary>
    /// マッチメイキング画面を表示
    /// </summary>
    public void Show()
    {
        if (matchmakingPanel != null)
        {
            matchmakingPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// マッチメイキング画面を非表示
    /// </summary>
    public void Hide()
    {
        if (matchmakingPanel != null)
        {
            matchmakingPanel.SetActive(false);
        }
    }
    
    private void OnHostClicked()
    {
        if (matchmakingSystem != null)
        {
            matchmakingSystem.StartHost();
            UpdateStatus("ホストとしてゲームを開始中...");
        }
    }
    
    private void OnJoinClicked()
    {
        if (matchmakingSystem != null)
        {
            string ipAddress = ipAddressInput != null ? ipAddressInput.text : "localhost";
            matchmakingSystem.JoinGame(ipAddress);
            UpdateStatus($"ゲームに参加中... ({ipAddress})");
        }
    }
    
    private void OnBackClicked()
    {
        Hide();
        if (gameModeSelectionPanel != null)
        {
            gameModeSelectionPanel.Show();
        }
    }
    
    private void OnMatchFound()
    {
        UpdateStatus("マッチング成功！ゲーム開始...");
        // マッチメイキング画面を非表示
        Hide();
    }
    
    private void OnMatchCancelled()
    {
        UpdateStatus("接続が切断されました");
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"OnlineMatchmakingPanel: {message}");
    }
}
```

### Step 4.8: GameModeSelectionPanelの拡張（オプション）

**ファイル**: `Assets/Main/Script/UI/GameModeSelectionPanel.cs`（最小限の変更）

**変更内容**:
- オンラインモード選択時は`OnlineMatchmakingPanel`を表示
- オフラインモード選択時は従来通りゲームを開始
- **重要**: 既存のシングルプレイ用の処理は変更しない

**実装方針**:
```csharp
// GameModeSelectionPanel.cs に追加（最小限の変更）

[Header("Online Matchmaking")]
[Tooltip("オンラインマッチメイキングパネル（オンラインモード時のみ使用）")]
[SerializeField] private OnlineMatchmakingPanel onlineMatchmakingPanel;

public void OnModeSelected(SinglePlayerGameModeType mode)
{
    Debug.Log($"GameModeSelectionPanel: モード選択 - {mode}");

    // GameModeManagerからオンラインモードかどうかを取得
    bool isOnlineMode = GameModeManager.Instance != null && GameModeManager.Instance.IsOnlineMode;
    
    // オンラインモードかつColorDefenseモードの場合
    if (isOnlineMode && mode == SinglePlayerGameModeType.ColorDefense && onlineMatchmakingPanel != null)
    {
        // マッチメイキング画面を表示
        Hide();
        onlineMatchmakingPanel.Show();
        return;
    }
    
    // 既存の処理（オフラインモードまたは他のモード）
    // アニメーションが設定されている場合はフェードアウトを開始
    if (fadeAnimator != null && !string.IsNullOrEmpty(fadeOutTriggerName))
    {
        fadeAnimator.SetTrigger(fadeOutTriggerName);
        StartCoroutine(TransitionToNextScreen(mode));
    }
    else
    {
        // 既存の処理...
        if (mode == SinglePlayerGameModeType.ColorDefense && colorDefenseLobbyPanel != null)
        {
            Hide();
            colorDefenseLobbyPanel.Open();
        }
        else
        {
            Hide();
            StartGame(mode);
        }
    }
}
```

**注意点**:
- 既存のシングルプレイ用の処理は全て保持
- オンラインモード時のみ追加処理を実行
- `GameModeManager`を使用することで、既存コードへの影響を最小限に

---

## 🔄 データフロー

### シーン構成（重要）
- **全機能統合**: `GameScene`シーンを使用（メニュー、設定、ゲームプレイ全て）
- **モード切り替え**: `GameModeManager`から状態を取得し、コンポーネントを有効/無効化
- **画面切り替え**: UIパネルの表示/非表示で切り替え（シーン遷移不要）

### シングルプレイモード（オフライン）
```
シーン: GameScene（1つのシーン）
├─ UI: メニューパネル非表示、ゲームプレイパネル表示
├─ プレイヤー → PaintBattleGameManager → PaintCanvas (playerId=1)
└─ CPU → EnemyPainter → PaintCanvas (playerId=-1)
    （EnemyPainter: 有効、NetworkManager: 無効）
```

### オンラインモード
```
シーン: GameScene（同じシーン）
├─ UI: メニューパネル非表示、ゲームプレイパネル表示
├─ ローカルプレイヤー → PaintBattleGameManager → NetworkPaintCanvas → ネットワーク → リモートクライアント
└─ リモートプレイヤー → ネットワーク → NetworkPaintCanvas → PaintCanvas (playerId=-1)
    （EnemyPainter: 無効、NetworkManager: 有効）
```

**重要なポイント**:
- **1つのシーン（GameScene）**で全てを管理
- UIパネルの表示/非表示でメニューとゲームプレイを切り替え
- `GameModeManager`から状態を取得し、コンポーネントを有効/無効化
- `PaintCanvas`、`ColorDefenseMode`等は全モード共通で使用

---

## ⚠️ 注意点と最適化

### 1. 塗りデータの送信頻度
- **問題**: 塗りコマンドは毎フレーム送信される可能性がある
- **解決策**: 
  - 送信頻度を制限（例: 30fps）
  - バッチ送信（複数の塗りコマンドをまとめて送信）
  - 差分送信（変更された領域のみ送信）

### 2. ネットワーク遅延の補正
- **問題**: ネットワーク遅延により、塗りが遅れて表示される
- **解決策**: 
  - クライアント側予測（ローカルで先に塗りを表示）
  - サーバー側の権威（サーバーが最終的な状態を決定）

### 3. 同期の確実性
- **問題**: 塗りデータが失われる可能性
- **解決策**: 
  - 信頼性のある送信（TCPまたはReliable RPC）
  - 定期的な状態同期（全塗りデータの再送）

### 4. パフォーマンス
- **問題**: ネットワーク送信が重い
- **解決策**: 
  - 圧縮（塗りデータの圧縮）
  - 優先度付け（重要な塗りを優先送信）

---

## 📝 実装チェックリスト

### Step 4.0: UIフロー実装（実装済み）
- [x] OnlineOfflineSelectionPanelクラスを実装
- [x] GameModeManagerクラスを実装
- [x] TitlePanelの修正（セーブデータがない場合: SettingsPanel、ある場合: OnlineOfflineSelectionPanel）
- [x] SettingsPanelの修正（次へボタンでOnlineOfflineSelectionPanelに遷移）
- [ ] UnityエディタでUIパネルを設定

### Step 4.1: ネットワークフレームワーク
- [ ] Unity Netcode for GameObjectsをインストール
- [ ] NetworkManagerをGameSceneに配置
- [ ] ネットワーク設定を構成
- [ ] シーン構成の確認（GameScene: 1つのシーンで全てを管理）
- [ ] GameplayManagerの実装（UIパネル切り替え、コンポーネント有効/無効化）

### Step 4.2: NetworkPaintCanvas
- [ ] NetworkPaintCanvasクラスを実装
- [ ] 塗りコマンドの送信機能
- [ ] 塗りコマンドの受信機能
- [ ] 初期状態の同期

### Step 4.3: NetworkColorDefenseMode
- [ ] NetworkColorDefenseModeクラスを実装
- [ ] プレイヤーIDの割り当て
- [ ] ゲーム状態の同期
- [ ] GameModeManagerからオンラインモードを取得

### Step 4.4: NetworkPaintBattleGameManager
- [ ] NetworkPaintBattleGameManagerクラスを実装
- [ ] ローカルプレイヤーの塗り送信
- [ ] ネットワークプレイヤーの塗り受信

### Step 4.5: マッチメイキング
- [ ] MatchmakingSystemクラスを実装
- [ ] ホスト/クライアント機能
- [ ] ゲーム開始の同期

### Step 4.6: ColorDefenseMode修正（最小限）
- [ ] GameModeManagerからオンラインモードを取得
- [ ] オンラインモード時のCPU無効化処理
- [ ] 既存のシングルプレイ機能の保持確認

### Step 4.7: UI実装
- [ ] OnlineMatchmakingPanelクラスを実装
- [ ] ホスト/クライアント選択UI
- [ ] 接続状態表示
- [ ] GameModeSelectionPanelからの呼び出し（オプション）

### Step 4.8: GameModeSelectionPanel拡張（オプション）
- [ ] オンラインモード時のOnlineMatchmakingPanel表示
- [ ] 既存のシングルプレイ処理の保持確認

### テスト
- [ ] UIフローの動作確認（タイトル → オフライン/オンライン選択 → ゲームセレクト）
- [ ] UIパネル切り替えの確認（メニュー ↔ ゲームプレイ、シーン遷移なし）
- [ ] オフラインモード時の既存機能の動作確認（1つのシーンで動作）
- [ ] オンラインモード時のコンポーネント有効/無効の確認
- [ ] ローカルネットワークでのテスト
- [ ] 2人対戦の動作確認
- [ ] ネットワーク切断時の処理
- [ ] パフォーマンステスト

---

## 🎮 使用方法

### ホストとして開始
1. ゲームを起動
2. オンラインモードを選択
3. "ホスト"ボタンをクリック
4. 他のプレイヤーの参加を待つ

### クライアントとして参加
1. ゲームを起動
2. オンラインモードを選択
3. ホストのIPアドレスを入力
4. "参加"ボタンをクリック

---

## 💰 費用まとめ

### 開発・運用費用（ColorDefense 2人対戦の場合）

| 項目 | 費用 | 備考 |
|------|------|------|
| **ネットワークフレームワーク** | **無料** | Unity Netcode for GameObjects |
| **サーバー（P2P方式）** | **無料** | プレイヤー間で直接接続 |
| **サーバー（専用サーバー方式）** | **月額 $5～$20** | AWS t2.micro等の小規模インスタンス |
| **マッチメイキングサーバー** | **月額 $10～$50** | オプション（将来的な拡張） |
| **合計（最小構成）** | **無料** | P2P方式を使用 |
| **合計（専用サーバー使用）** | **月額 $5～$20** | 小規模インスタンス |

### スケーラビリティ

**現在の設計（ColorDefense 2人対戦）**:
- 同時接続: **2人**
- 推奨構成: **P2P方式（無料）**
- サーバー費用: **不要**

**将来的な拡張（複数ルーム、ランキング等）**:
- 同時接続: **数十～数百人**
- 推奨構成: **専用サーバー + マッチメイキングサーバー**
- サーバー費用: **月額 $50～$200程度**

## 🔮 将来の拡張

### マッチメイキングサーバー
- 専用サーバーでのマッチメイキング
- ランキングシステム
- フレンド機能
- **費用**: 月額 $10～$50程度（小規模）

### リレーサーバー
- NAT越えのためのリレーサーバー
- より安定した接続
- **費用**: 月額 $5～$20程度（小規模）

### 観戦モード
- 他のプレイヤーの対戦を観戦
- リプレイ機能
- **費用**: サーバーストレージ費用のみ（月額 $1～$10程度）

---

## 📚 参考リソース

- [Unity Netcode for GameObjects ドキュメント](https://docs-multiplayer.unity3d.com/)
- [Unity Netcode サンプルプロジェクト](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects)
- [ネットワーク同期のベストプラクティス](https://docs-multiplayer.unity3d.com/netcode/current/learn/dealing-with-latency/)

