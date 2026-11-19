using System.Collections.Generic;
using UnityEngine;
using Stimulheat.Bluetooth;

/// <summary>
/// VR setup for StimulHeat: binds one StimulHeat device per hand, registers them with the
/// shared StimulheatDevicesManager (so the BleBridge-backed BLEPlatformSwitch discovers and
/// connects them), and exposes a hand->device map that SendThermalOnSelect reads on grab.
///
/// Setup:
///   1. Create a "StimulHeatVR" GameObject and add this component.
///   2. Set leftDeviceName / rightDeviceName to the devices' advertised BLE names.
///   3. Pick handsUsed (Left / Right / Both) to decide which hands get a device.
///   4. Place a BLEPlatformSwitch component in the scene on a GameObject with a child
///      "BleWindows" assigned to its blePlatformWindows field (disabled in the Inspector).
///      This component will enable it; BLEPlatformSwitch.Start then activates the child and
///      attaches StimulHeatDevicesManagerWindows, which requests BLE links via the bridge.
///
/// The BLEPlatformSwitch API is unchanged from the original StimulHeat package; this VR sample
/// only depends on StimulheatDevicesManager.Devices, StimulHeatDevice, EnslavementType, and
/// BLEPlatformSwitch.SendCommand, all of which the bridge-backed package still provides.
/// </summary>
public class StimulHeatVRSettings : MonoBehaviour
{
    [Tooltip("Default enslavement type applied to devices (overridden per ThermalStimuli).")]
    public EnslavementType enslavementType;

    [Tooltip("Which hands have a StimulHeat device bound to them.")]
    public HandSide handsUsed;

    [Tooltip("Advertised BLE name of the device worn on the left hand.")]
    public string leftDeviceName;

    [Tooltip("Advertised BLE name of the device worn on the right hand.")]
    public string rightDeviceName;

    /// <summary>Hand -> bound StimulHeat device. Read by SendThermalOnSelect on grab.</summary>
    public readonly Dictionary<HandSide, StimulHeatDevice> stimulHeatDevicesVR = new();

    private BLEPlatformSwitch _blePlatformSwitch;

    private void Start()
    {
        // StimulheatDevicesManager.Devices is a static accessor that returns the singleton's
        // device list; the singleton must exist before we add to it. The desktop demo creates it
        // the same way. Awake on StimulheatDevicesManager is a DontDestroyOnLoad singleton, so a
        // second instance in the scene is harmless.
        if (StimulheatDevicesManager.Instance == null)
        {
            var mgrGo = new GameObject("StimulHeatDevicesManager");
            mgrGo.AddComponent<StimulheatDevicesManager>();
        }

        var leftDevice = new StimulHeatDevice { deviceName = leftDeviceName };
        var rightDevice = new StimulHeatDevice { deviceName = rightDeviceName };

        switch (handsUsed)
        {
            case HandSide.Left:
                StimulheatDevicesManager.Devices.Add(leftDevice);
                stimulHeatDevicesVR.Add(HandSide.Left, leftDevice);
                break;
            case HandSide.Right:
                StimulheatDevicesManager.Devices.Add(rightDevice);
                stimulHeatDevicesVR.Add(HandSide.Right, rightDevice);
                break;
            case HandSide.Both:
                StimulheatDevicesManager.Devices.Add(leftDevice);
                StimulheatDevicesManager.Devices.Add(rightDevice);
                stimulHeatDevicesVR.Add(HandSide.Left, leftDevice);
                stimulHeatDevicesVR.Add(HandSide.Right, rightDevice);
                break;
        }

        // Enable the BLEPlatformSwitch so its Start() activates the BleWindows child and attaches
        // the bridge-backed StimulHeatDevicesManagerWindows, which requests one IBleLink per device.
        _blePlatformSwitch = FindFirstObjectByType<BLEPlatformSwitch>();
        if (_blePlatformSwitch == null)
        {
            Debug.LogError("[StimulHeatVR] No BLEPlatformSwitch found in scene. Add one on a GameObject with a BleWindows child assigned to blePlatformWindows.");
            return;
        }
        _blePlatformSwitch.enabled = true;
    }
}
