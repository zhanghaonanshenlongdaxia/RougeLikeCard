"""
Local AI generation runner — bridges Flask job queue with Codely image generation.

Usage:
    python local_runner.py next                    # Get next job + download canvas
    python local_runner.py submit <job_id> <img>   # Submit generated image
    python local_runner.py fail <job_id> [reason]  # Mark job as failed
    python local_runner.py status                  # Show queue status
    python local_runner.py run_all                 # Process entire queue (calls Codely tools via subprocess)
"""
import sys
import os
import json
import requests

BASE = "http://127.0.0.1:7860"
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.join(SCRIPT_DIR, "temp_canvases")
os.makedirs(OUT_DIR, exist_ok=True)


def cmd_next():
    resp = requests.get(f"{BASE}/api/local_next")
    data = resp.json()
    if not data.get("job_id"):
        print(json.dumps({"ok": False, "message": "No pending jobs"}))
        return
    job_id = data["job_id"]
    canvas_resp = requests.get(f"{BASE}/api/local_canvas/{job_id}")
    canvas_path = os.path.join(OUT_DIR, f"{job_id}_download.png")
    with open(canvas_path, "wb") as f:
        f.write(canvas_resp.content)
    data["canvas_local_path"] = canvas_path
    print(json.dumps(data, ensure_ascii=False, indent=2))


def cmd_submit(job_id, image_path):
    with open(image_path, "rb") as f:
        resp = requests.post(
            f"{BASE}/api/local_complete",
            data={"job_id": job_id},
            files={"image": f},
        )
    print(json.dumps(resp.json(), ensure_ascii=False, indent=2))


def cmd_fail(job_id, reason="unknown"):
    resp = requests.post(
        f"{BASE}/api/local_fail",
        data={"job_id": job_id, "reason": reason},
    )
    print(json.dumps(resp.json(), ensure_ascii=False, indent=2))


def cmd_status():
    resp = requests.get(f"{BASE}/api/local_status")
    print(json.dumps(resp.json(), ensure_ascii=False, indent=2))


def cmd_run_all():
    """Process all jobs — prints each job's canvas path + prompt for Codely to pick up.
    Each iteration: fetch next → print info → wait for Codely to generate → submit.
    In automatic mode, this just loops 'next' until queue is empty."""
    while True:
        resp = requests.get(f"{BASE}/api/local_next")
        data = resp.json()
        if not data.get("job_id"):
            print(json.dumps({"ok": False, "message": "Queue empty"}))
            break
        job_id = data["job_id"]
        canvas_resp = requests.get(f"{BASE}/api/local_canvas/{job_id}")
        canvas_path = os.path.join(OUT_DIR, f"{job_id}_download.png")
        with open(canvas_path, "wb") as f:
            f.write(canvas_resp.content)
        data["canvas_local_path"] = canvas_path
        # Print as JSON line for Codely to parse
        print(json.dumps(data, ensure_ascii=False))
        sys.stdout.flush()
        # Wait for Codely to generate and submit before continuing
        # Codely will call 'submit' or 'fail' separately
        break  # One at a time; Codely re-invokes run_all for next


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "status"
    if cmd == "next":
        cmd_next()
    elif cmd == "submit":
        cmd_submit(sys.argv[2], sys.argv[3])
    elif cmd == "fail":
        cmd_fail(sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "unknown")
    elif cmd == "status":
        cmd_status()
    elif cmd == "run_all":
        cmd_run_all()
    else:
        print(f"Unknown command: {cmd}")
        print("Usage: python local_runner.py [next|submit|fail|status|run_all]")
