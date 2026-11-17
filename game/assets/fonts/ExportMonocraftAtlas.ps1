param([string]$FontDirectory = $PSScriptRoot)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$collection = New-Object System.Drawing.Text.PrivateFontCollection
$bitmap = New-Object System.Drawing.Bitmap(128, 96)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$font = $null
$format = [System.Drawing.StringFormat]::GenericTypographic.Clone()
try {
    $collection.AddFontFile((Join-Path $FontDirectory 'Monocraft.ttf'))
    $font = New-Object System.Drawing.Font($collection.Families[0], 12, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel))
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit
    $format.FormatFlags = $format.FormatFlags -bor [System.Drawing.StringFormatFlags]::MeasureTrailingSpaces
    for ($character = 32; $character -lt 127; $character++) {
        $index = $character - 32
        $x = ($index % 16) * 8
        $y = [Math]::Floor($index / 16) * 16
        $graphics.DrawString(([char]$character).ToString(), $font, [System.Drawing.Brushes]::White, [System.Drawing.PointF]::new($x, $y), $format)
    }
    $bitmap.SetPixel(127, 95, [System.Drawing.Color]::White)
    $pixels = New-Object byte[] (128 * 96)
    for ($y = 0; $y -lt 96; $y++) {
        for ($x = 0; $x -lt 128; $x++) {
            $pixels[$y * 128 + $x] = if ($bitmap.GetPixel($x, $y).A -ge 128) { 255 } else { 0 }
        }
    }
    [System.IO.File]::WriteAllBytes((Join-Path $FontDirectory 'MonocraftAtlas.bin'), $pixels)
} finally {
    $format.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    if ($null -ne $font) { $font.Dispose() }
    $collection.Dispose()
}
