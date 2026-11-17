# Local Monocraft assets

Monocraft by Idrees Hassan, copyright 2022, is distributed under SIL OFL-1.1.
The complete copyright notice and license are in `OFL-1.1.txt`, distributed
alongside the original font and the derived bitmap atlas.

Source: https://github.com/IdreesInc/Monocraft

Pinned upstream commit: `e498bf70aeb25b4bdcff1e44d878fb2cb4f7c2a9`

Original path: `dist/Monocraft-ttf/Monocraft.ttf`

Original font SHA-256:
`262e31822e0a75be567b02320bfa092143d871e8a273e529d1c5ab0c04cd215f`

`MonocraftAtlas.bin` is a derived single-channel bitmap, also under OFL-1.1.
It contains 128 x 96 bytes, top to bottom, with 16 columns of 8 x 16 cells.
Cells contain ASCII 32 through 126 in order. The last texel is white for solid
UI rectangles. Unsupported characters are rendered as `?`.

The original font is rasterized at 12 pixels with monochrome grid fitting.
The game uses the embedded atlas with nearest filtering and integer scaling;
it does not require an installed font or a runtime rasterization dependency.
The original TTF and license are copied to build and publish output.

To regenerate the atlas on Windows using Windows PowerShell and System.Drawing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\game\assets\fonts\ExportMonocraftAtlas.ps1
```

The script only generates a font asset; it does not build or run the game.
