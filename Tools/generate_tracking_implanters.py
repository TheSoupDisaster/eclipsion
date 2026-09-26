"""Generates the faction vitals-tracking implanter RSIs.

The vanilla implanter.rsi is left alone as the neutral one (MedicalTrackingImplanter still uses it).
Every faction implanter gets its own RSI palette-swapped off that silhouette, so the syringe still
reads as a syringe, plus three overpainted tells:

  faction colour  -- chassis tint and the accent that fills the barrel window when the implant is loaded
  barrel stripe   -- a per-faction pattern on the window frame, so two factions read apart in greyscale
  clearance bars  -- rank bars on the rear grip: command 3 gold, service 2 accent, civilian 1 pale,
                     echoed by the hub dot next to the needle

Adding a faction = one IMPLANTERS entry (and a STRIPES pattern if it needs a new one).

States: implanter0 (body, window cut out), implanter1 (the fill seen through the window), broken.
The prototypes only override `sprite:`; layers and the GenericVisualizer are inherited from
BaseImplantOnlyImplanter, so the state names above are load-bearing.

Usage: python Tools/generate_tracking_implanters.py [repo] [--preview out.png]   (needs Pillow;
overwrites Resources/Textures/_Crescent/Objects/Specific/Medical/TrackingImplanters in place)
"""
import json
import os
import sys
from PIL import Image

argv = sys.argv[1:]
PREVIEW = None
if "--preview" in argv:
    i = argv.index("--preview")
    PREVIEW = argv[i + 1]
    del argv[i:i + 2]
REPO = argv[0] if argv else os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
BASE = os.path.join(REPO, "Resources/Textures/Objects/Specific/Medical/implanter.rsi")
OUT = os.path.join(REPO, "Resources/Textures/_Crescent/Objects/Specific/Medical/TrackingImplanters")

WHITE = (255, 255, 255)
GOLD = (226, 190, 96)
GOLD_DK = (150, 118, 44)
# Civilian bars are a dark slate rather than a pale one -- a pale bar vanishes on a light chassis.
PALE = (58, 62, 72)
PALE_DK = (36, 39, 46)

# The vanilla plunger is blue; those pixels become the faction accent instead of the chassis ramp.
PLUNGER = {(94, 171, 235), (84, 153, 209), (78, 140, 191), (151, 180, 186), (171, 205, 212)}

# Drawn area of implanter.rsi: rows 13..21, the barrel window frame at x 11..17 on rows 16/18,
# the rear grip block at x 18..23 on rows 16..19, the needle hub at x 24.
WIN_X0, WIN_X1 = 11, 17
STRIPE_ROWS = (16, 18)
GRIP_X0, GRIP_X1 = 18, 23
GRIP_Y0, GRIP_Y1 = 16, 19
HUB = (24, 17)
FILL_Y = 17

# chassis: body tint the grey ramp is pulled toward.  accent: fill, plunger and service bars.
# stripe:  window-frame pattern (STRIPES).  tier: which clearance bars the grip carries.
IMPLANTERS = {
    "dsm_command":  dict(chassis="#4A4256", accent="#9D7BE6", stripe="double",  tier="command"),
    "dsm_civilian": dict(chassis="#6E6478", accent="#CBC3E3", stripe="split",   tier="civilian"),
    "cmm":          dict(chassis="#3A4658", accent="#4E8FD6", stripe="chevron", tier="service"),
    "ncwl":         dict(chassis="#51533F", accent="#D9962B", stripe="hazard",  tier="service"),
    "shi":          dict(chassis="#535961", accent="#6FB0A6", stripe="solid",   tier="service"),
    "interdyne":    dict(chassis="#7A5560", accent="#F8BABA", stripe="band",    tier="civilian"),
}

# 7 wide, one string per row of STRIPE_ROWS.  '#' etched dark, '-' half dark, '.' leave the chassis.
# Etched rather than accent-coloured so the pattern still separates two factions in greyscale.
STRIPES = {
    "solid":   ["#######", "-------"],
    "double":  ["##.#.##", "##.#.##"],
    "split":   ["###.###", "..-.-.."],
    "dash":    ["##.##.#", ".-..-.."],
    "hazard":  ["#.-#.-#", "-#.-#.-"],
    "chevron": [".#.#.#.", "#.#.#.#"],
    "pips":    ["#.#.#.#", "..-.-.."],
    "band":    ["-#####-", "-#####-"],
}

# Clearance stripes painted as countable vertical bars down the 6-wide rear grip:
# three gold for command, two accent for service, one pale for civilian.  The hub dot repeats the colour.
TIERS = {
    "command":  dict(cols=(0, 2, 4), bar="gold",   hub="gold"),
    "service":  dict(cols=(1, 4),    bar="accent", hub="accent"),
    "civilian": dict(cols=(2,),      bar="pale",   hub="pale"),
}


def rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def scale(c, k):
    return tuple(max(0, min(255, int(round(v * k)))) for v in c)


def luma(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


def recolor(px, chassis, accent):
    """Remap the vanilla grey ramp onto a chassis-tinted one so the body carries the faction hue at a
    glance; the blue plunger and needle go to the accent instead."""
    if px[3] == 0:
        return px
    c = px[:3]
    l = luma(c) / 255.0
    if c in PLUNGER:
        return mix(scale(accent, 0.5), mix(accent, WHITE, 0.35), min(1.0, l * 1.15)) + (px[3],)
    lo = scale(chassis, 0.22)
    hi = mix(chassis, WHITE, 0.55)
    return mix(lo, hi, min(1.0, l ** 0.85)) + (px[3],)


def paint(img, x, y, c):
    if 0 <= x < img.width and 0 <= y < img.height:
        img.putpixel((x, y), c + (255,))


def build(fac):
    chassis, accent = rgb(fac["chassis"]), rgb(fac["accent"])
    tier = TIERS[fac["tier"]]
    bar = {"gold": GOLD, "pale": PALE, "accent": accent}[tier["bar"]]
    bar_dk = {"gold": GOLD_DK, "pale": PALE_DK, "accent": scale(accent, 0.55)}[tier["bar"]]
    hub = {"gold": GOLD, "pale": PALE, "accent": accent}[tier["hub"]]
    stripe = STRIPES[fac["stripe"]]

    out = {}
    for name in ("implanter0", "broken"):
        src = Image.open(os.path.join(BASE, name + ".png")).convert("RGBA")
        img = Image.new("RGBA", src.size, (0, 0, 0, 0))
        for y in range(src.height):
            for x in range(src.width):
                img.putpixel((x, y), recolor(src.getpixel((x, y)), chassis, accent))

        # window frame stripe -- only over pixels the base actually drew
        etch, etch_dk = scale(chassis, 0.62), mix(chassis, WHITE, 0.30)
        for row, pattern in zip(STRIPE_ROWS, stripe):
            for i, ch in enumerate(pattern):
                x = WIN_X0 + i
                if ch == "." or x > WIN_X1 or src.getpixel((x, row))[3] == 0:
                    continue
                paint(img, x, row, etch if ch == "#" else etch_dk)

        # clearance bars down the rear grip
        for i in tier["cols"]:
            x = GRIP_X0 + i
            if x > GRIP_X1:
                continue
            for y in range(GRIP_Y0, GRIP_Y1 + 1):
                if src.getpixel((x, y))[3] == 0:
                    continue
                paint(img, x, y, bar_dk if y == GRIP_Y1 else bar)

        # hub dot beside the needle repeats the clearance colour
        if src.getpixel(HUB)[3] != 0:
            paint(img, HUB[0], HUB[1], hub)
        out[name] = img

    # the fill seen through the window when an implant is loaded
    fill = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    for i, x in enumerate(range(WIN_X0, WIN_X1 + 1)):
        paint(fill, x, FILL_Y, accent if i % 2 == 0 else scale(accent, 0.86))
    out["implanter1"] = fill
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    tiles = []
    for name, fac in IMPLANTERS.items():
        states = build(fac)
        rsi = os.path.join(OUT, name + ".rsi")
        os.makedirs(rsi, exist_ok=True)
        for state, img in states.items():
            img.save(os.path.join(rsi, state + ".png"))
        meta = {
            "version": 1,
            "license": "CC-BY-SA-3.0",
            "copyright": "Palette swap of Objects/Specific/Medical/implanter.rsi (taken from vgstation13 "
                         "commit 1cdfb0230cc96d0ba751fa002d04f8aa2f25ad7d, resprite by @linkblyat), "
                         "faction markings by Taleryn via Tools/generate_tracking_implanters.py",
            "size": {"x": 32, "y": 32},
            "states": [{"name": s} for s in ("broken", "implanter0", "implanter1")],
        }
        with open(os.path.join(rsi, "meta.json"), "w", encoding="utf-8", newline="\n") as f:
            json.dump(meta, f, indent=2)
            f.write("\n")
        loaded = Image.alpha_composite(states["implanter1"], states["implanter0"])
        tiles.append((name, states["implanter0"], loaded, states["broken"]))
        print("wrote", os.path.relpath(rsi, REPO))

    if PREVIEW:
        z = 6
        sheet = Image.new("RGBA", (32 * 3 * z, 32 * len(tiles) * z), (28, 30, 34, 255))
        for r, (_, empty, loaded, broken) in enumerate(tiles):
            for c, img in enumerate((empty, loaded, broken)):
                big = img.resize((32 * z, 32 * z), Image.NEAREST)
                sheet.alpha_composite(big, (c * 32 * z, r * 32 * z))
        sheet.save(PREVIEW)
        print("preview", PREVIEW)


if __name__ == "__main__":
    main()
