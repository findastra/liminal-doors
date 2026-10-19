# Claude, 2026-10-19. The Liminal Doors phone booth and its payphone, modelled in Blender for Astra.
# Astra, 2026-10-19: "this booth still looks like rudimentary shapes ... I want to use those skills we researched to fix
# the phone and the booth to look real and handcrafted, not computer generated."
#
# What makes a prop read as made rather than generated, and how this script does each one:
#   1. Real proportions from a real object: Sir Giles Gilbert Scott's K6 kiosk (1935): square cast-iron box, corner
#      posts with recessed faces, glazing in eight rows of narrow-wide-narrow panes, a lit TELEPHONE sign on all four
#      sides, a crown in each arched pediment, a shallow dome. Kept at the old booth's 1.3 m footprint so avatars fit.
#   2. No razor edges: every hard part gets a bevel and weighted normals, so edges catch the light like cast metal.
#   3. Baked light: ambient occlusion from the whole booth (posts shade the panes, the payphone shades its board).
#   4. Wear where real wear goes: a curvature (edge) mask chips the paint on edges and corners, grime collects in the
#      occluded corners, rain streaks run down vertical faces, the sun fades the tops, brass is polished where touched.
#   5. Real surface detail from scans: CC0 ambientCG sets (PaintedMetal002, Metal009, Smear002, Scratches001), used for
#      roughness breakup, fine scratches and fingerprints, box-projected so there are no seams.
#   6. A real payphone: die-cast housing, chrome keypad with numbered buttons, instruction card, coin slots, coin return,
#      hook switch, a proper handset with ear and mouth cups, and a coiled cord.
#
# Run (headless):
#   blender -b --factory-startup --python Tools/Blender/make_phone_booth.py -- --scans <unpacked ambientCG folder>
#       --out Review/booth-blender --textures Assets/LiminalDoors/Booth/Textures [--stage model|all] [--device CPU|GPU]
# Writes: <out>/phone-booth.json (meshes in Unity space, read by LiminalDoors/Booth/Import Blender model),
#         <textures>/booth-*.png and payphone-*.png, <out>/previews/*.png.
import bpy, bmesh, math, json, os, sys, random
from mathutils import Vector, Matrix

ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
def arg(name, default=None):
    return ARGV[ARGV.index(name) + 1] if name in ARGV else default

OUT = os.path.abspath(arg('--out', 'Review/booth-blender'))
TEXOUT = os.path.abspath(arg('--textures', os.path.join(OUT, 'textures')))
SCANS = os.path.abspath(arg('--scans', '.'))
STAGE = arg('--stage', 'all')
RES = int(arg('--res', '2048')); PRES = int(arg('--pres', '1024'))
SAMPLES = int(arg('--samples', '48'))
DEVICE = arg('--device', 'CPU')
os.makedirs(OUT, exist_ok=True); os.makedirs(TEXOUT, exist_ok=True); os.makedirs(os.path.join(OUT, 'previews'), exist_ok=True)
random.seed(1935)

def lin(c):   # sRGB 0..1 -> linear (shader colours are linear)
    return tuple((x / 12.92) if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c) + (1.0,)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# ---------------------------------------------------------------------------------------------------------------------
# Geometry helpers. Parts accumulate into one bmesh per (object, material, bevel); objects stay at identity, so bevel
# widths are true metres. Blender is Z up; the booth's front (door) faces +Y, which becomes Unity's +Z.
POOL = {}
def acc(group, matname, bevel=0.006, segs=2, smooth=True):
    k = (group, matname, round(bevel, 4), segs, smooth)
    if k not in POOL: POOL[k] = bmesh.new()
    return POOL[k]

def face_frame(n):
    """local (u, w, v) -> world, for a booth face whose outward normal is n. u = n x Z (det +1)."""
    N = Vector(n); U = N.cross(Vector((0, 0, 1)))
    return Matrix(((U.x, N.x, 0, 0), (U.y, N.y, 0, 0), (0, 0, 1, 0), (0, 0, 0, 1)))

def box(group, matname, x0, x1, y0, y1, z0, z1, bevel=0.006, segs=2, inset=None):
    bm = acc(group, matname, bevel, segs)
    vs = bmesh.ops.create_cube(bm, size=1.0)['verts']
    bmesh.ops.scale(bm, vec=(x1 - x0, y1 - y0, z1 - z0), verts=vs)
    bmesh.ops.translate(bm, vec=((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), verts=vs)
    if inset:   # (list of local normal directions, thickness, depth): recessed panels on those faces
        dirs, th, dp = inset
        bm.normal_update()
        fs = list({f for v in vs for f in v.link_faces if any(f.normal.dot(Vector(d)) > 0.9 for d in dirs)})
        bmesh.ops.inset_individual(bm, faces=fs, thickness=th, depth=-dp)
    return vs

def box_m(group, matname, ranges, M, **kw):
    """box in face-local coordinates (u0,u1, w0,w1, v0,v1), transformed by M; inset verts follow."""
    bm = acc(group, matname, kw.get('bevel', 0.006), kw.get('segs', 2))
    before = set(bm.verts)
    box(group, matname, *ranges, **kw)
    new = [v for v in bm.verts if v not in before]
    bmesh.ops.transform(bm, matrix=M, verts=new)

def add_bm(group, matname, build, M=None, bevel=0.0, segs=2, smooth=True):
    """build(bm) adds geometry; the new verts are moved by M."""
    bm = acc(group, matname, bevel, segs, smooth)
    before = set(bm.verts)
    build(bm)
    new = [v for v in bm.verts if v not in before]
    if M is not None: bmesh.ops.transform(bm, matrix=M, verts=new)
    return new

def tube(bm, pts, radius, sides=8, cap=True):
    n = len(pts); rad = radius if isinstance(radius, (list, tuple)) else [radius] * n
    T = [(pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized() for i in range(n)]
    ref = Vector((0, 0, 1)) if abs(T[0].z) < 0.9 else Vector((1, 0, 0))
    N = (ref - T[0] * ref.dot(T[0])).normalized(); rings = []
    for i in range(n):
        N = (N - T[i] * N.dot(T[i])).normalized(); B = T[i].cross(N)
        rings.append([bm.verts.new(pts[i] + (N * math.cos(2 * math.pi * s / sides) + B * math.sin(2 * math.pi * s / sides)) * rad[i]) for s in range(sides)])
    for i in range(n - 1):
        for s in range(sides):
            bm.faces.new((rings[i][s], rings[i][(s + 1) % sides], rings[i + 1][(s + 1) % sides], rings[i + 1][s]))
    if cap:
        bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])

def extrude_closed(bm, faces, vec):
    """extrude flat faces into a closed solid and point every face outward."""
    ext = bmesh.ops.extrude_face_region(bm, geom=list(faces))
    vs = [e for e in ext['geom'] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=vec, verts=vs)
    bmesh.ops.recalc_face_normals(bm, faces=list({f for v in vs for f in v.link_faces} | set(faces)))

def cylinder(bm, r1, r2, depth, M, segs=16):
    return bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r1, radius2=r2, depth=depth, matrix=M)['verts']

def sphere(bm, r, M, u=12, v=8):
    return bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r, matrix=M)['verts']

def text_into(group, matname, body, size, M_face, u0, w0, v0, spacing=1.0, res=2, bold=0.0):
    """flat lettering on a face (reads correctly from outside that face)."""
    cu = bpy.data.curves.new('txt', 'FONT'); cu.body = body; cu.size = size
    cu.align_x = 'CENTER'; cu.align_y = 'CENTER'; cu.space_character = spacing; cu.resolution_u = res; cu.offset = bold
    ob = bpy.data.objects.new('txt', cu); scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob); bpy.data.curves.remove(cu)
    T = Matrix(((-1, 0, 0, u0), (0, 0, 1, w0), (0, 1, 0, v0), (0, 0, 0, 1)))   # text x -> -u (viewer's right), normal -> +w
    me.transform(M_face @ T)
    bm = acc(group, matname, 0.0, 1, False)
    bm.from_mesh(me); bpy.data.meshes.remove(me)

# ---------------------------------------------------------------------------------------------------------------------
# The booth (K6 proportions at the old booth's footprint). Units: metres.
FLOOR = 0.08                     # top of the plinth = the floor you stand on
POST_C, POST = 0.59, 0.12        # corner post centres and size
IN = 0.53                        # inner edge of the posts = edge of each panel
W0, W1 = 0.575, 0.625            # panel frame depth (outer face 0.625; the posts' outer faces are at 0.65)
C_DOME, K_DOME, A_DOME = 2.74, 0.4758, 0.68
def zdome(x, y): return C_DOME - K_DOME * (x * x + y * y)
FACES = {'front': (0, 1, 0), 'back': (0, -1, 0), 'left': (-1, 0, 0), 'right': (1, 0, 0)}

def glazed_panel(group, M, glass_group=None):
    G = group
    box_m(G, 'paint', (-IN, IN, W0, W1, FLOOR, 0.32), M)                                        # bottom rail (kick panel)
    box_m(G, 'paint', (-0.44, 0.44, W1, W1 + 0.009, 0.13, 0.27), M, bevel=0.004, segs=1)               # fielded panel, outside
    box_m(G, 'paint', (-0.44, 0.44, W0 - 0.009, W0, 0.13, 0.27), M, bevel=0.004, segs=1)               # and inside
    box_m(G, 'paint', (-IN, -0.47, W0, W1, 0.32, 1.94), M)                                      # stiles
    box_m(G, 'paint', (0.47, IN, W0, W1, 0.32, 1.94), M)
    box_m(G, 'paint', (-IN, IN, W0, W1, 1.94, 2.02), M)                                         # top rail
    for uc in (-0.235, 0.235):                                                                  # narrow-wide-narrow
        box_m(G, 'paint', (uc - 0.012, uc + 0.012, 0.580, 0.620, 0.318, 1.942), M, bevel=0.004, segs=1)
    cell = (1.62 - 7 * 0.024) / 8                                                               # eight rows of panes
    for k in range(7):
        vc = 0.32 + (k + 1) * cell + k * 0.024 + 0.012
        box_m(G, 'paint', (-0.472, 0.472, 0.583, 0.617, vc - 0.012, vc + 0.012), M, bevel=0.004, segs=1)
    if glass_group:
        def pane(bm):
            vs = [bm.verts.new(Vector(p)) for p in ((-0.47, 0.6, 0.32), (0.47, 0.6, 0.32), (0.47, 0.6, 1.94), (-0.47, 0.6, 1.94))]
            bm.faces.new(vs)
        add_bm(glass_group, 'glass', pane, M=M, smooth=False)

def frieze_and_pediment(n):
    M = face_frame(n); G = 'Booth body'
    box_m(G, 'paint', (-IN, IN, W0, 0.615, 2.02, 2.20), M)                                      # sign plate
    for r in ((-0.45, 0.45, 2.035, 2.045), (-0.45, 0.45, 2.175, 2.185), (-0.45, -0.44, 2.045, 2.175), (0.44, 0.45, 2.045, 2.175)):
        box_m(G, 'paint', (r[0], r[1], 0.615, 0.628, r[2], r[3]), M, bevel=0.003, segs=1)       # sign frame
    box_m(G, 'sign', (-0.44, 0.44, 0.606, 0.621, 2.045, 2.175), M, bevel=0.002, segs=1)         # lit opal panel
    text_into(G, 'ink', 'TELEPHONE', 0.088, M, 0.0, 0.6222, 2.106, spacing=1.30, res=5, bold=0.0028)
    # pediment: flat tympanum under the dome's arched edge
    def tympanum(bm):
        segs = 24; front = []
        for i in range(segs + 1):
            u = A_DOME - 2 * A_DOME * i / segs
            front.append(bm.verts.new(Vector((u, 0.67, zdome(u, A_DOME)))))
        front.append(bm.verts.new(Vector((-A_DOME, 0.67, 2.26)))); front.append(bm.verts.new(Vector((A_DOME, 0.67, 2.26))))
        extrude_closed(bm, [bm.faces.new(front)], (0, -0.04, 0))
    add_bm(G, 'paint', tympanum, M=M, bevel=0.005, segs=1)
    # rolled eave moulding along the arch
    pts = [Vector((u, 0.676, zdome(u, A_DOME) + 0.004)) for u in [(-A_DOME - 0.012) + (2 * A_DOME + 0.024) * i / 20 for i in range(21)]]
    add_bm(G, 'paint', lambda bm: tube(bm, pts, 0.019, sides=8), M=M)
    # the crown, gilded (Astra's butter-yellow and gold): band with jewels, a raised velvet cap, three pearled arches,
    # fleurons and crosses on the band, the orb and cross on top
    C = 'gold'
    box_m(G, C, (-0.085, 0.085, 0.670, 0.686, 2.300, 2.330), M, bevel=0.004)                     # band
    for i, u in enumerate((-0.062, -0.031, 0.0, 0.031, 0.062)):                                   # jewels: lozenges and pearls
        if i % 2 == 0: box_m(G, C, (u - 0.0075, u + 0.0075, 0.686, 0.6895, 2.3085, 2.3215), M, bevel=0.0018, segs=1)
    def cap(bm):   # the velvet cap, in the booth's pink, swelling out behind the arches
        ring = [bm.verts.new(Vector((0.074 * math.cos(a), 0.6705, 2.330 + 0.080 * math.sin(a)))) for a in [math.pi * i / 10 for i in range(11)]]
        extrude_closed(bm, [bm.faces.new(ring)], (0, 0.0105, 0))
    add_bm(G, 'paint', cap, M=M, bevel=0.004, segs=1)
    def crown(bm):
        for u in (-0.031, 0.031): sphere(bm, 0.0052, Matrix.Translation((u, 0.6875, 2.315)), 6, 4)      # pearls on the band
        for s in (-1, 1):                                                                       # outer arches, pearled
            pts = []
            for i in range(8):
                t = i / 7; e = math.sin(t * math.pi / 2)
                pts.append(Vector((s * 0.080 * (1 - e ** 1.7), 0.683 + 0.002 * e, 2.330 + 0.090 * e ** 0.72)))
            tube(bm, pts, 0.0062, sides=6)
            for t in (0.38, 0.72):
                e = math.sin(t * math.pi / 2)
                sphere(bm, 0.0046, Matrix.Translation((s * 0.080 * (1 - e ** 1.7), 0.689, 2.330 + 0.090 * e ** 0.72)), 6, 4)
            sphere(bm, 0.0072, Matrix.Translation((s * 0.080, 0.686, 2.336)), 6, 4)                 # fleurons at the band ends
        tube(bm, [Vector((0, 0.684, 2.330)), Vector((0, 0.6865, 2.375)), Vector((0, 0.685, 2.418))], 0.0058, sides=6)   # front arch
        sphere(bm, 0.0132, Matrix.Translation((0, 0.682, 2.431)), 8, 6)                        # orb
    add_bm(G, C, crown, M=M)
    for u in (-0.043, 0.043):                                                                   # small crosses on the band
        box_m(G, C, (u - 0.0022, u + 0.0022, 0.680, 0.688, 2.330, 2.348), M, bevel=0.001, segs=1)
        box_m(G, C, (u - 0.0068, u + 0.0068, 0.680, 0.688, 2.3365, 2.3415), M, bevel=0.001, segs=1)
    box_m(G, C, (-0.0042, 0.0042, 0.677, 0.687, 2.441, 2.476), M, bevel=0.0015, segs=1)            # cross on the orb
    box_m(G, C, (-0.0125, 0.0125, 0.677, 0.687, 2.4545, 2.4625), M, bevel=0.0015, segs=1)

def build_booth():
    G = 'Booth body'
    box(G, 'paint', -0.70, 0.70, -0.70, 0.70, 0.0, 0.05, bevel=0.008)                          # plinth
    box(G, 'paint', -0.67, 0.67, -0.67, 0.67, 0.05, FLOOR, bevel=0.006)
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx, cy = sx * POST_C, sy * POST_C
            box(G, 'paint', cx - 0.075, cx + 0.075, cy - 0.075, cy + 0.075, FLOOR, 0.22, bevel=0.008)           # post base
            box(G, 'paint', cx - 0.06, cx + 0.06, cy - 0.06, cy + 0.06, 0.22, 2.14, bevel=0.010, segs=2,
                inset=([(sx, 0, 0), (0, sy, 0)], 0.026, 0.006))                                                  # recessed faces
            box(G, 'paint', cx - 0.0725, cx + 0.0725, cy - 0.0725, cy + 0.0725, 2.14, 2.18, bevel=0.006)        # capital
            box(G, 'paint', cx - 0.066, cx + 0.066, cy - 0.066, cy + 0.066, 2.18, 2.20, bevel=0.004)
            box(G, 'paint', sx * 0.655 - 0.038, sx * 0.655 + 0.038, sy * 0.655 - 0.038, sy * 0.655 + 0.038, 2.26, 2.335, bevel=0.012)  # corner ears
    box(G, 'paint', -0.70, 0.70, -0.70, 0.70, 2.20, 2.23, bevel=0.008)                          # cornice
    box(G, 'paint', -0.685, 0.685, -0.685, 0.685, 2.23, 2.26, bevel=0.008)
    for name in ('back', 'left', 'right'):
        glazed_panel(G, face_frame(FACES[name]), glass_group='Booth glass')
    for n in FACES.values(): frieze_and_pediment(n)
    def dome(bm):   # shallow dome: z = c - k (x^2 + y^2), a little thickness under it
        n, a = 16, A_DOME + 0.02; grid = []
        for j in range(n + 1):
            row = []
            for i in range(n + 1):
                x, y = -a + 2 * a * i / n, -a + 2 * a * j / n
                row.append(bm.verts.new(Vector((x, y, zdome(x, y) + 0.022))))
            grid.append(row)
        faces = [bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i])) for j in range(n) for i in range(n)]
        extrude_closed(bm, faces, (0, 0, -0.035))
    add_bm(G, 'paint', dome, bevel=0.006, segs=2)
    # inside: the board the payphone hangs on, and a directory shelf
    box(G, 'board', -0.25, 0.27, -0.572, -0.548, 0.70, 1.86, bevel=0.006)
    box(G, 'board', -0.32, 0.34, -0.575, -0.37, 0.78, 0.805, bevel=0.006)
    for x in (-0.24, 0.26):
        def bracket(bm, x=x):
            vs = [bm.verts.new(Vector(p)) for p in ((x - 0.008, -0.574, 0.78), (x - 0.008, -0.40, 0.78), (x - 0.008, -0.574, 0.68))]
            extrude_closed(bm, [bm.faces.new(vs)], (0.016, 0, 0))
        add_bm(G, 'board', bracket, bevel=0.003, segs=1)

DOOR_HINGE = Vector((-IN, W1, 0)); DOOR_OPEN = 80.0
def build_door():
    # built closed, in the door's own space: hinge at the origin, door along +x, outer face at y = 0
    M = Matrix.Translation((IN, -W1, 0))
    glazed_panel('Booth door', M, glass_group='Booth door glass')
    G = 'Booth door'
    def handle(bm):   # long brass pull on the free edge, outside
        tube(bm, [Vector((1.03, 0.036, z)) for z in (0.93, 0.97, 1.05, 1.15, 1.23, 1.27)], 0.0115, sides=12)
        for z in (0.965, 1.235):
            tube(bm, [Vector((1.03, 0.0, z)), Vector((1.03, 0.02, z)), Vector((1.03, 0.036, z))], 0.0075, sides=10)
            cylinder(bm, 0.016, 0.016, 0.006, Matrix.Translation((1.03, 0.003, z)) @ Matrix.Rotation(math.pi / 2, 4, 'X'), 14)   # rose
    add_bm(G, 'brass', handle, bevel=0.0015, segs=1)
    def hinges(bm):
        for z0 in (0.24, 0.98, 1.74):
            cylinder(bm, 0.014, 0.014, 0.12, Matrix.Translation((0.0, 0.0, z0 + 0.06)), 14)
            sphere(bm, 0.0075, Matrix.Translation((0.0, 0.0, z0 + 0.125)), 10, 6)
    add_bm(G, 'brass', hinges, bevel=0.002, segs=1)

# ---------------------------------------------------------------------------------------------------------------------
# The payphone on the back board. It faces +Y (into the booth); the viewer's left is +X.
PX = -0.02; PB = -0.542; PF = -0.442
def build_payphone():
    G = 'Payphone'
    box(G, 'steel', PX - 0.135, PX + 0.165, -0.548, PB, 1.07, 1.69, bevel=0.004)                # back plate
    box(G, 'steel', PX - 0.105, PX + 0.105, PB, PF, 1.12, 1.64, bevel=0.014, segs=3)             # die-cast housing
    box(G, 'steel', PX - 0.11, PX + 0.11, PB - 0.003, PF + 0.004, 1.64, 1.655, bevel=0.006)     # top cap
    for i, x in enumerate((PX + 0.06, PX + 0.02, PX - 0.02)):                                     # coin slots
        box(G, 'chrome', x - 0.015, x + 0.015, -0.470, -0.456, 1.655, 1.661, bevel=0.0025, segs=2)
        box(G, 'black', x - 0.009 - 0.002 * i, x + 0.009 + 0.002 * i, -0.4645, -0.4615, 1.661, 1.6618, bevel=0.0006, segs=1)
    # instruction card behind a chrome bezel
    for r in ((-0.08, 0.08, 1.47, 1.477), (-0.08, 0.08, 1.593, 1.60), (-0.08, -0.073, 1.477, 1.593), (0.073, 0.08, 1.477, 1.593)):
        box(G, 'chrome', PX + r[0], PX + r[1], PF, PF + 0.006, r[2], r[3], bevel=0.002, segs=1)
    box(G, 'card', PX - 0.073, PX + 0.073, PF - 0.001, PF + 0.0015, 1.477, 1.593, bevel=0.0005, segs=1)
    Mf = face_frame((0, 1, 0))
    for line, v, s in (('PAY TELEPHONE', 1.575, 0.0125), ('LIFT RECEIVER', 1.551, 0.0098), ('DEPOSIT COINS', 1.533, 0.0098),
                       ('LISTEN FOR DIAL TONE', 1.515, 0.0098), ('DIAL NUMBER', 1.497, 0.0098)):
        text_into(G, 'cardink', line, s, Mf, PX, PF + 0.0025, v, spacing=1.08, res=2)
    # keypad: chrome plate and twelve pillowy chrome buttons with dark digits
    box(G, 'chrome', PX - 0.056, PX + 0.056, PF, PF + 0.005, 1.27, 1.432, bevel=0.004)
    labels = (('1', '2', '3'), ('4', '5', '6'), ('7', '8', '9'), ('*', '0', '#'))
    for r in range(4):
        for c in range(3):
            x = PX + (1 - c) * 0.032; z = 1.408 - r * 0.036
            box(G, 'chrome', x - 0.0128, x + 0.0128, PF + 0.005, PF + 0.0118, z - 0.0128, z + 0.0128, bevel=0.0045, segs=2)
            text_into(G, 'digits', labels[r][c], 0.0135, Mf, x, PF + 0.0128, z, res=2)
    # coin return
    box(G, 'chrome', PX - 0.036, PX + 0.036, PF, PF + 0.010, 1.15, 1.216, bevel=0.006, segs=2)
    box(G, 'black', PX - 0.026, PX + 0.026, PF + 0.0098, PF + 0.0106, 1.158, 1.184, bevel=0.001, segs=1)
    box(G, 'chrome', PX - 0.028, PX + 0.028, PF + 0.010, PF + 0.014, 1.186, 1.208, bevel=0.003, segs=1)
    # hook switch on the left side (+X)
    box(G, 'chrome', PX + 0.105, PX + 0.122, -0.502, -0.468, 1.374, 1.432, bevel=0.004)
    box(G, 'chrome', PX + 0.122, PX + 0.136, -0.500, -0.470, 1.376, 1.389, bevel=0.003)
    add_bm(G, 'chrome', lambda bm: cylinder(bm, 0.008, 0.008, 0.016, Matrix.Translation((PX + 0.035, -0.480, 1.113)), 12), bevel=0.0015, segs=1)

HX = PX + 0.141; HY = -0.484
def build_handset():
    G = 'Payphone handset'
    def body(bm):
        pts = [Vector((HX + 0.016 * math.sin(math.pi * t) + 0.004, HY, 1.238 + 0.168 * t)) for t in [i / 14 for i in range(15)]]
        tube(bm, pts, [0.0118 + 0.0015 * math.sin(math.pi * i / 14) for i in range(15)], sides=12)
    add_bm(G, 'black', body)
    for zc, r1, r2 in ((1.418, 0.029, 0.027), (1.226, 0.031, 0.025)):     # ear cup, mouth cup (cups face the housing, -X)
        def cup(bm, zc=zc, r1=r1, r2=r2):
            M = Matrix.Translation((HX - 0.006, HY, zc)) @ Matrix.Rotation(-math.pi / 2, 4, 'Y')
            vs = cylinder(bm, r2, r1, 0.032, M, 24)   # wide end toward the housing
            bm.normal_update()
            fs = [f for f in {f for v in vs for f in v.link_faces} if f.normal.x < -0.9]
            bmesh.ops.inset_individual(bm, faces=fs, thickness=0.006, depth=-0.002)
            bmesh.ops.inset_individual(bm, faces=fs, thickness=0.008, depth=0.0012)
        add_bm(G, 'black', cup, bevel=0.006, segs=3)
    add_bm(G, 'black', lambda bm: cylinder(bm, 0.0075, 0.006, 0.016, Matrix.Translation((HX - 0.004, HY, 1.196)), 12), bevel=0.0015, segs=1)

def build_cord():
    # a coiled cord hanging from the handset down in a loop and up into the bottom of the housing
    P0, P1, P2, P3 = Vector((HX - 0.004, HY, 1.188)), Vector((HX - 0.002, HY + 0.004, 0.99)), Vector((PX + 0.03, HY + 0.012, 0.95)), Vector((PX + 0.035, -0.480, 1.105))
    def bez(t): return (1 - t) ** 3 * P0 + 3 * (1 - t) ** 2 * t * P1 + 3 * (1 - t) * t * t * P2 + t ** 3 * P3
    path = [bez(i / 400) for i in range(401)]
    L = sum((path[i + 1] - path[i]).length for i in range(400))
    pitch, R, per = 0.0088, 0.0085, 6
    turns = int(L / pitch); n = turns * per
    cum = [0.0]
    for i in range(400): cum.append(cum[-1] + (path[i + 1] - path[i]).length)
    def at(s):
        for i in range(400):
            if cum[i + 1] >= s:
                f = (s - cum[i]) / max(cum[i + 1] - cum[i], 1e-9); return path[i].lerp(path[i + 1], f), (path[i + 1] - path[i]).normalized()
        return path[-1], (path[-1] - path[-2]).normalized()
    pts = []; N = None
    for k in range(n + 1):
        s = 0.012 + (L - 0.024) * k / n
        p, t = at(s)
        if N is None: N = (Vector((1, 0, 0)) - t * t.x).normalized()
        N = (N - t * N.dot(t)).normalized(); B = t.cross(N)
        a = 2 * math.pi * k / per
        pts.append(p + (N * math.cos(a) + B * math.sin(a)) * R)
    pts = [P0] + pts + [P3]
    add_bm('Payphone cord', 'black', lambda bm: tube(bm, pts, 0.0027, sides=5))

# ---------------------------------------------------------------------------------------------------------------------
def finalize():
    """one object per (group, material, bevel): bevel + weighted normals + triangulate, applied; then join per group."""
    objs = {}
    for (group, matname, bevel, segs, smooth), bm in POOL.items():
        me = bpy.data.meshes.new(group); bm.to_mesh(me); bm.free()
        ob = bpy.data.objects.new(group, me); scene.collection.objects.link(ob); ob['key'] = '%s b%s s%s' % (matname, bevel, segs)
        m = bpy.data.materials.get(matname) or bpy.data.materials.new(matname); m.use_nodes = True
        me.materials.append(m)
        for p in me.polygons: p.use_smooth = smooth
        if bevel > 0:
            b = ob.modifiers.new('bevel', 'BEVEL'); b.width = bevel; b.segments = segs
            b.limit_method = 'ANGLE'; b.angle_limit = math.radians(35); b.use_clamp_overlap = True
        if smooth:
            w = ob.modifiers.new('wn', 'WEIGHTED_NORMAL'); w.keep_sharp = True; w.weight = 50; w.mode = 'FACE_AREA'
        t = ob.modifiers.new('tri', 'TRIANGULATE'); t.keep_custom_normals = True; t.min_vertices = 4
        objs.setdefault(group, []).append(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    final = {}
    for group, parts in objs.items():
        for ob in parts:
            me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
            ob.modifiers.clear(); old = ob.data; ob.data = me; bpy.data.meshes.remove(old)
            if '--parts' in ARGV: print('[part] %-18s %-8s %6d tris  key=%s' % (group, me.materials[0].name if me.materials else '', len(me.polygons), ob.get('key','')))
        if len(parts) > 1:
            with bpy.context.temp_override(active_object=parts[0], object=parts[0], selected_objects=parts, selected_editable_objects=parts):
                bpy.ops.object.join()
        parts[0].name = group; parts[0].data.name = group
        final[group] = parts[0]
    for n in ('Booth door', 'Booth door glass'):   # the door (and its glass) stand open on the hinge
        final[n].location = DOOR_HINGE; final[n].rotation_euler = (0, 0, math.radians(DOOR_OPEN))
    return final

ATLASES = {'booth': ['Booth body', 'Booth door'], 'payphone': ['Payphone', 'Payphone handset', 'Payphone cord']}
def join_atlas(final, names):
    """one mesh per texture atlas, so all its parts unwrap and pack into one UV space; a face attribute remembers each
    face's part and the part's own transform, to split them again on export (the door keeps its hinge)."""
    bpy.context.view_layer.update()   # matrix_world is stale until the view layer updates (the door's hinge transform)
    parts = [final[n] for n in names]; info = [(n, final[n].matrix_world.copy()) for n in names]
    for i, o in enumerate(parts):
        a = o.data.attributes.new('part', 'INT', 'FACE'); a.data.foreach_set('value', [i] * len(o.data.polygons))
    if len(parts) > 1:
        with bpy.context.temp_override(active_object=parts[0], object=parts[0], selected_objects=parts, selected_editable_objects=parts):
            bpy.ops.object.join()
    for n in names[1:]: final.pop(n)
    parts[0].name = 'atlas ' + names[0]
    return parts[0], info

def planar_uvs(ob):
    """glass: UVs in metres across each pane, for the smudge texture."""
    me = ob.data; uv = me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        n = poly.normal
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            uv.data[li].uv = ((co.x if abs(n.x) < 0.5 else co.y), co.z)

def unwrap(objs, margin):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(52), island_margin=0.0, area_weight=0.0, correct_aspect=True, scale_to_bounds=False)
    bpy.ops.uv.pack_islands(rotate=True, margin_method='FRACTION', margin=margin, shape_method='CONCAVE')
    bpy.ops.object.mode_set(mode='OBJECT')

# ---------------------------------------------------------------------------------------------------------------------
# Materials: a node "composer" per material that builds albedo / roughness / metallic / emission / height from the baked
# AO and edge masks, box-projected scans and noise. Each channel is baked out through an Emission shader.
def scan(name, kind):
    p = os.path.join(SCANS, name + '_2K-JPG', name + '_2K-JPG_' + kind + '.jpg')
    if not os.path.exists(p): raise SystemExit('missing scan ' + p)
    img = bpy.data.images.load(p, check_existing=True)
    if kind != 'Color': img.colorspace_settings.name = 'Non-Color'
    return img

class NB:
    def __init__(self, m):
        self.m = m; self.nt = m.node_tree; self.n = self.nt.nodes; self.l = self.nt.links
        for nd in list(self.n):
            if nd.type != 'OUTPUT_MATERIAL': self.n.remove(nd)
        self.out = next((nd for nd in self.n if nd.type == 'OUTPUT_MATERIAL'), None) or self.n.new('ShaderNodeOutputMaterial')
        self.tc = self.n.new('ShaderNodeTexCoord'); self.geo = self.n.new('ShaderNodeNewGeometry')
    def node(self, t, **props):
        nd = self.n.new(t)
        for k, v in props.items(): setattr(nd, k, v)
        return nd
    def link(self, a, b): self.l.new(a, b)
    def val(self, x):
        if isinstance(x, (int, float)):
            nd = self.node('ShaderNodeValue'); nd.outputs[0].default_value = x; return nd.outputs[0]
        return x
    def math(self, op, a, b=0.0, clamp=False):
        nd = self.node('ShaderNodeMath', operation=op, use_clamp=clamp)
        for i, x in enumerate((a, b)):
            if isinstance(x, (int, float)): nd.inputs[i].default_value = x
            else: self.link(x, nd.inputs[i])
        return nd.outputs[0]
    def remap(self, x, a, b, c, d, clamp=True, smooth=False):
        nd = self.node('ShaderNodeMapRange', clamp=clamp)
        if smooth: nd.interpolation_type = 'SMOOTHSTEP'
        self.link(x, nd.inputs[0])
        for i, v in zip((1, 2, 3, 4), (a, b, c, d)): nd.inputs[i].default_value = v
        return nd.outputs[0]
    def mix(self, a, b, fac, color=True):
        nd = self.node('ShaderNodeMix', data_type='RGBA' if color else 'FLOAT')
        ia, ib = (6, 7) if color else (2, 3)
        for sock, x in ((nd.inputs[ia], a), (nd.inputs[ib], b), (nd.inputs[0], fac)):
            if isinstance(x, (int, float, tuple)): sock.default_value = x
            else: self.link(x, sock)
        return nd.outputs[2] if color else nd.outputs[0]
    def mul_color(self, c, f):
        nd = self.node('ShaderNodeMix', data_type='RGBA', blend_type='MULTIPLY'); nd.inputs[0].default_value = 1.0
        if isinstance(c, tuple): nd.inputs[6].default_value = c
        else: self.link(c, nd.inputs[6])
        if isinstance(f, (int, float)): nd.inputs[7].default_value = (f, f, f, 1)
        else: self.link(f, nd.inputs[7])
        return nd.outputs[2]
    def noise(self, scale, detail=3.0, stretch=None, rough=0.55):
        nd = self.node('ShaderNodeTexNoise'); nd.inputs['Scale'].default_value = scale
        nd.inputs['Detail'].default_value = detail; nd.inputs['Roughness'].default_value = rough
        v = self.tc.outputs['Object']
        if stretch:
            mp = self.node('ShaderNodeMapping'); mp.inputs['Scale'].default_value = stretch
            self.link(v, mp.inputs['Vector']); v = mp.outputs[0]
        self.link(v, nd.inputs['Vector']); return nd.outputs['Fac']
    def boxtex(self, img, scale, rot=0.0):
        nd = self.node('ShaderNodeTexImage', projection='BOX', projection_blend=0.3); nd.image = img
        mp = self.node('ShaderNodeMapping'); mp.inputs['Scale'].default_value = (scale, scale, scale)
        mp.inputs['Rotation'].default_value = (rot, rot * 0.7, rot * 1.3)
        self.link(self.tc.outputs['Object'], mp.inputs['Vector']); self.link(mp.outputs[0], nd.inputs['Vector'])
        return nd.outputs['Color']
    def uvtex(self, img):
        nd = self.node('ShaderNodeTexImage'); nd.image = img; nd.interpolation = 'Linear'
        self.link(self.tc.outputs['UV'], nd.inputs['Vector'])
        return nd
    def bw(self, c):
        nd = self.node('ShaderNodeRGBToBW'); self.link(c, nd.inputs[0]); return nd.outputs[0]
    def nz(self):
        sp = self.node('ShaderNodeSeparateXYZ'); self.link(self.geo.outputs['Normal'], sp.inputs[0]); return sp.outputs['Z']

def edge_shader(m, radius):
    nb = NB(m)
    bev = nb.node('ShaderNodeBevel', samples=16); bev.inputs['Radius'].default_value = radius
    dot = nb.node('ShaderNodeVectorMath', operation='DOT_PRODUCT')
    nb.link(bev.outputs['Normal'], dot.inputs[0]); nb.link(nb.geo.outputs['Normal'], dot.inputs[1])
    e = nb.remap(dot.outputs['Value'], 0.992, 0.78, 0.0, 1.0, smooth=True)
    em = nb.node('ShaderNodeEmission'); nb.link(e, em.inputs['Color']); nb.link(em.outputs[0], nb.out.inputs['Surface'])

PALETTE = {
    'paint': dict(base=(1.0, 0.25, 0.59), alt=(1.0, 0.40, 0.55), fade=(1.0, 0.52, 0.73), grime=(0.40, 0.15, 0.27),
                  edge=(1.0, 0.47, 0.70), primer=(0.62, 0.13, 0.22), bare=(0.22, 0.21, 0.22), rough=0.2, chip=0.85),
    'board': dict(base=(0.58, 0.20, 0.38), alt=(0.62, 0.26, 0.40), fade=(0.66, 0.30, 0.46), grime=(0.28, 0.10, 0.18),
                  edge=(0.72, 0.36, 0.52), primer=(0.45, 0.10, 0.18), bare=(0.22, 0.21, 0.22), rough=0.42, chip=0.35),
}

def compose(m, ao_img, edge_img):
    nb = NB(m); k = m.name
    ao = nb.math('POWER', nb.uvtex(ao_img).outputs['Color'], 1.0)
    edge = nb.math('POWER', nb.uvtex(edge_img).outputs['Color'], 1.0)
    cavity = nb.remap(ao, 0.35, 1.0, 1.0, 0.0)                                  # 1 in occluded corners
    scr = nb.bw(nb.boxtex(scan('Scratches001', 'Opacity'), 1.1, 0.4))           # fine scratches
    smear = nb.bw(nb.boxtex(scan('Smear002', 'Roughness'), 1.6, 1.1))            # fingerprints and smudges
    nz = nb.nz()
    out = {}
    if k in PALETTE:
        P = PALETTE[k]
        var = nb.remap(nb.noise(2.6, 3.0), 0.3, 0.7, 0.93, 1.05)
        col = nb.mix(lin(P['base']), lin(P['alt']), nb.remap(nb.noise(0.9, 2.0), 0.35, 0.7, 0.0, 0.35))
        col = nb.mul_color(col, var)
        lum = nb.remap(nb.bw(nb.boxtex(scan('PaintedMetal002', 'Color'), 1.4)), 0.15, 0.75, 0.95, 1.03)
        col = nb.mul_color(col, lum)
        col = nb.mix(col, lin(P['fade']), nb.math('MULTIPLY', nb.remap(nz, 0.5, 1.0, 0.0, 1.0, smooth=True), 0.35))
        grime = nb.math('MULTIPLY', nb.math('POWER', cavity, 1.3), nb.remap(nb.noise(8.0, 4.0), 0.3, 0.7, 0.5, 1.0), clamp=True)
        streak = nb.remap(nb.noise(1.0, 3.0, stretch=(26.0, 26.0, 1.3)), 0.56, 0.74, 0.0, 1.0)
        streak = nb.math('MULTIPLY', streak, nb.remap(nz, -0.3, 0.0, 0.0, 1.0))   # vertical and overhung faces only
        streak = nb.math('MULTIPLY', streak, nb.remap(nz, 0.0, 0.4, 1.0, 0.0))
        dirt = nb.math('MAXIMUM', nb.math('MULTIPLY', grime, 0.75), nb.math('MULTIPLY', streak, 0.3))
        col = nb.mix(col, lin(P['grime']), dirt)
        col = nb.mix(col, lin(P['edge']), nb.math('MULTIPLY', edge, 0.35))
        # chips: only on strong edges, and only where a high-contrast noise says so (a few small flakes, not a pattern)
        flake = nb.remap(nb.noise(38.0, 6.0, rough=0.7), 0.56, 0.68, 0.0, 1.0)
        chip = nb.math('MULTIPLY', nb.math('MULTIPLY', nb.remap(edge, 0.45, 0.9, 0.0, 1.0), flake), P['chip'])
        bare = nb.math('MULTIPLY', chip, nb.remap(nb.noise(70.0, 4.0), 0.6, 0.72, 0.0, 1.0))
        col = nb.mix(col, lin(P['primer']), chip); col = nb.mix(col, lin(P['bare']), bare)
        col = nb.mix(col, lin((1.0, 0.72, 0.84)), nb.math('MULTIPLY', scr, 0.22))
        rs = nb.remap(nb.bw(nb.boxtex(scan('PaintedMetal002', 'Roughness'), 1.4)), 0.0, 1.0, -0.08, 0.14)
        rough = nb.math('ADD', P['rough'], rs)
        for x, w in ((dirt, 0.4), (chip, 0.3), (scr, 0.14), (smear, 0.08)): rough = nb.math('ADD', rough, nb.math('MULTIPLY', x, w))
        out['albedo'] = col; out['rough'] = nb.math('MINIMUM', nb.math('MAXIMUM', rough, 0.1), 0.9)
        out['metal'] = nb.math('MULTIPLY', bare, 0.6)
        out['height'] = nb.math('SUBTRACT', nb.math('MULTIPLY', nb.bw(nb.boxtex(scan('PaintedMetal002', 'Displacement'), 1.4)), 0.6), nb.math('MULTIPLY', scr, 0.25))
    elif k in ('brass', 'gold'):
        base, tarn = ((0.96, 0.74, 0.40), (0.40, 0.29, 0.15)) if k == 'brass' else ((1.0, 0.80, 0.40), (0.52, 0.34, 0.14))
        tar = nb.math('MULTIPLY', nb.math('POWER', cavity, 1.1), nb.remap(nb.noise(12.0, 4.0), 0.3, 0.7, 0.6, 1.0), clamp=True)
        col = nb.mix(lin(base), lin(tarn), tar)
        col = nb.mix(col, lin((1.0, 0.88, 0.60)), nb.math('MULTIPLY', edge, 0.45))
        out['albedo'] = nb.mul_color(col, nb.remap(nb.noise(6.0, 3.0), 0.3, 0.7, 0.92, 1.04))
        rough = nb.math('ADD', 0.24 if k == 'brass' else 0.3, nb.math('MULTIPLY', tar, 0.35))
        rough = nb.math('SUBTRACT', rough, nb.math('MULTIPLY', edge, 0.12))
        out['rough'] = nb.math('MAXIMUM', nb.math('ADD', rough, nb.math('MULTIPLY', scr, 0.16)), 0.08)
        out['metal'] = nb.val(1.0)
        out['height'] = nb.math('MULTIPLY', scr, -0.3)
    elif k == 'sign':
        out['albedo'] = nb.mix(lin((1.0, 0.92, 0.70)), lin((0.9, 0.80, 0.62)), cavity)
        out['rough'] = nb.math('ADD', 0.28, nb.math('MULTIPLY', smear, 0.2)); out['metal'] = nb.val(0.0)
        out['emit'] = nb.mul_color(nb.mix(lin((1.0, 0.78, 0.32)), lin((1.0, 0.86, 0.48)), nb.remap(nb.noise(3.0, 2.0), 0.3, 0.7, 0.0, 0.4)),
                                   nb.remap(ao, 0.4, 1.0, 0.45, 1.0))
        out['height'] = nb.val(0.0)
    elif k in ('ink', 'digits', 'cardink'):
        c = {'ink': (0.32, 0.05, 0.18), 'digits': (0.05, 0.05, 0.06), 'cardink': (0.13, 0.12, 0.17)}[k]
        out['albedo'] = nb.mix(lin(c), lin((0.0, 0.0, 0.0)), nb.math('MULTIPLY', cavity, 0.3))
        out['rough'] = nb.val(0.45); out['metal'] = nb.val(0.0); out['height'] = nb.val(0.0)
    elif k == 'steel':
        lum = nb.remap(nb.bw(nb.boxtex(scan('Metal009', 'Color'), 2.2)), 0.2, 0.8, 0.9, 1.04)
        col = nb.mul_color(lin((0.80, 0.80, 0.82)), lum)
        col = nb.mix(col, lin((0.36, 0.34, 0.36)), nb.math('MULTIPLY', cavity, 0.55))
        rs = nb.remap(nb.bw(nb.boxtex(scan('Metal009', 'Roughness'), 2.2)), 0.0, 1.0, 0.22, 0.44)
        rough = nb.math('ADD', rs, nb.math('MULTIPLY', smear, 0.22))
        rough = nb.math('ADD', rough, nb.math('MULTIPLY', cavity, 0.25))
        out['albedo'] = col; out['rough'] = nb.math('MINIMUM', nb.math('ADD', rough, nb.math('MULTIPLY', scr, 0.1)), 0.85)
        out['metal'] = nb.val(1.0); out['height'] = nb.math('MULTIPLY', nb.bw(nb.boxtex(scan('Metal009', 'Displacement'), 2.2)), 0.35)
    elif k == 'chrome':
        out['albedo'] = nb.mix(lin((0.96, 0.96, 0.97)), lin((0.45, 0.43, 0.45)), nb.math('MULTIPLY', cavity, 0.5))
        rough = nb.math('ADD', 0.06, nb.math('MULTIPLY', scr, 0.18))
        rough = nb.math('ADD', rough, nb.math('MULTIPLY', smear, 0.2))
        out['rough'] = nb.math('ADD', rough, nb.math('MULTIPLY', cavity, 0.25)); out['metal'] = nb.val(1.0)
        out['height'] = nb.math('MULTIPLY', scr, -0.25)
    elif k == 'black':
        out['albedo'] = nb.mix(lin((0.035, 0.032, 0.036)), lin((0.10, 0.09, 0.10)), nb.math('MULTIPLY', edge, 0.5))
        rough = nb.math('ADD', 0.26, nb.math('MULTIPLY', scr, 0.2))
        rough = nb.math('ADD', rough, nb.math('MULTIPLY', smear, 0.28))
        out['rough'] = nb.math('SUBTRACT', rough, nb.math('MULTIPLY', edge, 0.1)); out['metal'] = nb.val(0.0)
        out['height'] = nb.math('MULTIPLY', scr, -0.2)
    elif k == 'card':
        out['albedo'] = nb.mix(lin((0.94, 0.91, 0.83)), lin((0.84, 0.76, 0.58)), nb.math('MULTIPLY', cavity, 0.8))
        out['rough'] = nb.val(0.62); out['metal'] = nb.val(0.0); out['height'] = nb.val(0.0)
    if 'emit' not in out: out['emit'] = nb.val(0.0)
    m['_nb'] = True
    return nb, out

def show(nb, sock, normal=None):
    for nd in [nd for nd in nb.n if nd.label in ('SHOW', 'SHOWBSDF', 'SHOWBUMP')]: nb.n.remove(nd)
    if normal is None:
        em = nb.node('ShaderNodeEmission', label='SHOW'); nb.link(sock, em.inputs['Color'])
        nb.link(em.outputs[0], nb.out.inputs['Surface'])
    else:
        bump = nb.node('ShaderNodeBump', label='SHOWBUMP'); bump.inputs['Strength'].default_value = 0.35; bump.inputs['Distance'].default_value = 0.0015
        nb.link(normal, bump.inputs['Height'])
        bs = nb.node('ShaderNodeBsdfPrincipled', label='SHOWBSDF'); nb.link(bump.outputs[0], bs.inputs['Normal'])
        nb.link(bs.outputs[0], nb.out.inputs['Surface'])

def target(mats, img):
    for m in mats:
        nt = m.node_tree
        nd = nt.nodes.get('BAKE_TARGET') or nt.nodes.new('ShaderNodeTexImage'); nd.name = 'BAKE_TARGET'
        nd.image = img
        for o in nt.nodes: o.select = False
        nd.select = True; nt.nodes.active = nd

def bake(objs, mats, img, kind, samples):
    target(mats, img)
    scene.cycles.samples = samples
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.bake(type=kind, margin=16, margin_type='EXTEND', use_clear=True, normal_space='TANGENT')

def new_img(name, res, color):
    img = bpy.data.images.get(name) or bpy.data.images.new(name, res, res, alpha=False, float_buffer=False)
    img.colorspace_settings.name = 'sRGB' if color else 'Non-Color'
    return img

def save(img, path):
    img.filepath_raw = path; img.file_format = 'PNG'; img.save()

def bake_atlas(prefix, objs, res, glass):
    mats = []
    for o in objs:
        for m in o.data.materials:
            if m not in mats: mats.append(m)
    for g in glass: g.hide_render = True
    ao = new_img(prefix + '-ao', res, False); bake(objs, mats, ao, 'AO', SAMPLES)
    edge = new_img(prefix + '-edge', res, False)
    for m in mats: edge_shader(m, 0.004 if prefix == 'booth' else 0.002)
    bake(objs, mats, edge, 'EMIT', 16)
    comp = {m.name: compose(m, ao, edge) for m in mats}
    imgs = {}
    for ch, color in (('albedo', True), ('rough', False), ('metal', False), ('emit', True)):
        if ch == 'emit' and prefix != 'booth': continue
        img = new_img(prefix + '-' + ch, res, color)
        for m in mats: show(comp[m.name][0], comp[m.name][1][ch])
        target(mats, img); bake(objs, mats, img, 'EMIT', 4); imgs[ch] = img
    nrm = new_img(prefix + '-normal', res, False)
    for m in mats: show(comp[m.name][0], None, normal=comp[m.name][1]['height'])
    bake(objs, mats, nrm, 'NORMAL', 4); imgs['normal'] = nrm
    for g in glass: g.hide_render = False
    # write Unity's textures: albedo, metallic (R) + smoothness (A), normal, occlusion, emission
    save(imgs['albedo'], os.path.join(TEXOUT, prefix + '-albedo.png'))
    save(nrm, os.path.join(TEXOUT, prefix + '-normal.png'))
    save(ao, os.path.join(TEXOUT, prefix + '-occlusion.png'))
    if 'emit' in imgs: save(imgs['emit'], os.path.join(TEXOUT, prefix + '-emission.png'))
    ms = bpy.data.images.new(prefix + '-ms', res, res, alpha=True, float_buffer=False); ms.colorspace_settings.name = 'Non-Color'
    import numpy as np
    met = np.empty(res * res * 4, np.float32); imgs['metal'].pixels.foreach_get(met)
    rou = np.empty(res * res * 4, np.float32); imgs['rough'].pixels.foreach_get(rou)
    px = met.copy(); px[3::4] = 1.0 - rou[0::4]
    ms.pixels.foreach_set(px); ms.alpha_mode = 'STRAIGHT'; save(ms, os.path.join(TEXOUT, prefix + '-metallic-smoothness.png'))
    return imgs, ao, mats

# ---------------------------------------------------------------------------------------------------------------------
def preview_materials(final, booth_imgs, phone_imgs):
    """the look Unity will get: a plain PBR material per object from the baked textures."""
    def pbr(name, imgs, ao):
        m = bpy.data.materials.new('preview ' + name); m.use_nodes = True; nt = m.node_tree; n = nt.nodes
        bs = n.get('Principled BSDF')
        def tex(img, color):
            t = n.new('ShaderNodeTexImage'); t.image = img; return t
        a = tex(imgs['albedo'], True); nt.links.new(a.outputs['Color'], bs.inputs['Base Color'])
        r = tex(imgs['rough'], False); nt.links.new(r.outputs['Color'], bs.inputs['Roughness'])
        mt = tex(imgs['metal'], False); nt.links.new(mt.outputs['Color'], bs.inputs['Metallic'])
        nm = n.new('ShaderNodeNormalMap'); t = tex(imgs['normal'], False); nt.links.new(t.outputs['Color'], nm.inputs['Color']); nt.links.new(nm.outputs[0], bs.inputs['Normal'])
        if 'emit' in imgs:
            e = tex(imgs['emit'], True); nt.links.new(e.outputs['Color'], bs.inputs['Emission Color']); bs.inputs['Emission Strength'].default_value = 3.0
        return m
    pb = pbr('booth', booth_imgs, None) if booth_imgs else None
    pp = pbr('phone', phone_imgs, None) if phone_imgs else None
    glass = bpy.data.materials.new('preview glass'); glass.use_nodes = True
    gb = glass.node_tree.nodes.get('Principled BSDF'); gb.inputs['Base Color'].default_value = (1.0, 0.75, 0.86, 1)
    gb.inputs['Roughness'].default_value = 0.05; gb.inputs['Transmission Weight'].default_value = 1.0; gb.inputs['Alpha'].default_value = 0.35
    if hasattr(glass, 'surface_render_method'): glass.surface_render_method = 'BLENDED'
    for name, ob in final.items():
        if 'glass' in name: mm = glass
        elif name.startswith('Booth'): mm = pb
        else: mm = pp
        if mm is None: continue
        ob.data.materials.clear(); ob.data.materials.append(mm)

def simple_materials():
    cols = {'paint': (1.0, 0.2, 0.56), 'board': (0.58, 0.2, 0.38), 'brass': (0.96, 0.74, 0.40), 'gold': (1.0, 0.8, 0.4), 'sign': (1.0, 0.95, 0.8),
            'ink': (0.32, 0.05, 0.18), 'steel': (0.8, 0.8, 0.82), 'chrome': (0.95, 0.95, 0.96), 'black': (0.04, 0.04, 0.04), 'card': (0.94, 0.91, 0.83),
            'cardink': (0.13, 0.12, 0.17), 'digits': (0.05, 0.05, 0.06), 'glass': (1.0, 0.75, 0.86)}
    for m in bpy.data.materials:
        if m.name not in cols: continue
        bs = m.node_tree.nodes.get('Principled BSDF') or m.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
        out = next(nd for nd in m.node_tree.nodes if nd.type == 'OUTPUT_MATERIAL')
        m.node_tree.links.new(bs.outputs[0], out.inputs['Surface'])
        bs.inputs['Base Color'].default_value = lin(cols[m.name])
        bs.inputs['Metallic'].default_value = 1.0 if m.name in ('brass', 'gold', 'steel', 'chrome') else 0.0
        bs.inputs['Roughness'].default_value = 0.25
        if m.name == 'sign': bs.inputs['Emission Color'].default_value = lin((1.0, 0.86, 0.46)); bs.inputs['Emission Strength'].default_value = 3.0
        if m.name == 'glass':
            bs.inputs['Alpha'].default_value = 0.25
            if hasattr(m, 'surface_render_method'): m.surface_render_method = 'BLENDED'

def render_previews(tag):
    for eng in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE'):
        try: scene.render.engine = eng; break
        except TypeError: continue
    scene.render.resolution_x, scene.render.resolution_y = 1400, 1050
    scene.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('sky'); scene.world = w; w.use_nodes = True
    bg = w.node_tree.nodes.get('Background')
    bg.inputs['Color'].default_value = lin((1.0, 0.72, 0.66)); bg.inputs['Strength'].default_value = 0.9
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); scene.collection.objects.link(sun)
    sun.data.energy = 3.2; sun.data.color = (1.0, 0.80, 0.66); sun.rotation_euler = (math.radians(58), 0, math.radians(215))
    fill = bpy.data.objects.new('fill', bpy.data.lights.new('fill', 'SUN')); scene.collection.objects.link(fill)
    fill.data.energy = 0.9; fill.data.color = (1.0, 0.7, 0.85); fill.rotation_euler = (math.radians(60), 0, math.radians(30))
    floor = bpy.data.meshes.new('floor'); bmf = bmesh.new(); bmesh.ops.create_grid(bmf, x_segments=1, y_segments=1, size=6); bmf.to_mesh(floor)
    fo = bpy.data.objects.new('floor', floor); scene.collection.objects.link(fo)
    fm = bpy.data.materials.new('floor'); fm.use_nodes = True; fm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = lin((1.0, 0.78, 0.85)); floor.materials.append(fm)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); scene.collection.objects.link(cam); scene.camera = cam
    shots = {'three-quarter': ((2.7, 3.5, 1.55), (0.0, 0.1, 1.35), 40),
             'payphone': ((0.30, 0.85, 1.48), (-0.01, -0.48, 1.38), 45),
             'crown': ((0.45, 1.55, 2.25), (0.0, 0.66, 2.33), 40),
             'back': ((-2.4, -3.2, 1.9), (0.0, 0.0, 1.35), 42)}
    for name, (loc, look, lens) in shots.items():
        cam.location = loc; d = Vector(look) - Vector(loc); cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler(); cam.data.lens = lens * 0.9
        scene.render.filepath = os.path.join(OUT, 'previews', tag + '-' + name + '.png'); bpy.ops.render.render(write_still=True)

def export_json(entries):
    """entries: (name, object, part index or None, the part's own world matrix). Writes Unity-space meshes in each part's
    own space (the door's around its hinge)."""
    data = []
    for name, ob, part, Mw in entries:
        me = ob.data
        me.calc_loop_triangles()
        try: me.calc_tangents(uvmap=me.uv_layers.active.name)
        except Exception as e: print('tangents', name, e)
        uv = me.uv_layers.active.data; cn = me.corner_normals
        pa = me.attributes['part'].data if part is not None else None
        Minv = Mw.inverted() @ ob.matrix_world; R = Minv.to_3x3().normalized()
        idx = {}; V, Nn, T, UV, tris = [], [], [], [], []
        for tri in me.loop_triangles:
            if pa is not None and pa[tri.polygon_index].value != part: continue
            ids = []
            for li in tri.loops:
                lp = me.loops[li]; co = Minv @ me.vertices[lp.vertex_index].co; n = R @ cn[li].vector; t = R @ lp.tangent; s = lp.bitangent_sign; u = uv[li].uv
                key = (round(co.x, 5), round(co.y, 5), round(co.z, 5), round(n.x, 3), round(n.y, 3), round(n.z, 3), round(u.x, 5), round(u.y, 5))
                if key not in idx:
                    idx[key] = len(V) // 3
                    V += [co.x, co.z, co.y]; Nn += [n.x, n.z, n.y]; T += [t.x, t.z, t.y, -s]; UV += [u.x, u.y]
                ids.append(idx[key])
            tris += [ids[0], ids[2], ids[1]]                     # Blender -> Unity flips handedness: reverse winding
        mat = 'Booth glass (pink)' if 'glass' in name else ('Booth' if name.startswith('Booth') else 'Payphone')
        loc, rz = Mw.to_translation(), Mw.to_euler().z
        parent = 'Booth door' if name == 'Booth door glass' else ''
        if parent: loc, rz = Vector((0, 0, 0)), 0.0
        data.append(dict(name=name, material=mat, parent=parent, position=[loc.x, loc.z, loc.y], rotationY=-math.degrees(rz),
                         vertices=[round(x, 5) for x in V], normals=[round(x, 4) for x in Nn], tangents=[round(x, 4) for x in T],
                         uv=[round(x, 5) for x in UV], triangles=tris))
        print('[booth] %-18s %6d tris %6d verts' % (name, len(tris) // 3, len(V) // 3))
    with open(os.path.join(OUT, 'phone-booth.json'), 'w') as f:
        json.dump(dict(source='Tools/Blender/make_phone_booth.py', meshes=data), f)

# ---------------------------------------------------------------------------------------------------------------------
build_booth(); build_door(); build_payphone(); build_handset(); build_cord()
final = finalize()
for n in ('Booth glass', 'Booth door glass'): planar_uvs(final[n])
tot = 0
for name, ob in final.items():
    me = ob.data; tris = sum(len(p.vertices) - 2 for p in me.polygons); tot += tris
    bb = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
    print('[booth] %-18s %6d tris  x %.3f..%.3f  y %.3f..%.3f  z %.3f..%.3f' % (name, tris, min(v.x for v in bb), max(v.x for v in bb), min(v.y for v in bb), max(v.y for v in bb), min(v.z for v in bb), max(v.z for v in bb)))
print('[booth] total %d triangles' % tot)

if STAGE == 'model':
    simple_materials(); render_previews('model')
else:
    scene.render.engine = 'CYCLES'
    if DEVICE == 'GPU':
        prefs = bpy.context.preferences.addons['cycles'].preferences
        for t in ('HIP', 'OPTIX', 'CUDA', 'ONEAPI', 'METAL'):
            try:
                prefs.compute_device_type = t; prefs.get_devices()
                if any(d.type == t for d in prefs.devices):
                    for d in prefs.devices: d.use = d.type == t
                    scene.cycles.device = 'GPU'; print('[booth] baking on', t); break
            except TypeError: continue
    if scene.world is None: scene.world = bpy.data.worlds.new('bake')
    scene.world.light_settings.distance = 0.22
    glass = [final['Booth glass'], final['Booth door glass']]
    atlas = {k: join_atlas(final, names) for k, names in ATLASES.items()}
    for k, (ob, info) in atlas.items(): unwrap([ob], 0.004 if k == 'booth' else 0.006)
    if STAGE == 'uvtest': raise SystemExit(0)
    bi, _, _ = bake_atlas('booth', [atlas['booth'][0]], RES, glass)
    pi, _, _ = bake_atlas('payphone', [atlas['payphone'][0]], PRES, glass)
    entries = []
    for k, (ob, info) in atlas.items():
        for i, (n, Mw) in enumerate(info): entries.append((n, ob, i, Mw))
    for g in glass: entries.append((g.name, g, None, g.matrix_world.copy()))
    export_json(entries)
    preview_materials(final, bi, pi); render_previews('baked')
print('[booth] done')
