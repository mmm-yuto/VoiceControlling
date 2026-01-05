# Phase 4: ColorDefenseオンライン実装 - 3段階実装計画

## 実装方針

ColorDefenseモードのオンライン対戦機能を3段階に分けて実装します。各段階で動作確認を行いながら進めます。

---

## 第1段階: ネットワーク同期の基盤構築

**目標**: 塗りコマンドをネットワーク経由で送受信できるようにする

### 実装タスク

#### 1-1: NetworkPaintCanvasの実装
**ファイル**: `Assets/Main/Script/Network/NetworkPaintCanvas.cs`

**実装内容**:
- `NetworkBehaviour`を継承
- 塗りコマンドの送信（ServerRpc）
- 塗りコマンドの受信（ClientRpc）
- `PaintCanvas`への参照を持ち、ネットワーク経由で塗りを同期
- 送信頻度の制限（パフォーマンス対策）

**主要機能**:
- `SendPaintCommand()`: ローカルプレイヤーの塗りをネットワークに送信
- `ApplyPaintCommandClientRpc()`: 他のプレイヤーの塗りを受信して適用

**参考**: `Cursor/Implementation/Phase4/Implementation_online.md` の Step 4.2

#### 1-2: NetworkPaintBattleGameManagerの実装
**ファイル**: `Assets/Main/Script/Network/NetworkPaintBattleGameManager.cs`

**実装内容**:
- `NetworkBehaviour`を継承
- `PaintBattleGameManager`の塗りイベント（`PaintCanvas.OnPaintCompleted`）を購読
- ローカルプレイヤーの塗りを`NetworkPaintCanvas`経由でネットワークに送信
- オーナーのみ実行（`IsOwner`チェック）

**主要機能**:
- `OnLocalPaintCompleted()`: ローカルプレイヤーの塗りイベントを処理
- `NetworkPaintCanvas.SendPaintCommand()`を呼び出して送信

**参考**: `Cursor/Implementation/Phase4/Implementation_online.md` の Step 4.4

### テスト方法

1. 2つのUnityエディタを起動（同じプロジェクト）
2. 両方でNetworkManagerを起動（1つはホスト、1つはクライアント）
3. ホスト側で塗り操作を行い、クライアント側に反映されることを確認
4. クライアント側で塗り操作を行い、ホスト側に反映されることを確認

### 完了条件

- [ ] NetworkPaintCanvasが正しく動作する
- [ ] NetworkPaintBattleGameManagerがローカル塗りを送信できる
- [ ] リモートプレイヤーの塗りが正しく受信・表示される
- [ ] 送信頻度が適切に制限されている（パフォーマンス確認）

---

## 第2段階: 接続・マッチメイキング機能

**目標**: プレイヤー同士を接続し、ゲームを開始できるようにする

### 実装タスク

#### 2-1: MatchmakingSystemの実装
**ファイル**: `Assets/Main/Script/Network/MatchmakingSystem.cs`

**実装内容**:
- ホスト/クライアント機能
- プレイヤーのマッチング（2人対戦）
- ゲーム開始の同期（2人揃ったら開始）
- 接続/切断の処理
- シングルトンパターンで実装

**主要機能**:
- `StartHost()`: ホストとしてゲームを開始
- `JoinGame(string ipAddress)`: クライアントとしてゲームに参加
- `LeaveGame()`: ゲームを終了
- `CheckAndStartGame()`: プレイヤー数が揃ったらゲーム開始

**参考**: `Cursor/Implementation/Phase4/Implementation_online.md` の Step 4.5

#### 2-2: OnlineMatchmakingPanelの実装
**ファイル**: `Assets/Main/Script/UI/OnlineMatchmakingPanel.cs`

**実装内容**:
- ホスト/クライアント選択UI
- IPアドレス入力フィールド（TMP_InputField）
- 接続状態表示（TextMeshProUGUI）
- `MatchmakingSystem`と連携
- イベント購読（`OnMatchFound`、`OnMatchCancelled`）

**主要機能**:
- `Show()` / `Hide()`: パネルの表示/非表示
- `OnHostClicked()`: ホストボタンクリック時の処理
- `OnJoinClicked()`: 参加ボタンクリック時の処理
- `OnBackClicked()`: 戻るボタンクリック時の処理

**参考**: `Cursor/Implementation/Phase4/Implementation_online.md` の Step 4.7

#### 2-3: GameModeSelectionPanelの拡張
**ファイル**: `Assets/Main/Script/UI/GameModeSelectionPanel.cs`

**実装内容**:
- `OnlineMatchmakingPanel`への参照を追加
- `OnModeSelected()`でオンラインモードかつColorDefenseモード選択時は`OnlineMatchmakingPanel`を表示
- 既存のシングルプレイ処理は保持（後方互換性）

**変更点**:
- `GameModeManager`からオンラインモードを取得
- オンラインモード時のみ追加処理を実行

**参考**: `Cursor/Implementation/Phase4/Implementation_online.md` の Step 4.8

### テスト方法

1. 2つのUnityエディタを起動
2. 両方でオンラインモードを選択
3. ColorDefenseモードを選択
4. 1つ目で「ホスト」ボタンをクリック
5. 2つ目で「参加」ボタンをクリック（IPアドレスを入力）
6. 接続が成功し、ゲームが開始されることを確認

### 完了条件

- [ ] MatchmakingSystemが正しく動作する
- [ ] OnlineMatchmakingPanelが表示される
- [ ] ホスト/クライアント接続が成功する
- [ ] 2人揃ったらゲームが開始される
- [ ] GameModeSelectionPanelからOnlineMatchmakingPanelに遷移できる

---

## 第3段階: ゲーム状態同期・統合

**目標**: ゲーム状態を同期し、オンライン/オフラインを統合する

### 実装タスク

#### 3-1: NetworkColorDefenseModeの実装
**ファイル**: `Assets/Main/Script/Network/NetworkColorDefenseMode.cs`

**実装内容**:
- `NetworkBehaviour`を継承
- `ColorDefenseMode`への参照を持ち、ゲーム状態を同期
- プレイヤーIDの割り当て（サーバー側）
- ゲーム開始/終了の同期
- `GameModeManager`からオンラインモードを取得し、オフライン時は無効化

**主要機能**:
- `OnNetworkSpawn()`: ネットワーク接続時の初期化
- `AssignPlayerIds()`: プレイヤーIDの割り当て（サーバー側）
- `StartGameServer()`: ゲーム開始の同期
- `OnNetworkPlayerPaint()`: ネットワークプレイヤーの塗りを処理

**参考**: `Cursor/Implementation/Phase4/Implementation_online.md` の Step 4.3

#### 3-2: ColorDefenseModeの修正（最小限）
**ファイル**: `Assets/Main/Script/SinglePlayer/Modes/ColorDefenseMode.cs`

**実装内容**:
- `StartGame()`で`GameModeManager`からオンラインモードを取得
- オンラインモード時は`EnemyPainter`を初期化しない
- `UpdateGame()`でオフラインモード時のみCPUの更新を実行
- 既存のシングルプレイ機能は保持

**変更点**:
```csharp
// StartGame()に追加
bool isOnlineMode = GameModeManager.Instance != null && GameModeManager.Instance.IsOnlineMode;
if (isOnlineMode)
{
    // EnemyPainterを初期化しない
    Debug.Log("ColorDefenseMode: オンラインモードのため、CPU（EnemyPainter）を無効化します");
}
else
{
    // 既存の処理（EnemyPainterを初期化）
}

// UpdateGame()に追加
bool isOnlineMode = GameModeManager.Instance != null && GameModeManager.Instance.IsOnlineMode;
if (!isOnlineMode && settings.enemyPaintMode == EnemyPaintMode.GlobalPainters && enemyPainters.Count > 0)
{
    // 既存のCPU更新処理
}
```

**参考**: `Cursor/Implementation/Phase4/Implementation_online.md` の Step 4.6

#### 3-3: GameplayManagerの実装（オプション）
**ファイル**: `Assets/Main/Script/GameplayManager.cs`

**実装内容**:
- UIパネルの切り替え（メニュー ↔ ゲームプレイ）
- モードに応じてコンポーネントを有効/無効化
- `GameModeManager`から状態を取得
- オンラインモード時: NetworkManager有効、EnemyPainter無効
- オフラインモード時: NetworkManager無効、EnemyPainter有効

**主要機能**:
- `StartGame()`: ゲーム開始時の処理
- `ShowGameplayPanel()`: ゲームプレイUIを表示
- `ReturnToMenu()`: メニューに戻る
- `InitializeOnline()` / `InitializeOffline()`: モードに応じた初期化

**参考**: `Cursor/Implementation/Phase4/Implementation_online.md` の「シーン構成」セクション

### テスト方法

1. **オフラインモードの動作確認**
   - オフラインモードでColorDefenseを開始
   - CPU（EnemyPainter）が動作することを確認
   - 既存の機能が正常に動作することを確認

2. **オンラインモードの動作確認**
   - オンラインモードでColorDefenseを開始
   - 2人接続してゲームを開始
   - ゲーム状態（時間、スコアなど）が同期されることを確認
   - CPUが動作しないことを確認
   - 両プレイヤーの塗りが正しく同期されることを確認

3. **統合テスト**
   - オフライン/オンラインの切り替えが正常に動作することを確認
   - UIパネルの切り替えが正常に動作することを確認

### 完了条件

- [ ] NetworkColorDefenseModeが正しく動作する
- [ ] ゲーム状態（開始、終了、スコアなど）が同期される
- [ ] ColorDefenseModeの修正が正しく動作する（オンライン時はCPU無効）
- [ ] オフラインモードの既存機能が正常に動作する
- [ ] GameplayManagerが正しく動作する（オプション）
- [ ] オンライン/オフラインの切り替えが正常に動作する

---

## 実装の進め方

1. **第1段階を完了**してから第2段階に進む
2. **各段階で動作確認**を行い、問題があれば修正してから次に進む
3. **第3段階で統合テスト**を行い、全体の動作を確認

## 注意点

- 各段階で既存機能への影響を確認する
- ネットワーク遅延やパフォーマンスに注意する
- エラーハンドリングを適切に実装する
- デバッグログを適切に出力する

