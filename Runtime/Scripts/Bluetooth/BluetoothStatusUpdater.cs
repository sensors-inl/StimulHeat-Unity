using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BluetoothStatusUpdater : MonoBehaviour
{
    private Image _image;
    private BLEPlatformSwitch _blePlatformSwitch;
    void Start()
    {
        _image = GetComponent<Image>();
        _blePlatformSwitch = FindObjectOfType<BLEPlatformSwitch>();
    }
    
    void Update()
    {
        if(_blePlatformSwitch.isScanning)
            _image.color = Color.yellow;
        else if (_blePlatformSwitch.isConnected)
            _image.color = Color.green;
        else
        {
            _image.color = Color.red;
        }
    }
}
