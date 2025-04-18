using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSystemManager : MonoBehaviour
{
    // Singleton instance
    public static CameraSystemManager Instance { get; private set; }

    [SerializeField] private CinemachineVirtualCamera overviewCamera;
    [SerializeField] private KeyCode toggleCameraKey = KeyCode.C;
    [SerializeField] private KeyCode nextAgentKey = KeyCode.Tab;
    
    private List<AgentCameraController> agentCameras = new List<AgentCameraController>();
    private int currentCameraIndex = -1;  // -1 means overview camera is active
    private bool isOverviewActive = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Don't use DontDestroyOnLoad to allow proper scene reloading
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    void Start()
    {
        // Reset camera system state when starting
        currentCameraIndex = -1;  // Reset to overview camera
        isOverviewActive = true;  // Ensure overview is active initially
        agentCameras.Clear();     // Clear previous agent cameras
        
        // Start with overview camera active
        if (overviewCamera != null)
        {
            overviewCamera.Priority = 100;
        }
        
        // Find all agent cameras in the scene
        StartCoroutine(FindAllAgentCameras());
    }
    
    IEnumerator FindAllAgentCameras()
    {
        // Wait a frame to ensure all agents are spawned
        yield return null;
        
        agentCameras.Clear();
        
        // Find all agent camera controllers
        AgentCameraController[] cameras = FindObjectsOfType<AgentCameraController>();
        foreach (var camera in cameras)
        {
            agentCameras.Add(camera);
        }
        
        Debug.Log($"Found {agentCameras.Count} agent cameras");
        
        // Find the heuristic agent camera if any
        ActivateHeuristicCamera();
    }
    
    void ActivateHeuristicCamera()
    {
        // The agent camera controllers will handle setting priorities automatically
        // We just need to update our index to the heuristic one
        for (int i = 0; i < agentCameras.Count; i++)
        {
            Transform parent = agentCameras[i].transform.parent;
            if (parent != null && parent.name.Contains("_Heuristic"))
            {
                currentCameraIndex = i;
                isOverviewActive = false;
                
                // Set overview camera to lower priority
                if (overviewCamera != null)
                {
                    overviewCamera.Priority = 10;
                }
                
                break;
            }
        }
    }
    
    void Update()
    {
        // Toggle between overview and agent cameras
        if (Input.GetKeyDown(toggleCameraKey))
        {
            ToggleOverviewCamera();
        }
        
        // Cycle through agent cameras
        if (Input.GetKeyDown(nextAgentKey) && !isOverviewActive)
        {
            CycleToNextAgentCamera();
        }
    }
    
    void ToggleOverviewCamera()
    {
        isOverviewActive = !isOverviewActive;
        
        if (isOverviewActive)
        {
            // Activate overview camera
            if (overviewCamera != null)
            {
                overviewCamera.Priority = 100;
            }
            
            // Deactivate all agent cameras
            foreach (var camera in agentCameras)
            {
                camera.SetCameraActive(false);
            }
        }
        else
        {
            // Deactivate overview camera
            if (overviewCamera != null)
            {
                overviewCamera.Priority = 10;
            }
            
            // Activate current or first agent camera
            if (currentCameraIndex < 0 || currentCameraIndex >= agentCameras.Count)
            {
                currentCameraIndex = 0;
            }
            
            if (agentCameras.Count > 0)
            {
                agentCameras[currentCameraIndex].SetCameraActive(true);
            }
        }
    }
    
    void CycleToNextAgentCamera()
    {
        // Deactivate current camera
        if (currentCameraIndex >= 0 && currentCameraIndex < agentCameras.Count)
        {
            agentCameras[currentCameraIndex].SetCameraActive(false);
        }
        
        // Move to next camera
        currentCameraIndex = (currentCameraIndex + 1) % agentCameras.Count;
        
        // Activate new camera
        if (agentCameras.Count > 0)
        {
            agentCameras[currentCameraIndex].SetCameraActive(true);
        }
    }
    
    // Method to notify when new agents are spawned
    public void RegisterAgentCamera(AgentCameraController camera)
    {
        if (!agentCameras.Contains(camera))
        {
            agentCameras.Add(camera);
        }
    }
    
    // Method to get the currently active camera
    public CinemachineVirtualCamera GetActiveCamera()
    {
        if (isOverviewActive)
        {
            return overviewCamera;
        }
        else if (currentCameraIndex >= 0 && currentCameraIndex < agentCameras.Count)
        {
            // Add null check to avoid MissingReferenceException when agents are destroyed
            if (agentCameras[currentCameraIndex] == null)
            {
                // If the camera controller is null, switch to overview camera
                SetOverviewCameraActive();
                return overviewCamera;
            }
            
            // Return the virtual camera component from the active agent camera
            Transform cameraTransform = agentCameras[currentCameraIndex].transform;
            if (cameraTransform != null)
            {
                return cameraTransform.GetComponent<CinemachineVirtualCamera>();
            }
            else
            {
                // If the transform is null, switch to overview camera
                SetOverviewCameraActive();
                return overviewCamera;
            }
        }
        
        return null;
    }
    
    // Method to set overview camera active (used at episode transitions)
    public void SetOverviewCameraActive()
    {
        // Only do work if not already on overview camera
        if (!isOverviewActive)
        {
            isOverviewActive = true;
            
            // Activate overview camera
            if (overviewCamera != null)
            {
                overviewCamera.Priority = 100;
            }
            
            // Deactivate all agent cameras
            for (int i = agentCameras.Count - 1; i >= 0; i--)
            {
                if (agentCameras[i] != null)
                {
                    agentCameras[i].SetCameraActive(false);
                }
                else
                {
                    // Remove null references from list
                    agentCameras.RemoveAt(i);
                }
            }
        }
    }
    
    // Method to clean up the list when the game starts a new episode
    public void CleanupAgentList()
    {
        // Remove any null references from the list
        for (int i = agentCameras.Count - 1; i >= 0; i--)
        {
            if (agentCameras[i] == null)
            {
                agentCameras.RemoveAt(i);
            }
        }
        
        // Reset to overview camera
        SetOverviewCameraActive();
    }
}
