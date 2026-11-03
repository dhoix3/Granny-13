# -*- coding: utf-8 -*-
"""Pick a wallpaper patch with no white robe edge / background."""
import numpy as np
from PIL import Image, ImageFilter

SRC = r"C:\Users\saaho\Documents\Granny 13\Granny.jpg"
OUT = r"C:\Users\saaho\Documents\Granny 13\assets\wallpaper.png"

im = Image.open(SRC).convert("RGB")
a = np.asarray(im).astype(np.int16)
h, w, _ = a.shape
mx = a.max(axis=2)
mn = a.min(axis=2)
sat = mx - mn
bad = (mx > 232) & (sat < 30)          # white background / pale edge
bad |= (mx > 245)                       # blown-out highlights

ii = np.zeros((h + 1, w + 1), dtype=np.int64)
ii[1:, 1:] = bad.cumsum(0).cumsum(1)

S = 150
best = None
for y0 in range(760, 960, 5):
    for x0 in range(540, 700, 5):
        tot = ii[y0 + S, x0 + S] - ii[y0, x0 + S] - ii[y0 + S, x0] + ii[y0, x0]
        if best is None or tot < best[0]:
            best = (tot, x0, y0)
tot, x0, y0 = best
print("patch", x0, y0, "bad px", tot)
patch = im.crop((x0, y0, x0 + S, y0 + S)).resize((256, 256), Image.LANCZOS)
patch = patch.filter(ImageFilter.GaussianBlur(0.3))
patch.save(OUT)
patch.save(r"C:\Users\saaho\Documents\Granny 13\tools\_wp_check.png")
print("saved")
