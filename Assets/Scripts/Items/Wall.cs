using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private GameObject destroyEffect;
    
    private float timer;
    private BoxCollider wallCollider;
    private Rigidbody wallRigidbody;
    private RunnerAgent creator; // Reference to the runner who created this wall
    
    private void Start()
    {
        // Get wall lifetime from current lesson if available
        if (GameManager.Instance != null && GameManager.Instance.CurrentLesson != null)
        {
            lifetime = GameManager.Instance.CurrentLesson.wall_lifetime;
            Debug.Log($"Wall using lesson parameter: lifetime={lifetime}s");
        }
        
        timer = lifetime;
        
        // Ensure wall is properly set up for solid collisions
        SetupPhysics();
        
        // Add fade-in animation or effect
        StartCoroutine(FadeIn());
        
        // Log that the wall has been created for debugging
        Debug.Log($"Wall created at {transform.position}. Testing collision system...");
        
        // Add a quick test to verify if the collider is working
        StartCoroutine(TestCollider());
    }
    
    // Method to set the creator of this wall
    public void SetCreator(RunnerAgent runner)
    {
        creator = runner;
    }
    
    private IEnumerator TestCollider()
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"Wall collider state: isTrigger={wallCollider.isTrigger}, enabled={wallCollider.enabled}, size={wallCollider.size}");
        
        // Count the number of colliders on this object
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        Debug.Log($"Wall has {colliders.Length} BoxCollider components");
        
        // No need to test collisions with a dummy object, just verify the setup is correct
    }
    
    private void SetupPhysics()
    {
        // Make sure the wall has a collider
        wallCollider = GetComponent<BoxCollider>();
        if (wallCollider == null)
        {
            wallCollider = gameObject.AddComponent<BoxCollider>();
        }
        
        // Make sure it's NOT a trigger (so it blocks physical movement)
        wallCollider.isTrigger = false;
        
        // Create a physics material to prevent sliding along the wall
        PhysicMaterial wallPhysicsMaterial = new PhysicMaterial()
        {
            name = "WallPhysicsMaterial",
            dynamicFriction = 0.8f,
            staticFriction = 0.8f,
            bounciness = 0.1f,
            frictionCombine = PhysicMaterialCombine.Maximum
        };
        wallCollider.material = wallPhysicsMaterial;
        
        // Add a Rigidbody to ensure proper collision detection
        wallRigidbody = GetComponent<Rigidbody>();
        if (wallRigidbody == null)
        {
            wallRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        
        // Make the wall immovable
        wallRigidbody.isKinematic = true;
        wallRigidbody.useGravity = false;
        wallRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Get second collider for trigger events
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        if (colliders.Length > 1) 
        {
            // If a second collider exists, configure it as a trigger
            colliders[1].isTrigger = true;
            Debug.Log("Second collider configured as trigger");
        }
        
        // Ensure wall has correct tag
        gameObject.tag = "Wall";
    }
    
    private void Update()
    {
        timer -= Time.deltaTime;
        
        if (timer <= 0)
        {
            DestroyWall();
        }
        // Start fading out when close to destruction
        else if (timer <= 0.5f)
        {
            // Fade out
            float alpha = timer / 0.5f;
            SetAlpha(alpha);
        }
    }
    
    private IEnumerator FadeIn()
    {
        float duration = 0.2f;
        float timer = 0;
        
        SetAlpha(0);
        
        while (timer < duration)
        {
            float alpha = timer / duration;
            SetAlpha(alpha);
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        SetAlpha(1);
    }
    
    private void SetAlpha(float alpha)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            Color color = renderer.material.color;
            color.a = alpha;
            renderer.material.color = color;
        }
    }
    
    private void DestroyWall()
    {
        if (destroyEffect != null)
        {
            GameObject effect = Instantiate(destroyEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        Destroy(gameObject);
    }
    
    // Handle physical collisions
    private void OnCollisionEnter(Collision collision)
    {
        // Only log collisions for debugging
        Debug.Log($"Wall collision detected with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");
        
        // Reward the wall creator if a tagger hits this wall
        if (creator != null && collision.gameObject.CompareTag("Tagger"))
        {
            // Wall blocking Tagger path reward - INCREASED FROM 0.6 to 1.0
            creator.AddReward(1.0f);
            Debug.Log($"Rewarding runner for wall blocking tagger path: +1.0");
        }
        
        // No longer handling notifications here as it's done by the colliding objects
    }
    
    // Handle both trigger and collision interactions
    private void OnTriggerEnter(Collider other)
    {
        // Only log trigger collisions for debugging
        Debug.Log($"Wall trigger detected with: {other.gameObject.name}, Tag: {other.gameObject.tag}");
        
        // Reward the wall creator if a freeze ball projectile hits this wall
        if (creator != null && other.CompareTag("FreezeBallProjectile"))
        {
            // Wall blocking freeze ball reward - INCREASED FROM 0.4 to 0.8
            creator.AddReward(0.8f);
            Debug.Log($"Rewarding runner for wall blocking freeze ball: +0.8");
        }
        
        // No longer handling notifications here as it's done by the colliding objects
    }
} 