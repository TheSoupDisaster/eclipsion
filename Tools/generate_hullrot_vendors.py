"""Generates the Hullrot faction vendor RSIs.

Vendor chassis, drawn from scratch rather than reskinned from the RMC ColMarTech racks:
  armory -- caged gun locker in the vein of Crescent's own armory.rsi: overhanging hood, sprayed unit
            markings, barred window with the racked stock behind it, keypad column, dispensing tray.
  supply -- slimmer requisitions rack: lit emblem sign, glass front with folded stock on shelves, tray.
  security -- equipment cabinet with a riot shield, cuffs and glasses for the seven faction SecTechs.
  sustenance -- Shinohara's ration vendor: broad lit header, tall window of boxed rations, keypad column.
  budget -- Shinohara's public gun shop: compact sign, colourful pistols and ammunition behind glass.
  surplus -- Taypani's second-hand arms shop: stepped brass cap, crescent sign and horizontal gun racks.
  grill -- the TFCF fast-food vendor: "???" marquee, striped awning, heat-lamp window of burgers and fries.

Every faction gets the armory and supply chassis. The sustenance chassis is Shinohara's alone -- SHI
supplies compact rations to the whole sector, so there is no per-faction variant of it. Beyond the colours, each faction varies the silhouette of the cap, the
emblem, the trim pattern on the header and the pillar dressing, so two vendors read apart even in greyscale.
Adding a faction = one FACTIONS entry (and an EMBLEMS glyph if it needs a new one).

States (all the vendor visualizers use): off, broken, panel, normal-unshaded, deny-unshaded, eject-unshaded.
CMAutomatedVendor racks have no visualizer, so they show off + normal-unshaded statically.

Also palette-swaps the shipyard console screen (shipyard_console.rsi, left untouched as the neutral one) into
ShipyardScreens/<faction>.rsi; consoles opt in with the ShipyardScreen component.

Usage: python Tools/generate_hullrot_vendors.py [repo] [--preview out.png]   (needs Pillow; overwrites
Resources/Textures/_Crescent/Structures/Machines/Vendors in place)
Use --security-only to generate only the faction security RSIs and their optional preview.
"""
import json
import math
import os
import sys
from PIL import Image

argv = sys.argv[1:]
SECURITY_ONLY = "--security-only" in argv
if SECURITY_ONLY:
    argv.remove("--security-only")
PREVIEW = None
if "--preview" in argv:
    i = argv.index("--preview")
    PREVIEW = argv[i + 1]
    del argv[i:i + 2]
REPO = argv[0] if argv else os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT = os.path.join(REPO, "Resources/Textures/_Crescent/Structures/Machines/Vendors")

S = 32
WHITE = (255, 255, 255)
BLACK = (0, 0, 0)
GLASS_TOP = (34, 46, 56)
GLASS_BOT = (16, 21, 27)
GLASS_HI = (86, 108, 124)
SCREEN_OFF = (12, 16, 18)
PLATE = (20, 22, 26)
METAL = (132, 138, 144)
METAL_DK = (70, 74, 80)
STOCK = (74, 58, 44)
LED_GO = (120, 232, 120)
LED_GO_DIM = (48, 100, 52)
DENY = (236, 66, 54)
WIRES = [(206, 62, 52), (226, 196, 64), (84, 186, 96), (74, 126, 214)]


def rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def scale(c, k):
    return tuple(max(0, min(255, int(round(v * k)))) for v in c)


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


# cap:    silhouette above the body (see cap_height)
# emblem: glyph on the crest plate / sign (EMBLEMS)
# trim:   pattern on the header band (trim_color)
# pillar: side dressing -- bolts, a gilded stripe, or a rivet column
FACTIONS = {
    "neutral": dict(chassis="#4A525B", accent="#8FB8C8", cap="flat", emblem="square", trim="solid", pillar="bolts"),
    "dsm": dict(chassis="#48425A", accent="#9D7BE6", cap="crown", emblem="sun", trim="double", pillar="stripe"),
    "ncwl": dict(chassis="#51533F", accent="#D9962B", cap="vents", emblem="star", trim="hazard", pillar="bolts"),
    "shi": dict(chassis="#7F888C", accent="#6FB0A6", cap="dome", emblem="s", trim="solid", pillar="stripe"),
    "tfsc": dict(chassis="#56453F", accent="#D0463F", cap="chamfer", emblem="diamonds", trim="dash", pillar="bolts"),
    "tap": dict(chassis="#766650", accent="#5DBB5A", cap="stepped", emblem="crescent", trim="chevron", pillar="rivets"),
    "srm": dict(chassis="#302C2A", accent="#B8A97A", cap="arch", emblem="cross", trim="double", pillar="stripe"),
    "cmm": dict(chassis="#3A4658", accent="#4E8FD6", cap="badge", emblem="shield", trim="solid", pillar="rivets"),
}

# Shinohara sells rations to every flag in the sector, so its ration vendor is a fleet-service machine
# rather than a showroom one: a darker teal-cast steel than the pale corporate shell the SHI armoury and
# requisitions racks wear, keeping the accent, the dome cap and the "S" so it still reads as Shinohara.
SUSTENANCE = dict(chassis="#3F4E52", accent="#6FB0A6", cap="dome", emblem="s", trim="solid", pillar="stripe")

BUDGET = dict(FACTIONS["shi"], chassis="#626F72", pillar="bolts")
SURPLUS = dict(FACTIONS["tap"], chassis="#82745A", accent="#72BA83")

# The Federation grill wears the TFCF armoury paint and chamfered cap, but swaps the diamonds for a burger.
GRILL = dict(FACTIONS["tfsc"], emblem="burger")

# Gliess has a civilian municipal livery.
SECURITY_FACTIONS = {
    "dsm": FACTIONS["dsm"],
    "shi": FACTIONS["shi"],
    "ncwl": FACTIONS["ncwl"],
    "cmm": FACTIONS["cmm"],
    "tap": FACTIONS["tap"],
    "gs": dict(chassis="#52636A", accent="#D1B878", cap="flat", emblem="square", trim="solid", pillar="bolts"),
    "srm": FACTIONS["srm"],
}

EMBLEMS = {
    "square": [".......", ".#####.", ".#...#.", ".#.#.#.", ".#...#.", ".#####.", "......."],
    "sun": ["#..#..#", ".#####.", ".##.##.", "###.###", ".##.##.", ".#####.", "#..#..#"],
    "star": ["...#...", "...#...", "#######", ".#####.", "..###..", ".##.##.", ".#...#."],
    "s": [".#####.", "##...##", "##.....", ".#####.", ".....##", "##...##", ".#####."],
    "diamonds": [".#...#.", "###.###", ".#...#.", ".......", ".#...#.", "###.###", ".#...#."],
    "crescent": ["..###..", ".##....", "##.....", "##...#.", "##.....", ".##....", "..###.."],
    "cross": ["..###..", "...#...", "#..#..#", "#######", "#..#..#", "...#...", "..###.."],
    "crown": ["#..#..#", "#..#..#", "##.#.##", "#######", "#######", ".......", "#######"],
    "shield": ["#######", "#.....#", "#..#..#", "#.###.#", "#..#..#", ".#...#.", "..###.."],
    "burger": ["..###..", ".#####.", "#######", ".......", "#######", ".......", ".#####."],
}


def palette(fac):
    base = rgb(fac["chassis"])
    acc = rgb(fac["accent"])
    return dict(
        out=scale(base, 0.3), dk=scale(base, 0.7), md=base,
        lt=mix(base, WHITE, 0.16), hi=mix(base, WHITE, 0.32), seam=scale(base, 0.5),
        acc=acc, acc_lt=mix(acc, WHITE, 0.45), acc_dk=scale(acc, 0.58), acc_dim=scale(acc, 0.4),
    )


def cap_height(shape, u, n):
    """Rows of cap above the body at column u of an n-wide body (n odd)."""
    mid = (n - 1) // 2
    d = abs(u - mid)
    e = min(u, n - 1 - u)
    edge = 1 if e == 0 else 2
    if shape == "flat":
        return edge
    if shape == "vents":  # industrial lip with two exhaust stacks
        return 5 if e in (3, 4) else (2 if e == 0 else 3)
    if shape == "crown":  # three points, studs between
        if d <= 1:
            return 5
        if e in (2, 3):
            return 4
        return 3 if d == mid // 2 else edge
    if shape == "dome":  # smooth corporate crown
        return 1 + int(round(2.6 * math.sqrt(max(0.0, 1 - (d / (mid + 1)) ** 2))))
    if shape == "chamfer":  # angular shoulders, notched centre
        return 2 if d <= 2 else min(3, e + 1)
    if shape == "stepped":  # caravan ziggurat
        return 1 if e < 3 else (5 if d <= 3 else 3)
    if shape == "arch":  # pointed gothic arch
        return max(1, 5 - int(d / 2.6))
    if shape == "spire":  # central spire, finials at the shoulders
        if d <= 3:
            return max(2, 5 - d)
        return 4 if e == 2 else edge
    if shape == "badge":  # flat, raised badge mount in the middle
        return 4 if d <= 2 else edge
    raise ValueError(shape)


def trim_color(pat, x, y, y0, y1, p):
    ymid = (y0 + y1) // 2
    if pat == "solid":
        return p["acc"] if y == y0 else (scale(p["acc"], 0.35) if y == y1 else p["acc_dk"])
    if pat == "double":
        return p["acc"] if y in (y0, y1) else p["dk"]
    if pat == "hazard":
        return p["acc"] if ((x + y) // 2) % 2 == 0 else (22, 22, 22)
    if pat == "dash":
        return p["acc"] if (y == ymid and x % 3 != 2) else p["acc_dk"]
    if pat == "chevron":
        return p["acc"] if (x + abs(y - ymid)) % 4 == 0 else p["acc_dk"]
    raise ValueError(pat)


class Canvas:
    def __init__(self):
        self.im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        self.px = self.im.load()

    def put(self, x, y, c, a=255):
        if 0 <= x < S and 0 <= y < S:
            self.px[x, y] = tuple(c[:3]) + (a,)

    def blend(self, x, y, c, t):
        """Mix c into the pixel already there. put() with an alpha would punch a hole in an opaque layer."""
        if not (0 <= x < S and 0 <= y < S):
            return
        r, g, b, a = self.px[x, y]
        if a:
            self.px[x, y] = mix((r, g, b), c, t) + (a,)

    def rect(self, x0, y0, x1, y1, c, a=255):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.put(x, y, c, a)

    def frame(self, x0, y0, x1, y1, c):
        for x in range(x0, x1 + 1):
            self.put(x, y0, c)
            self.put(x, y1, c)
        for y in range(y0, y1 + 1):
            self.put(x0, y, c)
            self.put(x1, y, c)

    def glyph(self, rows, x0, y0, c, a=255):
        for j, row in enumerate(rows):
            for i, ch in enumerate(row):
                if ch == "#":
                    self.put(x0 + i, y0 + j, c, a)


# Geometry. Bodies are odd-width so the emblem centres on a pixel column.
GEO = {
    "security": dict(
        bx0=3, bx1=29, bt=5,
        sign=(4, 5, 28, 13), plate=(5, 4, 13, 12),
        glass=(6, 15, 20, 26), glass_frame=(5, 14, 21, 27),
        screen=(24, 15, 26, 17), screen_frame=(23, 14, 27, 18),
        keys=(24, 20, 26, 22), led=(28, 20), slot=(24, 25, 27, 25),
        hatch=(7, 29, 20, 29), console=(23, 14, 28, 27),
    ),
    "surplus": dict(
        bx0=2, bx1=29, bt=5,
        sign=(4, 5, 27, 12), plate=(4, 4, 12, 12),
        glass=(5, 14, 20, 26), glass_frame=(4, 13, 21, 27),
        screen=(24, 14, 26, 16), screen_frame=(23, 13, 27, 17),
        keys=(24, 19, 26, 22), led=(28, 19), slot=(24, 25, 27, 25),
        hatch=(6, 29, 20, 29), console=(23, 13, 28, 27),
    ),
    "budget": dict(
        bx0=3, bx1=29, bt=4,
        sign=(4, 4, 28, 12), plate=(5, 4, 13, 12),
        glass=(7, 14, 20, 26), glass_frame=(6, 13, 21, 27),
        screen=(24, 15, 27, 17), screen_frame=(23, 14, 28, 18),
        keys=(24, 20, 27, 23), led=(28, 20), slot=(24, 26, 27, 26),
        hatch=(7, 29, 20, 29), console=(23, 13, 28, 27),
    ),
    "armory": dict(
        bx0=2, bx1=29, hood=(1, 1, 30, 3),
        band=(2, 4, 29, 10), plate=(4, 4, 10, 10), label=(12, 4, 27, 10),
        glass=(5, 13, 20, 26), glass_frame=(4, 12, 21, 27),
        screen=(23, 13, 28, 16), screen_frame=(22, 12, 29, 17),
        keys=(23, 19, 27, 22), led=(28, 19), slot=(23, 24, 27, 24),
        hatch=(6, 29, 19, 29), console=(22, 12, 29, 27),
    ),
    "supply": dict(
        bx0=4, bx1=26, bt=6,
        sign=(7, 7, 23, 15), plate=(11, 7, 19, 15),
        glass=(8, 18, 18, 26), glass_frame=(7, 17, 19, 27),
        screen=(21, 18, 22, 20), screen_frame=(20, 17, 23, 21),
        keys=(21, 23, 22, 24), led=(23, 23), slot=(21, 26, 22, 26),
        hatch=(9, 29, 21, 29), console=(20, 17, 23, 27),
    ),
    "sustenance": dict(
        bx0=3, bx1=29, bt=4,
        sign=(4, 5, 28, 15), plate=(6, 6, 14, 14),
        glass=(8, 17, 21, 26), glass_frame=(7, 16, 22, 27),
        screen=(24, 18, 27, 20), screen_frame=(23, 17, 28, 21),
        keys=(24, 23, 27, 24), led=(28, 23), slot=(24, 26, 27, 26),
        hatch=(8, 29, 21, 29), console=(23, 16, 28, 27),
    ),
    "grill": dict(
        bx0=3, bx1=29, bt=4,
        sign=(4, 5, 28, 14), plate=(6, 5, 14, 13), awning=(4, 15, 28, 16),
        glass=(8, 19, 21, 26), glass_frame=(7, 18, 22, 27),
        screen=(24, 19, 27, 21), screen_frame=(23, 18, 28, 22),
        keys=(24, 24, 27, 25), led=(28, 24), slot=(24, 27, 27, 27),
        hatch=(8, 29, 21, 29), console=(23, 18, 28, 27),
    ),
}

STOCK_COLORS = [(92, 98, 76), (124, 124, 128), (188, 180, 158), (62, 72, 98), (106, 76, 62)]


def shelf_items(p):
    """(x, y) -> colour for the folded stock behind the supply glass."""
    px = {}
    cols = [p["acc_dk"]] + STOCK_COLORS
    k = 0
    for top in (18, 21, 24):
        for x0 in (8, 12, 16):
            c = cols[k % len(cols)]
            k += 2 if k % 3 == 0 else 1
            for x in range(x0, x0 + 3):
                if x > 18:
                    continue
                px[(x, top)] = mix(c, WHITE, 0.22)
                px[(x, top + 1)] = c
    return px


def draw_body(kind, fac, p):
    if kind == "armory":
        return armory_body(fac, p)
    g = GEO[kind]
    c = Canvas()
    bx0, bx1, bt = g["bx0"], g["bx1"], g["bt"]
    n = bx1 - bx0 + 1

    # Silhouette mask: cap heightmap + body + plinth, then shade, then outline its border.
    mask = {}
    for u in range(n):
        x = bx0 + u
        h = cap_height(fac["cap"], u, n)
        for k in range(1, h + 1):
            mask[(x, bt - k)] = p["acc"] if k >= 3 else (p["hi"] if k == h - 1 else p["lt"])
        for y in range(bt, 31):
            t = (y - bt) / (30 - bt)
            col = mix(p["md"], p["dk"], t * 0.4)
            if y == bt:
                col = p["seam"]
            elif u == 1:
                col = p["lt"]
            elif u == n - 2:
                col = p["dk"]
            mask[(x, y)] = col
    for x in range(bx0 + 1, bx1):
        mask[(x, 31)] = p["out"]
    for (x, y), col in mask.items():
        c.put(x, y, col)
    for (x, y) in mask:
        if any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
            c.put(x, y, p["out"])

    {"supply": draw_supply, "sustenance": draw_sustenance, "surplus": draw_surplus,
     "grill": draw_grill, "budget": draw_budget, "security": draw_security}[kind](c, g, fac, p)
    return c


def draw_pillar(c, x0, y0, y1, fac, p, mirrored):
    lo, mid, hi = (p["dk"], p["md"], p["lt"]) if mirrored else (p["lt"], p["md"], p["dk"])
    for y in range(y0, y1 + 1):
        c.put(x0, y, lo)
        c.put(x0 + 1, y, mid)
        c.put(x0 + 2, y, hi)
    cx = x0 + 1
    if fac["pillar"] == "bolts":
        for y in (y0 + 2, (y0 + y1) // 2, y1 - 2):
            c.put(cx, y, p["hi"])
            c.put(cx, y + 1, p["out"])
    elif fac["pillar"] == "stripe":
        for y in range(y0 + 1, y1):
            c.put(cx, y, p["acc_dk"])
        c.put(cx, y0 + 1, p["acc_lt"])
        c.put(cx, y1 - 1, p["acc"])
    elif fac["pillar"] == "rivets":
        for y in range(y0 + 1, y1, 3):
            c.put(cx, y, p["hi"])


def draw_plate(c, g, fac, p, glyph_col):
    x0, y0, x1, y1 = g["plate"]
    c.rect(x0, y0, x1, y1, PLATE)
    c.frame(x0, y0, x1, y1, p["out"])
    c.put(x0 + 1, y1 - 1, PLATE)
    c.glyph(EMBLEMS[fac["emblem"]], x0 + 1, y0 + 1, glyph_col)


def draw_console(c, g, p):
    c.frame(*g["screen_frame"], p["out"])
    c.rect(*g["screen"], SCREEN_OFF)
    kx0, ky0, kx1, ky1 = g["keys"]
    for y in range(ky0, ky1 + 1):
        for x in range(kx0, kx1 + 1):
            c.put(x, y, p["hi"] if (x + y) % 2 == 0 else p["dk"])
    c.put(*g["led"], LED_GO_DIM)
    sx0, sy, sx1, _ = g["slot"]
    for x in range(sx0, sx1 + 1):
        c.put(x, sy, p["out"])
    c.put(sx1, sy, p["hi"])


def draw_glass(c, g, p):
    fx0, fy0, fx1, fy1 = g["glass_frame"]
    c.frame(fx0, fy0, fx1, fy1, p["out"])
    x0, y0, x1, y1 = g["glass"]
    for y in range(y0, y1 + 1):
        col = mix(GLASS_TOP, GLASS_BOT, (y - y0) / max(1, y1 - y0))
        for x in range(x0, x1 + 1):
            c.put(x, y, col)


def draw_supply(c, g, fac, p):
    bt = g["bt"]
    for y in range(bt + 1, 30):
        c.put(5, y, p["lt"])
        c.put(6, y, p["seam"])
        c.put(24, y, p["seam"])
        c.put(25, y, p["dk"])
    cx = 5
    if fac["pillar"] == "stripe":
        for y in range(bt + 2, 29):
            c.put(cx, y, p["acc_dk"])
            c.put(25, y, scale(p["acc_dk"], 0.8))
    elif fac["pillar"] == "rivets":
        for y in range(bt + 2, 29, 3):
            c.put(cx, y, p["hi"])
            c.put(25, y, p["lt"])
    else:
        for y in (bt + 3, 18, 27):
            c.put(cx, y, p["hi"])
            c.put(25, y, p["hi"])

    sx0, sy0, sx1, sy1 = g["sign"]
    c.frame(sx0, sy0, sx1, sy1, p["out"])
    for y in range(sy0 + 1, sy1):
        for x in range(sx0 + 1, sx1):
            c.put(x, y, trim_color(fac["trim"], x, y, sy0 + 2, sy1 - 2, p) if (y > sy0 + 1 and y < sy1 - 1) else p["acc_dim"])
    draw_plate(c, g, fac, p, p["acc_dim"])
    for x in range(7, 24):
        c.put(x, 16, p["seam"])

    draw_glass(c, g, p)
    for (x, y), col in shelf_items(p).items():
        c.put(x, y, col)
    for y in (20, 23, 26):
        for x in range(g["glass"][0], g["glass"][2] + 1):
            c.put(x, y, METAL_DK)
    c.put(g["glass"][0], g["glass"][1], GLASS_HI)
    c.put(g["glass"][0] + 1, g["glass"][1], GLASS_HI)

    draw_console(c, g, p)

    hx0, hy, hx1, _ = g["hatch"]
    for x in range(hx0 - 1, hx1 + 2):
        c.put(x, hy - 1, p["out"])
        c.put(x, hy, p["lt"])


# ---------------------------------------------------------------- faction security equipment

SECURITY_LETTERS = (
    ["###", "#..", "###", "..#", "###"],
    ["###", "#..", "##.", "#..", "###"],
    ["###", "#..", "#..", "#..", "###"],
)


def draw_security(c, g, fac, p):
    # Reinforced sides, faction crest and a small SEC sign above the equipment window.
    for x, mirrored in ((3, False), (27, True)):
        draw_pillar(c, x, 14, 28, fac, p, mirrored)
    c.rect(*g["sign"], p["out"])
    for y in (12, 13):
        for x in range(4, 29):
            c.put(x, y, trim_color(fac["trim"], x, y, 12, 13, p))
    draw_plate(c, g, fac, p, p["acc_dim"])
    for x, letter in zip((16, 20, 24), SECURITY_LETTERS):
        c.glyph(letter, x, 6, p["acc_dim"])
    draw_glass(c, g, p)

    # A tall riot shield occupies the left rack; its slit and centre boss stay legible at 1x.
    c.glyph([".#####.", "#######", "#.....#", "#.###.#", "#.....#",
             "#..#..#", "#..#..#", "#.....#", ".#...#.", "..###.."],
            6, 16, METAL)
    c.rect(8, 18, 10, 18, GLASS_HI)
    c.put(9, 22, p["acc"])
    # Cuffs, linked across the centre, on the upper right shelf.
    c.glyph(["##..##", "#.##.#", "##..##"], 14, 16, METAL)
    c.rect(14, 20, 20, 20, METAL_DK)
    # Security glasses with pale lenses on the lower shelf.
    c.glyph(["#######", "##.#.##", ".#...#."], 14, 22, p["acc_dk"])
    c.put(15, 23, GLASS_HI)
    c.put(19, 23, GLASS_HI)
    c.rect(14, 26, 20, 26, METAL_DK)
    draw_console(c, g, p)
    hx0, hy, hx1, _ = g["hatch"]
    c.rect(hx0 - 1, hy - 1, hx1 + 1, hy + 1, p["out"])
    c.rect(hx0, hy, hx1, hy, p["lt"])


# ---------------------------------------------------------------- Taypani surplus shop

SURPLUS_LETTERS = (["###", ".#.", ".#.", ".#.", ".#."],
                   ["###", "#..", "###", "..#", "###"])


def draw_surplus(c, g, fac, p):
    # A reclaimed field locker: brass edging, a stepped trade sign and mismatched stock.
    c.frame(*g["sign"], p["out"])
    c.rect(13, 6, 26, 11, PLATE)
    draw_plate(c, g, fac, p, p["acc_dim"])
    for x, letter in zip((16, 21), SURPLUS_LETTERS):
        c.glyph(letter, x, 6, p["acc_dim"])
    for x in range(7, 25):
        c.put(x, 3, p["hi"] if x % 4 else p["acc_dk"])
    for x in (3, 28):
        for y in (6, 11, 15, 24, 28):
            c.put(x, y, p["hi"])
            c.put(x, y + 1, p["seam"])

    draw_glass(c, g, p)
    # Horizontal long guns, with wood furniture and different barrel lengths.
    for y, length, stock in ((15, 13, (133, 94, 60)), (20, 11, (93, 104, 78))):
        c.rect(6, y + 1, 9, y + 2, stock)
        c.put(6, y + 3, stock)
        c.rect(9, y, 6 + length, y, GUN_M)
        c.rect(9, y + 1, 14, y + 1, GUN_B)
        c.put(12, y + 2, GUN_M)
        c.put(13, y + 3, GUN_B)
        c.put(6 + length - 1, y - 1, GUN_H)
        c.put(10, y, GUN_H)
        c.rect(5, y + 4, 20, y + 4, p["seam"])
    # A row of ammunition tins, each with a paper inventory label.
    for x, col in ((6, p["acc_dk"]), (11, CRATE), (16, (102, 74, 57))):
        c.rect(x, 25, x + 3, 26, col)
        c.put(x + 1, 25, (184, 174, 131))
    c.put(5, 14, GLASS_HI)
    c.put(6, 14, GLASS_HI)
    draw_console(c, g, p)
    # Recessed collection tray with a worn brass lip.
    c.rect(5, 28, 21, 30, p["out"])
    c.rect(*g["hatch"], (15, 19, 18))
    c.rect(6, 30, 20, 30, p["lt"])
    for x, y in ((3, 18), (28, 9), (22, 26), (4, 29), (18, 30)):
        c.put(x, y, p["hi"])
        c.put(x + 1, y, p["seam"])


# ---------------------------------------------------------------- budget gun shop

BUDGET_LETTERS = (["##.", "#.#", "##.", "#.#", "##."],
                  [".#.", "#.#", "###", "#.#", "#.#"])
BUDGET_PISTOL = ["hhhhh", "ccccb", ".gg..", ".g..."]
BUDGET_COLORS = [(190, 66, 63), (81, 137, 181), (169, 126, 186), (186, 157, 77)]


def draw_budget(c, g, fac, p):
    # A retail display with a short BA sign leaves room for two rows of cheap sidearms.
    c.frame(*g["sign"], p["out"])
    c.rect(14, 5, 27, 11, p["acc_dim"])
    draw_plate(c, g, fac, p, p["acc_dim"])
    for x, letter in zip((17, 22), BUDGET_LETTERS):
        c.glyph(letter, x, 6, p["acc_dk"])
    for y in range(13, 29):
        c.put(4, y, p["lt"])
        c.put(5, y, p["seam"])
        c.put(22, y, p["seam"])
    for y in (15, 24):
        c.put(4, y, p["hi"])
        c.put(4, y + 1, p["out"])
    draw_glass(c, g, p)
    for i, (x0, y0) in enumerate(((8, 14), (15, 14), (8, 20), (15, 20))):
        colour = BUDGET_COLORS[i]
        cols = {"h": mix(colour, WHITE, 0.35), "c": colour,
                "b": METAL_DK, "g": (51, 46, 43)}
        for dy, row in enumerate(BUDGET_PISTOL):
            for dx, ch in enumerate(row):
                if ch != ".":
                    c.put(x0 + dx, y0 + dy, cols[ch])
    for y in (18, 24):
        c.rect(7, y, 20, y, METAL_DK)
    for x in (9, 16):
        c.rect(x, 19, x + 2, 19, (193, 176, 120))
    for x, colour in ((8, p["acc_dk"]), (12, (157, 126, 65)), (17, METAL_DK)):
        c.rect(x, 25, x + 2, 26, colour)
        c.put(x + 1, 25, mix(colour, WHITE, 0.4))
    draw_console(c, g, p)
    draw_recess(c, g, p)


# ---------------------------------------------------------------- sustenance chassis
#
# Shinohara's compact-ration vendor. It shares the supply rack's silhouette machinery -- heightmap cap,
# plinth, glass front, keypad column -- but is a broader, heavier machine: the header is a wide lit sign
# with the crest plate pushed left and a stencilled ration band beside it, the window is tall enough for
# three shelves of boxed rations, and the tray at the foot is a deep recess rather than a lip.

RATION = [(96, 104, 72), (128, 116, 86), (74, 86, 90), (112, 92, 66), (86, 98, 102)]


def ration_items(g, p):
    """(x, y) -> colour for the boxed rations stacked behind the sustenance glass."""
    px = {}
    x0, y0, x1, y1 = g["glass"]
    k = 0
    for top in (y0 + 1, y0 + 4, y0 + 7):
        x = x0 + 1
        while x + 2 <= x1 - 1:
            col = RATION[k % len(RATION)]
            k += 1
            for dx in range(3):
                px[(x + dx, top)] = mix(col, WHITE, 0.24)
                px[(x + dx, top + 1)] = col
            px[(x + 1, top + 1)] = p["acc_dk"]          # the SHI band printed on every wrapper
            x += 4
    return px


def draw_sustenance(c, g, fac, p):
    bt = g["bt"]
    gx0, gy0, gx1, gy1 = g["glass"]
    fx0, fy0, fx1, fy1 = g["glass_frame"]
    cx0, _, cx1, cy1 = g["console"]

    # Left dressing column and the seam that separates the window from the keypad column.
    draw_pillar(c, fx0 - 3, bt + 2, 29, fac, p, False)
    for y in range(bt + 1, 30):
        c.put(cx0 - 1, y, p["seam"])

    # Header sign: crest plate on the left, stencilled ration band filling the rest.
    sx0, sy0, sx1, sy1 = g["sign"]
    px0, py0, px1, py1 = g["plate"]
    c.frame(sx0, sy0, sx1, sy1, p["out"])
    for y in range(sy0 + 1, sy1):
        for x in range(sx0 + 1, sx1):
            c.put(x, y, p["acc_dim"] if x <= px1 + 1
                  else trim_color(fac["trim"], x, y, py0 + 1, py1 - 1, p))
    draw_plate(c, g, fac, p, p["acc_dim"])
    for row, y in enumerate(range(py0 + 3, py1 - 1, 2)):   # stencilled ration marking on the band
        for x in range(px1 + 3, sx1 - 1 - row * 3):
            if x % 4 != 3:
                c.put(x, y, scale(p["dk"], 0.55))
    for x in range(sx0, sx1 + 1):
        c.put(x, sy1 + 1, p["seam"])

    # Window: three shelves of rations behind glass, with a rail under each.
    draw_glass(c, g, p)
    for (x, y), col in ration_items(g, p).items():
        c.put(x, y, col)
    for y in (gy0 + 3, gy0 + 6, gy0 + 9):
        for x in range(gx0, gx1 + 1):
            c.put(x, y, METAL_DK)
    c.put(gx0, gy0, GLASS_HI)
    c.put(gx0 + 1, gy0, GLASS_HI)

    draw_console(c, g, p)
    draw_recess(c, g, p)


def draw_recess(c, g, p):
    """Deep dispensing recess in the kick plate."""
    for y in range(28, 31):
        for x in range(g["bx0"] + 1, g["bx1"]):
            c.blend(x, y, p["out"], 0.4 if y == 28 else 0.25)
    hx0, hy, hx1, _ = g["hatch"]
    for x in range(hx0, hx1 + 1):
        c.put(x, hy - 1, p["out"])
        c.put(x, hy, (16, 18, 22))
        c.put(x, hy + 1, mix(p["dk"], p["lt"], 0.35))
    c.put(hx0, hy, p["seam"])
    c.put(hx1, hy, p["seam"])


# ---------------------------------------------------------------- grill chassis
#
# The TFCF fast-food vendor. Same heightmap body as the sustenance machine, but it sells to the hungry
# rather than the provisioned: the header is a "???" marquee (nobody will say what is in the patties),
# a striped awning hangs under it, and the window is a heat-lamp warmer with burgers on the top shelf and
# foil wraps and fry cartons below.

QMARK = ["##.", "..#", ".#.", "...", ".#."]
QMARK_X = (17, 21, 25)
QMARK_Y = 6
HEAT = (255, 150, 60)
BUN = (178, 120, 62)
BUN_HI = (222, 170, 98)
PATTY = (84, 50, 36)
FOIL = (150, 152, 160)
FOIL_HI = (196, 198, 204)
FRY = (230, 190, 86)


def grill_lamps(g):
    x0, _, x1, _ = g["glass"]
    return tuple(range(x0 + 2, x1, 4))


def grill_items(g, p):
    """(x, y) -> colour for the food kept warm behind the grill glass."""
    px = {}
    x0, y0, x1, _ = g["glass"]
    for i, x in enumerate(range(x0 + 1, x1 - 1, 4)):           # top shelf: burgers
        for dx in range(3):
            px[(x + dx, y0 + 1)] = BUN_HI if dx == 1 else BUN
            px[(x + dx, y0 + 2)] = PATTY
            px[(x + dx, y0 + 3)] = scale(BUN, 0.8)
        px[(x + 2 - 2 * (i % 2), y0 + 2)] = p["acc"]           # sauce dripping out one side
    for i, x in enumerate(range(x0 + 1, x1 - 1, 4)):           # bottom shelf: wraps and fry cartons
        if i % 2 == 0:
            for dx in range(3):
                px[(x + dx, y0 + 6)] = FOIL_HI
                px[(x + dx, y0 + 7)] = FOIL
            px[(x + 1, y0 + 7)] = p["acc_dk"]
        else:
            px[(x, y0 + 5)] = FRY
            px[(x + 2, y0 + 5)] = FRY
            px[(x + 1, y0 + 5)] = scale(FRY, 0.8)
            for dx in range(3):
                px[(x + dx, y0 + 6)] = p["acc"]
                px[(x + dx, y0 + 7)] = p["acc_dk"]
    return px


def draw_grill(c, g, fac, p):
    bt = g["bt"]
    gx0, gy0, gx1, gy1 = g["glass"]
    fx0 = g["glass_frame"][0]
    cx0 = g["console"][0]

    draw_pillar(c, fx0 - 3, bt + 2, 29, fac, p, False)
    for y in range(bt + 1, 30):
        c.put(cx0 - 1, y, p["seam"])

    # Header marquee: burger plate on the left, three unlit question marks and a dash rail beside it.
    sx0, sy0, sx1, sy1 = g["sign"]
    px1 = g["plate"][2]
    c.frame(sx0, sy0, sx1, sy1, p["out"])
    for y in range(sy0 + 1, sy1):
        for x in range(sx0 + 1, sx1):
            c.put(x, y, p["acc_dim"] if x <= px1 + 1 else scale(p["dk"], 0.6))
    draw_plate(c, g, fac, p, p["acc_dim"])
    for x in QMARK_X:
        c.glyph(QMARK, x, QMARK_Y, p["acc_dim"])
    for x in range(px1 + 2, sx1):
        c.put(x, sy1 - 2, scale(trim_color(fac["trim"], x, 0, -1, 1, p), 0.6))

    # Striped awning with a scalloped lower edge.
    ax0, ay0, ax1, ay1 = g["awning"]
    stripe_lt = mix(p["acc"], WHITE, 0.7)
    for x in range(ax0, ax1 + 1):
        lit = (x // 2) % 2 == 0
        c.put(x, ay0, p["acc"] if lit else stripe_lt)
        if lit:
            c.put(x, ay1, p["acc_dk"])
        else:
            c.put(x, ay1, scale(stripe_lt, 0.7))
            c.put(x, ay1 + 1, p["out"])

    # Warmer window: lamp housings along the top, burgers above a rail, wraps and fries below.
    draw_glass(c, g, p)
    for (x, y), col in grill_items(g, p).items():
        c.put(x, y, col)
    for x in range(gx0, gx1 + 1):
        c.put(x, gy0 + 4, METAL_DK)
    for x in grill_lamps(g):
        c.put(x, gy0, METAL_DK)

    draw_console(c, g, p)
    draw_recess(c, g, p)


# ---------------------------------------------------------------- armory chassis
#
# The reference is Crescent's own armory.rsi: a heavy gunmetal locker with a caged front, not a shop
# display. Faction paint is deliberately sparse -- a sprayed stencil, a marking stripe on the header
# plate, painted ammo cans and the console glow -- so a rack reads as military kit first and as
# <faction> second. The cap shape, the stencil, the header stripe and the pillar bolts still differ
# per faction, which is what tells two racks apart in a dark corridor.

CAGE_TOP = (23, 26, 32)
CAGE_BOT = (9, 10, 13)
WIRE = (150, 158, 168)
GUN_B = (68, 73, 80)       # barrel / sight
GUN_M = (112, 119, 128)    # receiver
GUN_H = (163, 171, 180)    # ejection port glint
GUN_S = (86, 66, 46)       # stock
CAN_DK = (48, 52, 46)
CRATE = (88, 80, 64)
CRATE_DK = (54, 49, 39)

RIFLE = [".b..", ".b..", ".b..", "bbb.", ".b..", "mmh.", ".mm.", ".mg.", ".s..", ".ss."]
SMG = ["....", "....", ".b..", "bbb.", ".b..", "mmh.", ".mm.", ".mg.", ".s..", ".ss."]
GUN_COLS = {"b": GUN_B, "m": GUN_M, "h": GUN_H, "s": GUN_S, "g": GUN_B}


def armory_interior(fac, p):
    """(x, y) -> colour for everything racked behind the wire; shared by the body and the light spill."""
    px = {}
    for x in range(5, 21):                                     # hanging rail
        px[(x, 13)] = GUN_M if x % 5 == 0 else GUN_B
    for slot, x0 in enumerate((6, 11, 16)):
        for j, row in enumerate(RIFLE if slot != 2 else SMG):
            for k, ch in enumerate(row):
                if ch != ".":
                    px[(x0 + k, 14 + j)] = GUN_COLS[ch]
                    px.setdefault((x0 + k + 1, 15 + j), (6, 7, 9))
    for x in range(5, 21):                                     # shelf
        px[(x, 24)] = GUN_B
        px[(x, 25)] = (30, 33, 38)
    for x in range(6, 10):                                     # painted ammo can
        px[(x, 25)] = p["acc_dk"]
        px[(x, 26)] = CAN_DK
    px[(6, 26)] = scale(CAN_DK, 1.3)
    for x in range(11, 15):                                    # magazine stack
        px[(x, 25)] = GUN_M if x % 2 else GUN_B
        px[(x, 26)] = GUN_B
    for x in range(16, 20):                                    # crate
        px[(x, 25)] = CRATE
        px[(x, 26)] = CRATE_DK
    px[(17, 25)] = CRATE_DK
    return px


def armory_cap(c, fac, p, x0, x1):
    """The one row of silhouette above the hood -- kept small so every rack still reads as a locker."""
    shape = fac["cap"]
    mid = (x0 + x1) // 2
    if shape == "vents":
        for x in (x0 + 3, x0 + 4, x1 - 4, x1 - 3):
            c.put(x, 0, p["dk"])
    elif shape == "crown":
        for x in (x0 + 4, mid, x1 - 4):
            c.put(x, 0, p["acc_dk"])
    elif shape == "dome":
        for x in range(x0 + 5, x1 - 4):
            c.put(x, 0, p["lt"])
    elif shape == "stepped":
        for x in range(x0 + 3, x1 - 2):
            c.put(x, 0, p["md"])
        for x in range(x0 + 7, x1 - 6):
            c.put(x, 0, p["lt"])
    elif shape == "arch":
        for x in range(mid - 4, mid + 5):
            c.put(x, 0, p["md"] if abs(x - mid) > 2 else p["lt"])
    elif shape == "spire":
        for x in (mid - 1, mid, mid + 1):
            c.put(x, 0, p["acc"] if x == mid else p["acc_dk"])
        for x in (x0 + 2, x1 - 2):
            c.put(x, 0, p["dk"])
    elif shape == "badge":
        for x in range(mid - 3, mid + 4):
            c.put(x, 0, p["dk"] if abs(x - mid) == 3 else p["md"])


def armory_body(fac, p):
    g = GEO["armory"]
    c = Canvas()
    bx0, bx1 = g["bx0"], g["bx1"]
    hx0, hy0, hx1, hy1 = g["hood"]

    # Cabinet: lit top-left edge, shaded right edge, gentle vertical falloff, plinth at the foot.
    for y in range(hy1 + 1, 31):
        t = (y - hy1) / (30 - hy1)
        for x in range(bx0, bx1 + 1):
            col = mix(p["md"], p["dk"], 0.35 * t)
            if x in (bx0, bx1):
                col = p["out"]
            elif x == bx0 + 1:
                col = p["lt"]
            elif x == bx1 - 1:
                col = p["dk"]
            c.put(x, y, col)
    for x in range(bx0, bx1 + 1):
        c.put(x, 30, p["dk"])
        c.put(x, 31, scale(p["out"], 0.7))
    c.put(bx0, 31, scale(p["out"], 0.5))
    c.put(bx1, 31, scale(p["out"], 0.5))

    # Overhanging hood.
    for x in range(hx0, hx1 + 1):
        c.put(x, hy0, p["hi"] if x < hx1 - 6 else p["lt"])
        c.put(x, hy0 + 1, p["md"] if x < hx1 - 3 else p["seam"])
        c.put(x, hy1, scale(p["dk"], 0.7))
    armory_cap(c, fac, p, hx0, hx1)
    c.frame(hx0, hy0, hx1, hy1, p["out"])
    c.put(hx0 + 1, hy0, p["hi"])
    c.put(hx1 - 1, hy0, p["dk"])

    # Header: sprayed stencil on the left, stamped marking plate on the right.
    bnx0, bny0, bnx1, bny1 = g["band"]
    for y in range(bny0, bny1 + 1):
        for x in range(bnx0 + 1, bnx1):
            c.put(x, y, mix(p["md"], p["dk"], 0.45 if y in (bny0, bny1) else 0.2))
    px0, py0, px1, py1 = g["plate"]
    c.glyph(EMBLEMS[fac["emblem"]], px0, py0, mix(p["acc"], p["dk"], 0.35))
    lx0, ly0, lx1, ly1 = g["label"]
    for y in range(ly0, ly0 + 3):                              # sprayed marking stripe
        for x in range(lx0, lx1 + 1):
            c.put(x, y, mix(trim_color(fac["trim"], x, y, ly0, ly0 + 2, p), p["dk"], 0.3))
    for x in range(lx0, lx1 - 1):                              # stencilled lot number under it
        if x % 4 != 3:
            c.put(x, ly1 - 1, scale(p["dk"], 0.55))
    c.put(lx1 - 1, ly0 + 4, p["hi"])
    c.put(lx1, ly0 + 4, p["out"])
    for x in range(bnx0, bnx1 + 1):                            # seam under the header
        c.put(x, bny1 + 1, p["out"])

    # Side pillar: the faction dressing lives on the bolt column beside the cage.
    fx0, fy0, fx1, fy1 = g["glass_frame"]
    for y in range(fy0, fy1 + 1):
        c.put(bx0 + 1, y, p["lt"] if y % 2 else p["md"])
    if fac["pillar"] == "bolts":
        for y in (fy0 + 2, (fy0 + fy1) // 2, fy1 - 2):
            c.put(bx0 + 1, y, p["hi"])
            c.put(bx0 + 1, y + 1, p["out"])
    elif fac["pillar"] == "stripe":
        for y in range(fy0 + 1, fy1):
            c.put(bx0 + 1, y, p["acc_dk"])
        c.put(bx0 + 1, fy0 + 1, p["acc"])
    else:
        for y in range(fy0 + 1, fy1, 3):
            c.put(bx0 + 1, y, p["hi"])

    # Cage: recessed frame, dark interior, racked stock, wire in front of it.
    c.frame(fx0, fy0, fx1, fy1, p["out"])
    gx0, gy0, gx1, gy1 = g["glass"]
    for y in range(gy0, gy1 + 1):
        col = mix(CAGE_TOP, CAGE_BOT, (y - gy0) / max(1, gy1 - gy0))
        for x in range(gx0, gx1 + 1):
            c.put(x, y, scale(col, 0.55) if (y == gy0 or x == gx0) else col)
    for (x, y), col in armory_interior(fac, p).items():
        c.put(x, y, col)
    for x in range(gx0, gx1 + 1, 5):                           # cage bars, in the gaps between racks
        for y in range(gy0, gy1 + 1):
            c.blend(x, y, WIRE, 0.30)
            c.blend(x + 1, y, (0, 0, 0), 0.18)

    # Control column: readout, keypad, card slot, vent.
    cx0, cy0, cx1, cy1 = g["console"]
    for y in range(cy0, cy1 + 1):
        c.put(cx0, y, p["seam"])
    c.frame(*g["screen_frame"], p["out"])
    c.rect(*g["screen"], SCREEN_OFF)
    kx0, ky0, kx1, ky1 = g["keys"]
    for y in range(ky0, ky1 + 1, 2):
        for x in range(kx0, kx1 + 1, 2):
            c.put(x, y, p["hi"])
            c.put(x, y + 1, p["out"])
    c.put(*g["led"], LED_GO_DIM)
    sx0, sy, sx1, _ = g["slot"]
    for x in range(sx0, sx1 + 1):
        c.put(x, sy, p["out"])
        c.put(x, sy + 1, p["hi"])
    for y in range(cy1 - 1, cy1 + 1):
        for x in range(cx0 + 2, cx1 - 1):
            c.put(x, y, p["dk"] if (x + y) % 2 else p["seam"])

    # Dispensing tray in the kick plate.
    for y in range(28, 31):                                    # kick plate sits in shadow
        for x in range(bx0 + 1, bx1):
            c.blend(x, y, p["out"], 0.45 if y == 28 else 0.3)
    hx0, hy, hx1, _ = g["hatch"]
    for x in range(hx0, hx1 + 1):
        c.put(x, hy - 1, p["out"])
        c.put(x, hy, (16, 18, 22))
        c.put(x, hy + 1, mix(p["dk"], p["lt"], 0.35))
    c.put(hx0, hy, p["seam"])
    c.put(hx1, hy, p["seam"])
    return c


def armory_ambient(c, g, fac, p):
    """Powered: the rack lamp spills down over the stock, the stencil catches a little of it."""
    gx0, gy0, gx1, gy1 = g["glass"]
    content = armory_interior(fac, p)
    for y, a in ((gy0, 78), (gy0 + 1, 40), (gy0 + 2, 18), (gy0 + 3, 8)):
        for x in range(gx0, gx1 + 1):
            if (x, y) not in content:
                c.put(x, y, p["acc_lt"], a)
    for (x, y), col in content.items():
        if y <= gy0 + 6:
            c.put(x, y, mix(col, p["acc_lt"], 0.22 if y <= gy0 + 2 else 0.10))
    px0, py0, _, _ = g["plate"]
    c.glyph(EMBLEMS[fac["emblem"]], px0, py0, p["acc"], 150)


SCREEN_ROWS = [(1, 3), (1, 2), (2, 4), (0, 3)]


def armory_screen(c, g, bg, fg, f, bars=True):
    """Ammunition readout: a short stack of list rows that shuffles frame to frame."""
    x0, y0, x1, y1 = g["screen"]
    c.rect(x0, y0, x1, y1, scale(bg, 0.55))
    if not bars:
        return
    for j in range(y1 - y0 + 1):
        w = SCREEN_ROWS[(f + j) % len(SCREEN_ROWS)][j % 2]
        row = fg if j == f % (y1 - y0 + 1) else mix(fg, scale(bg, 0.55), 0.55)
        for i in range(w):
            c.put(x0 + i, y0 + j, row)
        c.put(x1, y0 + j, fg if (f + j) % 3 == 0 else scale(bg, 0.55))


def armory_broken(fac, p):
    g = GEO["armory"]
    c = armory_body(fac, p)
    gx0, gy0, gx1, gy1 = g["glass"]
    for x in range(gx0, gx1 + 1):                              # racks stripped, two guns left behind
        for y in range(gy0, gy1 + 1):
            if x > gx0 + 4 and not (y >= gy1 - 2 and x > gx1 - 6):
                c.put(x, y, mix(CAGE_TOP, CAGE_BOT, (y - gy0) / (gy1 - gy0)))
            c.blend(x, y, (0, 0, 0), 0.35)
    for dx, dy in ((6, 1), (7, 2), (8, 2), (9, 3), (10, 4), (11, 4), (12, 5), (9, 6), (10, 7)):
        c.blend(gx0 + dx, gy0 + dy, WIRE, 0.75)                # bars torn back
    for dy in range(0, gy1 - gy0, 2):
        c.blend(gx1 - 1, gy0 + dy, WIRE, 0.4)
    sx0, sy0, sx1, sy1 = g["screen"]
    c.rect(sx0, sy0, sx1, sy1, (4, 5, 6))
    c.put(sx0 + 1, sy0, GLASS_HI)
    c.put(sx0 + 2, sy0 + 1, GLASS_HI)
    px0, py0, _, _ = g["plate"]
    c.glyph(EMBLEMS[fac["emblem"]], px0, py0, scale(p["acc_dim"], 0.55))
    for (x, y) in ((22, 26), (23, 27), (24, 26), (25, 28), (21, 28), (26, 27), (23, 25)):
        r, gg, b, a = c.px[x, y]
        if a:
            c.put(x, y, scale((r, gg, b), 0.4))
    c.put(*g["led"], (30, 16, 14))
    return c.im


# ---------------------------------------------------------------- lights (unshaded overlays)

SCREEN_TEXT = [["##.", ".#.", "###"], ["#.#", "##.", ".##"], [".##", "#.#", "##."], ["###", ".#.", "#.."]]
DENY_X = ["#.#", ".#.", "#.#"]
EJECT_ARROW = ["#.#", ".#.", "..."]


def paint_screen(c, g, bg, fg, rows):
    x0, y0, x1, y1 = g["screen"]
    c.rect(x0, y0, x1, y1, bg)
    for j in range(y1 - y0 + 1):
        for i in range(x1 - x0 + 1):
            if j < len(rows) and i < len(rows[j]) and rows[j][i] == "#":
                c.put(x0 + i, y0 + j, fg)


def paint_ambient(c, kind, g, fac, p):
    """Always-on lights: the emblem, plus the rack light (armory) or the sign trim (supply)."""
    if kind == "armory":
        armory_ambient(c, g, fac, p)
        return
    if kind == "security":
        px0, py0, _, _ = g["plate"]
        c.glyph(EMBLEMS[fac["emblem"]], px0 + 1, py0 + 1, p["acc_lt"])
        for x, letter in zip((16, 20, 24), SECURITY_LETTERS):
            c.glyph(letter, x, 6, p["acc_lt"])
        for x in range(6, 21):
            c.put(x, 15, p["acc_lt"], 65)
        return
    if kind == "surplus":
        px0, py0, _, _ = g["plate"]
        c.glyph(EMBLEMS[fac["emblem"]], px0 + 1, py0 + 1, p["acc_lt"])
        for x, letter in zip((16, 21), SURPLUS_LETTERS):
            c.glyph(letter, x, 6, p["acc_lt"])
        for x in range(5, 21):
            c.put(x, 14, p["acc_lt"], 65)
        return
    if kind == "budget":
        px0, py0, _, _ = g["plate"]
        c.glyph(EMBLEMS[fac["emblem"]], px0 + 1, py0 + 1, p["acc_lt"])
        for x, letter in zip((17, 22), BUDGET_LETTERS):
            c.glyph(letter, x, 6, p["acc_lt"])
        for x in range(7, 21):
            c.put(x, 14, p["acc_lt"], 45)
        return
    if kind == "sustenance":
        # Sustenance: the ration band lights up beside the crest; the window gets a shelf lamp at the top.
        px0, py0, px1, py1 = g["plate"]
        sx1 = g["sign"][2]
        for y in range(py0 + 1, py1):
            for x in range(px1 + 2, sx1):
                c.put(x, y, mix(trim_color(fac["trim"], x, y, py0 + 1, py1 - 1, p), WHITE, 0.15))
        for row, y in enumerate(range(py0 + 3, py1 - 1, 2)):
            for x in range(px1 + 3, sx1 - 1 - row * 3):
                if x % 4 != 3:
                    c.put(x, y, scale(p["acc_dk"], 0.45))
        c.glyph(EMBLEMS[fac["emblem"]], px0 + 1, py0 + 1, p["acc_lt"])
        gx0, gy0, gx1, _ = g["glass"]
        for x in range(gx0, gx1 + 1):
            c.put(x, gy0, p["acc_lt"], 90)
            c.put(x, gy0 + 1, p["acc_lt"], 30)
        return
    if kind == "grill":
        # Grill: the marquee and dash rail light up, and the heat lamps wash the food in orange.
        px0, py0, px1, _ = g["plate"]
        sx1, sy1 = g["sign"][2], g["sign"][3]
        for x in QMARK_X:
            c.glyph(QMARK, x, QMARK_Y, p["acc_lt"])
        for x in range(px1 + 2, sx1):
            c.put(x, sy1 - 2, trim_color(fac["trim"], x, 0, -1, 1, p))
        c.glyph(EMBLEMS[fac["emblem"]], px0 + 1, py0 + 1, p["acc_lt"])
        gx0, gy0, gx1, _ = g["glass"]
        food = grill_items(g, p)
        lamps = grill_lamps(g)
        for y, a in ((gy0, 110), (gy0 + 1, 55), (gy0 + 2, 25)):
            for x in range(gx0, gx1 + 1):
                if y == gy0 and x in lamps:
                    c.put(x, y, mix(HEAT, WHITE, 0.45))
                elif (x, y) in food:
                    c.put(x, y, mix(food[(x, y)], HEAT, 0.3))
                else:
                    c.put(x, y, HEAT, a)
        return
    # Supply: the trim pattern lights up rather than washing out into a flat block; the plate stays dark.
    px0, py0, px1, _ = g["plate"]
    sx0, sy0, sx1, sy1 = g["sign"]
    for y in range(sy0 + 1, sy1):
        for x in range(sx0 + 1, sx1):
            if px0 <= x <= px1:
                continue
            if sy0 + 1 < y < sy1 - 1:
                c.put(x, y, mix(trim_color(fac["trim"], x, y, sy0 + 2, sy1 - 2, p), WHITE, 0.15))
            else:
                c.put(x, y, p["acc"])
    gx0, gy0, gx1, _ = g["glass"]
    for x in range(gx0, gx1 + 1):
        c.put(x, gy0, p["acc_lt"], 70)
    c.glyph(EMBLEMS[fac["emblem"]], px0 + 1, py0 + 1, p["acc_lt"])


def lights_normal(kind, fac, p, f):
    g = GEO[kind]
    c = Canvas()
    paint_ambient(c, kind, g, fac, p)
    if kind == "armory":
        armory_screen(c, g, p["acc_dk"], p["acc_lt"], f)
    else:
        paint_screen(c, g, p["acc_dk"], p["acc_lt"], SCREEN_TEXT[f % 4])
    c.put(*g["led"], LED_GO if f < 2 else LED_GO_DIM)
    return c.im


def lights_deny(kind, fac, p, f):
    g = GEO[kind]
    c = Canvas()
    paint_ambient(c, kind, g, fac, p)
    if kind == "armory":
        armory_screen(c, g, scale(DENY, 0.5 if f == 0 else 0.3), DENY, f, bars=False)
        c.frame(*g["screen"], DENY if f == 0 else scale(DENY, 0.6))
        if f == 0:
            c.put(*g["led"], DENY)
    elif f == 0:
        paint_screen(c, g, scale(DENY, 0.45), DENY, DENY_X)
        c.put(*g["led"], DENY)
    else:
        paint_screen(c, g, scale(DENY, 0.3), scale(DENY, 0.6), DENY_X)
    return c.im


EJECT_FRAMES = 6


def lights_eject(kind, fac, p, f):
    g = GEO[kind]
    c = Canvas()
    paint_ambient(c, kind, g, fac, p)
    rows = EJECT_ARROW if f % 2 == 0 else ["...", "#.#", ".#."]
    if kind == "armory":
        armory_screen(c, g, p["acc_dk"], p["acc_lt"], f * 2)
    else:
        paint_screen(c, g, p["acc_dk"], p["acc_lt"], rows)
    c.put(*g["led"], LED_GO)
    hx0, hy, hx1, _ = g["hatch"]
    mid = (hx0 + hx1) // 2
    reach = int(round((hx1 - hx0) / 2 * min(1.0, (f + 1) / (EJECT_FRAMES - 2))))
    fade = 1.0 if f < EJECT_FRAMES - 1 else 0.5
    for x in range(mid - reach, mid + reach + 1):
        c.put(x, hy, scale(p["acc_lt"], fade))
    return c.im


# ---------------------------------------------------------------- broken / panel

CRACKS = [(0, 0), (1, 1), (1, 2), (2, 3), (3, 3), (4, 4), (4, 5), (5, 6), (3, 5), (2, 6), (6, 2), (7, 1)]


def draw_broken(kind, fac, p):
    if kind == "armory":
        return armory_broken(fac, p)
    g = GEO[kind]
    c = draw_body(kind, fac, p)
    gx0, gy0, gx1, gy1 = g["glass"]
    ox, oy = gx0 + 2, gy0 + 1
    for dx, dy in CRACKS:
        x, y = ox + dx, oy + dy
        if gx0 <= x <= gx1 and gy0 <= y <= gy1:
            c.put(x, y, GLASS_HI)
    for (x, y) in ((ox + 1, oy + 4), (ox + 5, oy + 3), (ox + 6, oy + 5)):
        if gx0 <= x <= gx1 and gy0 <= y <= gy1:
            c.put(x, y, (6, 7, 9))
    sx0, sy0, sx1, sy1 = g["screen"]
    c.rect(sx0, sy0, sx1, sy1, (4, 5, 6))
    c.put(sx0, sy0, GLASS_HI)
    c.put(sx0 + 1, sy0 + 1, GLASS_HI)
    px0, py0, _, _ = g["plate"]
    c.glyph(EMBLEMS[fac["emblem"]], px0 + 1, py0 + 1, scale(p["acc_dim"], 0.6))
    c.put(px0 + 3, py0 + 4, PLATE)
    # Scorch across the lower right.
    for (x, y) in ((20, 27), (21, 27), (22, 28), (19, 28), (21, 29), (23, 26), (22, 25)):
        r, gg, b, a = c.px[x, y]
        if a:
            c.put(x, y, scale((r, gg, b), 0.45))
    c.put(*g["led"], (30, 16, 14))
    return c.im


def draw_panel(kind, fac, p):
    g = GEO[kind]
    c = Canvas()
    x0, y0, x1, y1 = g["console"]
    y0 = g["keys"][1] - 1
    c.frame(x0, y0, x1, y1, p["out"])
    c.rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, (12, 12, 14))
    for i, x in enumerate(range(x0 + 1, x1)):
        col = WIRES[i % len(WIRES)]
        for y in range(y0 + 1, y1):
            if (y + i) % 3 != 0:
                c.put(x, y, col)
    # Door swung open against the side seam.
    for y in range(y0, y1 + 1):
        c.put(x1 + 1, y, p["hi"])
    return c.im


# ---------------------------------------------------------------- output

def sheet(frames):
    cols = math.ceil(math.sqrt(len(frames)))
    rows = math.ceil(len(frames) / cols)
    im = Image.new("RGBA", (cols * S, rows * S), (0, 0, 0, 0))
    for i, fr in enumerate(frames):
        im.paste(fr, ((i % cols) * S, (i // cols) * S))
    return im


def write_rsi(kind, name, fac):
    p = palette(fac)
    path = os.path.join(OUT, f"{kind}_{name}.rsi")
    os.makedirs(path, exist_ok=True)
    states = {
        "off": [draw_body(kind, fac, p).im],
        "broken": [draw_broken(kind, fac, p)],
        "panel": [draw_panel(kind, fac, p)],
        "normal-unshaded": [lights_normal(kind, fac, p, f) for f in range(4)],
        "deny-unshaded": [lights_deny(kind, fac, p, f) for f in range(2)],
        "eject-unshaded": [lights_eject(kind, fac, p, f) for f in range(EJECT_FRAMES)],
    }
    delays = {"normal-unshaded": 0.5, "deny-unshaded": 0.25, "eject-unshaded": 0.2}
    meta_states = []
    for st, frames in states.items():
        sheet(frames).save(os.path.join(path, st + ".png"))
        entry = {"name": st}
        if len(frames) > 1:
            entry["delays"] = [[delays[st]] * len(frames)]
        meta_states.append(entry)
    meta = {
        "version": 1,
        "license": "CC-BY-SA-3.0",
        "copyright": "Original art for Eclipsion by Taleryn",
        "size": {"x": S, "y": S},
        "states": meta_states,
    }
    if kind == "surplus":
        meta["copyright"] = "Original Taypani surplus layout based on Taleryn's Hullrot vendor generator."
    if kind == "security":
        meta["copyright"] = "Faction security equipment layout based on Taleryn's Hullrot vendor generator."
    with open(os.path.join(path, "meta.json"), "w", encoding="utf-8", newline="\n") as fh:
        json.dump(meta, fh, indent=2)
        fh.write("\n")
    return states


# ---------------------------------------------------------------- shipyard console screens

SCREEN_SRC = os.path.join(REPO, "Resources/Textures/_Crescent/Structures/Machines/shipyard_console.rsi")
SCREEN_OUT = os.path.join(REPO, "Resources/Textures/_Crescent/Structures/Machines/ShipyardScreens")
# Taleryn's screen is drawn in exactly these four colours; each maps onto a step of the faction accent.
SCREEN_KEYS = {(24, 57, 70): "bg", (34, 81, 96): "edge", (72, 191, 200): "mid", (182, 246, 239): "light"}


def write_screen(name, fac):
    """Palette-swap the shared shipyard screen onto a faction accent. The original stays the neutral one."""
    acc = rgb(fac["accent"])
    ramp = {"bg": scale(acc, 0.31), "edge": scale(acc, 0.45), "mid": acc, "light": mix(acc, WHITE, 0.62)}
    im = Image.open(os.path.join(SCREEN_SRC, "screen.png")).convert("RGBA")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            key = SCREEN_KEYS.get((r, g, b))
            if key is None:
                raise ValueError(f"shipyard screen has an unmapped colour {(r, g, b)} at {x},{y}; extend SCREEN_KEYS")
            px[x, y] = ramp[key] + (a,)
    path = os.path.join(SCREEN_OUT, f"{name}.rsi")
    os.makedirs(path, exist_ok=True)
    im.save(os.path.join(path, "screen.png"))
    with open(os.path.join(SCREEN_SRC, "meta.json"), encoding="utf-8-sig") as fh:
        meta = json.load(fh)
    meta["copyright"] = "Faction recolour of Taleryn's shipyard console screen for Hullrot:Eclipsion"
    with open(os.path.join(path, "meta.json"), "w", encoding="utf-8", newline="\n") as fh:
        json.dump(meta, fh, indent=2)
        fh.write("\n")
    return im


def security_preview(rendered, path):
    from PIL import ImageDraw

    labels = ("OFF", "ON", "DENY", "EJECT", "BROKEN", "PANEL")
    cell, left, top = 140, 64, 24
    out = Image.new("RGBA", (left + len(labels) * cell, top + len(SECURITY_FACTIONS) * cell), (40, 44, 50, 255))
    text = ImageDraw.Draw(out)
    for col, label in enumerate(labels):
        text.text((left + col * cell, 6), label, fill="white")
    for row, name in enumerate(SECURITY_FACTIONS):
        st = rendered[("security", name)]
        off = st["off"][0]
        frames = [off] + [Image.alpha_composite(off, st[state][0]) for state in
                         ("normal-unshaded", "deny-unshaded", "eject-unshaded")]
        frames += [st["broken"][0], Image.alpha_composite(off, st["panel"][0])]
        text.text((4, top + row * cell + 58), name.upper(), fill="white")
        for col, frame in enumerate(frames):
            frame = frame.resize((128, 128), Image.Resampling.NEAREST)
            out.alpha_composite(frame, (left + col * cell, top + row * cell))
    out.save(path)
    print("security preview:", path)


def main():
    rendered = {}
    for name, fac in SECURITY_FACTIONS.items():
        rendered[("security", name)] = write_rsi("security", name, fac)
    if PREVIEW:
        path = PREVIEW if SECURITY_ONLY else os.path.splitext(PREVIEW)[0] + "_security.png"
        security_preview(rendered, path)
    if SECURITY_ONLY:
        print(f"wrote {len(rendered)} security RSIs to {os.path.normpath(OUT)}")
        return
    for kind in ("armory", "supply"):
        for name, fac in FACTIONS.items():
            rendered[(kind, name)] = write_rsi(kind, name, fac)
    rendered[("sustenance", "shi")] = write_rsi("sustenance", "shi", SUSTENANCE)
    rendered[("budget", "shi")] = write_rsi("budget", "shi", BUDGET)
    rendered[("surplus", "tap")] = write_rsi("surplus", "tap", SURPLUS)
    rendered[("grill", "tfsc")] = write_rsi("grill", "tfsc", GRILL)
    print(f"wrote {len(rendered)} RSIs to {os.path.normpath(OUT)}")

    screens = {name: write_screen(name, fac) for name, fac in FACTIONS.items() if name != "neutral"}
    print(f"wrote {len(screens)} shipyard screens to {os.path.normpath(SCREEN_OUT)}")

    if PREVIEW:
        # Rows: faction. Columns: armory off / lit / broken+panel, supply off / lit / broken+panel. 4x.
        z, pad = 4, 4
        names = list(FACTIONS)
        cell = S * z + pad
        kinds = ("armory", "supply", "sustenance", "budget", "grill", "surplus")
        out = Image.new("RGBA", (3 * len(kinds) * cell + pad, len(names) * cell + pad), (40, 44, 50, 255))
        for r, name in enumerate(names):
            for k, kind in enumerate(kinds):
                st = rendered.get((kind, name))
                if st is None:
                    continue
                off = st["off"][0]
                lit = Image.alpha_composite(off, st["normal-unshaded"][0])
                brk = Image.alpha_composite(st["broken"][0], st["panel"][0])
                for j, im in enumerate((off, lit, brk)):
                    out.paste(im.resize((S * z, S * z), Image.NEAREST),
                              (pad + (k * 3 + j) * cell, pad + r * cell))
        out.save(PREVIEW)
        print("preview:", PREVIEW)

        # Shipyard screens: neutral original first, then each faction; first frame of each direction row.
        src = Image.open(os.path.join(SCREEN_SRC, "screen.png")).convert("RGBA")
        sheets = [src] + list(screens.values())
        strip = Image.new("RGBA", (len(sheets) * (S * z + pad) + pad, 2 * (S * z + pad) + pad), (40, 44, 50, 255))
        for i, sh in enumerate(sheets):
            for row in range(2):  # south and east sit on rows 0 and 2 of the 6-wide sheet
                fr = sh.crop((0, row * 2 * S, S, row * 2 * S + S)).resize((S * z, S * z), Image.NEAREST)
                strip.paste(fr, (pad + i * (S * z + pad), pad + row * (S * z + pad)), fr)
        screens_preview = os.path.splitext(PREVIEW)[0] + "_screens.png"
        strip.save(screens_preview)
        print("preview:", screens_preview)


if __name__ == "__main__":
    main()
