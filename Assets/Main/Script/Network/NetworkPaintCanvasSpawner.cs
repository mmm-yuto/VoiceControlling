using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetworkPaintCanvasを自動的にSpawnする
/// Inspectorで設定したGameObjectをSpawnする
/// </summary>
public class NetworkPaintCanvasSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("SpawnするGameObject（Inspectorで設定、NetworkObjectコンポーネントが必要）")]
    [SerializeField] private GameObject targetGameObject;
    
    void Start()
    {
        // オンラインモード時のみ実行
        if (GameModeManager.Instance == null || !GameModeManager.Instance.IsOnlineMode)
        {
            return;
        }
        
        // GameObjectが設定されていない場合は警告
        if (targetGameObject == null)
        {
            Debug.LogWarning("NetworkPaintCanvasSpawner: Target GameObjectが設定されていません");
            return;
        }
        
        // NetworkObjectコンポーネントの確認
        NetworkObject networkObject = targetGameObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"NetworkPaintCanvasSpawner: {targetGameObject.name}にNetworkObjectコンポーネントがありません");
            return;
        }
        
        // ネットワーク接続を待つ
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
    }
    
    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
    
    private void OnServerStarted()
    {
        // サーバー（ホスト）が起動したときにSpawn
        SpawnTargetGameObject();
    }
    
    private void OnClientConnected(ulong clientId)
    {
        // クライアントが接続したときにSpawn（自分自身が接続したとき）
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SpawnTargetGameObject();
        }
    }
    
    private void SpawnTargetGameObject()
    {
        if (targetGameObject == null || NetworkManager.Singleton == null)
        {
            return;
        }
        
        // NetworkObjectを取得
        NetworkObject networkObject = targetGameObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"NetworkPaintCanvasSpawner: {targetGameObject.name}にNetworkObjectコンポーネントがありません");
            return;
        }
        
        // 既にSpawnされている場合はスキップ
        if (networkObject.IsSpawned)
        {
            Debug.Log($"NetworkPaintCanvasSpawner: {targetGameObject.name}は既にSpawnされています");
            return;
        }
        
        // Spawn実行
        if (NetworkManager.Singleton.IsServer)
        {
            // サーバー側でSpawn（全クライアントに同期される）
            networkObject.Spawn();
            Debug.Log($"NetworkPaintCanvasSpawner: {targetGameObject.name}をSpawnしました");
        }
        else
        {
            Debug.LogWarning("NetworkPaintCanvasSpawner: サーバーではないため、Spawnできません");
        }
    }
}
