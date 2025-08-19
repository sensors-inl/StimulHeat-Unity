using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class StimulHeatDevicesManagerWindows : MonoBehaviour
{
    public bool isScanning => _isScanning;
    public bool isConnected => _isConnected;
    
    private readonly string _serviceUuid = "{6E400001-B5A3-F393-E0A9-E50E24DCCA9E}";
    private readonly string[] _readCharacteristicUuids = { "{6E400003-B5A3-F393-E0A9-E50E24DCCA9E}" };
    private readonly string _writeCharacteristicUuid = "{6E400002-B5A3-F393-E0A9-E50E24DCCA9E}";

    private BLE _ble;
    private BLE.BLEScan _scan;

    private bool _isScanning, _isConnected, _isReading;

    private IDictionary<string, string> _discoveredDevices = new Dictionary<string, string>();
    private int _devicesCount;

    private Thread _scanningThread, _connectionThread, _readingThread, _writingThread;
    

    private void Start()
    {
        _ble = new BLE();
        _readingThread = new Thread(ReadBleData);
        _writingThread = new Thread(WriteBleData);
        StartScanHandler();
    }

    private bool DevicesFound()
    {
        var found = true;
        foreach (var device in StimulheatDevicesManager.Devices)
        {
            found = found && device.deviceId != null;
        }
        return found;
    }

    private bool DevicesConnected()
    {
        var connected = true;
        foreach (var device in StimulheatDevicesManager.Devices)
        {
            connected = connected && device.connected;
        }
        return connected;
    }

    private void Update()
    {
        if (_isScanning)
        {
            if (_discoveredDevices.Count > _devicesCount)
            {
                _devicesCount = _discoveredDevices.Count;
            }
        }

        if (DevicesConnected())
        {
            // Device was found and is connected.
            if (_ble.isConnected && !_isConnected)
            {
                _isConnected = true;
                _isScanning = false;
            }
        }

        if (_isConnected && !_isReading)
        {
            StartReadingHandler();
            _isReading = true;
        }
    }

    private void OnDestroy()
    {
        CleanThreads();
    }

    private void OnApplicationQuit()
    {
        CleanThreads();
    }

    // Prevent threading issues and free BLE stack.
    private void CleanThreads()
    {
        try
        {
            _scan.Cancel();
            _ble.Close();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("BLE object never initialized.\n" + e);
        }

        try
        {
            _scanningThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Scanning Thread never initialized.\n" + e);
        }

        try
        {
            _connectionThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Connection Thread never initialized.\n" + e);
        }

        try
        {
            _readingThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Reading Thread never initialized.\n" + e);
        }

        try
        {
            _writingThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Writing Thread never initialized.\n" + e);
        }
    }

    public void StartScanHandler()
    {
        _devicesCount = 0;
        _isScanning = true;
        _discoveredDevices.Clear();
        _scanningThread = new Thread(ScanBleDevices);
        _scanningThread.Start();
    }

    public void ResetHandler()
    {
        _discoveredDevices.Clear();

        foreach (var device in StimulheatDevicesManager.Devices)
        {
            device.deviceId = "-1";
        }

        CleanThreads();
    }

    private void ReadBleData(object obj)
    {
        while (true)
        {
            Thread.Sleep(100);

            var packageReceived = BLE.ReadPackage();

            if (packageReceived.buf.Length == 0) break;

            foreach (var device in StimulheatDevicesManager.Devices)
            {
                if (!packageReceived.deviceId.Equals(device.deviceId)) continue;
                
                device.receivedProtocol = packageReceived;
                device.UpdateMeasurementResults();
            }
        }
    }

    private void WriteBleData(object obj)
    {
        while (true)
        {
            Thread.Sleep(100);
            
            foreach (var device in StimulheatDevicesManager.Devices)
            {
                if (device.enslavementProtocolToSend != null)
                {
                    BLE.WritePackage(device.deviceId, _serviceUuid, _writeCharacteristicUuid, device.enslavementProtocolToSend);
                }

                if (device.commandProtocolToSend != null)
                {
                    BLE.WritePackage(device.deviceId, _serviceUuid, _writeCharacteristicUuid, device.commandProtocolToSend);
                }
            }
        }
    }

    private void ScanBleDevices()
    {
        _scan = BLE.ScanDevices();
        Debug.Log("BLE.ScanDevices() started.");

        _scan.Found = (newDeviceId, newDeviceName) =>
        {
            try
            {
                _discoveredDevices.Add(newDeviceId, newDeviceName);
                Debug.Log("Found device with name: " + newDeviceName);
            }
            catch (Exception e)
            {
                // ignored
            }

            foreach (var device in StimulheatDevicesManager.Devices)
            {
                if (device.deviceId == null && newDeviceName.Contains(device.deviceName))
                {
                    device.deviceId = newDeviceId;
                }
            }
            
        };

        _scan.Finished = () =>
        {
            _isScanning = false;
            
            foreach (var device in StimulheatDevicesManager.Devices)
            {
                Debug.Log(device.ToString());
            }

            Debug.Log("Scan finished");
        };

        while (!DevicesFound()) Thread.Sleep(500);

        _scan.Cancel();
        _scanningThread = null;
        _isScanning = false;

        if (!DevicesFound())
        {
            foreach (var device in StimulheatDevicesManager.Devices)
            {
                if (device.deviceId == "-1") Debug.Log("Device Not Found ! " + device.ToString());
            }
        }
        else
        {
            StartConHandler();
        }
    }

    public void StartConHandler()
    {
        _connectionThread = new Thread(ConnectBleDevice);
        _connectionThread.Start();
    }

    public void StartReadingHandler()
    {
        if (!_readingThread.IsAlive)
        {
            _readingThread = new Thread(ReadBleData);
            _readingThread.Start();
        }
    }

    public void StartWritingHandler()
    {
        if (!_writingThread.IsAlive)
        {
            _writingThread = new Thread(WriteBleData);
            _writingThread.Start();
        }
    }

    private void ConnectBleDevice()
    {
        foreach (var device in StimulheatDevicesManager.Devices)
        {
            try
            {
                _ble.Connect(device.deviceId, _serviceUuid, _readCharacteristicUuids);
                device.connected = true;
            }
            catch (Exception e)
            {
                Debug.Log("Could not establish connection to device with ID " + device.deviceId + "\n" + e);
            }
        }

        if (!DevicesConnected()) return;
        Debug.Log("Every device connected successfully!");
        
        StartWritingHandler();
        StartReadingHandler();
    }
}