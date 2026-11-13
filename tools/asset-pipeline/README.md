# Original 2D art pipeline

These are original locally authored SVG illustrations, rendered to PNG with `@resvg/resvg-js@2.6.2`. No image-generation provider, downloaded item art, or third-party brand imagery is used. The bundled Fredoka font is under SIL OFL 1.1.

## Build whole-object pieces

```sh
# Install the renderer into a disposable tooling directory, not Assets or the game runtime.
npm install --prefix /path/to/art-runtime --ignore-scripts --no-audit --no-fund @resvg/resvg-js@2.6.2
# Use a new empty staging folder: the generator prunes unused images in its output.
node tools/asset-pipeline/item-art-build.js /path/to/art-runtime /path/to/new-staging Assets/Data/Levels/levels.json
# Review/diff staged images and catalog, then copy intended changes into Assets/Resources/Art.
```

The generator draws the complete object in the footprint, with continuous surfaces, stitching/materials, outlines, light and shade. There is no separate coloured puzzle block below a small item icon. Shape assignments are explicit per level/letter, with context-specific food, clothing, toiletries, heavy objects, upright bottles, and paired folding poses. It verifies all 163 bindings against the current level map, allowing artwork reflection and clockwise quarter turns only. The current catalog contains 74 referenced illustrations across all 20 levels. Levels 16–20 take their assignments from `tools/level-design/cozy-assignments.json`; the original 112 bindings remain unchanged.

PNG import contract: Sprite/Single, FullRect, centered pivot, 128 pixels per unit, alpha transparency, no mipmaps, Bilinear/Clamp, uncompressed, non-readable. Each authored cell is 100 SVG units rendered to 128 pixels. The drawing is inset 5 SVG units around its bounding rectangle, keeping the dark stroke inside the occupied footprint. SVG stroke width is 5 units and contour rounding radius is 14 units. These are art proportions, not gameplay constants.

## Manifest schema (version 1)

`Assets/Resources/Art/item_catalog.json` is presentation-only. It does **not** change `levels.json`, collision, placement, timer, pair, zone, hint or star rules. The manifest uses snake_case; private DTOs in `PieceArtCatalog` map this wire format into immutable runtime `PieceArt` properties.

- `schema_version`: integer `1`; other versions are rejected.
- `objects`: only referenced object definitions, each with:
  - `id`: unique illustration key.
  - `display_name`: English object name used in feedback, through the existing `GameTexts` formatter. Full localization is a pre-existing project gap.
  - `sprite`: resource path relative to `Resources/Art/`, without `.png`.
  - `cells`: equal-width strings; `X` occupies a cell, `.` is transparent/unoccupied. Top row first. The shape is normalized to a zero top-left origin.
- `bindings`: one per level and piece letter:
  - `level`: 1-based level number.
  - `piece`: map letter.
  - `art_id`: reference into `objects`.
  - `solution_turns`: clockwise quarter turns (0–3) from the authored image into the solution footprint.
  - `mirrored`: reflect the authored image horizontally **before** rotating. Default false.

Runtime view orientation also includes existing `startTurns`, increments explicitly on accepted tap rotation, and resets to solution orientation after Hint. Explicit orientation is necessary for squares and other rotationally symmetric footprints. Rule badges remain upright and the logical ghost still displays the actual occupied cells.

Missing manifest/binding falls back to legacy rendering. A missing sprite or unsupported schema is logged as an error. `PieceArt.SolutionShape()` exposes a pure validation path; the board never uses it to make placement decisions.

## Environment art

`art-build.js` builds frame, inset, handle, wheels, blocked cells, fill bar, hand, menu/game backgrounds and logo. It takes renderer directory, output directory and a static Fredoka Bold font path. It is separate from item artwork so item edits do not regenerate the existing environment assets.

```sh
node tools/asset-pipeline/art-build.js /path/to/art-runtime Assets/Resources/Art /path/to/Fredoka-Bold.ttf
```

## UI illustrations

```sh
node tools/asset-pipeline/ui-art-build.js /path/to/art-runtime Assets/Resources/Art
```

The generator builds 19 original illustrations: the original `ui_home`, `ui_hint`, `ui_restart`, `ui_star`, `ui_lock`, `ui_clock`, `ui_ribbon`, and `ui_card`, plus `ui_undo`, `ui_autopack`, `ui_freeze`, `ui_badge`, `ui_slot`, `ui_pause`, `ui_heavy`, `ui_upright`, `ui_pair`, `ui_fragile`, and `ui_chest`. They replace glyphs or plain surfaces in the HUD, timer, level select and result cards. Existing text/shape fallbacks remain available; not every emoji in the project has been replaced.

UI PNG import contract: Sprite/Single, FullRect, centered pivot, 100 pixels per unit, alpha transparency, no mipmaps, Bilinear/Clamp, uncompressed, non-readable, maximum texture size 2048, and no generated fallback physics shape. The SVGs render at 2× their authored dimensions. `ui_card` has an 84-pixel border on all four sides of its 384×384 texture; this preserves the illustrated rounded corners when `UIFactory.StyleCard` uses `Image.Type.Sliced`. The other eighteen UI sprites have zero borders. Generating PNGs does not itself configure Unity import settings; retain the associated `.meta` files.

## Tray presentation

The saved SampleScene and `PackingTuning` defaults use a minimum tray width of 9 cells, a frame-to-tray gap of 0.5 cells, and slot/row spacing of 0.28 cells. The wider, closer tray reduces vertical stacking of long whole-object silhouettes. These are presentation layout values; piece footprints, start rotations, placement rules and reference solutions are unchanged.

## Verification record

See `production/qa/whole-object-art-verification-2026-11-13.md` for the Editor controller checks, retained captures of all 15 initial levels, and seven UI states at each of 480×800 and 1440×2960. Editor presets are not device certification or creative sign-off. Localization, user-adjustable text size, dedicated colorblind modes and physical input/device checks remain outstanding.
