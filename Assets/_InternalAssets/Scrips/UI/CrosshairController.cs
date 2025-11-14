using System.Collections;
using UnityEngine;

/// <summary>
/// Manages crosshair elements: static dot, weapon lines (animated on shot), and kill lines (shown on target kill).
/// </summary>
public class CrosshairController : MonoBehaviour
{
    [Header("Static Elements")]
    [SerializeField] private GameObject staticDot;

    [Header("Weapon Lines (+ pattern)")]
    [SerializeField] private GameObject weaponLinesRoot;
    [SerializeField] private Animator weaponLinesAnimator;
    [SerializeField] private string weaponLinesShotTrigger = "Shot";

    [Header("Kill Lines (X pattern)")]
    [SerializeField] private GameObject killLinesRoot;
    [SerializeField] private float killLinesDuration = 1.5f;

    private Coroutine killLinesCoroutine;
    private bool weaponEquipped;

    private void Awake()
    {
        if (weaponLinesRoot != null)
        {
            weaponLinesRoot.SetActive(false);
        }

        if (killLinesRoot != null)
        {
            killLinesRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Sets whether weapon is equipped. Controls weapon lines visibility.
    /// </summary>
    public void SetWeaponEquipped(bool equipped)
    {
        weaponEquipped = equipped;
        UpdateWeaponLinesVisibility();
    }

    /// <summary>
    /// Triggers weapon lines recoil animation on shot.
    /// </summary>
    public void TriggerShotAnimation()
    {
        if (!weaponEquipped || weaponLinesAnimator == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(weaponLinesShotTrigger))
        {
            weaponLinesAnimator.SetTrigger(weaponLinesShotTrigger);
        }
    }

    /// <summary>
    /// Shows kill lines for configured duration.
    /// </summary>
    public void ShowKillLines()
    {
        if (killLinesRoot == null)
        {
            return;
        }

        if (killLinesCoroutine != null)
        {
            StopCoroutine(killLinesCoroutine);
        }

        killLinesCoroutine = StartCoroutine(KillLinesRoutine());
    }

    private void UpdateWeaponLinesVisibility()
    {
        if (weaponLinesRoot != null)
        {
            weaponLinesRoot.SetActive(weaponEquipped);
        }
    }

    private IEnumerator KillLinesRoutine()
    {
        if (killLinesRoot != null)
        {
            killLinesRoot.SetActive(true);
        }

        float duration = Mathf.Max(0f, killLinesDuration);
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        if (killLinesRoot != null)
        {
            killLinesRoot.SetActive(false);
        }

        killLinesCoroutine = null;
    }

    private void OnDisable()
    {
        if (killLinesCoroutine != null)
        {
            StopCoroutine(killLinesCoroutine);
            killLinesCoroutine = null;
        }

        if (killLinesRoot != null)
        {
            killLinesRoot.SetActive(false);
        }
    }
}

