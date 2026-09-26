"""Build the TFCF energy drink RSI using the existing Hullrot vendor drawing primitives.

The RSI keeps its legacy coffee_tfsc name: the TFCF machine fills the company "Coffee" product slot.
Run from the repository root: python Tools/generate_tfcf_coffee.py
Only coffee_tfsc.rsi and output/sprites/tfcf_coffee are written.
"""
import json
from pathlib import Path

from PIL import Image, ImageDraw
import generate_hullrot_vendors as v


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Resources/Textures/_Crescent/Structures/Machines/Vendors/coffee_tfsc.rsi"
PREVIEW = ROOT / "output/sprites/tfcf_coffee"
FAC = dict(v.FACTIONS["tfsc"], emblem="energy")
P = v.palette(FAC)
CREAM = (249, 216, 160)
VOLT = (160, 236, 96)
# A lightning bolt: energy, not espresso.
CAN = ["....##.", "...##..", "..##...", ".#####.", "...##..", "..##...", ".##...."]
v.EMBLEMS["energy"] = CAN
LETTERS = [
    ["###", ".#.", ".#.", ".#.", ".#."],
    ["###", "#..", "##.", "#..", "#.."],
    [".##", "#..", "#..", "#..", ".##"],
    ["###", "#..", "##.", "#..", "#.."],
]


def header(c, color):
    c.glyph(CAN, 6, 5, color)
    for x, letter in zip((14, 18, 22, 26), LETTERS):
        c.glyph(letter, x, 6, color)


def can(c, x, y, body_color):
    """A 3x5 can: metal top rim, red label with a volt stripe."""
    c.rect(x, y, x + 2, y, v.METAL)
    c.rect(x, y + 1, x + 2, y + 4, body_color)
    c.put(x + 1, y + 2, VOLT)


def body():
    c = v.draw_body("budget", FAC, P)
    # A worn red cap ties the machine to the Federation grill across the corridor.
    for y in range(1, 4):
        for x in range(4, 29):
            r, g, b, a = c.px[x, y]
            if a and (r, g, b) != P["out"]:
                c.put(x, y, P["acc_dk"] if y == 3 else P["acc"])
    # Replace the shop sign and stock window with an energy drink marquee and a chilled can rack.
    c.rect(4, 4, 28, 12, P["out"])
    c.rect(5, 5, 27, 11, v.PLATE)
    header(c, v.scale(CREAM, 0.36))
    c.rect(5, 13, 28, 13, P["acc"])
    c.rect(7, 14, 20, 26, (20, 17, 18))
    # Two shelves of cans behind the glass.
    for x in (9, 13, 17):
        can(c, x, 15, P["acc"])
    c.rect(8, 20, 19, 20, v.METAL_DK)
    # The pickup tray holds a freshly dropped can.
    can(c, 13, 21, P["acc"])
    c.rect(8, 26, 19, 26, v.METAL_DK)
    for x in range(8, 20, 2):
        c.put(x, 26, v.METAL)
    c.rect(7, 28, 20, 29, P["out"])
    for x in range(8, 20, 2):
        c.put(x, 28, P["hi"])
    return c.im


def lights(mode, frame):
    c = v.Canvas()
    header(c, CREAM)
    # Cold fridge glow over the can rack.
    c.rect(8, 14, 19, 14, VOLT)
    c.rect(8, 15, 19, 19, VOLT, 30)
    c.rect(9, 21, 18, 25, VOLT, 16)
    for x in (10, 14, 18):
        c.put(x, 17, VOLT)
    g = v.GEO["budget"]
    denied = mode == "deny-unshaded"
    color = v.DENY if denied else CREAM
    rows = v.DENY_X if denied else v.SCREEN_TEXT[frame % 4]
    v.paint_screen(c, g, v.scale(color, 0.25), color, rows)
    c.put(*g["led"], v.DENY if denied else v.LED_GO)
    if denied and frame % 2:
        c.rect(*g["screen"], v.scale(v.DENY, 0.25))
    if mode == "eject-unshaded":
        # The middle can drops from the rack into the tray, then frost puffs off it.
        if frame <= 3:
            can(c, 13, 15 + frame * 2, P["acc"])
        else:
            c.put(14, 23, VOLT)
            c.put(11 + frame % 2, 21, CREAM, 130)
            c.put(16 - frame % 2, 20, CREAM, 90)
    return c.im


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    off = body()
    broken = v.Canvas()
    broken.im = off.copy()
    broken.px = broken.im.load()
    for x, y in ((15, 6), (16, 7), (17, 8), (17, 9), (18, 10), (19, 10)):
        broken.put(x, y, v.METAL_DK)
        broken.put(x + 1, y, P["out"])
    broken.rect(24, 15, 27, 17, (9, 8, 8))
    broken.rect(13, 18, 15, 19, (35, 28, 26))
    broken.rect(12, 25, 17, 26, (44, 24, 15))
    states = {
        "off": [off],
        "broken": [broken.im],
        "panel": [v.draw_panel("budget", FAC, P)],
        "screen": [Image.new("RGBA", (32, 32))],
        "normal-unshaded": [lights("normal-unshaded", f) for f in range(4)],
        "deny-unshaded": [lights("deny-unshaded", f) for f in range(2)],
        "eject-unshaded": [lights("eject-unshaded", f) for f in range(6)],
    }
    metadata = []
    for name, frames in states.items():
        v.sheet(frames).save(OUT / (name + ".png"))
        state = {"name": name}
        if len(frames) > 1:
            delay = 0.5 if name == "normal-unshaded" else 0.2
            state["delays"] = [[delay] * len(frames)]
        metadata.append(state)
    (OUT / "meta.json").write_text(json.dumps({
        "version": 1,
        "license": "CC-BY-SA-3.0",
        "copyright": "TFCF energy drink layout based on Taleryn's Hullrot vendor drawing primitives.",
        "size": {"x": 32, "y": 32},
        "states": metadata,
    }, indent=2) + "\n", encoding="utf-8")

    lit = Image.alpha_composite(off, states["normal-unshaded"][0])
    lit.resize((512, 512), Image.Resampling.NEAREST).save(PREVIEW / "coffee.png")
    frames = [Image.alpha_composite(off, f).resize((256, 256), Image.Resampling.NEAREST)
              for f in states["eject-unshaded"]]
    frames[0].save(PREVIEW / "brewing.gif", save_all=True, append_images=frames[1:],
                   duration=200, loop=0, disposal=2)
    shi_path = ROOT / "Resources/Textures/_Crescent/Structures/shinohara_coffee.rsi"
    shi = Image.open(shi_path / "off.png").convert("RGBA").crop((0, 0, 32, 32))
    shi_light = Image.open(shi_path / "normal-unshaded.png").convert("RGBA").crop((0, 0, 32, 32))
    shi = Image.alpha_composite(shi, shi_light)
    preview = Image.new("RGBA", (960, 310), (35, 38, 44, 255))
    draw = ImageDraw.Draw(preview)
    entries = [("SHINOHARA", shi), ("TFCF / ON", lit), ("TFCF / OFF", off),
               ("TFCF / SERVICE", Image.alpha_composite(broken.im, states["panel"][0]))]
    for i, (label, frame) in enumerate(entries):
        draw.text((i * 240 + 24, 24), label, fill=CREAM)
        preview.alpha_composite(frame.resize((224, 224), Image.Resampling.NEAREST), (i * 240 + 8, 64))
    preview.save(PREVIEW / "comparison.png")
    print(f"Wrote {OUT} and {PREVIEW}")


if __name__ == "__main__":
    main()
