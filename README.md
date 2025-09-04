<p align="center">
    <img src="Readme~/stimulheat-logo.png" align="center" width="50%">
</p>

<p align="center">
    <h1 align="center">Stimulheat - Unity Package</h1>
</p>

This Unity package provides the libraries and API needed to connect the StimulHeat BLE device to Unity projects. It includes a Windows desktop demo that demonstrates how to establish connections and interact with the device.

⚠️ Note: Android support is currently in development.

## Installation
You can install the package directly via Unity's Package Manager:

1. Open your Unity project.
2. Go to Window > Package Manager.
3. Click the + button in the top-left.
4. Select "Add package from git URL...".
5. Paste the following repository URL and click Add. ```https://gitlab.com/BiomedicalSensors/renforce/stimulheat/stimulheat-unity-scenes```

The package will then appear in your Packages folder under StimulHeat.

## Demo Scene
A demo scene is included to help you quickly test the integration.

### How to Run
1. Navigate to `Packages/StimulHeat/Examples`
2. Open StimulheatDemoScene
3. Enter Play Mode in the Unity Editor

### Configure your devices
1. Turn on your StimulHeat devices
2. Add every the public name of your StimulHeat devices (e.g. 0F6C) by clicking on `+`
3. You can remove a device by clicking on `-`
4. When you have added every device, click on `Finish Configuration`

### Control your devices
1. Select the device you want to control by selecting the according tab
2. Click on the Temp/Flux toggle to change the modality of control
3. Use the slider to select the value
4. Click on Send Command to control the StimulHeat
5. Click on Turn Off if you wish to stop giving thermal control. Be aware that temperature readings are only available when the device is turned on. 

## Integrating StimulHeat in your own scenes
The demo shows the minimal workflow to integrate StimulHeat into a Unity project:

1. Navigate to `Packages/StimulHeat/Prefabs` and add the StimulHeat prefab to your scene. This handles BLE scanning, connecting, and communication.

2. By default, the BLEPlatformSwitch component is disabled, as it needs a list of StimulHeat devices to work. The list in the StimulHeatDevicesManager must be filled before enabling BLEPlatformSwitch (from either a static file or a GUI, the demo scene uses a GUI).

3. Use BLEPlatformSwitch methods to control your StimulHeat devices.

## License

This software source code is licensed under the terms of the GNU General Public License v3 (GPLv3).
You are free to use, study, modify, and distribute it under the same license.
SPDX-License-Identifier: [GPL-3.0-only](https://opensource.org/license/gpl-3-0)

🔒 For commercial use without open hardware obligations, a separate commercial license may be available.
For more details, see the [License](LICENSE.md) file.

---

## Acknowledgments

The main contributors to this project are Matthieu Mesnage and Bertrand Massot from INSA Lyon, CNRS, INL UMR 5270, Villeurbanne, France.

The main developer of this Unity Package is Sophie Villenave, from Ecole Centrale de Lyon, CNRS, LIRIS UMR 5205, Saint-Etienne, France.

This work was supported by the **French National Research Agency (ANR)** under grant **ANR-22-CE31-0023-03 RENFORCE**.

---

## Contact

[bertrand.massot@insa-lyon.fr](mailto:bertrand.massot@insa-lyon.fr)

---
