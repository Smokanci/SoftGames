#!/usr/bin/env python3
"""Draw the Ember button textures into Assets/Art/UI/.

Three textures, all white on transparent so the runtime tint decides the hue --
the same trick the card and flame art use. ui_panel.png is the button's rounded
face, ui_rim.png is the same rectangle as an outline only, and ember_radial.png is
the soft blob behind both the resting heat and the press bloom.

    python3 tools/generate_ui_sprites.py

Requires Pillow. All three PNGs are committed, so a clone needs neither Python nor
Pillow; re-run this only after changing the constants below.

The two rectangles are nine-sliced, so their size in the scene is independent of
the size drawn here -- only the corner radius is baked in. BORDER must stay larger
than RADIUS or the slice cuts through the curve and the corners smear.

The radial blob is drawn with a squared falloff rather than a linear one because a
linear ramp reads as a flat disc with a hard edge once it is tinted and stretched
across a button.
"""

import math
import os

from PIL import Image

SUPERSAMPLE = 4                    # drawn at this multiple, then box-filtered down

RECT_SIZE = 64                     # nine-slice source square, in pixels
RADIUS = 18                        # corner radius of both rectangles
BORDER = 24                        # nine-slice border; must exceed RADIUS
RIM_WIDTH = 2                      # stroke width of ui_rim.png

RADIAL_SIZE = 256
RADIAL_FALLOFF = 2.0               # exponent on the radial ramp; higher is tighter

OUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Art", "UI")


def rounded_coverage(x, y, w, h, radius):
    """Signed-distance coverage of a rounded rectangle, 1.0 inside, 0.0 outside."""
    dx = abs(x - w / 2.0) - (w / 2.0 - radius)
    dy = abs(y - h / 2.0) - (h / 2.0 - radius)
    dx = max(dx, 0.0)
    dy = max(dy, 0.0)
    return math.sqrt(dx * dx + dy * dy) - radius


def draw_rect(stroke_only):
    size = RECT_SIZE * SUPERSAMPLE
    radius = RADIUS * SUPERSAMPLE
    width = RIM_WIDTH * SUPERSAMPLE
    image = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    pixels = image.load()

    for y in range(size):
        for x in range(size):
            distance = rounded_coverage(x + 0.5, y + 0.5, size, size, radius)
            if stroke_only:
                inside = distance <= 0.0 and distance >= -width
            else:
                inside = distance <= 0.0
            pixels[x, y] = (255, 255, 255, 255 if inside else 0)

    return image.resize((RECT_SIZE, RECT_SIZE), Image.LANCZOS)


def draw_radial():
    size = RADIAL_SIZE * SUPERSAMPLE
    image = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    pixels = image.load()
    centre = size / 2.0

    for y in range(size):
        for x in range(size):
            dx = (x + 0.5 - centre) / centre
            dy = (y + 0.5 - centre) / centre
            distance = math.sqrt(dx * dx + dy * dy)
            ramp = max(0.0, 1.0 - distance)
            pixels[x, y] = (255, 255, 255, int(round(255 * (ramp ** RADIAL_FALLOFF))))

    return image.resize((RADIAL_SIZE, RADIAL_SIZE), Image.LANCZOS)


def main():
    if BORDER <= RADIUS:
        raise SystemExit("BORDER must exceed RADIUS or the nine-slice cuts through the corner curve")

    os.makedirs(OUT_DIR, exist_ok=True)
    draw_rect(stroke_only=False).save(os.path.join(OUT_DIR, "ui_panel.png"))
    draw_rect(stroke_only=True).save(os.path.join(OUT_DIR, "ui_rim.png"))
    draw_radial().save(os.path.join(OUT_DIR, "ember_radial.png"))
    print("wrote ui_panel.png, ui_rim.png and ember_radial.png to", OUT_DIR)
    print("nine-slice border for both rectangles:", BORDER)


if __name__ == "__main__":
    main()
