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
    
    private void UpdateItemText()
    {
        if (currentDisplayedAgent != null && itemCountText != null)
        {
            itemCountText.text = currentDisplayedAgent.GetItemText();
            itemCountText.color = currentDisplayedAgent.GetTextColor();
        }
    }
    
    private CinemachineVirtualCamera GetActiveVirtualCamera()
    {
        // Get the active virtual camera from Cinemachine
        CinemachineVirtualCamera activeCam = null;
        
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
                // Priority above 50 usually means it's active
                if (cam.Priority >= 50)
                {
                    activeCam = cam;
                    break;
                }
            }
        }
        
        return activeCam;
    }
    
    private AgentItemCounter FindAgentForCamera(CinemachineVirtualCamera cam)
    {
        if (cam == null) return null;
        
        // If it's the overview camera, return null
        if (cam.name.Contains("Overview")) return null;
        
        // Try to find the agent whose camera is active
        foreach (var agent in registeredAgents)
        {
            if (agent == null) continue;
            
            // Check if the agent's camera is the active one
            Transform agentCamTransform = agent.GetTransform();
            if (agentCamTransform != null && 
                agentCamTransform.parent != null && 
                cam.transform.IsChildOf(agentCamTransform.parent))
            {
                return agent;
            }
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