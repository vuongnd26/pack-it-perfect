# Story 007: Progression and meta screens

> **Epic**: pack-it-perfect
> **Status**: Ready
> **Layer**: Feature
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 7

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Progression & Meta), scoped to this story:*

- [ ] The 15 levels unlock in order: level N+1 opens after level N is won.
- [ ] A level-select screen lists all 15 levels with a lock state and the best stars earned on each, and any unlocked level can be replayed.
- [ ] Each level keeps its best star result (2 or 3). A worse replay never lowers it.
- [ ] The main screen shows the total stars (sum of the best stars) and the number of levels passed.
- [ ] Progress and stars survive closing and reopening the app.
- [ ] After a win, the player can go to the next level or back to level select.

---

## Implementation Notes

- Put saving behind an interface (for example `ISaveStore`) with a JSON file under the persistent data path, so it stays testable. Do not call storage from UI code.
- "Levels passed" means levels won at least once.
- The UI toolkit choice (UI Toolkit or UGUI) is left to `/dev-story` with the `unity-ui-specialist`. Record it here once chosen.

---

## Out of Scope

- Story 005: how the star result is decided.
- Story 009: final screen art.

---

## Files

- Assets/Scripts/Meta/ProgressData.cs
- Assets/Scripts/Meta/ISaveStore.cs
- Assets/Scripts/Meta/JsonSaveStore.cs
- Assets/Scripts/UI/MainScreen.cs
- Assets/Scripts/UI/LevelSelectScreen.cs
- Assets/Scenes/Main.unity

---

## QA Test Cases

N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above.

---

## Test Evidence

**Story Type**: UI
**Required evidence**: a retained screenshot of the main screen and the level-select screen, in `production/qa/evidence/`.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/pack-it-perfect/story-001-core-packing-loop.md`, `production/epics/pack-it-perfect/story-005-hint-and-stars.md`
- Unlocks: None
