# Web copies of the 37 floor PNGs for the Collection page: floor-NN-20261015.jpg, JPEG quality 90, same pixels.
param([string]$Dir, [string]$Out)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Out | Out-Null
$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$ep = New-Object System.Drawing.Imaging.EncoderParameters 1
$ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), ([long]90)
$total = 0
foreach ($f in Get-ChildItem $Dir -Filter '*.png' | Sort-Object Name) {
  $nn = $f.Name.Substring(0, 2)
  $img = [System.Drawing.Image]::FromFile($f.FullName)
  $bmp = New-Object System.Drawing.Bitmap $img.Width, $img.Height
  $g = [System.Drawing.Graphics]::FromImage($bmp); $g.Clear([System.Drawing.Color]::Black); $g.DrawImage($img, 0, 0, $img.Width, $img.Height); $g.Dispose()
  $dest = Join-Path $Out ("floor-$nn-20261015.jpg")
  $bmp.Save($dest, $codec, $ep); $bmp.Dispose(); $img.Dispose()
  $total += (Get-Item $dest).Length
}
'{0} files, {1:N1} MB' -f (Get-ChildItem $Out -Filter 'floor-*.jpg').Count, ($total / 1MB)
