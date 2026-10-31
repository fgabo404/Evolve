# NPC sprites (hexagonal grid)

Place transparent PNG sprite strips here. One NPC per folder:

```
Assets/NPC/
  npc_001/
    sprite.json
    idle_N.png, idle_NE.png, idle_SE.png, idle_S.png, idle_SW.png, idle_NW.png
    walk_N.png, walk_NE.png, walk_SE.png, walk_S.png, walk_SW.png, walk_NW.png
```

Each animation PNG is a **horizontal strip of equal-sized frames**, with no padding or margins. All directions must use the same frame size and consistent foot-anchor. Use nearest-neighbor filtering (pixel art) and alpha transparency. Do not mirror asymmetrical characters with bags or straps.

Directions refer to screen positions (N up, NE up-right, SE down-right, S down, SW down-left, NW up-left), not an assumed axial hex coordinate system. Map grid neighbor offsets to these labels in gameplay code.

`sprite.json` defines animation paths; files are resolved relative to the NPC folder. Missing PNGs are reported by the loader. The game currently has no NPC sprite rendering pipeline; this asset layer is preparation, not an automatic on-screen NPC implementation.

The generated reference art has **not** been uploaded here yet. Before uploading it, cut and align every frame into a regular strip and set the correct `frameWidth`, `frameHeight`, and `frames` fields.
