# Story 009: Art and audio pass

> **Epic**: pack-it-perfect
> **Status**: In Progress
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-12

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

- Assets/Scripts/Packing/PatternSpriteCache.cs (runtime patterned bodies and borders)
- Assets/Scripts/Packing/{PieceView,GridView,LidView}.cs
- Assets/Scripts/Audio/{GameAudio,AudioTuning}.cs (original procedural SFX and victory phrase)
- Assets/Scripts/Feedback/GameFeel.cs (event routing)
- Assets/Resources/Fonts/Fredoka SDF.asset
- Assets/Fonts/OFL-Fredoka.txt
- Assets/Art/EmojiSource/OFL-NotoEmoji.txt

---

## QA Test Cases

N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above.

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: retained screenshots of at least 3 levels with different containers, in `production/qa/evidence/` + sign-off in `production/qa/evidence/story-009-art-and-audio-evidence.md`.

**Status**: Art observed in Play Mode; sound generation/routing checked, but listening, fidelity approval and final lead sign-off remain pending.

- `production/qa/evidence/story-009-art-and-audio/level07-basket.png`
- `production/qa/evidence/story-009-art-and-audio/level13-cabinet.png`
- `production/qa/evidence/story-009-art-and-audio/level15-suitcase.png`
- Additional portrait samples: `level15-480x800.png`, `level15-1440x2960.png` in the same folder.
- Evidence and licence notes: `production/qa/evidence/story-009-art-and-audio-evidence.md`.

---

## Dependencies

- Depends on: `production/epics/pack-it-perfect/story-001-core-packing-loop.md`
- Unlocks: None
