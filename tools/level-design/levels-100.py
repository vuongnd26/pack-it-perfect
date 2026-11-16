#!/usr/bin/env python3
"""Builds levels 10-100 of Pack It Perfect with a gentle, measured difficulty curve, and removes every timer.

Levels 1-9 (the teaching levels) are kept as they are, except that level 5 loses its timer. Every new level is
packed from the existing illustrated objects in Assets/Resources/Art/item_catalog.json, so no new art is needed.

For each new level the script builds many seeded candidates, measures each with difficulty.py (solution count and a
simulated casual player) and keeps the one closest to the target for that slot. The ceiling is level 9's measured
difficulty: the old levels 10-20, where the simulated player got stuck in 43-99 % of runs, are replaced.

Usage (from the repository root):   python3 tools/level-design/levels-100.py
Writes: Assets/Data/Levels/levels.json, the "bindings" array of Assets/Resources/Art/item_catalog.json (objects are
unchanged) and tools/level-design/difficulty-100.json (measurements for all 100 levels).
"""
import json
import math
import random
import sys
from multiprocessing import Pool
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import difficulty  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
LEVELS = ROOT / "Assets/Data/Levels/levels.json"
CATALOG = ROOT / "Assets/Resources/Art/item_catalog.json"
REPORT = ROOT / "tools/level-design/difficulty-100.json"
KEEP = 9
TOTAL = 100
CANDIDATES = 32
SELECT_RUNS = 120
LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
STEPS = ((1, 0), (-1, 0), (0, 1), (0, -1))

# ------------------------------------------------------------------------------------------------ objects
# What each illustrated object is, so a fridge never gets a toolbox and only plausible things carry a rule.
TAGS = {
    "sock_long": "clothes", "sock_short": "clothes", "sock_sport": "clothes", "sock_travel": "clothes",
    "sock_folded": "clothes", "sock_scrunched": "clothes", "sock_t": "clothes", "sock_straight": "clothes",
    "scarf_s": "clothes", "scarf_w": "clothes", "towel_p": "clothes toiletry", "hoodie_p": "clothes",
    "pillow_v": "clothes travel", "cap": "clothes", "trainer": "clothes", "sneaker": "clothes",
    "hiking_boot": "clothes travel", "slipper_folded": "clothes", "slipper_straight": "clothes",
    "perfume": "toiletry", "perfume_upright": "toiletry", "atomizer_short": "toiletry", "atomizer_f": "toiletry",
    "shampoo_pump": "toiletry", "shampoo_w": "toiletry", "pump_long": "toiletry", "pump_t": "toiletry",
    "bottle_tall": "toiletry travel", "toothpaste": "toiletry", "toothbrush": "toiletry", "makeup": "toiletry",
    "bath_brush": "toiletry", "hand_mirror": "toiletry home",
    "sandwich": "food", "croissant": "food", "prawn": "food", "broccoli": "food", "sausage": "food",
    "baguette_v": "food", "baguette": "food", "carrot": "food", "pepper": "food", "cheese_u": "food",
    "leek": "food", "potato_sack": "food", "sushi_roll": "food", "biscuit": "food travel", "fried_egg": "food",
    "lunchbox": "food", "jar_t": "food home", "milk_carton": "food", "thermos": "food travel",
    "wine_glass": "home", "vase": "home", "snow_globe": "home toy", "tea_set": "home", "teapot": "home",
    "desk_lamp": "home", "oven_mitt": "home", "watering_can": "home tool",
    "plush": "toy", "plush_long": "toy", "toy_dinosaur": "toy", "telescope": "toy travel", "toy_drill": "toy tool",
    "headphones": "travel", "umbrella": "travel", "book": "travel home", "book_stack": "home",
    "powerbank": "travel", "camp_lantern": "travel", "snorkel": "travel toy", "poster_tube": "home",
    "shaker": "travel", "dumbbell": "tool", "wrench_z": "tool", "toolbox": "tool", "iron_l": "tool home",
}
HEAVY = {"toolbox", "dumbbell", "book_stack", "hiking_boot", "iron_l", "potato_sack", "watering_can"}
FRAGILE = {"wine_glass", "vase", "teapot", "snow_globe", "tea_set", "hand_mirror", "jar_t", "desk_lamp"}
UPRIGHT = {"perfume_upright", "shampoo_pump", "pump_long", "pump_t", "bottle_tall", "milk_carton", "atomizer_short"}
PAIRABLE = {"sock_short", "sock_sport", "sock_long", "trainer", "sneaker", "slipper_straight", "slipper_folded",
            "sock_straight", "sock_folded"}
# Legacy item id per tag: it picks the colour of the cell base under the illustration.
ITEMS = {
    "clothes": ["shirt", "sock", "sneaker", "hat", "umbrella"], "toiletry": ["perfume", "toothbrush", "milk"],
    "food": ["onigiri", "egg", "broccoli", "sushi", "carrot", "apple", "cheese"],
    "home": ["book", "camera", "teddy", "ball"], "toy": ["teddy", "ball", "sunglasses"],
    "travel": ["camera", "backpack", "sunglasses", "umbrella"], "tool": ["backpack", "camera", "hat"],
}

# ------------------------------------------------------------------------------------------------ containers
# (name, tags, theme, can have zones). Colours follow the existing levels' palette.
CONTAINERS = [
    ("suitcase", "clothes toiletry travel", ("#2F4D73", "#22395A", "#F4E7C5", "#A0783C33", True, True), True),
    ("backpack", "clothes travel food", ("#4E7D4A", "#365A33", "#EEF3E3", "#466E3C2E", True, False), False),
    ("beach bag", "clothes toiletry travel toy", ("#2FA59E", "#1F7F79", "#F7E8C4", "#A0783C2E", True, False), False),
    ("bento box", "food", ("#B8453A", "#8E2F27", "#3B2A24", "#FFDCB41F", False, False), False),
    ("fridge shelf", "food", ("#9CC3E4", "#6E9CC4", "#F1F8FC", "#5082AA33", False, False), False),
    ("car trunk", "travel toy clothes tool", ("#3A3F4A", "#262A31", "#5A6170", "#FFFFFF14", False, False), False),
    ("gym bag", "clothes toiletry travel", ("#E5484D", "#B23338", "#EEF1F5", "#505A6E29", True, False), False),
    ("picnic basket", "food home", ("#B98A4E", "#8F6634", "#F7EBD3", "#96642833", True, False), False),
    ("drawer", "clothes", ("#8A5A3C", "#64402A", "#F3E6D2", "#7850282E", False, False), False),
    ("moving box", "home toy tool travel", ("#C9A066", "#9C773F", "#F5E9D2", "#9664282E", False, False), False),
    ("grocery bag", "food", ("#C98A4B", "#9C6630", "#FBF1E2", "#9664282E", True, False), False),
    ("bathroom cabinet", "toiletry", ("#7FB7C9", "#5A93A6", "#F2FAFC", "#3C82962E", False, False), False),
    ("gift hamper", "food home", ("#E59A6F", "#B86F48", "#FFF6EA", "#B4703C24", True, False), False),
    ("toy chest", "toy travel home", ("#9B7BE0", "#7457BF", "#F4EEFF", "#6E50B429", True, False), False),
    ("travel trunk", "clothes toiletry travel home", ("#3F7FBF", "#2A5C91", "#F0F7FF", "#285A9624", True, True), True),
    ("carry-on", "clothes toiletry travel", ("#E8799B", "#B94E70", "#FFF3F6", "#B43C6424", True, True), True),
    ("camping pack", "travel food tool", ("#6FB7A8", "#44897B", "#F3FBF6", "#3C8C7824", True, False), False),
    ("crate", "home tool toy", ("#8E7CC3", "#6452A0", "#F6F2FF", "#5A46A024", False, False), False),
]
PREFIX = {
    "suitcase": ["Weekend", "Business", "City break", "Ski trip", "Island", "Family", "Summer", "Honeymoon",
                 "Spring break", "Winter", "Safari", "Reunion"],
    "backpack": ["School", "Day hike", "Festival", "Commuter", "Explorer", "Weekend", "Museum", "Field trip",
                 "Trekking", "Library", "Hostel", "Sightseeing"],
    "beach bag": ["Seaside", "Sunny", "Lagoon", "Surf day", "Poolside", "Coral", "Boardwalk", "Sandcastle",
                  "Tide pool", "Island hop", "Palm", "Sunset"],
    "bento box": ["Office", "Picnic", "Sakura", "School", "Train ride", "Rainy day", "Festival", "Lunch break",
                  "Field day", "Teatime", "Harvest", "Weekend"],
    "fridge shelf": ["Sunday", "Party", "Midnight", "Family", "Fresh", "Holiday", "Breakfast", "Barbecue",
                     "Dinner party", "Summer", "Leftovers", "Brunch"],
    "car trunk": ["Road trip", "Camping", "Airport run", "Holiday", "Weekend", "Beach run", "Big shop",
                  "Countryside", "Moving day", "Ski lodge", "Lake", "Grandma's"],
    "gym bag": ["Morning", "Yoga", "Team", "Swim class", "Marathon", "Tennis", "Dance class", "Climbing",
                "Cycling", "Pilates", "Boxing", "Weekend"],
    "picnic basket": ["Park", "Meadow", "Lakeside", "Spring", "Sunset", "Orchard", "Riverside", "Garden",
                      "Hilltop", "Cherry blossom", "Vineyard", "Autumn"],
    "drawer": ["Sock", "Winter", "Tidy", "Laundry day", "Cosy", "Summer", "Bedroom", "Guest room", "Spare",
               "Cedar", "Top", "Bottom"],
    "moving box": ["Kitchen", "New flat", "Attic", "Study", "Garage", "Bedroom", "Nursery", "Dorm", "Studio",
                   "Cottage", "Loft", "Office"],
    "grocery bag": ["Market", "Weekly", "Farm shop", "Bakery", "Corner shop", "Organic", "Night market",
                    "Deli", "Saturday", "Greengrocer", "Supermarket", "Village"],
    "bathroom cabinet": ["Hotel", "Spa", "Guest", "Morning", "Travel", "Seaside", "Cottage", "Family",
                         "Mirror", "Bedtime", "Downstairs", "Studio"],
    "gift hamper": ["Holiday", "Birthday", "Thank-you", "Housewarming", "Festive", "Anniversary", "Get-well",
                    "Welcome", "Teacher's", "Wedding", "Baby", "New year"],
    "toy chest": ["Playroom", "Rainy day", "Nursery", "Treasure", "Bedtime", "Pirate", "Dino", "Attic",
                  "Sunday", "Toybox", "Rocket", "Fairy"],
    "travel trunk": ["Grand voyage", "World tour", "Cruise", "Steamer", "Orient", "Safari", "Expedition",
                     "Railway", "Explorer's", "Vintage", "Captain's", "Odyssey"],
    "carry-on": ["Red-eye", "Jet-set", "Overnight", "Cabin", "First class", "Layover", "Business class",
                 "Weekend", "Window seat", "Stopover", "Boarding", "Short-haul"],
    "camping pack": ["Forest", "Mountain", "Riverside", "Starry night", "Trail", "Canyon", "Glacier", "Pine",
                     "Campfire", "Summit", "Valley", "Lakeshore"],
    "crate": ["Antique", "Workshop", "Flea market", "Barn", "Harbour", "Orchard", "Cellar", "Garden shed",
              "Dockside", "Farmhouse", "Vintage", "Pottery"],
}

ZONES = [
    {"key": "a", "icon": "🎒", "name": "Carry-on", "color": "#F2C14E47"},
    {"key": "b", "icon": "🧳", "name": "Checked", "color": "#4DA3E638"},
]
CHIP_LONG = {"heavy": "⚓ Heavy at the bottom", "upright": "⬆️ Bottles stay upright",
             "fragile": "✨ Fragile away from heavy", "pair": "🔗 Pairs touch", "outline": "👀 Match the outlines"}
CHIP_SHORT = {"heavy": "⚓ Heavy", "upright": "⬆️ Upright", "fragile": "✨ Fragile", "pair": "🔗 Pairs",
              "outline": "👀 Outlines"}
INTRO = {14: ("heavy", "New: heavy items", "⚓ Heavy things go on the bottom row"),
         22: ("upright", "New: upright bottles", "⬆️ Bottles can't be turned"),
         32: ("fragile", "New: fragile items", "✨ Keep fragile things away from heavy ones"),
         42: ("zone", "New: two sides", "🎒 Carry-on on one side, 🧳 checked on the other")}


# ------------------------------------------------------------------------------------------------ geometry
def cells_of(rows):
    return [(x, y) for y, row in enumerate(rows) for x, ch in enumerate(row) if ch == "X"]


def normalise(cells):
    mx, my = min(c[0] for c in cells), min(c[1] for c in cells)
    return tuple(sorted((x - mx, y - my) for x, y in cells))


def art_orientations(rows, upright):
    """Footprints the illustration can show: 4 turns x mirror; upright art keeps turn 0 (it may be mirrored)."""
    found = []
    for mirrored in (False, True):
        cells = [(-x if mirrored else x, y) for x, y in cells_of(rows)]
        for _ in range(1 if upright else 4):
            shape = normalise(cells)
            if shape not in found:
                found.append(shape)
            cells = [(-y, x) for x, y in cells]
    return found


def transform_for(art_rows, solution):
    """Same search as transformFor in tools/asset-pipeline/item-art-build.js."""
    target = normalise(solution)
    for mirrored in (False, True):
        cells = normalise([(-x if mirrored else x, y) for x, y in cells_of(art_rows)])
        for turns in range(4):
            if normalise(cells) == target:
                return {"solution_turns": turns, "mirrored": mirrored}
            cells = normalise([(-y, x) for x, y in cells])
    raise ValueError("art does not match its footprint")


# ------------------------------------------------------------------------------------------------ schedule
def schedule(n):
    """Size, piece count, rule budget and target difficulty for level n (10..100)."""
    t = (n - 10) / (TOTAL - 10)
    breather = n % 10 in (1, 6)
    challenge = n % 10 == 0
    if n <= 25:
        sizes, pieces, budget = [(5, 4), (5, 5), (6, 4)], (5, 7), 1
    elif n <= 50:
        sizes, pieces, budget = [(5, 5), (6, 4), (6, 5)], (6, 8), 1 if n < 36 else 2
    elif n <= 80:
        sizes, pieces, budget = [(6, 5), (6, 6), (7, 5)], (7, 9), 2
    else:
        sizes, pieces, budget = [(6, 6), (7, 5), (7, 6)], (8, 10), 3
    if challenge and n >= 80:
        sizes, pieces = [(7, 5), (7, 6)], (9, 10)
    if breather:
        small = (6, 5) if n > 80 else sizes[0]
        sizes, pieces, budget = [small], (pieces[0], pieces[0] + 1), min(budget, 1)
    target = 3.5 + 8.0 * t ** 0.85  # moves per piece for the simulated player; level 9 measures about 14
    if breather or n in INTRO:
        target *= 0.65
    elif challenge:
        target = min(target * 1.15, 13.0)
    stuck_cap = 0.12 if challenge else 0.08
    return dict(sizes=sizes, pieces=pieces, budget=budget, target=target, stuck_cap=stuck_cap,
                breather=breather, challenge=challenge)


def rules_known(n):
    known = ["pair"]
    if n >= 14:
        known.append("heavy")
    if n >= 22:
        known.append("upright")
    if n >= 32:
        known.append("fragile")
    if n >= 42:
        known.append("zone")
    return known


# ------------------------------------------------------------------------------------------------ candidates
def pool_for(container, objects):
    tags = set(container[1].split())
    return [o for o in objects if tags & set(TAGS[o["id"]].split())]


def pick_objects(rng, pool, area, count, required, pair_look=None):
    """Required objects plus random others from pool so that the footprints add up to exactly `area`."""
    need = area - sum(len(cells_of(o["cells"])) for o in required)
    free = count - len(required)
    if free < 1:
        return None
    weights = [4 if o.get("sourced_sprite") else 1 for o in pool]
    by_area = {}
    for o in pool:
        by_area.setdefault(len(cells_of(o["cells"])), []).append(o)
    for _ in range(300):
        chosen = rng.choices(pool, weights, k=free - 1) if free > 1 else []
        rest = need - sum(len(cells_of(o["cells"])) for o in chosen)
        if rest not in by_area:
            continue
        last = rng.choice(by_area[rest])
        picked = required + chosen + [last]
        # Two pieces that look the same must be a rule pair; otherwise players hunt for a pair that isn't there.
        # Several objects share one 3D picture (all socks use the same one), so compare pictures, not ids.
        looks = [o.get("sourced_sprite") or o["id"] for o in picked]
        if pair_look is not None:
            looks.remove(pair_look)
        if len(set(looks)) != len(looks):
            continue
        return picked
    return None


def tile(width, height, blocked, pieces, rng, node_cap=60000):
    """Seeded search for a packing that obeys every rule (pieces: dicts with art rows and flags)."""
    options = [art_orientations(p["art"]["cells"], p["upright"]) for p in pieces]
    grid, placed = {}, {}
    nodes = [0]

    def legal(i, cells):
        p = pieces[i]
        if p["heavy"] and not any(y == height - 1 for _, y in cells):
            return False
        touching = {grid[(x + dx, y + dy)] for x, y in cells for dx, dy in STEPS if (x + dx, y + dy) in grid}
        for o in touching:
            q = pieces[o]
            if (p["fragile"] and q["heavy"] and not q["fragile"]) or (p["heavy"] and not p["fragile"] and q["fragile"]):
                return False
        if p["pair"] >= 0:
            mate = next(j for j, q in enumerate(pieces) if j != i and q["pair"] == p["pair"])
            if mate in placed and mate not in touching:
                return False
        return True

    def search():
        nodes[0] += 1
        if nodes[0] > node_cap:
            return False
        empty = next(((x, y) for y in range(height) for x in range(width)
                      if (x, y) not in blocked and (x, y) not in grid), None)
        if empty is None:
            return True
        order = [i for i in range(len(pieces)) if i not in placed]
        rng.shuffle(order)
        for i in order:
            shapes = options[i][:]
            rng.shuffle(shapes)
            for shape in shapes:
                first = min(shape, key=lambda c: (c[1], c[0]))
                cells = [(empty[0] + x - first[0], empty[1] + y - first[1]) for x, y in shape]
                if any(not (0 <= x < width and 0 <= y < height) or (x, y) in blocked or (x, y) in grid
                       for x, y in cells):
                    continue
                if not legal(i, cells):
                    continue
                for c in cells:
                    grid[c] = i
                placed[i] = cells
                if search():
                    return True
                for c in cells:
                    del grid[c]
                del placed[i]
        return False

    return (grid, placed) if search() else None


def candidate(args):
    """One seeded attempt at level n. Returns (level, art ids, measurement) or None."""
    n, seed, objects = args
    rng = random.Random(n * 100003 + seed)
    spec = schedule(n)
    known = rules_known(n)
    rules = []
    if n in INTRO:
        rules += ["heavy", "fragile"] if INTRO[n][0] == "fragile" else [INTRO[n][0]]
    budget = max(spec["budget"], len(rules))
    pool_rules = [r for r in known if r not in rules]
    rng.shuffle(pool_rules)
    while len(rules) < budget and pool_rules and rng.random() < (0.85 if not rules else 0.55):
        r = pool_rules.pop()
        if r == "fragile" and "heavy" not in rules:
            if len(rules) + 2 > budget:
                continue
            rules.append("heavy")
        rules.append(r)
    if spec["breather"] and n not in INTRO:
        rules = rules[:1] if rng.random() < 0.5 else []
    container = None
    for _ in range(40):
        container = rng.choice(CONTAINERS)
        pool = pool_for(container, objects)
        ids = {o["id"] for o in pool}
        ok = ("heavy" not in rules or ids & HEAVY) and ("fragile" not in rules or ids & FRAGILE) and \
             ("upright" not in rules or ids & UPRIGHT) and ("pair" not in rules or ids & PAIRABLE) and \
             ("zone" not in rules or container[3])
        if ok:
            break
    else:
        return None
    by_id = {o["id"]: o for o in objects}
    pool = pool_for(container, objects)
    ids = {o["id"] for o in pool}
    width, height = rng.choice(spec["sizes"])
    blocked = set()
    if n >= 26 and n % 7 == 3 and not spec["breather"]:
        corners = [(0, 0), (width - 1, 0)]
        blocked = set(rng.sample(corners, rng.randint(1, 2)))
    area = width * height - len(blocked)
    count = rng.randint(*spec["pieces"])
    count = max(count, math.ceil(area / 4.6))
    count = min(count, math.floor(area / 2.6))
    required, flags = [], []
    if "heavy" in rules:
        for oid in rng.sample(sorted(ids & HEAVY), min(len(ids & HEAVY), rng.randint(1, 2))):
            required.append(by_id[oid])
            flags.append({"heavy": True})
    if "fragile" in rules:
        for oid in rng.sample(sorted(ids & FRAGILE), min(len(ids & FRAGILE), rng.randint(1, 2))):
            required.append(by_id[oid])
            flags.append({"fragile": True})
    if "upright" in rules:
        for oid in rng.sample(sorted(ids & UPRIGHT), min(len(ids & UPRIGHT), rng.randint(1, 2))):
            required.append(by_id[oid])
            flags.append({"upright": True})
    if "pair" in rules:
        oid = rng.choice(sorted(ids & PAIRABLE))
        required += [by_id[oid], by_id[oid]]
        flags += [{"pair": 0}, {"pair": 0}]
    # Ordinary filler never uses rule objects, so a heavy-looking toolbox is always a heavy toolbox.
    filler = [o for o in pool if o["id"] not in HEAVY | FRAGILE | UPRIGHT]
    pair_look = None
    if "pair" in rules:
        pair_look = required[-1].get("sourced_sprite") or required[-1]["id"]
    picked = pick_objects(rng, filler, area, count, required, pair_look)
    if not picked:
        return None
    pieces = []
    for k, o in enumerate(picked):
        f = flags[k] if k < len(flags) else {}
        pieces.append({"art": o, "heavy": f.get("heavy", False), "fragile": f.get("fragile", False),
                       "upright": f.get("upright", False), "pair": f.get("pair", -1)})
    rng.shuffle(pieces)
    result = tile(width, height, blocked, pieces, rng)
    if not result:
        return None
    grid, placed = result
    rows = ["".join("#" if (x, y) in blocked else LETTERS[grid[(x, y)]] for x in range(width))
            for y in range(height)]
    zone_of, zone_map = {}, []
    if "zone" in rules:
        for i, cells in placed.items():
            zone_of[i] = "a" if sum(x for x, _ in cells) / len(cells) < width / 2 else "b"
        if len(set(zone_of.values())) < 2:
            return None
        zone_map = ["".join("#" if (x, y) in blocked else zone_of[grid[(x, y)]] for x in range(width))
                    for y in range(height)]
    outline = set()
    if not rules and not spec["breather"] and n >= 30 and rng.random() < 0.3:
        outline = set(rng.sample(range(len(pieces)), 2))
        rules.append("outline")
    used_items = []
    out_pieces = []
    for i, p in enumerate(pieces):
        tag = TAGS[p["art"]["id"]].split()[0]
        choices = [c for c in ITEMS[tag] if c not in used_items] or ITEMS[tag]
        item = rng.choice(choices)
        used_items.append(item)
        out_pieces.append({
            "letter": LETTERS[i], "item": item, "startTurns": 0 if p["upright"] else rng.randint(0, 3),
            "zone": zone_of.get(i, ""), "pair": p["pair"], "heavy": p["heavy"], "upright": p["upright"],
            "outline": i in outline, "fragile": p["fragile"],
        })
    shown = [r for r in ("heavy", "fragile", "upright", "pair", "outline") if r in rules]
    chips = ["🎒 Carry-on", "🧳 Checked"] if "zone" in rules else []
    chips += [(CHIP_LONG if len(shown) + len(chips) <= 1 else CHIP_SHORT)[r] for r in shown]
    frame, rim, lining, line, handle, wheels = container[2]
    level = {"number": n, "name": container[0], "map": rows, "timerSeconds": 0,
             "bannerTitle": INTRO[n][1] if n in INTRO else "", "bannerSubtitle": INTRO[n][2] if n in INTRO else "",
             "ruleChips": chips, "zoneMap": zone_map, "zones": ZONES if zone_map else [], "pieces": out_pieces,
             "theme": {"frame": frame, "rim": rim, "lining": lining, "line": line, "block": "#1E1E2859",
                       "handle": handle, "wheels": wheels}}
    m = difficulty.measure(level, runs=SELECT_RUNS)
    return level, [p["art"]["id"] for p in pieces], m, seed


def pick(n, results, recent, relax=1.0):
    """Best candidate: closest to the target difficulty, mostly 3D art, a rule when the slot has room for one, and
    not the same kind of container as the last few levels."""
    spec = schedule(n)
    best = None
    for r in results:
        if not r:
            continue
        level, arts, m, seed = r
        cells = sum(ch != "#" for row in level["map"] for ch in row)
        if m["stuck"] > spec["stuck_cap"] * relax or m["solutions"] < (6 if cells >= 30 else 3):
            continue
        sourced = sum(1 for a in arts if SOURCED[a]) / len(arts)
        score = abs(m["ratio"] - spec["target"]) / spec["target"]
        score += 0 if sourced >= 0.8 else (0.8 - sourced)
        if not level["ruleChips"] and not spec["breather"] and n >= 14:
            score += 0.25
        if level["name"] in recent:
            score += 0.3
        if best is None or (score, seed) < best[:2]:
            best = (score, seed, r)
    return best


SOURCED = {}


def main():
    raw = LEVELS.read_text(encoding="utf-8")
    data = json.loads(raw)
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    objects = catalog["objects"]
    missing = [o["id"] for o in objects if o["id"] not in TAGS]
    if missing:
        sys.exit(f"Untagged catalog objects: {missing}")
    for o in objects:
        SOURCED[o["id"]] = bool(o.get("sourced_sprite"))
    kept = [lv for lv in data["levels"] if lv["number"] <= KEEP]
    for lv in kept:
        lv["timerSeconds"] = 0
    new_levels, bindings = [], [b for b in catalog["bindings"] if b["level"] <= KEEP]
    names_used = {lv["name"] for lv in kept}
    by_id = {o["id"]: o for o in objects}
    report = [difficulty.measure(lv) for lv in kept]
    recent = []
    with Pool() as pool:
        for n in range(KEEP + 1, TOTAL + 1):
            best = None
            for round_ in range(24):
                seeds = range(round_ * CANDIDATES, (round_ + 1) * CANDIDATES)
                results = pool.map(candidate, [(n, s, objects) for s in seeds])
                # After 16 rounds without a fit, accept a little more getting stuck rather than give up.
                found = pick(n, results, recent, 1.0 if round_ < 16 else 1.4)
                if found and (best is None or found[:2] < best[:2]):
                    best = found
                if best and best[0] <= 0.15:
                    break
            if not best:
                sys.exit(f"Level {n}: no candidate within the difficulty limits.")
            level, arts, _, seed = best[2]
            recent = (recent + [level["name"]])[-3:]
            base = level["name"]
            prefixes = PREFIX[base]
            name = next((f"{p} {base}" for p in prefixes[(n // 7) % len(prefixes):] + prefixes
                         if f"{p} {base}" not in names_used), None)
            if not name:
                sys.exit(f"Level {n}: ran out of names for {base}; add prefixes.")
            name = name[0].upper() + name[1:]
            names_used.add(name)
            level["name"] = name
            for piece, art in zip(level["pieces"], arts):
                cells = [(x, y) for y, row in enumerate(level["map"]) for x, ch in enumerate(row)
                         if ch == piece["letter"]]
                bindings.append({"level": n, "piece": piece["letter"], "art_id": art,
                                 **transform_for(by_id[art]["cells"], cells)})
            new_levels.append(level)
            m = difficulty.measure(level)
            report.append(m)
            print(f'{n:3} {m["size"]:>4} pcs {m["pieces"]:2} rules {m["rules"]} sol {m["solutions"]:5} '
                  f'ratio {m["ratio"]:5} (target {schedule(n)["target"]:.1f}) stuck {m["stuck"]:.2f}  {name}',
                  flush=True)
    data["levels"] = kept + new_levels
    if any(lv["timerSeconds"] for lv in data["levels"]):
        sys.exit("Refusing to write: a level still has a timer.")
    LEVELS.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    catalog["bindings"] = bindings
    CATALOG.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    REPORT.write_text(json.dumps(report, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote {len(data['levels'])} levels and {len(bindings)} bindings.")


if __name__ == "__main__":
    main()
