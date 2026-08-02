"""
Sect Hall Map Stitcher - Flask version with frameronin-style 3x3 grid UI.

Run with:
    python app.py
"""

import base64
import io
import re
import traceback
import uuid
from typing import Dict, Optional

from flask import Flask, jsonify, request, send_file, session, render_template
import requests
from PIL import Image


app = Flask(__name__)
app.secret_key = "sect-hall-map-stitcher-secret-key"


@app.after_request
def disable_caching(response):
    response.headers["Cache-Control"] = "no-cache, no-store, must-revalidate"
    response.headers["Pragma"] = "no-cache"
    response.headers["Expires"] = "0"
    return response


# In-memory session storage: sid -> {"cells": {"r-c": Image.Image}}
SESSIONS: Dict[str, Dict[str, Dict[str, Image.Image]]] = {}

DEFAULT_API_URL = "https://ark.cn-beijing.volces.com/api/plan/v3/images/generations"
DEFAULT_MODEL = "doubao-seedream-5.0-lite"


def get_session() -> Dict[str, Dict[str, Image.Image]]:
    sid = session.get("sid")
    if not sid or sid not in SESSIONS:
        sid = str(uuid.uuid4())
        session["sid"] = sid
        SESSIONS[sid] = {"cells": {}}
    return SESSIONS[sid]


def pil_to_bytes(img: Image.Image, fmt: str = "PNG") -> bytes:
    buf = io.BytesIO()
    img.save(buf, format=fmt)
    return buf.getvalue()


def load_image(file_storage) -> Image.Image:
    return Image.open(file_storage.stream).convert("RGBA")


# ==================== Image generation logic ====================


def create_outpaint_canvas(
    base_img: Image.Image, direction: str, overlap: float
) -> Image.Image:
    """
    Create a canvas with the SAME size as base_img.
    One edge contains a strip from base_img (overlap region); the rest is
    transparent so the AI fills the extension while keeping the layout
    identical to frameronin (all tiles same size as the center tile).
    """
    w, h = base_img.size
    overlap_px_h = int(w * overlap)
    overlap_px_v = int(h * overlap)

    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))

    if direction == "right":
        strip = base_img.crop((w - overlap_px_h, 0, w, h))
        canvas.paste(strip, (0, 0))
    elif direction == "left":
        strip = base_img.crop((0, 0, overlap_px_h, h))
        canvas.paste(strip, (w - overlap_px_h, 0))
    elif direction == "up":
        strip = base_img.crop((0, 0, w, overlap_px_v))
        canvas.paste(strip, (0, h - overlap_px_v))
    elif direction == "down":
        strip = base_img.crop((0, h - overlap_px_v, w, h))
        canvas.paste(strip, (0, 0))
    else:
        raise ValueError(f"Unknown direction: {direction}")
    return canvas


def call_doubao_api(
    image: Image.Image,
    prompt: str,
    api_key: str,
    api_url: str,
    model: str,
    size: str,
) -> Image.Image:
    b64 = base64.b64encode(pil_to_bytes(image)).decode("utf-8")
    payload = {
        "model": model,
        "prompt": prompt,
        "image": f"data:image/png;base64,{b64}",
        "n": 1,
        "size": size,
        "response_format": "b64_json",
    }
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    }

    resp = requests.post(api_url, headers=headers, json=payload, timeout=120)
    resp.raise_for_status()
    result = resp.json()
    b64_out = result["data"][0]["b64_json"]
    return Image.open(io.BytesIO(base64.b64decode(b64_out)))


def create_corner_canvas(
    base_size: tuple[int, int],
    refs: list,
    h_overlap: float,
    v_overlap: float,
) -> Image.Image:
    """
    Create a canvas for a corner tile that references two existing neighbors.

    refs: list of (neighbor_image, edge_to_match) where edge_to_match is the
          edge of the NEW tile that must match the neighbor ('top', 'bottom',
          'left', 'right').
    """
    w, h = base_size
    overlap_px_h = int(w * h_overlap)
    overlap_px_v = int(h * v_overlap)
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))

    for neighbor, edge in refs:
        if edge == "top":
            strip = neighbor.crop((0, h - overlap_px_v, w, h))
            canvas.paste(strip, (0, 0))
        elif edge == "bottom":
            strip = neighbor.crop((0, 0, w, overlap_px_v))
            canvas.paste(strip, (0, h - overlap_px_v))
        elif edge == "left":
            strip = neighbor.crop((w - overlap_px_h, 0, w, h))
            canvas.paste(strip, (0, 0))
        elif edge == "right":
            strip = neighbor.crop((0, 0, overlap_px_h, h))
            canvas.paste(strip, (w - overlap_px_h, 0))
    return canvas


def generate_corner_tile(
    base_size: tuple[int, int],
    refs: list,
    h_overlap: float,
    v_overlap: float,
    prompt: str,
    api_key: str,
    api_url: str,
    model: str,
) -> Image.Image:
    """
    Generate a corner tile that matches two existing orthogonal neighbors.
    """
    canvas = create_corner_canvas(base_size, refs, h_overlap, v_overlap)
    cw, ch = canvas.size

    min_side = min(cw, ch)
    if min_side < 1920:
        scale = 1920.0 / min_side
        cw = int(round(cw * scale))
        ch = int(round(ch * scale))
        cw += cw % 2
        ch += ch % 2
        canvas = canvas.resize((cw, ch), Image.Resampling.LANCZOS)
        size_str = f"{cw}x{ch}"
    else:
        cw += cw % 2
        ch += ch % 2
        size_str = f"{cw}x{ch}"

    edges = {edge for _, edge in refs}
    vertical = []
    if "top" in edges:
        vertical.append("downward")
    if "bottom" in edges:
        vertical.append("upward")
    horizontal = []
    if "left" in edges:
        horizontal.append("to the right")
    if "right" in edges:
        horizontal.append("to the left")
    directions = vertical + horizontal

    direction_hint = ""
    if directions:
        direction_hint = (
            "Extend the image " + " and ".join(directions) + ". "
            "Preserve and seamlessly match the existing "
            + " and ".join(sorted(edges))
            + " edge(s). "
        )
    full_prompt = direction_hint + prompt

    result = call_doubao_api(canvas, full_prompt, api_key, api_url, model, size_str)
    if result.size != base_size:
        result = result.resize(base_size, Image.Resampling.LANCZOS)
    return result


def generate_direction_tile(
    base_img: Image.Image,
    direction: str,
    overlap: float,
    prompt: str,
    api_key: str,
    api_url: str,
    model: str,
) -> Image.Image:
    """
    Generate an outpaint tile that has the SAME pixel size as base_img.
    The generated tile shares an overlapping edge strip with the base image.
    """
    canvas = create_outpaint_canvas(base_img, direction, overlap)
    cw, ch = canvas.size

    min_side = min(cw, ch)
    if min_side < 1920:
        scale = 1920.0 / min_side
        cw = int(round(cw * scale))
        ch = int(round(ch * scale))
        cw += cw % 2
        ch += ch % 2
        canvas = canvas.resize((cw, ch), Image.Resampling.LANCZOS)
        size_str = f"{cw}x{ch}"
    else:
        cw += cw % 2
        ch += ch % 2
        size_str = f"{cw}x{ch}"

    direction_hint = {
        "up": "Extend the image upward. ",
        "down": "Extend the image downward. ",
        "left": "Extend the image to the left. ",
        "right": "Extend the image to the right. ",
    }.get(direction, "")
    full_prompt = direction_hint + prompt

    result = call_doubao_api(canvas, full_prompt, api_key, api_url, model, size_str)
    if result.size != base_img.size:
        result = result.resize(base_img.size, Image.Resampling.LANCZOS)
    return result


def find_parent(cells: Dict[str, Image.Image], row: int, col: int):
    """Return (parent_key, direction) for generating cell (row, col).

    We can only extend in the four cardinal directions, so the target cell
    must share an edge with an existing tile. The first available neighbor
    is used as the parent.
    """
    candidates = [
        ((row - 1, col), "down"),   # tile above extends downward
        ((row + 1, col), "up"),     # tile below extends upward
        ((row, col - 1), "right"),  # tile to the left extends rightward
        ((row, col + 1), "left"),   # tile to the right extends leftward
    ]
    for (pr, pc), direction in candidates:
        key = f"{pr}-{pc}"
        if key in cells:
            return (key, direction)
    return (None, None)


def get_corner_refs(
    cells: Dict[str, Image.Image], row: int, col: int
) -> Optional[list]:
    """Return two orthogonal neighbor references for a corner cell.

    Each reference is (neighbor_image, edge_on_new_tile). Returns None if
    fewer than two neighbors exist (falls back to single-direction generation).
    """
    if row == 0 or col == 0:
        return None

    refs = []
    row_dir = 1 if row > 0 else -1
    col_dir = 1 if col > 0 else -1

    row_key = f"{row - row_dir}-{col}"
    if row_key in cells:
        edge = "top" if row > 0 else "bottom"
        refs.append((cells[row_key], edge))

    col_key = f"{row}-{col - col_dir}"
    if col_key in cells:
        edge = "left" if col > 0 else "right"
        refs.append((cells[col_key], edge))

    return refs if len(refs) == 2 else None


def cell_name(row: int, col: int) -> str:
    if row == 0 and col == 0:
        return "主图"
    parts = []
    if row < 0:
        parts.append(f"上{abs(row)}")
    elif row > 0:
        parts.append(f"下{row}")
    if col < 0:
        parts.append(f"左{abs(col)}")
    elif col > 0:
        parts.append(f"右{col}")
    return "".join(parts) or f"{row}-{col}"


def stitch_preview(
    cells: Dict[str, Image.Image], h_overlap: float, v_overlap: float
) -> Image.Image:
    center = cells.get("0-0")
    if center is None:
        return Image.new("RGBA", (512, 512), (40, 40, 40, 255))

    cw, ch = center.size
    h_step = cw - int(cw * h_overlap)
    v_step = ch - int(ch * v_overlap)

    def parse_key(key: str):
        # key format: "{row}-{col}" where both row and col may be negative.
        m = re.match(r"^(-?\d+)-(-?\d+)$", key)
        if not m:
            return None
        return int(m.group(1)), int(m.group(2))

    positions = {}
    for key in cells:
        parsed = parse_key(key)
        if parsed is None:
            continue
        r, c = parsed
        dx = c * h_step
        dy = r * v_step
        positions[key] = (dx, dy)

    if not positions:
        return Image.new("RGBA", (cw, ch), (30, 30, 30, 255))

    min_x = min(dx for dx, dy in positions.values())
    min_y = min(dy for dx, dy in positions.values())
    max_x = max(dx + cw for dx, dy in positions.values())
    max_y = max(dy + ch for dx, dy in positions.values())

    total_w = max_x - min_x
    total_h = max_y - min_y
    preview = Image.new("RGBA", (total_w, total_h), (30, 30, 30, 255))

    for key, (dx, dy) in positions.items():
        preview.paste(cells[key], (dx - min_x, dy - min_y))

    return preview


def parse_cell_key(key: str):
    """Parse key like '-1--1' into (-1, -1)."""
    m = re.match(r"^(-?\d+)-(-?\d+)$", key)
    if not m:
        return None
    return int(m.group(1)), int(m.group(2))


@app.route("/api/export/<engine>")
def export_tiles(engine: str):
    import zipfile

    data = get_session()
    cells = data["cells"]
    if not cells:
        return jsonify({"error": "No tiles"}), 400

    parsed = {k: parse_cell_key(k) for k in cells}
    parsed = {k: v for k, v in parsed.items() if v is not None}
    if not parsed:
        return jsonify({"error": "No valid tiles"}), 400

    rows = [v[0] for v in parsed.values()]
    cols = [v[1] for v in parsed.values()]
    min_r, min_c = min(rows), min(cols)

    buf = io.BytesIO()
    with zipfile.ZipFile(buf, "w", zipfile.ZIP_DEFLATED) as zf:
        for key, (r, c) in parsed.items():
            if engine == "godot":
                # Zero-based grid coordinates (top-left becomes 0,0)
                name = f"tile_{r - min_r}_{c - min_c}.png"
            elif engine == "unity":
                # Traditional Cartesian coordinates: x = col, y = -row
                # center (0,0), right (1,0), left (-1,0), up (0,1), down (0,-1)
                x = c
                y = -r
                name = f"tile_{x}_{y}.png"
            else:
                return jsonify({"error": "Unknown engine"}), 400
            zf.writestr(name, pil_to_bytes(cells[key]))
    buf.seek(0)
    return send_file(
        buf,
        mimetype="application/zip",
        as_attachment=True,
        download_name=f"{engine}_tiles.zip",
    )


# ==================== Flask routes ====================


@app.route("/")
def index():
    return render_template(
        "index.html",
        default_api_url=DEFAULT_API_URL,
        default_model=DEFAULT_MODEL,
    )


@app.route("/api/clear_all", methods=["POST"])
def clear_all():
    data = get_session()
    data["cells"] = {}
    return jsonify({"ok": True})


@app.route("/api/upload_center", methods=["POST"])
def upload_center():
    if "image" not in request.files:
        return jsonify({"error": "No image"}), 400
    data = get_session()
    img = load_image(request.files["image"])
    # Replace center image but keep any surrounding cells that were already created.
    data["cells"]["0-0"] = img
    return jsonify({"ok": True, "size": img.size})


@app.route("/api/upload_cell", methods=["POST"])
def upload_cell():
    try:
        row = int(request.form.get("row"))
        col = int(request.form.get("col"))
    except (TypeError, ValueError):
        return jsonify({"error": "Invalid row/col"}), 400
    if "image" not in request.files:
        return jsonify({"error": "No image"}), 400

    data = get_session()
    cells = data["cells"]
    center = cells.get("1-1")
    img = load_image(request.files["image"])
    if center is not None:
        img = img.resize(center.size, Image.Resampling.LANCZOS)
    cells[f"{row}-{col}"] = img
    return jsonify({"ok": True, "size": img.size})


@app.route("/api/generate", methods=["POST"])
def generate():
    try:
        row = int(request.form.get("row"))
        col = int(request.form.get("col"))
    except (TypeError, ValueError):
        return jsonify({"error": "Invalid row/col"}), 400

    try:
        h_overlap = float(request.form.get("h_overlap", 0.15))
        v_overlap = float(request.form.get("v_overlap", 0.15))
    except ValueError:
        return jsonify({"error": "Invalid overlap"}), 400

    prompt = request.form.get("prompt", "")
    api_key = request.form.get("api_key", "")
    api_url = request.form.get("api_url", DEFAULT_API_URL)
    model = request.form.get("model", DEFAULT_MODEL)

    if not api_key:
        return jsonify({"error": "API key required"}), 400

    data = get_session()
    cells = data["cells"]
    center = cells.get("0-0")
    if center is None:
        return jsonify({"error": "请先上传主图"}), 400

    parent_key, direction = find_parent(cells, row, col)
    if parent_key is None or direction is None:
        return jsonify({"error": "该位置没有可用的父图，请先生成相邻边块"}), 400

    parent_img = cells.get(parent_key)
    if parent_img is None:
        return jsonify({"error": "父图尚未生成"}), 400

    # Try dual-reference corner generation when two orthogonal neighbors exist.
    refs = get_corner_refs(cells, row, col)
    if refs:
        try:
            result = generate_corner_tile(
                center.size, refs, h_overlap, v_overlap, prompt, api_key, api_url, model
            )
            cells[f"{row}-{col}"] = result
            return jsonify({"ok": True, "size": result.size, "mode": "corner"})
        except Exception as e:
            return jsonify({"error": str(e), "trace": traceback.format_exc()}), 500

    overlap = h_overlap if direction in ("left", "right") else v_overlap

    try:
        result = generate_direction_tile(
            parent_img, direction, overlap, prompt, api_key, api_url, model
        )
        cells[f"{row}-{col}"] = result
        return jsonify({"ok": True, "size": result.size, "parent": parent_key})
    except Exception as e:
        return jsonify({"error": str(e), "trace": traceback.format_exc()}), 500


@app.route("/api/clear_cell", methods=["POST"])
def clear_cell():
    try:
        row = int(request.form.get("row"))
        col = int(request.form.get("col"))
    except (TypeError, ValueError):
        return jsonify({"error": "Invalid row/col"}), 400
    data = get_session()
    cells = data["cells"]
    key = f"{row}-{col}"
    if key in cells and key != "1-1":
        del cells[key]
    return jsonify({"ok": True})


@app.route("/api/image/<string:row>/<string:col>")
def get_image(row, col):
    try:
        r, c = int(row), int(col)
    except ValueError:
        return "", 404
    data = get_session()
    key = f"{r}-{c}"
    img = data["cells"].get(key)
    if img is None:
        return "", 404
    return send_file(io.BytesIO(pil_to_bytes(img)), mimetype="image/png")


@app.route("/api/combined")
def combined():
    try:
        h_overlap = float(request.args.get("h_overlap", 0.15))
        v_overlap = float(request.args.get("v_overlap", 0.15))
    except ValueError:
        h_overlap = v_overlap = 0.15
    data = get_session()
    preview = stitch_preview(data["cells"], h_overlap, v_overlap)
    return send_file(io.BytesIO(pil_to_bytes(preview)), mimetype="image/png")


if __name__ == "__main__":
    app.run(host="127.0.0.1", port=7860, debug=False)
