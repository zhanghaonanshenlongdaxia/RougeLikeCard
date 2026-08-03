"""
Sect Hall Map Stitcher - Flask version with frameronin-style 3x3 grid UI.

Run with:
    python app.py
"""

import base64
import io
import os
import re
import traceback
import uuid
from typing import Dict, Optional

from flask import Flask, jsonify, request, send_file, session, render_template
import numpy as np
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

DEFAULT_API_URL = "https://ark.cn-beijing.volces.com/api/v3/images/generations"
DEFAULT_MODEL = "doubao-seedream-4-0-250828"


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


def remove_black_background(img: Image.Image, threshold: int = 20) -> Image.Image:
    """Make near-black pixels transparent. Used for the object layer."""
    rgba = img.convert("RGBA")
    arr = np.array(rgba)
    black_mask = (
        (arr[:, :, 0] < threshold)
        & (arr[:, :, 1] < threshold)
        & (arr[:, :, 2] < threshold)
    )
    arr[black_mask] = [0, 0, 0, 0]
    return Image.fromarray(arr)


def cell_storage_key(row: int, col: int, layer: str = "full") -> str:
    """Return the session key for a cell layer."""
    if layer == "full" or not layer:
        return f"{row}-{col}"
    return f"{row}-{col}-{layer}"


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


def raise_with_body(resp: requests.Response) -> None:
    """Like resp.raise_for_status() but includes Doubao's error message body."""
    try:
        resp.raise_for_status()
    except requests.HTTPError:
        detail = resp.text[:800] if resp.text else "(empty body)"
        raise requests.HTTPError(
            f"{resp.status_code} from image API: {detail}", response=resp
        )


def call_doubao_text_api(
    prompt: str,
    api_key: str,
    api_url: str,
    model: str,
    size: str,
) -> Image.Image:
    """Text-to-image generation (no input image)."""
    payload = {
        "model": model,
        "prompt": prompt,
        "n": 1,
        "size": size,
        "response_format": "b64_json",
        "watermark": False,
    }
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    }

    resp = requests.post(api_url, headers=headers, json=payload, timeout=120)
    raise_with_body(resp)
    result = resp.json()
    b64_out = result["data"][0]["b64_json"]
    return Image.open(io.BytesIO(base64.b64decode(b64_out)))


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
        "watermark": False,
    }
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    }

    resp = requests.post(api_url, headers=headers, json=payload, timeout=120)
    raise_with_body(resp)
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

    layer = request.args.get("layer", "full")
    if layer not in ("full", "ground", "object"):
        layer = "full"

    if layer == "full":
        layer_cells = {
            k: v
            for k, v in cells.items()
            if "-" in k and not k.endswith(("-ground", "-object"))
        }
    else:
        suffix = f"-{layer}"
        layer_cells = {}
        for k, v in cells.items():
            if k.endswith(suffix):
                base_key = k[: -len(suffix)]
                layer_cells[base_key] = v

    parsed = {k: parse_cell_key(k) for k in layer_cells}
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
                name = f"tile_{r - min_r}_{c - min_c}.png"
            elif engine == "unity":
                x = c
                y = -r
                name = f"tile_{x}_{y}.png"
            else:
                return jsonify({"error": "Unknown engine"}), 400
            zf.writestr(name, pil_to_bytes(layer_cells[key]))
    buf.seek(0)
    layer_suffix = {"full": "", "ground": "_ground", "object": "_object"}[layer]
    return send_file(
        buf,
        mimetype="application/zip",
        as_attachment=True,
        download_name=f"{engine}_tiles{layer_suffix}.zip",
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


@app.route("/api/generate_center", methods=["POST"])
def generate_center():
    prompt = request.form.get("prompt", "")
    api_key = request.form.get("api_key", "")
    api_url = request.form.get("api_url", DEFAULT_API_URL)
    model = request.form.get("model", DEFAULT_MODEL)

    try:
        width = int(request.form.get("width", 2848))
        height = int(request.form.get("height", 1600))
    except (TypeError, ValueError):
        return jsonify({"error": "Invalid size"}), 400

    if not api_key:
        return jsonify({"error": "API key required"}), 400
    if not prompt:
        return jsonify({"error": "Prompt required"}), 400

    # Doubao Seedream size limits: total pixels in [1280x720, 4096x4096],
    # aspect ratio within [1/16, 16]. seedream-5.0-lite needs >= 2560x1440.
    if not (256 <= width <= 4096 and 256 <= height <= 4096):
        return jsonify({"error": f"尺寸单边需在 256~4096 之间，当前 {width}x{height}"}), 400
    pixels = width * height
    if pixels < 1280 * 720:
        return jsonify({"error": f"总像素 {pixels} 低于下限 1280x720（5.0-lite 需 ≥ 2560x1440）"}), 400
    if pixels > 4096 * 4096:
        return jsonify({"error": f"总像素 {pixels} 超过上限 4096x4096"}), 400
    if max(width, height) / min(width, height) > 16:
        return jsonify({"error": "宽高比不能超过 16:1"}), 400

    # Ensure even dimensions for the API.
    width += width % 2
    height += height % 2
    size_str = f"{width}x{height}"

    full_prompt = (
        f"{prompt} "
        "Top-down tile-based game map tile. "
        "Fill the entire image edge to edge with playable map content. "
        "Orthographic bird's eye view, grid-aligned, clean readable tiles, "
        "consistent art style, vibrant but coherent palette. "
        "No black borders, no empty space, no vignette, no watermark, "
        "no text, no UI, no perspective distortion. "
        "Suitable as a center tile for seamless map extension."
    )

    try:
        result = call_doubao_text_api(full_prompt, api_key, api_url, model, size_str)
        # Replacing the center clears any previously generated surrounding tiles
        # to avoid size mismatches.
        data = get_session()
        data["cells"] = {cell_storage_key(0, 0, "full"): result}
        return jsonify({"ok": True, "size": result.size})
    except Exception as e:
        return jsonify({"error": str(e), "trace": traceback.format_exc()}), 500


@app.route("/api/upload_center", methods=["POST"])
def upload_center():
    if "image" not in request.files:
        return jsonify({"error": "No image"}), 400
    layer = request.form.get("layer", "full")
    if layer not in ("full", "ground", "object"):
        layer = "full"
    data = get_session()
    img = load_image(request.files["image"])
    key = cell_storage_key(0, 0, layer)
    data["cells"][key] = img
    # Keep existing surrounding cells when replacing center.
    return jsonify({"ok": True, "size": img.size, "layer": layer})


@app.route("/api/upload_cell", methods=["POST"])
def upload_cell():
    try:
        row = int(request.form.get("row"))
        col = int(request.form.get("col"))
    except (TypeError, ValueError):
        return jsonify({"error": "Invalid row/col"}), 400
    if "image" not in request.files:
        return jsonify({"error": "No image"}), 400

    layer = request.form.get("layer", "full")
    if layer not in ("full", "ground", "object"):
        layer = "full"

    data = get_session()
    cells = data["cells"]
    center = cells.get("0-0")
    img = load_image(request.files["image"])
    if center is not None:
        img = img.resize(center.size, Image.Resampling.LANCZOS)
    key = cell_storage_key(row, col, layer)
    cells[key] = img
    return jsonify({"ok": True, "size": img.size, "layer": layer})


@app.route("/api/import_tiles", methods=["POST"])
def import_tiles():
    """Batch import tiles from uploaded files.

    Expects filenames like tile_x_y.png where x=col, y=-row (Unity style).
    Creates all necessary cells in the session.
    """
    layer = request.form.get("layer", "full")
    if layer not in ("full", "ground", "object"):
        layer = "full"

    files = request.files.getlist("files")
    if not files:
        return jsonify({"error": "No files"}), 400

    data = get_session()
    cells = data["cells"]
    center = cells.get("0-0")
    center_size = center.size if center is not None else None

    placed = []
    for file in files:
        # webkitdirectory uploads send relative paths like "folder/tile_0_0.png"
        filename = os.path.basename(file.filename or "")
        m = re.match(r"tile_(-?\d+)_(-?\d+)\.png$", filename, re.IGNORECASE)
        if not m:
            continue
        x = int(m.group(1))
        y = int(m.group(2))
        row = -y
        col = x

        img = load_image(file)
        if center_size is None:
            center_size = img.size
        elif img.size != center_size:
            img = img.resize(center_size, Image.Resampling.LANCZOS)

        key = cell_storage_key(row, col, layer)
        cells[key] = img
        placed.append({"row": row, "col": col, "filename": filename})

    if not placed:
        return jsonify({"error": "No valid tile_x_y.png files found"}), 400

    return jsonify({"ok": True, "placed": placed, "size": center_size})


NEIGHBOR_DIRECTIONS = [
    ((-1, 0), "down"),   # neighbor above extends down, matches target top edge
    ((1, 0), "up"),      # neighbor below extends up, matches target bottom edge
    ((0, -1), "right"),  # neighbor left extends right, matches target left edge
    ((0, 1), "left"),    # neighbor right extends left, matches target right edge
]


def find_all_neighbors(
    cells: Dict[str, Image.Image], row: int, col: int, layer: str = "full"
) -> list:
    """Return all existing orthogonal neighbors as [(image, direction)]."""
    refs = []
    for (dr, dc), direction in NEIGHBOR_DIRECTIONS:
        key = cell_storage_key(row + dr, col + dc, layer)
        if key in cells:
            refs.append((cells[key], direction))
    return refs


def create_multi_direction_canvas(
    base_size: tuple[int, int],
    refs: list,
    h_overlap: float,
    v_overlap: float,
) -> Image.Image:
    """
    Create a canvas for a tile using overlap strips from all given neighbors.
    refs: list of (neighbor_image, direction) where direction describes how the
          neighbor extends relative to the target tile.
    """
    w, h = base_size
    overlap_px_h = int(w * h_overlap)
    overlap_px_v = int(h * v_overlap)
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))

    for neighbor, direction in refs:
        if direction == "down":  # neighbor above, paste its bottom strip to top
            strip = neighbor.crop((0, h - overlap_px_v, w, h))
            canvas.paste(strip, (0, 0))
        elif direction == "up":  # neighbor below, paste its top strip to bottom
            strip = neighbor.crop((0, 0, w, overlap_px_v))
            canvas.paste(strip, (0, h - overlap_px_v))
        elif direction == "right":  # neighbor left, paste its right strip to left
            strip = neighbor.crop((w - overlap_px_h, 0, w, h))
            canvas.paste(strip, (0, 0))
        elif direction == "left":  # neighbor right, paste its left strip to right
            strip = neighbor.crop((0, 0, overlap_px_h, h))
            canvas.paste(strip, (w - overlap_px_h, 0))
    return canvas


def apply_edge_strips(
    result: Image.Image,
    strips_canvas: Image.Image,
    base_size: tuple[int, int],
    refs: list,
    h_overlap: float,
    v_overlap: float,
) -> Image.Image:
    """Force the generated tile's overlap regions to match the neighbor
    strips pixel-exactly at the outer edge, feathering inward to the
    generated content. The model redraws strips with small offsets; this
    pass guarantees seamless joints when tiles are stitched."""
    w, h = base_size
    overlap_px_h = int(w * h_overlap)
    overlap_px_v = int(h * v_overlap)

    res = np.array(result.convert("RGBA")).astype(np.float32)
    strips = np.array(strips_canvas).astype(np.float32)

    mask = np.zeros((h, w), dtype=np.float32)
    directions = {d for _, d in refs}
    if "right" in directions:  # strip at left edge
        grad = 1.0 - np.arange(overlap_px_h, dtype=np.float32) / overlap_px_h
        mask[:, :overlap_px_h] = np.maximum(mask[:, :overlap_px_h], grad[None, :])
    if "left" in directions:  # strip at right edge
        grad = 1.0 - np.arange(overlap_px_h, dtype=np.float32) / overlap_px_h
        mask[:, w - overlap_px_h:] = np.maximum(mask[:, w - overlap_px_h:], grad[None, ::-1])
    if "down" in directions:  # strip at top edge
        grad = 1.0 - np.arange(overlap_px_v, dtype=np.float32) / overlap_px_v
        mask[:overlap_px_v, :] = np.maximum(mask[:overlap_px_v, :], grad[:, None])
    if "up" in directions:  # strip at bottom edge
        grad = 1.0 - np.arange(overlap_px_v, dtype=np.float32) / overlap_px_v
        mask[h - overlap_px_v:, :] = np.maximum(mask[h - overlap_px_v:, :], grad[::-1, None])

    strip_alpha = (strips[:, :, 3] > 0).astype(np.float32)
    m = (mask * strip_alpha)[..., None]
    out = res * (1.0 - m) + strips * m
    return Image.fromarray(out.astype(np.uint8), "RGBA")


def generate_multi_direction_tile(
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
    Generate a tile that matches all provided orthogonal neighbors.
    Works for 1-4 neighbors.
    """
    strips_canvas = create_multi_direction_canvas(
        base_size, refs, h_overlap, v_overlap
    )
    # Transparent canvas: the model reliably fills empty areas with new
    # content (flat or blurred fills get preserved/copied by this model).
    # Seam alignment is handled afterwards by apply_edge_strips.
    canvas = strips_canvas
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

    directions = {d for _, d in refs}

    if len(refs) == 4:
        direction_hint = (
            "The sharp strips along the top, bottom, left and right edges are "
            "reference content copied from the adjacent map tiles. Keep those "
            "edge strips exactly unchanged. Fill the empty transparent area "
            "in the middle with brand new, sharp, fully detailed map content "
            "that continues the edge strips seamlessly. Invent new varied "
            "terrain and objects; do not copy, clone or repeat content from "
            "the edge strips. "
        )
    else:
        expand_parts = []
        edge_parts = []
        if "down" in directions:
            expand_parts.append("downward")
            edge_parts.append("top edge")
        if "up" in directions:
            expand_parts.append("upward")
            edge_parts.append("bottom edge")
        if "right" in directions:
            expand_parts.append("to the right")
            edge_parts.append("left edge")
        if "left" in directions:
            expand_parts.append("to the left")
            edge_parts.append("right edge")

        expand_text = " and ".join(expand_parts)
        edge_text = " and ".join(edge_parts)
        direction_hint = (
            f"The sharp strip along the {edge_text} is reference content "
            f"copied from the adjacent map tile. Keep it exactly unchanged. "
            f"Fill the empty transparent area with brand new, sharp, fully "
            f"detailed map content, extending the scene {expand_text} "
            f"seamlessly. Invent new varied terrain and objects; do not "
            f"copy, clone or repeat content from the edge strip. "
        )

    full_prompt = direction_hint + prompt
    result = call_doubao_api(canvas, full_prompt, api_key, api_url, model, size_str)
    if result.size != base_size:
        result = result.resize(base_size, Image.Resampling.LANCZOS)
    # Pin the overlap regions to the original neighbor strips (feathered)
    # so stitched seams line up pixel-perfectly.
    result = apply_edge_strips(
        result, strips_canvas, base_size, refs, h_overlap, v_overlap
    )
    return result


def generate_layer_tile(
    full_img: Image.Image,
    layer: str,
    prompt: str,
    api_key: str,
    api_url: str,
    model: str,
) -> Image.Image:
    """
    Generate a ground or object layer from an existing full tile.
    Object layers are generated against a black background and then have
    that background removed.
    """
    cw, ch = full_img.size
    cw += cw % 2
    ch += ch % 2
    size_str = f"{cw}x{ch}"

    if layer == "ground":
        prefix = (
            "Ground layer only. Keep only terrain, ground, grass, river, "
            "stone path, floor tiles, soil. Remove all buildings, trees, rocks, "
            "decorations, bridges, furniture and objects. "
        )
    elif layer == "object":
        prefix = (
            "Object layer only. Keep only buildings, trees, rocks, bridges, "
            "decorations, furniture and props. Replace all ground, grass, river, "
            "stone path, floor tiles, soil with solid black background. "
        )
    else:
        prefix = ""

    full_prompt = prefix + prompt
    result = call_doubao_api(full_img, full_prompt, api_key, api_url, model, size_str)
    if result.size != full_img.size:
        result = result.resize(full_img.size, Image.Resampling.LANCZOS)

    if layer == "object":
        result = remove_black_background(result)
    return result


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

    layer = request.form.get("layer", "full")
    if layer not in ("full", "ground", "object"):
        layer = "full"

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

    if layer != "full":
        full_img = cells.get(cell_storage_key(row, col, "full"))
        if full_img is None:
            return jsonify({"error": "请先生成整体层，再分层"}), 400
        try:
            result = generate_layer_tile(
                full_img, layer, prompt, api_key, api_url, model
            )
            cells[cell_storage_key(row, col, layer)] = result
            return jsonify({"ok": True, "size": result.size, "layer": layer})
        except Exception as e:
            return jsonify({"error": str(e), "trace": traceback.format_exc()}), 500

    # Full layer generation: use all existing orthogonal neighbors as references.
    refs = find_all_neighbors(cells, row, col, "full")
    if not refs:
        return jsonify({"error": "该位置没有可用的父图，请先生成相邻边块"}), 400

    try:
        result = generate_multi_direction_tile(
            center.size, refs, h_overlap, v_overlap, prompt, api_key, api_url, model
        )
        cells[cell_storage_key(row, col, "full")] = result
        return jsonify({"ok": True, "size": result.size, "neighbors": len(refs)})
    except Exception as e:
        return jsonify({"error": str(e), "trace": traceback.format_exc()}), 500


@app.route("/api/clear_cell", methods=["POST"])
def clear_cell():
    try:
        row = int(request.form.get("row"))
        col = int(request.form.get("col"))
    except (TypeError, ValueError):
        return jsonify({"error": "Invalid row/col"}), 400
    layer = request.form.get("layer", "full")
    if layer not in ("full", "ground", "object"):
        layer = "full"
    data = get_session()
    cells = data["cells"]
    key = cell_storage_key(row, col, layer)
    if key in cells and key != "0-0":
        del cells[key]
    return jsonify({"ok": True})


@app.route("/api/image/<string:row>/<string:col>")
def get_image(row, col):
    try:
        r, c = int(row), int(col)
    except ValueError:
        return "", 404
    layer = request.args.get("layer", "full")
    if layer not in ("full", "ground", "object"):
        layer = "full"
    data = get_session()
    key = cell_storage_key(r, c, layer)
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
    layer = request.args.get("layer", "full")
    if layer not in ("full", "ground", "object"):
        layer = "full"
    data = get_session()

    if layer == "full":
        target_cells = {
            k: v
            for k, v in data["cells"].items()
            if "-" in k and not k.endswith(("-ground", "-object"))
        }
    else:
        suffix = f"-{layer}"
        target_cells = {}
        for k, v in data["cells"].items():
            if k.endswith(suffix):
                base_key = k[: -len(suffix)]
                target_cells[base_key] = v

    preview = stitch_preview(target_cells, h_overlap, v_overlap)
    return send_file(io.BytesIO(pil_to_bytes(preview)), mimetype="image/png")


if __name__ == "__main__":
    app.run(host="127.0.0.1", port=7860, debug=False)
