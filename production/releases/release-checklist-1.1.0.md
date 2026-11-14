# Pack It Perfect 1.1.0 — Internal Android Release

Generated: 2026-11-14. Audience: the developer and internal Android testers.

## Scope and decision

The user approved **an internal APK**, not a Google Play release. Certification for this run is explicitly `none` (internal distribution); `project.yaml` has not been changed to set a permanent certification tier. Workflow/rigor: minimal.

**Build/package verification: PASS. Physical-device verification: NOT ASSESSED. Public release: NOT READY.**

This is not a zero-warning build and is not certified for public distribution. No store upload, Git commit, release tag, push, or GitHub release was performed.

## Release identity

| Field | Value |
|---|---|
| Product | Pack It Perfect |
| Game version | **1.1.0** |
| Android versionCode | **2** |
| Package | `com.sonat.packitperfect` (unchanged) |
| Channel | Internal |
| Unity | 6000.6.8f1 |
| Backend / ABI | IL2CPP / arm64-v8a only |
| Minimum Android | API 26 / Android 8.0 |
| Target / compile SDK | API 36 |
| Build options | None; Development Build, debugging and profiler connection disabled |
| Signing | Existing local **Android Debug** keystore; APK Signature Scheme v2 verified |
| Base source commit | `d280e438cc9bcee1ce49a9381691fd2dbe322012` |
| Source state | Working tree, **not a clean/tagged commit**; see build manifest and source provenance files |
| Built at | 2026-11-14 01:56:59 UTC; build duration 207.26 seconds |
| APK | `pack-it-perfect-1.1.0-b2-android-arm64-internal.apk` |
| APK size | 36,396,318 bytes / **34.71 MiB** |
| Build report | Succeeded; **0 errors, 5 warnings** |
| SHA-256 | `7941878c908b68e01cf50331075170951c35f6a92442590f48dbdd5945729566` |

Versioning rationale: `1.0` / code `1` was the pre-build project setting. `1.1.0` identifies the additive Cozy Travel update; `2` advances Android's install version. Existing repository tags `v1.0.0`–`v1.3.0` belong to the studio framework, not this game. Future game tags should use a distinct namespace, for example `pack-it-perfect/v1.1.0`, and only be created after the user requests a commit/tag. Every subsequent distributed APK must increase versionCode; do not reuse code `2` for changed content.

## Included game content

- 20 packing levels, including levels 16–20 and fragile/heavy placement constraints.
- Undo, Hint, Auto Pack and Freeze, progression rewards and a star-funded booster shop; no real-money purchases or ads.
- Cozy Travel item art and adaptive tray layout, mobile HUD/pause/result screens and UI scale options.
- Motion, particle feedback and procedural audio improvements.
- Performance work including a 60 FPS target, on-demand item art, staged audio synthesis and Android ASTC texture overrides. **A target is not a measured phone frame rate.**

The release operation changes build/version tooling and metadata; it does not change gameplay, item geometry or level timers.

## Codebase health

Scanned **77 C# source files under Assets/**, including runtime, Editor and tests: **0 TODO, 0 FIXME, 0 HACK**. These counts are observations, not a proof of release readiness.

Open bugs: **NOT ASSESSED — no bug records in production/qa/bugs/**. An absent bug register is not a zero-bug result. No milestone directory exists to assess against a release milestone; scope here is the latest current game source and the approved internal APK.

## Completed checks

- [x] Unity Android build succeeded (0 errors; warnings listed below).
- [x] APK identity independently inspected using Android SDK aapt: package, versionName 1.1.0, versionCode 2, minimum SDK 26, target SDK 36.
- [x] `android:debuggable` absent (false); build options are non-development, no script debugging.
- [x] APK signature independently verified using apksigner; one Android Debug RSA-2048 signer, v2 scheme.
- [x] APK ZIP integrity verified.
- [x] Native library inventory confirms ARM64 only and includes libil2cpp.so.
- [x] ZIP alignment check passed, including the 16 KiB page-alignment option.
- [x] ELF PT_LOAD alignment checked for all **7 native libraries**: each supports at least 16 KiB alignment. This does not replace running on a 16 KiB device.
- [x] SHA-256 computed independently and matches build-manifest.json.
- [x] Fresh **100/100 EditMode tests passed**, 0 failed, 0 skipped (2026-11-14 01:51:54–01:51:56 UTC).
- [x] Fresh **18/18 PlayMode tests passed**, 0 failed, 0 skipped (2026-11-14 01:54:22–01:55:26 UTC).
- [x] Automated controller replay wins **all 20 levels without assists**. This is simulated controller input in the Editor, not physical touch input or an installed APK run.
- [x] Regression covers assist cancellation/charge accounting/search limits, pause callback behavior, safe area, large UI, shop purchase restrictions and navigation.
- [x] PlayMode launched and retained runtime screenshots in production/qa/evidence/mobile-puzzle/. Main menu, level 20 play and final result images inspected. These are Editor captures, not device screenshots or human art/feel sign-off.
- [x] Editor performance test passed. Own gameplay Update code: 0 B/frame idle and while dragging; LoadLevel 1/10/20: 5.8/8.6/8.9 ms. Desktop Editor timings are not mobile performance evidence.
- [x] Production progress.json and progress_cozy.json hashes unchanged across the test run.
- [x] Original unrelated Assets/_Recovery files preserved.

## Warnings and limitations

The build report has **5 warnings**, retained verbatim in build-diagnostics.log:

1. Unity: player build started with uncompiled code changes; changed post-processing/importing code may not apply. No gameplay source was edited by this release operation. Nonetheless, a clean recompile/rebuild is required before claiming a clean public candidate.
2. Existing PackingGame.cs uses deprecated `DEVELOPMENT_BUILD` conditional compilation (UAC0009). Public-candidate cleanup should use supported variants/runtime checks as appropriate; no gameplay source change was authorized or made here.
3. TextMeshPro TMP_TextParsingUtilities static constructor emitted to its own C++ file because of compilation cost.
4. TextMeshPro GenerateTextMesh emitted to its own C++ file for the same reason.
5. TextMeshProUGUI GenerateTextMesh emitted to its own C++ file for the same reason.

Additional unassessed/public-release gates:

- [ ] Install and launch this exact APK on an ARM64 Android device.
- [ ] Verify actual touch/rotation/drag, audio/haptics, background/resume, save/restart and display cutouts on a phone.
- [ ] Measure FPS, load times, memory, thermal behavior and battery on the target device; no mobile measurements or soak test were performed.
- [ ] Human art/feel acceptance. Short/landscape layouts remain a known limitation; the Android player is portrait-oriented.
- [ ] Public signing/upload key and Android App Bundle; this APK is signed with a debug key only.
- [ ] Clean compiled candidate and zero-warning policy assessment.
- [ ] Clean game release commit/tag and reproducibility from that exact commit. Base commit alone does not identify every working-tree/generated asset in this APK.

Localization, dedicated colorblind mode and full gamepad board control remain outside the implemented scope. Store metadata, policy/privacy/data safety, age ratings and store certification are out of scope for this internal handoff and have **not** been certified. APK declares INTERNET; no new permissions were introduced by the release tooling.

Certification: omitted — cert_tier is 'none' for the user-approved internal build; no permanent project config change.

## Files and provenance

Output directory: `builds/android/1.1.0/` (Git-ignored).

- APK and SHA256SUMS (APK hash).
- build-manifest.json: Unity build summary, base commit and tracked worktree changes observed before the build.
- build-diagnostics.log: all five warning diagnostics.
- apk-verification.json: independent package/signature/ABI/alignment/test checks.
- badging.txt, androidManifest.txt, signature.txt, alignment.txt: Android SDK inspection output.
- editmode.xml, playmode.xml and editor-performance.md: fresh QA results.
- release-config.json: copy of the approved game version config.
- release-checklist-1.1.0.md: copy of this handoff.
- source-provenance/: post-build tracked diff, status, build-input hashes and copies of untracked release tooling. **Not a clean-source guarantee; Unity mutates some assets during the build.**

Unity also regenerated the dynamic Fredoka font atlas/glyph tables during tests/build, shader-prefilter data in UniversalRP.asset, global render-pipeline build settings and an Input System preloaded-asset entry. Tests regenerated screenshots and perf-report.md. These engine/test-generated files remain visible in Git; they have not been manually edited, reverted or committed. The build generated .utmp/ as a local native-build cache; it has not been deleted or committed.

## Install and verify

Copy the APK to an ARM64 Android phone (Android 8.0 or later), then allow installation from that file source and install it. Alternatively, from this directory with a device connected:

```sh
adb install -r pack-it-perfect-1.1.0-b2-android-arm64-internal.apk
sha256sum -c SHA256SUMS
```

Update-in-place requires an existing installation with the **same package and signing certificate**, and an older versionCode. A different signature causes Android to reject the update. Do not uninstall merely to bypass that error without backing up progress: uninstalling can delete local saves. Keep the local debug keystore safe for future internal updates; do not include its private key or passwords in the release package.

## Overall disposition

**Internal APK built and package-verified; device playability NOT ASSESSED. Public release NOT READY.**

Known checklist failures: zero-warning/clean-compiled build and clean tagged-source reproducibility. Public signing is intentionally absent for the chosen internal channel. Open-bug count, device QA, mobile performance and human acceptance remain unassessed rather than passed. No estimate is claimed for checks requiring hardware or the user's acceptance.
