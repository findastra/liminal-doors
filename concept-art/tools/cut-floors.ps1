# Cuts Liminal Doors' 7 concept plates (Codex, 2026-10-15) into one image per floor, using the divider lines found in
# each plate. Floors 1-36: six per plate, 2 columns x 3 rows (the Concept Atlas' own order). Floor 37: plate 7's top band;
# plate 7's lower panels (A glass wolf, B bubble chair) are references, saved as extras.
# Since 2026-10-19 floors 34, 35 and 37 and both extras are out of the set (copied from other worlds; see
# docs/originality-check-20261019.md), and floor 01 is named Pinkscape. Delete those outputs if you run this again.
param([string]$Out)
$sp = Split-Path $MyInvocation.MyCommand.Path
Add-Type -ReferencedAssemblies System.Drawing -Path (Join-Path $sp 'PlateCutter.cs')
$plates = "$env:USERPROFILE\Documents\Codex\2026-10-15\we-re-going-to-create-a\outputs"
$floors = Get-Content (Join-Path $sp 'floors.json') -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Force $Out, (Join-Path $Out 'extras') | Out-Null

# divider spans per plate, measured by PlateCutter.Uniform (rows: top, mid1, mid2, bottom; cols: left, mid, right)
$grid = @{
  1 = @{ r = @(@(0,4), @(505,512), @(1019,1027), @(1533,1535)); c = @(@(0,0), @(511,514), @(1018,1023)) }
  2 = @{ r = @($null, @(501,510), @(1019,1028), $null);          c = @($null, @(507,516), $null) }
  3 = @{ r = @(@(0,0), @(506,515), @(1017,1027), @(1530,1535)); c = @(@(0,3), @(507,517), @(1020,1023)) }
  4 = @{ r = @(@(0,4), @(507,513), @(1015,1022), @(1527,1535)); c = @(@(0,9), @(507,516), @(1014,1023)) }
  5 = @{ r = @(@(0,6), @(506,516), @(1016,1026), @(1527,1535)); c = @(@(0,6), @(507,516), @(1017,1023)) }
  6 = @{ r = @(@(0,3), @(509,514), @(1020,1025), @(1532,1535)); c = @(@(0,3), @(509,514), @(1020,1023)) }
}
function Slug($s) { (($s.ToLower() -replace "[’']", '') -replace '[^a-z0-9]+', '-').Trim('-') }
$rows = @()
foreach ($f in $floors) {
  $n = [int]$f.n
  $name = '{0:D2}-{1}-{2}-20261015.png' -f $n, (Slug $f.animal), (Slug $f.place)
  if ($n -le 36) {
    $plate = [int]([math]::Floor(($n - 1) / 6) + 1); $k = ($n - 1) % 6; $col = $k % 2; $row = [int][math]::Floor($k / 2)
    $g = $grid[$plate]
    $x0 = if ($col -eq 0) { if ($g.c[0]) { $g.c[0][1] + 1 } else { 0 } } else { $g.c[1][1] + 1 }
    $x1 = if ($col -eq 0) { $g.c[1][0] - 1 } else { if ($g.c[2]) { $g.c[2][0] - 1 } else { 1023 } }
    $y0 = if ($row -eq 0) { if ($g.r[0]) { $g.r[0][1] + 1 } else { 0 } } else { $g.r[$row][1] + 1 }
    $y1 = if ($row -eq 2) { if ($g.r[3]) { $g.r[3][0] - 1 } else { 1535 } } else { $g.r[$row + 1][0] - 1 }
    $src = 'plate {0}, {1} column, row {2}' -f $plate, @('left', 'right')[$col], ($row + 1)
  } else {
    $plate = 7; $x0 = 0; $x1 = 1535; $y0 = 0; $y1 = 475; $src = 'plate 7, top band'
  }
  $p = Join-Path $plates ('liminal-doors-plate-{0:D2}-20261015.png' -f $plate)
  [PlateCutter]::Crop($p, $x0, $y0, $x1 - $x0 + 1, $y1 - $y0 + 1, (Join-Path $Out $name))
  $rows += [pscustomobject]@{ n = $n; animal = $f.animal; place = $f.place; file = $name; source = $src; w = $x1 - $x0 + 1; h = $y1 - $y0 + 1 }
}
$p7 = Join-Path $plates 'liminal-doors-plate-07-20261015.png'
[PlateCutter]::Crop($p7, 0, 477, 765, 547, (Join-Path $Out 'extras\ref-a-glass-wolf-20261015.png'))
[PlateCutter]::Crop($p7, 771, 477, 765, 547, (Join-Path $Out 'extras\ref-b-bubble-chair-20261015.png'))
$rows | ConvertTo-Json | Set-Content (Join-Path $sp 'floors-cut.json') -Encoding utf8
$rows | ForEach-Object { '{0,2} {1,-40} {2}x{3}  {4}' -f $_.n, $_.file, $_.w, $_.h, $_.source }
