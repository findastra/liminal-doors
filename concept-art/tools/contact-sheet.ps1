# Contact sheet of the cut floor images: 6 across, each thumbnail labelled with its file name's number, animal and place.
param([string]$Dir, [string]$Out)
Add-Type -AssemblyName System.Drawing
$files = Get-ChildItem $Dir -Filter '*.png' | Sort-Object Name
$cols = 6; $tw = 300; $th = 300; $lab = 34; $pad = 10
$rows = [math]::Ceiling($files.Count / $cols)
$W = $cols * ($tw + $pad) + $pad; $H = $rows * ($th + $lab + $pad) + $pad
$sheet = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(26, 19, 31)); $g.InterpolationMode = 'HighQualityBicubic'
$font = New-Object System.Drawing.Font 'Segoe UI', 10.5; $brush = [System.Drawing.Brushes]::White
for ($i = 0; $i -lt $files.Count; $i++) {
  $img = [System.Drawing.Image]::FromFile($files[$i].FullName)
  $x = $pad + ($i % $cols) * ($tw + $pad); $y = $pad + [math]::Floor($i / $cols) * ($th + $lab + $pad)
  $s = [math]::Min($tw / $img.Width, $th / $img.Height); $w = [int]($img.Width * $s); $h = [int]($img.Height * $s)
  $g.DrawImage($img, $x + [int](($tw - $w) / 2), $y + [int](($th - $h) / 2), $w, $h)
  $label = ($files[$i].BaseName -replace '-20261015$', '') -replace '-', ' '
  $g.DrawString($label, $font, $brush, (New-Object System.Drawing.RectangleF $x, ($y + $th + 2), $tw, $lab))
  $img.Dispose()
}
$sheet.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $sheet.Dispose()
$Out
