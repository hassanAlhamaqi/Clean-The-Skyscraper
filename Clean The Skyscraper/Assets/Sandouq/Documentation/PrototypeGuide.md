# Glass cleaning prototype

**New multi-window scene:** see [SkyscraperGuide.md](SkyscraperGuide.md) for the 24-window façade, editable dirt textures, overall/window UI, four-state lift and multiplayer preparation. The instructions below describe the original single-wall scene.

## Play

1. Open this project in Unity **6000.0.60f1**. Allow script/shader imports to finish.
2. Open `Assets/Sandouq/Scenes/Prototype/Cleaning_Prototype.unity`.
3. Press Play. The player starts facing a dirty 6 × 3 metre glass wall.
4. Hold left mouse button while aiming and moving across the glass. Walk sideways to reach its edges; look up or jump for the top.
5. Press **V** to switch first/third person. Press **Escape** to unlock/relock the cursor.

| Action | Keyboard/mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Wipe | Hold left mouse | Hold right trigger |
| Sprint | Left Shift | Left stick click |
| Jump | Space | South button |
| Camera mode | V | Right stick click |
| Cursor | Escape | Start |

The Input System and URP are existing project dependencies. No packages were added. The setup does not alter the existing SampleScene, input assets, project settings or build scene list. To make a standalone build, explicitly add this scene to your own Build Profile.

## Architecture

```text
PlayerInputReader → PlayerMovement
                  → PlayerCameraController
                  → CleaningToolController ← CleaningTool + CleaningToolData
                         ↓ PlayerInteraction (camera aim + hand reach + obstruction)
                    CleanableSurface (UV, radius, strength, falloff)
                         ↓ GlassSurface
                      DirtMask ← DirtType
                         ↓ periodic progress event
                    CleanableSurface → CleaningHUD
```

`PrototypeBindings` connects scene instances. Prefabs contain their own internal references; the HUD does not depend on the GlassWall class.

## Scripts and responsibilities

All paths below are relative to `Assets/Sandouq/`.

| Script | Responsibility / useful settings |
|---|---|
| `Scripts/Player/PlayerInputReader.cs` | Owns and disposes modern Input System actions; keyboard/gamepad bindings and cursor capture. Actions are created once, not every frame. |
| `Scripts/Player/PlayerMovement.cs` | CharacterController walking, sprint, jump, gravity. Tune speeds, jump height and gravity. |
| `Scripts/Player/PlayerCameraController.cs` | First/third person setting, pitch/yaw, body visibility, third-person obstruction. Tune sensitivity, stick speed, camera distance and obstruction layers. |
| `Scripts/Player/PlayerInteraction.cs` | Raycast for camera aim, cache cleanable collider component, enforce hand reach and hand-to-surface occlusion. Tune contact layers and debug ray. |
| `Scripts/Cleaning/CleanableSurface.cs` | Abstract reusable cleaning contract, cleanliness, required threshold and progress/completion events. |
| `Scripts/Cleaning/CleaningToolController.cs` | Reads clean input, sends strokes, resets continuity on release/lost contact/surface change/large contact jump. Tune spacing, maximum stroke distance and debug options. |
| `Scripts/Tools/CleaningTool.cs` | Holds tool data, positions the visible blade's CleaningPoint at surface contact, restores its resting pose. |
| `Scripts/Tools/CleaningToolData.cs` | Tool ScriptableObject: name, icon, world-space radius, strength, speed multiplier, falloff and reach. |
| `Scripts/Dirt/DirtType.cs` | Dirt ScriptableObject: name, optional grayscale texture, color, amount, resistance. |
| `Scripts/Dirt/DirtMask.cs` | Owns each surface's GPU textures/material, initializes dirt, erases strokes, asynchronously samples progress and releases resources. Tune resolution and progress interval. |
| `Scripts/Glass/GlassSurface.cs` | Implements the cleaning contract for a planar UV-mapped surface; converts local dimensions and transform scale to world brush dimensions. |
| `Scripts/UI/CleaningHUD.cs` | Subscribes to a CleanableSurface's events, shows percentage/completion and bound tool name; safely unsubscribes. |
| `Scripts/Core/PrototypeBindings.cs` | Explicitly wires the HUD to the scene's player and wall in Start. |
| `Scripts/Editor/PrototypeBuilder.cs` | Creates missing assets and a scene with serialized references using built-in Unity primitives. Preserves existing prefabs/materials/data; exits if the scene exists. |
| `Scripts/Editor/PrototypeValidation.cs` | Explicit batch-editor smoke test for GPU strokes, camera contacts, reach, progress and completion. Does not auto-run in the interactive editor. |

## Assets and prefab contents

```text
Assets/Sandouq/
├── Scripts/                 (listed above)
├── Prefabs/
│   ├── Player/PF_Player.prefab
│   ├── Tools/PF_Squeegee.prefab
│   ├── Glass/PF_GlassWall.prefab
│   └── UI/PF_CleaningHUD.prefab
├── ScriptableObjects/
│   ├── Tools/Squeegee_Default.asset
│   └── Dirt/Basic_Dirt.asset
├── Materials/
│   ├── M_DirtyGlass.mat
│   ├── M_Frame.mat
│   ├── M_Floor.mat
│   ├── M_Squeegee.mat
│   └── M_Skyline.mat
├── Shaders/
│   ├── DirtBrush.shader
│   └── DirtyGlass.shader
├── Scenes/Prototype/Cleaning_Prototype.unity
└── Documentation/PrototypeGuide.md
```

Requested category folders are reserved under this root. No texture, VFX, audio, job or separate dirt prefab is necessary yet: dirt is a per-wall GPU mask and the default pattern is procedural.

**PF_Player:** CharacterController, input reader, movement, camera controller, interaction and cleaning controller on its root. Camera Root holds Camera (+ AudioListener) and Hand Origin. Hand Origin contains a nested PF_Squeegee instance. A capsule body appears in third person. The player uses built-in Ignore Raycast layer 2; contact/camera masks exclude that layer.

**PF_Squeegee:** CleaningTool on root; handle and rubber blade mesh children; CleaningPoint at blade centre. No colliders on the tool so it cannot obstruct its own aim.

**PF_GlassWall:** Glass Surface is a scaled built-in Quad with Renderer, MeshFilter, MeshCollider and GlassSurface. Its Dirt System child owns DirtMask. Frame children provide visible borders and box collisions. An invisible backing BoxCollider blocks the player. This prototype is intended to be cleaned from the front (-Z) side. Each prefab instance creates independent dirt textures, while sharing static materials and data safely through a MaterialPropertyBlock.

**PF_CleaningHUD:** Canvas, CanvasScaler and CleaningHUD; child Text elements for progress, tool, controls and crosshair. It has no interactive buttons, so an EventSystem is unnecessary.

## Scene setup from scratch

The delivered scene is ready to use. For another scene:

1. Add a floor with a collider and a directional light.
2. Drag PF_GlassWall into the scene at the origin. Its front faces -Z.
3. Drag PF_Player to `(0, 0.05, -1.65)` with rotation `(0, 0, 0)`.
4. Drag PF_CleaningHUD into the scene. Avoid adding a second camera or AudioListener.
5. Add PrototypeBindings to a scene object. Assign the wall's **Glass Surface** component, player's **CleaningToolController**, and HUD's **CleaningHUD**.
6. Press Play. Each play session starts with fresh dirt.

If using only the source files in a fresh project, save your current scene first, then choose **Sandouq → Create Missing Prototype Assets** after compilation. It creates the missing prefabs and scene; it does not open the generated scene or replace your active scene. It never regenerates an existing prototype scene. Keep custom edits in prefab variants or scene copies.

## Dirt and progress

Each mask starts with grayscale dirt coverage from DirtType.Texture, or procedural mottling when empty, multiplied by Amount. `DirtBrush` initializes a linear GPU RenderTexture. The visible `DirtyGlass` URP shader blends dirt color into a transparent blue glass tint according to this mask. The skyline behind the glass makes cleared regions visible. This is simple transparent glass without physical refraction.

Initialization waits one frame for URP startup, then records the initial dirt mass. `DirtMask.IsReady` reports when that baseline is available. Blits restore the previous active render target so they do not leave global rendering state pointing at the mask.

A raycast supplies position, normal and MeshCollider UV. The tool controller passes UV endpoints and data to CleanableSurface; it never reads/writes a texture. GlassSurface converts its planar dimensions into world units. DirtMask draws a capsule-shaped stroke into a second texture, then swaps the two textures. The shader analytically measures distance to the entire segment, so rapid movement leaves no stamp gaps. Spacing controls when the anchor advances; maximum stroke distance breaks jumps. Stroke strength is multiplied by delta time and tool speed, then divided by dirt resistance. Holding still cleans a circle.

Every 0.3 seconds while dirty, GPU mip generation reduces the mask and asynchronous readback retrieves a **16 × 16 mip**. Remaining coverage is divided by the initial coverage to calculate removed dirt, rather than comparing against assumed solid-white dirt. Completion fires once when the configured threshold is crossed (default 95%). Progress is approximate and delayed by the interval/readback. Cleaning remains possible after completion. Initial cleaning waits for the initial GPU snapshot, typically a few frames.

Performance: two mipmapped 512² RGBA8 masks cost approximately **2.7 MiB per wall**. Each cleaning call performs one full-mask blit; no CPU pixel painting or large texture readbacks occur during gameplay. Progress sums only 256 samples. RGBA8 erosion/mip filtering has quantization error; the threshold deliberately tolerates small residuals. For many active walls, consider tiled masks, dirty-region rendering, shared GPU brush resources and a budget for progress requests. Current masks are recreated on play and are not saved. The prototype requires async GPU readback support; unsupported graphics devices report a configuration error instead of pretending progress works.

## Tune first

- **Squeegee_Default:** Radius (metres), Strength, Cleaning Speed, Falloff, Reach.
- **Basic_Dirt:** Amount, Resistance, Color, Texture. Textures should represent linear grayscale coverage; turn off sRGB in their import settings.
- **Glass Surface:** Required Cleanliness; Local Size only changes if the underlying mesh dimensions change. Keep it `(1,1)` for the built-in Quad, even when scaling the transform.
- **Dirt System:** Resolution and Progress Interval.
- **Player:** movement speeds, camera perspective/sensitivity, maximum stroke distance, spacing; enable debug rays, contact gizmo or UV display as needed. Gizmos require the Game/Scene view Gizmos toggle.

## Expand

**Second tool (e.g. sponge):** Create → Sandouq → Cleaning Tool, set name/radius/strength/falloff/reach. Duplicate PF_Squeegee under Prefabs/Tools, replace its primitive visual children, keep CleaningTool and CleaningPoint, and assign the new data. Replace the nested tool on a PF_Player variant and assign its CleaningTool in CleaningToolController. No cleaning-core edits are needed. Runtime tool switching would need an equip API plus a tool-change event for the HUD; that system is intentionally not present yet.

**Second planar surface (e.g. dirty sign):** Duplicate PF_GlassWall under Prefabs/Glass or a new Prefabs/Surfaces folder inside Sandouq. Change the frame/visual material and dimensions, retain its Quad/MeshCollider, GlassSurface and DirtMask, and bind the desired surface to a HUD. Surface behavior is independent of camera/tool. For a truly different geometry/cleaning model, derive from CleanableSurface and implement Clean; the player and tool remain unchanged. The current world-radius mapping assumes planar, non-overlapping 0–1 UVs with perpendicular local axes; curved meshes and UV seams require a separate mapping strategy.

**Second dirt type:** Create → Sandouq → Dirt Type; set texture, color, amount and resistance; assign it to Dirt System's DirtMask on a wall or variant. No core code changes.

**Future systems:** Put water/battery/stamina checks around tool use; upgrades can supply effective tool settings without mutating shared data assets. Aggregate surface completion events in a future job component for rewards/stages. Keep economy separate from the dirt renderer. Save masks only at explicit save points. A job can decide which surface the existing HUD observes through Bind. Resource consumption, economy, jobs and persistent progress are extension points, not implemented features.
