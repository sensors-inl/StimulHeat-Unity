using Stimulheat.Bluetooth;
using UnityEngine;
using Lirisxr.BleBridge;

/// <summary>
/// Bridges the platform-specific device manager with the rest of the app.
///
/// In the BleBridge refactor, the actual BLE connection is owned by
/// StimulHeatDevicesManagerWindows -> BleConnectionManager (com.lirisxr.blebridge), which
/// auto-switches its native backend per build target:
///   - Windows/Editor: BleWinrtDll (background polling thread)
///   - Android: Velorexe Unity-Android-Bluetooth-Low-Energy (event-driven MonoBehaviour)
/// So this single manager class works on both platforms unchanged; this component stays
/// the public command surface the demo/UI talks to, exactly as before.
/// </summary>
public class BLEPlatformSwitch : MonoBehaviour
{
    public GameObject blePlatformWindows;
    private StimulHeatDevicesManagerWindows _stimulHeatDevicesManagerWindows;
    public bool isScanning, isConnected;
    public bool init;

    public void Start()
    {
        blePlatformWindows.SetActive(true);
        _stimulHeatDevicesManagerWindows = blePlatformWindows.AddComponent<StimulHeatDevicesManagerWindows>();
    }

    private void Update()
    {
        isScanning = _stimulHeatDevicesManagerWindows.isScanning;
        isConnected = _stimulHeatDevicesManagerWindows.isConnected;
        if (isConnected && !init)
        {
            init = true;
            foreach (var device in StimulheatDevicesManager.Devices)
            {
                SendOffCommand(device.deviceId);
            }
        }
    }

    public void SendThermalComfortCommand(CommandIntensity commandIntensity, bool commandEnabled = true)
    {
        if (!isConnected) return;
        if (StimulheatDevicesManager.Devices.Count == 0)
        {
            Debug.LogWarning("No device to update.");
            return;
        }
        foreach (var device in StimulheatDevicesManager.Devices)
        {
            device.UpdateCommandEnslavement(EnslavementType.Heat);
            device.UpdateCommandIntensity(commandIntensity, commandEnabled);
        }
    }

    public void SendCommand(string deviceId, float value, EnslavementType type, bool enabled = true)
    {
        if (!isConnected) return;
        if (StimulheatDevicesManager.Devices.Count == 0)
        {
            Debug.LogWarning("No device to update.");
            return;
        }
        if (type == EnslavementType.Heat)
        {
            if (value is < -5f or > 5f)
            {
                Debug.LogWarning("Invalid heat flow intensity value. Must be between -5 and 5.");
                return;
            }
        }
        else if (type == EnslavementType.Temp)
        {
            if (value is < 15f or > 42f)
            {
                Debug.LogWarning("Invalid temperature value. Must be between 15 and 42.");
                return;
            }
        }
        foreach (var device in StimulheatDevicesManager.Devices)
        {
            if (device.deviceId != deviceId) continue;

            device.UpdateCommandEnslavement(type);
            device.UpdateCommandValue(value, enabled);
        }
    }

    public void SendHeatFlowCommand(string deviceId, float heatFlowIntensity, bool commandEnabled = true)
    {
        SendCommand(deviceId, heatFlowIntensity, EnslavementType.Heat, commandEnabled);
    }

    public void SendTemperatureCommand(string deviceId, float temperature, bool commandEnabled = true)
    {
        SendCommand(deviceId, temperature, EnslavementType.Temp, commandEnabled);
    }

    public void SendNeutralHeatCommand(string deviceId)
    {
        SendCommand(deviceId, 0, EnslavementType.Heat);
    }

    public void SendNeutralTemperatureCommand(string deviceId)
    {
        SendCommand(deviceId, 32, EnslavementType.Temp);
    }

    public void SendOffCommand(string deviceId)
    {
        SendCommand(deviceId, 0, EnslavementType.Heat, false);
    }

    [ContextMenu("VeryHotThermalComfortCommand")]
    public void VeryHotThermalComfortCommand()
    {
        SendThermalComfortCommand(CommandIntensity.VeryHot);
    }

    [ContextMenu("NeutralThermalComfortCommand")]
    public void NeutralThermalComfortCommand()
    {
        SendThermalComfortCommand(CommandIntensity.Neutral);
    }

    [ContextMenu("VeryColdThermalComfortCommand")]
    public void VeryColdThermalComfortCommand()
    {
        SendThermalComfortCommand(CommandIntensity.VeryCold);
    }
}
