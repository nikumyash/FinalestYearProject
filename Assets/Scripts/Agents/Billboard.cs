using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Make the canvas look at the camera
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
        else
        {
            // If the main camera changes, find it again
            mainCamera = Camera.main;
        }
    }
} 