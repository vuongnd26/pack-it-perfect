#!/usr/bin/env python3
"""Difficulty measurement for Pack It Perfect levels (observations only, no verdicts).

For a level dict in the levels.json schema it reports:
  solutions  - distinct legal full packings reachable by rotation only (the player cannot mirror), capped
  moves      - median place/remove actions a simulated casual player needs to finish (200 seeded runs)
  ratio      - moves / pieces (1.0 means every item went straight to a correct spot)
  stuck      - share of runs that hit the move cap without finishing

Rules mirror Assets/Scripts/Packing/Rules: heavy touches the bottom row; fragile never shares an edge with a
heavy-only piece; pairs must touch when the board is full; upright pieces never rotate; zoned pieces stay inside
their zone; blocked cells ('#') take nothing.

Usage: python3 tools/level-design/difficulty.py [first last]   (measures Assets/Data/Levels/levels.json)
"""
import json
import math
import random
import statistics
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LEVELS = ROOT / "Assets/Data/Levels/levels.json"
RUNS = 200
MOVE_CAP = 400
SOLUTION_CAP = 5000
NODE_CAP = 400000


def normalise(cells):
    mx, my = min(c[0] for c in cells), min(c[1] for c in cells)
    return tuple(sorted((x - mx, y - my) for x, y in cells))


def rotations(cells, upright):
    found, cur = [], list(cells)
    for _ in range(1 if upright else 4):
        shape = normalise(cur)
        if shape not in found:
            found.append(shape)
        cur = [(-y, x) for x, y in cur]  # same direction as PieceShape.Rotated
    return found


class Board:
    """Bitmask view of a level: every legal-in-isolation placement of every piece."""

    def __init__(self, level):
        rows = level["map"]
        self.h, self.w = len(rows), len(rows[0])
        w, h = self.w, self.h
        self.blocked = 0
        cells_by_letter = {}
        for y, row in enumerate(rows):
            for x, ch in enumerate(row):
                if ch == "#":
                    self.blocked |= 1 << (y * w + x)
                else:
                    cells_by_letter.setdefault(ch, []).append((x, y))
        self.full = ((1 << (w * h)) - 1) & ~self.blocked
        zone_map = level.get("zoneMap") or []
        pieces = level["pieces"]
        self.n = len(pieces)
        self.area = []
        self.heavy_only = [p["heavy"] and not p.get("fragile", False) for p in pieces]
        self.fragile = [p.get("fragile", False) for p in pieces]
        self.outline = [p.get("outline", False) for p in pieces]
        self.partner = [-1] * self.n
        for i, p in enumerate(pieces):
            if p["pair"] >= 0:
                self.partner[i] = next(j for j, q in enumerate(pieces) if j != i and q["pair"] == p["pair"])
        self.placements = []  # per piece: list of (mask, neighbour_mask, contact)
        self.solution = []
        for i, p in enumerate(pieces):
            sol = cells_by_letter[p["letter"]]
            self.area.append(len(sol))
            self.solution.append(sum(1 << (y * w + x) for x, y in sol))
            opts = []
            for shape in rotations(normalise(sol), p["upright"]):
                sw, sh = max(c[0] for c in shape) + 1, max(c[1] for c in shape) + 1
                for oy in range(h - sh + 1):
                    for ox in range(w - sw + 1):
                        cells = [(ox + x, oy + y) for x, y in shape]
                        mask = sum(1 << (y * w + x) for x, y in cells)
                        if mask & self.blocked:
                            continue
                        if p["heavy"] and not any(y == h - 1 for _, y in cells):
                            continue
                        if p["zone"] and any(zone_map[y][x] != p["zone"] for x, y in cells):
                            continue
                        nb, wall = 0, 0
                        for x, y in cells:
                            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                                nx, ny = x + dx, y + dy
                                if not (0 <= nx < w and 0 <= ny < h) or (self.blocked >> (ny * w + nx)) & 1:
                                    wall += 1
                                elif (nx, ny) not in cells:
                                    nb |= 1 << (ny * w + nx)
                        opts.append((mask, nb, wall))
            self.placements.append(opts)

    def legal(self, i, opt, occ, heavy_mask, fragile_mask):
        mask, nb, _ = opt
        if mask & occ:
            return False
        if self.fragile[i] and nb & heavy_mask:
            return False
        if self.heavy_only[i] and nb & fragile_mask:
            return False
        return True

    def pairs_ok(self, where):
        for i, j in enumerate(self.partner):
            if j >= 0 and not (self.placements[i][where[i]][1] & self.placements[j][where[j]][0]):
                return False
        return True

    # ---------------------------------------------------------------- exact count
    def count_solutions(self):
        n, order_area = self.n, self.area
        where = [None] * n
        state = {"count": 0, "nodes": 0}
        by_cell = {}
        for i in range(n):
            for k, opt in enumerate(self.placements[i]):
                low = opt[0] & -opt[0]
                by_cell.setdefault(low, []).append((i, k))

        def masks():
            hm = fm = 0
            for i in range(n):
                if where[i] is not None:
                    m = self.placements[i][where[i]][0]
                    if self.heavy_only[i]:
                        hm |= m
                    if self.fragile[i]:
                        fm |= m
            return hm, fm

        def rec(occ):
            if state["count"] >= SOLUTION_CAP or state["nodes"] >= NODE_CAP:
                return
            state["nodes"] += 1
            free = self.full & ~occ
            if not free:
                if self.pairs_ok(where):
                    state["count"] += 1
                return
            low = free & -free
            hm, fm = masks()
            for i, k in by_cell.get(low, ()):
                if where[i] is not None:
                    continue
                opt = self.placements[i][k]
                if not self.legal(i, opt, occ, hm, fm):
                    continue
                where[i] = k
                rec(occ | opt[0])
                where[i] = None

        rec(0)
        return state["count"], state["nodes"] >= NODE_CAP

    # ---------------------------------------------------------------- casual player
    def holes_ok(self, occ, remaining_min):
        free = self.full & ~occ
        w = self.w
        left_edge = sum(1 << (y * w) for y in range(self.h))
        right_edge = sum(1 << (y * w + w - 1) for y in range(self.h))
        while free:
            comp = free & -free
            while True:
                grow = comp | ((comp << 1) & ~left_edge) | ((comp >> 1) & ~right_edge) | (comp << w) | (comp >> w)
                grow &= free
                if grow == comp:
                    break
                comp = grow
            if bin(comp).count("1") < remaining_min:
                return False
            free &= ~comp
        return True

    def play(self, rng):
        n = self.n
        where = [None] * n
        history = []
        tabu = {}  # a piece just taken out is not put straight back into the same spot
        occ = hm = fm = 0
        moves = 0

        def take(i):
            nonlocal occ, hm, fm
            m = self.placements[i][where[i]][0]
            occ &= ~m
            hm &= ~m
            fm &= ~m
            tabu[i] = where[i]
            where[i] = None
            history.remove(i)

        while moves < MOVE_CAP:
            unplaced = [i for i in range(n) if where[i] is None]
            victims = []
            if not unplaced:
                if self.pairs_ok(where):
                    return moves, True
                bad = [i for i in range(n) if self.partner[i] >= 0 and not (
                    self.placements[i][where[i]][1] & self.placements[self.partner[i]][where[self.partner[i]]][0])]
                victims = [rng.choice(bad)]
            else:
                options = {}
                dead = not self.holes_ok(occ, min(self.area[i] for i in unplaced))
                if not dead:
                    for i in unplaced:
                        ok = [k for k, o in enumerate(self.placements[i]) if self.legal(i, o, occ, hm, fm)]
                        if not ok:
                            dead = True
                            break
                        options[i] = ok
                if dead:
                    victims = [history[-1] if rng.random() < 0.6 else rng.choice(history)]
            if victims:
                # Taking out one item rarely helps on its own: a player usually lifts a neighbour too.
                if len(history) > 1 and rng.random() < 0.5:
                    victims.append(rng.choice([h for h in history if h != victims[0]]))
                for v in victims:
                    take(v)
                    moves += 1
                continue
            # Bigger items first, then the snuggest spot (touching walls and other items), with some randomness.
            weights = [self.area[i] ** 2 for i in unplaced]
            i = rng.choices(unplaced, weights)[0]
            ok = options[i]
            if i in tabu and len(ok) > 1:
                ok = [k for k in ok if k != tabu[i]] or ok
            tabu.pop(i, None)
            mate = self.partner[i]
            if mate >= 0 and where[mate] is not None:
                # A player puts the second shoe next to the first one when there is room for it.
                beside = [k for k in ok if self.placements[i][k][1] & self.placements[mate][where[mate]][0]]
                ok = beside or ok
            sol = next((k for k in ok if self.placements[i][k][0] == self.solution[i]), None)
            if self.outline[i] and sol is not None:
                k = sol
            else:
                scores = [self.placements[i][k][2] + bin(self.placements[i][k][1] & occ).count("1") for k in ok]
                top = max(scores)
                k = rng.choices(ok, [math.exp(1.3 * (s - top)) for s in scores])[0]
            m = self.placements[i][k][0]
            where[i] = k
            occ |= m
            if self.heavy_only[i]:
                hm |= m
            if self.fragile[i]:
                fm |= m
            history.append(i)
            moves += 1
        return moves, False


def measure(level, runs=RUNS):
    board = Board(level)
    solutions, capped = board.count_solutions()
    rng = random.Random(level["number"] * 7919)
    results = [board.play(rng) for _ in range(runs)]
    moves = statistics.median(m for m, _ in results)
    stuck = sum(1 for _, done in results if not done) / runs
    rules = sum(1 for key in ("heavy", "upright", "fragile") if any(p.get(key) for p in level["pieces"]))
    rules += any(p["pair"] >= 0 for p in level["pieces"]) + any(p["zone"] for p in level["pieces"])
    return {
        "level": level["number"], "name": level["name"], "size": f'{board.w}x{board.h}', "pieces": board.n,
        "rules": rules, "timer": level.get("timerSeconds", 0), "solutions": solutions,
        "solutions_capped": capped or solutions >= SOLUTION_CAP, "moves": moves,
        "ratio": round(moves / board.n, 2), "stuck": round(stuck, 3),
    }


def main():
    levels = json.loads(LEVELS.read_text(encoding="utf-8"))["levels"]
    first, last = (int(a) for a in sys.argv[1:3]) if len(sys.argv) >= 3 else (1, len(levels))
    for level in levels[first - 1:last]:
        r = measure(level)
        print(f'{r["level"]:3} {r["size"]:>4} pcs {r["pieces"]:2} rules {r["rules"]} timer {r["timer"]:2} '
              f'sol {r["solutions"]:5}{"+" if r["solutions_capped"] else " "} moves {r["moves"]:5} '
              f'ratio {r["ratio"]:5} stuck {r["stuck"]:.2f}  {r["name"]}')


if __name__ == "__main__":
    main()
