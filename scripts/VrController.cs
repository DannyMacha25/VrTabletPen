using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;
using System.Threading;

public class VrController : MonoBehaviour
{
    [Header("VR")]
    public XRController rightHand;
    public XRController leftHand;
    public XRRayInteractor rightRay;
    public XRRayInteractor leftRay;
    public InputHelpers.Button activation;
    public ContinuousMoveProviderBase moveProvider;

    [Header("Menu Objects")]
    public GameObject handMenu;
    public GameObject rgbSlider;
    public GameObject wholeMenu;

    public TextMeshProUGUI handText, speedText, rayText;

    [Header("Options")]
    public bool leftHanded;

    public float rayLength = 10, movementSpeed = 5;

    enum Hand
    {
        LEFT,
        RIGHT
    };
    // Update is called once per frame
    private void Start()
    {
        UpdateSpeed(movementSpeed);
        UpdateRayLength(rayLength);
    }
    void Update()
    {
        rightRay.maxRaycastDistance = rayLength;
        leftRay.maxRaycastDistance = rayLength;
        moveProvider.moveSpeed = movementSpeed;

        bool pressed, menuActive;

        if (!leftHanded)
        {
            rightHand.inputDevice.IsPressed(activation, out pressed);
            rightRay.enabled = pressed;

            leftHand.inputDevice.IsPressed(activation, out menuActive);
        }else
        {
            rightHand.inputDevice.IsPressed(activation, out menuActive);

            leftHand.inputDevice.IsPressed(activation, out pressed);
            leftRay.enabled = pressed;
        }
        SwitchMenuHand();
        SetMenuActive(menuActive);
    }

    void SetMenuActive(bool active)
    {
        handMenu.SetActive(active);
        rgbSlider.SetActive(active);
        
    }

    void SwitchMenuHand()
    {
        if (leftHanded)
        {
            wholeMenu.transform.SetParent(rightHand.transform, false);
        }else
        {
            wholeMenu.transform.SetParent(leftHand.transform, false);
        }
    }

    public void UISwitchHand()
    {
        SetMenuActive(false);
        rightRay.enabled = false;
        leftRay.enabled = false;
        if (!leftHanded)
        {
            handText.text = "Left";
            leftHanded = true;
        }
        else
        {
            handText.text = "Right";
            leftHanded = false;
        }
        SwitchMenuHand();
        
    }

    public void UpdateRayLength(float l)
    {
        rayLength = l;
        rayText.text = "Ray Length: " + string.Format("{00}", rayLength);
    }

    public void UpdateSpeed(float s)
    {
        movementSpeed = s;
        speedText.text = "Move Speed: " + string.Format("{00}", movementSpeed);
    }
}
