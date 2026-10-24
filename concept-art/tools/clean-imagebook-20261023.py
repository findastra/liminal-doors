"""Liminal Doors imagebook, cleaned (2026-10-23): python -I clean-imagebook-20261023.py <original.pdf> <cleaned.pdf>

Takes out what the originality check removed (docs/originality-check-20261019.md; Astra, 2026-10-23: "without the
removed floors 34, 35, 37, the glass wolf, the bubble chair and any screenshot from other creators' worlds"):
pages 15 (scene 37, glass wolf and bubble chair studies), 16 (wolf screenshot in Crystals Absence), 19 (glass-animal
and bubble-chair tools) and 24 (Unity render of the bubble-chair stand-in); plate 7 on the cover (plate 1 instead);
floors 34 and 35 on plate 6 and in the register; the wolf, chipmunk and Backrooms lines; the index rows and links for
Astral Bounds, Crystals Absence and Copperhead ASMR Barbershop. Only objects still reachable are written, so nothing
removed stays inside the file.

The original (liminal-doors-imagebook-20261015.pdf) stays on Astra's PC only. A small reader for simple ReportLab
PDFs comes first, then the cleaning steps. No libraries; Claude Opus 5.5 (claude-opus-5-5), 2026-10-23."""
import re, zlib, base64
def load(path):
    d = open(path, 'rb').read()
    objs = {}
    for m in re.finditer(rb'(\d+) 0 obj\s*(.*?)endobj', d, re.S):
        objs[int(m.group(1))] = m.group(2)
    return d, objs
def stream(body):
    m = re.match(rb'\s*<<(.*?)>>\s*stream\r?\n(.*)endstream', body, re.S)
    if not m: return None, None
    h, data = m.group(1), m.group(2).rstrip()
    L = re.search(rb'/Length (\d+)', h)
    if L: data = m.group(2)[:int(L.group(1))]
    if b'ASCII85' in h:
        s = data.strip(); e = s.find(b'~>'); s = s[:e] if e >= 0 else s
        if s.startswith(b'<~'): s = s[2:]
        data = base64.a85decode(s)
    if b'FlateDecode' in h and b'/Image' not in h: data = zlib.decompress(data)
    return h, data
def pages(objs):
    out = []
    def walk(n):
        b = objs[n]
        if re.search(rb'/Type\s*/Pages', b):
            for k in re.findall(rb'(\d+) 0 R', re.search(rb'/Kids\s*\[(.*?)\]', b, re.S).group(1)): walk(int(k))
        else: out.append(n)
    tops = [n for n, b in objs.items() if re.search(rb'/Type\s*/Pages', b) and b'/Parent' not in b]
    walk(tops[0]); return tops[0], out
def text(objs, page):
    b = objs[page]; cs = re.search(rb'/Contents\s*(\d+) 0 R', b)
    _, data = stream(objs[int(cs.group(1))])
    words = [w.decode('latin-1') for w in re.findall(rb'\(((?:\.|[^\)])*)\)\s*Tj', data)]
    imgs = re.findall(rb'/(\w+) Do', data)
    xo = re.search(rb'/XObject\s*<<(.*?)>>', b, re.S)
    names = dict((k, int(v)) for k, v in re.findall(rb'/(\w+) (\d+) 0 R', xo.group(1))) if xo else {}
    return ' '.join(words), [names.get(i) for i in imgs]
import struct
def image_rgb(objs, n):
    h, data = stream(objs[n])
    w = int(re.search(rb'/Width (\d+)', h).group(1)); ht = int(re.search(rb'/Height (\d+)', h).group(1))
    return w, ht, zlib.decompress(data), h
def save_png(w, h, raw, path):
    def ch(t, b): return struct.pack('>I', len(b)) + t + b + struct.pack('>I', zlib.crc32(t + b) & 0xffffffff)
    rows = b''.join(b'\0' + raw[y*w*3:(y+1)*w*3] for y in range(h))
    open(path, 'wb').write(b'\x89PNG\r\n\x1a\n' + ch(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 2, 0, 0, 0)) + ch(b'IDAT', zlib.compress(rows)) + ch(b'IEND', b''))

import re, sys, zlib

src, dst = sys.argv[1], sys.argv[2]
d, objs = load(src)
top, pages = pages(objs)
DROP = {15, 16, 19, 24}

def contents_id(n): return int(re.search(rb'/Contents\s*(\d+) 0 R', objs[n]).group(1))
def page_text(i): return stream(objs[contents_id(pages[i - 1])])[1].decode('latin-1')
def put_stream(n, data, extra=b''):
    z = zlib.compress(data, 9)
    objs[n] = b'<< /Filter /FlateDecode /Length %d%s >>\nstream\n' % (len(z), extra) + z + b'\nendstream\n'
def set_text(i, text): put_stream(contents_id(pages[i - 1]), text.encode('latin-1'))
def replace(t, old, new):
    if old not in t: raise SystemExit('not found: ' + old[:60])
    return t.replace(old, new, 1)
def cut_lines(t, a, b, note=None):
    lines = t.split('\n'); keep = lines[:a] + ([note] if note else []) + lines[b + 1:]
    return '\n'.join(keep)
GREY = '.733333 .690196 .784314 rg'

# p1, p2, p20, p23: the wolf, the chipmunk (floor 34) and the Backrooms replica
t = page_text(1)
t = replace(t, '(Draft 01 / Environment art, floor register and production direction. AI concept renders are not Unity screenshots. The live wolf reference) Tj T* (appears separately.)',
               '(Draft 01 / Environment art, floor register and production direction. AI concept renders are not Unity screenshots. Floors 34, 35 and) Tj T* (37 were removed on 2026-10-19.)')
# The cover showed plate 7 (scene 37, the glass wolf, the bubble chair): show plate 1 instead, fitted to the same box.
plate7 = re.search(rb'/(FormXob\.\w+) 4 0 R', objs[pages[0]]).group(1).decode()
plate1 = re.search(rb'/(FormXob\.\w+) (\d+) 0 R', objs[pages[2]])
assert re.search(rb'/Width 1024 /Height 1536|/Height 1536', objs[int(plate1.group(2))])
t = replace(t, '636 0 0 424 42 290.5 cm\n/' + plate7 + ' Do', '282.6667 0 0 424 218.6667 290.5 cm\n/' + plate1.group(1).decode() + ' Do')
objs[pages[0]] = objs[pages[0]].replace(b'/' + plate7.encode() + b' 4 0 R', b'/' + plate1.group(1) + b' ' + plate1.group(2) + b' 0 R')
set_text(1, t)
t = page_text(2)
t = replace(t, '(chipmunk on a bench can be', '(animal on a bench can be')
set_text(2, t)
t = page_text(20)
assert 'The exact Backrooms replica' in t.split('\n')[56] and 'additional candidate' in t.split('\n')[61]
t = replace(t, "(Replace the generic glass treatment in later concepts with the live wolf reference's angular prismatic language. Add)",
               "(Give every hidden animal one angular prismatic look of our own in later concepts. Add)")
t = replace(t, '-0.049895 Tw (The wolf is now observed in the live client. The bubble chair, bathtub sponge, water toys, ASMR props, door mechanics,) Tj T* 0 Tw',
               '(The bathtub sponge, water toys, ASMR props, door mechanics,) Tj T*')
t = cut_lines(t, 54, 63)
set_text(20, t)
t = page_text(23)
t = replace(t, '(proposals, door effects, interactions and palettes. The live wolf screenshot records the corrected material direction.)',
               '(proposals, door effects, interactions and palettes. Floors 34, 35 and 37 were removed on 2026-10-19.)')
t = replace(t, '(this book. The live VRChat image is reference documentation; no source assets were extracted. Floor naming is provisional and can change)',
               '(this book. No source assets were taken from other worlds. Floor naming is provisional and can change)')
set_text(23, t)

# p14: floor register entries 34 and 35
t = page_text(14); L = t.split('\n')
assert '34  Chipmunk' in L[98] and '35  Wolf' in L[122] and 'Hidden animal' in L[141] and '36  Kingfisher' in L[146]
t = cut_lines(t, 96, 143, GREY + '\nBT 1 0 0 1 42 380 Tm /F1 10 Tf (34 and 35: removed on 2026-10-19, copied from other creators\' VRChat worlds \\(originality check\\).) Tj ET')
set_text(14, t)

# p21, p22: the index rows for Astral Bounds (06) and Crystals Absence (12), and their links
t = page_text(21); L = t.split('\n')
assert '06  Astral Bounds' in L[91] and 'wrld_34693736' in L[101]
set_text(21, cut_lines(t, 89, 101, GREY + '\nBT 1 0 0 1 42 387 Tm /F1 10 Tf (06  Removed on 2026-10-19 \\(originality check\\).) Tj ET'))
t = page_text(22); L = t.split('\n')
# Copperhead's ASMR tools were dropped by the same check: cut its row (16) first, so row 12's line numbers still hold.
c = next(j for j, l in enumerate(L) if '16  Copperhead' in l)
assert 'wrld_2a9a6669' in L[c + 10]
t = cut_lines(t, c - 2, c + 10, GREY + '\nBT 1 0 0 1 42 225 Tm /F1 10 Tf (16  Removed on 2026-10-19 \\(originality check\\).) Tj ET')
L = t.split('\n')
assert '12  Crystals Absence' in L[65] and 'wrld_68f4641c' in L[75]
set_text(22, cut_lines(t, 63, 75, GREY + '\nBT 1 0 0 1 42 549 Tm /F1 10 Tf (12  Removed on 2026-10-19 \\(originality check\\).) Tj ET'))
for annot, world in ((38, b'wrld_34693736'), (45, b'wrld_68f4641c'), (49, b'wrld_2a9a6669')):
    assert world in objs[annot], annot
for i, annot in ((21, 38), (22, 45), (22, 49)):
    n = pages[i - 1]
    assert re.search(rb'(?<!\d)%d 0 R' % annot, objs[n])
    objs[n] = re.sub(rb'(?<!\d)%d 0 R' % annot, b'', objs[n], count=1)

# p13: plate 6 without floors 34 and 35, and a note under it
w, h, raw, head = image_rgb(objs, 22)
raw = bytearray(raw)
for x0, y0, x1, y1 in ((515, 515, 1019, 1019), (5, 1026, 508, 1531)):
    for y in range(y0, y1 + 1):
        raw[(y * w + x0) * 3:(y * w + x1 + 1) * 3] = bytes((22, 15, 34)) * (x1 - x0 + 1)
put_stream(22, bytes(raw), b' /Type /XObject /Subtype /Image /Width %d /Height %d /ColorSpace /DeviceRGB /BitsPerComponent 8' % (w, h))
t = page_text(13)
t += '\n' + GREY + '\nBT 1 0 0 1 42 56 Tm /F1 8 Tf (Panels 34 and 35 removed on 2026-10-19 \\(originality check\\).) Tj ET\n'
set_text(13, t)

# page tree
kids = [n for i, n in enumerate(pages, 1) if i not in DROP]
objs[top] = re.sub(rb'/Count \d+', b'/Count %d' % len(kids), objs[top])
objs[top] = re.sub(rb'/Kids\s*\[.*?\]', b'/Kids [ ' + b' '.join(b'%d 0 R' % k for k in kids) + b' ]', objs[top], flags=re.S)

# write only what the catalog and info still reach
root = int(re.search(rb'/Root (\d+) 0 R', d).group(1)); info = int(re.search(rb'/Info (\d+) 0 R', d).group(1))
seen, todo = set(), [root, info]
while todo:
    n = todo.pop()
    if n in seen: continue
    seen.add(n)
    body = objs[n]; m = re.match(rb'\s*<<(.*?)>>\s*stream', body, re.S)
    refs = re.findall(rb'(?<![\d.])(\d+) 0 R', m.group(1) if m else body)
    todo += [int(r) for r in refs if int(r) in objs]
size = max(objs) + 1
out = bytearray(b'%PDF-1.4\n%\xe2\xe3\xcf\xd3\n'); offsets = {}
for n in sorted(seen):
    offsets[n] = len(out)
    out += b'%d 0 obj\n' % n + objs[n].strip(b'\n') + b'\nendobj\n'
xref = len(out)
out += b'xref\n0 %d\n0000000000 65535 f \n' % size
for n in range(1, size):
    out += (b'%010d 00000 n \n' % offsets[n]) if n in offsets else b'0000000000 65535 f \n'
out += b'trailer\n<< /Size %d /Root %d 0 R /Info %d 0 R >>\nstartxref\n%d\n%%%%EOF\n' % (size, root, info, xref)
open(dst, 'wb').write(out)
print('pages', len(kids), 'objects', len(seen), 'of', len(objs), 'bytes', len(out))
