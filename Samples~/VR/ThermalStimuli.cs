using UnityEngine;
using Stimulheat.Bluetooth;

/// <summary>
/// Tags a grabbable VR object with a thermal stimulus to send to a StimulHeat device while
/// it is held. Attach alongside an XR interactable; SendThermalOnSelect reads these on grab.
///
/// `thermalValue` is a temperature in °C when type == Temp (15–42), or a heat-flow value
/// when type == Heat (-5–5). `enabled` mirrors the Unity component enabled state: only
/// enabled ThermalStimuli components fire, so you can author several on one object and toggle
/// the active one at runtime.
/// </summary>
public class ThermalStimuli : MonoBehaviour
{
    [Tooltip("Temperature (°C, 15–42) when type=Temp, or heat-flow value (-5–5) when type=Heat.")]
    public float thermalValue;

    [Tooltip("Enslavement mode the device must be in to apply this stimulus.")]
    public EnslavementType type;
}
