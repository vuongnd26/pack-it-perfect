# Story 006: Timer mode and lose screen

> **Epic**: pack-it-perfect
> **Status**: Ready
> **Layer**: Feature
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 6

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Điều kiện thua, Hẹn giờ), scoped to this story:*

- [ ] A countdown shows only on levels 5, 10 and 15: 5 = 30 s, 10 = 45 s, 15 = 75 s.
- [ ] Those levels show the banner, verbatim: `"Hard level! Pack before the taxi leaves"`.
- [ ] The clock turns red with 10 seconds left. A tick sound plays with 5 seconds left.
- [ ] Winning stops the clock.
- [ ] When time runs out the player loses. The lose screen reads, verbatim: `"Out of time! The taxi is leaving and the trunk won't close."`
- [ ] The lose screen has two buttons: `"Try again"` and `"Install to keep packing"`.
- [ ] `"Try again"` restarts the same level with a full clock.
- [ ] The timer is always on for levels 5, 10 and 15, with no mode switch. Every other level has no clock and no lose state.

---

## Implementation Notes

- Decided 2026-11-12:
  - The timer is mandatory on levels 5/10/15. There is no relax/timer mode switch.
  - `"Install to keep packing"` keeps the GDD text and opens no link (game-jam build). Raise an event so a link can be attached later.
- Timer levels show only the "Hard level!" banner, not the level's own `bannerTitle`.
- Timer lengths and the 10 s / 5 s thresholds are tuning values in config, not literals.
- The tick sound asset comes from story 009. Wire a placeholder here.

---

## Out of Scope

- Story 002: which levels have a timer (data).
- Story 009: final clock art and tick sound.

---

## Files

- Assets/Scripts/Packing/LevelTimer.cs
- Assets/Scripts/UI/TimerView.cs
- Assets/Scripts/UI/LoseScreen.cs

---

## QA Test Cases

N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above.

---

## Test Evidence

**Story Type**: UI
**Required evidence**: a retained screenshot of the timer in its red state and of the lose screen, in `production/qa/evidence/`.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/pack-it-perfect/story-001-core-packing-loop.md`, `production/epics/pack-it-perfect/story-002-level-data.md`
- Unlocks: None
