using System.Collections.Generic;
using UnityEngine;

public class StimulheatDevicesManager : MonoBehaviour
{
    public static StimulheatDevicesManager Instance { get; private set; }

    public List<StimulHeatDevice> stimulheatDevices = new();
    
    public static List<StimulHeatDevice> Devices => Instance.stimulheatDevices;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

