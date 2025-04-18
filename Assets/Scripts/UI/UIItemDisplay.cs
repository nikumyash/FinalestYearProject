using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cinemachine;

public class UIItemDisplay : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private TextMeshProUGUI itemCountText;
    [SerializeField] private Vector2 topRightOffset = new Vector2(20, 20);
    
    // Static reference for agent registration
    private static UIItemDisplay instance;
    private static List<AgentItemCounter> registeredAgents = new List<AgentItemCounter>();
    
    // Camera references
    private CinemachineVirtualCamera activeCam;
    private AgentItemCounter currentDisplayedAgent;
    
    // Singleton initialization
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize UI if not set
        if (uiCanvas == null) CreateUICanvas();
        if (itemCountText == null) CreateItemText();
    }
    
    private void Start()
    {
        // Hide text initially
        if (itemCountText != null)
            itemCountText.gameObject.SetActive(false);
    }
    
    private void CreateUICanvas()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("ItemDisplayCanvas");
        canvasObj.transform.SetParent(transform);
        
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Add canvas scaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Add graphic raycaster (required by Unity UI)
        canvasObj.AddComponent<GraphicRaycaster>();
    }
    
    private void CreateItemText()
    {
        // Create text object
        GameObject textObj = new GameObject("AgentItemText");
        textObj.transform.SetParent(uiCanvas.transform);
        
        // Add text component
        itemCountText = textObj.AddComponent<TextMeshProUGUI>();
        itemCountText.fontSize = 28;
        itemCountText.fontStyle = FontStyles.Bold;
        itemCountText.alignment = TextAlignmentOptions.Right;
        itemCountText.color = Color.white;
        itemCountText.outlineWidth = 0.2f;
        itemCountText.outlineColor = Color.black;
        
        // Position in top-right
        RectTransform rectTransform = itemCountText.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.sizeDelta = new Vector2(300, 60);
        rectTransform.anchoredPosition = new Vector2(-topRightOffset.x, -topRightOffset.y);
    }
    
    private void Update()
    {
        // Check if we need to update the displayed agent
        UpdateDisplayedAgent();
    }
    
    private void UpdateDisplayedAgent()
    {
        try
        {
            // Get active Cinemachine camera
            CinemachineVirtualCamera currentCam = GetActiveVirtualCamera();
            
            if (currentCam != activeCam)
            {
                activeCam = currentCam;
                currentDisplayedAgent = FindAgentForCamera(activeCam);
                
                // Show/Hide UI based on if we found an agent
                if (itemCountText != null)
                {
                    if (currentDisplayedAgent != null)
                    {
                        itemCountText.gameObject.SetActive(true);
                        UpdateItemText();
                    }
                    else
                    {
                        itemCountText.gameObject.SetActive(false);
                    }
                }
            }
            else if (currentDisplayedAgent != null)
            {
                // Update the text if an agent is displayed
                UpdateItemText();
            }
        }
        catch (System.Exception e)
        {
            // Handle any exceptions gracefully
            Debug.LogWarning($"Error updating displayed agent: {e.Message}");
            
            // Reset state and hide UI
            activeCam = null;
            currentDisplayedAgent = null;
            if (itemCountText != null)
            {
                itemCountText.gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdateItemText()
    {
        if (currentDisplayedAgent == null)
        {
            // If agent is destroyed, hide the text
            if (itemCountText != null)
            {
                itemCountText.gameObject.SetActive(false);
            }
            currentDisplayedAgent = null;
            return;
        }
        
        // Use try-catch to handle any null reference errors
        try
        {
            if (itemCountText != null)
            {
                itemCountText.text = currentDisplayedAgent.GetItemText();
                itemCountText.color = currentDisplayedAgent.GetTextColor();
            }
        }
        catch (System.Exception)
        {
            // If we get an error, the agent reference is likely destroyed
            if (itemCountText != null)
            {
                itemCountText.gameObject.SetActive(false);
            }
            currentDisplayedAgent = null;
        }
    }
    
    private CinemachineVirtualCamera GetActiveVirtualCamera()
    {
        // Get the active virtual camera from Cinemachine
        CinemachineVirtualCamera activeCam = null;
        
        try
        {
            // Try to get from CameraSystemManager if it exists
            if (CameraSystemManager.Instance != null)
            {
                activeCam = CameraSystemManager.Instance.GetActiveCamera();
            }
            
            // Fallback in case we can't get it from CameraSystemManager
            if (activeCam == null)
            {
                CinemachineVirtualCamera[] cams = FindObjectsOfType<CinemachineVirtualCamera>();
                foreach (var cam in cams)
                {
                    if (cam != null && cam.isActiveAndEnabled && cam.Priority >= 50)
                    {
                        activeCam = cam;
                        break;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error getting active camera: {e.Message}");
        }
        
        return activeCam;
    }
    
    private AgentItemCounter FindAgentForCamera(CinemachineVirtualCamera cam)
    {
        if (cam == null) return null;
        
        try
        {
            // If it's the overview camera, return null
            if (cam.name.Contains("Overview")) return null;
            
            // Try to find the agent whose camera is active
            for (int i = registeredAgents.Count - 1; i >= 0; i--)
            {
                if (i >= registeredAgents.Count) continue; // Safety check for collection modified
                
                AgentItemCounter agent = registeredAgents[i];
                if (agent == null)
                {
                    // Clean up null entries
                    registeredAgents.RemoveAt(i);
                    continue;
                }
                
                // Check if the agent's camera is the active one
                Transform agentCamTransform = agent.GetTransform();
                if (agentCamTransform != null && 
                    agentCamTransform.parent != null && 
                    cam.transform.IsChildOf(agentCamTransform.parent))
                {
                    return agent;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error finding agent for camera: {e.Message}");
        }
        
        return null;
    }
    
    // Static methods for agents to register with this system
    public static void RegisterAgent(AgentItemCounter agent)
    {
        if (instance == null)
        {
            // Create the UIItemDisplay if it doesn't exist
            GameObject displayObj = new GameObject("UIItemDisplay");
            displayObj.AddComponent<UIItemDisplay>();
        }
        
        if (!registeredAgents.Contains(agent))
        {
            registeredAgents.Add(agent);
        }
    }
    
    public static void UnregisterAgent(AgentItemCounter agent)
    {
        registeredAgents.Remove(agent);
    }
} 