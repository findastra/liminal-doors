"""Read-only scan of a Unity scene YAML: print the subtree under named GameObjects."""
import re, sys, json

scene_path, *roots = sys.argv[1:]
hdr = re.compile(r'^--- !u!(\d+) &(-?\d+)')
docs = {}
cur = None
with open(scene_path, encoding='utf-8', errors='replace') as f:
    for line in f:
        m = hdr.match(line)
        if m:
            cur = {'cls': int(m.group(1)), 'id': m.group(2), 'lines': []}
            docs[cur['id']] = cur
            continue
        if cur is not None:
            cur['lines'].append(line.rstrip('\n'))

def field(d, name):
    for l in d['lines']:
        s = l.strip()
        if s.startswith(name + ':'):
            return s[len(name) + 1:].strip()
    return None

def refs(d, name):
    """fileIDs in a list field like m_Children or m_Component."""
    out, on = [], False
    for l in d['lines']:
        s = l.strip()
        if s.startswith(name + ':'):
            on = True
            if s.endswith('[]'):
                return []
            continue
        if on:
            if s.startswith('- '):
                m = re.search(r'fileID: (-?\d+)', s)
                if m: out.append(m.group(1))
            else:
                break
    return out

def guids(d, name):
    out, on = [], False
    for l in d['lines']:
        s = l.strip()
        if s.startswith(name + ':'):
            on = True
            m = re.search(r'guid: ([0-9a-f]{32})', s)
            if m: out.append(m.group(1)); return out
            continue
        if on:
            if s.startswith('- '):
                m = re.search(r'guid: ([0-9a-f]{32})', s)
                out.append(m.group(1) if m else None)
            else:
                break
    return out

gos = {k: d for k, d in docs.items() if d['cls'] == 1}
CLS = {4: 'Transform', 33: 'MeshFilter', 23: 'MeshRenderer', 65: 'BoxCollider', 64: 'MeshCollider',
       114: 'MonoBehaviour', 82: 'AudioSource', 108: 'Light', 198: 'ParticleSystem', 199: 'ParticleSystemRenderer',
       135: 'SphereCollider', 136: 'CapsuleCollider', 54: 'Rigidbody'}

def node(go_id):
    g = gos[go_id]
    comps = [docs[c] for c in refs(g, 'm_Component') if c in docs]
    n = {'name': field(g, 'm_Name'), 'active': field(g, 'm_IsActive'), 'layer': field(g, 'm_Layer'), 'components': [], 'children': []}
    tr = None
    for c in comps:
        kind = CLS.get(c['cls'], str(c['cls']))
        e = {'type': kind}
        if c['cls'] == 4:
            tr = c
            e.update(pos=field(c, 'm_LocalPosition'), rot=field(c, 'm_LocalRotation'), scale=field(c, 'm_LocalScale'))
        elif c['cls'] == 33:
            e['mesh'] = guids(c, 'm_Mesh')
        elif c['cls'] in (23, 199):
            e['materials'] = guids(c, 'm_Materials')
        elif c['cls'] == 65:
            e.update(trigger=field(c, 'm_IsTrigger'), size=field(c, 'm_Size'), center=field(c, 'm_Center'))
        elif c['cls'] == 114:
            e['script'] = guids(c, 'm_Script')
        elif c['cls'] == 108:
            e.update(color=field(c, 'm_Color'), intensity=field(c, 'm_Intensity'), range=field(c, 'm_Range'), ltype=field(c, 'm_Type'))
        n['components'].append(e)
    if tr is not None:
        for ch in refs(tr, 'm_Children'):
            t = docs.get(ch)
            if t is None: continue
            gid = re.search(r'fileID: (-?\d+)', field(t, 'm_GameObject') or '')
            if gid and gid.group(1) in gos:
                n['children'].append(node(gid.group(1)))
        n['father'] = field(tr, 'm_Father')
    return n

def world_chain(go_id):
    """Names of ancestors from the root, plus each ancestor's local TRS."""
    chain = []
    g = gos[go_id]
    tr = next((docs[c] for c in refs(g, 'm_Component') if c in docs and docs[c]['cls'] == 4), None)
    while tr is not None:
        gid = re.search(r'fileID: (-?\d+)', field(tr, 'm_GameObject')).group(1)
        chain.append({'name': field(gos[gid], 'm_Name'), 'pos': field(tr, 'm_LocalPosition'),
                      'rot': field(tr, 'm_LocalRotation'), 'scale': field(tr, 'm_LocalScale')})
        fa = re.search(r'fileID: (-?\d+)', field(tr, 'm_Father') or '')
        tr = docs.get(fa.group(1)) if fa and fa.group(1) != '0' else None
    return list(reversed(chain))

out = []
for r in roots:
    for k, g in gos.items():
        if field(g, 'm_Name') == r:
            out.append({'root': r, 'fileID': k, 'ancestors': world_chain(k), 'tree': node(k)})
print(json.dumps(out, indent=1))
