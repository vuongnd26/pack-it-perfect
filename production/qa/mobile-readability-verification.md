# Mobile readability pass (option B) — verification, 2026-11-14

Scope: two-row paged tray, readable booster dock, larger rule markers, held-item info line, simplified art.
Not changed: levels.json, progression.json, rules, timers, solver, saves (hashes identical before/after), version 1.1.0, existing APK.

## Tests (Unity 6000.6.8f1, open Editor)
- EditMode 104/104, PlayMode 20/20 (was 100 + 18). New: TrayPaginationTests (3), MobileReadabilityLayoutTests (1), MobileReadabilityTests (2).
- Watched fail first: tray viewport 18.96 world units vs limit 2.68 (before/editmode-red.xml).
- NOT watched fail on old code: the rendered dock font test (its first run failed on a fixture lookup, not on the font size).
- All 20 levels are won through the real controller, turning tray pages via SetTrayPage.

## Measured board cell size (px per cell)
| Screen | Level | Before | After |
|---|---|---|---|
| 480x800 | 1 | 67.7 | 69.2 |
| 480x800 | 15 | 37.2 | 37.6 |
| 480x800 | 20 | 38.3 | 39.3 |
| 1080x2400 | 1 | 209.4 | 214.5 |
| 1080x2400 | 15 | 111.1 | 119.6 |
| 1080x2400 | 20 | 114.3 | 125.2 |
Tray pieces are drawn at 0.66 of a cell (was 0.60): about +13% at 480x800 and +20% at 1080x2400 on level 20.
Dock at 480x800: name 42 ref px = 18 px rendered (was 11), count 48 ref = 20.6 px (was <= 13).

## Looked at (Editor captures, not a phone)
evidence/mobile-readability/before/, after/ (review-sheet.jpg), evidence/mobile-puzzle/ (regenerated matrix).

## Known gaps
- At 150% UI size on 480x800, level 20's board is smaller than before (about 26 vs 32 px per cell): bigger text and the pager row take the space.
- Shop/menu secondary text was not enlarged. No Rules legend screen; chips keep their words instead.
- Paging reveal rules (Hint, Auto Pack, Undo, return) are implemented but have no dedicated PlayMode test.
- No Android device: touch, real DPI, audio, frame rate NOT ASSESSED. Art/feel needs human sign-off.
