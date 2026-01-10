# パフォーマンスを維持しながらオンライン化する方法

## 📊 現状分析：オフライン版が軽量な理由

### 現在のオフライン版の処理フロー

```
PaintBattleGameManager.Update() (毎フレーム)
    ↓
VoiceInputHandlerから座標・音量を取得
    ↓
PaintAt() を直接呼び出し
    ↓
PaintCanvas.PaintAt() を直接呼び出し
    ↓
配列を直接更新（paintData, colorData, intensityData）
    ↓
テクスチャを更新（SetPixel → LateUpdateでApply）
    ↓
描画完了（イベント発火のみ、購読者なし）
```

### 軽量な理由

1. **直接的な処理**
   - 中間層がない（ネットワーク同期がない）
   - 余計な処理がない（差分検出、RPC送信など）

2. **更新頻度の最適化**
   ```csharp
   // PaintCanvas.cs 178-183行目
   frameCount++;
   if (frameCount % settings.updateFrequency != 0)
   {
       return; // 毎フレーム処理しない
   }
   ```
   - `updateFrequency`による間引き（デフォルト: 1 = 毎フレーム）
   - 必要に応じて調整可能（2-3フレームに1回など）

3. **テクスチャ更新のバッチ処理**
   ```csharp
   // PaintCanvas.cs 555-597行目
   private void UpdateTexturePixel(int x, int y, Color color)
   {
       canvasTexture.SetPixel(x, y, color);
       textureNeedsFlush = true; // Apply()は呼ばない
   }
   
   void LateUpdate()
   {
       if (textureNeedsFlush)
       {
           canvasTexture.Apply(); // フレームごとに1回だけ
           textureNeedsFlush = false;
       }
   }
   ```
   - `SetPixel()`は即座に実行、`Apply()`は`LateUpdate()`でまとめて1回のみ
   - GPU転送回数を最小化

4. **補間処理の最適化**
   ```csharp
   // PaintBattleGameManager.cs 249-260行目
   // 距離が短い場合は補間をスキップ
   if (distance < radius * 0.25f)
   {
       brush.Paint(paintCanvas, endPos, playerId, playerColor, intensity);
       return;
   }
   
   // 最大ステップ数を制限
   const int maxSteps = 50;
   steps = Mathf.Min(steps, maxSteps);
   ```
   - 短距離の補間をスキップ
   - 最大ステップ数を50に制限

5. **イベント発火のオーバーヘッドが最小**
   ```csharp
   OnPaintCompleted?.Invoke(screenPosition, playerId, effectiveIntensity);
   ```
   - イベントは発火されるが、購読者がいないため追加処理なし
   - nullチェック（`?.`）で安全にスキップ

6. **配列への直接アクセス**
   ```csharp
   paintData[canvasX, canvasY] = playerId;
   colorData[canvasX, canvasY] = color;
   intensityData[canvasX, canvasY] = effectiveIntensity;
   ```
   - メモリ上の配列を直接更新
   - シリアライズ/デシリアライズ、ネットワーク転送なし

7. **キャッシュによる最適化**
   ```csharp
   private void UpdatePixelCountCache(int oldPlayerId, int newPlayerId)
   {
       // 全ピクセルを走査せず、キャッシュを更新するだけ
   }
   ```
   - ピクセル数集計はキャッシュを使用
   - 毎回全ピクセルを走査しない

---

## 🎯 オンライン化の基本方針

### 原則：オフライン版と同じ軽量な処理を維持

1. **差分検出をしない**（全ピクセル走査を削除）
2. **塗りコマンドのみを送信**（位置、色、強度、半径）
3. **既存の最適化を活用**（updateFrequency、テクスチャ更新のバッチ処理）

### 処理フローの比較

#### ❌ 差分検出方式（削除済み、重い）

```
クライアント → SendClientPaintServerRpc() → サーバー側でPaintAtWithRadius()
                                     ↓
                     NetworkPaintCanvas.Update() が0.1秒ごとに実行
                                     ↓
                     PaintDiffManager.DetectChanges() で全ピクセル走査
                                     (width × height回の比較処理)
                                     ↓
                     ApplyPaintDiffClientRpc() で差分を送信
```

**問題点**:
- `NetworkPaintCanvas.Update()` が0.1秒ごとに全ピクセル（例: 1024×1024 = 1,048,576回）を走査
- CPU負荷が高い

#### ✅ 塗りコマンド方式（推奨、軽量）

```
クライアント → SendClientPaintServerRpc() → サーバー側でPaintAtWithRadius()
                                     ↓
                     ApplyPaintCommandClientRpc() で同じ塗りコマンドを全クライアントに転送
                                     ↓
                     各クライアントで PaintAtWithRadius() を実行
                                     （オフラインと同じ軽量な処理）
```

**改善点**:
- 全ピクセル走査を削除
- 塗りコマンド（位置、色、強度、半径）を転送するだけ（軽量）
- オフラインと同じように直接塗り処理を実行

---

## 🛠️ 実装パターン

### パターンA: シンプル（推奨、難易度：中）

**特徴**:
- `OnPaintCompleted`イベントで`updateFrequency`に合わせて送信
- バッチングなし
- 実装が簡単で安定

**実装**:

```csharp
// NetworkPaintBattleGameManager.cs
using Unity.Netcode;
using UnityEngine;

public class NetworkPaintBattleGameManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PaintBattleGameManager localPaintManager;
    [SerializeField] private NetworkPaintCanvas networkPaintCanvas;
    
    [Header("Settings")]
    [SerializeField] private bool onlyWorkInOnlineMode = true;
    
    private bool isSubscribed = false;
    private int frameCount = 0; // updateFrequency用のカウンター
    
    void OnEnable()
    {
        if (onlyWorkInOnlineMode && !IsOnlineMode())
        {
            return;
        }
        SubscribeToPaintEvents();
    }
    
    void OnDisable()
    {
        UnsubscribeFromPaintEvents();
    }
    
    private bool IsOnlineMode()
    {
        if (NetworkManager.Singleton != null)
        {
            return NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer;
        }
        
        if (GameModeManager.Instance != null)
        {
            return GameModeManager.Instance.IsOnlineMode;
        }
        
        return false;
    }
    
    private void SubscribeToPaintEvents()
    {
        if (isSubscribed) return;
        
        PaintCanvas paintCanvas = null;
        if (localPaintManager != null && localPaintManager.paintCanvas != null)
        {
            paintCanvas = localPaintManager.paintCanvas;
        }
        else
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
        }
        
        if (paintCanvas != null)
        {
            paintCanvas.OnPaintCompleted += OnLocalPaintCompleted;
            isSubscribed = true;
            Debug.Log("NetworkPaintBattleGameManager: PaintCanvasのイベントを購読しました");
        }
    }
    
    private void UnsubscribeFromPaintEvents()
    {
        if (!isSubscribed) return;
        
        PaintCanvas paintCanvas = null;
        if (localPaintManager != null && localPaintManager.paintCanvas != null)
        {
            paintCanvas = localPaintManager.paintCanvas;
        }
        else
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
        }
        
        if (paintCanvas != null)
        {
            paintCanvas.OnPaintCompleted -= OnLocalPaintCompleted;
            isSubscribed = false;
        }
    }
    
    /// <summary>
    /// ローカルプレイヤーの塗りイベントを処理
    /// </summary>
    private void OnLocalPaintCompleted(Vector2 position, int playerId, float intensity)
    {
        // ホスト側（IsServer && IsClient）の場合は、直接PaintCanvasに描画されるため送信不要
        if (IsServer)
        {
            return;
        }
        
        // クライアント側のみ実行
        if (!IsClient)
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
        
        // PaintCanvasのupdateFrequencyを取得して間引き
        var settings = localPaintManager?.paintCanvas?.GetSettings();
        if (settings != null)
        {
            frameCount++;
            if (frameCount % settings.updateFrequency != 0)
            {
                return; // PaintCanvasと同じ頻度で間引き
            }
        }
        
        // プレイヤー色を取得
        Color playerColor = GetPlayerColor();
        
        // ブラシの半径を取得
        float brushRadius = GetBrushRadius();
        
        // サーバーに塗りデータを送信
        networkPaintCanvas.SendClientPaintServerRpc(position, playerId, intensity, playerColor, brushRadius);
    }
    
    private Color GetPlayerColor()
    {
        if (BattleSettings.Instance != null && BattleSettings.Instance.Current != null)
        {
            string brushKey = BattleSettings.Instance.Current.brushKey;
            if (!string.IsNullOrEmpty(brushKey) && brushKey != "Default")
            {
                return BattleSettings.Instance.Current.playerColor;
            }
            return BattleSettings.Instance.GetMainColor1();
        }
        
        return Color.blue; // フォールバック値
    }
    
    private float GetBrushRadius()
    {
        if (localPaintManager != null)
        {
            // リフレクションでブラシを取得
            var brushField = typeof(PaintBattleGameManager).GetField("brush", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var brush = brushField?.GetValue(localPaintManager) as BrushStrategyBase;
            
            if (brush != null)
            {
                return brush.GetRadius();
            }
        }
        
        return 10f; // デフォルト半径
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Debug.Log($"NetworkPaintBattleGameManager: ネットワーク接続 - IsServer: {IsServer}, IsClient: {IsClient}");
        
        // ローカルのPaintBattleGameManagerのplayerIdを設定
        if (localPaintManager != null && IsClient)
        {
            if (IsServer)
            {
                localPaintManager.playerId = 1;
            }
            else
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                localPaintManager.playerId = (int)localClientId + 1;
            }
        }
        
        // ネットワーク接続時にイベントを購読（クライアント側のみ）
        if (IsClient)
        {
            SubscribeToPaintEvents();
        }
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        UnsubscribeFromPaintEvents();
        Debug.Log("NetworkPaintBattleGameManager: ネットワーク切断");
    }
}
```

**NetworkPaintCanvas.cs**:

```csharp
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ネットワーク対応PaintCanvas
/// 塗りコマンドベースの同期方式でネットワーク経由で同期（オフラインと同じ軽量な方法）
/// </summary>
public class NetworkPaintCanvas : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PaintCanvas paintCanvas;
    
    [Header("Network Settings")]
    [Tooltip("初回同期時の最大ピクセル数（これを超える場合は分割送信）")]
    [SerializeField] private int maxPixelsPerMessage = 5000;
    
    void Awake()
    {
        if (paintCanvas == null)
        {
            paintCanvas = FindObjectOfType<PaintCanvas>();
            if (paintCanvas == null)
            {
                Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが見つかりません。Inspectorで設定してください。");
            }
        }
    }
    
    /// <summary>
    /// クライアント側の塗りをサーバーに送信（ServerRpc）
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SendClientPaintServerRpc(Vector2 position, int playerId, float intensity, Color color, float radius, ServerRpcParams rpcParams = default)
    {
        if (paintCanvas == null)
        {
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        // サーバー側のPaintCanvasに塗りを適用
        paintCanvas.PaintAtWithRadius(position, playerId, intensity, color, radius);
        
        // 全クライアントに同じ塗りコマンドを転送（オフラインと同じ軽量な方法）
        ApplyPaintCommandClientRpc(position, playerId, intensity, color, radius);
    }
    
    /// <summary>
    /// 塗りコマンドを受信して適用（ClientRpc）
    /// オフラインと同じように直接塗り処理を実行する軽量な方法
    /// </summary>
    [ClientRpc]
    private void ApplyPaintCommandClientRpc(Vector2 position, int playerId, float intensity, Color color, float radius, ClientRpcParams rpcParams = default)
    {
        // サーバー側（ホスト）は既に塗り済みなのでスキップ
        if (IsServer)
        {
            return;
        }
        
        if (paintCanvas == null)
        {
            Debug.LogWarning("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        // オフラインと同じように直接塗り処理を実行（軽量）
        paintCanvas.PaintAtWithRadius(position, playerId, intensity, color, radius);
    }
    
    /// <summary>
    /// サーバー時刻を取得（初回同期用）
    /// </summary>
    private float GetServerTime()
    {
        if (IsServer)
        {
            return Time.time;
        }
        else
        {
            return (float)NetworkManager.Singleton.ServerTime.Time;
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (paintCanvas == null)
        {
            Debug.LogError("NetworkPaintCanvas: PaintCanvasが設定されていません");
            return;
        }
        
        // PaintCanvasにタイムスタンプ取得コールバックを設定
        paintCanvas.SetTimestampCallback(GetServerTime);
        
        Debug.Log($"NetworkPaintCanvas: ネットワーク接続 - IsServer: {IsServer}, IsClient: {IsClient}");
        
        // サーバー側でクライアント接続時に初回同期を送信（必要に応じて実装）
        if (IsServer)
        {
            // 初回同期は必要に応じて実装
            // StartCoroutine(SendInitialSnapshotDelayed());
        }
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Debug.Log("NetworkPaintCanvas: ネットワーク切断");
    }
}
```

**効果**:
- `PaintCanvas`の更新頻度と同期することで、不要な送信を削減
- 実装が簡単で安定
- オフラインと同じ軽量さを維持

---

### パターンB: バッチング（オプション、難易度：やや高）

**特徴**:
- バッチングを追加
- 高頻度時も負荷を抑えられる
- 実装がやや複雑

**実装**:

```csharp
// NetworkPaintBattleGameManager.cs（追加部分）
private Queue<PaintCommand> paintCommandBuffer = new Queue<PaintCommand>();
private float lastSendTime = 0f;
private const float SEND_INTERVAL = 0.05f; // 20fps（50ms間隔）

private struct PaintCommand
{
    public Vector2 position;
    public int playerId;
    public float intensity;
    public Color color;
    public float radius;
}

private void OnLocalPaintCompleted(Vector2 position, int playerId, float intensity)
{
    // 既存のチェック（ホスト判定、クライアント判定など）
    if (IsServer || !IsClient || playerId <= 0) return;
    
    if (onlyWorkInOnlineMode && !IsOnlineMode()) return;
    if (networkPaintCanvas == null) return;
    
    // バッファに追加
    paintCommandBuffer.Enqueue(new PaintCommand
    {
        position = position,
        playerId = playerId,
        intensity = intensity,
        color = GetPlayerColor(),
        radius = GetBrushRadius()
    });
}

void Update()
{
    // 一定間隔でバッファを送信
    if (Time.time - lastSendTime >= SEND_INTERVAL && paintCommandBuffer.Count > 0)
    {
        SendBufferedCommands();
        lastSendTime = Time.time;
    }
}

private void SendBufferedCommands()
{
    // 最新のコマンドのみ送信（または最大N個）
    const int MAX_COMMANDS_PER_FRAME = 10;
    int sendCount = Mathf.Min(paintCommandBuffer.Count, MAX_COMMANDS_PER_FRAME);
    
    for (int i = 0; i < sendCount; i++)
    {
        var cmd = paintCommandBuffer.Dequeue();
        networkPaintCanvas.SendClientPaintServerRpc(
            cmd.position, cmd.playerId, cmd.intensity, cmd.color, cmd.radius
        );
    }
    
    // バッファが溢れた場合は古いコマンドを破棄
    if (paintCommandBuffer.Count > 100)
    {
        Debug.LogWarning("Paint command buffer overflow, clearing old commands");
        paintCommandBuffer.Clear();
    }
}
```

**効果**:
- ネットワーク負荷を制御
- パケット数を削減

**注意点**:
- 若干の遅延が発生（最大50ms）
- バッファオーバーフローの処理が必要

---

## 📊 パフォーマンス比較

| 方式 | CPU負荷 | ネットワーク負荷 | リアルタイム性 | 実装難易度 |
|------|---------|------------------|----------------|------------|
| **差分検出方式（削除済み）** | 高い（全ピクセル走査） | 中（差分データ） | 低（0.1秒遅延） | 高 |
| **塗りコマンド方式（パターンA）** | 低（オフラインと同じ） | 低（軽量なコマンド） | 高（即座） | 中 |
| **バッチング方式（パターンB）** | 低 | 極低 | 中（50ms遅延） | やや高 |

---

## 🎯 推奨実装順序

### Step 1: パターンA（シンプル）を実装

1. `NetworkPaintCanvas.cs`を作成（上記コード）
2. `NetworkPaintBattleGameManager.cs`を作成（上記コード、パターンA）
3. シーンに配置してテスト

**期待される結果**:
- オフラインと同等の軽量さ
- リアルタイムな同期

### Step 2: 必要に応じてパターンB（バッチング）を追加

高頻度に塗りが発生する場合や、ネットワーク負荷が高い場合に実装。

---

## 🔍 最適化のポイント

### 1. updateFrequencyの活用

```csharp
// PaintCanvasのupdateFrequencyを取得して間引き
var settings = localPaintManager?.paintCanvas?.GetSettings();
if (settings != null)
{
    frameCount++;
    if (frameCount % settings.updateFrequency != 0)
    {
        return; // PaintCanvasと同じ頻度で間引き
    }
}
```

**効果**: 不要な送信を削減

### 2. 補間処理の注意点

`PaintBattleGameManager`の補間処理（`PaintLineBetween`）は維持するが、**補間された各点を個別に送信すると負荷が高い**。

**推奨**:
- 補間はローカルで実行
- 最終的な位置のみを送信するか、補間の代表点のみを送信

### 3. イベント購読の最適化

```csharp
// オンラインモード時のみ購読
if (onlyWorkInOnlineMode && !IsOnlineMode())
{
    return;
}
```

**効果**: オフラインモード時はイベント購読をスキップ

### 4. ホスト側の最適化

```csharp
// ホスト側は既に描画済みなので、ApplyPaintCommandClientRpc()でスキップ
if (IsServer)
{
    return;
}
```

**効果**: ホスト側の重複描画を防止

---

## 📝 設定の推奨値

### PaintSettings

```csharp
[Tooltip("塗りの更新頻度（フレーム単位、1=毎フレーム）")]
[Range(1, 10)]
public int updateFrequency = 1; // オフライン: 1、オンライン: 1-3（負荷に応じて調整）

[Tooltip("テクスチャ更新頻度（フレーム単位、1=毎フレーム、2=2フレームに1回）")]
[Range(1, 5)]
public int textureUpdateFrequency = 1; // 1-2を推奨
```

### NetworkPaintBattleGameManager

```csharp
[Tooltip("オンラインモード時のみ動作するか")]
[SerializeField] private bool onlyWorkInOnlineMode = true; // 推奨: true
```

### ネットワーク送信間隔（パターンBの場合）

```csharp
private const float SEND_INTERVAL = 0.05f; // 20fps（50ms間隔）
const int MAX_COMMANDS_PER_FRAME = 10; // 1回の送信あたりの最大コマンド数
```

---

## ✅ 結論

**難易度**: 中程度

**実装可能性**: 高い

- オフライン版の軽量な処理を維持できる
- `updateFrequency`の活用で不要な送信を削減
- バッチングは任意で追加可能
- 差分検出を避けることで重い処理を排除

**推奨**: パターンA（シンプル）から始めて、必要に応じてパターンB（バッチング）を追加する。

これにより、オフラインと同等の軽量さを維持しながらオンライン化できます。

---

## 🐛 トラブルシューティング

### 問題1: ネットワーク送信が頻繁すぎる

**解決策**:
- `updateFrequency`を2-3に設定
- パターンB（バッチング）を実装

### 問題2: ホスト側で重複描画される

**解決策**:
- `ApplyPaintCommandClientRpc()`で`IsServer`チェックを追加

### 問題3: 補間処理で大量のコマンドが送信される

**解決策**:
- 補間の代表点のみを送信
- または、補間はローカルで実行し、最終位置のみを送信

### 問題4: 初回同期が必要

**解決策**:
- `OnNetworkSpawn()`で初回同期を実装（必要に応じて）
- 初回同期は別途実装（分割送信などを使用）
