"""Read-only: resolve every asset file (by GUID, recursively) that a set of start GUIDs needs.

Usage: deps.py <project root> <out json> <guid> [<guid> ...]
Prints the list of asset paths (relative to the project root) with sizes.
"""
import os, re, sys, json

root, out_path, *start = sys.argv[1:]
assets = os.path.join(root, 'Assets')
guid_re = re.compile(rb'guid: ([0-9a-f]{32})')
inc_re = re.compile(rb'#include\s+"([^"]+)"')

index = {}
for d, _, files in os.walk(assets):
    for f in files:
        if f.endswith('.meta'):
            p = os.path.join(d, f)
            with open(p, 'rb') as fh:
                m = guid_re.search(fh.read(4096))
            if m:
                index[m.group(1).decode()] = p[:-5]

need, queue, missing = {}, list(start), set()
while queue:
    g = queue.pop()
    if g in need or g in missing:
        continue
    p = index.get(g)
    if p is None:
        missing.add(g)
        continue
    need[g] = p
    if os.path.isdir(p):
        continue
    ext = os.path.splitext(p)[1].lower()
    if ext in ('.mat', '.asset', '.shader', '.cginc', '.hlsl', '.prefab', '.controller', '.anim', '.physicmaterial', '.renderTexture'.lower()):
        with open(p, 'rb') as fh:
            data = fh.read()
        if data[:5] == b'%YAML' or ext in ('.shader', '.cginc', '.hlsl'):
            for m in guid_re.finditer(data):
                queue.append(m.group(1).decode())
        if ext in ('.shader', '.cginc', '.hlsl'):
            for m in inc_re.finditer(data):
                inc = m.group(1).decode()
                cand = os.path.normpath(os.path.join(os.path.dirname(p), inc))
                if not os.path.exists(cand):
                    cand = os.path.normpath(os.path.join(root, inc))
                if os.path.exists(cand + '.meta'):
                    with open(cand + '.meta', 'rb') as fh:
                        mm = guid_re.search(fh.read(4096))
                    if mm:
                        queue.append(mm.group(1).decode())
                elif not inc.startswith(('UnityCG', 'Lighting', 'AutoLight', 'UnityPBS', 'UnityStandard', 'UnityShader', 'HLSLSupport', 'TerrainEngine', 'Packages/')):
                    missing.add('include:' + inc + ' (from ' + os.path.relpath(p, root) + ')')

rows = sorted((os.path.relpath(p, root).replace('\\', '/'), g, os.path.getsize(p) if os.path.isfile(p) else 0) for g, p in need.items())
for r, g, s in rows:
    print(f'{s:>10}  {g}  {r}')
print('missing:', sorted(missing))
with open(out_path, 'w') as fh:
    json.dump({'files': [r for r, _, _ in rows], 'missing': sorted(missing)}, fh, indent=1)
