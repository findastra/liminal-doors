"""Colorways of the flooded pool corridor Astra loves (2026-10-29): the same picture turned round the color wheel, with
more contrast and saturation. Usage: python ref-colorways-20261029.py <picture> <out dir>"""
import sys, os
import numpy as np, cv2

src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
b = cv2.imread(src).astype(np.float32) / 255
H, S, V = cv2.split(cv2.cvtColor(b, cv2.COLOR_BGR2HSV))
cv2.imwrite(os.path.join(out, 'neon-pool-01-flooded-corridor-20261029.png'), (b * 255 + 0.5).astype(np.uint8))
# each colorway gives the water, the walls and the pink light their own hues, so they contrast instead of the whole
# picture turning one color. The water is found by its waterline (drawn for this 1448 x 1086 picture), the pink by its hue
h0, w0 = H.shape
water = np.zeros((h0, w0), np.float32)
poly = np.array([(0, 720), (590, 582), (890, 580), (1448, 735), (1448, 1086), (0, 1086)], np.float32) * [w0 / 1448, h0 / 1086]
cv2.fillPoly(water, [poly.astype(np.int32)], 1.0)
water = cv2.GaussianBlur(water, (0, 0), 5)
pink = np.clip(1 - (np.abs((H - 325 + 180) % 360 - 180) - 25) / 20, 0, 1) * np.clip(S * 4, 0, 1)
pink = cv2.GaussianBlur(pink.astype(np.float32), (0, 0), 6)
WAYS = {'acid': (95, 300, 55), 'fire-ice': (185, 12, 40), 'toxic': (130, 278, 78), 'candy': (330, 45, 185), 'ultra-red': (355, 335, 180)}
w = np.stack([water * (1 - pink), (1 - water) * (1 - pink), pink]) + 1e-6
for name, tgt in WAYS.items():
    t = np.deg2rad(np.array(tgt, np.float32))[:, None, None]
    h = np.rad2deg(np.arctan2((w * np.sin(t)).sum(0), (w * np.cos(t)).sum(0))) % 360
    s = np.clip(S * 1.2 + 0.05, 0, 1)
    v = np.clip(0.5 + (V - 0.5) * 1.18, 0, 1)  # more contrast
    o = cv2.cvtColor(np.dstack([h, s, v]).astype(np.float32), cv2.COLOR_HSV2BGR)
    cv2.imwrite(os.path.join(out, f'neon-pool-01-flooded-corridor-{name}-20261029.png'), (np.clip(o, 0, 1) * 255 + 0.5).astype(np.uint8))
