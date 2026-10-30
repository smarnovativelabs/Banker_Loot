using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
    public Transform playerCam;   // Assigned in the Inspector
    public Transform targetCam;

    public string counterCameraTag = "CounterCamera";

    void Start()
    {
        if (playerCam == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerCam = mainCam.transform;
            }
            else
            {
                Debug.LogWarning("No Main Camera found in the scene for BillBoard script.");
            }
        }

        targetCam = playerCam;
    }

    void Update()
    {
        // Default to player camera
        targetCam = playerCam;

        // Find all cameras tagged as CounterCamera
        GameObject[] counterCameras = GameObject.FindGameObjectsWithTag(counterCameraTag);

        foreach (GameObject camObj in counterCameras)
        {
            Camera cam = camObj.GetComponent<Camera>();
            if (cam != null && cam.isActiveAndEnabled && cam.gameObject.activeInHierarchy)
            {
                targetCam = cam.transform;
                break; // found an active counter camera, stop searching
            }
        }

        transform.LookAt(targetCam);

        Vector3 targetAngle = transform.eulerAngles;
        targetAngle.x = 0;
        targetAngle.z = 0;
        transform.eulerAngles = targetAngle;
    }
}
