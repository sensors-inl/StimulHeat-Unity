using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal StimulHeat demo using the new shared BLE bridge.
///
/// Requires the com.lirisxr.blebridge + fr.renforce.stimulheat packages.
/// Windows/Editor: builds for x64, Windows 10+ with Bluetooth on. Android:
/// the bridge auto-switches to its Velorexe-based backend; no manual define needed.
///
/// This is a self-contained version of the package's Samples~/Demo/DemoGUI: it
/// configures StimulHeat device name(s), then drives temperature / heat-flux via
/// BLEPlatformSwitch, which in turn talks to StimulHeatDevicesManagerWindows ->
/// BleConnectionManager (com.lirisxr.blebridge). No manual GameObject wiring
/// needed beyond dragging this component onto one GameObject with a child object
/// named "BleWindows" (the BLEPlatformSwitch references it).
///
/// Quick setup:
///   1. Create a GameObject "StimulHeatRoot", add this component.
///   2. Create a child GameObject "BleWindows", disable it in the Inspector.
///   3. Assign the BleWindows object to this component's blePlatformWindows field.
///   4. Press Play; add device advertised names (e.g. "0F6C") and click Configure.
/// </summary>
public class StimulHeatBleDemo : MonoBehaviour
{
    [Tooltip("Child GameObject that BLEPlatformSwitch will activate and attach the Windows manager to. Disable it in the Inspector.")]
    public GameObject blePlatformWindows;

    private const int MaxDevices = 8;
    private bool configurationComplete;
    private int currentTab;

    private BLEPlatformSwitch _blePlatformSwitch;

    // UI-only state per device
    private class DeviceUIState
    {
        public List<string> logs = new();
        public Vector2 scrollPos;
        public bool tempMode = true;
        public float tempValue = 32f;
        public float fluxValue;
        public float measuredTemperature;
    }

    private readonly Dictionary<string, DeviceUIState> uiStates = new();

    private void Start()
    {
        // The setup panel reads StimulheatDevicesManager.Devices (a static singleton), so the
        // manager must exist before OnGUI runs. Create it here and seed one default device.
        if (StimulheatDevicesManager.Instance == null)
        {
            var mgrGo = new GameObject("StimulHeatDevicesManager");
            mgrGo.AddComponent<StimulheatDevicesManager>();
        }
        if (StimulheatDevicesManager.Devices.Count == 0)
        {
            StimulheatDevicesManager.Devices.Add(new StimulHeatDevice
            {
                deviceName = "0F6C",
                connected = false
            });
        }
    }

    private void Update()
    {
        if (!configurationComplete) return;

        foreach (var dev in StimulheatDevicesManager.Devices)
        {
            if (!uiStates.ContainsKey(dev.deviceName)) continue;
            var ui = uiStates[dev.deviceName];
            if (dev.connected) ui.measuredTemperature = dev.temperature;
        }
    }

    private void OnGUI()
    {
        if (!configurationComplete) DrawSetupPanel();
        else DrawTabbedPanel();
    }

    // ---------------- SETUP PANEL ----------------
    private void DrawSetupPanel()
    {
        GUILayout.BeginVertical("box", GUILayout.Width(420));
        GUILayout.Label("StimulHeat BLE Demo  (fr.renforce.stimulheat + blebridge)");

        if (blePlatformWindows == null)
        {
            GUILayout.Label("Assign the 'BleWindows' child object to blePlatformWindows in the Inspector.");
            GUILayout.EndVertical();
            return;
        }

        GUILayout.Label("Device Setup (max " + MaxDevices + ")");
        for (int i = 0; i < StimulheatDevicesManager.Devices.Count; i++)
        {
            var dev = StimulheatDevicesManager.Devices[i];
            GUILayout.BeginHorizontal();
            dev.deviceName = GUILayout.TextField(dev.deviceName, GUILayout.Width(250));
            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
                StimulheatDevicesManager.Devices.RemoveAt(i);
                uiStates.Remove(dev.deviceName);
                break;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        if (StimulheatDevicesManager.Devices.Count < MaxDevices)
        {
            if (GUILayout.Button("+ Add Device"))
            {
                var newDevice = new StimulHeatDevice
                {
                    deviceName = "Device" + (StimulheatDevicesManager.Devices.Count + 1),
                    connected = false
                };
                StimulheatDevicesManager.Devices.Add(newDevice);
                uiStates[newDevice.deviceName] = new DeviceUIState();
            }
        }

        GUILayout.Space(20);
        if (StimulheatDevicesManager.Devices.Count > 0 && GUILayout.Button("Configuration Complete"))
        {
            configurationComplete = true;
            currentTab = 0;
            // Wire the platform switch; the manager singleton already exists (created in Start).
            _blePlatformSwitch = gameObject.AddComponent<BLEPlatformSwitch>();
            _blePlatformSwitch.blePlatformWindows = blePlatformWindows;
            _blePlatformSwitch.enabled = true;
        }
        GUILayout.EndVertical();
    }

    // ---------------- TABBED PANEL ----------------
    private void DrawTabbedPanel()
    {
        GUILayout.BeginVertical("box", GUILayout.Width(500));
        GUILayout.BeginHorizontal();
        for (int i = 0; i < StimulheatDevicesManager.Devices.Count; i++)
        {
            var dev = StimulheatDevicesManager.Devices[i];
            if (GUILayout.Toggle(currentTab == i, dev.deviceName, "Button", GUILayout.Width(100)))
                currentTab = i;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        if (currentTab >= 0 && currentTab < StimulheatDevicesManager.Devices.Count)
        {
            var dev = StimulheatDevicesManager.Devices[currentTab];
            DrawDeviceControl(dev);
        }
        GUILayout.EndVertical();
    }

    private void DrawDeviceControl(StimulHeatDevice dev)
    {
        uiStates.TryAdd(dev.deviceName, new DeviceUIState());
        var ui = uiStates[dev.deviceName];

        GUILayout.Label("Device: " + dev.deviceName);
        GUILayout.Label("Status: " + (dev.connected ? "Connected" : "Disconnected"));
        GUILayout.Label("Measured: " + ui.measuredTemperature.ToString("F1") + " °C");
        GUILayout.Space(10);

        ui.tempMode = GUILayout.Toggle(ui.tempMode, ui.tempMode ? "Temperature Mode" : "Flux Mode");
        GUILayout.BeginHorizontal();
        if (ui.tempMode)
        {
            ui.tempValue = GUILayout.HorizontalSlider(ui.tempValue, 28f, 42f, GUILayout.Width(200));
            GUILayout.Label(ui.tempValue.ToString("F1") + " °C", GUILayout.Width(80));
        }
        else
        {
            ui.fluxValue = GUILayout.HorizontalSlider(ui.fluxValue, -4f, 4f, GUILayout.Width(200));
            GUILayout.Label(ui.fluxValue.ToString("F2"), GUILayout.Width(80));
        }

        if (GUILayout.Button("Send Command", GUILayout.Width(120)))
        {
            if (dev.connected)
            {
                if (ui.tempMode)
                {
                    Log(ui, "Sent temperature setpoint: " + ui.tempValue.ToString("F1") + " °C");
                    _blePlatformSwitch.SendTemperatureCommand(dev.deviceId, ui.tempValue);
                }
                else
                {
                    Log(ui, "Sent flux value: " + ui.fluxValue.ToString("F2"));
                    _blePlatformSwitch.SendHeatFlowCommand(dev.deviceId, ui.fluxValue);
                }
            }
            else Log(ui, "Cannot send command: Not connected.");
        }

        if (GUILayout.Button("Turn Off", GUILayout.Width(120)))
        {
            if (dev.connected)
            {
                Log(ui, "Sent Off Command. Temperature readings won't be available until turning StimulHeat on again.");
                _blePlatformSwitch.SendOffCommand(dev.deviceId);
            }
            else Log(ui, "Cannot send command: Not connected.");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("Log:");
        ui.scrollPos = GUILayout.BeginScrollView(ui.scrollPos, GUILayout.Height(120));
        foreach (var line in ui.logs) GUILayout.Label(line);
        GUILayout.EndScrollView();
    }

    private static void Log(DeviceUIState ui, string msg)
    {
        ui.logs.Add(msg);
        if (ui.logs.Count > 50) ui.logs.RemoveAt(0);
        ui.scrollPos.y = float.MaxValue;
    }
}
