param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\Assets\Brand')
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param(
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius
    )

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$size = 256
$bitmap = New-Object System.Drawing.Bitmap $size, $size
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$graphics.Clear([System.Drawing.Color]::Transparent)

$backgroundPath = New-RoundedRectanglePath -X 12 -Y 12 -Width 232 -Height 232 -Radius 54
$backgroundBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    ([System.Drawing.Point]::new(0, 0)),
    ([System.Drawing.Point]::new(256, 256)),
    ([System.Drawing.Color]::FromArgb(255, 10, 18, 27)),
    ([System.Drawing.Color]::FromArgb(255, 32, 54, 72))
)
$graphics.FillPath($backgroundBrush, $backgroundPath)

$glowBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($backgroundPath)
$glowBrush.CenterColor = [System.Drawing.Color]::FromArgb(90, 130, 188, 214)
$glowBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 130, 188, 214))
$graphics.FillPath($glowBrush, $backgroundPath)

$mountainPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70, 209, 230, 244), 4)
$mountainPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$mountainPoints = [System.Drawing.PointF[]]@(
    [System.Drawing.PointF]::new(46, 190),
    [System.Drawing.PointF]::new(92, 145),
    [System.Drawing.PointF]::new(124, 172),
    [System.Drawing.PointF]::new(156, 130),
    [System.Drawing.PointF]::new(209, 190)
)
$graphics.DrawLines($mountainPen, $mountainPoints)

$bearColor = [System.Drawing.Color]::FromArgb(255, 242, 246, 249)
$furBrush = New-Object System.Drawing.SolidBrush $bearColor
$shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(45, 0, 0, 0))
$detailBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 17, 29, 38))
$iceBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 153, 205, 221))

$graphics.FillEllipse($shadowBrush, 56, 42, 146, 146)
$graphics.FillEllipse($furBrush, 64, 48, 128, 128)
$graphics.FillEllipse($furBrush, 74, 34, 34, 34)
$graphics.FillEllipse($furBrush, 148, 34, 34, 34)
$graphics.FillEllipse($shadowBrush, 84, 88, 20, 20)
$graphics.FillEllipse($shadowBrush, 152, 88, 20, 20)
$graphics.FillEllipse($iceBrush, 95, 108, 66, 44)

$nosePath = New-Object System.Drawing.Drawing2D.GraphicsPath
$nosePoints = [System.Drawing.PointF[]]@(
    [System.Drawing.PointF]::new(128, 120),
    [System.Drawing.PointF]::new(116, 133),
    [System.Drawing.PointF]::new(140, 133)
)
$nosePath.AddPolygon($nosePoints)
$graphics.FillPath($detailBrush, $nosePath)
$graphics.DrawArc((New-Object System.Drawing.Pen($detailBrush.Color, 4)), 106, 128, 22, 20, 10, 150)
$graphics.DrawArc((New-Object System.Drawing.Pen($detailBrush.Color, 4)), 128, 128, 22, 20, 20, 150)

$featherPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$featherPath.AddBezier(
    [System.Drawing.PointF]::new(176, 62),
    [System.Drawing.PointF]::new(212, 54),
    [System.Drawing.PointF]::new(228, 88),
    [System.Drawing.PointF]::new(198, 124)
)
$featherPath.AddBezier(
    [System.Drawing.PointF]::new(198, 124),
    [System.Drawing.PointF]::new(193, 104),
    [System.Drawing.PointF]::new(183, 83),
    [System.Drawing.PointF]::new(176, 62)
)
$graphics.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 201, 228, 238))), $featherPath)
$graphics.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 83, 124, 148), 3)), 184, 70, 201, 118)

for ($i = 0; $i -lt 18; $i++) {
    $x = Get-Random -Minimum 36 -Maximum 220
    $y = Get-Random -Minimum 34 -Maximum 212
    $r = Get-Random -Minimum 2 -Maximum 6
    $alpha = Get-Random -Minimum 30 -Maximum 90
    $graphics.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb($alpha, 240, 249, 255))), $x, $y, $r, $r)
}

$pngPath = Join-Path $OutputDirectory 'tld-editor-preview.png'
$icoPath = Join-Path $OutputDirectory 'tld-editor.ico'

$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

$pngBytes = [System.IO.File]::ReadAllBytes($pngPath)
$fs = [System.IO.File]::Create($icoPath)
$writer = New-Object System.IO.BinaryWriter($fs)
$writer.Write([byte[]](0,0))
$writer.Write([uint16]1)
$writer.Write([uint16]1)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([uint16]1)
$writer.Write([uint16]32)
$writer.Write([uint32]$pngBytes.Length)
$writer.Write([uint32]22)
$writer.Write($pngBytes)
$writer.Flush()
$writer.Close()
$fs.Close()

$graphics.Dispose()
$bitmap.Dispose()

Write-Output "Created $icoPath"
