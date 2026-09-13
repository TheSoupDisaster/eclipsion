param(
    [Parameter(Mandatory = $true)][string]$Source,
    [string]$Repository
)

$ErrorActionPreference = 'Stop'
if (-not $Repository) { $Repository = (Resolve-Path "$PSScriptRoot/../../..").Path }
Add-Type -AssemblyName System.Drawing
$target = Join-Path $Repository 'Resources/Textures/_Crescent/Structures/field_artillery_75mm.rsi'
$original = Join-Path $PSScriptRoot 'original'
New-Item -ItemType Directory -Force -Path $original | Out-Null
foreach ($name in @('base.png', 'icon.png', 'mag-0.png', 'mag-1.png', 'meta.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $original $name))) {
        Copy-Item -LiteralPath (Join-Path $target $name) -Destination (Join-Path $original $name)
    }
}

$sourceImage = [System.Drawing.Bitmap]::new($Source)
$sprite = [System.Drawing.Bitmap]::new(64, 64)
$background = New-Object 'bool[,]' 64,64
# Mechanical export: sample the generated pixel-art design onto the RSI grid.
# The generator baked a light checkerboard into RGB despite the alpha request.
# Its neutral light pixels are outside the gun's dark material range.
for ($y = 0; $y -lt 64; $y++) {
    for ($x = 0; $x -lt 64; $x++) {
        $sx = [Math]::Min($sourceImage.Width - 1, [int][Math]::Floor(($x + 0.5) * $sourceImage.Width / 64))
        $sy = [Math]::Min($sourceImage.Height - 1, [int][Math]::Floor(($y + 0.5) * $sourceImage.Height / 64))
        $color = $sourceImage.GetPixel($sx, $sy)
        $minimum = [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
        $maximum = [Math]::Max($color.R, [Math]::Max($color.G, $color.B))
        $background[$x,$y] = $minimum -ge 155 -and ($maximum - $minimum) -le 20
        $sprite.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $color.R, $color.G, $color.B))
    }
}
$sourceImage.Dispose()
# Remove connected background, retaining isolated steel highlights within the gun.
$queue = [System.Collections.Generic.Queue[System.Drawing.Point]]::new()
for ($i = 0; $i -lt 64; $i++) {
    $queue.Enqueue([System.Drawing.Point]::new($i, 0))
    $queue.Enqueue([System.Drawing.Point]::new($i, 63))
    $queue.Enqueue([System.Drawing.Point]::new(0, $i))
    $queue.Enqueue([System.Drawing.Point]::new(63, $i))
}
# Enclosed openings in the two trail feet.
$queue.Enqueue([System.Drawing.Point]::new(9, 3))
$queue.Enqueue([System.Drawing.Point]::new(52, 3))
while ($queue.Count -gt 0) {
    $point = $queue.Dequeue()
    $x = $point.X
    $y = $point.Y
    if ($x -lt 0 -or $x -ge 64 -or $y -lt 0 -or $y -ge 64 -or -not $background[$x,$y]) { continue }
    $background[$x,$y] = $false
    $sprite.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
    $queue.Enqueue([System.Drawing.Point]::new($x + 1, $y))
    $queue.Enqueue([System.Drawing.Point]::new($x - 1, $y))
    $queue.Enqueue([System.Drawing.Point]::new($x, $y + 1))
    $queue.Enqueue([System.Drawing.Point]::new($x, $y - 1))
}
$sprite.Save((Join-Path $target 'base.png'), [System.Drawing.Imaging.ImageFormat]::Png)

# Retain the existing shell indicator, translating it into the new breech.
$oldMagazine = [System.Drawing.Bitmap]::new((Join-Path $original 'mag-1.png'))
$magazine = [System.Drawing.Bitmap]::new(64, 64)
for ($y = 0; $y -lt 64; $y++) {
    for ($x = 0; $x -lt 64; $x++) {
        $color = $oldMagazine.GetPixel($x, $y)
        if ($color.A -eq 0) { continue }
        if ($color.R -gt 2 * $color.G) {
            $magazine.SetPixel($x - 5, $y - 4, $color)
        } else {
            $magazine.SetPixel($x - 10, $y + 2, $color)
        }
    }
}
$oldMagazine.Dispose()
$magazine.Save((Join-Path $target 'mag-1.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$empty = [System.Drawing.Bitmap]::new(64, 64)
$empty.Save((Join-Path $target 'mag-0.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$empty.Dispose()
$graphics = [System.Drawing.Graphics]::FromImage($sprite)
$graphics.DrawImageUnscaled($magazine, 0, 0)
$graphics.Dispose()
$sprite.Save((Join-Path $target 'icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$magazine.Dispose()
$sprite.Dispose()

# Before/after preview, enlarged only with nearest-neighbor sampling.
$preview = [System.Drawing.Bitmap]::new(832, 448)
$graphics = [System.Drawing.Graphics]::FromImage($preview)
$graphics.Clear([System.Drawing.Color]::FromArgb(38, 40, 43))
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$font = [System.Drawing.Font]::new('Consolas', 14)
$graphics.DrawString('BEFORE', $font, [System.Drawing.Brushes]::Silver, 16, 10)
$graphics.DrawString('AFTER', $font, [System.Drawing.Brushes]::Silver, 432, 10)
$before = [System.Drawing.Bitmap]::new((Join-Path $original 'icon.png'))
$after = [System.Drawing.Bitmap]::new((Join-Path $target 'icon.png'))
$graphics.DrawImage($before, [System.Drawing.Rectangle]::new(16, 48, 384, 384))
$graphics.DrawImage($after, [System.Drawing.Rectangle]::new(432, 48, 384, 384))
$before.Dispose()
$after.Dispose()
$font.Dispose()
$graphics.Dispose()
$preview.Save((Join-Path $PSScriptRoot 'comparison.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
Write-Output "Exported 64x64 RGBA states to $target"
