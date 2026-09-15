# Skyscraper prototype

## Start here

Open **Assets/Sandouq/Scenes/Prototype/Skyscraper_Prototype.unity** and press Play.

The façade contains **24 independently cleanable windows** (4 columns × 6 floors), a window percentage label on every pane, and an overall percentage in the original HUD. The player starts on the ground just behind the lift. Walk forward onto its shallow deck. Aim at glass and hold left mouse / right trigger to clean.

Aim at the **yellow button on the central pedestal** and press **E / gamepad X (west button)**. Each new press advances the cycle:

1. **Stopped, ready to rise → ascending**.
2. **Ascending → stopped, ready to descend**.
3. **Stopped, ready to descend → descending**.
4. **Descending → stopped, ready to rise**.

The next action is shown on the button sign and in the HUD when you aim at it. Holding the interaction key does not repeat the action. At the top or bottom, the lift automatically stops and selects the safe next direction. The platform spans the façade so you can walk sideways to each window. It carries a grounded player without making the player a child of the platform. Jumping/walking off releases that support.

Existing controls remain: WASD / left stick, mouse / right stick, V / right stick click for camera, Shift / left stick click for sprint, Space / south button for jump, Escape / Start for cursor.

The original **Cleaning_Prototype.unity** is retained as a small single-wall test scene. Both scenes use the upgraded player and HUD prefabs. No project settings, packages, or unrelated folders were intentionally changed by this update.

## Control the dirt texture

Select any façade window and expand:

`Window → Glass and Frame → Glass Surface → Dirt System`

On **DirtMask**, edit these fields before entering Play:

| Setting | Effect |
|---|---|
| Dirt | Shared DirtType asset: grayscale texture, color, amount, resistance. |
| Texture Override | Optional texture for only this window; takes priority over the DirtType texture. |
| Texture Tiling | Pattern repetition/stretch on the pane. |
| Texture Offset | Pattern position; avoids repeated identical windows. |
| Starting Cleanliness | Initial percentage already clean, from 0 to 1. Also reduces visible dirt. |
| Resolution | GPU mask resolution; façade panes use 256. |
| Progress Interval | Seconds between asynchronous progress samples. |

Three editable sample textures are in **Assets/Sandouq/Textures/**:

- `Dirt_Dust.png`
- `Dirt_RainStreaks.png`
- `Dirt_Speckles.png`

Their matching assets are in **ScriptableObjects/Dirt/**: `Dirt_Dust`, `Dirt_RainStreaks`, `Dirt_Speckles`. Changing a DirtType affects windows that share it; a window's Texture Override affects just that window. Clear both texture fields to use the procedural fallback.

For your own image, put it under **Assets/Sandouq/Textures/**, disable **sRGB (Color Texture)**, and choose **Wrap Mode: Repeat** when using tiling. White means dirt, black means no dirt. A grayscale coverage image works best; an RGB photo's red channel is used as coverage, not its full color. Dirt color is controlled separately by the DirtType. CPU Read/Write is not needed. Texture/settings edits apply on the next Play session; this prototype does not hot-reload masks during cleaning.

The façade uses explicit saved settings, not runtime randomness: the three dirt types cycle across the panes, tiling and offsets vary, and starting cleanliness ranges from 0% to 54%. This also gives a future multiplayer host a reproducible starting configuration.

## Progress meaning

Every pane owns its own GPU mask and emits progress events. Its world-space label reads those events. CleaningJob computes:

`overall cleanliness = sum(window area × window cleanliness) / sum(window area)`

Larger panes therefore count proportionately more. The overall percentage **includes the starting cleanliness**; it is the current clean state of the building, not just work performed since pressing Play. It intentionally starts above zero in this scene. A window reads CLEAN at its Required Cleanliness threshold (default 95%), but its actual percentage is retained in the aggregate. Expect a short delay of approximately the configured progress interval.

## Multiplayer preparation — still offline

There is no networking package, lobby, transport, RPC, synchronization or multiplayer implementation.

The useful boundaries are now explicit:

- **LocalPlayerRig** on PF_Player owns the `Is Local Player` setting and player ID. `SetLocalPlayer(false)` disables input, movement, camera control, cleaning, button interaction, platform riding, the camera, AudioListener and CharacterController. It leaves a visible avatar that a future adapter can position. The array of local-only behaviours is serialized in the prefab. Configure ownership **before activating** a future remote instance, and assign unique player IDs when adding actual networking.
- **CleaningActionRouter** on the scene's Gameplay object separates `Request(stroke)` from `ApplyAccepted(stroke)`. The stroke contains player/surface IDs, UV endpoints and brush settings. Offline requests apply immediately. A future adapter can disable Apply Requests Locally, subscribe to StrokeRequested, validate requests on the host, and play back accepted strokes.
- **Surface Id** is saved on each CleanableSurface. The router rejects a job configuration with duplicate/empty IDs. Keep these unique when adding windows to the same job. IDs in this scene are W01-01 through W06-04.
- **MovingPlatform** separates `RequestButtonPress(playerId)`, `AdvanceState()` and `Simulate(deltaTime)`. Disabling Simulate Locally stops offline request acceptance and local ticking. A future host can own the state transitions, movement and network replication.
- HUD/camera bindings are explicit. Shared surfaces and the platform do not search for Camera.main, a singleton player, or a global input source.

GPU mask readback remains a local approximate visual measure. A future server must validate reach, rate, tool ownership and resources; assign authority to platform/cleaning state; prevent duplicate/out-of-order stroke application; and provide a snapshot or stroke history for late joiners. GPU results should not be treated as deterministic authoritative network state. Remote collision, remote character interpolation, network platform snapshots and client prediction remain future work.

## New/updated components

| Component | Responsibility |
|---|---|
| DirtMask | Texture override/transform and starting cleanliness, with independent GPU state per window. |
| CleanableSurface / GlassSurface | Stable scene ID and world area in addition to the existing cleaning contract. |
| CleaningJob | Explicit surface list, event subscriptions, area-weighted overall progress. |
| CleaningActionRouter / CleaningStroke | Request vs accepted-action boundary for cleaning. |
| LocalPlayerRig | Local-only input, camera/audio and movement ownership. |
| PlatformRider | Transfers platform displacement to grounded local CharacterControllers. |
| MovingPlatform | Four motion states, speed, travel limits and offline simulation. |
| PlatformButton | Button target, state sign and local player's press request. |
| PlayerButtonInteraction | Aim/reach detection, E/X input, interaction prompt. |
| WindowProgressLabel | Event-driven world-space percentage for one window. |
| CleaningHUD | Existing single-wall binding plus overall-job binding. |
| PrototypeBindings | Connects the scene's job, action router, player, HUD and interaction prompt. |
| SkyscraperBuilder | Editor-only authoring of textures, prefab upgrades, façade, lift and new scene. |
| SkyscraperValidation | Explicit isolated Play Mode smoke tests; never runs automatically in the open editor. |

Script comments explain ownership, event flow, UV mapping, platform riding and other non-obvious choices. Editor generation is split into methods; gameplay responsibilities remain separate small components.

## Prefabs

- **Prefabs/Glass/PF_Window.prefab:** nested original glass/frame prefab, 256-pixel DirtMask and world-space window label.
- **Prefabs/Glass/PF_SkyscraperFacade.prefab:** 24 PF_Window instances with saved IDs/dirt variation; structural floor bands/backing; CleaningJob with all 24 references.
- **Prefabs/Platforms/PF_CleaningPlatform.prefab:** kinematic Rigidbody, MovingPlatform, collidable deck/rails, central pedestal/button and state sign.
- **Prefabs/Player/PF_Player.prefab:** original player plus LocalPlayerRig, PlatformRider and PlayerButtonInteraction; local behaviours and references assigned.
- **Prefabs/UI/PF_CleaningHUD.prefab:** original controls/tool/progress plus interaction prompt.

To make another scene, place the façade, platform, PF_Player and HUD. Add CleaningActionRouter and PrototypeBindings to a scene object and assign their fields explicitly. Keep the platform close enough to the glass for the tool's reach. Use only one locally controlled avatar/camera for this offline prototype.

To add windows, duplicate PF_Window instances, assign unique Surface Id values, configure their DirtMask, and add every surface to CleaningJob's Surfaces list. To adjust the supplied façade permanently, edit its prefab. Its initial builder layout is 4 columns × 6 rows; the builder preserves an existing generated scene rather than rebuilding over edits.

Tune lift **Speed** and **Travel Height** on MovingPlatform. Grounded rider logic assumes a vertically translating platform; rotating lifts need a support-point/rotation extension.

## Performance

Each 256² window owns two mipmapped RGBA8 masks (about 0.67 MiB); all 24 use about 16 MiB of mask memory, plus materials/textures. Only cleaned windows are repainted/read back after initialization. Window labels update on progress events. The initialization readbacks briefly cover all panes; streaming/throttled initialization is an extension for a much larger tower.

The mask progress remains approximate, with linear grayscale textures, RGBA8 quantization and periodic readback. Per-window starting cleanliness is included by reconstructing a fully dirty reference mass before calculating the percentage. Masks reset on a new Play session.
