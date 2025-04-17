using UnityEngine;
using Cinemachine;

public class AgentCameraController : MonoBehaviour
{
    private CinemachineVirtualCamera virtualCamera;
    
    void Start()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("Virtual Camera component missing!");
            return;
        }
        
        // Set initial priority to low
        virtualCamera.Priority = 10;
        
        // Check if this agent is in heuristic mode by looking at its name or behavior parameters
        Transform parent = transform.parent;
        if (parent != null && parent.name.Contains("_Heuristic"))
        {
            // If this is a heuristic agent, set higher priority
            virtualCamera.Priority = 100;
            Debug.Log($"Camera activated for heuristic agent: {parent.name}");
        }
    }
    
    // Optionally add methods to activate/deactivate this camera
    public void SetCameraActive(bool active)
    {
        if (virtualCamera != null)
        {
            virtualCamera.Priority = active ? 100 : 10;
        }
    }
}
