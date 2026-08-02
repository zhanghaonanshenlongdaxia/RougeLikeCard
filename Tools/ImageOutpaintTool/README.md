# 宗门大厅扩图工具 / Sect Hall Outpaint Tool

一个本地运行的网页小工具，用于把一张中心房间图往上下左右四个方向做 AI 扩图（outpainting），方便快速生成修仙宗门大厅的大场景美术资源。

## 环境要求

- Python 3.9+
- 已安装豆包（火山引擎 Ark）API key

## 安装

```bash
cd Tools/ImageOutpaintTool
pip install -r requirements.txt
```

## 运行

```bash
python app.py
```

运行后会自动打开浏览器，访问 `http://127.0.0.1:7860`。

## 使用步骤

1. **上传中心图**：把你生成的宗门庭院图拖进去（比如豆包生成的 1024x1024 俯视图）。
2. **选择扩展方向**：勾选上 / 下 / 左 / 右。
3. **调整扩展比例**：默认 0.3，表示每个方向扩展原图 30% 的大小。
4. **填写 Prompt**：描述你想要扩展出的内容（默认是竹林、溪流、石板路）。
5. **配置 API**：
   - API Key：你的豆包 key（默认已填）。
   - API Endpoint：默认是 `https://ark.cn-beijing.volces.com/api/plan/v3/images/edits`，如果豆包实际接口不同请修改。
   - Model ID：换成你在豆包控制台看到的**图生图模型名**。
6. 点击 **Generate**，等待每个方向生成完成。
7. 下载单方向结果或 Combined Preview。

## 导入 Unity

1. 把生成的 `Combined Preview` 或单方向图片保存为 PNG。
2. 拖进 Unity `Assets/` 下的某个文件夹（比如 `Assets/Textures/SectHall/`）。
3. 设置 Texture Type 为 `Sprite (2D and UI)`。
4. 作为背景 Sprite 放到宗门大厅场景里，或切成 Tilemap 使用。

## 常见问题

### 1. 返回 400 / model not found

豆包的图生图模型名和 endpoint 可能与脚本默认值不同。请到火山引擎控制台确认：
- 你的模型 ID（例如 `doubao-seedream-3-0-i2i-250115`）
- 图像编辑接口的真实 URL

### 2. 返回 size not supported

脚本会自动把 canvas 缩放到最近的支持尺寸，但最好的做法是你手动控制扩展比例，使目标尺寸接近 256x256、512x512、1024x1024、1024x1792、1792x1024 等豆包支持的尺寸。

### 3. 生成结果和原图风格不一致 / 接缝明显

- 在 prompt 里更具体地描述风格，例如 `pixel art, top-down view, xianxia cultivation theme, green bamboo forest, blue stream`。
- 降低单次扩展比例，分多次小步扩展。
- 在 Unity 里用装饰物（竹子、石头、云雾）遮盖接缝。

### 4. 我想一次扩展多个方向但拼接不完美

目前工具会分别生成四个方向的结果和一个简单的组合预览。由于 AI 扩图的随机性，**最终无缝大图通常需要手动在 Photoshop / Unity 中微调拼接**。这个工具的定位是快速产出素材，不是一键产出最终成品。

## 安全提示

- 这个工具只在本地运行，API key 不会上传到任何第三方服务器。
- 不要把 `app.py` 或截图中的 key 提交到公共仓库。
