# -*- coding: utf-8 -*-
"""Prepare sprites/textures for the Granny parody game."""
import os, math, random
from PIL import Image, ImageDraw, ImageFilter, ImageChops, ImageEnhance

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ASSETS = os.path.join(ROOT, "assets")
os.makedirs(ASSETS, exist_ok=True)
SRC = os.path.join(ROOT, "Granny.jpg")

def log(*a):
    print("[assets]", *a)

# ---------------------------------------------------------------- granny cutout
def cutout():
    im = Image.open(SRC).convert("RGB")
    px = im.load()
    w, h = im.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    op = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            mx = max(r, g, b)
            mn = min(r, g, b)
            sat = mx - mn
            # soft key for the white studio background
            if mx >= 246 and sat <= 6:
                a = 0
            elif mx >= 226 and sat <= 14:
                a = int(max(0, min(255, (246.0 - mx) / 20.0 * 255.0)))
            else:
                a = 255
            if a:
                op[x, y] = (r, g, b, a)
    bbox = out.getbbox()
    out = out.crop(bbox)
    log("cutout bbox", bbox, "->", out.size)
    # light edge cleanup: pull a tiny bit of the white fringe in
    a_ch = out.split()[3].filter(ImageFilter.GaussianBlur(0.6))
    out.putalpha(a_ch)
    out.save(os.path.join(ASSETS, "granny.png"))
    return out

# ------------------------------------------------------------- granny close-up
def face(gr):
    w, h = gr.size
    # head occupies the top ~22% of the cutout
    crop = gr.crop((int(w * 0.22), 0, int(w * 0.80), int(h * 0.255)))
    crop = crop.resize((crop.width * 3, crop.height * 3), Image.LANCZOS)
    crop = crop.filter(ImageFilter.UnsharpMask(radius=3, percent=70, threshold=2))
    crop.save(os.path.join(ASSETS, "granny_face.png"))
    log("face", crop.size)
    return crop

# ------------------------------------------------------------------- wallpaper
def wallpaper(gr):
    # tile the floral fabric of granny's robe as the house wallpaper
    im = Image.open(SRC).convert("RGB")
    w, h = im.size
    # pick a clean patch of the skirt
    box = (int(w * 0.33), int(h * 0.55), int(w * 0.33) + 320, int(h * 0.55) + 320)
    patch = im.crop(box)
    patch = patch.resize((256, 256), Image.LANCZOS)
    patch = patch.filter(ImageFilter.GaussianBlur(0.4))
    patch.save(os.path.join(ASSETS, "wallpaper.png"))
    log("wallpaper", patch.size)

# ------------------------------------------------------------------------- cat
def draw_cat(angry=False):
    S = 4  # supersample
    W, H = 220 * S, 240 * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    fur = (46, 44, 52) if not angry else (52, 42, 44)
    fur2 = (68, 64, 76)
    fur3 = (32, 30, 36)
    pink = (196, 122, 132)
    eye = (150, 214, 96) if not angry else (222, 156, 64)
    glow = (110, 200, 90, 70) if not angry else (230, 140, 60, 90)

    def E(cx, cy, rx, ry, col):
        d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], fill=col)

    # tail curling to the left of the body
    d.line([(72 * S, 214 * S), (40 * S, 196 * S), (26 * S, 160 * S), (44 * S, 130 * S)],
           fill=fur3, width=int(15 * S), joint="curve")
    d.line([(72 * S, 214 * S), (40 * S, 196 * S), (26 * S, 160 * S), (44 * S, 130 * S)],
           fill=fur, width=int(11 * S), joint="curve")

    # body
    E(122, 168, 58, 62, fur)
    E(122, 176, 50, 50, fur2)
    E(122, 168, 58, 62, fur)
    # chest fluff
    E(122, 150, 30, 34, fur2)
    # haunches
    E(92, 196, 34, 30, fur)
    E(152, 196, 34, 30, fur)

    # head
    hx, hy = 122, 96
    d.polygon([(hx - 44, hy - 16), (hx - 58, hy - 74), (hx - 8, hy - 46)], fill=fur)
    d.polygon([(hx + 44, hy - 16), (hx + 58, hy - 74), (hx + 8, hy - 46)], fill=fur)
    d.polygon([(hx - 40, hy - 20), (hx - 50, hy - 62), (hx - 20, hy - 44)], fill=pink)
    d.polygon([(hx + 40, hy - 20), (hx + 50, hy - 62), (hx + 20, hy - 44)], fill=pink)
    E(hx, hy, 50, 42, fur)
    E(hx, hy + 6, 44, 36, fur2)

    # eyes
    eo = 22 if not angry else 26
    ey = hy - 4 if not angry else hy + 2
    for s in (-1, 1):
        g = Image.new("RGBA", img.size, (0, 0, 0, 0))
        gd = ImageDraw.Draw(g)
        gd.ellipse([(hx + s * eo - 16) * S, (ey - 12) * S, (hx + s * eo + 16) * S, (ey + 12) * S], fill=glow)
        g = g.filter(ImageFilter.GaussianBlur(10 * S))
        img.alpha_composite(g)
        E(hx + s * eo, ey, 15, 18 if not angry else 11, (18, 16, 20))
        E(hx + s * eo, ey, 12, 15 if not angry else 8, eye)
        d.ellipse([(hx + s * eo - 3) * S, (ey - 11) * S, (hx + s * eo + 3) * S, (ey + 11) * S], fill=(10, 10, 12))
        E(hx + s * eo - 4, ey - 6, 3.4, 3.4, (250, 250, 250))

    # nose + mouth
    d.polygon([(hx - 6, hy + 18), (hx + 6, hy + 18), (hx, hy + 25)], fill=pink)
    if angry:
        # open hissing mouth
        d.polygon([(hx - 13, hy + 28), (hx + 13, hy + 28), (hx, hy + 46)], fill=(30, 14, 18))
        d.polygon([(hx - 6, hy + 30), (hx - 2, hy + 30), (hx - 4, hy + 39)], fill=(240, 240, 235))
        d.polygon([(hx + 6, hy + 30), (hx + 2, hy + 30), (hx + 4, hy + 39)], fill=(240, 240, 235))
    else:
        d.arc([(hx - 10) * S, (hy + 24) * S, (hx) * S, (hy + 34) * S], 0, 120, fill=(20, 18, 22), width=2 * S)
        d.arc([(hx) * S, (hy + 24) * S, (hx + 10) * S, (hy + 34) * S], 60, 180, fill=(20, 18, 22), width=2 * S)

    # whiskers
    for s in (-1, 1):
        for k, dy in enumerate((-2, 4, 10)):
            d.line([(hx + s * 30) * S, (hy + 22 + dy) * S, (hx + s * 86) * S, (hy + 10 + dy * 2.2) * S],
                   fill=(196, 196, 206), width=int(1.4 * S))

    # fur strokes
    rnd = random.Random(7)
    for i in range(260):
        x = rnd.uniform(60, 185)
        y = rnd.uniform(60, 215)
        if (x - 122) ** 2 / 62 ** 2 + (y - 168) ** 2 / 66 ** 2 < 1 or (x - 122) ** 2 + (y - 96) ** 2 < 48 ** 2:
            d.line([(x * S, y * S), ((x + rnd.uniform(-3, 3)) * S, (y + rnd.uniform(3, 9)) * S)],
                   fill=(*(tuple(int(c * 0.8) for c in fur)), 120), width=S)

    img = img.resize((W // S, H // S), Image.LANCZOS)
    return img

# ------------------------------------------------------------------------ icon
def make_icon(face_img):
    img = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    base = face_img.copy()
    base.thumbnail((256, 256), Image.LANCZOS)
    img.alpha_composite(base, ((256 - base.width) // 2, (256 - base.height) // 2))
    img.save(os.path.join(ASSETS, "icon.ico"), sizes=[(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    img.save(os.path.join(ASSETS, "icon.png"))
    log("icon ok")

if __name__ == "__main__":
    gr = cutout()
    fc = face(gr)
    wallpaper(gr)
    mar = draw_cat(False); mar.save(os.path.join(ASSETS, "marusya.png")); log("cat", mar.size)
    mar2 = draw_cat(True); mar2.save(os.path.join(ASSETS, "marusya_angry.png"))
    make_icon(fc)
    log("done")
