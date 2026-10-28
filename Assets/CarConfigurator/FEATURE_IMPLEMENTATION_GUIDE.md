# Mobile Car Configurator — Feature Controller & UI Setup

Target: Unity 6000.3, URP, Android/iOS. Showroom scene: `Assets/Scenes/3D Car Configurator.unity`. AR scene: `Assets/Scenes/AR_Scene.unity`.

## One-click scaffold (run separately in each scene)

1. Pull the repository and wait for Unity to compile scripts.
2. Open your Showroom scene. Choose **Tools > Car Configurator > Create Missing UI + Assignment Slots**.
3. This adds (only if absent): an **empty** `CarConfiguratorSystem` manager with scripts, a hidden `Canvas/CarFeaturePanel`, a top-right `FEATURES` opener, an offline typed-command field with **RUN COMMAND**, scrollable option buttons, `PriceBreakdown` text, and a `FeatureAssignmentSlots` hierarchy.
4. Creates six editable URP material presets in `Assets/CarConfigurator/Generated/Materials/` (no existing `.mat` is overwritten).
5. Repeat in `AR_Scene`. The AR UI contains all exterior and cabin configuration options but **no seat/camera navigation buttons**.
6. Existing `Canvas/MenuBand` icons and callbacks are preserved. The new panel is an **additional UI**, not a destructive replacement.

This command can be run again: existing UI and assignment entries are preserved. Undo works for newly created scene objects, while `.mat` assets remain in the Project folder.

## Assign references (critical)

- `CarConfiguratorManager`: `bodyRenderers` (paint-only materials), `seatRenderers`, `upholsteryRenderers`, `glossyPaint`, `metallicPaint`, `mattePaint`, `fabricMaterial`, `leatherMaterial`, `premiumLeatherMaterial`. Generated materials are **examples** and require manual assignment.
- `CarConfiguratorManager.parts`: each optional part gets an ID and an empty `root` slot, e.g. `body_kit`, `spoiler`, `roof_rail`, `badge`, `chrome`, `cladding`, `door_visor`, `headlight_upgrade`, `panoramic_roof`, `fog_lamp_trim`, `side_steps`, `sport_diffuser`. Drag the correct actual mesh root into the `root` field. Missing roots are intentionally rejected rather than faking an enabled accessory.
- `CarConfiguratorManager.wheels`, `.grilles`, `.dashboardTrims`, `.steeringWheels`: ID entries are created with `root` = None. Assign each alternative model/mesh (including `standard` where needed). Style selection is rejected until its actual model is assigned.
- `VehicleMotionController.movingParts`: slots for `front_left_door`, `front_right_door`, `rear_left_door`, `rear_right_door`, and four windows. Assign the correct hinged/sliding `pivot`. Configure `openEulerOffset` for doors and `openLocalOffset` for window glass; the sample window distance (-0.4 local Y) must be checked on your FBX.
- `VehicleLightsAndSounds`: assign `Light` arrays or emissive GameObjects and AudioSource/AudioClips for the horn, locks, indicators.
- `CarCabinControls`: Sunroof Animator with an `Open` bool parameter, AudioSources for music and engine, ambient light objects and AC indicator.
- `ShowroomFeatureController`: Infotainment panels, premium/basic audio roots, analog/digital cluster roots, wireless charger, AC temperature/fan TMP labels.
- `CarInteriorAppearanceController`: assign **verified** seat and back-seat renderer/material slots, cabin lights. Splitting a combined interior mesh/material may require FBX edits.
- `CarSeatNavigator` + `MobileShowroomOrbit`: camera rig, exterior/seat anchors, showroom camera orbit target; both are **Showroom only**.
- `CarShowcaseServices`: day/night environment objects, `PartInformation` records, info/price TMP labels. `ShowroomPartPicker` uses colliders + `PartPickTarget` for tappable part highlights.
- `CarConfiguratorFeatureActions` must point to the corresponding car controllers. In a showroom system it auto-finds attached controllers; in AR it rebinds when the car spawns if `ARCarPlacementManager` is in use.

**Important:** `FeatureAssignmentSlots` only contains empty organizational references. It does not generate realistic car accessories, lights, seats or animations from the FBX. Assign real objects from your model. Do not drag a placeholder into a part slot if it has no mesh.

## Inspector button methods

`CarConfiguratorFeatureActions` is the common callable controller. Generated buttons contain `CarFeatureOptionButton`, which serializes `command` + `option` and calls it automatically. You can also assign these methods directly in your own Unity Button `OnClick`:

- `ChangeBodyToRed`, `ChangeBodyToBlue`, `ChangeBodyToWhite`
- `ToggleHeadlights`, `ToggleMusic`, `ToggleAC`, `ToggleSunroof`, `ToggleDayNight`
- `SaveConfiguration`, `LoadConfiguration`, `ResetConfiguration`, `ShareConfiguration`
- `ExecuteFromUI(string action)` for no-value actions such as `honk`, `orbit_auto`, `price`, `all_doors_open`, `all_doors_close`
- `VehicleMotionController.OpenFrontLeftDoor`, `.CloseFrontLeftDoor`, `.OpenAllDoors`, `.CloseAllDoors` and corresponding right/rear door methods

For options that need a value use a `CarFeatureOptionButton` component (e.g. `body_color` + `blue`, `seat_color` + `brown`, `variant` + `Top`, `part_toggle` + `roof_rail`, `wheel` + `alloy1`). Each button highlights only when selected. Toggling a part with no assigned mesh returns false and stays unselected.

## AI text commands

`SimpleTextCommandAdapter.Submit(string phrase)` understands a limited set of English text commands, for example:

- `change car color blue`, `change rear seat color brown`, `make dashboard color black`
- `open driver door`, `close rear right door`, `open all doors`
- `set matte paint`, `top model`, `turn headlights on`, `turn music on`
- `go inside the car` (Showroom only; **rejected in AR**)

The adapter is **not** speech recognition or a cloud LLM. A microphone speech-to-text provider can pass transcribed text into `Submit`, or an AI service can send a validated `{ "action": "feature", "target": "body_color", "value": "blue" }` payload to `AICommandExecutor`. No API keys are stored in this repository.

The generated **FEATURES** panel includes a basic text field and **RUN COMMAND** button so you can test these commands immediately. This proves the command routing but does not imply that a speech-recognition or LLM service has been connected.

## Feature boundaries and current limitations

- **No bonnet open, boot open or mirror-fold functions**: deliberately excluded based on the approved UI update.
- AR: car placement and configuration, including cabin materials/controls; no player teleport or seat navigation.
- Mobile AR: wire the `ARCarPlacementManager` prefab, AR Raycast Manager, XR Origin and AR-compatible scene camera in Unity; test on a supported Android/iOS device.
- 360 auto-rotation / zoom: requires assigning `MobileShowroomOrbit` target and camera in Showroom.
- Price breakdown uses **demo prices** until you enter your variant and accessory prices in the Inspector.
- Share currently copies configuration JSON to clipboard; a native iOS/Android share sheet needs a separate plugin/integration.
- Sunroof, wheels, doors, windows, audio and lights do nothing visible until actual meshes, pivots, materials, Animator and sound clips are assigned.
- Unity Editor/device builds were **not run** in this environment. Test script compilation and behavior before shipping.

## Recommended quick test

1. Run one-click scaffold, press **Play**, tap **FEATURES**. Confirm scroll/buttons and selected-state colors.
2. Assign body renderer with paint-only slot and `glossyPaint` / `metallicPaint` / `mattePaint`; change paint and colors.
3. Assign one optional part + one alloy mesh. Verify selecting them activates only the correct GameObject.
4. Assign door pivots/offsets; test four door buttons (no bonnet/boot/mirror fold).
5. Change interior colors, AC and infotainment. Save, exit Play, re-enter; verify saved configuration + highlighted option state.
6. Build/test AR on Android/iOS. Confirm interior appearance controls work but seat navigation is unavailable.
