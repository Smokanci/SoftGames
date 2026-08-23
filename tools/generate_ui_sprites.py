#!/usr/bin/env python3
"""Draw the Ember button textures into Assets/Art/UI/.

Four textures, all white on transparent so the runtime tint decides the hue --
the same trick the card and flame art use. ui_panel.png is the button's rounded
face, ui_rim.png is the same rectangle as an outline only, ember_radial.png is
the soft blob behind both the resting heat and the press bloom, and ui_pause.png
is the two-bar glyph on the pause button.

    python3 tools/generate_ui_sprites.py

Requires Pillow. All four PNGs are committed, so a clone needs neither Python nor
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

# The pause glyph is not nine-sliced, so these proportions are the ones that reach
# the screen. They are fractions of GLYPH_SIZE, which keeps the shape intact if the
# source is ever redrawn larger.
GLYPH_SIZE = 64
GLYPH_BAR_WIDTH = 0.18             # each bar, as a fraction of the square
GLYPH_BAR_HEIGHT = 0.62
GLYPH_BAR_GAP = 0.16               # clear space between the two bars
GLYPH_BAR_RADIUS = 0.045

OUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Art", "UI")


def rounded_coverage(x, y, w, h, radius):
    """Signed distance to a rounded rectangle's edge: negative inside, positive outside."""
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


def draw_pause():
    size = GLYPH_SIZE * SUPERSAMPLE
    bar_width = GLYPH_BAR_WIDTH * size
    bar_height = GLYPH_BAR_HEIGHT * size
    radius = min(GLYPH_BAR_RADIUS * size, bar_width / 2.0)
    left = (size - (2.0 * bar_width + GLYPH_BAR_GAP * size)) / 2.0
    top = (size - bar_height) / 2.0

    image = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    pixels = image.load()

    for y in range(size):
        for x in range(size):
            local_y = y + 0.5 - top
            for bar in range(2):
                local_x = x + 0.5 - (left + bar * (bar_width + GLYPH_BAR_GAP * size))
                if rounded_coverage(local_x, local_y, bar_width, bar_height, radius) <= 0.0:
                    pixels[x, y] = (255, 255, 255, 255)
                    break

    return image.resize((GLYPH_SIZE, GLYPH_SIZE), Image.LANCZOS)


def main():
    if BORDER <= RADIUS:
        raise SystemExit("BORDER must exceed RADIUS or the nine-slice cuts through the corner curve")

    os.makedirs(OUT_DIR, exist_ok=True)
    draw_rect(stroke_only=False).save(os.path.join(OUT_DIR, "ui_panel.png"))
    draw_rect(stroke_only=True).save(os.path.join(OUT_DIR, "ui_rim.png"))
    draw_radial().save(os.path.join(OUT_DIR, "ember_radial.png"))
    draw_pause().save(os.path.join(OUT_DIR, "ui_pause.png"))
    print("wrote ui_panel.png, ui_rim.png, ember_radial.png and ui_pause.png to", OUT_DIR)
    print("nine-slice border for both rectangles:", BORDER)


if __name__ == "__main__":
    main()
