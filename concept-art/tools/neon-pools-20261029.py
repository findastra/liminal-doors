"""Neon pools: flooded, tiled rooms with glowing slits of light and one door (Astra, 2026-10-29, after the flooded pool
corridor picture she loves). Four rooms, each in several neon colorways. Run with Blender 4.5, headless:
  blender -b --factory-startup -P neon-pools-20261029.py -- <out dir> [scene[:palette] ...]
With no scenes given it renders every room in every palette."""
import bpy, sys, os, math
from mathutils import Vector

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT = argv[0] if argv else os.getcwd()
WANT = argv[1:]
DATE = '20261029'
SIZE = 1024
SAMPLES = 128
EXPOSURE = float(os.environ.get('NP_EXPOSURE', -2.0))
VIEW = os.environ.get('NP_VIEW', 'Standard')

# window light, the two side lights, door, door glow, water, tile tint, ambient; colors are linear RGB
PALETTES = {
    'dream':  dict(win=(0.85, 0.75, 1.0), left=(0.55, 0.15, 1.0), right=(1.0, 0.25, 0.65), door=(0.5, 0.4, 0.9), glow=(0.7, 0.4, 1.0), water=(0.02, 0.08, 1.0), tile=(0.85, 0.82, 0.95), amb=(0.06, 0.03, 0.3)),
    'acid':   dict(win=(0.8, 1.0, 0.4), left=(0.35, 1.0, 0.0), right=(1.0, 0.0, 0.7), door=(1.0, 0.05, 0.55), glow=(1.0, 0.1, 0.6), water=(0.0, 0.55, 0.25), tile=(0.85, 0.95, 0.8), amb=(0.05, 0.2, 0.02)),
    'fire-ice': dict(win=(1.0, 0.7, 0.35), left=(1.0, 0.3, 0.0), right=(1.0, 0.02, 0.08), door=(0.0, 0.85, 1.0), glow=(0.0, 0.8, 1.0), water=(0.0, 0.6, 1.0), tile=(0.95, 0.85, 0.8), amb=(0.25, 0.04, 0.0)),
    'toxic':  dict(win=(0.8, 1.0, 0.7), left=(0.5, 0.0, 1.0), right=(0.1, 1.0, 0.2), door=(0.95, 1.0, 0.0), glow=(0.7, 1.0, 0.0), water=(0.35, 0.0, 0.9), tile=(0.85, 0.85, 0.9), amb=(0.15, 0.0, 0.3)),
    'candy':  dict(win=(1.0, 0.95, 0.6), left=(1.0, 0.75, 0.0), right=(1.0, 0.05, 0.45), door=(0.0, 0.9, 0.85), glow=(0.0, 1.0, 0.8), water=(1.0, 0.02, 0.35), tile=(0.98, 0.9, 0.9), amb=(0.3, 0.02, 0.1)),
    'ultra-red': dict(win=(1.0, 0.6, 0.6), left=(1.0, 0.0, 0.05), right=(1.0, 0.0, 0.3), door=(0.0, 1.0, 0.9), glow=(0.0, 1.0, 0.85), water=(0.6, 0.0, 0.02), tile=(0.95, 0.8, 0.8), amb=(0.25, 0.0, 0.02)),
}


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    try:
        prefs = bpy.context.preferences.addons['cycles'].preferences
        for kind in ('HIP', 'CUDA', 'OPTIX'):
            try:
                prefs.compute_device_type = kind; prefs.get_devices()
                if any(d.type == kind for d in prefs.devices):
                    for d in prefs.devices: d.use = d.type == kind
                    sc.cycles.device = 'GPU'; break
            except Exception:
                pass
    except Exception:
        pass
    sc.cycles.samples = SAMPLES
    sc.cycles.use_denoising = True
    sc.cycles.max_bounces = 10; sc.cycles.transmission_bounces = 10; sc.cycles.glossy_bounces = 6
    sc.cycles.caustics_reflective = False; sc.cycles.caustics_refractive = False
    sc.render.resolution_x = SIZE; sc.render.resolution_y = SIZE
    sc.view_settings.view_transform = VIEW
    try: sc.view_settings.look = 'AgX - Punchy' if VIEW == 'AgX' else 'None'
    except Exception: pass
    sc.view_settings.exposure = EXPOSURE
    return sc


def mat_tiles(name, axes, tint, emit=None, emit_strength=0.0, tile=0.2):
    """glossy square tiles with grout, laid in world space on the two given axes ('xz', 'yz', 'xy')"""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; n = nt.nodes; L = nt.links
    bsdf = n['Principled BSDF']
    geo = n.new('ShaderNodeNewGeometry'); sep = n.new('ShaderNodeSeparateXYZ'); comb = n.new('ShaderNodeCombineXYZ')
    L.new(geo.outputs['Position'], sep.inputs[0])
    idx = {'x': 0, 'y': 1, 'z': 2}
    L.new(sep.outputs[idx[axes[0]]], comb.inputs[0]); L.new(sep.outputs[idx[axes[1]]], comb.inputs[1])
    br = n.new('ShaderNodeTexBrick'); br.offset = 0.0; br.squash = 1.0
    br.inputs['Scale'].default_value = 1.0; br.inputs['Mortar Size'].default_value = tile * 0.035
    br.inputs['Brick Width'].default_value = tile; br.inputs['Row Height'].default_value = tile
    br.inputs['Color1'].default_value = (*tint, 1); br.inputs['Color2'].default_value = (*[c * 0.93 for c in tint], 1)
    br.inputs['Mortar'].default_value = (*[c * 0.78 for c in tint], 1)
    L.new(comb.outputs[0], br.inputs['Vector'])
    L.new(br.outputs['Color'], bsdf.inputs['Base Color'])
    rough = n.new('ShaderNodeMapRange'); rough.inputs['To Min'].default_value = 0.08; rough.inputs['To Max'].default_value = 0.6
    L.new(br.outputs['Fac'], rough.inputs['Value']); L.new(rough.outputs[0], bsdf.inputs['Roughness'])
    bsdf.inputs['Coat Weight'].default_value = 0.4
    bump = n.new('ShaderNodeBump'); bump.inputs['Distance'].default_value = 0.004; bump.invert = True
    L.new(br.outputs['Fac'], bump.inputs['Height']); L.new(bump.outputs[0], bsdf.inputs['Normal'])
    if emit:
        bsdf.inputs['Emission Color'].default_value = (*emit, 1); bsdf.inputs['Emission Strength'].default_value = emit_strength
    return m


def mat_plain(name, col, rough=0.4, emit=None, es=0.0, metal=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True; b = m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = (*col, 1); b.inputs['Roughness'].default_value = rough; b.inputs['Metallic'].default_value = metal
    if emit:
        b.inputs['Emission Color'].default_value = (*emit, 1); b.inputs['Emission Strength'].default_value = es
    return m


def mat_emit(name, col, strength):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree
    for x in list(nt.nodes): nt.nodes.remove(x)
    e = nt.nodes.new('ShaderNodeEmission'); o = nt.nodes.new('ShaderNodeOutputMaterial')
    e.inputs[0].default_value = (*col, 1); e.inputs[1].default_value = strength
    nt.links.new(e.outputs[0], o.inputs[0])
    return m


def mat_water(name, tint):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; n = nt.nodes; L = nt.links
    b = n['Principled BSDF']
    b.inputs['Base Color'].default_value = (*[0.3 + 0.7 * c for c in tint], 1)
    b.inputs['Transmission Weight'].default_value = 1.0; b.inputs['IOR'].default_value = 1.333
    b.inputs['Roughness'].default_value = 0.015
    geo = n.new('ShaderNodeNewGeometry')
    w1 = n.new('ShaderNodeTexWave'); w1.wave_type = 'BANDS'; w1.bands_direction = 'Y'
    w1.inputs['Scale'].default_value = 0.35; w1.inputs['Distortion'].default_value = 6; w1.inputs['Detail'].default_value = 3
    no = n.new('ShaderNodeTexNoise'); no.inputs['Scale'].default_value = 0.9; no.inputs['Detail'].default_value = 3
    mix = n.new('ShaderNodeMath'); mix.operation = 'ADD'
    L.new(geo.outputs['Position'], w1.inputs['Vector']); L.new(geo.outputs['Position'], no.inputs['Vector'])
    L.new(w1.outputs['Fac'], mix.inputs[0]); L.new(no.outputs['Fac'], mix.inputs[1])
    bump = n.new('ShaderNodeBump'); bump.inputs['Distance'].default_value = 0.06; bump.inputs['Strength'].default_value = 0.35
    L.new(mix.outputs[0], bump.inputs['Height']); L.new(bump.outputs[0], b.inputs['Normal'])
    return m


def box(name, center, dims, mat, render=True):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    o = bpy.context.object; o.name = name; o.scale = dims
    bpy.ops.object.transform_apply(scale=True)
    if mat: o.data.materials.append(mat)
    o.hide_render = not render
    return o


def cut(target, center, dims):
    c = box(target.name + '-cut', center, dims, None, render=False); c.display_type = 'WIRE'
    mod = target.modifiers.new('cut', 'BOOLEAN'); mod.operation = 'DIFFERENCE'; mod.object = c; mod.solver = 'EXACT'
    return c


def area(center, rot, sx, sy, col, energy):
    d = bpy.data.lights.new('win-light', 'AREA'); d.shape = 'RECTANGLE'; d.size = sx; d.size_y = sy
    d.color = col; d.energy = energy
    o = bpy.data.objects.new('win-light', d); o.location = center; o.rotation_euler = rot
    o.visible_glossy = False; o.visible_transmission = False
    bpy.context.collection.objects.link(o)
    return o


def point(center, col, energy, radius=0.3):
    d = bpy.data.lights.new('pt', 'POINT'); d.color = col; d.energy = energy; d.shadow_soft_size = radius
    o = bpy.data.objects.new('pt', d); o.location = center; bpy.context.collection.objects.link(o)
    o.visible_glossy = False; o.visible_transmission = False


class Room:
    """a flooded tiled room along +Y: x from -W/2 to W/2, water at z=0, pool floor at -D, ceiling at Hc, door wall at y=L"""

    def __init__(self, P, W, L, Hc, D=0.7, T=0.35):
        self.P, self.W, self.L, self.Hc, self.D, self.T = P, W, L, Hc, D, T
        tint = [0.42 * c for c in P['tile']]
        # each side wall's tiles lean toward its own light, so two neon lights don't mix to grey on them
        self.m_side_l = mat_tiles('side-l', 'yz', [0.5 * a + 0.5 * 0.45 * b for a, b in zip(tint, P['left'])])
        self.m_side_r = mat_tiles('side-r', 'yz', [0.5 * a + 0.5 * 0.45 * b for a, b in zip(tint, P['right'])])
        self.m_end = mat_tiles('end', 'xz', tint)
        self.m_ceil = mat_tiles('ceil', 'xy', tint)
        self.m_pool = mat_tiles('pool', 'xy', [0.5 + 0.5 * c for c in P['water']], emit=P['water'], emit_strength=1.6)
        self.m_poolside = mat_tiles('poolside', 'yz', [0.5 + 0.5 * c for c in P['water']], emit=P['water'], emit_strength=0.5)
        self.m_poolend = mat_tiles('poolend', 'xz', [0.5 + 0.5 * c for c in P['water']], emit=P['water'], emit_strength=0.5)
        H = Hc + D
        zc = (Hc - D) / 2
        # the walls above the water carry the windows; below the water the tiles glow with the pool's color
        self.left = box('wall-l', (-W / 2 - T / 2, L / 2, Hc / 2), (T, L + 2, Hc), self.m_side_l)
        self.right = box('wall-r', (W / 2 + T / 2, L / 2, Hc / 2), (T, L + 2, Hc), self.m_side_r)
        self.end = box('wall-end', (0, L + T / 2, Hc / 2), (W + 2 * T, T, Hc), self.m_end)
        box('wall-back', (0, -1 - T / 2, zc), (W + 2 * T, T, H), self.m_end)
        box('ceiling', (0, L / 2, Hc + T / 2), (W + 2 * T, L + 2, T), self.m_ceil)
        box('pool-floor', (0, L / 2, -D - T / 2), (W + 2 * T, L + 2, T), self.m_pool)
        box('pool-l', (-W / 2 - T / 2, L / 2, -D / 2), (T, L + 2, D), self.m_poolside)
        box('pool-r', (W / 2 + T / 2, L / 2, -D / 2), (T, L + 2, D), self.m_poolside)
        box('pool-end', (0, L + T / 2, -D / 2), (W + 2 * T, T, D), self.m_poolend)
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0, L / 2 - 0.5, 0))
        w = bpy.context.object; w.scale = (W, L + 1, 1); bpy.ops.object.transform_apply(scale=True)
        w.data.materials.append(mat_water('water', P['water']))
        sub = w.modifiers.new('sub', 'SUBSURF'); sub.subdivision_type = 'SIMPLE'; sub.levels = 0; sub.render_levels = 0
        # the world: a faint colored dark
        world = bpy.data.worlds.new('w'); bpy.context.scene.world = world; world.use_nodes = True
        world.node_tree.nodes['Background'].inputs[0].default_value = (*P['amb'], 1)
        world.node_tree.nodes['Background'].inputs[1].default_value = 0.15

    def window(self, side, y, z0, w, h, col=None, power=1.0, strength=9.0):
        """a tall glowing slit in a side wall ('l' or 'r') or the end wall ('e', then y is x)"""
        P, W, T = self.P, self.W, self.T
        col = col or (P['left'] if side == 'l' else P['right'] if side == 'r' else P['win'])
        glass = mat_emit('glass', [0.55 * a + 0.45 * b for a, b in zip(P['win'], col)], strength)
        if side in 'lr':
            s = -1 if side == 'l' else 1
            wall = self.left if side == 'l' else self.right
            cut(wall, (s * (W / 2 + T / 2), y, z0 + h / 2), (T * 3, w, h))
            box('glass', (s * (W / 2 + 0.08), y, z0 + h / 2), (0.02, w + 0.2, h + 0.2), glass)
            area((s * (W / 2 + 0.02), y, z0 + h / 2), (0, math.radians(-90 * -s), 0), h, w, col, 120 * w * h * power)
        else:
            x = y
            cut(self.end, (x, self.L + T / 2, z0 + h / 2), (w, T * 3, h))
            box('glass', (x, self.L + 0.08, z0 + h / 2), (w + 0.2, 0.02, h + 0.2), glass)
            area((x, self.L - 0.02, z0 + h / 2), (math.radians(-90), 0, 0), w, h, col, 110 * w * h * power)

    def door(self, z0=0.05, x=0.0, y=None, w=1.0, h=2.15, light=1.0):
        P, L = self.P, (self.L if y is None else y)
        dm = mat_plain('door', P['door'], 0.35, emit=P['door'], es=0.7)
        fm = mat_plain('frame', [0.7 * c + 0.2 for c in P['door']], 0.3)
        hm = mat_plain('handle', (0.9, 0.9, 0.95), 0.15, metal=1.0)
        box('door', (x, L - 0.05, z0 + h / 2), (w, 0.08, h), dm)
        box('frame-l', (x - w / 2 - 0.04, L - 0.06, z0 + h / 2 + 0.04), (0.08, 0.12, h + 0.08), fm)
        box('frame-r', (x + w / 2 + 0.04, L - 0.06, z0 + h / 2 + 0.04), (0.08, 0.12, h + 0.08), fm)
        box('frame-t', (x, L - 0.06, z0 + h + 0.04), (w + 0.16, 0.12, 0.08), fm)
        box('handle', (x + w / 2 - 0.12, L - 0.12, z0 + h * 0.48), (0.12, 0.03, 0.025), hm)
        box('plate', (x + w / 2 - 0.1, L - 0.1, z0 + h * 0.46), (0.035, 0.012, 0.16), hm)
        point((x, L - 1.6, z0 + h + 0.9), P['glow'], 220 * light, 0.6)
        point((x, L - 0.8, z0 + 0.3), P['glow'], 40 * light, 0.3)

    def camera(self, loc, target, lens=22):
        d = bpy.data.cameras.new('cam'); d.lens = lens; d.sensor_width = 36
        o = bpy.data.objects.new('cam', d); bpy.context.collection.objects.link(o); o.location = loc
        o.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
        bpy.context.scene.camera = o


def corridor(P):
    """the long flooded corridor: slits on both sides, the door at the end"""
    r = Room(P, W=4.2, L=24, Hc=4.4)
    for y, s, w, h in [(4.5, 'l', 0.95, 3.0), (10.5, 'l', 0.6, 2.4), (16.5, 'l', 0.4, 2.0), (6.0, 'r', 1.0, 3.0), (12.5, 'r', 0.55, 2.4), (18.5, 'r', 0.35, 2.0)]:
        r.window(s, y, 0.55, w, h)
    r.door(z0=0.0)
    r.camera((0.25, 0.2, 1.05), (0, 24, 1.15), lens=24)


def stairs(P):
    """a wide flooded hall; broad tiled steps rise out of the water to a landing and the door, slits either side"""
    r = Room(P, W=7.5, L=16, Hc=6.5)
    step = mat_tiles('step', 'xy', P['tile']); riser = mat_tiles('riser', 'xz', P['tile'])
    n, rise, run, y0 = 7, 0.24, 0.42, 10.0
    for i in range(n):
        z = -0.25 + rise * (i + 1)
        y = y0 + run * i
        box('step', (0, y + (16 - y) / 2, (z - 0.8) / 2), (4.2 - i * 0.12, 16 - y, z + 0.8), step)
    top = -0.25 + rise * n
    for x in (-2.6, 2.6):
        r.window('e', x, top + 0.2, 0.55, 4.6)
    r.window('l', 5.0, 0.6, 0.8, 4.2); r.window('r', 3.5, 0.6, 0.8, 4.2); r.window('l', 8.5, 0.6, 0.5, 4.2, power=0.7)
    r.door(z0=top)
    r.camera((0.0, 0.0, 1.25), (0, 16, 1.9), lens=24)


def colonnade(P):
    """square tiled pillars standing in the water, slits behind them on both walls, the door at the end"""
    r = Room(P, W=8.0, L=24, Hc=4.8)
    pm = mat_tiles('pillar', 'yz', P['tile']); pm2 = mat_tiles('pillar2', 'xz', P['tile'])
    for y in [7.5, 11.5, 15.5, 19.5]:
        for x in (-2.3, 2.3):
            b = box('pillar', (x, y, 4.8 / 2 - 0.35), (0.7, 0.7, 4.8 + 0.7), pm)
            b.data.materials.append(pm2)
            # the x-facing sides use pm, the y-facing sides pm2
            for poly in b.data.polygons:
                poly.material_index = 1 if abs(poly.normal.y) > 0.5 else 0
    for y in [7.5, 11.5, 15.5, 19.5]:
        r.window('l', y, 0.5, 0.9, 3.4); r.window('r', y, 0.5, 0.9, 3.4)
    r.window('e', -2.4, 0.5, 0.5, 3.6); r.window('e', 2.4, 0.5, 0.5, 3.6)
    r.door(z0=0.0, light=1.3)
    r.camera((0.9, 0.0, 1.1), (0, 24, 1.3), lens=22)


def skylight(P):
    """a tall, narrow flooded room lit from one long slit in the ceiling, the door at the end under a thin slit"""
    r = Room(P, W=3.4, L=18, Hc=7.5)
    cut(bpy.data.objects['ceiling'], (0, 9.5, 7.5 + 0.17), (0.45, 14, 1))
    sky = mat_emit('sky', [0.5 * a + 0.5 * b for a, b in zip(P['win'], P['left'])], 10)
    box('sky-glass', (0, 9.5, 7.5 + 0.4), (0.8, 14.4, 0.02), sky)
    for y in range(3, 17, 2):
        area((0, y, 7.45), (0, 0, 0), 0.45, 2, P['left'], 220)
    r.window('e', 0, 2.6, 0.22, 4.4, col=P['right'], strength=12)
    r.window('r', 7.0, 0.4, 1.1, 2.6, col=P['right'], power=1.4)
    r.door(z0=0.0)
    r.camera((-0.3, 0.0, 0.85), (0, 18, 2.2), lens=20)


SCENES = {'corridor': corridor, 'stairs': stairs, 'colonnade': colonnade, 'skylight': skylight}
ORDER = list(SCENES)


def render(scene, pal):
    sc = reset()
    SCENES[scene](PALETTES[pal])
    # a soft bloom round the bright slits
    sc.use_nodes = True
    nt = sc.node_tree
    rl = nt.nodes.get('Render Layers') or nt.nodes.new('CompositorNodeRLayers')
    comp = nt.nodes.get('Composite') or nt.nodes.new('CompositorNodeComposite')
    g = nt.nodes.new('CompositorNodeGlare')
    g.glare_type = 'FOG_GLOW'; g.quality = 'HIGH'
    for k, v in (('Threshold', 1.0), ('Strength', 0.22), ('Size', 0.55), ('Saturation', 1.2)):
        if k in g.inputs: g.inputs[k].default_value = v
    nt.links.new(rl.outputs['Image'], g.inputs['Image']); nt.links.new(g.outputs['Image'], comp.inputs['Image'])
    num = ORDER.index(scene) + 2
    name = f'neon-pool-{num:02d}-{scene}-{pal}-{DATE}.png'
    sc.render.filepath = os.path.join(OUT, name)
    bpy.ops.render.render(write_still=True)
    print('RENDERED', name, flush=True)


jobs = []
for w in (WANT or [f'{s}:{p}' for s in ORDER for p in PALETTES]):
    s, _, p = w.partition(':')
    jobs += [(s, p)] if p else [(s, q) for q in PALETTES]
for s, p in jobs:
    render(s, p)
