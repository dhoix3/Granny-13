# -*- coding: utf-8 -*-
"""Item icons + UI atlas (4x2 grid of 128x128 tiles)."""
import os, math, random
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ASSETS = os.path.join(ROOT, "assets")

T = 128
N = 8
atlas = Image.new("RGBA", (T * 4, T * 2), (0, 0, 0, 0))

def tile(i):
    return Image.new("RGBA", (T * 4, T * 4), (0, 0, 0, 0)), ImageDraw.Draw

def new_tile():
    S = 4
    img = Image.new("RGBA", (T * S, T * S), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img), S

def finish(img, i):
    global atlas
    img = img.resize((T, T), Image.LANCZOS)
    atlas.alpha_composite(img, ((i % 4) * T, (i // 4) * T))

def shadow(d, S, pts=None, box=None, r=24):
    pass

# ---------------------------------------------------------------- 0: key
def icon_key():
    img, d, S = new_tile()
    gold = (226, 186, 88); goldd = (168, 128, 44); goldl = (255, 226, 150)
    cx, cy = 64, 64
    # bow (ring)
    d.ellipse([(cx - 46) * S, (cy - 26) * S, (cx - 4) * S, (cy + 16) * S], outline=goldd, width=12 * S)
    d.ellipse([(cx - 46) * S, (cy - 26) * S, (cx - 4) * S, (cy + 16) * S], outline=gold, width=7 * S)
    d.ellipse([(cx - 38) * S, (cy - 18) * S, (cx - 12) * S, (cy + 8) * S], fill=(0, 0, 0, 0))
    # shaft
    d.rounded_rectangle([(cx - 6) * S, (cy - 8) * S, (cx + 44) * S, (cy + 6) * S], radius=4 * S, fill=goldd)
    d.rounded_rectangle([(cx - 6) * S, (cy - 8) * S, (cx + 42) * S, (cy + 2) * S], radius=4 * S, fill=gold)
    # teeth
    d.rectangle([(cx + 26) * S, (cy + 2) * S, (cx + 34) * S, (cy + 22) * S], fill=goldd)
    d.rectangle([(cx + 38) * S, (cy + 2) * S, (cx + 44) * S, (cy + 16) * S], fill=goldd)
    d.line([(cx + 26) * S, (cy + 2) * S, (cx + 34) * S, (cy + 2) * S], fill=goldl, width=2 * S)
    finish(img, 0)

# ---------------------------------------------------------------- 1: crowbar
def icon_crowbar():
    img, d, S = new_tile()
    c = (176, 62, 48); cd = (110, 38, 30); cl = (226, 120, 100)
    d.line([(26 * S, 108 * S), (58 * S, 66 * S), (86 * S, 54 * S)], fill=cd, width=13 * S, joint="curve")
    d.line([(26 * S, 108 * S), (58 * S, 66 * S), (86 * S, 54 * S)], fill=c, width=8 * S, joint="curve")
    d.line([(30 * S, 102 * S), (60 * S, 64 * S)], fill=cl, width=2 * S)
    # claw
    d.arc([(74 * S, 26 * S), (112 * S, 62 * S)], 150, 350, fill=cd, width=9 * S)
    d.arc([(74 * S, 26 * S), (112 * S, 62 * S)], 150, 350, fill=c, width=5 * S)
    d.polygon([(22 * S, 106 * S), (30 * S, 118 * S), (38 * S, 110 * S)], fill=cd)
    finish(img, 1)

# ---------------------------------------------------------------- 2: note
def icon_note():
    img, d, S = new_tile()
    paper = (232, 224, 200); pd = (186, 176, 148)
    d.polygon([(28 * S, 18 * S), (88 * S, 14 * S), (104 * S, 30 * S), (100 * S, 110 * S), (26 * S, 108 * S)], fill=pd)
    d.polygon([(30 * S, 20 * S), (86 * S, 17 * S), (100 * S, 32 * S), (97 * S, 106 * S), (29 * S, 104 * S)], fill=paper)
    rnd = random.Random(3)
    for y in range(32, 96, 9):
        w = rnd.randint(40, 66)
        d.line([(36 * S, y * S), ((36 + w) * S, (y + 1) * S)], fill=(96, 86, 70), width=int(1.6 * S))
    # red code stamp
    d.rounded_rectangle([(56 * S, 60 * S), (92 * S, 82 * S)], radius=3 * S, outline=(170, 40, 36), width=3 * S)
    d.text((62 * S, 63 * S), "137", fill=(170, 40, 36))
    finish(img, 2)

# ---------------------------------------------------------------- 3: milk
def icon_milk():
    img, d, S = new_tile()
    w = (238, 238, 234); wd = (188, 190, 190); blue = (86, 140, 200)
    d.rounded_rectangle([(42 * S, 40 * S), (86 * S, 112 * S)], radius=5 * S, fill=wd)
    d.rounded_rectangle([(44 * S, 42 * S), (84 * S, 110 * S)], radius=4 * S, fill=w)
    d.polygon([(46 * S, 40 * S), (82 * S, 40 * S), (76 * S, 22 * S), (52 * S, 22 * S)], fill=w)
    d.rectangle([(48 * S, 16 * S), (80 * S, 26 * S)], fill=(220, 220, 216))
    d.rectangle([(46 * S, 62 * S), (82 * S, 88 * S)], fill=blue)
    d.ellipse([(58 * S, 68 * S), (70 * S, 82 * S)], fill=(250, 250, 250))
    finish(img, 3)

# ---------------------------------------------------------------- 4: battery
def icon_battery():
    img, d, S = new_tile()
    body = (58, 58, 64); lime = (204, 214, 74)
    d.rounded_rectangle([(44 * S, 24 * S), (84 * S, 108 * S)], radius=6 * S, fill=body)
    d.rectangle([(52 * S, 24 * S), (76 * S, 30 * S)], fill=(180, 180, 180))
    d.rectangle([(48 * S, 34 * S), (80 * S, 66 * S)], fill=lime)
    d.rectangle([(48 * S, 74 * S), (80 * S, 100 * S)], fill=(40, 40, 44))
    d.polygon([(64 * S, 76 * S), (54 * S, 92 * S), (63 * S, 92 * S), (60 * S, 102 * S)], fill=lime)
    d.polygon([(64 * S, 76 * S), (54 * S, 92 * S), (63 * S, 92 * S)], fill=(240, 240, 160))
    finish(img, 4)

# ---------------------------------------------------------------- 5: bandage
def icon_bandage():
    img, d, S = new_tile()
    c1 = (222, 214, 198); c2 = (186, 176, 158)
    d.rounded_rectangle([(20 * S, 44 * S), (108 * S, 84 * S)], radius=12 * S, fill=c2)
    d.rounded_rectangle([(22 * S, 46 * S), (106 * S, 82 * S)], radius=10 * S, fill=c1)
    d.ellipse([(48 * S, 52 * S), (80 * S, 76 * S)], fill=(176, 60, 56))
    d.rectangle([(62 * S, 56 * S), (66 * S, 72 * S)], fill=(240, 230, 220))
    d.rectangle([(56 * S, 62 * S), (72 * S, 66 * S)], fill=(240, 230, 220))
    for x in range(26, 44, 6):
        d.line([(x * S, 48 * S), ((x + 4) * S, 50 * S)], fill=c2, width=2 * S)
    finish(img, 5)

# ---------------------------------------------------------------- 6: beartrap
def icon_trap():
    img, d, S = new_tile()
    metal = (150, 152, 160); metd = (92, 94, 102); rust = (128, 74, 44)
    d.ellipse([(14 * S, 40 * S), (114 * S, 104 * S)], fill=metd)
    d.ellipse([(18 * S, 44 * S), (110 * S, 100 * S)], fill=(60, 58, 62))
    # jaws
    for side in (0, 1):
        y = 40 if side == 0 else 100
        d.polygon([(18 * S, y * S), (58 * S, (y + (-14 if side == 0 else 14)) * S),
                   (110 * S, y * S), (110 * S, (y + (6 if side == 0 else -6)) * S),
                   (58 * S, (y + (-4 if side == 0 else 4)) * S), (18 * S, (y + (8 if side == 0 else -8)) * S)], fill=metal)
        for k in range(5):
            tx = 30 + k * 17
            d.polygon([(tx * S, (y + 2 * (1 if side == 0 else -1)) * S), ((tx + 6) * S, (y - 16 * (1 if side == 0 else -1)) * S),
                       ((tx + 10) * S, (y + 2 * (1 if side == 0 else -1)) * S)], fill=metal)
    d.ellipse([(54 * S, 62 * S), (74 * S, 82 * S)], fill=rust)
    d.ellipse([(58 * S, 66 * S), (70 * S, 78 * S)], fill=(70, 40, 26))
    finish(img, 6)

# ---------------------------------------------------------------- 7: padlock
def icon_lock():
    img, d, S = new_tile()
    gold = (214, 176, 84); goldd = (152, 116, 44)
    d.arc([(42 * S, 18 * S), (86 * S, 62 * S)], 180, 360, fill=goldd, width=11 * S)
    d.arc([(42 * S, 18 * S), (86 * S, 62 * S)], 180, 360, fill=gold, width=7 * S)
    d.rounded_rectangle([(32 * S, 48 * S), (96 * S, 108 * S)], radius=8 * S, fill=goldd)
    d.rounded_rectangle([(35 * S, 51 * S), (93 * S, 104 * S)], radius=6 * S, fill=gold)
    d.rectangle([(58 * S, 64 * S), (66 * S, 84 * S)], fill=(80, 56, 20))
    d.ellipse([(56 * S, 60 * S), (68 * S, 72 * S)], fill=(80, 56, 20))
    finish(img, 7)

for f in (icon_key, icon_crowbar, icon_note, icon_milk, icon_battery, icon_bandage, icon_trap, icon_lock):
    f()

atlas.save(os.path.join(ASSETS, "items.png"))
print("[assets] items atlas", atlas.size)
