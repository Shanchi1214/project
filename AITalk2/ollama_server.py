import json
import os
from flask import Flask, request, jsonify
import requests

app = Flask(__name__)

CURRENT_MODEL_FILE = "current_model.txt"
OLLAMA_API_URL = "http://localhost:11434/api/chat"
DEFAULT_MODEL = "yao"

# 獲取目前模型名稱
def get_current_model():
    if not os.path.exists(CURRENT_MODEL_FILE):
        with open(CURRENT_MODEL_FILE, "w", encoding="utf-8") as f:
            f.write(DEFAULT_MODEL)
        return DEFAULT_MODEL
    with open(CURRENT_MODEL_FILE, "r", encoding="utf-8") as f:
        return f.read().strip()

# 根據模型名稱回傳歷史紀錄檔案名
def get_history_filename(model_name):
    return f"chat_history_{model_name}.json"

# 讀取歷史
def load_history(model_name):
    filename = get_history_filename(model_name)
    if not os.path.exists(filename):
        return []
    with open(filename, "r", encoding="utf-8") as f:
        return json.load(f)

# 儲存歷史
def save_history(model_name, history):
    filename = get_history_filename(model_name)
    with open(filename, "w", encoding="utf-8") as f:
        json.dump(history, f, ensure_ascii=False, indent=2)

# 構建 chat message 格式
def build_message_history(model_name):
    history = load_history(model_name)
    messages = []
    for entry in history:
        messages.append({"role": "user", "content": entry["prompt"]})
        messages.append({"role": "assistant", "content": entry["response"]})
    return messages

# 切換模型
@app.route("/loadNPC/<model>", methods=["GET"])
def load_npc(model):
    with open(CURRENT_MODEL_FILE, "w", encoding="utf-8") as f:
        f.write(model)
    return jsonify({"message": f"模型 {model} 已載入", "success": True})

# 發送 prompt 並儲存歷史
@app.route("/run_ollama", methods=["POST"])
def run_ollama():
    data = request.get_json()
    prompt = data.get("prompt")
    if not prompt:
        return jsonify({"error": "No prompt received", "success": False})

    model = get_current_model()
    messages = build_message_history(model)
    messages.append({"role": "user", "content": prompt})

    payload = {
        "model": model,
        "messages": messages,
        "stream": False
    }

    try:
        response = requests.post(OLLAMA_API_URL, json=payload)
        response.raise_for_status()
        result = response.json()["message"]["content"].strip()

        history = load_history(model)
        history.append({"prompt": prompt, "response": result})
        save_history(model, history)

        return jsonify({"output": result, "success": True})
    except requests.RequestException as e:
        return jsonify({"output": "", "success": False, "error": str(e)})

# 回傳目前模型的歷史
@app.route("/get_history", methods=["GET"])
def get_history():
    model = get_current_model()
    history = load_history(model)
    return jsonify({"history": history, "success": True})

if __name__ == "__main__":
    app.run(host="127.0.0.1", port=5000)
