using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class VrController : MonoBehaviour
{
    public XRController rightHand;
    public XRController leftHand;
    public XRRayInteractor rightRay;
    public InputHelpers.Button activation;

    public GameObject handMenu;
    public GameObject rgbSlider;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bool pressed, menuActive;
        rightHand.inputDevice.IsPressed(activation, out pressed);
        rightRay.enabled = pressed;

        leftHand.inputDevice.IsPressed(activation, out menuActive);
        SetMenuActive(menuActive);
    }

    void SetMenuActive(bool active)
    {
        handMenu.SetActive(active);
        rgbSlider.SetActive(active);
        
    }
}
