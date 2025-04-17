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
            OnFreezeBallCollected += GameManager.Instance.NotifyFreezeBallCollected;
            OnFreezeBallShot += GameManager.Instance.NotifyFreezeBallUsed;
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
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.forward);
        sensor.AddObservation(currentFreezeBalls);
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
    
    private void OnTriggerEnter(Collider other)
    {
        // Check for collectable items
        if (other.CompareTag("FreezeBall") && currentFreezeBalls < maxFreezeBalls)
        {
            currentFreezeBalls++;
            Destroy(other.gameObject);
            OnFreezeBallCollected?.Invoke();
            
            // Notify game manager to respawn a new freeze ball
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RespawnFreezeball();
            }
        }
    }
} 