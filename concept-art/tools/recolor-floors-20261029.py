"""Recolor seven Liminal Doors floors (Astra, 2026-10-29). Keeps each door and scene; only colors move.
Usage: python recolor-floors-20261029.py <floors dir> <out dir>  (from concept-art: tools/recolor-floors-20261029.py floors floors)"""
import sys, glob, os
import numpy as np, cv2

SRC, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
DATE = '20261029'


def load(n):
    f = glob.glob(os.path.join(SRC, f'{n}-*-20261015.png'))[0]
    bgr = cv2.imread(f).astype(np.float32) / 255
    return os.path.basename(f).replace('-20261015.png', ''), bgr


def hsv(bgr):
    return cv2.cvtColor(bgr.astype(np.float32), cv2.COLOR_BGR2HSV)  # H 0..360, S, V 0..1


def bgr_of(h, s, v):
    return cv2.cvtColor(np.dstack([h % 360, np.clip(s, 0, 1), np.clip(v, 0, 1)]).astype(np.float32), cv2.COLOR_HSV2BGR)


def hue_near(h, centre, width):
    """soft 0..1 weight for hues within width degrees of centre"""
    d = np.abs((h - centre + 180) % 360 - 180)
    return np.clip(1 - (d - width) / 12, 0, 1)


def soft(mask, r=3):
    return cv2.GaussianBlur(mask.astype(np.float32), (0, 0), r)


def keep_label(out, orig):
    """the floor number in the bottom-left corner stays as it was"""
    h = orig.shape[0]
    box = orig[h - 52:, :70]
    white = (box.min(-1) > 0.62) & (box.max(-1) - box.min(-1) < 0.2)
    m = soft(cv2.dilate(white.astype(np.uint8), np.ones((3, 3), np.uint8)), 0.8)[..., None]
    out[h - 52:, :70] = out[h - 52:, :70] * (1 - m) + box * m
    return out


def save(name, bgr, orig):
    bgr = keep_label(np.clip(bgr, 0, 1), orig)
    cv2.imwrite(os.path.join(OUT, f'{name}-{DATE}.png'), (bgr * 255 + 0.5).astype(np.uint8))


def ramp(t, stops):
    """gradient map: t 0..1 through [(pos, (r, g, b))] stops; returns BGR"""
    pos = np.array([p for p, _ in stops]); cols = np.array([c for _, c in stops], np.float32) / 255
    out = np.stack([np.interp(t, pos, cols[:, k]) for k in range(3)], -1)
    return out[..., ::-1]


# 03 pool after closing: the aqua and purple stay; the red, orange and peach go violet and magenta so it all matches
def f03():
    name, b = load('03'); H, S, V = cv2.split(hsv(b))
    warm = np.clip(1 - np.abs(((H + 40) % 360) - 60) / 60, 0, 1) ** 0.5  # hues -40..100 (pink, red, orange, yellow)
    warm *= np.clip(S * 4, 0, 1)
    w = ((H + 40) % 360)  # 0 = pink, 40 = red, 80 = orange
    nh = 312 - w * 0.55  # pink -> 312 magenta, red -> 290, orange -> 268 violet
    H2 = H + warm * (((nh - H + 180) % 360) - 180)
    S2 = S * (1 + 0.25 * warm)
    save(name, bgr_of(H2, S2, V), b)


# 04 laundrette: the pink door (and its glow on the floor) becomes bright ocean blue
def f04():
    name, b = load('04'); H, S, V = cv2.split(hsv(b))
    pink = hue_near(H, 345, 28) * np.clip((S - 0.25) * 4, 0, 1)
    box = np.zeros_like(H); box[55:330, 292:400] = 1; box[320:490, 280:430] = 1
    m = (pink * soft(box, 5))[..., None]
    blue = bgr_of(np.full_like(H, 198), np.clip(S * 1.1, 0, 1), np.clip(V * 1.04, 0, 1))
    save(name, b * (1 - m) + blue * m, b)


# 05 apricot underpass: the aqua door stays; the orange concrete round it goes bright pink
def f05():
    name, b = load('05'); H, S, V = cv2.split(hsv(b))
    warm = hue_near(H, 25, 30) * np.clip(S * 5, 0, 1)
    H2 = H + warm * (((328 - H + 180) % 360) - 180)
    S2 = S + warm * (np.clip(S * 1.25 + 0.08, 0, 1) - S)
    V2 = V + warm * (np.clip(V * 1.12 + 0.03, 0, 1) - V)
    # the violet haze down the tunnel joins the pink
    vio = hue_near(H, 270, 25) * np.clip(S * 4, 0, 1)
    H2 = H2 + vio * (((315 - H2 + 180) % 360) - 180)
    save(name, bgr_of(H2, S2, V2), b)


def plume_layer(shape, vases, seed=7):
    """fluffy pampas plumes fanning out of each vase mouth; returns (bgr, alpha) at image size"""
    h, w = shape[:2]; k = 4
    acc = np.zeros((h * k, w * k), np.float32); col = np.zeros((h * k, w * k, 3), np.float32)
    rng = np.random.default_rng(seed)
    for (cx, cy, spread, length) in vases:
        n = 7
        for i in range(n):
            a = np.deg2rad(-90 + (i - (n - 1) / 2) * spread / (n - 1) + rng.normal(0, 5))
            L = length * rng.uniform(0.8, 1.05) * (1 - 0.18 * abs(i - (n - 1) / 2) / n)
            bend = rng.normal(0, 0.15) + (i - (n - 1) / 2) * 0.05
            # stem
            pts = [(cx + np.cos(a + bend * t * t) * L * t, cy + np.sin(a + bend * t * t) * L * t) for t in np.linspace(0, 0.35, 8)]
            for p, q in zip(pts, pts[1:]):
                cv2.line(acc, (int(p[0] * k), int(p[1] * k)), (int(q[0] * k), int(q[1] * k)), 1.0, k)
                cv2.line(col, (int(p[0] * k), int(p[1] * k)), (int(q[0] * k), int(q[1] * k)), (0.45, 0.6, 0.75), k)
            # the plume: soft puffs, widest two thirds up, many small ones on top for fluff
            for _ in range(2600):
                t = rng.uniform(0.3, 1.0)
                ang = a + bend * t * t
                px = cx + np.cos(ang) * L * t; py = cy + np.sin(ang) * L * t
                u = (t - 0.3) / 0.7
                width = L * 0.16 * np.sin(np.pi * min(1, u * 0.95 + 0.05)) ** 0.7 + 0.8
                off = rng.normal(0, width * 0.45)
                px += -np.sin(ang) * off; py += np.cos(ang) * off
                r = max(1, int(rng.uniform(0.6, 2.2) * k))
                edge = abs(off) / (width + 1e-3)
                light = 1.12 + 0.15 * u - 0.35 * edge + 0.25 * (np.cos(ang) * 0 + (off > 0) * 0.3)
                c = np.array([0.78, 0.9, 1.0]) * light  # warm cream (BGR)
                c = c * (1 - 0.35 * edge) + np.array([0.95, 0.78, 0.86]) * 0.35 * edge  # lavender rim
                cv2.circle(acc, (int(px * k), int(py * k)), r, 0.35, -1)
                cv2.circle(col, (int(px * k), int(py * k)), r, tuple(float(v) * 0.35 for v in c), -1)
    a = np.clip(acc, 0, 1); c = col / np.maximum(acc, 1e-4)[..., None]
    a = cv2.GaussianBlur(a, (0, 0), 2.0); c = cv2.GaussianBlur(c, (0, 0), 3.0)
    c = cv2.resize(c, (w, h), interpolation=cv2.INTER_AREA); a = cv2.resize(a, (w, h), interpolation=cv2.INTER_AREA)
    return np.clip(c, 0, 1), np.clip(a * 1.1, 0, 0.97)


# 07 velvet waiting room: the door stays; the velvet goes a soft deep indigo-violet, the yellow's own complement,
# and the bare branches in the vases are swapped for fluffy pampas plumes
def f07():
    name, b = load('07'); H, S, V = cv2.split(hsv(b))
    gold = hue_near(H, 50, 22) * np.clip((S - 0.25) * 4, 0, 1) * np.clip((V - 0.35) * 4, 0, 1)
    vel = 1 - gold
    H2 = H + vel * (((262 - H + 180) % 360) - 180)
    S2 = S + vel * (S * 0.85 - S)
    V2 = V + vel * (np.clip(V * 1.06, 0, 1) - V)
    out = bgr_of(H2, S2, V2)
    # paint out the branches (above each vase) with the curtain around them
    mask = np.zeros(H.shape, np.uint8)
    for (x0, y0, x1, y1) in [(4, 118, 110, 212), (145, 143, 218, 214), (448, 136, 510, 220)]:
        reg = b[y0:y1, x0:x1]
        hv = cv2.cvtColor(reg, cv2.COLOR_BGR2HSV)
        lum = reg.mean(-1)
        bg = cv2.medianBlur((lum * 255).astype(np.uint8), 15).astype(np.float32) / 255
        twig = (np.abs(lum - bg) > 0.035) | (hv[..., 1] < 0.45)
        mask[y0:y1, x0:x1] = twig.astype(np.uint8) * 255
    mask = cv2.dilate(mask, np.ones((5, 5), np.uint8))
    out8 = (np.clip(out, 0, 1) * 255).astype(np.uint8)
    out = cv2.inpaint(out8, mask, 6, cv2.INPAINT_TELEA).astype(np.float32) / 255
    pc, pa = plume_layer(out.shape, [(50, 212, 110, 92), (176, 214, 100, 70), (486, 220, 100, 80)])
    # warm lamplight from the right side of each plume
    out = out * (1 - pa[..., None]) + pc * pa[..., None]
    save(name, out, b)


# 15 empty aquarium: all red, orange and yellow, from the dark to the light (red in the shadows, yellow only in the light,
# so nothing goes olive)
def f15():
    name, b = load('15'); H, S, V = cv2.split(hsv(b))
    lum = (b[..., 2] * 0.3 + b[..., 1] * 0.45 + b[..., 0] * 0.25)
    lum = cv2.GaussianBlur(lum, (0, 0), 0.6)
    t = np.clip(lum ** 0.8 * 1.25, 0, 1)
    hue = 356 + 62 * t ** 1.4  # crimson -> red -> orange -> yellow
    sat = 1 - 0.55 * t ** 5
    val = np.clip(0.05 + t ** 0.85 * 1.0, 0, 1)
    save(name, bgr_of(hue, sat, val), b)


# 16 blackwater footbridge: blue, navy and aqua, with the pink door; hardly any purple left
def f16():
    name, b = load('16'); H, S, V = cv2.split(hsv(b))
    pink = hue_near(H, 325, 22) * np.clip((S - 0.35) * 4, 0, 1) * np.clip((V - 0.45) * 4, 0, 1)
    rest = 1 - pink
    bright = np.clip((V - 0.55) * 3, 0, 1) * np.clip((S - 0.3) * 3, 0, 1)
    target = 228 - bright * 42  # navy blue in the dark, aqua in the light bars
    H2 = H + rest * (((target - H + 180) % 360) - 180)
    H2 = H2 + pink * (((330 - H2 + 180) % 360) - 180)
    S2 = S + rest * (np.clip(S * 1.2 + 0.05, 0, 1) - S)
    V2 = V + rest * (V * (1.08 + 0.25 * bright) - V)
    save(name, bgr_of(H2, S2, V2), b)


# 18 snow in the lobby: less detail (no snowflakes, marble veins or sparkle) and new colors:
# a mint-aqua room, a hot pink desk, the gold door kept warm
def f18():
    name, b = load('18')
    sm = cv2.medianBlur((b * 255).astype(np.uint8), 5)
    sm = cv2.edgePreservingFilter(sm, flags=1, sigma_s=25, sigma_r=0.18).astype(np.float32) / 255
    H, S, V = cv2.split(hsv(sm))
    doorbox = np.zeros_like(H); doorbox[100:282, 300:378] = 1
    shape = np.zeros_like(H)
    cv2.fillPoly(shape, [np.array([(140, 276), (398, 276), (399, 325), (380, 334), (270, 341), (160, 334), (139, 325)], np.int32)], 1.0)
    desk = soft(shape, 1.2)[..., None]
    door = soft(hue_near(H, 42, 22) * np.clip((S - 0.2) * 5, 0, 1) * doorbox, 1.2)[..., None] * (1 - desk)
    room = bgr_of(np.full_like(H, 172), np.clip(S * 1.5 + 0.18, 0, 0.7), V * 0.94)
    pink = bgr_of(np.full_like(H, 330), np.full_like(H, 0.72), np.clip(V * 1.05, 0, 1))
    gold = bgr_of(np.full_like(H, 40), np.clip(S * 1.2 + 0.1, 0, 1), V)
    out = room * (1 - desk - door) + pink * desk + gold * door
    save(name, out, b)


for f in [f03, f04, f05, f07, f15, f16, f18]:
    f(); print('done', f.__name__)
