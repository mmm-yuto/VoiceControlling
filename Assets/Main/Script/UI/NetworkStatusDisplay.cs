using Unity.Netcode;
using UnityEngine;
using TMPro;

/// <summary>
/// ネットワーク状態をゲーム画面に表示する
/// オンラインモードかどうか、接続人数などを表示
/// </summary>
public class NetworkStatusDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("ネットワーク状態を表示するTextMeshProUGUI")]
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Update Settings")]
    [Tooltip("更新間隔（秒）")]
    [SerializeField] private float updateInterval = 0.5f;
    
    [Header("Debug Settings")]
    [Tooltip("デバッグ用: InspectorでOnlineModeを切り替える")]
    [SerializeField] private bool debugOnlineMode = false;
    [Tooltip("前回のdebugOnlineModeの値（変更検出用）")]
    private bool lastDebugOnlineMode = false;
    
    private float lastUpdateTime = 0f;
    
    void Start()
    {
        // TextMeshProUGUIを自動検索（設定されていない場合）
        if (statusText == null)
        {
            statusText = GetComponent<TextMeshProUGUI>();
            if (statusText == null)
            {
                statusText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        
        // 初期値を設定（現在のOnlineModeの状態に合わせる）
        if (GameModeManager.Instance != null)
        {
            lastDebugOnlineMode = GameModeManager.Instance.IsOnlineMode;
            debugOnlineMode = lastDebugOnlineMode;
        }
        else
        {
            lastDebugOnlineMode = debugOnlineMode;
        }
        
        // 初期表示
        UpdateStatusDisplay();
    }
    
    void Update()
    {
        // デバッグ用: InspectorでOnlineModeを切り替え
        if (debugOnlineMode != lastDebugOnlineMode)
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SetOnlineMode(debugOnlineMode);
                Debug.Log($"NetworkStatusDisplay: OnlineModeを{(debugOnlineMode ? "ON" : "OFF")}に変更しました");
            }
            else
            {
                Debug.LogWarning("NetworkStatusDisplay: GameModeManager.Instanceがnullです。GameModeManagerがシーンに存在することを確認してください。");
            }
            lastDebugOnlineMode = debugOnlineMode;
        }
        
        // 定期的に更新
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateStatusDisplay();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// ネットワーク状態を更新して表示
    /// </summary>
    private void UpdateStatusDisplay()
    {
        if (statusText == null)
        {
            return;
        }
        
        string status = "";
        
        // GameModeManager status
        if (GameModeManager.Instance != null)
        {
            status += $"GameModeManager: Exists\n";
            bool isOnlineMode = GameModeManager.Instance.IsOnlineMode;
            status += $"Online Mode: {(isOnlineMode ? "ON" : "OFF")}\n";
            status += $"Debug Online Mode: {(debugOnlineMode ? "ON" : "OFF")}\n";
        }
        else
        {
            status += "GameModeManager: Not Found\n";
            status += $"Debug Online Mode: {(debugOnlineMode ? "ON" : "OFF")}\n";
        }
        
        // NetworkManager status
        if (NetworkManager.Singleton != null)
        {
            status += $"NetworkManager: Exists\n";
            status += $"IsServer: {NetworkManager.Singleton.IsServer}\n";
            status += $"IsHost: {NetworkManager.Singleton.IsHost}\n";
            status += $"IsClient: {NetworkManager.Singleton.IsClient}\n";
            
            // Connected clients count
            int connectedClients = NetworkManager.Singleton.ConnectedClients.Count;
            status += $"Connected Clients: {connectedClients}\n";
            
            // LocalClientId
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
            {
                status += $"LocalClientId: {NetworkManager.Singleton.LocalClientId}\n";
            }
            
            // NetworkPaintCanvas status
            NetworkPaintCanvas networkPaintCanvas = FindObjectOfType<NetworkPaintCanvas>();
            if (networkPaintCanvas != null)
            {
                status += $"NetworkPaintCanvas: Exists\n";
                status += $"IsSpawned: {networkPaintCanvas.IsSpawned}\n";
                status += $"LocalPlayerId: {networkPaintCanvas.GetLocalPlayerId()}\n";
            }
            else
            {
                status += $"NetworkPaintCanvas: Not Found\n";
            }
            
            // NetworkPaintCanvasSpawner status
            NetworkPaintCanvasSpawner spawner = FindObjectOfType<NetworkPaintCanvasSpawner>();
            if (spawner != null)
            {
                status += $"NetworkPaintCanvasSpawner: Exists\n";
            }
            else
            {
                status += $"NetworkPaintCanvasSpawner: Not Found\n";
            }
        }
        else
        {
            status += "NetworkManager: Not Found\n";
        }
        
        statusText.text = status;
    }
}
