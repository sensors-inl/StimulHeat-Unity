using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Stimulheat.Bluetooth;

/// <summary>
/// Sends a ThermalStimuli's value to the StimulHeat device bound to this hand while the
/// interactable is held, and turns it off (Heat, value 0, disabled) on release.
///
/// Attach to the same GameObject as an XRBaseInteractor (e.g. XR Ray Interactor /
/// XR Direct Interactor) on each controller. Set handSide to match the controller. On
/// selectEntered it scans the grabbed object for enabled ThermalStimuli components and, for
/// each, sends BLEPlatformSwitch.SendCommand(deviceId, thermalValue, type) to the device bound
/// to this hand via StimulHeatVRSettings. On selectExited it sends an off command.
///
/// This is the VR equivalent of the desktop StimulHeatBleDemo's "Send Command" button, driven
/// by XR grab events instead of IMGUI. It depends only on the public BLEPlatformSwitch /
/// StimulHeatDevice / EnslavementType API, which the BleBridge-backed package keeps unchanged.
/// </summary>
[RequireComponent(typeof(XRBaseInteractor))]
public class SendThermalOnSelect : MonoBehaviour
{
    [Tooltip("Which hand this interactor represents; must match a StimulHeatVRSettings binding.")]
    public HandSide handSide;

    private XRBaseInteractor _hand;
    private BLEPlatformSwitch _blePlatformSwitch;
    private StimulHeatVRSettings _stimulHeatVRSettings;

    private void OnEnable()
    {
        _hand = GetComponent<XRBaseInteractor>();
        // Cache the scene-level VR setup. FindFirstObjectByType covers the case where the settings
        // component is on a different GameObject and may not have run Start() yet; we re-resolve
        // lazily on first grab if these are still null.
        _stimulHeatVRSettings = FindFirstObjectByType<StimulHeatVRSettings>();
        _blePlatformSwitch = FindFirstObjectByType<BLEPlatformSwitch>();

        if (_hand != null)
        {
            _hand.selectEntered.AddListener(OnGrabbed);
            _hand.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (_hand != null)
        {
            _hand.selectEntered.RemoveListener(OnGrabbed);
            _hand.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactableObject == null) return;
        var thermalStimuli = args.interactableObject.transform.GetComponents<ThermalStimuli>();
        if (thermalStimuli.Length == 0) return;

        if (!ResolveTargets(out var device)) return;

        foreach (var stimuli in thermalStimuli)
        {
            if (!stimuli.enabled) continue;
            _blePlatformSwitch.SendCommand(device.deviceId, stimuli.thermalValue, stimuli.type);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (args.interactableObject == null) return;
        var thermalStimuli = args.interactableObject.transform.GetComponents<ThermalStimuli>();
        if (thermalStimuli.Length == 0) return;

        if (!ResolveTargets(out var device)) return;

        // Turn the stimulus off. The original VR demo always sent Heat/0/disabled on release;
        // we keep that contract so a release reliably zeroes the device regardless of which
        // enslavement type was active during the grab.
        foreach (var stimuli in thermalStimuli)
        {
            if (!stimuli.enabled) continue;
            _blePlatformSwitch.SendCommand(device.deviceId, 0, EnslavementType.Heat, false);
        }
    }

    // Lazily resolves the BLEPlatformSwitch + the device bound to this hand, re-fetching the
    // scene VR settings if Start() hadn't run when OnEnable cached them (e.g. component enabled
    // before the settings object was active). Returns false and logs once if anything is missing.
    private bool ResolveTargets(out StimulHeatDevice device)
    {
        device = null;
        if (_stimulHeatVRSettings == null)
            _stimulHeatVRSettings = FindFirstObjectByType<StimulHeatVRSettings>();
        if (_blePlatformSwitch == null)
            _blePlatformSwitch = FindFirstObjectByType<BLEPlatformSwitch>();

        if (_stimulHeatVRSettings == null || _blePlatformSwitch == null)
        {
            Debug.LogWarning("[StimulHeatVR] Missing StimulHeatVRSettings or BLEPlatformSwitch in scene; thermal command ignored.");
            return false;
        }
        if (!_blePlatformSwitch.isConnected)
        {
            // Not connected yet (device.deviceId is still null/"-1" until OnLinkConnected fires).
            // Silently skip; once the bridge connects, subsequent grabs will command the device.
            return false;
        }
        if (!_stimulHeatVRSettings.stimulHeatDevicesVR.TryGetValue(handSide, out device) || device == null)
        {
            Debug.LogWarning($"[StimulHeatVR] No StimulHeat device bound to hand {handSide}.");
            return false;
        }
        return true;
    }
}
