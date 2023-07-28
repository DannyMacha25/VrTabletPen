using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class VrControllerTest : MonoBehaviour
{
    public XRController rightHand;
    public XRRayInteractor rightRay;
    public InputHelpers.Button button;
    public InputActionProperty triggerAction;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bool pressed;
        rightHand.inputDevice.IsPressed(button, out pressed);
        rightRay.enabled = pressed;
        if (rightRay.enabled)
        {
            RaycastHit hit;
            //Debug.Log(rightRay.TryGetCurrent3DRaycastHit(out hit));
            //Debug.Log(hit.point);
        }
    }
}
