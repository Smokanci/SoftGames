#!/usr/bin/env python3
"""Draw the Phoenix Flame particle textures into Assets/Art/Flame/.

Three textures, all white on transparent so the runtime tint decides the colour --
the same trick the card art uses. flame_puff.png is the ragged, hot-centred blob
the core and body emitters draw; smoke_puff.png is the soft cloud the smoke
emitter draws; spark_puff.png is the hot dot the spark emitter draws.

    python3 tools/generate_flame_sprites.py

Requires Pillow. All three PNGs are committed, so a clone needs neither Python nor
Pillow; re-run this only after changing the constants below.

The wisps come from value noise whose influence ramps with distance from the
centre, so the middle of a puff stays solid and only its edge breaks up. A puff
with noise applied evenly reads as dirty rather than fiery.

The spark texture carries no noise at all. A spark covers a handful of pixels, and
at that size the wisps that shape a puff read as a dirty edge rather than as fire --
the ember has to come from a clean radial falloff.
"""

import math
import os
import random

from PIL import Image

SIZE = 128

# Lattice resolution of each noise octave, in cells across the texture, and its weight.
FLAME_OCTAVES = ((7, 1.0), (14, 0.5), (28, 0.25))
SMOKE_OCTAVES = ((4, 1.0), (8, 0.5))

FLAME_FALLOFF = 1.6                # exponent on the radial ramp; higher is tighter
SMOKE_FALLOFF = 2.0

FLAME_WISP = 1.8                   # how hard the noise pushes the alpha around
SMOKE_WISP = 1.1

# Exponent on the distance-from-centre term that scales the noise. Raising it keeps
# the noise further out and leaves a larger solid core.
FLAME_EDGE = 2.0
SMOKE_EDGE = 1.2

FLAME_CORE = 0.3                   # extra alpha piled into the middle of a flame puff
SMOKE_OPACITY = 0.8                # smoke never reaches full alpha

SPARK_SIZE = 64
SPARK_HALO = 3.0                   # exponent on the soft glow around the head
SPARK_HALO_WEIGHT = 0.35
SPARK_CORE = 14.0                  # exponent on the hot centre; higher is tighter

SEED = 20240822

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Art", "Flame")


def lattice(cells, rng):
    # One extra row and column so the bilinear lookup never needs to wrap.
    return [[rng.random() for _ in range(cells + 1)] for _ in range(cells + 1)]


def smoothstep(t):
    return t * t * (3.0 - 2.0 * t)


def sample(grid, cells, u, v):
    x, y = u * cells, v * cells
    x0, y0 = int(x), int(y)
    fx, fy = smoothstep(x - x0), smoothstep(y - y0)

    top = grid[y0][x0] * (1.0 - fx) + grid[y0][x0 + 1] * fx
    bottom = grid[y0 + 1][x0] * (1.0 - fx) + grid[y0 + 1][x0 + 1] * fx
    return top * (1.0 - fy) + bottom * fy


def fbm(octaves, rng):
    grids = [(cells, weight, lattice(cells, rng)) for cells, weight in octaves]
    total = sum(weight for _, weight in octaves)

    def evaluate(u, v):
        acc = sum(weight * sample(grid, cells, u, v) for cells, weight, grid in grids)
        return acc / total

    return evaluate


def puff(octaves, falloff, wisp, edge, core, opacity, rng):
    noise = fbm(octaves, rng)
    field = [[noise((x + 0.5) / SIZE, (y + 0.5) / SIZE) for x in range(SIZE)]
             for y in range(SIZE)]

    # Stretched to the full 0..1 range: three averaged octaves land in a narrow band
    # around 0.5, and the wisps come out invisible at any usable weight.
    low = min(min(row) for row in field)
    span = max(max(row) for row in field) - low

    image = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 0))
    pixels = image.load()
    half = SIZE / 2.0

    for y in range(SIZE):
        for x in range(SIZE):
            dx, dy = (x + 0.5 - half) / half, (y + 0.5 - half) / half
            d = min(math.hypot(dx, dy), 1.0)

            n = (field[y][x] - low) / span
            ramp = (1.0 - d) ** falloff

            alpha = ramp * (1.0 + wisp * (d ** edge) * (n - 0.5) * 2.0)
            alpha += core * (1.0 - d) ** 6.0
            alpha = max(0.0, min(1.0, alpha)) * opacity

            pixels[x, y] = (255, 255, 255, int(round(alpha * 255)))

    return image


def spark():
    image = Image.new("RGBA", (SPARK_SIZE, SPARK_SIZE), (255, 255, 255, 0))
    pixels = image.load()
    half = SPARK_SIZE / 2.0

    for y in range(SPARK_SIZE):
        for x in range(SPARK_SIZE):
            dx, dy = (x + 0.5 - half) / half, (y + 0.5 - half) / half
            d = min(math.hypot(dx, dy), 1.0)

            alpha = SPARK_HALO_WEIGHT * (1.0 - d) ** SPARK_HALO + (1.0 - d) ** SPARK_CORE
            alpha = max(0.0, min(1.0, alpha))

            pixels[x, y] = (255, 255, 255, int(round(alpha * 255)))

    return image


def main():
    os.makedirs(OUT, exist_ok=True)

    rng = random.Random(SEED)
    puff(FLAME_OCTAVES, FLAME_FALLOFF, FLAME_WISP, FLAME_EDGE, FLAME_CORE, 1.0, rng).save(
        os.path.join(OUT, "flame_puff.png"))
    puff(SMOKE_OCTAVES, SMOKE_FALLOFF, SMOKE_WISP, SMOKE_EDGE, 0.0, SMOKE_OPACITY, rng).save(
        os.path.join(OUT, "smoke_puff.png"))

    spark().save(os.path.join(OUT, "spark_puff.png"))

    print("wrote flame_puff.png, smoke_puff.png and spark_puff.png to", OUT)


if __name__ == "__main__":
    main()
