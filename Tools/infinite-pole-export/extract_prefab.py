"""Read a Unity scene (read-only) and write one GameObject subtree out as a standalone .prefab file.

Usage: extract_prefab.py <scene> <root GameObject name> <out .prefab>
Keeps GameObjects, Transforms, meshes, renderers, colliders, particles and lights byte-for-byte.
Drops MonoBehaviours (Udon scripts need the SDK side wired separately) and nulls any reference
that points outside the subtree. Prints what it dropped and every asset GUID the prefab uses.
"""
import re, sys, uuid, json

scene, root_name, out = sys.argv[1:]
hdr = re.compile(r'^--- !u!(\d+) &(-?\d+)(.*)$')
docs, order, cur = {}, [], None
with open(scene, encoding='utf-8', newline='') as f:
    for line in f:
        m = hdr.match(line.rstrip('\r\n'))
        if m:
            cur = {'cls': int(m.group(1)), 'id': m.group(2), 'head': line, 'body': []}
            docs[cur['id']] = cur
            order.append(cur['id'])
        elif cur is not None:
            cur['body'].append(line)

def val(d, key):
    for l in d['body']:
        s = l.strip()
        if s.startswith(key + ':'):
            return s[len(key) + 1:].strip()

def ids_in_list(d, key):
    out, on = [], False
    for l in d['body']:
        s = l.strip()
        if s.startswith(key + ':'):
            on = not s.endswith('[]')
            continue
        if on:
            if s.startswith('- '):
                m = re.search(r'fileID: (-?\d+)', s)
                if m: out.append(m.group(1))
            else:
                break
    return out

KEEP = {1, 4, 33, 23, 65, 64, 135, 136, 198, 199, 108, 82, 54, 137}
root_go = next(k for k, d in docs.items() if d['cls'] == 1 and val(d, 'm_Name') == root_name)
keep, dropped = set(), []

def walk(go):
    keep.add(go)
    tr = None
    for c in ids_in_list(docs[go], 'm_Component'):
        d = docs.get(c)
        if d is None: continue
        if d['cls'] in KEEP:
            keep.add(c)
            if d['cls'] == 4: tr = d
        else:
            dropped.append((val(docs[go], 'm_Name'), d['cls'], c))
    if tr:
        for ch in ids_in_list(tr, 'm_Children'):
            g = re.search(r'fileID: (-?\d+)', val(docs[ch], 'm_GameObject')).group(1)
            walk(g)

walk(root_go)
drop_ids = {c for _, _, c in dropped}
root_tr = next(c for c in ids_in_list(docs[root_go], 'm_Component') if docs[c]['cls'] == 4)

ref_re = re.compile(r'\{fileID: (-?\d+)\}')
external = []
lines_out = ['%YAML 1.1\n', '%TAG !u! tag:unity3d.com,2011:\n']
guids = set()
for k in order:
    if k not in keep: continue
    d = docs[k]
    lines_out.append(d['head'])
    for l in d['body']:
        s = l.strip()
        if s.startswith('- component: {fileID: ') and re.search(r'fileID: (-?\d+)', s).group(1) in drop_ids:
            continue
        if k == root_tr and s.startswith('m_Father:'):
            l = l[:len(l) - len(l.lstrip())] + 'm_Father: {fileID: 0}' + ('\r\n' if l.endswith('\r\n') else '\n')
        def fix(m):
            fid = m.group(1)
            if fid == '0' or fid in keep: return m.group(0)
            external.append((d['cls'], k, s[:80]))
            return '{fileID: 0}'
        l = ref_re.sub(fix, l)
        for g in re.findall(r'guid: ([0-9a-f]{32})', l):
            guids.add(g)
        lines_out.append(l)

with open(out, 'w', encoding='utf-8', newline='') as f:
    f.writelines(lines_out)
with open(out + '.meta', 'w', encoding='utf-8', newline='\n') as f:
    f.write('fileFormatVersion: 2\nguid: %s\nPrefabImporter:\n  externalObjects: {}\n  userData: \n'
            '  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex)
print(json.dumps({'objects': len(keep), 'dropped': dropped, 'external_refs_nulled': external,
                  'guids': sorted(guids)}, indent=1))
