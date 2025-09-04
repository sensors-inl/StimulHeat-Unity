using UnityEngine;
using System.Collections.Generic;

public class DemoGUI : MonoBehaviour
{
    private const int MaxDevices = 8;

    // Phase control
    private bool configurationComplete;

    // Tab management
    private int currentTab;

    // UI-only state per device
    private class DeviceUIState
    {
        public bool settingsValidated = false;
        public List<string> logs = new ();
        public Vector2 scrollPos;

        public bool tempMode = true;
        public float tempValue = 32f;
        public float fluxValue;

        public float measuredTemperature;
    }

    private Dictionary<string, DeviceUIState> uiStates = new ();

    private BLEPlatformSwitch _blePlatformSwitch;
    
    void Update()
    {
        if (!configurationComplete) return;
        
        foreach (var dev in StimulheatDevicesManager.Devices)
        {
            if (!uiStates.ContainsKey(dev.deviceName)) continue;
            var ui = uiStates[dev.deviceName];

            if (dev.connected)
            {
                ui.measuredTemperature = dev.temperature;
            }
        }
    }

    void OnGUI()
    {
        if (!configurationComplete)
        {
            DrawSetupPanel();
        }
        else
        {
            DrawTabbedPanel();
        }
    }

    // ---------------- SETUP PANEL ----------------
    private void DrawSetupPanel()
    {
        GUILayout.BeginVertical("box", GUILayout.Width(420));
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

            _blePlatformSwitch = FindFirstObjectByType<BLEPlatformSwitch>();
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
            {
                currentTab = i;
            }
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
            else
            {
                Log(ui, "Cannot send command: Not connected.");
            }
        }
        
        if (GUILayout.Button("Turn Off", GUILayout.Width(120)))
        {
            if (dev.connected)
            {
                Log(ui, "Sent Off Command. Temperature readings won't be available until turning StimulHeat on again.");
                _blePlatformSwitch.SendOffCommand(dev.deviceId);
            }
            else
            {
                Log(ui, "Cannot send command: Not connected.");
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.Label("Measured Temperature: " + ui.measuredTemperature.ToString("F1") + " °C");

        GUILayout.Space(10);

        GUILayout.Label("Console:");
        ui.scrollPos = GUILayout.BeginScrollView(ui.scrollPos, GUILayout.Height(150));
        foreach (string log in ui.logs)
        {
            GUILayout.Label(log);
        }
        GUILayout.EndScrollView();
    }

    private void Log(DeviceUIState ui, string message)
    {
        ui.logs.Add(System.DateTime.Now.ToString("HH:mm:ss") + " - " + message);
        if (ui.logs.Count > 100)
        {
            ui.logs.RemoveAt(0);
        }
    }
}
