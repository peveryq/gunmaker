using UnityEngine;

/// <summary>
/// Marks a collider as part of a shooting target and identifies which hit zone it represents.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShootingTargetZone : MonoBehaviour
{
    [SerializeField] private ShootingTarget.HitZone zone = ShootingTarget.HitZone.Normal;
    [SerializeField] private ShootingTarget target;

    public ShootingTarget.HitZone Zone => zone;
    public ShootingTarget Target => target;

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponentInParent<ShootingTarget>();
        }

        if (target == null)
        {
            Debug.LogWarning($"ShootingTargetZone on {name} requires a parent ShootingTarget.");
        }
    }

    /// <summary>
    /// Helper for projectiles to report a hit without needing to locate the parent target manually.
    /// </summary>
    public void ReportHit(Vector3 point, Vector3 normal)
    {
        if (target == null)
        {
            return;
        }

        target.RegisterHit(zone, point, normal);
    }
}

