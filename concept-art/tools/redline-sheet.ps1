# Full-size redline sheet of Liminal Doors' 37 floors, laid out like the contact sheet Astra marks up in Procreate:
# 6 across, each floor labelled "NN animal place" underneath, floor 37 (the wide galaxy band) across the whole last row.
# JPEG, so the 3000 x 4400 px sheet stays small enough to send and import.
# With -Data (the page's floors-data.json), each label reads "NN place animal", as on the Collection page (Astra, 2026-10-19).
param([string]$Dir, [string]$Out, [string]$Data)
Add-Type -AssemblyName System.Drawing
$files = Get-ChildItem $Dir -Filter '*.png' | Sort-Object Name
$cols = 6; $tile = 500; $lab = 46; $pad = 14
$grid = @($files | Where-Object { $_.Name -notlike '37-*' }); $wide = $files | Where-Object { $_.Name -like '37-*' } | Select-Object -First 1
$rows = [math]::Ceiling($grid.Count / $cols)
$W = $cols * ($tile + $pad) + $pad
$wideH = 0; if ($wide) { $wi = [System.Drawing.Image]::FromFile($wide.FullName); $wideH = [int](($W - 2 * $pad) * $wi.Height / $wi.Width); $wi.Dispose() }
$H = $pad + $rows * ($tile + $lab + $pad) + $(if ($wide) { $wideH + $lab + $pad } else { 0 })
$sheet = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(26, 19, 31)); $g.InterpolationMode = 'HighQualityBicubic'; $g.TextRenderingHint = 'AntiAliasGridFit'
$font = New-Object System.Drawing.Font 'Segoe UI', 17; $brush = [System.Drawing.Brushes]::White
$names = @{}
if ($Data) { foreach ($f in (Get-Content $Data -Raw -Encoding UTF8 | ConvertFrom-Json)) { $names[[int]$f.n] = ('{0:D2} {1} {2}' -f [int]$f.n, $f.place, $f.animal).ToLower() -replace '-', ' ' } }
function Label($file) {
  $n = [int]$file.Name.Substring(0, 2)
  if ($names.ContainsKey($n)) { $names[$n] } else { ($file.BaseName -replace '-\d{8}$', '') -replace '-', ' ' }
}
for ($i = 0; $i -lt $grid.Count; $i++) {
  $img = [System.Drawing.Image]::FromFile($grid[$i].FullName)
  $x = $pad + ($i % $cols) * ($tile + $pad); $y = $pad + [math]::Floor($i / $cols) * ($tile + $lab + $pad)
  $s = [math]::Min($tile / $img.Width, $tile / $img.Height); $iw = [int]($img.Width * $s); $ih = [int]($img.Height * $s)
  $g.DrawImage($img, $x + [int](($tile - $iw) / 2), $y + [int](($tile - $ih) / 2), $iw, $ih)
  $g.DrawString((Label $grid[$i]), $font, $brush, (New-Object System.Drawing.RectangleF $x, ($y + $tile + 6), $tile, $lab))
  $img.Dispose()
}
if ($wide) {
  $img = [System.Drawing.Image]::FromFile($wide.FullName); $y = $pad + $rows * ($tile + $lab + $pad)
  $g.DrawImage($img, $pad, $y, $W - 2 * $pad, $wideH)
  $g.DrawString((Label $wide), $font, $brush, (New-Object System.Drawing.RectangleF $pad, ($y + $wideH + 6), ($W - 2 * $pad), $lab))
  $img.Dispose()
}
$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$ep = New-Object System.Drawing.Imaging.EncoderParameters 1
$ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), ([long]90)
$sheet.Save($Out, $codec, $ep); $g.Dispose(); $sheet.Dispose()
'{0}  {1} x {2} px  {3:N1} MB' -f $Out, $W, $H, ((Get-Item $Out).Length / 1MB)
