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
    
    private void Start()
    {
        timer = lifetime;
        
        // Ensure wall is properly set up for solid collisions
        SetupPhysics();
        
        // Add fade-in animation or effect
        StartCoroutine(FadeIn());
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
        // Check for tagger collisions
        if (collision.gameObject.CompareTag("Tagger"))
        {
            // Notify GameManager about tagger hitting a wall
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyWallHitByTagger();
            }
        }
        
        // Freeze balls are blocked by walls
        if (collision.gameObject.CompareTag("FreezeBallProjectile"))
        {
            // Notify GameManager about freezeball projectile hitting a wall
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyWallHitByFreezeBallProjectile();
            }
            
            Destroy(collision.gameObject);
        }
    }
    
    // Handle both trigger and collision interactions
    private void OnTriggerEnter(Collider other)
    {
        // Check for tagger collisions (in case tagger has trigger collider)
        if (other.CompareTag("Tagger"))
        {
            // Notify GameManager about tagger hitting a wall
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyWallHitByTagger();
            }
        }
        
        // Freeze balls are blocked by walls
        if (other.CompareTag("FreezeBallProjectile"))
        {
            // Notify GameManager about freezeball projectile hitting a wall
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyWallHitByFreezeBallProjectile();
            }
            
            Destroy(other.gameObject);
        }
    }
} 