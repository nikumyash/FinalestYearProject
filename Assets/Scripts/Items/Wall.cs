using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private GameObject destroyEffect;
    
    private float timer;
    
    private void Start()
    {
        timer = lifetime;
        
        // Add fade-in animation or effect
        StartCoroutine(FadeIn());
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
    
    private void OnTriggerEnter(Collider other)
    {
        // Freeze balls are blocked by walls
        if (other.CompareTag("FreezeBallProjectile"))
        {
            Destroy(other.gameObject);
        }
    }
} 