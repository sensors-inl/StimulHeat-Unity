using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Lirisxr.BleBridge;

/// <summary>
/// Drives one or more StimulHeat devices over BLE.
///
/// This is the BleBridge-backed replacement for the original StimulHeatDevicesManagerWindows.
/// It no longer imports BleWinrtDll or manages its own scan/connect/quit threads. Instead it
/// requests one IBleLink per configured device from the shared BleConnectionManager
/// (com.lirisxr.blebridge), which owns the single native BLE stack and lets StimulHeat and
/// Nebula coexist in the same project without DLL or Quit() conflicts.
///
/// StimulHeat uses the Nordic UART-like service 6E400001:
///   - 6E400003: notify (incoming measurements)
///   - 6E400002: write  (outgoing COBS-encoded protobuf commands)
/// </summary>
public class StimulHeatDevicesManagerWindows : MonoBehaviour
{
    public bool isScanning => _isScanning;
    public bool isConnected => _isConnected;

    // Nordic UART-like service. Full 128-bit UUIDs; BleBridge matches them as substrings.
    private const string ServiceUuid = "{6E400001-B5A3-F393-E0A9-E50E24DCCA9E}";
    private const string NotifyCharacteristicUuid = "{6E400003-B5A3-F393-E0A9-E50E24DCCA9E}";
    private const string WriteCharacteristicUuid = "{6E400002-B5A3-F393-E0A9-E50E24DCCA9E}";

    private readonly Dictionary<string, IBleLink> _links = new Dictionary<string, IBleLink>();
    private readonly Dictionary<string, WriteState> _writeStates = new Dictionary<string, WriteState>();
    private bool _isScanning, _isConnected;
    private Thread _writingThread;
    private volatile bool _running;

    // Per-device acknowledged-write state. The ack callback signals the write thread.
    private const int MaxWriteRetries = 3;
    private class WriteState
    {
        public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
        public bool AckResult;
        public void Reset() { AckResult = false; Done.Reset(); }
    }

    private WriteState GetWriteState(string deviceName)
    {
        if (!_writeStates.TryGetValue(deviceName, out var s))
        {
            s = new WriteState();
            _writeStates[deviceName] = s;
        }
        return s;
    }

    /// <summary>
    /// Sends one packet with acknowledgment and retries on failure/timeout.
    /// Returns true if the native stack acked the write within the retry budget.
    /// </summary>
    private bool SendAcked(IBleLink link, WriteState state, byte[] data)
    {
        for (int attempt = 0; attempt < MaxWriteRetries; attempt++)
        {
            state.Reset();
            link.Write(data, ok =>
            {
                state.AckResult = ok;
                state.Done.Set();
            });
            if (state.Done.Wait(2000))
                return state.AckResult;
            // Timed out waiting for ack -> retry.
        }
        return false;
    }

    private void Start()
    {
        StartScanHandler();
    }

    private void Update()
    {
        // Connection state is driven by the links; reflect it here for the UI/switch.
        if (_links.Count == 0) return;
        bool allConnected = true;
        foreach (var device in StimulheatDevicesManager.Devices)
        {
            if (!_links.TryGetValue(device.deviceName, out var link) || !link.IsConnected)
            {
                allConnected = false;
                break;
            }
        }
        if (allConnected && !_isConnected)
        {
            _isConnected = true;
            _isScanning = false;
            if (!_running)
            {
                _running = true;
                _writingThread = new Thread(WriteBleData) { IsBackground = true, Name = "StimulHeatWrite" };
                _writingThread.Start();
            }
        }
    }

    private void OnDestroy() => CleanUp();
    private void OnApplicationQuit() => CleanUp();

    /// <summary>Releases every link (ref-counted in BleBridge; the native stack is torn down only when no package is using it).</summary>
    private void CleanUp()
    {
        _running = false;
        try { _writingThread?.Join(500); } catch { }
        foreach (var kvp in _links)
        {
            try { kvp.Value.Dispose(); } catch { }
        }
        _links.Clear();
        foreach (var kvp in _writeStates)
        {
            try { kvp.Value.Done.Dispose(); } catch { }
        }
        _writeStates.Clear();
        _isConnected = false;
        _isScanning = false;
    }

    public void StartScanHandler()
    {
        _isScanning = true;
        _isConnected = false;

        // Request one link per configured device. BleConnectionManager runs the single shared
        // scan; each link's OnConnected fires once its device is discovered and subscribed.
        foreach (var device in StimulheatDevicesManager.Devices)
        {
            if (string.IsNullOrEmpty(device.deviceName)) continue;
            if (_links.ContainsKey(device.deviceName)) continue;

            var profile = new BleDeviceProfile
            {
                deviceName = device.deviceName,
                serviceUuid = ServiceUuid,
                notifyCharacteristicUuids = new[] { NotifyCharacteristicUuid },
                writeCharacteristicUuid = WriteCharacteristicUuid
            };

            var link = BleConnectionManager.Instance.RequestLink(profile);
            link.OnConnected += l => OnLinkConnected(l, device);
            link.OnDisconnected += l => OnLinkDisconnected(l, device);
            link.OnDataReceived += (l, p) => OnLinkData(l, p, device);
            _links[device.deviceName] = link;
        }
    }

    public void ResetHandler()
    {
        CleanUp();
        foreach (var device in StimulheatDevicesManager.Devices)
        {
            device.deviceId = "-1";
            device.connected = false;
        }
    }

    // =========================================================
    // Link events (fire on BleBridge's background thread)
    // =========================================================
    private void OnLinkConnected(IBleLink link, StimulHeatDevice device)
    {
        device.deviceId = link.DeviceId;
        device.connected = true;
        Debug.Log($"[StimulHeat] Connected: {device}");
    }

    private void OnLinkDisconnected(IBleLink link, StimulHeatDevice device)
    {
        device.connected = false;
        _isConnected = false;
        Debug.LogWarning($"[StimulHeat] Disconnected: {device}");
    }

    private void OnLinkData(IBleLink link, BleDataPacket packet, StimulHeatDevice device)
    {
        if (packet.size <= 0) return;
        device.receivedProtocol = packet;
        device.UpdateMeasurementResults();
    }

    // =========================================================
    // Outgoing writes (acknowledged, own thread)
    // =========================================================
    // Each packet is sent once with an acknowledgment callback. On ack failure or timeout
    // (no GATT write-complete within 2s) the packet is retried up to MaxWriteRetries times.
    // Once acked, the packet is cleared so it is sent exactly once. This replaces the old
    // blind continuous re-send hotfix: both backends (WinRT SendData(block=true) and Velorexe's
    // onCharacteristicWrite) now confirm the write, so temperature-mode enslavement switching is
    // delivered reliably without flooding the link.
    private void WriteBleData()
    {
        while (_running)
        {
            Thread.Sleep(100);
            foreach (var device in StimulheatDevicesManager.Devices)
            {
                if (!_links.TryGetValue(device.deviceName, out var link) || !link.IsConnected) continue;
                var state = GetWriteState(device.deviceName);

                if (device.enslavementProtocolToSend != null)
                {
                    if (SendAcked(link, state, device.enslavementProtocolToSend))
                        device.enslavementProtocolToSend = null;
                }
                if (device.commandProtocolToSend != null)
                {
                    if (SendAcked(link, state, device.commandProtocolToSend))
                        device.commandProtocolToSend = null;
                }
            }
        }
    }
}
