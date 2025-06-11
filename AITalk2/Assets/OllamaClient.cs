using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class OllamaClient : MonoBehaviour
{
    public TMP_InputField inputField;
    public Button sendButton;
    public TMP_Text responseText;

    public Button yaoButton;
    public Button llamaButton;

    private List<string> chatLog = new List<string>();

    void Start()
    {
        sendButton.onClick.AddListener(OnSendButtonClick);

        // 加入角色切換按鈕監聽
        yaoButton.onClick.AddListener(() => SwitchModel("yao"));
        llamaButton.onClick.AddListener(() => SwitchModel("llama3"));

        StartCoroutine(WaitForServerThenLoad());
    }

    void OnSendButtonClick()
    {
        string prompt = inputField.text;
        if (!string.IsNullOrEmpty(prompt))
        {
            chatLog.Add($"<b>你：</b>{prompt}");
            UpdateChatDisplay();

            StartCoroutine(SendOllamaCMD(prompt));
            inputField.text = "";
        }
    }

    IEnumerator SendOllamaCMD(string prompt)
    {
        string url = "http://127.0.0.1:5000/run_ollama";
        string jsonData = JsonUtility.ToJson(new PromptData(prompt));

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            OllamaResponse result = JsonUtility.FromJson<OllamaResponse>(response);

            if (responseText != null && result.success)
            {
                chatLog.Add($"<b>AI：</b>{result.output.Trim()}");
                UpdateChatDisplay();
            }
        }
        else
        {
            UnityEngine.Debug.LogError("錯誤: " + request.error);
        }
    }

    void UpdateChatDisplay()
    {
        responseText.text = string.Join("\n\n", chatLog);
        Canvas.ForceUpdateCanvases();

        ScrollRect scroll = responseText.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            scroll.verticalNormalizedPosition = 0f;
        }
    }

    IEnumerator WaitForServerThenLoad()
    {
        string url = "http://127.0.0.1:5000/get_history";
        int maxRetries = 10;

        while (maxRetries-- > 0)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                StartCoroutine(LoadHistory());
                yield break;
            }

            UnityEngine.Debug.Log("等待 Flask server 啟動中...");
            yield return new WaitForSeconds(1f);
        }

        UnityEngine.Debug.LogError("無法連接 Flask server，請確認是否正確啟動。");
    }

    IEnumerator LoadHistory()
    {
        string url = "http://127.0.0.1:5000/get_history";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            ChatHistoryResponse chatHistory = JsonUtility.FromJson<ChatHistoryResponse>(json);

            foreach (var entry in chatHistory.history)
            {
                chatLog.Add($"<b>你：</b>{entry.prompt}");
                chatLog.Add($"<b>AI：</b>{entry.response}");
            }

            UpdateChatDisplay();
        }
        else
        {
            UnityEngine.Debug.LogError("載入歷史紀錄失敗: " + request.error);
        }
    }

    // ⭐ 新增：切換角色模型
    void SwitchModel(string model)
    {
        string url = $"http://127.0.0.1:5000/loadNPC/{model}";
        StartCoroutine(SendGetRequest(url, model));
    }

    IEnumerator SendGetRequest(string url, string model)
{
    UnityWebRequest request = UnityWebRequest.Get(url);
    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        chatLog.Clear(); // 清除原本的對話紀錄
        chatLog.Add($"<color=orange><b>（已切換角色為 {model}）</b></color>");
        UpdateChatDisplay();

        // ⭐ 載入新角色對應的歷史
        StartCoroutine(LoadHistory());
    }
    else
    {
        UnityEngine.Debug.LogError($"切換模型 {model} 失敗: " + request.error);
    }
}


    // === 資料結構 ===
    [System.Serializable]
    public class PromptData
    {
        public string prompt;
        public PromptData(string prompt) { this.prompt = prompt; }
    }

    [System.Serializable]
    public class OllamaResponse
    {
        public string output;
        public bool success;
    }

    [System.Serializable]
    public class ChatEntry
    {
        public string prompt;
        public string response;
    }

    [System.Serializable]
    public class ChatHistoryResponse
    {
        public ChatEntry[] history;
        public bool success;
    }
}
