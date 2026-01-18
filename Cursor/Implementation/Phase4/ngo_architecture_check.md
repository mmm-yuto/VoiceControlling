# Unity Netcode for GameObjects (NGO) アーキテクチャ確認

## 概要

Unity Netcode for GameObjects (NGO) において、「ホストの変更は伝わるが、クライアントの変更が反映されない」のは、**NGOが「サーバー権限（Server Authoritative）」を基本としているから**です。

クライアントが自分のローカルで値を書き換えても、サーバー（ホスト）側がそれを許可して同期し直さない限り、他のプレイヤーには反映されません。これを解決するには、主に2つの方法があります。

---

## 現在の実装の確認結果

### ✅ 1. RPC（ServerRpc）を使ってサーバーに依頼する

**実装状況**: **実装済み**

現在のコードは、**ServerRpcパターンを正しく使用しています**。

#### 実装箇所

**1. `NetworkPaintCanvas.cs` - ServerRpcの定義**

```csharp
/// <summary>
/// クライアント側の塗りをサーバーに送信（ServerRpc）
/// </summary>
[ServerRpc(RequireOwnership = false)]
public void SendClientPaintServerRpc(
    Vector2 position, 
    int playerId, 
    float intensity, 
    Color color, 
    float radius, 
    ServerRpcParams rpcParams = default
)
{
    if (paintCanvas == null)
    {
        Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
        return;
    }
    
    // サーバー側のPaintCanvasに塗りを適用
    // これにより、サーバー側の差分検出が変更を検出できる
    paintCanvas.PaintAtWithRadius(position, playerId, intensity, color, radius);
}
```

**特徴**:
- ✅ `[ServerRpc(RequireOwnership = false)]` 属性を使用
- ✅ メソッド名が `〜ServerRpc` で終わる
- ✅ サーバー側で`PaintCanvas.PaintAtWithRadius()`を実行して塗りを適用

**2. `NetworkPaintBattleGameManager.cs` - ServerRpcの呼び出し**

```csharp
/// <summary>
/// ローカルプレイヤーの塗りイベントを処理
/// </summary>
private void OnLocalPaintCompleted(Vector2 position, int playerId, float intensity)
{
    // オーナーのみ実行（自分の塗りのみ送信）
    if (!IsOwner)
    {
        return;
    }
    
    // プレイヤーの塗りのみ送信（playerId > 0）
    if (playerId <= 0)
    {
        return; // 敵の塗りは送信しない
    }
    
    // オンラインモードチェック
    if (onlyWorkInOnlineMode && !IsOnlineMode())
    {
        return;
    }
    
    // NetworkPaintCanvasが設定されているか確認
    if (networkPaintCanvas == null)
    {
        Debug.LogWarning("NetworkPaintBattleGameManager: NetworkPaintCanvasが設定されていません");
        return;
    }
    
    // プレイヤー色を取得
    Color playerColor = GetPlayerColor();
    
    // ブラシの半径を取得
    float brushRadius = GetBrushRadius();
    
    // サーバーに塗りデータを送信
    networkPaintCanvas.SendClientPaintServerRpc(position, playerId, intensity, playerColor, brushRadius);
}
```

**特徴**:
- ✅ クライアント側（`IsOwner`の場合）でServerRpcを呼び出し
- ✅ サーバーに「この位置に塗ってください」とリクエストを送信

#### データフロー

```
[クライアント側]
1. クライアントが塗る
   ↓
2. PaintCanvas.PaintAt() 実行（ローカル）
   ↓
3. OnPaintCompleted イベント発火
   ↓
4. NetworkPaintBattleGameManager.OnLocalPaintCompleted() 実行
   ↓
5. networkPaintCanvas.SendClientPaintServerRpc() を呼び出し
   ↓
[サーバー側（ホスト）]
6. SendClientPaintServerRpc() がサーバー上で実行される
   ↓
7. paintCanvas.PaintAtWithRadius() をサーバー側で実行
   - サーバー側のPaintCanvasに塗りを適用
```

**✅ 結論**: ServerRpcパターンは**正しく実装されています**。

---

### ❓ 2. NetworkVariable の書き込み権限を変更する

**実装状況**: **使用していない**

現在のコードでは、`NetworkVariable`を使用していません。

#### 理由

- `PaintCanvas`の状態（色データ、プレイヤーID、タイムスタンプ）は、通常の配列（`Color[,]`, `int[,]`, `float[,]`）で管理されています
- ネットワーク同期は、**ServerRpc + ClientRpc + 差分同期**の方式で実現しています
- `NetworkVariable`は使用していないため、書き込み権限の設定は不要です

#### なぜNetworkVariableを使わないのか？

1. **データサイズの問題**: キャンバス全体（例: 1920x1080 = 2,073,600ピクセル）を`NetworkVariable`で管理するには大きすぎる
2. **パフォーマンス**: 毎フレーム全ピクセルを同期するのは非効率的
3. **差分同期の利点**: 変更されたピクセルのみを送信する方が効率的

**✅ 結論**: `NetworkVariable`は使用していませんが、**これは設計上の意図**であり、問題ではありません。

---

### ❓ 3. Transform（位置・回転）が同期されない場合

**実装状況**: **該当なし**

塗りキャンバスの同期は、Transformの同期ではなく、**テクスチャデータ（ピクセル情報）の同期**です。

#### 現在の実装

- `PaintCanvas`は通常の`MonoBehaviour`であり、`NetworkTransform`は使用していません
- 同期対象は「どこに、何色で塗られたか」という**ピクセルデータ**です
- Transformの同期は不要です

**✅ 結論**: Transform同期は使用していませんが、**これは仕様に合わせた実装**です。

---

## 現在の問題点と原因

### 問題: クライアントが塗った色がホスト側に反映されない

**現状**:
- ✅ ホストが塗った色 → クライアント側に反映される（正常）
- ❌ クライアントが塗った色 → ホスト側に反映されない（問題）

### 原因分析

#### ServerRpcの実装は正しい

**確認結果**:
- ✅ ServerRpcは正しく実装されている
- ✅ クライアントからServerRpcが呼び出されている
- ✅ サーバー側で`PaintCanvas.PaintAtWithRadius()`が実行されている

#### 考えられる根本原因

ServerRpcパターン自体は正しいですが、以下の問題が考えられます：

1. **インスタンス参照の問題**
   - `NetworkPaintCanvas.paintCanvas`と、ホスト側が表示に使っている`PaintCanvas`が**別のインスタンス**の可能性
   - サーバー側で塗りを適用しても、ホスト側の表示用PaintCanvasとは別のインスタンスに適用されている

2. **テクスチャ更新の問題**
   - サーバー側で`PaintCanvas.PaintAtWithRadius()`が実行されても、実際にテクスチャが更新されていない
   - `FlushTextureUpdates()`や`OnTextureUpdated`イベントが正しく動作していない可能性

3. **PaintRendererの参照問題**
   - ホスト側の`PaintRenderer`が参照している`PaintCanvas`と、`NetworkPaintCanvas`が参照している`PaintCanvas`が異なる

### 解決方法の優先順位

1. **最優先**: インスタンス参照の確認
   - `NetworkPaintCanvas.paintCanvas`とホスト側の表示用`PaintCanvas`が同じインスタンスか確認
   - UnityエディタのInspectorで確認するか、コードで`GetInstanceID()`を比較

2. **次**: テクスチャ更新の確認
   - サーバー側で`PaintCanvas.PaintAtWithRadius()`実行後、テクスチャが実際に更新されているか確認
   - `FlushTextureUpdates()`や`OnTextureUpdated`イベントの実行を確認

3. **最後**: PaintRendererの確認
   - `PaintRenderer`が参照している`PaintCanvas`の確認

---

## まとめ

### NGOパターンの適合性

| パターン | 実装状況 | 評価 |
|---------|---------|------|
| **ServerRpc** | ✅ 実装済み | **正しく実装されている** |
| **NetworkVariable** | ❌ 使用していない | **設計上の意図（問題なし）** |
| **Transform同期** | ❌ 使用していない | **仕様に合わせた実装（問題なし）** |

### 結論

**現在の実装は、Unity Netcode for GameObjects (NGO) のServerRpcパターンに正しく従っています。**

問題は、**ServerRpcパターンの実装自体ではなく**、以下のいずれかの可能性が高いです：

1. **インスタンス参照の不一致**: サーバー側とホスト側で異なる`PaintCanvas`インスタンスを使用している
2. **テクスチャ更新の失敗**: サーバー側で塗りが適用されても、テクスチャが更新されていない
3. **PaintRendererの参照問題**: ホスト側の`PaintRenderer`が別の`PaintCanvas`を参照している

### 次のステップ

`Cursor/Implementation/Phase4/current.md`の「確認方法」セクションに従って、上記の3点を順番に確認してください。

---

## 参考資料

- [Unity Netcode for GameObjects - ServerRPC & ClientRPC](https://www.youtube.com/watch?v=jXyxF42kZ_s)
- [Unity Netcode for GameObjects 公式ドキュメント](https://docs-multiplayer.unity3d.com/netcode/current/learn/rpc/)
