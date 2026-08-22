#!/usr/bin/env python3
"""Draw the Ace of Shadows card art into Assets/Art/Cards/cards_sheet.png.

Everything is drawn white on transparent. The 144 distinct cards come from tinting
13 sprites at runtime with SpriteRenderer.color (12 glyphs x 12 colours).

All 13 land on one sheet, laid out here rather than left to a Sprite Atlas asset:
the packed result is then a committed file rather than something the Editor's
sprite-packer mode decides, so one texture is guaranteed on every machine and in
the WebGL build. The rects also go to tools/cards_sheet.json as a record of the
layout -- Unity does not read it, so a layout change means re-slicing the sheet in
the Sprite Editor by hand.

    python3 tools/generate_card_sprites.py

Requires Pillow. Re-run it after editing a shape; the sheet is committed.
"""

import json
import math
import os

from PIL import Image, ImageDraw

# Pillow's ImageDraw has no anti-aliasing, so every shape is drawn large and
# downsampled. 4x is the point past which LANCZOS stops visibly improving.
SS = 4

GLYPH = 256
CARD_W, CARD_H = 256, 358          # 5:7, the usual playing-card ratio

# White fill, mid-grey border. SpriteRenderer.color multiplies, so one tint gives
# the card its colour and the border the same colour darkened, in one draw.
BORDER_LEVEL = 140

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Art", "Cards")
RECTS = os.path.join(ROOT, "tools", "cards_sheet.json")

# Bilinear filtering samples a texel past a sprite's edge, so every cell is padded.
PAD = 4
COLUMNS = 4

# Block-compressed formats need whole blocks on both axes: 4 covers DXT/BC and ASTC
# 4x4. The larger ASTC footprints run to 12x12 and do not divide 4, so a sheet that
# must suit those as well needs a multiple of 120.
BLOCK = 4

S = GLYPH * SS
C = S / 2
R = S * 0.39


def new_mask():
    m = Image.new("L", (S, S), 0)
    return m, ImageDraw.Draw(m)


def ngon(n, r, rot=-math.pi / 2, cx=C, cy=C):
    return [(cx + r * math.cos(rot + 2 * math.pi * i / n),
             cy + r * math.sin(rot + 2 * math.pi * i / n)) for i in range(n)]


def star(points, r_out, r_in, rot=-math.pi / 2):
    pts = []
    for i in range(points * 2):
        r = r_out if i % 2 == 0 else r_in
        a = rot + math.pi * i / points
        pts.append((C + r * math.cos(a), C + r * math.sin(a)))
    return pts


def circle():
    m, d = new_mask()
    d.ellipse([C - R, C - R, C + R, C + R], fill=255)
    return m


def ring():
    m, d = new_mask()
    d.ellipse([C - R, C - R, C + R, C + R], fill=255)
    ri = R * 0.58
    d.ellipse([C - ri, C - ri, C + ri, C + ri], fill=0)
    return m


def square():
    m, d = new_mask()
    a = R * 0.86
    d.rounded_rectangle([C - a, C - a, C + a, C + a], radius=S * 0.03, fill=255)
    return m


def diamond():
    m, d = new_mask()
    d.polygon(ngon(4, R * 1.06), fill=255)
    return m


def triangle():
    m, d = new_mask()
    d.polygon(ngon(3, R * 1.05, cy=C + R * 0.10), fill=255)
    return m


def chevron():
    m, d = new_mask()
    w, h, t = R * 0.95, R * 0.80, R * 0.42
    d.polygon([(C - w, C - h), (C, C + h * 0.15), (C + w, C - h),
               (C + w, C - h + t), (C, C + h * 0.15 + t), (C - w, C - h + t)], fill=255)
    d.polygon([(C - w, C + h * 0.02), (C, C + h * 1.05), (C + w, C + h * 0.02),
               (C + w, C + h * 0.02 + t), (C, C + h * 1.05 + t), (C - w, C + h * 0.02 + t)], fill=255)
    return m


def hexagon():
    m, d = new_mask()
    d.polygon(ngon(6, R * 1.02), fill=255)
    return m


def spiral():
    # 2.5 turns at this width leaves a clear gap between them; more turns, or a
    # thicker line, and the arms merge into a disc with hairline seams.
    m, d = new_mask()
    turns = 2.5
    pts, t = [], 0.0
    while t <= turns * 2 * math.pi:
        r = R * 0.12 + (R * 0.98 - R * 0.12) * (t / (turns * 2 * math.pi))
        pts.append((C + r * math.cos(t), C + r * math.sin(t)))
        t += 0.04
    d.line(pts, fill=255, width=int(R * 0.18), joint="curve")
    r0 = R * 0.11
    d.ellipse([C - r0, C - r0, C + r0, C + r0], fill=255)
    return m


def star4():
    m, d = new_mask()
    d.polygon(star(4, R * 1.12, R * 0.30), fill=255)
    return m


def crescent():
    m, d = new_mask()
    d.ellipse([C - R, C - R, C + R, C + R], fill=255)
    ox, ri = R * 0.46, R * 0.92
    d.ellipse([C + ox - ri, C - ri, C + ox + ri, C + ri], fill=0)
    return m


def bolt():
    m, d = new_mask()
    w, h = R * 0.62, R * 1.10
    d.polygon([(C + w * 0.55, C - h), (C - w, C + h * 0.12), (C - w * 0.10, C + h * 0.12),
               (C - w * 0.55, C + h), (C + w, C - h * 0.18), (C + w * 0.06, C - h * 0.18)], fill=255)
    return m


def droplet():
    # The classic teardrop curve x = cos t, y = sin t * sin^3(t/2), rotated point-up.
    m, d = new_mask()
    pts = []
    n = 240
    for i in range(n):
        t = 2 * math.pi * i / n
        x = math.cos(t)
        y = math.sin(t) * (math.sin(t / 2) ** 3)
        pts.append((C + y * R * 1.26, C - x * R * 1.10))
    d.polygon(pts, fill=255)
    return m


# Order matters: CardTableView maps glyph index i % 12 onto this list, so
# reordering it reshuffles which glyph pairs with which colour.
SHAPES = [
    ("circle", circle), ("ring", ring), ("square", square), ("diamond", diamond),
    ("triangle", triangle), ("chevron", chevron), ("hexagon", hexagon), ("spiral", spiral),
    ("star4", star4), ("crescent", crescent), ("bolt", bolt), ("droplet", droplet),
]


def white_on_alpha(mask, size):
    img = Image.new("RGBA", size, (255, 255, 255, 0))
    img.paste((255, 255, 255), (0, 0), mask)
    img.putalpha(mask)
    return img


def card_body():
    w, h = CARD_W * SS, CARD_H * SS
    radius = CARD_W * SS * 0.08
    inset = 2 * SS          # keeps the card off the texture edge so the atlas cannot bleed
    border = 3 * SS

    fill = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(fill)
    d.rounded_rectangle([inset, inset, w - 1 - inset, h - 1 - inset], radius=radius, fill=255)

    body = Image.new("L", (w, h), 255)
    d = ImageDraw.Draw(body)
    d.rounded_rectangle([inset, inset, w - 1 - inset, h - 1 - inset],
                        radius=radius, fill=255, outline=BORDER_LEVEL, width=border)

    img = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    img.paste(body.convert("RGB"), (0, 0), fill)
    img.putalpha(fill)
    return img.resize((CARD_W, CARD_H), Image.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)

    body_row_h = CARD_H + 2 * PAD
    glyph_col_w = GLYPH + 2 * PAD
    glyph_row_h = GLYPH + 2 * PAD
    rows = (len(SHAPES) + COLUMNS - 1) // COLUMNS
    sheet_w = -(-(COLUMNS * glyph_col_w) // BLOCK) * BLOCK
    sheet_h = -(-(body_row_h + rows * glyph_row_h) // BLOCK) * BLOCK

    sheet = Image.new("RGBA", (sheet_w, sheet_h), (255, 255, 255, 0))
    rects = []

    def place(name, img, x, y):
        sheet.paste(img, (x, y))
        # Unity measures sprite rects from the bottom-left; PIL from the top-left.
        rects.append({"name": name, "x": x, "y": sheet_h - (y + img.height),
                      "w": img.width, "h": img.height})

    place("card_body", card_body(), PAD, PAD)

    for i, (name, fn) in enumerate(SHAPES):
        mask = fn().resize((GLYPH, GLYPH), Image.LANCZOS)
        x = (i % COLUMNS) * glyph_col_w + PAD
        y = body_row_h + (i // COLUMNS) * glyph_row_h + PAD
        place("glyph_%s" % name, white_on_alpha(mask, (GLYPH, GLYPH)), x, y)

    sheet.save(os.path.join(OUT, "cards_sheet.png"))
    with open(RECTS, "w") as f:
        json.dump({"texture": "Assets/Art/Cards/cards_sheet.png",
                   "width": sheet_w, "height": sheet_h, "sprites": rects}, f, indent=2)

    print("wrote cards_sheet.png (%dx%d, %d sprites) and %s"
          % (sheet_w, sheet_h, len(rects), RECTS))


if __name__ == "__main__":
    main()
