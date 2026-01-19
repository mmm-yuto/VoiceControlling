# シーン設定チェックリスト

## 確認すべき項目

### 1. NetworkPaintBattleGameManager の設定
- [ ] `PaintBattleGameManager` オブジェクトに `NetworkPaintBattleGameManager` コンポーネントがアタッチされている
- [ ] `NetworkPaintBattleGameManager` に `NetworkObject` コンポーネントがアタッチされている
- [ ] `NetworkPaintBattleGameManager.localPaintManager` が正しく設定されている（同じGameObjectの`PaintBattleGameManager`を参照）
- [ ] `NetworkPaintBattleGameManager.networkPaintCanvas` が正しく設定されている（`PaintCanvasManager`の`NetworkPaintCanvas`を参照）
- [ ] `NetworkPaintBattleGameManager.onlyWorkInOnlineMode` が `true` に設定されている

### 2. NetworkPaintCanvas の設定
- [ ] `PaintCanvasManager` オブジェクトに `NetworkPaintCanvas` コンポーネントがアタッチされている
- [ ] `PaintCanvasManager` に `NetworkObject` コンポーネントがアタッチされている
- [ ] `NetworkPaintCanvas.paintCanvas` が正しく設定されている（同じGameObjectの`PaintCanvas`を参照）

### 3. PaintCanvas の設定
- [ ] `PaintCanvasManager` オブジェクトに `PaintCanvas` コンポーネントがアタッチされている
- [ ] `PaintCanvas.settings` が正しく設定されている

### 4. PaintBattleGameManager の設定
- [ ] `PaintBattleGameManager.paintCanvas` が正しく設定されている（`PaintCanvasManager`の`PaintCanvas`を参照）
- [ ] `PaintBattleGameManager.voiceInputHandler` が正しく設定されている

### 5. NetworkObject の設定
- [ ] `PaintBattleGameManager` の `NetworkObject` が `Scene Placed` として設定されている
- [ ] `PaintCanvasManager` の `NetworkObject` が `Scene Placed` として設定されている
- [ ] 両方の `NetworkObject` の `GlobalObjectIdHash` が正しく設定されている（0以外）

### 6. 実行時の確認
- [ ] ホスト側で `NetworkPaintBattleGameManager.OnNetworkSpawn()` が呼ばれている
- [ ] クライアント側で `NetworkPaintBattleGameManager.OnNetworkSpawn()` が呼ばれている
- [ ] クライアント側で `NetworkPaintBattleGameManager.SubscribeToPaintEvents()` が呼ばれている
- [ ] クライアント側で `NetworkPaintBattleGameManager.OnLocalPaintCompleted()` が呼ばれている
- [ ] サーバー側で `NetworkPaintCanvas.SendClientPaintServerRpc()` が呼ばれている

## よくある問題

### 問題1: NetworkObject が正しく設定されていない
**症状**: クライアント側で `OnNetworkSpawn()` が呼ばれない
**解決策**: 
- `NetworkObject` コンポーネントがアタッチされているか確認
- `GlobalObjectIdHash` が正しく設定されているか確認（0以外）

### 問題2: 参照が正しく設定されていない
**症状**: `networkPaintCanvas` が null
**解決策**:
- Inspectorで `NetworkPaintBattleGameManager.networkPaintCanvas` に `PaintCanvasManager` の `NetworkPaintCanvas` を設定
- または、コードで自動検索されることを確認（`FindObjectOfType<NetworkPaintCanvas>()`）

### 問題3: シーンに配置されていない
**症状**: オブジェクトが見つからない
**解決策**:
- `PaintBattleGameManager` と `PaintCanvasManager` がシーンに配置されているか確認
- PrefabInstanceとして配置されている場合は、Prefabの参照が正しいか確認

### 問題4: NetworkObject が Spawn されていない
**症状**: ネットワーク同期が動作しない
**解決策**:
- `NetworkObject` が `Scene Placed` として設定されているか確認
- `NetworkManager` が正しく動作しているか確認

## デバッグ手順

1. **Unityエディタで確認**:
   - Hierarchyで `PaintBattleGameManager` を選択
   - Inspectorで `NetworkPaintBattleGameManager` コンポーネントを確認
   - `networkPaintCanvas` フィールドが設定されているか確認

2. **実行時のログを確認**:
   - `[DEBUG] NetworkPaintBattleGameManager.OnNetworkSpawn` が出力されているか
   - `[DEBUG] NetworkPaintBattleGameManager.SubscribeToPaintEvents` が出力されているか
   - `[DEBUG] NetworkPaintBattleGameManager.OnLocalPaintCompleted` が出力されているか

3. **参照の確認**:
   - `NetworkPaintBattleGameManager.networkPaintCanvas` が null でないか
   - `NetworkPaintCanvas.paintCanvas` が null でないか
   - `PaintBattleGameManager.paintCanvas` が null でないか
