# Validation — 2026-09-15

## Skyscraper update

The new `SkyscraperValidation` isolated Play Mode smoke test passed:

- 24 unique window IDs and independent initialized GPU masks.
- Different starting cleanliness values and 24 world-space percentage labels.
- Correct overall aggregate and HUD job binding.
- Window reach checks, continuous GPU wiping, and isolation from neighboring windows.
- Four-state lift button cycle, upward/downward movement, stationary stops, and top/bottom limits.
- Grounded CharacterController carried in both directions.
- Remote-role player replica disables input/camera/audio/controller, preserves the local cursor and cannot operate the local button.
- Deferred cleaning requests do not mutate dirt until accepted; accepted playback updates the window.
- Visual inspection of `Documentation/Validation/Skyscraper.png`.

The button state test calls the same `PlatformButton.Press` method used by E/X interaction; it is not a manual keyboard/gamepad playtest. Actual networking and standalone player builds were not tested or implemented.

Validated in a separate temporary Unity **6000.0.60f1** project using this project's installed package versions and Direct3D 11 on the available NVIDIA GPU. Only the generated Sandouq assets were copied back; project settings and package manifests were not changed.

## Passed

- C# compilation and checks for errors in both custom shaders.
- Generation of the four real prefab assets and prototype scene; no missing scripts.
- Play-mode scene references, GPU mask initialization and initial asynchronous readback.
- First-person camera contact and valid mesh UV coordinates.
- Rejection when requested reach is too short.
- Third-person camera offset and contact using the player's hand reach.
- A segment from UV `(0.2, 0.5)` to `(0.8, 0.5)` clears its midpoint, preserving a corner outside the stroke.
- Partial cleanliness: **0.08177822**, approximately **8.2%**.
- Full-surface cleaning reaches greater than 99% cleanliness.
- Completion emits once and does not repeat on subsequent cleaning.
- Visual inspection of dirty first-person and wiped third-person camera captures in `Documentation/Validation/`. Camera-only captures omit the overlay HUD.

These are automated Play Mode smoke tests and render inspections, not a complete manual controller playtest or standalone player build. The source test is `Scripts/Editor/PrototypeValidation.cs`; run `Sandouq.Editor.PrototypeValidation.Run` using Unity's `-batchmode -executeMethod` in an isolated copy. Do not add `-quit`: the test exits after its Play Mode checks.

## Environment note

The temporary project used local package-cache references because online package resolution stalled. Unity emitted a non-blocking assembly validation exception for a missing DLL inside the pre-existing `com.unity.collections` package test folder. Prototype compilation and runtime checks completed successfully. No package files were intentionally modified to resolve that unrelated warning.

## Prototype limits

Planar front-facing surfaces with non-overlapping 0–1 UVs; approximate periodic progress; GPU readback support required. No persistent saves, economy, resource consumption or runtime tool inventory yet. See PrototypeGuide.md for settings, performance costs and extension instructions.
