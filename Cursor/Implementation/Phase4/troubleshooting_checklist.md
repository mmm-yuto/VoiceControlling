# クライアントの変更がホストに反映されない問題の確認チェックリスト

## 問題の概要

- ✅ **ホストの変更 → クライアント側に反映される**（正常）
- ❌ **クライアントの変更 → ホスト側に反映されない**（問題）

## コード側の確認（確認済み）

- ✅ `SendClientPaintServerRpc()`が正常に呼ばれている
- ✅ サーバー側で`PaintAtWithRadius()`が呼ばれている（コード上）
- ✅ `NetworkPaintCanvas.paintCanvas`と`PaintRenderer.paintCanvas`が同じインスタンス（InstanceID一致）
- ✅ `RequireOwnership = false`が設定されている
- ✅ `IsClient`チェックが正しく動作している

## 根本的な原因として考えられること（Unityエディタ設定）

### ⚠️ 最重要チェック項目

#### 1. `NetworkPaintCanvas`のGameObjectに`NetworkObject`コンポーネントが付いているか？

**確認方法**:
1. UnityエディタのHierarchyで`NetworkPaintCanvas`がアタッチされているGameObjectを選択
2. Inspectorウィンドウを確認
3. **`NetworkObject`コンポーネント**が表示されているか確認

**問題の症状**:
- `NetworkObject`が欠けている場合、`ServerRpc`が呼ばれても**サーバー側で実行されない**
- `NetworkBehaviour`が正しく動作しない

**解決方法**:
- `Add Component` → `Network Object`で`NetworkObject`コンポーネントを追加
- **重要**: `NetworkPaintCanvas`と同じGameObjectに追加する必要がある

#### 2. `NetworkPaintBattleGameManager`のGameObjectに`NetworkObject`コンポーネントが付いているか？

**確認方法**:
1. UnityエディタのHierarchyで`NetworkPaintBattleGameManager`がアタッチされているGameObjectを選択
2. Inspectorウィンドウを確認
3. **`NetworkObject`コンポーネント**が表示されているか確認

**問題の症状**:
- `NetworkObject`が欠けている場合、`OnNetworkSpawn()`が呼ばれない
- イベント購読が行われない可能性がある

**解決方法**:
- `Add Component` → `Network Object`で`NetworkObject`コンポーネントを追加

#### 3. Inspectorでの参照設定が正しいか？

**`NetworkPaintCanvas`のInspector確認**:
1. `Paint Canvas`フィールドに`PaintCanvas`コンポーネントが設定されているか
2. `None (PaintCanvas)`になっていないか

**`NetworkPaintBattleGameManager`のInspector確認**:
1. `Local Paint Manager`フィールドに`PaintBattleGameManager`が設定されているか
2. `Network Paint Canvas`フィールドに`NetworkPaintCanvas`が設定されているか
3. 両方とも`None`になっていないか

**問題の症状**:
- 参照が`None`の場合、自動検索（`FindObjectOfType`）に依存するが、複数存在する場合に問題が発生する可能性がある

**解決方法**:
- Hierarchyから正しいGameObjectをドラッグ&ドロップして設定

#### 4. `NetworkObject`の`Spawn With Observers`がチェックされているか？

**確認方法**:
1. `NetworkPaintCanvas`のGameObjectを選択
2. Inspectorで`NetworkObject`コンポーネントを確認
3. **`Spawn With Observers`**がチェックされているか確認

**問題の症状**:
- チェックが外れている場合、クライアント側でオブジェクトが検出されない可能性がある
- `ServerRpc`が呼ばれても実行されない可能性がある

**解決方法**:
- `Spawn With Observers`にチェックを入れる

#### 5. GameObjectがアクティブになっているか？

**確認方法**:
1. Hierarchyで`NetworkPaintCanvas`のGameObjectを選択
2. Inspectorの左上のチェックボックスが**オン**になっているか確認
3. `NetworkPaintBattleGameManager`のGameObjectも同様に確認

**問題の症状**:
- 非アクティブの場合、`Awake()`や`Start()`が呼ばれない
- コンポーネントが動作しない

**解決方法**:
- チェックボックスをオンにする

#### 6. コンポーネントが有効化されているか？

**確認方法**:
1. `NetworkPaintCanvas`コンポーネントのチェックボックスがオンになっているか確認
2. `NetworkPaintBattleGameManager`コンポーネントのチェックボックスがオンになっているか確認
3. `NetworkObject`コンポーネントのチェックボックスがオンになっているか確認

**問題の症状**:
- コンポーネントが無効化されている場合、そのコンポーネントは動作しない

**解決方法**:
- 各コンポーネントのチェックボックスをオンにする

### その他の確認項目

#### 7. シーンが保存されているか？

**確認方法**:
- シーンファイルのタブに`*`が表示されていないか確認（未保存の場合`*`が表示される）

**問題の症状**:
- 設定を変更しても、シーンが保存されていない場合は反映されない

**解決方法**:
- `Ctrl + S`（または`Cmd + S`）でシーンを保存

#### 8. 同じシーンでホストとクライアントが動作しているか？

**確認方法**:
- ホストとクライアントが同じシーン（例: `GameScene`）を使用しているか確認

**問題の症状**:
- 異なるシーンを使用している場合、`NetworkObject`が検出されない可能性がある

**解決方法**:
- 同じシーンを使用する

#### 9. `NetworkManager`が正しく設定されているか？

**確認方法**:
1. Hierarchyで`NetworkManager`のGameObjectを選択
2. Inspectorで`Network Manager`コンポーネントを確認
3. `Unity Transport`コンポーネントが設定されているか確認

**問題の症状**:
- `NetworkManager`が存在しない場合、ネットワーク機能が動作しない
- `Unity Transport`が設定されていない場合、接続できない

**解決方法**:
- `NetworkManager`のGameObjectに`Unity Transport`コンポーネントを追加

## 推奨される確認手順（優先順位順）

### ステップ1: `NetworkObject`コンポーネントの確認（最優先）

1. `NetworkPaintCanvas`のGameObjectを選択
2. `NetworkObject`コンポーネントが存在するか確認
3. 存在しない場合は追加

4. `NetworkPaintBattleGameManager`のGameObjectを選択
5. `NetworkObject`コンポーネントが存在するか確認
6. 存在しない場合は追加

### ステップ2: Inspectorでの参照設定の確認

1. `NetworkPaintCanvas`のInspectorで`Paint Canvas`フィールドを確認
2. `NetworkPaintBattleGameManager`のInspectorで`Local Paint Manager`と`Network Paint Canvas`フィールドを確認
3. 設定されていない場合は、Hierarchyからドラッグ&ドロップで設定

### ステップ3: `Spawn With Observers`の確認

1. `NetworkPaintCanvas`のGameObjectを選択
2. `NetworkObject`コンポーネントで`Spawn With Observers`がチェックされているか確認
3. チェックされていない場合はチェック

### ステップ4: アクティブ状態の確認

1. すべてのGameObjectとコンポーネントがアクティブになっているか確認

### ステップ5: シーンの保存

1. `Ctrl + S`でシーンを保存

## 最も可能性の高い原因

**`NetworkObject`コンポーネントが欠けている**可能性が最も高いです。

`NetworkObject`が欠けている場合：
- `NetworkBehaviour`が正しく動作しない
- `ServerRpc`が呼ばれてもサーバー側で実行されない
- `OnNetworkSpawn()`が呼ばれない

この問題は、コードをいくら見ても見つからないため、**Unityエディタでの設定確認が必須**です。

## 確認後の動作確認

上記の確認を行った後：

1. **シーンを保存**（`Ctrl + S`）
2. **ゲームを実行**
3. **ホストとして開始**
4. **クライアントとして接続**
5. **クライアント側で塗りを実行**
6. **ホスト側の画面で塗りが反映されるか確認**

反映されない場合は、以下のログを確認：

- `SendClientPaintServerRpc()`が呼ばれているか（クライアント側）
- `SendClientPaintServerRpc()`が実行されているか（サーバー側）
- `PaintAtWithRadius()`が実行されているか（サーバー側）
