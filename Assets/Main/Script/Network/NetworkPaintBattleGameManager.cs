using Unity.Netcode;
using UnityEngine;
using System.Reflection;

/// <summary>
/// ネットワーク対応PaintBattleGameManager
/// ローカルプレイヤーの塗りをネットワークに送信
/// </summary>
public class NetworkPaintBattleGameManager : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("ローカルのPaintBattleGameManager（Inspectorで接続）")]
    [SerializeField] private PaintBattleGameManager localPaintManager;
    
    [Tooltip("ネットワーク対応PaintCanvas（Inspectorで接続）")]
    [SerializeField] private NetworkPaintCanvas networkPaintCanvas;
    
    [Header("Settings")]
    [Tooltip("オンラインモード時のみ動作するか")]
    [SerializeField] private bool onlyWorkInOnlineMode = true;
    
    [Tooltip("デバッグログを出力するか")]
    [SerializeField] private bool enableDebugLog = false;
    
    // イベント購読フラグ
    private bool isSubscribed = false;
    
    // updateFrequencyによる間引き用のフレームカウンター
    private int frameCount = 0;
    
    void Awake()
    {
        // 参照の自動検索（未設定の場合）
        if (localPaintManager == null)
        {
            localPaintManager = FindObjectOfType<PaintBattleGameManager>();
        }
        
        if (networkPaintCanvas == null)
        {
            networkPaintCanvas = FindObjectOfType<NetworkPaintCanvas>();
        }
    }
    
    void OnEnable()
    {
        // オンラインモードチェック
        if (onlyWorkInOnlineMode && !IsOnlineMode())
        {
            if (enableDebugLog)
            {
                Debug.Log($"[NetworkPaintBattleGameManager] OnEnable: オンラインモードではないため、イベント購読をスキップ");
            }
            return;
        }
        
        // PaintCanvasのOnPaintCompletedイベントを購読
        SubscribeToPaintEvents();
    }
    
    void OnDisable()
    {
        // イベント購読を解除
        UnsubscribeFromPaintEvents();
    }
    
    /// <summary>
    /// オンラインモードかどうかを確認
    /// </summary>
    private bool IsOnlineMode()
    {
        // NetworkManagerが接続されているかどうかで判定
        // これにより、ParrelSyncで接続している場合も正しく判定される
        if (NetworkManager.Singleton != null)
        {
            return NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer;
        }
        
        // フォールバック: GameModeManagerを使用
        if (GameModeManager.Instance != null)
        {
            return GameModeManager.Instance.IsOnlineMode;
        }
        
        return false;
    }
    
    /// <summary>
    /// プレイヤー色を取得
    /// </summary>
    private Color GetPlayerColor()
    {
        // BattleSettingsからプレイヤー色を取得
        if (BattleSettings.Instance != null && BattleSettings.Instance.Current != null)
        {
            // brushKeyが設定されている場合、設定UIで選択された色を使用
            string brushKey = BattleSettings.Instance.Current.brushKey;
            if (!string.IsNullOrEmpty(brushKey) && brushKey != "Default")
            {
                return BattleSettings.Instance.Current.playerColor;
            }
            
            // デフォルト色を使用
            return BattleSettings.Instance.GetMainColor1();
        }
        
        return Color.blue; // フォールバック値
    }
    
    /// <summary>
    /// PaintCanvasのイベントを購読
    /// </summary>
    private void SubscribeToPaintEvents()
    {
        if (isSubscribed) 
        {
            return;
        }
        
        // PaintCanvasを取得
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
            
            if (enableDebugLog)
            {
                Debug.Log($"[NetworkPaintBattleGameManager] イベント購読完了 - IsServer: {IsServer}, IsClient: {IsClient}");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[NetworkPaintBattleGameManager] PaintCanvasが見つかりません");
            }
            
            // クライアント側でPaintCanvasが見つからない場合、少し遅延して再試行
            if (IsClient)
            {
                StartCoroutine(RetrySubscribeToPaintEvents());
            }
        }
    }
    
    /// <summary>
    /// PaintCanvasのイベント購読を再試行（遅延実行）
    /// </summary>
    private System.Collections.IEnumerator RetrySubscribeToPaintEvents()
    {
        yield return new WaitForSeconds(0.5f);
        if (!isSubscribed)
        {
            SubscribeToPaintEvents();
        }
    }
    
    /// <summary>
    /// PaintCanvasのイベント購読を解除
    /// </summary>
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
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintBattleGameManager] OnLocalPaintCompleted - position: {position}, playerId: {playerId}, intensity: {intensity}, IsServer: {IsServer}, IsClient: {IsClient}");
        }
        
        // クライアント側のみ実行（ホストもクライアントとして扱われる）
        if (!IsClient)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[NetworkPaintBattleGameManager] IsClientがfalseのため送信をスキップ");
            }
            return;
        }
        
        // プレイヤーの塗りのみ送信（playerId > 0）
        // playerId == -1 は敵（CPU）の塗りなので送信しない
        if (playerId <= 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[NetworkPaintBattleGameManager] playerIdが0以下のため送信をスキップ (playerId: {playerId})");
            }
            return; // 敵の塗りは送信しない
        }
        
        // オンラインモードチェック
        if (onlyWorkInOnlineMode && !IsOnlineMode())
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[NetworkPaintBattleGameManager] オンラインモードではないため送信をスキップ");
            }
            return;
        }
        
        // NetworkPaintCanvasが設定されているか確認
        if (networkPaintCanvas == null)
        {
            if (enableDebugLog)
            {
                Debug.LogError($"[NetworkPaintBattleGameManager] NetworkPaintCanvasが設定されていません");
            }
            return;
        }
        
        // PaintCanvasのupdateFrequencyを取得して間引き
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
            var settings = paintCanvas.GetSettings();
            if (settings != null)
            {
                frameCount++;
                if (frameCount % settings.updateFrequency != 0)
                {
                    // PaintCanvasと同じ頻度で間引き
                    if (enableDebugLog)
                    {
                        Debug.Log($"[NetworkPaintBattleGameManager] updateFrequencyによる間引き (frameCount: {frameCount}, updateFrequency: {settings.updateFrequency})");
                    }
                    return;
                }
            }
        }
        
        // プレイヤー色を取得
        Color playerColor = GetPlayerColor();
        
        // ブラシの半径を取得
        float brushRadius = GetBrushRadius();
        
        // 画面座標を正規化座標（0-1）に変換（画面サイズが異なる環境でも正しく動作するため）
        Vector2 normalizedPosition = new Vector2(
            position.x / Screen.width,
            position.y / Screen.height
        );
        
        // 半径も正規化（画面幅を基準）
        float normalizedRadius = brushRadius / Screen.width;
        
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintBattleGameManager] 塗りデータを送信 - position: {position} (normalized: {normalizedPosition}), playerId: {playerId}, color: {playerColor}, radius: {brushRadius} (normalized: {normalizedRadius})");
        }
        
        // サーバーに塗りデータを送信（ホスト側でも送信して、他のクライアントに同期させる）
        networkPaintCanvas.SendClientPaintServerRpc(normalizedPosition, playerId, intensity, playerColor, normalizedRadius);
    }
    
    /// <summary>
    /// ブラシの半径を取得
    /// </summary>
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
    
    /// <summary>
    /// ネットワーク接続時の初期化
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (enableDebugLog)
        {
            Debug.Log($"[NetworkPaintBattleGameManager] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, LocalClientId: {NetworkManager.Singleton?.LocalClientId}");
        }
        
        // ローカルのPaintBattleGameManagerのplayerIdを設定
        // IsOwnerチェックを削除し、IsClientチェックに変更（クライアント側でもplayerIdを設定するため）
        if (localPaintManager != null && IsClient)
        {
            // ホスト（サーバー）の場合は1、クライアントの場合は2以降
            if (IsServer)
            {
                localPaintManager.playerId = 1;
                if (enableDebugLog)
                {
                    Debug.Log($"[NetworkPaintBattleGameManager] ホスト側: playerId = 1 を設定");
                }
            }
            else
            {
                // クライアントIDをプレイヤーIDに変換
                // LocalClientIdは通常1から始まる（0はサーバー）
                // ただし、ホストもクライアントとして扱われるため、ホストの場合は既に1が設定されている
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                // クライアントID + 1 でプレイヤーIDを生成（ホストは0なので1、最初のクライアントは1なので2）
                localPaintManager.playerId = (int)localClientId + 1;
                if (enableDebugLog)
                {
                    Debug.Log($"[NetworkPaintBattleGameManager] クライアント側: playerId = {localPaintManager.playerId} を設定 (LocalClientId: {localClientId})");
                }
            }
        }
        
        // ネットワーク接続時にイベントを購読（クライアント側のみ）
        if (IsClient)
        {
            SubscribeToPaintEvents();
        }
    }
    
    /// <summary>
    /// ネットワーク切断時の処理
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        // イベント購読を解除
        UnsubscribeFromPaintEvents();
    }
}

