# Car Configurator UI Integration — 27 Oct 2026

Target scene: `Assets/Scenes/3D Car Configurator.unity` (Unity 6000.3.8f1).

## What changed

- Reused the **existing** `Canvas/MenuBand` hierarchy and images. No UI objects were deleted or renamed.
- Attached `CarConfiguratorUIController` and `CarInteriorAppearanceController` to the **empty** `CarConfiguratorSystem` parent. They are not attached to the model itself.
- The UI controller rebinds supported buttons at runtime, disabling the old persistent callbacks on those same buttons, many of which point at absent scripts.
- Removed mirror fold, bonnet open and boot open APIs/AI actions from the shared control scripts. Four-door control and separate window motion hooks remain.
- Preserved AR rule: cabin customizations are available, but AR camera/seat teleporting is disabled.

## Existing button paths supported

### Exterior
`MenuBand/ExteriorMenu/{Colour,Alloy,Chrome,Cladding,Door-Visor,Roof-Rail,OpenDoor,TestDrive}`

- Color selectors: `MenuBand/ColourSubMenu/{Orange,Black,yellow,Blue,White}`
- Wheels: `MenuBand/Alloy/{Alloy1,Alloy2}`
- `OpenDoor` is the old GameObject name; its **new behavior is Close All Doors**, matching the visible "CLOSE DOOR" icon.
- `TestDrive` logs a pending-feature message until the driving mode is agreed.

### Interior
`MenuBand/Interiormenu/{DashBoard,FrontSeat,BackSeat,SeatCover,Door-Colour,CabinHeight,AmbientLight,Music}`

- Dashboard colors: `MenuBand/DashBoard/{Dashboard-white,Dashboard-leather,Dashboard-premium}`
- Front/back seat color and cover controls: `MenuBand/Seat/{Seat-white,Seat-leather,Seat-premium}`
- Door trim: `MenuBand/Door/{DoorWhite,DoorLeather}`
- Ambient: `MenuBand/AmbienLight/{Pink,Red,Green,Purple,Blue,NoColor}`
- Sidebar: `SideMenu/{Reset,MusicOnOff,Share (1)}`

## Asset-specific work remaining in Inspector

1. `CarConfiguratorManager.wheels` needs **two actually distinct wheel model options** (`alloy1`, `alloy2`); the UI logs a warning until these exist.
2. `CarConfiguratorManager.parts` needs `chrome`, `cladding`, `door_visor`, and `roof_rail` entries with model GameObject references. Do not toggle a whole car root instead of a missing mesh.
3. `CarConfiguratorManager` paint/seat material slots and `CarInteriorAppearanceController.separatelyModeledRearSeats` need review. The FBX may use combined interiors; rear-seat color is saved but visually independent only when rear renderer/material is assigned.
4. `CarInteriorAppearanceController` colors dashboard/seat/door **material slots** by their existing material names, to avoid changing the whole interior mesh. Material name conventions must be checked if the model changes.
5. Assign `CarInteriorAppearanceController.cabinAmbientLights` for colored ambient lighting. Assign audio clips and AudioSources on `CarCabinControls` for music and other sounds.
6. Four door motion references are present but the current `VehicleMotionController.openEulerOffset` values in the scene are all zero. Verify hinge positions and use real angles before door movement will be visible.
7. Set up `MobileShowroomOrbit` / 360 controls separately; its UI button currently logs that camera orbit is not yet configured. Native share sheet and test-drive simulation are also pending.
8. AR scene still needs UI/controller connections; this change integrates the new **3D Showroom** Canvas only. The AR camera should remain under AR Foundation tracking and never move to a seat anchor.

## Quick Unity QA

1. Open `3D Car Configurator.unity`. In Play Mode, Console should report the number of existing buttons bound by `CarConfiguratorUIController`.
2. Click the visible Interior/Exterior switch both ways; confirm submenu changes and original imagery stay intact.
3. Click each exterior paint swatch; confirm the body changes. Switch to dashboard/seat/door material options and inspect intended material slots.
4. Check warnings for missing wheel/accessory variants rather than silent success.
5. Press `CLOSE DOOR`: controller calls `SetAllDoors(false)`. With 0° door offsets the doors won't visibly animate until pivots/angles are configured.
6. Reset configuration; verify saved color/options restore defaults. Copy share JSON to clipboard.
7. Test on Android/iOS devices before release. No Unity Editor/device runtime was available to execute this QA automatically.

This repository update intentionally avoids rewriting the large Canvas prefab or overwriting source FBX assets.
