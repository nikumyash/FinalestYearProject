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
    [SerializeField] private GameObject unfreezeRangeIndicator; // Reference to the unfreeze range indicator child object
    
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
    
    // Added fields for freeze time tracking
    private float freezeStartTime = 0f;
    private float currentFreezeTime = 0f;
    
    // Added fields for survival time tracking
    private float lastUnfreezeTime = 0f;
    private bool hasSurvivalTimerStarted = false;
    
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
        // Call base implementation first to ensure communicator is registered early
        base.Awake();
        
        // Log registration for debugging
        Debug.Log("RunnerAgent registered with Unity ML-Agents framework.");
        
        // Ensure this agent has the "Runner" tag
        gameObject.tag = "Runner";
        
        if (agentMovement == null)
        {
            agentMovement = GetComponent<AgentMovement>();
            if (agentMovement == null)
            {
                agentMovement = gameObject.AddComponent<AgentMovement>();
            }
        }
        
        // Make sure the unfreeze range indicator is initially disabled
        if (unfreezeRangeIndicator != null)
        {
            unfreezeRangeIndicator.SetActive(false);
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
            
            // Update parameters from current lesson
            if (GameManager.Instance.CurrentLesson != null)
            {
                maxWallBalls = GameManager.Instance.CurrentLesson.max_wallballs;
                wallCooldown = GameManager.Instance.CurrentLesson.wall_cooldown;
                
                // Apply runner speed multiplier to agent movement
                if (agentMovement != null)
                {
                    float originalSpeed = agentMovement.GetMoveSpeed();
                    float multiplier = GameManager.Instance.CurrentLesson.runner_speed_multiplier;
                    agentMovement.SetMoveSpeed(originalSpeed * multiplier);
                    Debug.Log($"Runner speed adjusted: original={originalSpeed}, multiplier={multiplier}, final={originalSpeed * multiplier}");
                }
                
                Debug.Log($"Runner using lesson parameters: maxWallBalls={maxWallBalls}, wallCooldown={wallCooldown}");
            }
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
        
        // Start tracking survival time from episode beginning
        lastUnfreezeTime = Time.time;
        hasSurvivalTimerStarted = true;
        
        // Clear existing freeze effect
        if (freezeEffect != null)
        {
            Destroy(freezeEffect);
            freezeEffect = null;
        }
        
        // Hide unfreeze range indicator
        if (unfreezeRangeIndicator != null)
        {
            unfreezeRangeIndicator.SetActive(false);
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
            // Update total freeze time
            currentFreezeTime += Time.fixedDeltaTime;
            
            // Check for nearby unfreezing runners
            CheckForUnfreeze();
        }
        else
        {
            // Survival reward (per timestep) - REDUCED FROM 0.005 to 0.001
            AddReward(0.001f);
        }
    }
    
    private void CheckForUnfreeze()
    {
        // Check if there's a non-frozen runner in range
        bool runnerInRange = false;
        RunnerAgent unfreezeHelper = null;
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, freezeRange);
        foreach (var collider in colliders)
        {
            RunnerAgent runner = collider.GetComponent<RunnerAgent>();
            if (runner != null && runner != this && !runner.IsFrozen)
            {
                runnerInRange = true;
                unfreezeHelper = runner;
                break;
            }
        }
        
        if (runnerInRange && unfreezeHelper != null)
        {
            // While unfreezing teammate reward (per timestep) - REDUCED FROM 0.02 to 0.005
            unfreezeHelper.AddReward(0.005f);
            Debug.Log($"Rewarding runner for unfreezing teammate (in progress): +0.005");
            
            unfreezeCounter += Time.fixedDeltaTime;
            if (unfreezeCounter >= unfreezeTime)
            {
                Unfreeze();
                
                // Successfully unfreezing teammate reward
                unfreezeHelper.AddReward(1.5f);
                Debug.Log($"Rewarding runner for successfully unfreezing teammate: +1.5");
            }
        }
        else
        {
            // Check if there was an unfreeze attempt in progress that's now interrupted
            if (unfreezeCounter > 0)
            {
                // Notify GameManager about an unsuccessful unfreeze attempt
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.NotifyUnsuccessfulUnfreezeAttempt();
                }
                
                // Log the interrupted unfreeze attempt
                Debug.Log($"Unfreeze attempt interrupted at {unfreezeCounter:F2}/{unfreezeTime} seconds");
            }
            
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
            
            // Set this runner as the creator of the wall
            Wall wallComponent = wall.GetComponent<Wall>();
            if (wallComponent != null)
            {
                wallComponent.SetCreator(this);
            }
            
            // No need to manually Destroy the wall, it will handle its own lifetime based on lesson settings
        }
        
        // Add reward for creating wall - REDUCED FROM 0.3 to 0.1
        AddReward(0.1f);
        Debug.Log("Rewarding runner for creating wall: +0.1");
        
        OnWallUsed?.Invoke();
    }
    
    public void Freeze()
    {
        if (isFrozen) return;
        
        // If survival timer was active, calculate survival time and report it
        if (hasSurvivalTimerStarted)
        {
            float survivalTime = Time.time - lastUnfreezeTime;
            
            // Report survival time to GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateSurvivalTimeAfterFreeze(survivalTime);
            }
            
            // Log survival time
            Debug.Log($"Runner survived for {survivalTime:F2} seconds since last unfreeze");
            
            // Stop survival timer while frozen
            hasSurvivalTimerStarted = false;
        }
        
        isFrozen = true;
        unfreezeCounter = 0f;
        
        // Start tracking freeze time
        freezeStartTime = Time.time;
        currentFreezeTime = 0f;
        
        // Spawn freeze effect
        if (freezeEffectPrefab != null)
        {
            freezeEffect = Instantiate(freezeEffectPrefab, transform.position, Quaternion.identity);
            freezeEffect.transform.SetParent(transform);
        }
        
        // Show unfreeze range indicator
        if (unfreezeRangeIndicator != null)
        {
            unfreezeRangeIndicator.SetActive(true);
            // Update scale to match the freeze range if needed
            unfreezeRangeIndicator.transform.localScale = new Vector3(
                freezeRange * 2, 
                freezeRange * 2, 
                freezeRange * 2
            );
        }
        
        // Stop all movement
        agentMovement.StopMovement();
        
        if (agentRenderer != null && frozenMaterial != null)
        {
            agentRenderer.material = frozenMaterial;
        }
        
        // Negative reward for getting frozen
        AddReward(-1.0f);
        Debug.Log("Penalizing runner for getting frozen: -1.0");
        
        OnFreeze?.Invoke();
    }
    
    public void Unfreeze()
    {
        if (!isFrozen) return;
        
        // Calculate total freeze time before unfreezing
        float totalFreezeTime = currentFreezeTime;
        
        isFrozen = false;
        unfreezeCounter = 0f;
        
        // Start tracking survival time after being unfrozen
        lastUnfreezeTime = Time.time;
        hasSurvivalTimerStarted = true;
        
        // Update freeze time stats in GameManager
        if (GameManager.Instance != null)
        {
            // Add to total time spent freezing
            GameManager.Instance.AddFreezeTime(totalFreezeTime);
            
            // Update fastest unfreeze time if this one was faster
            GameManager.Instance.CheckFastestUnfreeze(totalFreezeTime);
        }
        
        // Remove freeze effect
        if (freezeEffect != null)
        {
            Destroy(freezeEffect);
            freezeEffect = null;
        }
        
        // Hide unfreeze range indicator
        if (unfreezeRangeIndicator != null)
        {
            unfreezeRangeIndicator.SetActive(false);
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
            
            // Reward for collecting wall ball removed
            // Keeping logging for tracking purposes
            Debug.Log("Wall ball collected - no immediate reward");
            
            OnWallBallCollected?.Invoke();
            
            // Notify game manager to respawn a new wall ball
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RespawnWallball();
            }
        }
        
        // Check for freeze ball projectile hit
        if (other.CompareTag("FreezeBallProjectile") && !isFrozen)
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
    
    // Add OnCollisionEnter to handle non-trigger collisions
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Runner collision with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}, Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        
        // Check for wall collisions
        if (collision.gameObject.CompareTag("Wall"))
        {
            Debug.Log($"RUNNER HIT WALL: {collision.gameObject.name}");
            // Apply any wall collision logic here
            // e.g., push back, stop movement, etc.
        }
        
        // No need to check for tagger collisions here as that's handled by the tagger
    }
    
    // Add method to report final survival times at end of episode
    protected override void OnDisable()
    {
        // Call the base class implementation first
        base.OnDisable();
        
        // If the agent is still alive and survival timer is active, report the final survival time
        if (hasSurvivalTimerStarted && !isFrozen && GameManager.Instance != null)
        {
            float finalSurvivalTime = Time.time - lastUnfreezeTime;
            GameManager.Instance.UpdateSurvivalTimeAfterFreeze(finalSurvivalTime);
            Debug.Log($"Runner survived for {finalSurvivalTime:F2} seconds until end of episode");
        }
    }
    
    // Add method to get current wall balls for UI
    public int GetCurrentWallBalls()
    {
        return currentWallBalls;
    }
} 