Add-Type -AssemblyName System.Drawing

$iconDir = Join-Path (Join-Path $PSScriptRoot "CameraStreaming") "Icons"
if (-not (Test-Path $iconDir)) { New-Item -ItemType Directory -Path $iconDir -Force | Out-Null }
$icoPath = Join-Path $iconDir "app.ico"

$sizes = @(16, 32, 48, 256)
$pngStreams = @()

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $size / 256.0
    $cx = $size / 2.0
    $cy = $size / 2.0

    # Background circle
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 30, 30, 30))
    $bgPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 80, 80, 80), (2 * $scale))
    $radius = 110 * $scale
    $g.FillEllipse($bgBrush, ($cx - $radius), ($cy - $radius), ($radius * 2), ($radius * 2))
    $g.DrawEllipse($bgPen, ($cx - $radius), ($cy - $radius), ($radius * 2), ($radius * 2))
    $bgBrush.Dispose(); $bgPen.Dispose()

    # Camera body (rounded rectangle)
    $bodyBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 100, 100, 100))
    $bodyPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 200, 200, 200), (2 * $scale))
    $bw = 140 * $scale; $bh = 90 * $scale
    $bx = $cx - $bw/2; $by = $cy - $bh/2 + 5*$scale
    $r = [Math]::Min([Math]::Min((8 * $scale), ($bw/2)), ($bh/2))
    $g.FillRectangle($bodyBrush, $bx, $by, $bw, $bh)
    $g.DrawRectangle($bodyPen, $bx, $by, $bw, $bh)
    $bodyBrush.Dispose(); $bodyPen.Dispose()

    # Camera bump (viewfinder)
    $bumpBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 80, 80, 80))
    $bumpW = 50 * $scale; $bumpH = 20 * $scale
    $bumpX = $cx - $bumpW/2; $bumpY = ($cy - 90*$scale/2 + 5*$scale) - $bumpH + 2*$scale
    $g.FillRectangle($bumpBrush, $bumpX, $bumpY, $bumpW, $bumpH)
    $bumpBrush.Dispose()

    # Lens outer ring
    $lensR = 40 * $scale
    $ringBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 50, 50, 50))
    $ringPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 180, 180, 180), (3 * $scale))
    $ly = $cy - $lensR + 5*$scale
    $g.FillEllipse($ringBrush, ($cx - $lensR), $ly, ($lensR*2), ($lensR*2))
    $g.DrawEllipse($ringPen, ($cx - $lensR), $ly, ($lensR*2), ($lensR*2))
    $ringBrush.Dispose(); $ringPen.Dispose()

    # Lens glass
    $innerR = $lensR - 3*$scale
    $lyInner = $cy - $innerR + 8*$scale
    $glassBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 60, 160, 240))
    $g.FillEllipse($glassBrush, ($cx - $innerR), $lyInner, ($innerR*2), ($innerR*2))
    $glassBrush.Dispose()

    # Lens highlight
    $hlBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(100, 255, 255, 255))
    $hlR = 14 * $scale
    $g.FillEllipse($hlBrush, ($cx - $hlR - 8*$scale), ($cy - $hlR - 2*$scale), ($hlR*2), ($hlR*2))
    $hlBrush.Dispose()

    # Record dot
    $dotBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 220, 50, 50))
    $dotR = 6 * $scale
    $g.FillEllipse($dotBrush, ($cx + 60*$scale - $dotR), ($cy - 20*$scale - $dotR), ($dotR*2), ($dotR*2))
    $dotBrush.Dispose()

    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngStreams += ,$ms
    $bmp.Dispose()
}

# Build ICO file
$ms_ico = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($ms_ico)

$writer.Write([Int16]0)
$writer.Write([Int16]1)
$writer.Write([Int16]$pngStreams.Count)

# First pass: calculate offsets
$headerSize = 6 + $pngStreams.Count * 16
$dataOffset = $headerSize
$entryData = @()

foreach ($i in 0..($pngStreams.Count-1)) {
    $pngBytes = $pngStreams[$i].ToArray()
    $sz = $sizes[$i]
    $w = if ($sz -ge 256) { [byte]0 } else { [byte]$sz }
    $entryData += @{
        Width = $w
        Height = $w
        Data = $pngBytes
        Offset = $dataOffset
    }
    $dataOffset += $pngBytes.Length
}

# Write directory entries
foreach ($entry in $entryData) {
    $writer.Write($entry.Width)
    $writer.Write($entry.Width)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([Int16]1)
    $writer.Write([Int16]32)
    $writer.Write([Int32]$entry.Data.Length)
    $writer.Write([Int32]$entry.Offset)
}

# Write image data
foreach ($entry in $entryData) {
    $writer.Write($entry.Data)
}

[System.IO.File]::WriteAllBytes($icoPath, $ms_ico.ToArray())
$writer.Dispose()
$ms_ico.Dispose()
foreach ($ms in $pngStreams) { $ms.Dispose() }

Write-Host "Icon created at: $icoPath" -ForegroundColor Green
