# 現在のネットワークデータ送信方式

## 概要

現在の実装では、**差分同期方式**を使用してネットワーク経由で塗り情報を同期しています。

## NGOのServer Authoritativeパターンへの適合性

### ✅ 確認結果：すべてのステップを正しく実装しています

Unity Netcode for GameObjects (NGO) の**Server Authoritative（サーバー権限）**パターンに従って、クライアント側の変更をサーバーに反映させるために必要な3つのステップをすべて実装しています。

#### ✅ ステップ1: ServerRpcを使って「依頼」を出す

**実装状況**: **完全に実装済み** ✅

**実装箇所**: `NetworkPaintCanvas.cs`

```csharp
[ServerRpc(RequireOwnership = false)]  // ← [ServerRpc]属性が付いている
public void SendClientPaintServerRpc(  // ← メソッド名が「ServerRpc」で終わっている
    Vector2 position, 
    int playerId, 
    float intensity, 
    Color color, 
    float radius, 
    ServerRpcParams rpcParams = default
)
{
    // ここはサーバー側（ホスト）のPC上で実行される
    paintCanvas.PaintAtWithRadius(position, playerId, intensity, color, radius);
}
```

**呼び出し箇所**: `NetworkPaintBattleGameManager.cs`

```csharp
// クライアント側で実行
private void OnLocalPaintCompleted(Vector2 position, int playerId, float intensity)
{
    // 直接書き換えず、サーバー側のメソッドを呼ぶ
    networkPaintCanvas.SendClientPaintServerRpc(position, playerId, intensity, playerColor, brushRadius);
}
```

**確認ポイント**:
- ✅ `[ServerRpc]`属性が付いている
- ✅ `RequireOwnership = false`により、`IsOwner`が`false`でも送信可能
- ✅ メソッド名が`SendClientPaintServerRpc`で`ServerRpc`で終わっている
- ✅ クライアント側から正しく呼び出されている

#### ✅ ステップ2: NetworkVariableの権限（該当なし）

**実装状況**: **NetworkVariableは使用していない** ✅

現在の実装では、`NetworkVariable`を使用していません。代わりに、以下の方式で同期しています：

- **配列でデータ管理**: `Color[,]`, `int[,]`, `float[,]`でローカルに管理
- **ServerRpc + ClientRpc**: ネットワーク同期はRPCで実現
- **差分同期**: 変更されたピクセルのみを送信

**理由**:
1. **データサイズの問題**: キャンバス全体（例: 1920x1080 = 2,073,600ピクセル）を`NetworkVariable`で管理するには大きすぎる
2. **パフォーマンス**: 毎フレーム全ピクセルを同期するのは非効率的
3. **差分同期の利点**: 変更されたピクセルのみを送信する方が効率的

**結論**: `NetworkVariable`は使用していませんが、**これは設計上の意図**であり、問題ではありません。

#### ✅ ステップ3: 所有権（Ownership）の確認

**実装状況**: **正しく対処済み** ✅

**問題点**:
- `NetworkPaintBattleGameManager`はシーンオブジェクトのため、`IsOwner`が常に`false`（サーバーが所有者）になる

**解決方法**:
1. **`RequireOwnership = false`を使用**: `[ServerRpc(RequireOwnership = false)]`により、`IsOwner`が`false`でもServerRpcを送信可能
2. **`IsClient`チェック**: `IsOwner`チェックの代わりに`IsClient`チェックを使用

**実装箇所**: `NetworkPaintBattleGameManager.cs`

```csharp
// 修正前（問題あり）
if (!IsOwner)  // シーンオブジェクトでは常にfalseになる
{
    return;
}

// 修正後（正しい実装）
if (!IsClient)  // クライアント側でも実行される
{
    return;
}

// ServerRpcは RequireOwnership = false により、IsOwnerがfalseでも送信可能
networkPaintCanvas.SendClientPaintServerRpc(position, playerId, intensity, playerColor, brushRadius);
```

**確認ポイント**:
- ✅ `[ServerRpc(RequireOwnership = false)]`により、`IsOwner`が`false`でも送信可能
- ✅ `IsClient`チェックでクライアント側での実行を確認
- ✅ シーンオブジェクトでも正常に動作

### チェックリスト

| 項目 | 状況 | 確認結果 |
|------|------|----------|
| **メソッドに`[ServerRpc]`を付けているか？** | ✅ | `[ServerRpc(RequireOwnership = false)]`付き |
| **メソッド名の末尾が`ServerRpc`で終わっているか？** | ✅ | `SendClientPaintServerRpc` |
| **そのオブジェクトの`NetworkObject`コンポーネントは付いているか？** | ✅ | `NetworkPaintCanvas`に付いている |
| **`IsOwner`（または`RequireOwnership = false`）を確認しているか？** | ✅ | `RequireOwnership = false`により、`IsOwner`が`false`でも送信可能 |

### 結論

**現在の実装は、NGOのServer Authoritativeパターンの3つのステップをすべて正しく実装しています。**

- ✅ ステップ1: ServerRpcを使用してサーバーに依頼を送信
- ✅ ステップ2: NetworkVariableは使用していない（設計上の意図）
- ✅ ステップ3: `RequireOwnership = false`により、シーンオブジェクトでも正常に動作

---

## アーキテクチャ

### コンポーネント構成

1. **`PaintCanvas`**: ローカルの塗りキャンバス（テクスチャ、色データ、プレイヤーIDを管理）
2. **`NetworkPaintBattleGameManager`**: ローカルプレイヤーの塗りイベントを検知してネットワークに送信
3. **`NetworkPaintCanvas`**: サーバー側で差分を検出して全クライアントに送信

## データフロー

### 1. ローカルプレイヤーの塗り → サーバー

```
[ローカルプレイヤーが塗る]
    ↓
PaintCanvas.PaintAt() 実行
    ↓
PaintCanvas.OnPaintCompleted イベント発火
    ↓
NetworkPaintBattleGameManager.OnLocalPaintCompleted()
    ↓
NetworkPaintCanvas.SendClientPaintServerRpc() [ServerRpc]
    ↓
[サーバー側]
NetworkPaintCanvas.SendClientPaintServerRpc()
    ↓
PaintCanvas.PaintAtWithRadius() 実行（サーバー側にも塗りを適用）
```

**送信データ（ServerRpc）**:
- `Vector2 position`: 画面座標
- `int playerId`: プレイヤーID（1=ホスト, 2以降=クライアント）
- `float intensity`: 塗り強度
- `Color color`: 塗り色
- `float radius`: ブラシの半径

### 2. サーバー → 全クライアント（差分同期）

```
[サーバー側]
PaintDiffManager.DetectChanges() で差分を検出（0.2秒ごと）
    ↓
変更されたピクセルを収集
    ↓
PaintDiffData にパック
    ↓
NetworkPaintCanvas.ApplyPaintDiffClientRpc() [ClientRpc]
    ↓
[全クライアント]
ApplyPaintDiffClientRpc() 受信
    ↓
PaintCanvas.PaintAtWithTimestamp() で各ピクセルを適用
```

**送信データ（ClientRpc - 差分）**:
```csharp
struct PaintDiffData {
    int pixelCount;              // 変更されたピクセル数
    int[] xCoords;              // X座標配列
    int[] yCoords;              // Y座標配列
    Color[] colors;             // 色配列
    int[] playerIds;            // プレイヤーID配列
    float[] timestamps;         // タイムスタンプ配列
}
```

### 3. 初回同期（ゲーム開始時）

```
[サーバー側]
OnNetworkSpawn() 実行
    ↓
SendInitialSnapshotDelayed() コルーチン（0.5秒待機）
    ↓
SendInitialSnapshot() 実行
    ↓
塗られた全ピクセルを収集
    ↓
maxPixelsPerMessage (30000ピクセル) ごとにチャンク分割
    ↓
SendSnapshotChunkClientRpc() [ClientRpc] で各チャンクを送信
    ↓
[クライアント側]
全チャンクを受信してバッファに保存
    ↓
全チャンクが揃ったら ApplySnapshot() で一括適用
```

**送信データ（ClientRpc - 初回同期）**:
- チャンク形式で送信（1チャンクあたり最大30000ピクセル）
- 各チャンクに `xCoords`, `yCoords`, `colors`, `playerIds`, `timestamps` を含む

## データ送信の詳細

### ServerRpc: クライアント → サーバー

**メソッド**: `NetworkPaintCanvas.SendClientPaintServerRpc()`

```csharp
[ServerRpc(RequireOwnership = false)]
public void SendClientPaintServerRpc(
    Vector2 position,      // 画面座標
    int playerId,          // プレイヤーID
    float intensity,       // 塗り強度
    Color color,           // 塗り色
    float radius,          // ブラシ半径
    ServerRpcParams rpcParams = default
)
```

**特徴**:
- `RequireOwnership = false`: どのクライアントからでも呼び出し可能
- サーバー側で`PaintCanvas.PaintAtWithRadius()`を実行して塗りを適用
- これにより、サーバー側の差分検出が変更を検出できる

### ClientRpc: サーバー → 全クライアント

**差分同期**:
```csharp
[ClientRpc]
private void ApplyPaintDiffClientRpc(
    PaintDiffData diffData,
    ClientRpcParams rpcParams = default
)
```

**初回同期**:
```csharp
[ClientRpc]
private void SendSnapshotChunkClientRpc(
    int width, int height,
    int chunkIndex, int totalChunks,
    int[] xCoords, int[] yCoords,
    Color[] colors, int[] playerIds, float[] timestamps,
    ClientRpcParams rpcParams = default
)
```

## 差分検出の仕組み

### PaintDiffManager

- 前フレームの状態と現在の状態を比較
- 変更されたピクセル（色、プレイヤーID、タイムスタンプが変更）を検出
- サーバー側の`Update()`で0.2秒ごとに実行

**差分検出の条件**:
- 色が変更された
- プレイヤーIDが変更された
- タイムスタンプが更新された

## タイムスタンプによる競合解決

### 仕組み

1. サーバー側でタイムスタンプを管理
2. 各ピクセルにタイムスタンプを付与
3. クライアント側で`PaintAtWithTimestamp()`を使用して適用
4. 古いタイムスタンプの塗りは上書きされない（新しいもののみ適用）

**実装**:
- `PaintCanvas.SetTimestampCallback()` でサーバー時刻取得関数を設定
- `PaintCanvas.PaintAtWithTimestamp()` でタイムスタンプを比較して適用

## メッセージサイズ制限への対応

### 分割送信

- **最大ピクセル数**: `maxPixelsPerMessage = 30000`（約480KB）
- これを超える場合は自動的にチャンク分割
- 各チャンクを個別に`ApplyPaintDiffClientRpc()`で送信

### 初回同期での分割

- 全ピクセルデータを1回で送信できないため、必ずチャンク分割
- クライアント側で全チャンクを受信するまでバッファに保存
- 全チャンクが揃ったら一括適用

## プレイヤーIDの設定

### ホスト（サーバー）
- `playerId = 1` に設定

### クライアント
- `playerId = LocalClientId + 1` に設定
- 例: LocalClientId=1 → playerId=2

**設定タイミング**:
- `NetworkPaintBattleGameManager.OnNetworkSpawn()` で実行

## 同期のタイミング

### リアルタイム送信（ローカル → サーバー）
- 塗りが発生するたびに`SendClientPaintServerRpc()`を即座に送信

### 定期送信（サーバー → クライアント）
- `sendInterval = 0.2秒`ごとに差分を検出して送信
- `NetworkPaintCanvas.Update()`で実行

### 初回同期
- クライアント接続後0.5秒待機してから実行
- 接続確立を待つための遅延

## 現在の問題点

### 問題: クライアントが塗った色がホスト側に反映されない

**現状**:
- ✅ ホストが塗った色 → クライアント側に反映される（正常）
- ❌ クライアントが塗った色 → ホスト側に反映されない（問題）

### 調査結果サマリー

#### ✅ 確認済み（正常に動作している部分）

1. **インスタンス参照の一致**
   - `NetworkPaintCanvas.paintCanvas`のInstanceID: `60284`
   - `PaintBattleGameManager.paintCanvas`のInstanceID: `60284`
   - `PaintRenderer.paintCanvas`のInstanceID: `60284`
   - **結論**: すべて同じインスタンスを参照している ✅

2. **ServerRpcの実行**
   - クライアント側で`SendClientPaintServerRpc()`が正常に呼ばれている ✅
   - ホスト側（サーバー）で`SendClientPaintServerRpc()`が正常に実行されている ✅
   - `PlayerId: 2`が正しく送信されている ✅

3. **データフローの確認**
   - クライアント側: `OnLocalPaintCompleted()` → `SendClientPaintServerRpc()` ✅
   - サーバー側: `SendClientPaintServerRpc()` → `PaintAtWithRadius()`（確認中）

#### ❓ 確認中・未解決の部分

1. **`PaintAtWithRadius()`の実行確認**
   - サーバー側で`PaintAtWithRadiusInternal()`が実行されているかの確認が必要
   - ホスト側のログに`[DEBUG] PaintCanvas.PaintAtWithRadiusInternal`が出力されているか確認

2. **テクスチャ更新の確認**
   - サーバー側で塗りが適用された後、テクスチャが実際に更新されているか確認
   - `FlushTextureUpdates()`や`OnTextureUpdated`イベントが実行されているか確認

#### 🔧 修正済みの問題

1. **`IsOnlineMode()`の問題**
   - **問題**: `GameModeManager.Instance`が`null`の場合、常に`false`を返していた
   - **修正**: `NetworkManager.Singleton`でネットワーク状態を確認するように変更
   - **結果**: オンラインモードチェックが正常に動作するようになった ✅

2. **`IsOwner`チェックの問題**
   - **問題**: シーンオブジェクトでは`IsOwner`が常に`false`（サーバーが所有者）になるため、クライアント側で送信されなかった
   - **修正**: `IsOwner`チェックを`IsClient`チェックに変更
   - **結果**: クライアント側でも`SendClientPaintServerRpc()`が呼ばれるようになった ✅

3. **`OnNetworkSpawn()`での`playerId`設定**
   - **問題**: `IsOwner`チェックにより、クライアント側で`playerId`が設定されなかった
   - **修正**: `IsOwner`チェックを削除し、`IsClient`で判定するように変更
   - **結果**: クライアント側で`playerId = 2`が正しく設定されるようになった ✅

### 原因分析

#### 1. データフローの確認

**クライアントが塗った場合のフロー**:

```
[クライアント側]
1. クライアントが塗る
   ↓
2. PaintCanvas.PaintAt() 実行
   ↓
3. OnPaintCompleted イベント発火
   ↓
4. NetworkPaintBattleGameManager.OnLocalPaintCompleted() 実行
   - IsOwner = true の場合は送信
   ↓
5. SendClientPaintServerRpc() でサーバーに送信
   ↓
[サーバー側（ホスト）]
6. SendClientPaintServerRpc() が実行される
   ↓
7. PaintCanvas.PaintAtWithRadius() 実行
   - サーバー側のPaintCanvasに塗りを適用
   ↓
8. SendPaintDiff() で差分検出（0.2秒ごと）
   ↓
9. ApplyPaintDiffClientRpc() で全クライアントに送信
   - if (IsServer) return; でサーバー側では実行されない
```

#### 2. 問題の原因

**理論的な動作**:
- `SendClientPaintServerRpc()`でサーバー側の`PaintCanvas.PaintAtWithRadius()`が実行される
- サーバー側のPaintCanvasに塗りが適用されるはず

**実際の問題**:
- サーバー側で`PaintCanvas.PaintAtWithRadius()`が実行されても、ホスト側の画面に反映されない

**考えられる原因**:

1. **ホストとサーバーが別のPaintCanvasインスタンスを使っている可能性**
   - `NetworkPaintCanvas`が参照している`paintCanvas`と、ホスト側が表示に使っている`PaintCanvas`が異なる

2. **サーバー側でPaintCanvas.PaintAtWithRadius()が実行されても、テクスチャが更新されていない**
   - `PaintCanvas.PaintAtWithRadius()`は`FlushTextureUpdates()`を呼ぶが、実際のテクスチャ更新が適切に行われていない可能性

3. **ホスト側のPaintRendererが更新されていない**
   - サーバー側のPaintCanvasのテクスチャは更新されていても、ホスト側の`PaintRenderer`がそれを表示していない

4. **差分同期のタイミングの問題**
   - サーバー側で`PaintCanvas.PaintAtWithRadius()`が実行されるが、`ApplyPaintDiffClientRpc()`は`if (IsServer) return;`でサーバー側では実行されない
   - つまり、**サーバー側では差分同期による更新が行われない**

#### 3. 現在の状況（2024年1月調査時点）

**確認済みの動作**:
- ✅ クライアント側で`SendClientPaintServerRpc()`が呼ばれている
- ✅ サーバー側（ホスト）で`SendClientPaintServerRpc()`が実行されている
- ✅ `PlayerId: 2`が正しく送信されている
- ✅ `NetworkPaintCanvas.paintCanvas`と`PaintRenderer.paintCanvas`が同じインスタンス（InstanceID: 60284）

**未確認の部分**:
- ❓ サーバー側で`PaintAtWithRadius()`が実際に実行されているか
- ❓ `PaintAtWithRadiusInternal()`が呼ばれているか
- ❓ テクスチャが更新されているか（`FlushTextureUpdates()`の実行）
- ❓ `OnTextureUpdated`イベントが発火しているか

**次の確認ポイント**:
1. ホスト側のログで`[DEBUG] PaintCanvas.PaintAtWithRadiusInternal`が出力されているか確認
2. `[DEBUG] PaintCanvas: 初期化されていません`のログが出力されていないか確認
3. 更新頻度チェックや強度閾値でスキップされていないか確認

## 確認方法

### ステップ1: インスタンス参照の確認

**確認内容**: `NetworkPaintCanvas`とホスト側の表示用`PaintCanvas`が同じインスタンスか

**確認方法**:

1. **Unityエディタで確認**
   - `NetworkPaintCanvas`コンポーネントの`paintCanvas`フィールドを確認
   - ホスト側のシーンにある`PaintCanvas`のGameObjectと一致しているか確認

2. **コードで確認（一時的にログを追加）**
   ```csharp
   // NetworkPaintCanvas.cs の SendClientPaintServerRpc() 内
   Debug.LogWarning($"[DEBUG] NetworkPaintCanvas.paintCanvas: {paintCanvas.GetInstanceID()}");
   
   // PaintBattleGameManager.cs や PaintRenderer.cs から
   // ホスト側が使っているPaintCanvasのInstanceIDを出力
   ```

### ステップ2: `PaintCanvas.PaintAtWithRadius()`の実行確認

**確認内容**: サーバー側で`SendClientPaintServerRpc()`が実行された時、実際に`PaintCanvas.PaintAtWithRadius()`が呼ばれているか

**確認方法**:

1. **`NetworkPaintCanvas.SendClientPaintServerRpc()`にログを追加**
   ```csharp
   // NetworkPaintCanvas.cs 218行目の前
   Debug.LogWarning($"[DEBUG] SendClientPaintServerRpc - IsServer: {IsServer}, Position: {position}, PlayerId: {playerId}");
   paintCanvas.PaintAtWithRadius(position, playerId, intensity, color, radius);
   Debug.LogWarning($"[DEBUG] SendClientPaintServerRpc - PaintAtWithRadius() 実行完了");
   ```

2. **`PaintCanvas.PaintAtWithRadiusInternal()`の先頭にログを追加**
   ```csharp
   // PaintCanvas.cs 288行目の直後
   Debug.LogWarning($"[DEBUG] PaintAtWithRadiusInternal - IsServer: (NetworkPaintCanvasから呼ばれた), Position: {screenPosition}, PlayerId: {playerId}, checkUpdateFrequency: {checkUpdateFrequency}");
   ```

3. **更新頻度チェックと強度閾値の確認**
   ```csharp
   // PaintCanvas.cs 300行目付近（更新頻度チェックの後）
   if (frameCount % settings.updateFrequency != 0)
   {
       Debug.LogWarning($"[DEBUG] 更新頻度チェックでスキップ - frameCount: {frameCount}, updateFrequency: {settings.updateFrequency}");
       return;
   }
   
   // 316行目付近（強度閾値チェックの後）
   if (effectiveIntensity < settings.minVolumeThreshold)
   {
       Debug.LogWarning($"[DEBUG] 強度閾値でスキップ - effectiveIntensity: {effectiveIntensity}, minVolumeThreshold: {settings.minVolumeThreshold}");
       return;
   }
   ```

### ステップ3: テクスチャ更新の確認

**確認内容**: サーバー側で`PaintCanvas.PaintAtWithRadius()`が実行された後、テクスチャが実際に更新されているか

**確認方法**:

1. **`PaintCanvas.PaintAtWithRadiusInternal()`の最後にログを追加**
   ```csharp
   // PaintCanvas.cs 369行目付近（hasPainted判定の後）
   if (hasPainted)
   {
       Debug.LogWarning($"[DEBUG] 塗り処理完了 - hasPainted: true, PlayerId: {playerId}, 塗られたピクセル数: (計算)");
       FlushTextureUpdates();
       // ...
   }
   else
   {
       Debug.LogWarning($"[DEBUG] 塗り処理スキップ - hasPainted: false");
   }
   ```

2. **`PaintCanvas.FlushTextureUpdates()`の実行確認**
   ```csharp
   // PaintCanvas.cs の FlushTextureUpdates() 内
   Debug.LogWarning($"[DEBUG] FlushTextureUpdates() 実行");
   ```

3. **テクスチャの実際のピクセル値を確認**
   ```csharp
   // サーバー側で塗りが適用された後、特定のピクセルの色を確認
   // NetworkPaintCanvas.SendClientPaintServerRpc() の後
   int canvasX = Mathf.RoundToInt((position.x / Screen.width) * paintCanvas.GetSettings().textureWidth);
   int canvasY = Mathf.RoundToInt((position.y / Screen.height) * paintCanvas.GetSettings().textureHeight);
   Color pixelColor = paintCanvas.GetColorData()[canvasX, canvasY];
   Debug.LogWarning($"[DEBUG] サーバー側のPaintCanvas - ピクセル({canvasX}, {canvasY})の色: {pixelColor}");
   ```

### ステップ4: ホスト側の表示更新確認

**確認内容**: サーバー側の`PaintCanvas`のテクスチャが更新されても、ホスト側の`PaintRenderer`がそれを表示しているか

**確認方法**:

1. **`PaintRenderer`が参照している`PaintCanvas`の確認**
   - `PaintRenderer.paintCanvas`が、`NetworkPaintCanvas.paintCanvas`と同じインスタンスか確認

2. **`PaintRenderer.OnTextureUpdated()`の実行確認**
   ```csharp
   // PaintRenderer.cs の OnTextureUpdated() 内
   Debug.LogWarning($"[DEBUG] PaintRenderer.OnTextureUpdated() 実行 - paintCanvas.InstanceID: {paintCanvas.GetInstanceID()}");
   ```

3. **テクスチャのSprite更新確認**
   ```csharp
   // PaintRenderer.cs の OnTextureUpdated() 内
   Debug.LogWarning($"[DEBUG] Sprite更新前 - displayImage.sprite: {displayImage.sprite}");
   // Sprite作成後
   Debug.LogWarning($"[DEBUG] Sprite更新後 - displayImage.sprite: {displayImage.sprite}");
   ```

### ステップ5: 簡単なテスト方法

**最も簡単な確認方法**:

1. **一時的に強制的に塗りを適用**
   ```csharp
   // NetworkPaintCanvas.SendClientPaintServerRpc() の最後に追加
   // サーバー側で強制的にテクスチャを更新して表示を確認
   paintCanvas.FlushTextureUpdates();
   paintCanvas.OnTextureUpdated?.Invoke(); // イベントを手動で発火
   ```

2. **Inspectorで確認**
   - UnityエディタのInspectorで`PaintCanvas`のテクスチャを直接確認
   - クライアントが塗った後、サーバー側の`PaintCanvas`のテクスチャに変更があるか確認

## 注意事項

1. **敵（CPU）の塗りは送信しない**
   - `playerId <= 0` の場合は`OnLocalPaintCompleted()`で送信をスキップ

2. **サーバー側ではClientRpcを実行しない**
   - `ApplyPaintDiffClientRpc()` と `SendSnapshotChunkClientRpc()` で `if (IsServer) return;` チェック
   - **これが原因でホスト側で差分同期による更新が行われない可能性がある**

3. **オンラインモードチェック**
   - `onlyWorkInOnlineMode = true` の場合、オフラインモードでは送信しない

4. **ホスト側のイベント購読**
   - `NetworkPaintBattleGameManager.OnNetworkSpawn()`で`if (IsClient)`の時のみ`SubscribeToPaintEvents()`を実行
   - ホストも`IsClient = true`なので、ホスト側でもイベント購読されるはず

## 修正履歴

### 2024年1月の修正

#### 修正1: `IsOnlineMode()`の改善

**ファイル**: `NetworkPaintBattleGameManager.cs`

**変更内容**:
```csharp
// 修正前
private bool IsOnlineMode()
{
    if (GameModeManager.Instance != null)
    {
        return GameModeManager.Instance.IsOnlineMode;
    }
    return false;  // GameModeManagerがnullの場合、常にfalse
}

// 修正後
private bool IsOnlineMode()
{
    if (GameModeManager.Instance != null)
    {
        return GameModeManager.Instance.IsOnlineMode;
    }
    
    // GameModeManagerが存在しない場合でも、NetworkManagerが動作していればオンラインモードと判断
    if (NetworkManager.Singleton != null)
    {
        return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient;
    }
    
    return false;
}
```

**効果**: `GameModeManager.Instance`が`null`でも、ネットワークが動作している場合はオンラインモードと判断されるようになった。

#### 修正2: `IsOwner`チェックから`IsClient`チェックへの変更

**ファイル**: `NetworkPaintBattleGameManager.cs`

**変更内容**:
```csharp
// 修正前
if (!IsOwner)
{
    return;  // シーンオブジェクトでは常にfalseになる
}

// 修正後
if (!IsClient)
{
    return;  // クライアント側でも実行される
}
```

**効果**: シーンオブジェクトでもクライアント側で`SendClientPaintServerRpc()`が呼ばれるようになった。

#### 修正3: `OnNetworkSpawn()`での`playerId`設定の修正

**ファイル**: `NetworkPaintBattleGameManager.cs`

**変更内容**:
```csharp
// 修正前
if (localPaintManager != null && IsOwner)  // シーンオブジェクトでは常にfalse
{
    // playerIdが設定されない
}

// 修正後
if (localPaintManager != null)  // IsOwnerチェックを削除
{
    if (IsServer)
    {
        localPaintManager.playerId = 1;
    }
    else if (IsClient)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        localPaintManager.playerId = (int)localClientId + 1;
    }
}
```

**効果**: クライアント側でも`playerId`が正しく設定されるようになった（例: `playerId = 2`）。

## 現在の状態

### 動作確認済み

- ✅ インスタンス参照の一致（すべてInstanceID: 60284）
- ✅ ServerRpcの実行（クライアント→サーバー）
- ✅ `PlayerId: 2`の正しい送信
- ✅ `IsOnlineMode()`の正常動作
- ✅ クライアント側での`SendClientPaintServerRpc()`呼び出し

### 確認が必要

- ❓ サーバー側での`PaintAtWithRadius()`実行
- ❓ `PaintAtWithRadiusInternal()`の実行
- ❓ テクスチャ更新（`FlushTextureUpdates()`）
- ❓ `OnTextureUpdated`イベントの発火

### 次のステップ

ホスト側のログを確認し、`[DEBUG] PaintCanvas.PaintAtWithRadiusInternal`のログが出力されているかを確認してください。出力されていない場合、`PaintAtWithRadius()`が実行されていない、または初期化されていない可能性があります。

## Unityエディタ側の確認チェックリスト

コードには問題がないと判断した場合、Unityエディタ側の設定や構成を確認してください。

### ✅ チェック1: NetworkObjectコンポーネントの確認

**確認項目**:

1. **`NetworkPaintCanvas`のGameObject**
   - `NetworkPaintCanvas`コンポーネントがアタッチされているか ✅
   - **`NetworkObject`コンポーネントが同じGameObjectにアタッチされているか** ⚠️ **重要**
   - `NetworkObject`のInspectorで以下を確認：
     - `GlobalObjectIdHash`: 数値が表示されているか（例: `325207396`）
     - `NetworkManager`: 実行時に自動設定される（エディタでは`null`でも可）

2. **`NetworkPaintBattleGameManager`のGameObject**
   - `NetworkPaintBattleGameManager`コンポーネントがアタッチされているか ✅
   - **`NetworkObject`コンポーネントが同じGameObjectにアタッチされているか** ⚠️ **重要**

**確認方法**:
- Hierarchyで`NetworkPaintCanvas`のGameObjectを選択
- Inspectorで`NetworkObject`コンポーネントが表示されているか確認
- なければ、`Add Component` → `Network Object`で追加

### ✅ チェック2: Inspectorでの参照設定

**確認項目**:

1. **`NetworkPaintCanvas`のInspector**
   - `Paint Canvas`フィールドに`PaintCanvas`コンポーネントが設定されているか ✅
   - ドラッグ&ドロップで正しく接続されているか確認

2. **`NetworkPaintBattleGameManager`のInspector**
   - `Local Paint Manager`フィールドに`PaintBattleGameManager`コンポーネントが設定されているか
   - `Network Paint Canvas`フィールドに`NetworkPaintCanvas`コンポーネントが設定されているか ✅
   - 両方ともドラッグ&ドロップで正しく接続されているか確認

**確認方法**:
- Hierarchyで各GameObjectを選択
- Inspectorでフィールドが`None (PaintCanvas)`や`None (NetworkPaintCanvas)`になっていないか確認
- `None`になっている場合は、ProjectウィンドウまたはHierarchyから正しいオブジェクトをドラッグ&ドロップ

### ✅ チェック3: シーンオブジェクトとして登録されているか

**確認項目**:

`NetworkPaintCanvas`と`NetworkPaintBattleGameManager`が、**シーンオブジェクトとして正しく登録されているか**確認してください。

**確認方法**:

1. **NetworkManagerの設定を確認**
   - Hierarchyで`NetworkManager`のGameObjectを選択（存在する場合）
   - Inspectorで`Network Manager`コンポーネントを確認
   - `Scene Management`セクションで、現在のシーンが登録されているか確認

2. **自動的に検出されるか確認**
   - Unity Netcode for GameObjectsは、`NetworkObject`コンポーネントが付いたGameObjectを自動的に検出します
   - シーンに保存されている限り、自動的に同期されます

### ✅ チェック4: NetworkObjectの設定オプション

**確認項目**:

`NetworkPaintCanvas`のGameObjectの`NetworkObject`コンポーネントで、以下を確認：

1. **`Spawn With Observers`**: ✅ チェックされているか
   - デフォルトでチェックされているはず
   - チェックが外れていると、クライアント側で検出されない可能性があります

2. **`Synchronize Transform`**: ✅ チェックされているか（Transform同期が必要な場合）
   - `PaintCanvas`は位置が変わらないので、チェックが外れていても問題ありません
   - ただし、チェックされていても問題ありません

**確認方法**:
- Hierarchyで`NetworkPaintCanvas`のGameObjectを選択
- Inspectorで`NetworkObject`コンポーネントを確認
- `Spawn With Observers`がチェックされているか確認

### ✅ チェック5: NetworkManagerの存在確認

**確認項目**:

1. **`NetworkManager.Singleton`が存在するか**
   - ゲーム実行時に`NetworkManager`が存在するか確認
   - ログで`NetworkManager.Singleton`が`null`になっていないか確認

2. **NetworkManagerの初期化**
   - ゲーム開始時に`NetworkManager`が正しく初期化されているか確認
   - ホスト/クライアントとして正しく動作しているか確認

**確認方法**:
- ゲーム実行時にConsoleで警告がないか確認
- `NetworkManager.Singleton`が`null`のエラーが出ていないか確認

### ✅ チェック6: ゲームオブジェクトの有効化

**確認項目**:

1. **GameObjectがアクティブか**
   - `NetworkPaintCanvas`のGameObjectが`Active`になっているか ✅
   - `NetworkPaintBattleGameManager`のGameObjectが`Active`になっているか ✅

2. **コンポーネントが有効化されているか**
   - `NetworkPaintCanvas`コンポーネントのチェックボックスがオンになっているか ✅
   - `NetworkPaintBattleGameManager`コンポーネントのチェックボックスがオンになっているか ✅
   - `NetworkObject`コンポーネントのチェックボックスがオンになっているか ✅

**確認方法**:
- Hierarchyで各GameObjectを選択
- Inspectorで左上のチェックボックスがオンになっているか確認
- 各コンポーネントのチェックボックスがオンになっているか確認

### ✅ チェック7: 実行順序の問題

**確認項目**:

`NetworkPaintCanvas`と`NetworkPaintBattleGameManager`の実行順序を確認してください。

**確認方法**:
- Unityエディタの`Edit` → `Project Settings` → `Script Execution Order`
- `NetworkPaintCanvas`と`NetworkPaintBattleGameManager`の実行順序を確認
- 必要に応じて、実行順序を調整（通常は問題ありませんが、確認のため）

### よくある問題と解決方法

#### 問題1: `NetworkObject`コンポーネントが欠けている

**症状**:
- `ServerRpc`が呼ばれても、サーバー側で実行されない
- `OnNetworkSpawn()`が呼ばれない

**解決方法**:
- `NetworkPaintCanvas`のGameObjectに`NetworkObject`コンポーネントを追加
- `NetworkPaintBattleGameManager`のGameObjectに`NetworkObject`コンポーネントを追加

#### 問題2: Inspectorでの参照が設定されていない

**症状**:
- ログで`NetworkPaintCanvas: PaintCanvasが設定されていません`が出る
- ログで`NetworkPaintBattleGameManager: NetworkPaintCanvasが設定されていません`が出る

**解決方法**:
- Inspectorでフィールドを正しく設定
- 自動検索（`FindObjectOfType`）に依存している場合は、シーン内に1つだけ存在するか確認

#### 問題3: シーンが保存されていない

**症状**:
- ゲーム実行時に設定が反映されない

**解決方法**:
- `Ctrl + S`でシーンを保存
- 変更後に必ずシーンを保存する習慣をつける

### 推奨される確認手順

1. **まず、`NetworkObject`コンポーネントを確認**
   - `NetworkPaintCanvas`と`NetworkPaintBattleGameManager`の両方のGameObjectに`NetworkObject`が付いているか確認

2. **次に、Inspectorでの参照を確認**
   - すべてのフィールドが正しく設定されているか確認

3. **最後に、シーンを保存**
   - 変更を加えた場合は、必ずシーンを保存

これらの確認を行っても問題が解決しない場合は、次のステップとして`PaintAtWithRadiusInternal`のログを確認してください。
