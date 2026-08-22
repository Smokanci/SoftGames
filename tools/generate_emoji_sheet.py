#!/usr/bin/env python3
"""Lift the Magic Words emoji out of Noto Color Emoji into a TMP sprite sheet.

Noto Color Emoji is a CBDT/CBLC font -- every glyph is a PNG, not an outline. TMP's
font-asset pipeline loads glyphs with FreeType's no-bitmap flag, so it reads that
font as having no glyphs at all and bakes an empty atlas. The supported route for
colour emoji in TMP is a sprite asset, which wants a plain sheet of images, so this
script takes the PNGs straight out of the font's bitmap table and lays them out.

    python3 tools/generate_emoji_sheet.py

Requires fonttools and Pillow. The sheet and Assets/Art/Emoji/emoji_sheet.json are
committed; re-run after changing EMOJI, then re-slice in Unity.

The source font is Assets/Art/Fonts/NotoColorEmoji-Subset.ttf, itself produced by:

    pyftsubset NotoColorEmoji.ttf --unicodes=1F60C,1F928,1F610,1F44D,1F602,1F3C6 \
        --output-file=NotoColorEmoji-Subset.ttf

The subset is committed too, so this script never needs the 10 MB original.
"""

import io
import json
import os

from fontTools.ttLib import TTFont
from PIL import Image

# Order sets the sheet layout. These names label the sprites inside the TMP sprite
# asset only -- the {tokens} the endpoint sends are mapped separately, in
# Assets/Data/EmojiTable.asset, so the two lists do not have to agree.
EMOJI = [
    ("satisfied",   0x1F60C),
    ("intrigued",   0x1F928),
    ("neutral",     0x1F610),
    ("affirmative", 0x1F44D),
    ("laughing",    0x1F602),
    ("win",         0x1F3C6),
]

COLUMNS = 3
PAD = 4                            # bilinear filtering samples a texel past the edge

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONT = os.path.join(ROOT, "Assets", "Art", "Fonts", "NotoColorEmoji-Subset.ttf")
OUT = os.path.join(ROOT, "Assets", "Art", "Emoji")


def bitmaps(font):
    cmap = font.getBestCmap()
    strike = font["CBDT"].strikeData[0]
    out = []
    for name, codepoint in EMOJI:
        glyph_name = cmap[codepoint]
        image = Image.open(io.BytesIO(strike[glyph_name].imageData)).convert("RGBA")
        out.append((name, codepoint, image))
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    font = TTFont(FONT)
    images = bitmaps(font)

    cell_w = max(i.width for _, _, i in images) + PAD * 2
    cell_h = max(i.height for _, _, i in images) + PAD * 2
    rows = (len(images) + COLUMNS - 1) // COLUMNS
    sheet = Image.new("RGBA", (cell_w * COLUMNS, cell_h * rows), (0, 0, 0, 0))

    rects = []
    for index, (name, codepoint, image) in enumerate(images):
        column, row = index % COLUMNS, index // COLUMNS
        x = column * cell_w + (cell_w - image.width) // 2
        y = row * cell_h + (cell_h - image.height) // 2
        sheet.paste(image, (x, y))
        # Unity measures a sprite rect from the bottom-left; Pillow from the top-left.
        rects.append({
            "name": name,
            "unicode": "%X" % codepoint,
            "x": x,
            "y": sheet.height - y - image.height,
            "w": image.width,
            "h": image.height,
        })

    sheet.save(os.path.join(OUT, "emoji_sheet.png"))
    with open(os.path.join(OUT, "emoji_sheet.json"), "w") as handle:
        json.dump({
            "texture": "Assets/Art/Emoji/emoji_sheet.png",
            "width": sheet.width,
            "height": sheet.height,
            "sprites": rects,
        }, handle, indent=2)
        handle.write("\n")

    print("%dx%d, %d sprites" % (sheet.width, sheet.height, len(rects)))


if __name__ == "__main__":
    main()
