# Whole-object art and UI — Editor verification, 2026-11-13

## Result and scope

**Run result: OBSERVED — Unity 6000.6.8f1, SampleScene, Android-target Game View in Play Mode.** All 112 pieces across 15 levels now bind to 63 complete object illustrations. Eight illustrated UI assets are integrated into the HUD, timer, level select and result cards. Original local SVG drawings are rendered to PNG; no external image-generation provider or downloaded item artwork was used.

This record covers the current uncommitted art/presentation pass. It is not a release approval or creative sign-off. No APK, commit, push or story closure was performed for this pass. The previous gameplay commit remains `a893149`.

## Changes observed

- Whole-object illustrations replace patterned coloured pieces with small item icons. Examples include socks and trainers for bent pieces, a square makeup compact, a T-shaped open umbrella, toiletries, food, books and travel equipment. Several unusual footprints use folded or stylized object poses; recognizability still needs human art acceptance.
- Artwork orientation includes reflection, authored solution orientation and the existing start rotation. Tap rotation updates the picture explicitly, including for symmetric footprints. Hint resets the picture to solution orientation. Logical cell ghosts remain the precise placement aid, and rule badges remain upright.
- The presentation manifest is separate from `levels.json`. Feedback uses the illustrated object's name without mutating shared gameplay item definitions.
- Illustrated Home, Hint, Restart, clock, stars, locks, ribbon and nine-slice result cards are visible. Existing Fredoka font, environment art, soft piece shadows and dimensional buttons remain in use. Some rule chips and labels still contain emoji; this is not an all-glyph replacement.
- SampleScene's saved tray values match the source defaults: gap 0.5 cells, slot spacing 0.28 cells, minimum width 9 cells. This changes layout, not occupancy or rules.

## Checks executed

| Check | Observed result | Scope / limitation |
|---|---|---|
| Catalog coverage and files | Schema 1; 63 objects; 112 unique level/letter bindings; no missing referenced PNGs | Coverage does not prove object recognizability |
| Compiled solution geometry and rendered orientation | All 112 art bindings matched compiled solution shapes; 405 authored cell centers matched occupied cells after actual sprite transforms | Reference arrangements, not exhaustive alternate packings |
| Final controller regression with saved tray layout | 160 accepted rotations; every piece placed through `BeginPress` / `MovePress` / `EndPress`; 15/15 wins with three stars in an isolated save | Reflection-driven controller commands, not physical mouse/touch/gamepad; clocks stopped for deterministic solving |
| Earlier explicit rotation / Hint regression in this art pass | 105 visual rotations and 15/15 Hint resets passed, including symmetric pieces | Ran before the final UI/tray integration; not rerun afterward; Hint/art-transform logic was not changed afterward |
| Mandatory timers | Levels 5, 10, 15 started at 30, 45, 75 seconds | Does not prove human completion within those limits |
| Timeout flow | Actual `LevelTimer.Tick(100f)` caused the Lost state; illustrated timeout screen rendered | Accelerated clock, not a real-time waiting/device lifecycle check |
| Actual win presentation | Controller-solving level 1 reached the real win card after the lid sequence | Isolated QA progress; production save restored |
| Button interaction | Main Play center was found by EventSystem raycast; synthetic down scaled to 0.94; up and exit restored 1; hide/reopen, disabled and reduced-motion cases passed | Synthetic `ExecuteEvents` callbacks, not a physical finger or mouse |
| Fresh and existing level select | Fresh QA state: only level 1 unlocked; existing state: all 15 with three stars | Fresh state rendered using isolated progress, without replacing production save |
| UI sprite import | All eight load as Sprite/Single, 100 PPU, no mipmaps, non-readable, uncompressed, max 2048; card border 84 px on every side; other borders zero | Import settings checked in the running Editor |
| Portrait UI samples | Seven states each at native 480×800 and 1440×2960; contact sheet reviewed with no obvious text/button clipping | Available portrait preset extremes, not formally certified supported device bounds; no notch simulation |
| Initial level presentation | All 15 initial level screenshots at 720×1280 retained and contact sheet reviewed; levels 13 and 15 also inspected individually | Still images do not establish animation feel, audio quality or touch usability |
| Source/data integrity | `levels.json` byte-identical to HEAD; BoardModel, LevelLoader, PieceShape and LevelTimer unchanged in git diff | PackingGame has presentation binding/orientation/name calls; this is not a claim that no controller source changed |
| Static checks | `node --check` passed for all three art generators; `git diff --check` passed again after the documentation/checkpoint updates | Syntax and whitespace checks, not gameplay runs |
| Fresh Play console | Zero Error/Warning entries at the clean-session checks and after captures | Not an Android player build or a Unity Test Runner pass |

No automated unit test suite was run. No test directories were found at `Assets/Tests/` or root `tests/`; the test-resource root nodes alone were not treated as test coverage.

## Retained evidence

All paths below are relative to `production/qa/evidence/ui-polish/`.

### Current whole-object presentation

- [All 15 initial levels](evidence/ui-polish/17-review-sheet.jpg) — montage of `17-level-01-initial.png` through `17-level-15-initial.png`, each native 720×1280. Shows initial pieces, start rotations, empty boards, outlines, obstacles and zones.
- [Level 13, initial](evidence/ui-polish/17-level-13-initial.png) — bathroom objects, upright rule and obstacles.
- [Level 15, initial](evidence/ui-polish/17-level-15-initial.png) — 13 pieces, zones, rule badges and 75-second clock.
- [Level 13, partially packed](evidence/ui-polish/09-illustrated-hud-level13.png).
- [Level 14](evidence/ui-polish/10-whole-objects-level14.png).
- [Timed level 5](evidence/ui-polish/11-timed-hud-level05.png).

### Screens and results

- [Fresh level select](evidence/ui-polish/12-level-select-fresh.png).
- [Real controller win](evidence/ui-polish/13-illustrated-win.png).
- [Actual timer expiry](evidence/ui-polish/14-illustrated-timeout.png).
- [Main at 480×800](evidence/ui-polish/15-main-480x800.png).
- [Two-resolution screen review](evidence/ui-polish/16-review-sheet.jpg) — 14 native captures: Main, fresh select, existing-progress select, timed level 15, two-star win, timeout and final end card at each of 480×800 and 1440×2960.

The native resolution files use `16-480x800-` or `16-1440x2960-` followed by `main`, `levels-fresh`, `levels-progress`, `timed15`, `win-two-stars`, `timeout` or `endcard`, with `.png` extension. The two-star win and end-card resolution captures are presentation-state previews, not actual completion at each resolution. The timeout captures use actual timer expiry.

The in-memory capture callback reported completion of all 29 native captures (14 UI states plus 15 initial levels) and removed itself. It waited at least 1.5 seconds and 60 Editor update ticks between state changes and captures; those ticks were not independently measured as 60 rendered game frames. Earlier `01-` through `08-` images show intermediate polish and are not the final whole-object UI evidence.

`stamp.txt` in this evidence folder identifies the source/assets associated with this pass; it is content identity, not a test or art approval.

## Recovery and exploratory failures

An interrupted/hot-reloaded Play session emitted NullReferenceExceptions from nonserialized GameUI/GameFeel runtime references. A full stop and fresh Play restored the runtime; subsequent checks had no Error/Warning entries. No speculative gameplay/lifecycle patch was made. Stop fully before restarting after source changes.

One synthetic Play-button test raycasted immediately after changing UI state in the same Editor command and did not find the button. After a normal Editor update, the raycast found Main Play and the callback cases passed. The initial failed check is not presented as a pass. One guarded asset-import attempt returned “Stop Play first” without changes; import and scene saving were performed only after stopping.

## Remaining gates

1. Human visual/feel acceptance, especially recognizability of folded and uncommon F/W/Z-shaped objects. Geometry passing is not aesthetic approval.
2. Physical Android touch, keyboard/gamepad navigation, haptics, pause/resume, save persistence across process restart and target-device performance.
3. Audio listening and mix acceptance; particle motion/feel on hardware.
4. A formal supported-resolution and safe-area matrix, including cutouts, system UI, tablets and any supported landscape orientation. The two portrait sample sizes are not that certification.
5. UI-policy gaps remain: the central English text table is not a localization system; adjustable text scale and a dedicated colorblind mode are absent. Existing icons/patterns and reduced-motion support do not satisfy every accessibility requirement.
6. Story/release gates stay open; this art record does not retroactively close existing QA or checklist gaps.

## Editor handoff

Verified in live runtime after captures: Main state, Play Mode running and unpaused, 720×1280. Production progress still showed 45 stars; isolated QA wins, fresh-save previews and capture setup did not overwrite it. Existing unrelated config, package, font, scene and engine-reference changes are preserved. No device/release readiness is claimed.
