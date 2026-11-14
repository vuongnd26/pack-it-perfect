# Sourced polish continuation — 2026-11-14

**Goal:** Finish the interrupted sourced-art, one-tray HUD and runtime feedback pass from the user handoff.
**Architecture:** Keep existing Unity runtime/view separation. Fit sourced illustrations inside occupied cells; legacy illustrations remain for unmatched objects. Notices share the HUD info row; Rules opens pause with readable explanations. Reuse bounded audio/VFX pools.
**Spec:** User handoff in this session; existing design/game-brief.md.
**Constraints:** Preserve 20-level data, rules, economy, production saves, release version/APK. No commit/push. Continue the existing dirty workspace without moving or dropping prior work.

- [x] Repair HUD compile errors; compile in the running Editor.
- [x] Verify banner bounds, Rules, pill text and sourced-art markers using PlayMode captures; fix observed defects.
- [x] Connect sourced SFX and particle sprites; preserve mute, reduced-motion and resource lifetime.
- [x] Run full EditMode/PlayMode suites; inspect screenshots, source manifest and integrity hashes; record results.

Review focus: narrow timed-level pill; changing safe area/UI scale; notices restoring held-item info; concave/rotated footprints and marker occlusion; loaded audio clips surviving runtime disposal.

Evidence on entry: Logs/Editor.log has CS0117/CS1061/CS0103 for RULES_BUTTON, rulesButtonWidth, RuleIcon, FitPillText and removed HUD pager APIs. Existing banner regression is ready to expose board overlap once compilation succeeds.
Ruling: execute the already-authorized continuation in this workspace; no new design approval or commits required.

## Runtime structure and changes

- `PackingGame` loads level data through `LevelLoader` and owns controller input, board state, rules and tray positions. `PieceArtCatalog`, `PieceView` and `SpriteFactory` remain presentation-only; neither image geometry nor marker geometry changes collision or placement.
- `GameUI` owns screen state, progress and timer coordination. HUD no longer subscribes to pager events. `FitPillText` separates level and percentage; missing Rules constants/configuration and Unicode symbol extraction are restored.
- `BannerView` is hosted inside the HUD information row. Title and subtitle appear sequentially at a fixed font size. It restores the ordinary rule/item information after fading, without using board space.
- `PauseScreen` explains actual level traits and controls in its scrollable card. Text height comes from TMP after the viewport assigns its real width. UI label creation explicitly initializes the default font/material so measuring chips created under a temporarily hidden row is safe.
- Sourced objects retain aspect ratio and fit into a fully occupied rectangle. Markers first seek unused image space; if necessary, one count marker gets a reserved rectangle and the image refits around it. Legacy images remain unchanged. A failed optional sourced-image load now falls back to the original catalog sprite, preserving its mirrored/rotated orientation.
- `GameFeel` routes gameplay/UI events into a bounded voice bank and particle systems. Seven semantic cues now load imported SFX; other cues keep synthesized tones. Only synthesized clips are destroyed by `GameAudio.Dispose`. Star/spark/puff textures use owned materials; mute and reduced motion remain functional.

## Verification record

Executed in the running Unity 6000.6.8f1 Editor using `EditorVerificationBridge`. Requests may specify `testNames`; omitting it runs the full project assembly. Test sessions use isolated in-memory saves.

- Initial targeted run reproduced banner overflow (350 reference pixels vs a 181-pixel band limit), absent Rules explanations, unused sourced SFX and unused VFX textures.
- A subsequent run reproduced TMP material initialization failure when switching levels while a notice hid the info row, plus marker overlap on level 15. Both were fixed and rerun successfully.
- The missing-source regression reproduced the catalog error, then passed after legacy fallback was added.
- Independent read-only review found the fallback issue and raised possible Rules scroll retention. A rendered regression at 800×480, UI 150%, verifies actual scrolling before close and verifies first-line visibility after reopen; it passes without a Rules behavior change. No additional substantive review finding remained.
- Final EditMode request 201013: **113 passed, 0 failed, 0 skipped**. Includes all twenty level witnesses/solver checks, tray layout, and L/U/T/square fitting with rotations and reserved marker space.
- Final full PlayMode request 201014: **27 passed, 0 failed, 0 skipped**. [PlayMode XML](evidence/sourced-polish/after/playmode.xml).
- The full PlayMode suite covers actual controller replay of all 20 levels, motion/audio flow, booster/save behavior, safe-area changes, 480×800 through 1536×2048 plus landscape captures, large UI, readable dock labels/counts, notice bounds/restoration, rule details/reopening, source markers, imported audio lifetime and VFX clearing.

## Integrity

Local manifest verification: all 72 sourced entries match recorded output hashes. Of the 78 catalog objects, 46 have sourced images and 32 intentionally retain legacy artwork. All 163 bindings and every original catalog field remain byte-equivalent after removing the optional `sourced_sprite` fields for comparison. No network re-import was performed in this continuation.

The level JSON, progression JSON and both production save files match the retained baseline SHA-256 values; see `evidence/sourced-polish/after/integrity.json`. Version remains **1.1.0 / Android build code 2**. No APK build, commit or push was performed.

## Evidence and limits

- [Level 1, 480×800](evidence/sourced-polish/after/480x800-level01.png)
- [Level 20, 480×800](evidence/sourced-polish/after/480x800-level20.png)
- [Unlocked dock](evidence/sourced-polish/after/unlocked-dock.png)
- [Rules on level 20](evidence/sourced-polish/after/rules-level20.png)
- [Rules reopened in landscape at UI 150%](evidence/sourced-polish/after/rules-landscape-reopened.png)
- [EditMode XML](evidence/sourced-polish/after/editmode.xml)
- Full screen matrix and per-level initial/win captures: `evidence/mobile-puzzle/`.

Desktop Editor validation is not phone certification or user visual acceptance. The 32 unresolved objects remain listed in `tools/asset-pipeline/sourced-assets.json`; no substitute identities were used. Strict image/marker non-overlap is verified for sourced images; original silhouette art retains its previous marker presentation. Landscape is a fit/cropping regression view, not a claim of comfortable gameplay at every aspect ratio. Existing Unity migration warnings and desktop ASTC decompression warnings remain; no zero-warning claim is made.

Final verification: both complete suites passed after the final runtime change. `git diff --check` passed. The latest [performance report](perf-report.md) records 0 B per-frame managed allocation for the measured idle/drag Update paths; timings are desktop Editor measurements only.
