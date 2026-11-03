# -*- coding: utf-8 -*-
"""Refine wallpaper patch + cat sprite."""
import os, random
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ASSETS = os.path.join(ROOT, "assets")
SRC = os.path.join(ROOT, "Granny.jpg")

im = Image.open(SRC).convert("RGB")
cand = im.crop((620, 880, 790, 1050)).resize((256, 256), Image.LANCZOS)
cand = cand.filter(ImageFilter.GaussianBlur(0.3))
cand.save(os.path.join(ASSETS, "wallpaper.png"))
cand.save(os.path.join(ROOT, "tools", "_wp_preview.png"))

def draw_cat(angry=False, seed=7):
    S = 4
    W, H = 210, 245
    img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    fur = (48, 46, 55) if not angry else (56, 45, 47)
    fur_lo = (33, 31, 38)
    fur_hi = (74, 71, 84)
    pink = (205, 128, 138)
    eye_col = (150, 212, 96) if not angry else (228, 168, 62)
    glow = (120, 210, 100, 90) if not angry else (240, 150, 60, 110)

    def E(cx, cy, rx, ry, col):
        d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], fill=col)

    def P(pts, col):
        d.polygon([(x * S, y * S) for x, y in pts], fill=col)

    def L(xy, col, w):
        d.line([(x * S, y * S) for x, y in xy], fill=col, width=max(1, int(w * S)), joint="curve")

    # ---- tail (left, curling)
    tail = [(70, 208), (44, 205), (26, 186), (22, 160), (34, 146), (50, 150), (54, 164)]
    L(tail, fur_lo, 17)
    L(tail, fur, 12)
    L(tail, fur_hi, 4)

    # ---- body
    E(112, 176, 62, 60, fur_lo)
    E(112, 173, 57, 56, fur)
    E(112, 186, 44, 42, fur_lo)
    E(108, 160, 34, 36, fur_hi)      # chest
    E(80, 204, 28, 22, fur)
    E(148, 204, 28, 22, fur)
    # front paws
    E(92, 226, 17, 10, fur_lo)
    E(136, 226, 17, 10, fur_lo)

    # ---- head
    hx, hy = 112, 92
    # ears
    P([(hx - 48, hy - 6), (hx - 54, hy - 58), (hx - 6, hy - 34)], fur)
    P([(hx + 48, hy - 6), (hx + 54, hy - 58), (hx + 6, hy - 34)], fur)
    P([(hx - 43, hy - 12), (hx - 46, hy - 48), (hx - 16, hy - 30)], pink)
    P([(hx + 43, hy - 12), (hx + 46, hy - 48), (hx + 16, hy - 30)], pink)
    E(hx, hy, 52, 44, fur_lo)
    E(hx, hy + 2, 48, 40, fur)
    E(hx + 2, hy + 10, 40, 30, fur_hi)

    # ---- eyes
    eo = 21 if not angry else 25
    ey = hy - 2 if not angry else hy + 4
    g = Image.new("RGBA", img.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(g)
    for s in (-1, 1):
        gd.ellipse([(hx + s * eo - 20) * S, (ey - 15) * S, (hx + s * eo + 20) * S, (ey + 15) * S], fill=glow)
    g = g.filter(ImageFilter.GaussianBlur(9 * S))
    img.alpha_composite(g)
    for s in (-1, 1):
        E(hx + s * eo, ey, 17, 19 if not angry else 12, (14, 13, 16))
        E(hx + s * eo, ey, 14, 16 if not angry else 9, eye_col)
        d.ellipse([(hx + s * eo - 3) * S, (ey - 13) * S, (hx + s * eo + 3) * S, (ey + 13) * S], fill=(8, 8, 10))
        E(hx + s * eo - 5, ey - 7, 3.6, 3.6, (252, 252, 252))
        E(hx + s * eo + 4, ey + 7, 1.8, 1.8, (230, 240, 255))

    # ---- nose / mouth
    P([(hx - 7, hy + 20), (hx + 7, hy + 20), (hx, hy + 28)], pink)
    if angry:
        P([(hx - 15, hy + 30), (hx + 15, hy + 30), (hx, hy + 50)], (26, 12, 16))
        P([(hx - 7, hy + 32), (hx - 2, hy + 32), (hx - 4, hy + 43)], (240, 240, 236))
        P([(hx + 7, hy + 32), (hx + 2, hy + 32), (hx + 4, hy + 43)], (240, 240, 236))
        P([(hx - 12, hy + 52), (hx + 12, hy + 52), (hx, hy + 60)], (188, 96, 108))
    else:
        L([(hx, hy + 28), (hx, hy + 32)], (20, 18, 22), 1.6)
        L([(hx, hy + 32), (hx - 9, hy + 38)], (20, 18, 22), 1.5)
        L([(hx, hy + 32), (hx + 9, hy + 38)], (20, 18, 22), 1.5)

    # ---- whiskers
    for s in (-1, 1):
        for dy in (-1, 5, 11):
            L([(hx + s * 26, hy + 22 + dy), (hx + s * 88, hy + 8 + dy * 2.3)], (188, 190, 202, 210), 1.2)

    # ---- fur strokes
    rnd = random.Random(seed)
    for _ in range(420):
        x = rnd.uniform(45, 180)
        y = rnd.uniform(20, 232)
        inbody = ((x - 112) ** 2 / 60 ** 2 + (y - 175) ** 2 / 58 ** 2) < 1
        inhead = ((x - hx) ** 2 / 50 ** 2 + (y - hy) ** 2 / 43 ** 2) < 1
        if inbody or inhead:
            L([(x, y), (x + rnd.uniform(-2.5, 2.5), y + rnd.uniform(2.5, 7))],
              (fur_hi if rnd.random() < 0.5 else fur_lo) + (150,), 1.0)

    img = img.resize((W, H), Image.LANCZOS)
    return img

draw_cat(False).save(os.path.join(ASSETS, "marusya.png"))
draw_cat(True).save(os.path.join(ASSETS, "marusya_angry.png"))
print("[assets] cat + wallpaper refreshed")
