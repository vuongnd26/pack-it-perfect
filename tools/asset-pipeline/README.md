# Art pipeline

The runtime now combines sourced illustrations with the earlier original 2D artwork. The bundled Fredoka font is under SIL OFL 1.1.

## Sourced presentation pass

`sourced-assets.json` records source URLs, revisions/archive hashes, output hashes, transformations and licenses. The imported files are under `Assets/Resources/Art/Sourced` and `Assets/Resources/Audio/Sourced`; redistribution notices are in `Assets/Resources/Art/THIRD_PARTY_NOTICES.txt`.

- 46 of 78 catalog objects have a same-identity Fluent Emoji illustration. The other 32 retain their original art; unresolved entries are explicit in the manifest.
- `sourced_sprite` is an optional presentation-only object field. Authored cells, the original `sprite` path and all 163 level/piece bindings stay unchanged.
- Natural images keep their aspect ratio and sit on occupied-cell bases. `PiecePresentationLayout` fits the image inside a filled rectangle, avoiding holes and any reserved rule-marker rectangle. Crowded traits can share one count marker; holding the item shows its traits and Rules explains the level's rules.
- `SourcedArtImport` owns PNG import settings for this subtree, including FullRect meshes and Android ASTC. Existing original-image import settings are separate.
- Seven sourced SFX serve Pickup, Place, Rotate, Return, Undo, UI and Locked. Other cues retain procedural audio. Imported clips are shared assets and are never destroyed by a session's audio bank.
- Kenney star, spark and puff textures feed the existing bounded particle systems. Materials are owned per effect instance and disposed with it; reduced motion clears these effects.

`import-sourced-art.py` is a promotion script, not a read-only validator: it downloads the pinned Fluent assets and consumes Kenney archives already staged in `Library/SourcedArtStaging`, then updates the imported files, catalog and provenance manifest. Do not run it merely to check current assets. Compare the local bytes against manifest hashes for verification.

Runtime evidence and current test results are recorded in `production/qa/sourced-polish-continuation.md`. Editor captures do not replace phone testing or user art acceptance.

## Original 2D generators (legacy artwork)

The generators below create locally authored SVG illustrations and render them to PNG with `@resvg/resvg-js@2.6.2`. They remain useful for the retained legacy assets; they do not generate the sourced art or its catalog additions.

### Staging only

All three generators require an **existing, empty, absolute output directory outside the project**. They reject non-empty destinations and destinations inside the project, including symlinks resolving there, before writing. Use **separate directories for items, UI and environment**. The item generator removes its unreferenced work-in-progress images from its own output; never mix it with imported assets or another generator's output.

```sh
# Reuse an installed renderer where available. If installation is necessary, keep it outside the project.
npm install --prefix /path/to/art-runtime --ignore-scripts --no-audit --no-fund @resvg/resvg-js@2.6.2

# Choose a NEW scratch run directory for every build; do not reuse a previous output.
RENDERER=/absolute/path/to/art-runtime
STAGING=/absolute/path/to/new-scratch-run
mkdir -p "$STAGING/items" "$STAGING/ui" "$STAGING/environment"
node tools/asset-pipeline/item-art-build.js "$RENDERER" "$STAGING/items" Assets/Data/Levels/levels.json
node tools/asset-pipeline/ui-art-build.js "$RENDERER" "$STAGING/ui"
node tools/asset-pipeline/art-build.js "$RENDERER" "$STAGING/environment" Assets/Fonts/Fredoka-SemiBold.ttf
```

Generation is not promotion or visual acceptance. Run baseline runtime QA before promotion, compare staged samples at actual gameplay sizes, and get the parent/user's visual approval. Then promote only the intended PNGs through the parent/import workflow. Do not replace the imported catalog if it is unchanged, do not replace directories wholesale, and do not hand-edit `.meta` files. Keep existing resource IDs, GUIDs, import overrides, sprite borders and Android ASTC settings. None of these scripts imports into Unity, changes a scene or changes gameplay data.

## Whole-object pieces

The generator draws the complete object in its footprint, not a separate coloured puzzle block below a small item icon. Shape assignments are explicit per level/letter, with context-specific food, clothing, toiletries, heavy objects, upright bottles and paired folding poses. It verifies all 163 bindings against the current level map, allowing artwork reflection and clockwise quarter turns only. The current catalog contains 78 referenced illustrations across all 20 levels. Levels 16–20 take their assignments from `tools/level-design/cozy-assignments.json`; the original 112 bindings remain unchanged.

### Readability cleanup

The approved Cozy Travel 2D cleanup preserves the existing palettes, footprint contour, construction details and material categories. Shared helpers apply the cleanup across all 78 illustrations rather than replacing objects or manually redesigning every item:

- Smooth cloth and paper finishes, with procedural weave/grain removed entirely.
- Fewer, broader ribs on cuffs/caps/grips; smooth subdued construction seams instead of fine dashed stitching.
- Hairline decoration is subdued, bottle labels no longer put printed stripes behind their central emblem, and the small sock dot motif is simplified.
- Top-left key light, broad base gradients and soft lower contact shade remain; the bright doubled perimeter bevel becomes a low-contrast soft edge response.
- Metal keeps one broad reflection and glass keeps a wider soft reflection. Construction cues still distinguish materials; everything is not given a plastic gloss.

Context replacements from the prior art pass remain: snorkel (beach bag), protein shaker (gym bag), desk lamp, watering can and rolled poster (moving box), and camp lantern (camping backpack). No display names, object IDs, cells, rotations or mirror flags are changed by this cleanup.

PNG geometry contract: each authored cell is 100 SVG units rendered to 128 pixels. The drawing is inset 5 SVG units around its bounding rectangle, keeping the dark stroke inside the occupied footprint. Outline width is 5 units and the unchanged contour rounding radius is 16 units. These are art proportions, not gameplay constants.

Import contract: Sprite/Single, FullRect, centered pivot, 128 pixels per unit, alpha transparency, no mipmaps, Bilinear/Clamp and non-readable. Preserve the existing default/target compression and max-size settings; the PNG generator does not configure them.

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

`art-build.js` stages 11 images: frame, inset, handle, wheel, blocked cell, two fill-bar images, tutorial hand, two backgrounds and logo. It takes renderer directory, empty staging directory and a static Fredoka font path. The existing `Assets/Fonts/Fredoka-SemiBold.ttf` is a static local face; the logo uses weight 600 to match it. No system-font loading or network font download is required.

The gameplay background removes repeating dots but retains its warm gradient, broad light area and overlapping lower dunes. The menu removes sunburst rays while retaining radial light, soft clouds and lower hills. Frame rivets/inner border and the blocked-cell inner border are removed; blocked hatching is fewer and broader. Existing dimensions, grayscale/tintable frame behavior and slicing geometry are preserved. Environment is separate from item artwork so item edits do not regenerate it.

## UI illustrations

`ui-art-build.js` stages 19 original illustrations: `ui_home`, `ui_hint`, `ui_restart`, `ui_star`, `ui_lock`, `ui_clock`, `ui_ribbon`, `ui_card`, `ui_undo`, `ui_autopack`, `ui_freeze`, `ui_badge`, `ui_slot`, `ui_pause`, `ui_heavy`, `ui_upright`, `ui_pair`, `ui_fragile` and `ui_chest`. Existing text/shape fallbacks remain available; not every emoji in the project has been replaced.

- `ui_badge` is now a blank opaque rounded cream number backing, not a decorative medal. The runtime owns the dark count text; no numeral is baked into the image.
- `ui_slot` is one chunky cream tile with a short lower edge; no dashed inset or inner border. Its imported border remains zero: use as a simple image/panel, not an assumed new nine-slice contract.
- Hint is a large bulb; Undo is one broad bent arrow; Auto Pack is a suitcase with one large magic mark; Freeze is a dark snowflake on opaque pale blue.
- Rule symbols are thick dark weight/upright arrow/chain links/cracked glass on opaque cream backings. They have shape cues independent of colour and no fine decorative strokes. Marker sizing/layout remains the runtime's responsibility.
- Cards, timer and other small UI illustrations lose unnecessary inner borders or decorative highlight lines without changing resource names or texture dimensions.

UI PNG import contract: Sprite/Single, FullRect, centered pivot, 100 pixels per unit, alpha transparency, no mipmaps, Bilinear/Clamp, non-readable and no generated fallback physics shape. The SVGs render at 2× their authored dimensions. `ui_card` retains its 384×384 dimensions/corner geometry and existing 84-pixel border on all four sides; the other eighteen UI sprites have zero borders. Retain existing target compression and max-size overrides via the importer.

## Verification and performance scope

Before promotion, compare the generated catalog byte-for-byte with the current catalog, verify all 163 shape bindings, and check all PNG file names/dimensions. For items also compare alpha support against a same-renderer baseline, not just the occupied-cell strings. Review the count badge with dark numbers, boosters/rules at actual marker size, both backdrops, and cloth/glass/metal/irregular objects at gameplay scale. A contact sheet is only a staging preview, not evidence of runtime acceptance.

This pass introduces no new resource IDs, catalog entries, shader/material changes, texture dimensions, runtime texture loads or per-frame work. With imports unchanged, the nominal texture pixel allocation is unchanged; smoother PNGs may reduce file bytes, not GPU texture memory. No numeric rendering budget is introduced here, and no draw-call, frame-time, overdraw or device-performance improvement is claimed without profiling. Parent runtime QA, retained before/after screenshots and user art/feel acceptance remain required.

Historical verification is recorded in `production/qa/whole-object-art-verification-2026-11-13.md`; it does not approve these staged candidates. Runtime tray layout is owned by the separate mobile-readability pass, not by these generators. Editor presets are not physical-device certification.
