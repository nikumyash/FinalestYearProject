using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeBall : MonoBehaviour
{
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private bool isProjectile = false;
    
    private bool isCollected = false;
    private bool hasHit = false;
    
    public void SetAsProjectile(bool isProj)
    {
        isProjectile = isProj;
        
        // Change tag to differentiate from collectible freeze balls
        if (isProj)
        {
            gameObject.tag = "FreezeBallProjectile";
        }
        else
        {
            gameObject.tag = "FreezeBall";
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (isCollected || hasHit) return;
        
        // If this is a collectible freeze ball (not a projectile)
        if (!isProjectile)
        {
            // Handle collection by tagger
            TaggerAgent tagger = other.GetComponent<TaggerAgent>();
            if (tagger != null)
            {
                isCollected = true;
                PlayCollectEffect();
                
                // Object will be destroyed by the TaggerAgent component
            }
        }
        // If this is a projectile freeze ball
        else
        {
            // Handle hitting a runner - only when it's a projectile
            RunnerAgent runner = other.GetComponent<RunnerAgent>();
            if (runner != null && !runner.IsFrozen)
            {
                hasHit = true;
                PlayHitEffect();
                
                // Freeze the runner
                runner.Freeze();
                
                // Notify game manager about freeze ball hit as projectile
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.NotifyFreezeBallHit();
                }
                
                // Destroy this projectile
                Destroy(gameObject);
            }
            // Handle collision with walls or other objects
            else if (other.CompareTag("Wall"))
            {
                hasHit = true;
                PlayHitEffect();
                
                // Directly notify GameManager about freeze ball hitting wall
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.NotifyWallHitByFreezeBallProjectile();
                    Debug.Log("DIRECT NotifyWallHitByFreezeBallProjectile called from FreezeBall");
                }
                
                Destroy(gameObject);
            }
            else if (!other.CompareTag("Tagger") && !other.CompareTag("FreezeBall") && !other.CompareTag("WallBall"))
            {
                hasHit = true;
                PlayHitEffect();
                Destroy(gameObject);
            }
        }
    }
    
    // Add physical collision handler for FreezeBall projectiles hitting walls
    private void OnCollisionEnter(Collision collision)
    {
        if (isProjectile && !hasHit && collision.gameObject.CompareTag("Wall"))
        {
            hasHit = true;
            PlayHitEffect();
            
            // Directly notify GameManager about freeze ball hitting wall
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyWallHitByFreezeBallProjectile();
                Debug.Log("DIRECT NotifyWallHitByFreezeBallProjectile called from FreezeBall (collision)");
            }
            
            Destroy(gameObject);
        }
    }
    
    private void PlayCollectEffect()
    {
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    private void PlayHitEffect()
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
} 