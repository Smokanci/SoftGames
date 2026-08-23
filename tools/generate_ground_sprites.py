#!/usr/bin/env python3
"""Draw the Ember Dust ground texture into Assets/Art/UI/.

One texture: ui_flat.png, the flat fill behind everything. It is white on
transparent so the runtime tint decides the hue -- the same trick the button,
card and flame art use.

    python3 tools/generate_ground_sprites.py

Requires Pillow. The PNG is committed, so a clone needs neither Python nor
Pillow; re-run this only after changing the constants below.

The bloom in the middle of the ground is ember_radial.png, drawn by
generate_ui_sprites.py -- the ground reuses it rather than owning a second blob.
The ash over both is a particle system in Bootstrap.unity, not a texture.
"""

import os

from PIL import Image

FLAT_SIZE = 64                     # the fill is stretched by its transform, so this only needs to
                                   # survive mip generation

OUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Art", "UI")


def draw_flat():
    return Image.new("RGBA", (FLAT_SIZE, FLAT_SIZE), (255, 255, 255, 255))


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    draw_flat().save(os.path.join(OUT_DIR, "ui_flat.png"))
    print("wrote ui_flat.png to", OUT_DIR)


if __name__ == "__main__":
    main()
