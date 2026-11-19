# StimulHeat (`fr.renforce.stimulheat`)

Unity package to connect and command **StimulHeat** thermal devices (heat / heat-flow wristbands)
over BLE. Includes a desktop demo and XR Interaction Toolkit VR samples that emit thermal
stimuli on grab.

## Relation to the BLE Bridge

This package does **not** talk to BLE hardware itself. All BLE is wired through the shared
**[`com.lirisxr.blebridge`](https://github.com/liris-xr/multisensory-bridge)** package,
which this package depends on (`com.lirisxr.blebridge` is declared in `package.json`, so UPM
installs it automatically).

How the two relate:

- **The bridge owns the native stack.** `StimulHeatDevicesManagerWindows` requests an
  `IBleLink` from the bridge's `BleConnectionManager` with a `BleDeviceProfile` (advertised
  device name + service/characteristic UUIDs). Scanning, connecting, subscribing and writing
  are all done by the bridge.
- **No native DLL shipping.** StimulHeat ships no `BleWinrtDll.dll` and no Android jar, and
  never calls the native `Quit()`. Before the bridge existed, StimulHeat and Nebula each bound
  the same Windows BLE library and could not coexist in one project (duplicate DLL, and one
  package's `Quit()` tore the other's connection down). The bridge is now the single owner, so
  StimulHeat and Nebula can be used together.
- **Automatic platform coverage.** Because the bridge picks its backend from the build target
  (Windows/Editor: `BleWinrtDll`; Android: Velorexe), StimulHeat works on Windows and Android
  unchanged — `BLEPlatformSwitch` is the same public command surface on both platforms.
- **Shared scan.** The bridge runs one scan and routes devices by name + service UUID, so
  StimulHeat devices are discovered alongside Nebula's without competing scanners.

If you use this package, keep the bridge installed; see the bridge repo for platform setup
(Android permissions manifest wizard, entry point, etc.).

## Installation

Unity 2022.3+. Add this package via the Unity Package Manager (git URL or local path). The BLE
bridge dependency is pulled in automatically.

## Usage

`BLEPlatformSwitch` is the main component: add it to the scene with a child object assigned to
`blePlatformWindows` (the child gets the device manager added at runtime). It exposes the
command surface:

```csharp
// per-device commands
blePlatformSwitch.SendTemperatureCommand(deviceId, 40f);   // target temperature (°C)
blePlatformSwitch.SendHeatFlowCommand(deviceId, 1.5f);      // heat-flow intensity
blePlatformSwitch.SendOffCommand(deviceId);                 // stop all outputs

// comfort presets applied to every connected device
blePlatformSwitch.VeryHotThermalComfortCommand();
blePlatformSwitch.VeryColdThermalComfortCommand();
blePlatformSwitch.NeutralThermalComfortCommand();
```

Connected devices are listed in `StimulheatDevicesManager.Devices` (each a `StimulHeatDevice`
with its `deviceId`).

## Samples

Import from the Package Manager ("Import samples" on StimulHeat):

- **Demo** (`Samples~/Demo`) — `StimulHeatBleDemo.cs`, a desktop OnGUI demo for temperature /
  heat-flux control via `BLEPlatformSwitch`. Works on Windows and Android unchanged.
- **VR Demo** (`Samples~/VR`) — XR Interaction Toolkit scripts that send a thermal command on
  grab and stop on release. Requires `com.unity.xr.interaction.toolkit`.
  - `StimulHeatVRSettings` — scene component. Set the `Enslavement Type`, `Hands Used`
    (Left/Right/Both), and the `Left/Right Device Name` (the BLE advertised names). On `Start`
    it registers the devices with `StimulheatDevicesManager` and enables `BLEPlatformSwitch`.
  - `SendThermalOnSelect` — put on each XR controller's interactor. `Hand Side` selects which
    bound device receives the command. On `selectEntered` it scans the grabbed object for
    `ThermalStimuli` components and sends `BLEPlatformSwitch.SendCommand(deviceId, value, type)`;
    on `selectExited` it sends a stop (value 0, enabled false).
  - `ThermalStimuli` — put on a grabbable interactable. `Thermal Value` + `Type` define what the
    StimulHeat device emits while the object is held.

VR setup:
1. Install `com.unity.xr.interaction.toolkit`.
2. In the scene, add `BLEPlatformSwitch` (disabled) and `StimulHeatVRSettings` configured as above.
3. On each XR controller interactor, add `SendThermalOnSelect` with the matching `Hand Side`.
4. On each grabbable object that should emit heat, add a `ThermalStimuli` component.
5. Run on Android (or Windows): the bridge connects to the devices named in the settings, and
   grabbing a `ThermalStimuli` object fires the command on the bound hand's device.

## Contact

Bertrand Massot - bertrand.massot@insa-lyon.fr
Sophie Villenave - sophie.villenave@ec-lyon.fr
