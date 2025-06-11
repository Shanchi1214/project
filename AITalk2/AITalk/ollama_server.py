import json
import os
from flask import Flask, request, jsonify
import subprocess

app = Flask(__name__)
HISTORY_FILE = "chat_history.json"
#cd C:\Users\user\OneDrive\桌面\ python ollama_server.py"
# 初始化歷史紀錄檔案（如果不存在）
if not os.path.exists(HISTORY_FILE):
    with open(HISTORY_FILE, "w", encoding="utf-8") as f:
        json.dump([], f, ensure_ascii=False, indent=2)

@app.route("/run_ollama", methods=["POST"])
def run_ollama():
    data = request.get_json()
    prompt = data.get("prompt")
    print(f"收到的 prompt 是：{prompt}")

    if not prompt:
        return jsonify({"error": "No prompt received", "success": False})

    cmd = f'ollama run yao "{prompt}"'

    try:
        result = subprocess.check_output(cmd, shell=True, text=True, encoding='utf-8')

        # 儲存對話到 JSON 檔案
        with open(HISTORY_FILE, "r+", encoding="utf-8") as f:
            history = json.load(f)
            history.append({"prompt": prompt, "response": result.strip()})
            f.seek(0)
            json.dump(history, f, ensure_ascii=False, indent=2)
            f.truncate()

        return jsonify({"output": result, "success": True})
    except subprocess.CalledProcessError as e:
        return jsonify({"output": e.output, "success": False, "error": str(e)})

@app.route("/get_history", methods=["GET"])
def get_history():
    with open(HISTORY_FILE, "r", encoding="utf-8") as f:
        history = json.load(f)
    return jsonify({"history": history, "success": True})

if __name__ == "__main__":
    app.run(host="127.0.0.1", port=5000)
