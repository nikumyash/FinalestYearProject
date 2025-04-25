using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class TaggerAgent : Agent
{
    [Header("Agent Settings")]
    [SerializeField] private int maxFreezeBalls = 3;
    [SerializeField] private GameObject freezeBallPrefab;
    [SerializeField] private Transform freezeBallSpawnPoint;
    [SerializeField] private float freezeBallSpeed = 3f;
    [SerializeField] private float freezeBallLifetime = 3f;
    [SerializeField] private float shootCooldown = 1.0f; // Cooldown time in seconds
    
    [Header("References")]
    [SerializeField] private AgentMovement agentMovement;
    
    // Freeze ball variables
    public int currentFreezeBalls = 0;
    
    // Events
    public delegate void TaggerEvent();
    public event TaggerEvent OnFreezeBallCollected;
    public event TaggerEvent OnFreezeBallShot;
    
    private float shootCooldownTimer = 0f; // Timer to track cooldown
    private bool canShoot = true; // Flag to check if shooting is allowed
    
    protected override void Awake()
    {
        // Call base implementation first to ensure communicator is registered early
        base.Awake();
        
        // Log registration for debugging
        Debug.Log("TaggerAgent registered with Unity ML-Agents framework.");
        
        // Ensure this agent has the "Tagger" tag
        gameObject.tag = "Tagger";
        
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
            OnFreezeBallCollected += GameManager.Instance.NotifyFreezeBallCollected;
            OnFreezeBallShot += GameManager.Instance.NotifyFreezeBallUsed;
            
            // Update parameters from current lesson
            if (GameManager.Instance.CurrentLesson != null)
            {
                maxFreezeBalls = GameManager.Instance.CurrentLesson.max_freezeballs;
                shootCooldown = GameManager.Instance.CurrentLesson.shoot_cooldown;
                freezeBallSpeed = GameManager.Instance.CurrentLesson.freezeball_speed;
                Debug.Log($"Tagger using lesson parameters: maxFreezeBalls={maxFreezeBalls}, shootCooldown={shootCooldown}, freezeBallSpeed={freezeBallSpeed}");
            }
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            OnFreezeBallCollected -= GameManager.Instance.NotifyFreezeBallCollected;
            OnFreezeBallShot -= GameManager.Instance.NotifyFreezeBallUsed;
        }
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset agent state
        currentFreezeBalls = 0;
        
        // Reset shooting cooldown
        canShoot = true;
        shootCooldownTimer = 0f;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Basic observations
        float perfreeze = (float)currentFreezeBalls/maxFreezeBalls;
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.forward);
        sensor.AddObservation(perfreeze);
        sensor.AddObservation(canShoot);
        
        // Add more observations as needed for the specific game mechanics
        // These will be customized by the user as per their needs
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Parse the discrete actions
        int moveForwardAction = actions.DiscreteActions[0]; // 0: none, 1: forward, 2: backward
        int rotateAction = actions.DiscreteActions[1];      // 0: none, 1: left, 2: right
        int strafeAction = actions.DiscreteActions[2];      // 0: none, 1: left, 2: right
        int shootAction = actions.DiscreteActions[3];       // 0: none, 1: shoot
        
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
        
        // Handle shooting
        if (shootAction == 1 && currentFreezeBalls > 0)
        {
            ShootFreezeBall();
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
        
        // Shooting
        if (Input.GetKey(KeyCode.Space) && canShoot)
        {
            discreteActionsOut[3] = 1; // Shoot
        }
        else
        {
            discreteActionsOut[3] = 0; // None
        }
    }
    
    private void Update()
    {
        // Update cooldown timer
        if (!canShoot)
        {
            shootCooldownTimer -= Time.deltaTime;
            if (shootCooldownTimer <= 0f)
            {
                canShoot = true;
            }
        }
    }
    
    private void ShootFreezeBall()
    {
        // Check if we can shoot and have ammo
        if (!canShoot || currentFreezeBalls <= 0) return;
        
        // Set cooldown
        canShoot = false;
        shootCooldownTimer = shootCooldown;
        
        // Reduce ammo
        currentFreezeBalls--;
        
        // Reward for shooting freeze ball
        AddReward(0.1f);
        Debug.Log("Rewarding tagger for shooting freeze ball: +0.1");
        
        Debug.Log($"Tagger shooting freeze ball. Remaining: {currentFreezeBalls}");
        
        // Create freeze ball projectile
        if (freezeBallSpawnPoint != null && freezeBallPrefab != null)
        {
            GameObject freezeBall = Instantiate(freezeBallPrefab, freezeBallSpawnPoint.position, freezeBallSpawnPoint.rotation);
            
            // Set it as a projectile
            FreezeBall freezeBallComponent = freezeBall.GetComponent<FreezeBall>();
            if (freezeBallComponent != null)
            {
                freezeBallComponent.SetAsProjectile(true);
                // Pass a reference to this agent to the freeze ball component
                freezeBallComponent.SetOwner(this);
            }
            
            Rigidbody rb = freezeBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Launch the freeze ball
                rb.velocity = transform.forward * freezeBallSpeed;
                
                // Set freeze ball to destroy after lifetime
                Destroy(freezeBall, freezeBallLifetime);
            }
        }
        
        OnFreezeBallShot?.Invoke();
    }
    
    // Add OnCollisionEnter to handle non-trigger collisions
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Tagger collision with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}, Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        
        // Check for runner contact - if tagger has freezeballs, freeze the runner directly
        if (collision.gameObject.CompareTag("Runner") && currentFreezeBalls > 0)
        {
            RunnerAgent runner = collision.gameObject.GetComponent<RunnerAgent>();
            if (runner != null && !runner.IsFrozen)
            {
                // Use a freezeball
                currentFreezeBalls--;
                
                // Freeze the runner
                runner.Freeze();
                
                // Reward for freezing runner by direct contact
                AddReward(1.0f);
                Debug.Log("Rewarding tagger for freezing runner (direct contact): +1.0");
                
                // Notify about freezeball use
                OnFreezeBallShot?.Invoke();
                
                // Notify game manager about freezing by touch
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.NotifyFreezeByTouch();
                }
                
                // Log the direct freeze
                Debug.Log($"Tagger directly froze runner using a freezeball. Remaining: {currentFreezeBalls}");
            }
        }
        
        // Add specific check for wall collisions
        if (collision.gameObject.CompareTag("Wall"))
        {
            Debug.Log($"TAGGER HIT WALL: {collision.gameObject.name} - This should be counted in metrics!");
            
            // Removed penalty for colliding with wall during pursuit
            
            // Directly notify GameManager about tagger hitting wall
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyWallHitByTagger();
                Debug.Log("DIRECT NotifyWallHitByTagger called from TaggerAgent");
            }
        }
    }
    
    // Keep the existing OnTriggerEnter for trigger-based collisions
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Tagger trigger with: {other.gameObject.name}, Tag: {other.gameObject.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        
        // Check for collectable items
        if (other.CompareTag("FreezeBall") && currentFreezeBalls < maxFreezeBalls)
        {
            currentFreezeBalls++;
            Destroy(other.gameObject);
            
            // Removed reward for collecting freeze ball
            // Debug.Log("Rewarding tagger for collecting freeze ball: +0.2");
            
            OnFreezeBallCollected?.Invoke();
            
            // Notify game manager to respawn a new freeze ball
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RespawnFreezeball();
            }
        }
        
        // Check for runner contact - if tagger has freezeballs, freeze the runner directly
        if (other.CompareTag("Runner") && currentFreezeBalls > 0)
        {
            RunnerAgent runner = other.GetComponent<RunnerAgent>();
            if (runner != null && !runner.IsFrozen)
            {
                // Use a freezeball
                currentFreezeBalls--;
                
                // Freeze the runner
                runner.Freeze();
                
                // Reward for freezing runner by direct contact
                AddReward(1.0f);
                Debug.Log("Rewarding tagger for freezing runner (direct contact): +1.0");
                
                // Notify about freezeball use
                OnFreezeBallShot?.Invoke();
                
                // Notify game manager about freezing by touch
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.NotifyFreezeByTouch();
                }
                
                // Log the direct freeze
                Debug.Log($"Tagger directly froze runner using a freezeball. Remaining: {currentFreezeBalls}");
            }
        }
        
        // Add specific check for wall collisions through trigger
        if (other.CompareTag("Wall"))
        {
            Debug.Log($"TAGGER TRIGGER HIT WALL: {other.gameObject.name} - This should be counted in metrics!");
            
            // Removed penalty for colliding with wall during pursuit
            
            // Directly notify GameManager about tagger hitting wall
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyWallHitByTagger();
                Debug.Log("DIRECT NotifyWallHitByTagger called from TaggerAgent (trigger)");
            }
        }
    }
    
    // Method to be called when this tagger's freeze ball hits a runner
    public void RewardFreezeBallHit()
    {
        // Premium reward for skill-based freezing with a freeze ball
        AddReward(1.5f);
        Debug.Log("Rewarding tagger for freezing runner with freeze ball: +1.5");
    }
    
    // Method to be called when this tagger's freeze ball misses
    public void PenalizeMissedFreezeBall()
    {
        // Removed penalty for missing with freeze ball
        Debug.Log("Freeze ball missed but no penalty applied");
    }
    
    // Method to be called when this tagger's freeze ball hits a wall
    public void PenalizeFreezeBallHitWall()
    {
        // Removed penalty for hitting wall with freeze ball
        Debug.Log("Freeze ball hit wall but no penalty applied");
    }
    
    // Add method to get current freeze balls for UI
    public int GetCurrentFreezeBalls()
    {
        return currentFreezeBalls;
    }
}