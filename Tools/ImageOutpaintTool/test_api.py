import base64
import io
import requests
from PIL import Image

API_KEY = ""  # 请填入你的豆包 API Key
API_URL = "https://ark.cn-beijing.volces.com/api/plan/v3/images/generations"
MODEL = "doubao-seedream-5.0-lite"

def pil_to_bytes(img, fmt="PNG"):
    buf = io.BytesIO()
    img.save(buf, format=fmt)
    return buf.getvalue()

def test_api():
    # Create a simple test image (green square with a red dot)
    img = Image.new("RGBA", (512, 512), (100, 180, 100, 255))
    img.paste((255, 100, 100, 255), (230, 230, 282, 282))

    b64 = base64.b64encode(pil_to_bytes(img)).decode("utf-8")
    payload = {
        "model": MODEL,
        "prompt": "Extend this image to the right, continue the green grassland style.",
        "image": f"data:image/png;base64,{b64}",
        "n": 1,
        "size": "1920x1920",
        "response_format": "b64_json",
    }
    headers = {
        "Authorization": f"Bearer {API_KEY}",
        "Content-Type": "application/json",
    }

    print(f"POST {API_URL}")
    print(f"Payload keys: {payload.keys()}")
    try:
        resp = requests.post(API_URL, headers=headers, json=payload, timeout=120)
        print(f"Status: {resp.status_code}")
        print(f"Response: {resp.text[:500]}")
        if resp.status_code == 200:
            result = resp.json()
            b64_out = result["data"][0]["b64_json"]
            out = Image.open(io.BytesIO(base64.b64decode(b64_out)))
            out.save("test_output.png")
            print(f"Saved test_output.png, size: {out.size}")
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    test_api()
