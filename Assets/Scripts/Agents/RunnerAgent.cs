using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class RunnerAgent : Agent
{
    [Header("Agent Settings")]
    [SerializeField] private float unfreezeTime = 3f;
    [SerializeField] private float freezeRange = 3f;
    [SerializeField] private int maxWallBalls = 2;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private Transform wallSpawnPoint;
    [SerializeField] private GameObject freezeEffectPrefab;
    [SerializeField] private float wallCooldown = 1.0f; // Cooldown time in seconds
    
    [Header("References")]
    [SerializeField] private AgentMovement agentMovement;
    [SerializeField] private Renderer agentRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material frozenMaterial;
    
    // Wall variables
    private int currentWallBalls = 0;
    
    // Freeze variables
    private bool isFrozen = false;
    private float unfreezeCounter = 0f;
    private GameObject freezeEffect;
    
    // Wall cooldown variables
    private float wallCooldownTimer = 0f; // Timer to track cooldown
    private bool canUseWall = true; // Flag to check if wall creation is allowed
    
    // Events
    public delegate void RunnerEvent();
    public event RunnerEvent OnFreeze;
    public event RunnerEvent OnUnfreeze;
    public event RunnerEvent OnWallBallCollected;
    public event RunnerEvent OnWallUsed;
    
    // Properties
    public bool IsFrozen => isFrozen;
    public float WallCooldownPercent => !canUseWall ? wallCooldownTimer / wallCooldown : 0f;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (agentMovement == null)
        {
            agentMovement = GetComponent<AgentMovement>();
            if (agentMovement == null)
            {
                agentMovement = gameObject.AddComponent<AgentMovement>();
            }
        }
    }
    
    private void Start()
    {
        // Subscribe to game events
        if (GameManager.Instance != null)
        {
            OnFreeze += GameManager.Instance.NotifyFreeze;
            OnUnfreeze += GameManager.Instance.NotifyUnfreeze;
            OnWallBallCollected += GameManager.Instance.NotifyWallBallCollected;
            OnWallUsed += GameManager.Instance.NotifyWallUsed;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            OnFreeze -= GameManager.Instance.NotifyFreeze;
            OnUnfreeze -= GameManager.Instance.NotifyUnfreeze;
            OnWallBallCollected -= GameManager.Instance.NotifyWallBallCollected;
            OnWallUsed -= GameManager.Instance.NotifyWallUsed;
        }
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset agent state
        isFrozen = false;
        unfreezeCounter = 0f;
        currentWallBalls = 0;
        
        // Clear existing freeze effect
        if (freezeEffect != null)
        {
            Destroy(freezeEffect);
            freezeEffect = null;
        }
        
        if (agentRenderer != null && normalMaterial != null)
        {
            agentRenderer.material = normalMaterial;
        }
        
        // Reset wall cooldown
        canUseWall = true;
        wallCooldownTimer = 0f;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Basic observations
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.forward);
        sensor.AddObservation(isFrozen ? 1 : 0);
        sensor.AddObservation(currentWallBalls);
        sensor.AddObservation(canUseWall);
        // Add more observations as needed for the specific game mechanics
        // These will be customized by the user as per their needs
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        // If frozen, can't move
        if (isFrozen) return;
        
        // Parse the discrete actions
        int moveForwardAction = actions.DiscreteActions[0]; // 0: none, 1: forward, 2: backward
        int rotateAction = actions.DiscreteActions[1];      // 0: none, 1: left, 2: right
        int strafeAction = actions.DiscreteActions[2];      // 0: none, 1: left, 2: right
        int useWallAction = actions.DiscreteActions[3];     // 0: none, 1: use wall
        
        // Handle movement
        if (moveForwardAction == 1)
        {
            agentMovement.MoveForward();
        }
        else if (moveForwardAction == 2)
        {
            agentMovement.MoveBackward();
        }
        
        // Handle rotation
        if (rotateAction == 1)
        {
            agentMovement.RotateLeft();
        }
        else if (rotateAction == 2)
        {
            agentMovement.RotateRight();
        }
        
        // Handle strafing
        if (strafeAction == 1)
        {
            agentMovement.MoveLeft();
        }
        else if (strafeAction == 2)
        {
            agentMovement.MoveRight();
        }
        
        // Handle wall use - respect cooldown
        if (useWallAction == 1 && currentWallBalls > 0 && canUseWall)
        {
            UseWall();
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        
        // Forward/Backward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1; // Forward
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2; // Backward
        }
        else
        {
            discreteActionsOut[0] = 0; // None
        }
        
        // Rotation
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 1; // Left
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 2; // Right
        }
        else
        {
            discreteActionsOut[1] = 0; // None
        }
        
        // Strafing
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[2] = 1; // Strafe Left
        }
        else if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[2] = 2; // Strafe Right
        }
        else
        {
            discreteActionsOut[2] = 0; // None
        }
        
        // Wall creation - only allow if not in cooldown
        if (Input.GetKey(KeyCode.Space) && canUseWall)
        {
            discreteActionsOut[3] = 1; // Use Wall
        }
        else
        {
            discreteActionsOut[3] = 0; // None
        }
    }
    
    private void FixedUpdate()
    {
        // Check if frozen and handle unfreezing logic
        if (isFrozen)
        {
            // Check for nearby unfreezing runners
            CheckForUnfreeze();
        }
    }
    
    private void CheckForUnfreeze()
    {
        // Check if there's a non-frozen runner in range
        bool runnerInRange = false;
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, freezeRange);
        foreach (var collider in colliders)
        {
            RunnerAgent runner = collider.GetComponent<RunnerAgent>();
            if (runner != null && runner != this && !runner.IsFrozen)
            {
                runnerInRange = true;
                break;
            }
        }
        
        if (runnerInRange)
        {
            unfreezeCounter += Time.fixedDeltaTime;
            if (unfreezeCounter >= unfreezeTime)
            {
                Unfreeze();
            }
        }
        else
        {
            // Reset counter if no runner in range
            unfreezeCounter = 0f;
        }
    }
    
    private void UseWall()
    {
        // Check if we can use wall and have wall balls
        if (!canUseWall || currentWallBalls <= 0) return;
        
        // Set cooldown
        canUseWall = false;
        wallCooldownTimer = wallCooldown;
        
        // Reduce wall ball count
        currentWallBalls--;
        
        Debug.Log($"Runner creating wall. Remaining wall balls: {currentWallBalls}");
        
        // Create wall at spawn point
        if (wallSpawnPoint != null && wallPrefab != null)
        {
            GameObject wall = Instantiate(wallPrefab, wallSpawnPoint.position, wallSpawnPoint.rotation);
            Destroy(wall, 3f); // Wall lasts 3 seconds
        }
        
        OnWallUsed?.Invoke();
    }
    
    public void Freeze()
    {
        if (isFrozen) return;
        
        isFrozen = true;
        unfreezeCounter = 0f;
        
        // Spawn freeze effect
        if (freezeEffectPrefab != null)
        {
            freezeEffect = Instantiate(freezeEffectPrefab, transform.position, Quaternion.identity);
            freezeEffect.transform.SetParent(transform);
        }
        
        // Stop all movement
        agentMovement.StopMovement();
        
        if (agentRenderer != null && frozenMaterial != null)
        {
            agentRenderer.material = frozenMaterial;
        }
        
        OnFreeze?.Invoke();
    }
    
    public void Unfreeze()
    {
        if (!isFrozen) return;
        
        isFrozen = false;
        unfreezeCounter = 0f;
        
        // Remove freeze effect
        if (freezeEffect != null)
        {
            Destroy(freezeEffect);
            freezeEffect = null;
        }
        
        if (agentRenderer != null && normalMaterial != null)
        {
            agentRenderer.material = normalMaterial;
        }
        
        OnUnfreeze?.Invoke();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check for collectable items
        if (other.CompareTag("WallBall") && currentWallBalls < maxWallBalls)
        {
            currentWallBalls++;
            Destroy(other.gameObject);
            OnWallBallCollected?.Invoke();
            
            // Notify game manager to respawn a new wall ball
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RespawnWallball();
            }
        }
        
        // Check for tagger touch
        if (other.CompareTag("Tagger") && !isFrozen)
        {
            Freeze();
        }
        
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw freeze range
        Gizmos.color = isFrozen ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, freezeRange);
    }
    
    private void Update()
    {
        // Update cooldown timer for wall usage
        if (!canUseWall)
        {
            wallCooldownTimer -= Time.deltaTime;
            if (wallCooldownTimer <= 0f)
            {
                canUseWall = true;
            }
        }
    }
    
} 