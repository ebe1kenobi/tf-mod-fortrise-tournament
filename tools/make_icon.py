#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Genere l'icone du bouton TOURNAMENT du menu principal.

Icone 64x64 a palette reduite avec contour sombre, au format des icones de
boutons du jeu (trialsMode, questModeGlow... font toutes 64x64).

    python make_icon.py        ->  ../ModFile/Content/Atlas/tournamentMode.png

Dependance : Pillow  ->  pip install Pillow
"""

from PIL import Image, ImageDraw

W = H = 64
OUTLINE = (43, 22, 10, 255)
DARK    = (150, 96, 24, 255)
BASE    = (222, 162, 46, 255)
LIGHT   = (255, 222, 128, 255)

def new(): return Image.new("RGBA", (W, H), (0, 0, 0, 0))

shape = new()
d = ImageDraw.Draw(shape)

# --- anses, dessinees d'abord pour que le bol les recouvre au raccord ---
d.arc([10, 18, 26, 36], 60, 300, fill=BASE, width=3)
d.arc([38, 18, 54, 36], 240, 120, fill=BASE, width=3)

# --- bol : trapeze + fond arrondi ---
d.polygon([(17, 17), (47, 17), (43, 33), (21, 33)], fill=BASE)
d.ellipse([21, 25, 43, 41], fill=BASE)

# --- levre ---
d.rectangle([16, 15, 47, 20], fill=BASE)

# --- tige et socle ---
d.rectangle([29, 40, 34, 46], fill=BASE)
d.polygon([(24, 46), (39, 46), (42, 51), (21, 51)], fill=BASE)
d.rectangle([19, 51, 44, 56], fill=BASE)

px = shape.load()

def is_solid(x, y):
    return 0 <= x < W and 0 <= y < H and px[x, y][3] > 0

# --- ombrage : bord droit assombri, reflet vertical a gauche du bol ---
for y in range(H):
    for x in range(W):
        if not is_solid(x, y): continue
        if x >= 38 and not is_solid(x + 3, y):
            px[x, y] = DARK          # bord droit de chaque forme
        elif 21 <= x <= 25 and 16 <= y <= 36:
            px[x, y] = LIGHT         # reflet dans le bol
        elif y <= 19 and 16 <= x <= 47:
            px[x, y] = LIGHT         # levre eclairee

# --- contour ---
out = new()
op = out.load()
for y in range(H):
    for x in range(W):
        if is_solid(x, y): continue
        if any(is_solid(x + dx, y + dy)
               for dx, dy in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1))):
            op[x, y] = OUTLINE
out.alpha_composite(shape)

import os
dest = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                    "..", "ModFile", "Content", "Atlas", "tournamentMode.png")
os.makedirs(os.path.dirname(dest), exist_ok=True)
out.save(dest)
print(f"{os.path.normpath(dest)} ({len({p for p in out.getdata() if p[3]})} couleurs)")
