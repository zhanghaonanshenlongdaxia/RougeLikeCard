

## Codely Structured Memories

### User

### Feedback
- [2026-08-05 14:25:27] Do NOT run white-background removal scripts on object layer tiles. The white removal script (threshold r>0.92,g>0.92,b>0.92) was intended for role sprite sheets only but accidentally damaged baseRoom object tiles by making building walls/parts transparent. Why: white/near-white pixels in object tiles include legitimate building features (walls, stone bases, light-colored roofs), not just background. How to apply: always separate role sprite processing from map tile processing; never run generic white-removal on map assets.

### Project
- [2026-08-03 19:48:32] Base room (宗门) tile layer separation workflow (2026-08-03): Original full tiles at Assets/Res/sprites/baseRoom/unity_tiles/ (9 tiles, 1088x608, 15% overlap). Layer separation via AI (Frontier Game Design) + alpha extraction script. All 9 tiles processed. Pipeline: 1) Upload original to CDN (file_upload + curl.exe PUT, must copy to temp dir first to avoid backslash 403), 2) AI generate object layer (grey bg where ground was) + ground layer (ground only), 3) C# script: detect grey bg by brightness>0.82 + saturation<0.08 → alpha=0, dilate 3px to cover white edges, use ORIGINAL RGB (not AI RGB, to preserve details like pavilion roofs), 2-pass edge color fill from opaque neighbors. Result: ~35-53% transparent object layers. tile_1_-1 had 0% transparent (all objects, little ground). Ground layer used as-is from AI output. BaseRoom in scene 3-Base.unity has Layer_Ground (sortingOrder=0) + Layer_Object (sortingOrder=2) + Player placeholder (sortingOrder=1, red 32x32 square).
- [2026-08-03 20:06:45] FINAL tile layer separation params (2026-08-03): DUAL-CONDITION alpha extraction. Transparent ONLY if: (AI obj approx AI ground diff<0.12 AND original approx AI ground diff<0.25) OR (AI obj pure grey bright>0.88 sat<0.04 AND original also bright>0.5). KEY rule: when AI replaces walls with grey but original has dark pixels origBright<=0.5 KEEP opaque. RGB always from original. 2-pass edge color fill. Result 3-12pct transparent walls intact. Failed approaches: brightness-only diff-only strict-grey-only.

### Reference

