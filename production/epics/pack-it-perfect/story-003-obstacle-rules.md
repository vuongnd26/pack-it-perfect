# Story 003: Obstacle rules

> **Epic**: pack-it-perfect
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 3

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Obstacles table), scoped to this story. Use the texts verbatim, icons included:*

- [ ] **Blocked cells** (levels 3, 9, 13): items cannot be placed on them and they do not need to be filled.
- [ ] **Zones** (levels 7, 9, 10, 15): the container is split into 2 coloured, labelled zones. An item with a zone icon must lie entirely inside its zone. Violation text: `"[item] goes on the [zone] side"`.
- [ ] **Pairs** (levels 8, 9, 10, 14, 15): two items with a pair icon must sit next to each other. Checked once all items are placed. Pass: `"Pair! 💞"`. Fail: the container shakes and shows `"Pairs must touch – move the [item]s together"`.
- [ ] **Heavy** (levels 12, 14, 15): an item with the ⚓ icon must touch the bottom row. Violation text: `"⚓ Heavy items must touch the bottom"`.
- [ ] **Upright** (levels 13, 14, 15): an item with the ⬆️ icon cannot be rotated. Tapping it shakes it and shows `"⬆️ Bottles stay upright – no turning"`.
- [ ] A level is won only when every active rule holds, in addition to the full fill.
- [ ] Which rules are active comes from the level flags (story 002). No level numbers are hardcoded in rule code.

---

## Implementation Notes

- One rule class per obstacle behind a common interface, evaluated by the board model from story 001.
- Decided 2026-11-12 (brief § "Quyết định đã chốt"):
  - Heavy and zone: checked on drop. An invalid drop returns the item to the tray and shows the toast.
  - Pairs: touching means sharing an edge (corners do not count), checked once all items are placed. On a valid drop that touches its partner, show `"Pair! 💞"` instead of a praise word.
- The intro banners for zones, pairs, heavy and upright come from the level data (`bannerTitle`/`bannerSubtitle`, taken from the prototype).

---

## Out of Scope

- Story 002: which levels have which rule (data).
- Story 004: outlines.
- Story 005: how Hint treats upright items.

---

## Files

- Assets/Scripts/Packing/Rules/IPackingRule.cs
- Assets/Scripts/Packing/Rules/ZoneRule.cs
- Assets/Scripts/Packing/Rules/PairRule.cs
- Assets/Scripts/Packing/Rules/HeavyRule.cs
- Assets/Scripts/Packing/Rules/UprightRule.cs
- Assets/Scripts/Packing/BoardModel.cs

---

## QA Test Cases

N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above.

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Test: waived at qa.level minimal. Launch and observe at least levels 7, 8, 12 and 13 once; keep a screenshot in `production/qa/evidence/`.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/pack-it-perfect/story-001-core-packing-loop.md`, `production/epics/pack-it-perfect/story-002-level-data.md`
- Unlocks: story-005
