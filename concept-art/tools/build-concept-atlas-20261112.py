#!/usr/bin/env python3
"""Build the Liminal Doors Concept Atlas (Astra's Collection page) from this repo.

The Atlas is the page where Astra reviews every floor: a check for "I have been here", a + to put floors on the
redline sheet, and a T that lists the tools each floor was made with. Its first builder (collection-template.html and
build-collection.py) was lost from the PC; this one rebuilds the same page from:

  concept-art/atlas/concept-atlas-template-20261112.html   the page, with placeholders for its data
  concept-art/atlas/concept-atlas-floors-20261112.json     the floors on the page: number, animal, place
  docs/tools-by-floor-20261019.json                        the T view: which tools made which floor
  concept-art/floors/NN-*.png                              one picture per floor (made into web JPEGs here)
  concept-art/liminal-doors-redline-sheet-20261019.jpg     the full redline sheet

Usage:
  python concept-art/tools/build-concept-atlas-20261112.py --out <empty folder>
  python concept-art/tools/build-concept-atlas-20261112.py --out <folder> --threshold-link https://...

It writes <out>/liminal-doors-concept-atlas-20261019.html and <out>/liminal-doors-assets/ (34 floor pictures and the
redline sheet), the same files the published Atlas serves. The page's link to The Threshold's hosted page is left out
unless --threshold-link gives it: that address holds Astra's name, so it never goes in this public repo.

Needs Python 3.9+ and Pillow (pip install pillow) to make the floor JPEGs; with --pictures <folder of
floor-NN-*.jpg> it copies ready-made web copies instead and needs no Pillow.

Written by Claude Opus 5.5 (claude-opus-5-5), 2026-11-12, at Astra's request (hub card 336).
"""
import argparse
import html
import json
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TEMPLATE = ROOT / 'concept-art' / 'atlas' / 'concept-atlas-template-20261112.html'
FLOORS = ROOT / 'concept-art' / 'atlas' / 'concept-atlas-floors-20261112.json'
TOOLS = ROOT / 'docs' / 'tools-by-floor-20261019.json'
PICTURES = ROOT / 'concept-art' / 'floors'
SHEET = ROOT / 'concept-art' / 'liminal-doors-redline-sheet-20261019.jpg'
PAGE = 'liminal-doors-concept-atlas-20261019.html'
# The page asks for every floor as floor-NN-20261015.jpg (the date the plates were drawn), whatever its PNG's date.
WEB_NAME = 'floor-{:02d}-20261015.jpg'


def fail(msg):
    sys.exit('build-concept-atlas: ' + msg)


def jpeg_size(path):
    """Width and height of a JPEG, read from its frame header (no Pillow needed)."""
    data = path.read_bytes()
    i = 2
    while i < len(data) - 9:
        if data[i] != 0xFF:
            i += 1
            continue
        marker = data[i + 1]
        if marker in (0xC0, 0xC1, 0xC2, 0xC3, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF):
            return int.from_bytes(data[i + 7:i + 9], 'big'), int.from_bytes(data[i + 5:i + 7], 'big')
        i += 2 + int.from_bytes(data[i + 2:i + 4], 'big')
    fail(f'{path.name} is not a JPEG I can read')


def embed(obj):
    """JSON for a <script type="application/json"> block: compact, and never closing the script early."""
    return json.dumps(obj, ensure_ascii=False).replace('</', '<\\/')


def load_floors():
    floors = json.loads(FLOORS.read_text(encoding='utf-8'))
    seen = set()
    for f in floors:
        if not isinstance(f.get('n'), int) or not isinstance(f.get('animal'), str) or not isinstance(f.get('place'), str):
            fail(f'{FLOORS.name}: every floor needs n (a number), animal and place: {f!r}')
        if f['n'] in seen:
            fail(f'{FLOORS.name}: floor {f["n"]} is listed twice')
        seen.add(f['n'])
    return floors


def load_tools(floors):
    tools = json.loads(TOOLS.read_text(encoding='utf-8'))
    if tools.get('schema') != 'liminal-doors.tools-by-floor/v1':
        fail(f'{TOOLS.name} is not a liminal-doors.tools-by-floor/v1 file')
    ids = set(tools.get('tools', {}))
    used = list(tools.get('every_floor', [])) + list(tools.get('whole_world', []))
    for n, on_floor in tools.get('floors', {}).items():
        used += on_floor
        if int(n) not in {f['n'] for f in floors}:
            print(f'note: {TOOLS.name} lists tools for floor {n}, which is not on the page')
    missing = sorted(set(used) - ids)
    if missing:
        fail(f'{TOOLS.name} uses tools it does not describe: {", ".join(missing)}')
    return tools


def floor_png(n):
    found = sorted(PICTURES.glob(f'{n:02d}-*.png'))
    if not found:
        fail(f'no picture for floor {n:02d} in concept-art/floors')
    return found[-1]  # the newest, when a floor was redrawn (names end in their date)


def make_pictures(floors, out, ready, quality):
    dest = out / 'liminal-doors-assets' / 'floors'
    dest.mkdir(parents=True, exist_ok=True)
    if ready:
        for f in floors:
            found = sorted(Path(ready).glob(f'floor-{f["n"]:02d}-*.jpg'))
            if not found:
                fail(f'no floor-{f["n"]:02d}-*.jpg in {ready}')
            shutil.copyfile(found[-1], dest / WEB_NAME.format(f['n']))
        return
    try:
        from PIL import Image
    except ImportError:
        fail('making the floor JPEGs needs Pillow (pip install pillow), or pass --pictures <folder of floor-NN-*.jpg>')
    for f in floors:
        with Image.open(floor_png(f['n'])) as im:
            im = im.convert('RGBA')
            flat = Image.new('RGB', im.size, (0, 0, 0))  # like floors-to-jpg.ps1: transparency on black
            flat.paste(im, mask=im.getchannel('A'))
            flat.save(dest / WEB_NAME.format(f['n']), 'JPEG', quality=quality)


def main():
    ap = argparse.ArgumentParser(description='Build the Liminal Doors Concept Atlas from this repo.')
    ap.add_argument('--out', required=True, help='folder to write the page and its pictures into (empty or new)')
    ap.add_argument('--threshold-link', help="https address of The Threshold's hosted page; left out when not given")
    ap.add_argument('--pictures', help='folder of ready web copies (floor-NN-*.jpg) to use instead of making them')
    ap.add_argument('--quality', type=int, default=90, help='JPEG quality of the floor pictures (default 90)')
    a = ap.parse_args()

    out = Path(a.out).resolve()
    if out.exists() and any(out.iterdir()):
        fail(f'{out} is not empty; give an empty or new folder')
    if out == ROOT or ROOT in out.parents:
        fail('write the build outside the repo: it is made from the repo, so it is never committed')

    floors = load_floors()
    tools = load_tools(floors)
    for f in floors:
        floor_png(f['n'])

    link = ''
    if a.threshold_link:
        if not a.threshold_link.startswith('https://'):
            fail('--threshold-link must be an https address')
        link = f'<a href="{html.escape(a.threshold_link, quote=True)}">The Threshold ↗</a>'

    out.mkdir(parents=True, exist_ok=True)
    make_pictures(floors, out, a.pictures, a.quality)
    sheet = out / 'liminal-doors-assets' / SHEET.name
    shutil.copyfile(SHEET, sheet)
    w, h = jpeg_size(sheet)

    page = TEMPLATE.read_text(encoding='utf-8')
    for key, value in {
        'FLOORS_DATA': embed(floors),
        'TOOLS_DATA': embed(tools),
        'FLOOR_COUNT': str(len(floors)),
        'SHEET_SIZE': f'{w} × {h} px',
        'THRESHOLD_LINK': link,
    }.items():
        if '{{' + key + '}}' not in page:
            fail(f'the template has no {{{{{key}}}}}')
        page = page.replace('{{' + key + '}}', value)
    (out / PAGE).write_text(page, encoding='utf-8', newline='\n')

    print(f'{out / PAGE}')
    print(f'{len(floors)} floors, {len(tools["tools"])} tools (ref {tools.get("ref")}), redline sheet {w} x {h} px')
    print('To publish it as the Atlas artifact: the page is the file, and every file under liminal-doors-assets/ goes')
    print('in `files` at its path relative to the page.')


if __name__ == '__main__':
    main()
