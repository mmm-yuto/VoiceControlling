using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// デバッグ用: クライアントの描画情報を画面に表示
/// </summary>
public class NetworkPaintDebugUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("デバッグメッセージを表示するTextMeshProUGUI（Inspectorで設定）")]
    [SerializeField] private TextMeshProUGUI debugText;
    
    [Header("Settings")]
    [Tooltip("表示するメッセージの最大数")]
    [SerializeField] private int maxMessages = 5;
    
    [Tooltip("メッセージの表示時間（秒）")]
    [SerializeField] private float messageDisplayTime = 3f;
    
    // メッセージのキュー
    private Queue<DebugMessage> messageQueue = new Queue<DebugMessage>();
    
    // メッセージデータ
    private struct DebugMessage
    {
        public string text;
        public float displayTime;
    }
    
    void Awake()
    {
        // TextMeshProUGUIの自動検索（未設定の場合）
        if (debugText == null)
        {
            debugText = GetComponent<TextMeshProUGUI>();
            if (debugText == null)
            {
                // 子オブジェクトから検索
                debugText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
    }
    
    void Update()
    {
        // 古いメッセージを削除
        while (messageQueue.Count > 0)
        {
            var message = messageQueue.Peek();
            if (Time.time - message.displayTime > messageDisplayTime)
            {
                messageQueue.Dequeue();
            }
            else
            {
                break;
            }
        }
        
        // UIテキストを更新
        UpdateDisplayText();
    }
    
    /// <summary>
    /// デバッグメッセージを追加（ClientRpcから呼ばれる）
    /// </summary>
    public void AddDebugMessage(int playerId, Color color)
    {
        string colorString = $"RGB({color.r:F2}, {color.g:F2}, {color.b:F2})";
        string message = $"Client Player {playerId} tried to paint with color {colorString}";
        
        messageQueue.Enqueue(new DebugMessage
        {
            text = message,
            displayTime = Time.time
        });
        
        // 最大数を超えた場合は古いメッセージを削除
        while (messageQueue.Count > maxMessages)
        {
            messageQueue.Dequeue();
        }
        
        // 即座に表示を更新
        UpdateDisplayText();
    }
    
    /// <summary>
    /// 表示テキストを更新
    /// </summary>
    private void UpdateDisplayText()
    {
        if (debugText == null) return;
        
        if (messageQueue.Count == 0)
        {
            debugText.text = "";
            return;
        }
        
        // メッセージを結合
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var message in messageQueue)
        {
            sb.AppendLine(message.text);
        }
        
        debugText.text = sb.ToString();
    }
    
    /// <summary>
    /// 全メッセージをクリア
    /// </summary>
    public void ClearMessages()
    {
        messageQueue.Clear();
        if (debugText != null)
        {
            debugText.text = "";
        }
    }
}
