$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$rsiDirectory = Join-Path $workspace 'Resources/Textures/_Crescent/Structures/imperialflag_improved.rsi'
$variants = (Get-Content (Join-Path $PSScriptRoot 'variants.json') -Raw | ConvertFrom-Json).variants
$sheet = [System.Drawing.Bitmap]::new(960, 280)
$graphics = [System.Drawing.Graphics]::FromImage($sheet)
$graphics.Clear([System.Drawing.Color]::FromArgb(38, 40, 49))
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$font = [System.Drawing.Font]::new('Arial', 11)
$index = 0
foreach ($variant in $variants) {
    $source = [System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot "$($variant.name)-source.png"))
    if ($source.GetPixel(0, 0).A -ne 0) { throw "Source background must be transparent: $($variant.name)" }
    $sprite = [System.Drawing.Bitmap]::new(32, 32)
    $resize = [System.Drawing.Graphics]::FromImage($sprite)
    $resize.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $resize.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    # Align the centered, narrow blade with a destination pixel center so it
    # survives nearest-neighbor downsampling onto an even-width canvas.
    $resize.DrawImage($source, [System.Drawing.RectangleF]::new(0.5, 0, 32, 32), [System.Drawing.RectangleF]::new(0, 0, $source.Width, $source.Height), [System.Drawing.GraphicsUnit]::Pixel)
    $resize.Dispose()
    for ($y = 0; $y -lt 32; $y++) {
        for ($x = 0; $x -lt 32; $x++) {
            $pixel = $sprite.GetPixel($x, $y)
            if ($pixel.A -lt 128) {
                $sprite.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            } else {
                $sprite.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $pixel.R, $pixel.G, $pixel.B))
            }
        }
    }
    $sprite.Save((Join-Path $rsiDirectory "$($variant.name).png"))
    $graphics.DrawString($variant.name, $font, [System.Drawing.Brushes]::White, $index * 192 + 15, 8)
    $graphics.DrawImage($sprite, [System.Drawing.Rectangle]::new($index * 192, 30, 192, 192), 0, 0, 32, 32, [System.Drawing.GraphicsUnit]::Pixel)
    $graphics.DrawImage($sprite, [System.Drawing.Rectangle]::new($index * 192 + 80, 235, 32, 32), 0, 0, 32, 32, [System.Drawing.GraphicsUnit]::Pixel)
    $sprite.Dispose()
    $source.Dispose()
    $index++
}
$sheet.Save((Join-Path $PSScriptRoot 'preview.png'))
$font.Dispose()
$graphics.Dispose()
$sheet.Dispose()
Write-Output 'Exported five 32x32 sprites and preview.png.'
