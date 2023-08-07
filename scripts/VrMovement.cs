using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

public class VrMovement : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] GameObject settingsMenu;
    [SerializeField] XRController leftHand, rightHand;
    Transform tf;

    InputHelpers.Button ascend = InputHelpers.Button.Trigger;
    private void Start()
    {
        tf = this.GetComponent<Transform>();
    }
    private void Update()
    {
        // NOTE: Current movement is frame based, might become a problem?
        // Movement
        bool ascended, descended;

        leftHand.inputDevice.IsPressed(ascend, out ascended);
        rightHand.inputDevice.IsPressed(ascend, out descended);

        if (ascended)
        {
            tf.position += tf.up * speed;
        }

        if (descended)
        {
            tf.position -= tf.up * speed;
        }

        // Menu

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsMenu();
        }

    }

    public void ChangeMovementSpeed(float s)
    {
        speed = s;
    }


    public void ToggleSettingsMenu()
    {
        settingsMenu.SetActive(!settingsMenu.activeSelf);
    }
}
