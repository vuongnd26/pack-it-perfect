# Story 008 — retained visual evidence

Date: 2026-11-12. Engine: Unity 6000.6.8f1, SampleScene, Play Mode.

**Run result: OBSERVED. Sign-off: PENDING — no final lead/user approval or hardware haptic verification is claimed.**

## Observed

- Tutorial hand on level 1: `story-008-tutorial-and-juice/level01-tutorial.png`.
- Correct-placement praise and light burst: `story-008-tutorial-and-juice/placement-praise.png`.
- Completion confetti: `story-008-tutorial-and-juice/confetti-completion.png` (live completion with the burst advanced for capture, then Editor paused).
- GameFeel routes placement, rotation, rejection, completion and warning events. Reduced motion propagates into pieces, lid, tutorial, confetti and UI animation.

## Not signed off

- Android haptics use the platform view haptic API and respect device settings; no physical-device test was performed.
- Wrong-drop shake / tactile feel and overall pacing still need hands-on approval.
- The praise word selection uses the closed five-word list. Other feedback such as rule toasts is separate; this evidence does not certify the literal requirement that no other praise-like text appears anywhere.
- Do not cite `story-008-tutorial-and-juice/confetti.png`; it captured the wrong state.

Source identity: `story-008-tutorial-and-juice/stamp.txt`.
Full execution details: `../pack-it-perfect-editor-verification-2026-11-12.md`.
