using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ネットワークテスト用の一時スクリプト
/// 第1段階のテスト用（後で削除可能）
/// </summary>
public class NetworkTestHelper : MonoBehaviour
{
    [Header("Test Buttons")]
    [Tooltip("ホストとして開始するボタン（Inspectorで設定、オプション）")]
    [SerializeField] private Button hostButton;
    
    [Tooltip("クライアントとして接続するボタン（Inspectorで設定、オプション）")]
    [SerializeField] private Button clientButton;
    
    [Tooltip("切断するボタン（Inspectorで設定、オプション）")]
    [SerializeField] private Button disconnectButton;
    
    [Header("Client Settings")]
    [Tooltip("接続先のIPアドレス（デフォルト: localhost）")]
    [SerializeField] private string serverIP = "127.0.0.1";
    
    [Tooltip("接続先のポート番号（デフォルト: 7777）")]
    [SerializeField] private ushort serverPort = 7777;
    
    [Header("Debug")]
    [Tooltip("コンソールに詳細ログを出力するか")]
    [SerializeField] private bool enableDebugLog = true;
    
    void Awake()
    {
        if (enableDebugLog)
        {
            Debug.Log($"NetworkTestHelper: Awake() が呼ばれました - GameObject: {gameObject.name}, Enabled: {enabled}");
        }
    }
    
    void Start()
    {
        if (enableDebugLog)
        {
            Debug.Log("NetworkTestHelper: Start() が呼ばれました");
        }
        
        // ボタンのイベントを設定
        if (hostButton != null)
        {
            hostButton.onClick.AddListener(StartHost);
            if (enableDebugLog)
            {
                Debug.Log("NetworkTestHelper: ホストボタンのイベントを設定しました");
            }
        }
        
        if (clientButton != null)
        {
            clientButton.onClick.AddListener(StartClient);
            if (enableDebugLog)
            {
                Debug.Log("NetworkTestHelper: クライアントボタンのイベントを設定しました");
            }
        }
        
        if (disconnectButton != null)
        {
            disconnectButton.onClick.AddListener(Disconnect);
            if (enableDebugLog)
            {
                Debug.Log("NetworkTestHelper: 切断ボタンのイベントを設定しました");
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log("NetworkTestHelper: 初期化完了 - Hキー: ホスト開始, Cキー: クライアント接続, Dキー: 切断");
            Debug.Log($"NetworkTestHelper: 接続先設定 - IP: {serverIP}, Port: {serverPort}");
        }
    }
    
    /// <summary>
    /// ホストとして開始
    /// </summary>
    public void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkTestHelper: NetworkManagerが見つかりません。GameSceneにNetworkManagerを配置してください。");
            return;
        }
        
        // 既に接続されている場合は切断
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("NetworkTestHelper: 既に接続されています。先に切断してください。");
            return;
        }
        
        bool success = NetworkManager.Singleton.StartHost();
        if (success)
        {
            if (enableDebugLog)
            {
                Debug.Log("NetworkTestHelper: ホストとして開始しました");
            }
        }
        else
        {
            Debug.LogError("NetworkTestHelper: ホストの開始に失敗しました");
        }
    }
    
    /// <summary>
    /// クライアントとして接続
    /// </summary>
    public void StartClient()
    {
        if (enableDebugLog)
        {
            Debug.Log("NetworkTestHelper: StartClient() が呼ばれました");
        }
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkTestHelper: NetworkManagerが見つかりません。GameSceneにNetworkManagerを配置してください。");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"NetworkTestHelper: NetworkManagerが見つかりました - IsHost: {NetworkManager.Singleton.IsHost}, IsClient: {NetworkManager.Singleton.IsClient}");
        }
        
        // 既に接続されている場合は切断
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning($"NetworkTestHelper: 既に接続されています (IsHost: {NetworkManager.Singleton.IsHost}, IsClient: {NetworkManager.Singleton.IsClient})。先に切断してください。");
            return;
        }
        
        // Unity TransportのIPアドレスとポートを設定
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = serverIP;
            transport.ConnectionData.Port = serverPort;
            
            if (enableDebugLog)
            {
                Debug.Log($"NetworkTestHelper: 接続先を設定 - IP: {serverIP}, Port: {serverPort}");
            }
        }
        else
        {
            Debug.LogError("NetworkTestHelper: Unity Transportが見つかりません。NetworkManagerにUnity Transportが設定されているか確認してください。");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log("NetworkTestHelper: StartClient() を実行します...");
        }
        
        bool success = NetworkManager.Singleton.StartClient();
        if (success)
        {
            if (enableDebugLog)
            {
                Debug.Log($"NetworkTestHelper: クライアントとして接続しました - IP: {serverIP}, Port: {serverPort}");
            }
        }
        else
        {
            Debug.LogError("NetworkTestHelper: クライアントの接続に失敗しました");
        }
    }
    
    /// <summary>
    /// 切断
    /// </summary>
    public void Disconnect()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("NetworkTestHelper: NetworkManagerが見つかりません");
            return;
        }
        
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
            if (enableDebugLog)
            {
                Debug.Log("NetworkTestHelper: ホストを終了しました");
            }
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            if (enableDebugLog)
            {
                Debug.Log("NetworkTestHelper: クライアントを切断しました");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log("NetworkTestHelper: 接続されていません");
            }
        }
    }
    
    /// <summary>
    /// キーボードショートカット（開発用）
    /// Hキー: ホスト開始
    /// Cキー: クライアント接続
    /// Dキー: 切断
    /// </summary>
    void Update()
    {
        // エディタモードまたはPlayモードで実行
        if (Input.GetKeyDown(KeyCode.H))
        {
            StartHost();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            StartClient();
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            Disconnect();
        }
    }
    
    /// <summary>
    /// 現在の接続状態を表示（デバッグ用）
    /// </summary>
    void OnGUI()
    {
        if (!enableDebugLog) return;
        
        // NetworkTestHelperの状態を表示
        GUI.Label(new Rect(10, 10, 400, 20), $"NetworkTestHelper: {gameObject.name} (Enabled: {enabled})");
        
        if (NetworkManager.Singleton != null)
        {
            string status = "未接続";
            if (NetworkManager.Singleton.IsHost)
            {
                status = $"ホスト (接続数: {NetworkManager.Singleton.ConnectedClients.Count})";
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                status = "クライアント";
            }
            
            GUI.Label(new Rect(10, 30, 400, 20), $"ネットワーク状態: {status}");
            GUI.Label(new Rect(10, 50, 400, 20), $"接続先: {serverIP}:{serverPort}");
            GUI.Label(new Rect(10, 70, 400, 20), "H: ホスト開始, C: クライアント接続, D: 切断");
            
            // Unity Transportの状態を表示
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                GUI.Label(new Rect(10, 90, 400, 20), $"Unity Transport: 設定済み");
            }
            else
            {
                GUI.Label(new Rect(10, 90, 400, 20), $"Unity Transport: 未設定 (エラー)");
            }
        }
        else
        {
            GUI.Label(new Rect(10, 30, 400, 20), "NetworkManager: 見つかりません (エラー)");
        }
    }
}

