#!/usr/bin/env python3
"""Builds the Cozy Travel chapter (levels 16-20) and appends it to Assets/Data/Levels/levels.json.

Each level is a fixed list of illustrated objects with rule flags. A seeded search tiles the grid with them so that the
reference solution obeys every rule the game enforces:
  heavy   -> touches the bottom row            upright -> stays in its authored orientation (may be mirrored)
  fragile -> shares no cell edge with a heavy  pair    -> the two pieces share a cell edge
Levels 1-15 are never rewritten (the script re-serialises the file byte-identically and only replaces levels above 15).

Usage: python3 tools/level-design/cozy-levels.py            (from the repository root)
Outputs: levels.json (levels 16-20) and tools/level-design/cozy-assignments.json (art id per piece, level order).
"""
import json
import random
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LEVELS = ROOT / "Assets/Data/Levels/levels.json"
ASSIGNMENTS = ROOT / "tools/level-design/cozy-assignments.json"
FIRST_NEW_LEVEL = 16
BLOCKED = "#"

# Authored footprints (top row first). Must match the art catalog (tools/asset-pipeline/item-art-build.js).
SHAPES = {
    "wine_glass": ["X", "X"], "vase": ["X", "X", "X"], "teapot": ["XX", "XX"], "snow_globe": ["XX", "XX"],
    "tea_set": ["X.", "XX"], "hand_mirror": ["XXX", ".X."], "dumbbell": ["XXX"], "book_stack": ["XX", "XX"],
    "hiking_boot": ["X.", "X.", "XX"], "toolbox": ["XXX", "XXX"], "iron_l": ["XX", "X."],
    "scarf_s": ["XX.", ".XX"], "sock_long": ["X.", "X.", "XX"], "cap": ["XXX", ".X."], "toothpaste": ["XXX"],
    "sock_short": ["X.", "XX"], "towel_p": ["XXX", ".XX"], "book": ["XX"], "biscuit": ["XX"],
    "pump_long": ["XX", ".X", ".X"], "perfume_upright": [".X", "XX"], "trainer": ["X.", "XX"],
    "hoodie_p": ["XXX", ".XX"], "shampoo_pump": ["XX", "X.", "X."], "atomizer_short": ["XX", "X."],
    "slipper_straight": ["XXX"], "headphones": ["X.X", "XXX"], "umbrella": ["XXX", ".X."],
    "toothbrush": ["X", "X", "X", "X"], "camera_case": ["XX", "XX"],
}

# Legacy item id per object (colour/pattern fallback when an illustration is missing).
ITEM = {
    "wine_glass": "perfume", "vase": "perfume", "teapot": "perfume", "snow_globe": "ball", "tea_set": "perfume",
    "hand_mirror": "sunglasses", "dumbbell": "cheese", "book_stack": "book", "hiking_boot": "sneaker",
    "toolbox": "backpack", "iron_l": "camera", "scarf_s": "shirt", "sock_long": "sock", "cap": "hat",
    "toothpaste": "toothbrush", "sock_short": "sock", "towel_p": "shirt", "book": "book", "biscuit": "onigiri",
    "pump_long": "milk", "perfume_upright": "perfume", "trainer": "sneaker", "hoodie_p": "shirt",
    "shampoo_pump": "milk", "atomizer_short": "perfume", "slipper_straight": "sneaker", "headphones": "sunglasses",
    "umbrella": "umbrella", "toothbrush": "toothbrush", "camera_case": "camera",
}

H, F, U = "heavy", "fragile", "upright"


def piece(art, *flags, pair=-1):
    return {"art": art, "flags": set(flags), "pair": pair}


# Timer rationale: level 15 gives 75 s for 13 items on 7x7 (about 5.8 s per item). The timed levels here keep
# 5.5-6.2 s per item: one more rule to read (fragile), but the player holds Freeze by now.
SPECS = [
    dict(number=16, name="Gift hamper", size=(5, 5), timer=0, seed=5,
         banner=("New: fragile items", "✨ Keep fragile things away from heavy ones"),
         chips=["✨ Fragile away from heavy", "⚓ Heavy at the bottom"],
         theme=dict(frame="#E59A6F", rim="#B86F48", lining="#FFF6EA", line="#B4703C24", handle=True, wheels=False),
         pieces=[piece("book_stack", H), piece("teapot", F), piece("wine_glass", F), piece("scarf_s"),
                 piece("sock_long"), piece("cap"), piece("toothpaste")]),
    dict(number=17, name="Housewarming crate", size=(6, 5), timer=0, seed=3,
         banner=("Handle with care", "Fragile, heavy and a pair"),
         chips=["✨ Fragile away from heavy", "⚓ Heavy bottom", "🔗 Pairs touch"],
         theme=dict(frame="#6FB7A8", rim="#44897B", lining="#F3FBF6", line="#3C8C7824", handle=False, wheels=False),
         pieces=[piece("dumbbell", H), piece("hiking_boot", H), piece("vase", F), piece("tea_set", F),
                 piece("snow_globe", F), piece("sock_short", pair=0), piece("sock_short", pair=0),
                 piece("towel_p"), piece("book")]),
    dict(number=18, name="Honeymoon suitcase", size=(6, 6), timer=60, seed=11,
         banner=("Hard level!", "Pack before the taxi leaves"),
         chips=["✨ Fragile", "⚓ Heavy", "⬆️ Upright", "🔗 Pairs"],
         theme=dict(frame="#E8799B", rim="#B94E70", lining="#FFF3F6", line="#B43C6424", handle=True, wheels=True),
         pieces=[piece("toolbox", H), piece("iron_l", H), piece("hand_mirror", F), piece("wine_glass", F),
                 piece("teapot", F), piece("pump_long", U), piece("perfume_upright", U),
                 piece("trainer", pair=0), piece("trainer", pair=0), piece("book"), piece("biscuit")]),
    dict(number=19, name="Antique market box", size=(7, 6), timer=0, seed=7, blocked=[(0, 0), (6, 0)],
         banner=("Tight squeeze", "Blocked corners, fragile and heavy"),
         chips=["✨ Fragile away from heavy", "⚓ Heavy bottom", "🔗 Pairs touch"],
         theme=dict(frame="#8E7CC3", rim="#6452A0", lining="#F6F2FF", line="#5A46A024", handle=False, wheels=False),
         pieces=[piece("book_stack", H), piece("dumbbell", H), piece("vase", F), piece("snow_globe", F),
                 piece("tea_set", F), piece("sock_long", pair=0), piece("sock_long", pair=0), piece("hoodie_p"),
                 piece("sock_short"), piece("toothpaste"), piece("cap")]),
    dict(number=20, name="Grand voyage trunk", size=(7, 7), timer=80, seed=2, zones=True,
         banner=("Hard level!", "Pack before the taxi leaves"),
         chips=["🎒 Carry-on", "🧳 Checked", "✨ Fragile", "⚓ Heavy", "⬆️ Upright", "🔗 Pairs"],
         theme=dict(frame="#3F7FBF", rim="#2A5C91", lining="#F0F7FF", line="#285A9624", handle=True, wheels=True),
         pieces=[piece("toolbox", H), piece("hiking_boot", H), piece("book_stack", H), piece("hand_mirror", F),
                 piece("teapot", F), piece("wine_glass", F), piece("vase", F), piece("shampoo_pump", U),
                 piece("atomizer_short", U), piece("slipper_straight", pair=0), piece("slipper_straight", pair=0),
                 piece("headphones"), piece("scarf_s")]),
]

ZONES = [
    {"key": "a", "icon": "🎒", "name": "Carry-on", "color": "#F2C14E47"},
    {"key": "b", "icon": "🧳", "name": "Checked", "color": "#4DA3E638"},
]
STEPS = ((1, 0), (-1, 0), (0, 1), (0, -1))


def cells_of(rows):
    return [(x, y) for y, row in enumerate(rows) for x, ch in enumerate(row) if ch == "X"]


def normalise(cells):
    mx, my = min(c[0] for c in cells), min(c[1] for c in cells)
    return tuple(sorted((x - mx, y - my) for x, y in cells))


def orientations(rows, upright):
    """Unique footprints: 4 clockwise turns x mirror; upright keeps turn 0 (mirror allowed, as the art may be flipped)."""
    found = []
    for mirrored in (False, True):
        cells = [(-x if mirrored else x, y) for x, y in cells_of(rows)]
        for turn in range(1 if upright else 4):
            shape = normalise(cells)
            if shape not in found:
                found.append(shape)
            cells = [(-y, x) for x, y in cells]
    return found


def solve(spec):
    width, height = spec["size"]
    blocked = set(spec.get("blocked", []))
    pieces = spec["pieces"]
    area = sum(len(cells_of(SHAPES[p["art"]])) for p in pieces)
    if area != width * height - len(blocked):
        sys.exit(f"Level {spec['number']}: pieces cover {area} cells, grid needs {width * height - len(blocked)}.")
    rng = random.Random(spec["seed"])
    options = [orientations(SHAPES[p["art"]], U in p["flags"]) for p in pieces]
    grid = {}
    placed = {}

    def neighbours(index):
        return {grid[(x + dx, y + dy)] for x, y in placed[index] for dx, dy in STEPS
                if (x + dx, y + dy) in grid and grid[(x + dx, y + dy)] != index}

    def legal(index, cells):
        p = pieces[index]
        if H in p["flags"] and not any(y == height - 1 for _, y in cells):
            return False
        touching = {grid[(x + dx, y + dy)] for x, y in cells for dx, dy in STEPS if (x + dx, y + dy) in grid}
        for other in touching:
            o = pieces[other]
            if (F in p["flags"] and H in o["flags"]) or (H in p["flags"] and F in o["flags"]):
                return False
        if p["pair"] >= 0:
            partner = next(i for i, q in enumerate(pieces) if i != index and q["pair"] == p["pair"])
            if partner in placed and partner not in touching:
                return False
        return True

    def search():
        empty = next(((x, y) for y in range(height) for x in range(width)
                      if (x, y) not in blocked and (x, y) not in grid), None)
        if empty is None:
            return True
        order = [i for i in range(len(pieces)) if i not in placed]
        rng.shuffle(order)
        for index in order:
            shapes = options[index][:]
            rng.shuffle(shapes)
            for shape in shapes:
                first = min(shape, key=lambda c: (c[1], c[0]))
                cells = [(empty[0] + x - first[0], empty[1] + y - first[1]) for x, y in shape]
                if any(not (0 <= x < width and 0 <= y < height) or (x, y) in blocked or (x, y) in grid for x, y in cells):
                    continue
                if not legal(index, cells):
                    continue
                for c in cells:
                    grid[c] = index
                placed[index] = cells
                if search():
                    return True
                for c in cells:
                    del grid[c]
                del placed[index]
        return False

    if not search():
        sys.exit(f"Level {spec['number']}: no packing found for seed {spec['seed']}.")
    return grid, placed, rng


def build(spec):
    width, height = spec["size"]
    grid, placed, rng = solve(spec)
    letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    rows = ["".join(BLOCKED if (x, y) not in grid else letters[grid[(x, y)]] for x in range(width))
            for y in range(height)]
    zone_of = {}
    zone_map, zones = None, None
    if spec.get("zones"):
        # Every piece belongs to one side: left of the middle by centroid is Carry-on, the rest Checked.
        for index, cells in placed.items():
            zone_of[index] = "a" if sum(x for x, _ in cells) / len(cells) < width / 2 else "b"
        zone_map = ["".join(zone_of[grid[(x, y)]] for x in range(width)) for y in range(height)]
        zones = ZONES
    pieces = []
    for index, p in enumerate(spec["pieces"]):
        pieces.append({
            "letter": letters[index], "item": ITEM[p["art"]],
            "startTurns": 0 if U in p["flags"] else rng.randint(0, 3),
            "zone": zone_of.get(index, ""), "pair": p["pair"], "heavy": H in p["flags"],
            "upright": U in p["flags"], "outline": False, "fragile": F in p["flags"],
        })
    level = {"number": spec["number"], "name": spec["name"], "map": rows, "timerSeconds": spec["timer"],
             "bannerTitle": spec["banner"][0], "bannerSubtitle": spec["banner"][1], "ruleChips": spec["chips"]}
    level["zoneMap"] = zone_map or []
    level["zones"] = zones or []
    level["pieces"] = pieces
    level["theme"] = dict(spec["theme"], block="#1E1E2859")
    level["theme"] = {k: level["theme"][k] for k in ("frame", "rim", "lining", "line", "block", "handle", "wheels")}
    return level, [p["art"] for p in spec["pieces"]]


def main():
    raw = LEVELS.read_text(encoding="utf-8")
    data = json.loads(raw)
    original = [lv for lv in data["levels"] if lv["number"] < FIRST_NEW_LEVEL]
    check = dict(data, levels=original)
    built = [build(spec) for spec in SPECS]
    data["levels"] = original + [level for level, _ in built]
    text = json.dumps(data, indent=2, ensure_ascii=False) + "\n"
    if not text.startswith((json.dumps(check, indent=2, ensure_ascii=False))[: raw.index('"number": 15')]):
        sys.exit("Refusing to write: levels 1-15 would change.")
    LEVELS.write_text(text, encoding="utf-8")
    ASSIGNMENTS.write_text(json.dumps({str(level["number"]): arts for level, arts in built}, indent=2) + "\n",
                           encoding="utf-8")
    for level, _ in built:
        print(level["number"], level["name"], *level["map"], sep="\n  ")


if __name__ == "__main__":
    main()
