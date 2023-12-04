using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFreeLookCam : MonoBehaviour
{

    [Header("Refrences")]
    [SerializeField] Transform player;
    [SerializeField] CinemachineFreeLook freeLookCam;

    [Header("Settings")]
    [SerializeField, Range(0f, 100f)] float SpeedXMulitiplier = 1f;
    [SerializeField, Range(0f, 100f)] float SpeedYMulitiplier = 1f;

    bool isUnLockPressed = false;
    bool cameraMovementLock = false;

    private void Update()
    {

        Vector2 input = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        if(input.magnitude != 0)
        {
            OnLook(input, false);
        }
        
        else
        {

            freeLookCam.m_YAxis.m_InputAxisValue = 0;
        }
    }

    void OnLook(Vector2 cameraMovement, bool isDeviceMouse)
    {

        float deviceMultiplier = isDeviceMouse ? Time.fixedDeltaTime : Time.deltaTime;
        //freeLookCam.m_XAxis.m_InputAxisValue = cameraMovement.x * SpeedMulitiplier * deviceMultiplier;
        freeLookCam.m_YAxis.m_InputAxisValue = cameraMovement.y * SpeedYMulitiplier * deviceMultiplier;

        player.Rotate(Vector3.up, cameraMovement.x * SpeedXMulitiplier * deviceMultiplier);

    }
}
