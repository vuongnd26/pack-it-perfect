# Story 002: 15-level data

> **Epic**: pack-it-perfect
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Config/Data
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 2

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Obstacles, Cơ chế chính, Progression & Meta), scoped to this story:*

- [ ] 15 level assets exist, in order, loadable by level number.
- [ ] Every level's grid is within 4x3 to 7x7 and has 3 to 13 items.
- [ ] The level schema carries a flag or value for each rule. Values match the brief exactly (blank = none):

  | Level | Timer | Blocked | Outlines | Zones | Pairs | Heavy | Upright |
  |---|---|---|---|---|---|---|---|
  | 1 | | | | | | | |
  | 2 | | | | | | | |
  | 3 | | yes | | | | | |
  | 4 | | | | | | | |
  | 5 | 30s | | | | | | |
  | 6 | | | yes | | | | |
  | 7 | | | | Food/Stuff | | | |
  | 8 | | | yes | | yes | | |
  | 9 | | yes | yes | Kitchen/Toys | yes | | |
  | 10 | 45s | | NO | Clothes/Toiletries | yes | | |
  | 11 | | | | | | | |
  | 12 | | | | | | yes | |
  | 13 | | yes | | | | | yes |
  | 14 | | | | | yes | yes | yes |
  | 15 | 75s | | NO | Carry-on/Checked | yes | yes | yes |
- [ ] Level 1 items all start in the correct orientation. From level 2, several items start wrongly oriented.
- [ ] Every level carries a reference solution (item placements). Hint (005) and outlines (004) read it.
- [ ] Each level has a container theme id and frame colour field (art is filled in by story 009).
- [ ] The per-level grid size, item list/shapes, blocked cells and outlined items come from `Assets/Data/Levels/levels.json` (extracted from the prototype 2026-11-12 and already checked against the table above). A loader check rejects a level whose solution breaks its own rules.

---

## Implementation Notes

- Extends `LevelData` from story 001. All gameplay values live in assets, never in code (coding standards).
- Keep rule flags per level, not "level >= N" checks in code.
- Source of truth for flags is the Obstacles and Cơ chế chính tables in the brief.

---

## Out of Scope

- Story 003, 004, 005, 006: the behaviour of each rule.
- Story 009: container art.

---

## Files

- Assets/Scripts/Packing/LevelData.cs
- Assets/Data/Levels/Level01.asset … Level15.asset

---

## QA Test Cases

N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above.

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check pass (`production/qa/smoke-*.md`), with the flag table above checked level by level.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/pack-it-perfect/story-001-core-packing-loop.md`
- Unlocks: story-003, story-004, story-005, story-006, story-007
