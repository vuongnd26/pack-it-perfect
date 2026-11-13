# Story 008: Tutorial hand and placement juice

> **Epic**: pack-it-perfect
> **Status**: In Progress
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-12

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 8

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Cơ chế chính, Progression & Meta, Art & Audio), scoped to this story:*

- [x] Level 1 shows a tutorial hand that drags an item from the tray into the grid.
- [ ] Every correct placement plays a bounce, a light burst and a light haptic.
- [x] Every correct placement shows one praise word picked at random from this closed list only: `"Snug!"`, `"Nice fit!"`, `"Click!"`, `"Perfect!"`, `"Neat!"`.
- [x] Completing a level shows confetti.
- [ ] A wrong drop shakes the item and gives haptic feedback if the device supports it.
- [ ] No praise word outside the list appears anywhere.

---

## Implementation Notes

- The praise list and the effect strengths live in config. The list is closed: do not add words.
- Make the tutorial hand skippable by finishing the first placement, and show it only on level 1.
- Haptics: check the platform call at implementation time. Do not assume an API from memory (see the engine reference).

---

## Out of Scope

- Story 004 and 006: the outline and timer banners.
- Story 009: final sprites and sound files.

---

## Files

- Assets/Scripts/Feedback/GameFeel.cs
- Assets/Scripts/Feedback/FeelTuning.cs
- Assets/Scripts/Packing/Feedback/WorldFeedback.cs
- Assets/Scripts/Feedback/TutorialHand.cs
- Assets/Scripts/Feedback/ConfettiBurst.cs

---

## QA Test Cases

N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above.

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: a retained screenshot of the tutorial hand, a praise word and the confetti, in `production/qa/evidence/` + sign-off in `production/qa/evidence/story-008-tutorial-and-juice-evidence.md`.

**Status**: Screenshots retained; final feel sign-off and Android haptics remain pending. Reduced-motion preference suppresses decorative movement.

- `production/qa/evidence/story-008-tutorial-and-juice/level01-tutorial.png`
- `production/qa/evidence/story-008-tutorial-and-juice/placement-praise.png`
- `production/qa/evidence/story-008-tutorial-and-juice/confetti-completion.png`
- Evidence scope: `production/qa/evidence/story-008-tutorial-and-juice-evidence.md`.

---

## Dependencies

- Depends on: `production/epics/pack-it-perfect/story-001-core-packing-loop.md`
- Unlocks: None
