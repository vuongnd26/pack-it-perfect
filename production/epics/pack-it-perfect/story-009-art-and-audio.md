# Story 009: Art and audio pass

> **Epic**: pack-it-perfect
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 9

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Art & Audio, USP), scoped to this story:*

- [ ] Flat 2D cartoon look with thick dark outlines, and the Fredoka rounded font on all text.
- [ ] Every item has its own colour, one pattern (stripes, dots, plaid, knit, diagonal stripes) and an object icon, so no two items in a level look alike.
- [ ] Each level changes the container frame colour. The suitcase has a handle and wheels.
- [ ] Each of the 15 levels has its own container setting, as named in `levels.json` (Weekend suitcase, Beach bag, Bento box … World tour suitcase), with the theme colours and handle/wheels flags from the same file.
- [ ] Item icons follow the prototype's drawings (https://claude.ai/artifact/PskwEekJTZ85nAVkZywy4j).
- [ ] Sound effects exist for: pick up, place, rotate, wrong place, zipper, and the 5-second tick.
- [ ] Victory music plays when a level is completed.
- [ ] Audio and art replace the placeholders from stories 001, 006 and 008.

---

## Implementation Notes

- Check the Fredoka font licence before shipping and record the result.
- Keep patterns, colours and per-level themes in data (story 002 theme and frame-colour fields).
- Asset sourcing (made, bought or generated) is the user's call. Ask before adding any.

---

## Out of Scope

- Story 008: effect logic, haptics, confetti behaviour.
- Monetization and store assets (not in the GDD).

---

## Files

- Assets/Art/Items/ (sprites, patterns, icons)
- Assets/Art/Containers/
- Assets/Fonts/Fredoka
- Assets/Audio/Sfx/
- Assets/Audio/Music/

---

## QA Test Cases

N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above.

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: retained screenshots of at least 3 levels with different containers, in `production/qa/evidence/` + sign-off in `production/qa/evidence/story-009-art-and-audio-evidence.md`.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/pack-it-perfect/story-001-core-packing-loop.md`
- Unlocks: None
