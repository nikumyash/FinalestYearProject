using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AgentItemCounter : MonoBehaviour
{
    [SerializeField] private Canvas worldSpaceCanvas;
    [SerializeField] private TextMeshProUGUI itemCountText;
    [SerializeField] private float fontSize = 0.5f;
    [SerializeField] private float canvasSize = 4.0f; // Increased width to prevent wrapping
    [SerializeField] private float heightAboveAgent = 1.6f; // Reduced height to be closer to agent
    
    private Camera mainCamera;
    private Transform agentTransform;
    private RunnerAgent runnerAgent;
    private TaggerAgent taggerAgent;
    private bool isRunner;
    
    void Start()
    {
        // Get main camera
        mainCamera = Camera.main;
        
        // Get parent agent transform
        agentTransform = transform.parent;
        
        // Check if this is attached to a Runner or Tagger
        runnerAgent = agentTransform.GetComponent<RunnerAgent>();
        if (runnerAgent != null)
        {
            isRunner = true;
        }
        else
        {
            taggerAgent = agentTransform.GetComponent<TaggerAgent>();
            if (taggerAgent == null)
            {
                Debug.LogError("AgentItemCounter must be attached to a Runner or Tagger agent!");
                enabled = false;
                return;
            }
        }
        
        // Create world space canvas if not already assigned
        if (worldSpaceCanvas == null)
        {
            CreateWorldSpaceCanvas();
        }
        
        // Create text component if not already assigned
        if (itemCountText == null)
        {
            CreateItemCountText();
        }
        
        // Register with UIItemDisplay for screen display
        UIItemDisplay.RegisterAgent(this);
    }
    
    void OnDestroy()
    {
        // Unregister from UIItemDisplay when destroyed
        UIItemDisplay.UnregisterAgent(this);
    }
    
    void CreateWorldSpaceCanvas()
    {
        // Create a new GameObject for the canvas
        GameObject canvasObj = new GameObject("ItemCountCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(0, heightAboveAgent, 0); // Position closer to the agent
        canvasObj.transform.localRotation = Quaternion.identity;
        
        // Add Canvas component
        worldSpaceCanvas = canvasObj.AddComponent<Canvas>();
        worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
        
        // Set canvas size
        RectTransform rectTransform = worldSpaceCanvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(canvasSize, 0.8f); // Wider canvas to prevent text wrapping
        
        // Add CanvasScaler for consistent sizing
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;
        
        // Ensure text is always facing the camera
        canvasObj.AddComponent<Billboard>();
    }
    
    void CreateItemCountText()
    {
        // Create a new GameObject for the text
        GameObject textObj = new GameObject("ItemCountText");
        textObj.transform.SetParent(worldSpaceCanvas.transform, false);
        
        // Position the text in the center of the canvas
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Add TextMeshProUGUI component
        itemCountText = textObj.AddComponent<TextMeshProUGUI>();
        itemCountText.alignment = TextAlignmentOptions.Center;
        itemCountText.fontSize = fontSize;
        
        // Set bold for better visibility
        itemCountText.fontStyle = FontStyles.Bold;
        
        // Add black outline for better visibility against any background
        itemCountText.outlineWidth = 0.2f;
        itemCountText.outlineColor = Color.black;
        
        // Set color based on agent type
        itemCountText.color = isRunner ? Color.green : Color.red;
        itemCountText.text = "0";
    }
    
    void LateUpdate()
    {
        // Update the item count text based on agent type
        if (isRunner)
        {
            int wallBalls = runnerAgent != null ? runnerAgent.GetCurrentWallBalls() : 0;
            itemCountText.text = $"WallBalls: {wallBalls}";
        }
        else
        {
            int freezeBalls = taggerAgent != null ? taggerAgent.GetCurrentFreezeBalls() : 0;
            itemCountText.text = $"FreezeBalls: {freezeBalls}";
        }
        
        // Scale the text based on distance to camera for better visibility from overview camera
        ScaleBasedOnCameraDistance();
    }
    
    void ScaleBasedOnCameraDistance()
    {
        if (mainCamera != null && worldSpaceCanvas != null)
        {
            // Get distance to camera
            float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
            
            // Adjust scale based on distance
            // The further away, the larger the scale to maintain visibility
            float scaleMultiplier = Mathf.Clamp(distance / 10f, 1f, 3f);
            
            // Apply the scale to the canvas
            worldSpaceCanvas.transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);
        }
    }
    
    // Helper methods for the UI display
    public bool IsRunner() => isRunner;
    public string GetItemText()
    {
        if (isRunner)
        {
            int wallBalls = runnerAgent != null ? runnerAgent.GetCurrentWallBalls() : 0;
            return $"WallBalls: {wallBalls}";
        }
        else
        {
            int freezeBalls = taggerAgent != null ? taggerAgent.GetCurrentFreezeBalls() : 0;
            return $"FreezeBalls: {freezeBalls}";
        }
    }
    public Color GetTextColor() => isRunner ? Color.green : Color.red;
    public Transform GetTransform() => transform;
} 