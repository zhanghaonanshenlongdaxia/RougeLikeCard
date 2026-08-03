

## Codely Structured Memories

### User

### Feedback

### Project
- [2026-08-03 18:35:45] Map stitching tool (地图拼接) at Tools/ImageOutpaintTool/ (Flask app on 127.0.0.1:7860). Original: Volcengine doubao-seedream API for AI outpaint tile generation with 3x3 grid UI, overlap blending, layer separation (full/ground/object), Unity/Godot export. v2 (2026-08-03): Added non-blocking local AI generation. Sidebar has mode toggle: "自定义API" (original Volcengine) vs "Unity本地生成" (Codely processes via generate_image). In local mode, /api/generate and /api/generate_center return immediately with {pending:true, job_id}, browser polls /api/local_check/<job_id> every 2s. Codely auto-polls /api/local_next every 1min via cron_create → downloads canvas → uploads to CDN → generate_image → submits via /api/local_complete which stores result directly into browser's session (using sid stored in job). Key fix: local_complete uses job["sid"] to find the correct SESSIONS entry, solving the session isolation problem from v1. Endpoints: /api/local_next, /api/local_canvas/<id>, /api/local_complete, /api/local_fail, /api/local_check/<id>, /api/local_status. Flask runs with threaded=True.

- [2026-08-03 18:24:33] Local AI generation workflow verified end-to-end (2026-08-02): 4 tiles (上1/左1/右1/下1) generated via Frontier Game Design and submitted to Flask. Workflow per tile: curl.exe download canvas from /api/local_canvas/<id> → file_upload to get presigned URL → curl.exe PUT upload to CDN → generate_image with image_urls+prompt → wait for completion → curl.exe download result → curl.exe POST to /api/local_complete. Generation order enforced by order_pending_cells(): horizontal first, then vertical, then corners. ~20-40s per tile. All 4 completed successfully, edge blending via apply_edge_strips().

### Reference

