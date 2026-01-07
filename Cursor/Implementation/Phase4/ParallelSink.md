# ParrelSyncを用いたオンラインデバッグ方法

## 概要

ParrelSyncは、Unityでのマルチプレイヤーゲーム開発時に、**ビルドせずに複数のゲームインスタンスを同時に実行・デバッグできるツール**です。Phase 4のオンライン対戦実装において、ホストとクライアントを同時にデバッグする際に非常に有効です。

## ParrelSyncの特徴

- **ビルド不要**: エディタ上で直接マルチプレイヤーテストが可能
- **Assets共有**: 元のプロジェクトとAssetsフォルダを共有（シンボリックリンク）
- **自動同期**: コードやアセットの変更が自動的に反映
- **簡単セットアップ**: Unity Package Managerから簡単にインストール可能

---

## インストール手順

### 1. ParrelSyncのインストール

1. Unityのメニューから `Window > Package Manager` を開く
2. 左上の「+」ボタンをクリック
3. 「Add package from git URL...」を選択
4. 以下のURLを入力して「Add」をクリック:

```
https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync
```

5. インストールが完了すると、Unityのメニューに「ParrelSync」が追加されます

### 2. インストール確認

- `ParrelSync > Clones Manager` がメニューに表示されていれば成功です

---

## 基本的な使い方

### ステップ1: クローンプロジェクトの作成

1. Unityのメニューから `ParrelSync > Clones Manager` を選択
2. 「Clones Manager」ウィンドウが開きます
3. 「Create new clone」ボタンをクリック
4. クローン名を入力（例: `VoiceControlling_Clone`）
5. クローンが作成されます（数秒かかることがあります）

**注意**: クローンは元のプロジェクトと同じフォルダ内に作成されます

### ステップ2: クローンプロジェクトの起動

1. 「Clones Manager」ウィンドウで、作成したクローンの「Open in New Editor」ボタンをクリック
2. 新しいUnityエディタでクローンプロジェクトが開きます
3. これで、**元のプロジェクトとクローンプロジェクトの2つのエディタが同時に起動**します

### ステップ3: オンラインデバッグの準備

#### エディタ1（元のプロジェクト - ホスト側）
- 通常通りUnityエディタでプロジェクトを開いた状態
- このエディタでホストとしてゲームを起動

#### エディタ2（クローンプロジェクト - クライアント側）
- ParrelSyncで作成したクローンプロジェクトを開いた状態
- このエディタでクライアントとしてゲームを起動

---

## Phase 4オンライン対戦での実践的な使い方

### ワークフロー

#### 1. セットアップ

```
1. ParrelSync > Clones Manager を開く
2. 「Create new clone」でクローンを作成（例: VoiceControlling_Clone）
3. 「Open in New Editor」でクローンを起動
4. 元のエディタとクローンエディタの2つが起動している状態
```

#### 2. ホスト側の設定（エディタ1 - 元のプロジェクト）

1. エディタ1でシーンを開く（例: `GameScene`）
2. `MatchmakingSystem` または `NetworkManager` を確認
3. ゲームを実行（Play ボタン）
4. 「ホスト」ボタンをクリックしてホストとして開始

#### 3. クライアント側の設定（エディタ2 - クローンプロジェクト）

1. エディタ2で同じシーンを開く（`GameScene`）
2. ゲームを実行（Play ボタン）
3. 「参加」ボタンをクリック
4. IPアドレスに `localhost` または `127.0.0.1` を入力して接続

#### 4. 同時デバッグ

- **Console**: 両方のエディタでConsoleウィンドウを開き、ログを同時に確認
- **Inspector**: 両方のエディタでNetworkManagerやPaintCanvasの状態を監視
- **Profiler**: 両方のエディタにProfilerを接続してパフォーマンスを比較
- **ブレークポイント**: 両方のエディタでブレークポイントを設定してステップ実行

---

## 便利な機能

### 1. クローンの削除

1. `ParrelSync > Clones Manager` を開く
2. 削除したいクローンの「Delete」ボタンをクリック
3. 確認ダイアログで「Yes」をクリック

**注意**: クローンを削除しても、元のプロジェクトには影響しません

### 2. 複数のクローン作成

- 複数のクローンを作成することで、3人以上のマルチプレイヤーテストも可能
- 例: ホスト + クライアント1 + クライアント2 の3人同時テスト

### 3. 自動同期

- 元のプロジェクトでコードやアセットを変更すると、クローンプロジェクトにも自動的に反映されます
- 両方のエディタで「Reimport All」を実行する必要はありません

---

## デバッグのコツ

### 1. ログの識別

両方のエディタでログが混在しないように、プレフィックスを追加:

```csharp
// ホスト側
Debug.Log($"[HOST] NetworkManager started");

// クライアント側
Debug.Log($"[CLIENT] Connecting to host...");
```

### 2. ネットワーク状態の可視化

デバッグ用のUIを追加して、ネットワーク状態を表示:

```csharp
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class NetworkDebugDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;
    
    void Update()
    {
        if (NetworkManager.Singleton != null)
        {
            string role = NetworkManager.Singleton.IsHost ? "[HOST]" : "[CLIENT]";
            string status = $"{role}\n";
            status += $"IsHost: {NetworkManager.Singleton.IsHost}\n";
            status += $"IsClient: {NetworkManager.Singleton.IsClient}\n";
            status += $"Connected Clients: {NetworkManager.Singleton.ConnectedClients.Count}\n";
            status += $"Local Client ID: {NetworkManager.Singleton.LocalClientId}\n";
            
            if (debugText != null)
            {
                debugText.text = status;
            }
        }
    }
}
```

### 3. 塗りデータ同期の確認

```csharp
using UnityEngine;
using Unity.Netcode;

public class PaintSyncDebugger : NetworkBehaviour
{
    [SerializeField] private PaintCanvas paintCanvas;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        string role = IsOwner ? "[HOST]" : "[CLIENT]";
        Debug.Log($"{role} PaintSyncDebugger spawned - Client ID: {NetworkManager.Singleton.LocalClientId}");
    }
    
    // 塗りコマンド送信時のデバッグ
    public void OnPaintCommandSent(Vector2 position, int playerId, float intensity)
    {
        string role = NetworkManager.Singleton.IsHost ? "[HOST]" : "[CLIENT]";
        Debug.Log($"{role} Paint command sent - Position: {position}, PlayerID: {playerId}, Intensity: {intensity}");
    }
    
    // 塗りコマンド受信時のデバッグ
    public void OnPaintCommandReceived(Vector2 position, int playerId, float intensity)
    {
        string role = NetworkManager.Singleton.IsHost ? "[HOST]" : "[CLIENT]";
        Debug.Log($"{role} Paint command received - Position: {position}, PlayerID: {playerId}, Intensity: {intensity}");
    }
}
```

---

## Unity Profilerとの併用

### 複数エディタの同時プロファイリング

1. **エディタ1（ホスト）**:
   - `Window > Analysis > Profiler` を開く
   - 「Active Profiler」でエディタ1を選択

2. **エディタ2（クライアント）**:
   - `Window > Analysis > Profiler` を開く
   - 「Active Profiler」でエディタ2を選択

3. **統合確認**:
   - 両方のProfilerウィンドウを並べて表示
   - FPS、メモリ使用量、ネットワーク通信量などを比較

---

## トラブルシューティング

### 問題1: クローンが作成できない

**原因**: 
- プロジェクトが開いている
- ディスク容量が不足している
- 権限の問題

**解決策**:
- Unityエディタを一度閉じてから再試行
- ディスク容量を確認
- 管理者権限でUnityを起動

### 問題2: クローンが起動しない

**原因**:
- クローンのパスに問題がある
- Unity Hubの設定の問題

**解決策**:
- Clones Managerでクローンを削除して再作成
- Unity Hubでプロジェクトを手動で開く（クローンのフォルダを指定）

### 問題3: ネットワーク接続ができない

**原因**:
- ファイアウォールの設定
- ポートの競合
- NetworkManagerの設定

**解決策**:
- ファイアウォールでUnityの通信を許可
- `localhost` または `127.0.0.1` を使用
- NetworkManagerの設定を確認（ポート番号など）

### 問題4: 変更が反映されない

**原因**:
- シンボリックリンクの問題
- キャッシュの問題

**解決策**:
- 両方のエディタで「Assets > Reimport All」を実行
- エディタを再起動

### 問題5: パフォーマンスが低下する

**原因**:
- 2つのエディタが同時に実行されているため、リソース使用量が増加

**解決策**:
- 必要最小限のシーンのみを開く
- 不要なアセットをアンロード
- メモリ使用量を監視（Profilerを使用）

---

## 実践例: Phase 4オンライン対戦のデバッグ

### シナリオ: 塗りデータの同期確認

#### 手順

1. **ParrelSyncでクローンを作成**
   ```
   ParrelSync > Clones Manager > Create new clone
   ```

2. **エディタ1（ホスト）でゲーム開始**
   - `GameScene` を開く
   - Play ボタンをクリック
   - 「ホスト」ボタンをクリック

3. **エディタ2（クライアント）で接続**
   - `GameScene` を開く
   - Play ボタンをクリック
   - 「参加」ボタンをクリック（IP: `localhost`）

4. **塗り操作のテスト**
   - エディタ1（ホスト）で塗り操作を実行
   - エディタ2（クライアント）で塗りが同期されることを確認
   - エディタ2（クライアント）で塗り操作を実行
   - エディタ1（ホスト）で塗りが同期されることを確認

5. **デバッグログの確認**
   - 両方のエディタのConsoleウィンドウを確認
   - ネットワーク同期のログを確認
   - 問題があれば、ブレークポイントを設定してステップ実行

---

## まとめ

ParrelSyncを用いたオンラインデバッグにより、以下のメリットが得られます:

1. **ビルド不要**: エディタ上で直接マルチプレイヤーテストが可能
2. **迅速な反復**: コード変更が即座に反映され、テストサイクルが短縮
3. **同時デバッグ**: ホストとクライアントを同時にデバッグできる
4. **効率的な開発**: オンライン対戦のデバッグ時間を大幅に短縮

Phase 4のオンライン対戦実装において、ParrelSyncを活用することで、ネットワーク同期の問題を迅速に特定・解決できます。

---

## 参考リンク

- [ParrelSync GitHub リポジトリ](https://github.com/VeriorPies/ParrelSync)
- [ParrelSyncを使ってUnityで簡単に複数インスタンス起動できました（マルチプレイヤーテストに便利）](https://dev.classmethod.jp/articles/trying_out_parrel_sync_for_unity/)
- [ParrelSyncを使ってUnityのネットワークマルチゲーム開発を倍速で進めよう](https://soysoftware.sakura.ne.jp/archives/1741)
