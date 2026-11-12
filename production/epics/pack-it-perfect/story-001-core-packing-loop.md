# Story 001: Core packing loop

> **Epic**: pack-it-perfect
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-12

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 1

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Core loop, Điều khiển, Điều kiện thắng), scoped to this story:*

- [ ] A grid (width, height, blocked cells) and a tray of multi-cell items are built from a level data asset.
- [ ] Dragging an item from the tray shows a preview shadow: green if it can be placed (in bounds, no overlap, not on a blocked cell), red if not.
- [ ] Dropping on a valid spot snaps the item to the cells. Dropping on an invalid spot returns the item to the tray and shakes it.
- [ ] Tapping (not dragging) an item in the tray rotates it 90° per tap.
- [ ] Tapping a placed item returns it to the tray. Dragging a placed item moves it. No limit on how often.
- [ ] A fill bar shows the percentage of fillable cells that are covered.
- [ ] The level is won when every item is placed and every active level rule holds (none beyond placement in this story). The lid then closes with a zipper animation.
- [ ] Board logic (placement, overlap, rotation, fill) is plain C# with no MonoBehaviour dependency, so it can be unit-tested later.

---

## Implementation Notes

- Split a pure board model (cells, items, rotation, validity) from the views and input.
- Level data is external config: `Assets/Data/Levels/levels.json` (all 15 levels, already extracted). Each level `map` is a letter grid. Each letter is one piece, its position is the reference solution, and `#` is a blocked cell. A piece starts in the tray turned `startTurns` × 90° from its solution shape. This story reads only `map`, `pieces[].letter/item/startTurns`, `items[]` and `theme`. Story 002 adds the loader checks and rule fields.
- The prototype (https://claude.ai/artifact/PskwEekJTZ85nAVkZywy4j) is the behaviour reference for drag, rotate, preview and snap.
- Use the Input System package for pointer input, not the legacy Input Manager (deprecated per `docs/engine-reference/unity/`).
- Win text and praise words are not part of this story (see 008).

---

## Out of Scope

- Story 002: the 15 levels and the rule flags.
- Story 003: blocked-cell rules beyond "cannot place", zones, pairs, heavy, upright.
- Story 005: Hint and stars.
- Story 008: bounce, praise words, confetti, haptic.
- Story 009: final art and audio.

---

## Files

Assets/Scripts/Packing/BoardModel.cs
Assets/Scripts/Packing/GridView.cs
Assets/Scripts/Packing/LevelData.cs
Assets/Scripts/Packing/LevelLoader.cs
Assets/Scripts/Packing/LidView.cs
Assets/Scripts/Packing/PackingGame.cs
Assets/Scripts/Packing/PackingTuning.cs
Assets/Scripts/Packing/PieceShape.cs
Assets/Scripts/Packing/PieceView.cs
Assets/Scripts/Packing/SpriteFactory.cs
Assets/Scripts/Packing/TrayView.cs

## QA Test Cases

N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above.

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Test: waived at qa.level minimal. The change is still launched and observed once; keep a screenshot in `production/qa/evidence/` (see `.claude/docs/run-and-observe.md`).

**Status**: [x] Screenshot retained: production/qa/evidence/story-001-core-packing-loop/level01-start.png (tests waived at qa.level minimal). Played by user 2026-11-12: OK.

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/pack-it-perfect/story-002-level-data.md`, story-003, story-004, story-005, story-006, story-007, story-008, story-009
