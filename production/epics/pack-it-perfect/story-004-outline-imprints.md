# Story 004: Outline imprints

> **Epic**: pack-it-perfect
> **Status**: Complete
> **Layer**: Feature
> **Type**: Visual/Feel
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-12

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 4

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Cơ chế chính, Khuôn gợi ý), scoped to this story:*

- [ ] On levels 6, 8 and 9, a faded shape plus the item icon is drawn at the correct position of a few items.
- [ ] Levels 10 and 15 have no outlines (the brief says so explicitly), and every other level has none.
- [ ] Level 6 shows the banner, verbatim: `"New: outlines — Faded shapes show where items go"`.
- [ ] Which items get an outline comes from the level's reference solution (story 002), not from code.
- [ ] An outline disappears or dims once its item is placed correctly.

---

## Implementation Notes

- Decided 2026-11-12: an outline only advises. A valid fill elsewhere still wins.
- Outline tint and opacity are tuning values in config.

---

## Out of Scope

- Story 002: picking the outlined items per level.
- Story 005: the Hint glow.

---

## Files

- Assets/Scripts/Packing/OutlineView.cs
- Assets/Scripts/UI/BannerView.cs

---

## QA Test Cases

N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above.

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: a retained screenshot of levels 6 and 10 in `production/qa/evidence/` + sign-off in `production/qa/evidence/story-004-outline-imprints-evidence.md`.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/pack-it-perfect/story-001-core-packing-loop.md`, `production/epics/pack-it-perfect/story-002-level-data.md`
- Unlocks: None
