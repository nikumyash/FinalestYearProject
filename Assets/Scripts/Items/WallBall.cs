using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBall : MonoBehaviour
{
    [SerializeField] private GameObject collectEffect;
    
    private bool isCollected = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        
        // Handle collection by runner
        RunnerAgent runner = other.GetComponent<RunnerAgent>();
        if (runner != null)
        {
            isCollected = true;
            PlayCollectEffect();
            
            // Object will be destroyed by the RunnerAgent component
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
} 