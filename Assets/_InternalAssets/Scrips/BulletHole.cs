using UnityEngine;
using System.Collections;

/// <summary>
/// Individual bullet hole decal with lifetime and fade
/// </summary>
public class BulletHole : MonoBehaviour
{
    private float lifetime;
    private System.Action onDestroy;
    
    public void Initialize(float life, System.Action onDestroyCallback)
    {
        lifetime = life;
        onDestroy = onDestroyCallback;
        
        // Start lifetime coroutine
        StopAllCoroutines();
        StartCoroutine(LifetimeCoroutine());
    }
    
    public void Reset()
    {
        StopAllCoroutines();
    }
    
    private IEnumerator LifetimeCoroutine()
    {
        // Wait for lifetime
        yield return new WaitForSeconds(lifetime);
        
        // Return to pool
        onDestroy?.Invoke();
    }
}

