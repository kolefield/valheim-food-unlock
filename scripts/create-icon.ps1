# Original geometric plate-and-infinity icon. No external or game artwork.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$bitmap = [Drawing.Bitmap]::new(256, 256)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$background = [Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Point]::new(0,0), [Drawing.Point]::new(256,256), [Drawing.ColorTranslator]::FromHtml('#302727'), [Drawing.ColorTranslator]::FromHtml('#121619'))
$gold = [Drawing.ColorTranslator]::FromHtml('#E6B665')
$pen = [Drawing.Pen]::new($gold, 5)
$thin = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#776044'), 2)
$brush = [Drawing.SolidBrush]::new($gold)
$font = [Drawing.Font]::new('Georgia', 21, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$format = [Drawing.StringFormat]::new()
$format.Alignment = [Drawing.StringAlignment]::Center
try {
    $graphics.FillRectangle($background, 0, 0, 256, 256)
    $graphics.DrawRectangle($thin, 9, 9, 238, 238)
    $graphics.DrawEllipse($pen, 62, 27, 132, 132)
    $graphics.DrawEllipse($thin, 73, 38, 110, 110)
    $loop = [Drawing.Drawing2D.GraphicsPath]::new()
    $loop.AddBezier(128,93, 101,55, 72,69, 84,99)
    $loop.AddBezier(84,99, 101,124, 125,87, 145,76)
    $loop.AddBezier(145,76, 175,60, 185,101, 158,110)
    $loop.AddBezier(158,110, 146,114, 136,102, 128,93)
    $graphics.DrawPath($pen, $loop)
    $loop.Dispose()
    $graphics.DrawLine($pen, 39, 36, 39, 151)
    $graphics.DrawLine($pen, 28, 34, 28, 65)
    $graphics.DrawLine($pen, 50, 34, 50, 65)
    $graphics.DrawArc($pen, 28, 53, 22, 26, 0, 180)
    $graphics.FillEllipse($brush, 207, 31, 22, 43)
    $graphics.DrawLine($pen, 218, 65, 218, 151)
    $graphics.DrawString('FOOD', $font, $brush, [Drawing.RectangleF]::new(14,171,228,30), $format)
    $graphics.DrawString('UNLOCK', $font, $brush, [Drawing.RectangleF]::new(14,200,228,32), $format)
    $bitmap.Save((Join-Path $root 'icon.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally {
    $format.Dispose(); $font.Dispose(); $brush.Dispose(); $thin.Dispose(); $pen.Dispose()
    $background.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
