using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public class Test2 : MonoBehaviour
{
    private Dictionary<InputFeature, InputFeatureUsage<bool>> inputFeatureUsageMap = new Dictionary<InputFeature, InputFeatureUsage<bool>>() {
        { InputFeature.primaryButton, CommonUsages.primaryButton },
        { InputFeature.primaryTouch, CommonUsages.primaryTouch },
        { InputFeature.secondaryButton, CommonUsages.secondaryButton },
        { InputFeature.secondaryTouch, CommonUsages.secondaryTouch },
        { InputFeature.gripButton, CommonUsages.gripButton },
        { InputFeature.triggerButton, CommonUsages.triggerButton },
        { InputFeature.menuButton, CommonUsages.menuButton },
        { InputFeature.primary2DAxisClick, CommonUsages.primary2DAxisClick },
        { InputFeature.primary2DAxisTouch, CommonUsages.primary2DAxisTouch },
        { InputFeature.secondary2DAxisClick, CommonUsages.secondary2DAxisClick },
        { InputFeature.secondary2DAxisTouch, CommonUsages.secondary2DAxisTouch },
        { InputFeature.userPresence, CommonUsages.userPresence }
    };

    [SerializeField]
    public InputFeature ButtonInputFeature;

    [SerializeField]
    public InputDeviceCharacteristics inputDeviceType = InputDeviceCharacteristics.None;

    [SerializeField]
    public UnityEvent OnInputDown;

    [SerializeField]
    public UnityEvent OnInputUp;

    private InputFeatureUsage<bool> buttonInputFeatureUsage;

    private bool lastButtonState = false;
    private List<InputDevice> devicesButton;

    private void Awake()
    {
        if (OnInputDown == null)
            OnInputDown = new UnityEvent();
        if (OnInputUp == null)
            OnInputUp = new UnityEvent();

        devicesButton = new List<InputDevice>();

        if (!inputFeatureUsageMap.TryGetValue(ButtonInputFeature, out buttonInputFeatureUsage))
        {
            Debug.LogError("not found inputFeature: " + ButtonInputFeature);
        }
    }

    private void OnEnable()
    {
        Init();
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= InputDevices_deviceConnected;
        InputDevices.deviceDisconnected -= InputDevices_deviceDisconnected;
        devicesButton.Clear();
    }

    private void InputDevices_deviceConnected(InputDevice device)
    {
        bool discardedValue;
        if (device.TryGetFeatureValue(buttonInputFeatureUsage, out discardedValue))
        {
            
            if(device.name.Contains("Left"))
            {
                 //Debug.Log($"add device: {device.name}");
                 devicesButton.Add(device); 
            }
          // Add any devices that have a primary button.
        }
    }

    private void InputDevices_deviceDisconnected(InputDevice device)
    {
        if (devicesButton.Contains(device))
            devicesButton.Remove(device);
    }

    private void Update()
    {
        bool tempState = false;
        foreach (var device in devicesButton)
        {
            bool primaryButtonState = false;
            tempState = device.TryGetFeatureValue(buttonInputFeatureUsage, out primaryButtonState) // did get a value
                        && primaryButtonState // the value we got
                        || tempState; // cumulative result from other controllers
        }

        if (tempState != lastButtonState) // Button state changed since last frame
        {
            if (tempState)
                OnInputDown?.Invoke();
            else
                OnInputUp?.Invoke();

            lastButtonState = tempState;
        }
    }

    public void Init()
    {
        List<InputDevice> allDevices = new List<InputDevice>();
        if (inputDeviceType == InputDeviceCharacteristics.None)
            InputDevices.GetDevices(allDevices);
        else
            InputDevices.GetDevicesWithCharacteristics(inputDeviceType, allDevices);

        foreach (InputDevice device in allDevices)
            InputDevices_deviceConnected(device);

        InputDevices.deviceConnected += InputDevices_deviceConnected;
        InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
    }
}