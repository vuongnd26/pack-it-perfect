# Story 005: Hint booster and star rule

> **Epic**: pack-it-perfect
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 5

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Booster, Điều kiện thắng), scoped to this story:*

- [ ] A button labelled `"💡 Hint"` is on the game screen. There is no limit on how often it is used.
- [ ] Pressing it rotates one unplaced item to its correct orientation.
- [ ] Items currently occupying that item's target cells go back to the tray. The player still drags the item in themselves.
- [ ] The target cells glow for about 2 seconds and the item in the tray is highlighted.
- [ ] Winning without using Hint on that level gives 3 stars. Using Hint at least once gives at most 2 stars.
- [ ] The Hint-used flag belongs to the current play of the level. Replaying a level starts it unflagged.
- [ ] The result (2 or 3 stars) is passed to the progression system (story 007).

---

## Implementation Notes

- The target cells come from the level's reference solution (story 002).
- Decided 2026-11-12: Hint picks the first unplaced piece in data order. An upright piece already starts upright, so Hint only clears its target cells and lights them.
- The glow duration (about 2 s) is a tuning value in config.

---

## Out of Scope

- Story 007: saving and showing stars.
- Story 004: the permanent outlines.

---

## Files

- Assets/Scripts/Packing/HintService.cs
- Assets/Scripts/Packing/StarCalculator.cs
- Assets/Scripts/UI/HintButton.cs

---

## QA Test Cases

N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above.

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Test: waived at qa.level minimal. Launch and observe Hint once with an occupied target cell; keep a screenshot in `production/qa/evidence/`.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/pack-it-perfect/story-001-core-packing-loop.md`, `production/epics/pack-it-perfect/story-002-level-data.md`, `production/epics/pack-it-perfect/story-003-obstacle-rules.md`
- Unlocks: `production/epics/pack-it-perfect/story-007-progression-meta.md`
