using UnityEngine;

/// <summary>
/// Arrowオブジェクトの表示/非表示を制御するコンポーネント
/// 参照オブジェクトが表示されているとき、Arrowオブジェクトを非表示にします
/// </summary>
public class ArrowVisibilityController : MonoBehaviour
{
    [Header("Arrow Object")]
    [Tooltip("表示/非表示を制御するArrowオブジェクト")]
    [SerializeField] private GameObject arrowObject;
    
    [Header("Reference Objects")]
    [Tooltip("このオブジェクトが表示されているとき、Arrowを非表示にします\n複数のオブジェクトを設定した場合、いずれか1つでも表示されていればArrowが非表示になります")]
    [SerializeField] private GameObject[] referenceObjects;
    
    [Header("Settings")]
    [Tooltip("参照オブジェクトの表示状態をチェックする間隔（秒）\n0の場合は毎フレームチェックします")]
    [SerializeField] private float checkInterval = 0f;
    
    [Tooltip("初期状態でArrowを表示するかどうか")]
    [SerializeField] private bool showArrowInitially = false;
    
    private float lastCheckTime = 0f;
    private bool lastArrowState = false;
    
    void Start()
    {
        // Arrowオブジェクトの自動取得（未設定の場合）
        if (arrowObject == null)
        {
            arrowObject = gameObject;
            Debug.LogWarning("ArrowVisibilityController: Arrowオブジェクトが設定されていません。このGameObjectを使用します。");
        }
        
        // 初期状態を設定
        if (arrowObject != null)
        {
            arrowObject.SetActive(showArrowInitially);
            lastArrowState = showArrowInitially;
        }
        
        // 参照オブジェクトが設定されているか確認
        if (referenceObjects == null || referenceObjects.Length == 0)
        {
            Debug.LogWarning("ArrowVisibilityController: 参照オブジェクトが設定されていません。");
        }
    }
    
    void Update()
    {
        // チェック間隔を考慮
        if (checkInterval > 0f && Time.time - lastCheckTime < checkInterval)
        {
            return;
        }
        
        lastCheckTime = Time.time;
        
        // 参照オブジェクトの表示状態をチェック
        bool isReferenceObjectVisible = CheckReferenceObjectsVisibility();
        
        // 参照オブジェクトが表示されている場合はArrowを非表示、非表示の場合はArrowを表示
        bool shouldShowArrow = !isReferenceObjectVisible;
        
        // Arrowの表示状態を更新（状態が変わった場合のみ）
        if (shouldShowArrow != lastArrowState)
        {
            UpdateArrowVisibility(shouldShowArrow);
            lastArrowState = shouldShowArrow;
        }
    }
    
    /// <summary>
    /// 参照オブジェクトの表示状態をチェック
    /// </summary>
    /// <returns>いずれかの参照オブジェクトが表示されている場合はtrue</returns>
    private bool CheckReferenceObjectsVisibility()
    {
        if (referenceObjects == null || referenceObjects.Length == 0)
        {
            return false;
        }
        
        // いずれかの参照オブジェクトが表示されているかチェック
        foreach (GameObject referenceObject in referenceObjects)
        {
            if (referenceObject != null && referenceObject.activeSelf)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Arrowオブジェクトの表示状態を更新
    /// </summary>
    /// <param name="show">表示する場合はtrue、非表示にする場合はfalse</param>
    private void UpdateArrowVisibility(bool show)
    {
        if (arrowObject != null)
        {
            arrowObject.SetActive(show);
            Debug.Log($"ArrowVisibilityController: Arrowオブジェクトを{(show ? "表示" : "非表示")}にしました。");
        }
    }
    
    /// <summary>
    /// 参照オブジェクトを設定（実行時に動的に変更可能）
    /// </summary>
    /// <param name="objects">参照オブジェクトの配列</param>
    public void SetReferenceObjects(GameObject[] objects)
    {
        referenceObjects = objects;
    }
    
    /// <summary>
    /// Arrowオブジェクトを設定（実行時に動的に変更可能）
    /// </summary>
    /// <param name="arrow">Arrowオブジェクト</param>
    public void SetArrowObject(GameObject arrow)
    {
        arrowObject = arrow;
    }
    
    /// <summary>
    /// 手動でArrowの表示状態を更新（即座に反映）
    /// </summary>
    public void RefreshArrowVisibility()
    {
        bool isReferenceObjectVisible = CheckReferenceObjectsVisibility();
        bool shouldShowArrow = !isReferenceObjectVisible;
        UpdateArrowVisibility(shouldShowArrow);
        lastArrowState = shouldShowArrow;
    }
}

