using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Manages crosshair elements: static dot, weapon lines (animated on shot), hit lines (shown on target hit), and kill lines (shown on target kill).
/// </summary>
public class CrosshairController : MonoBehaviour
{
    public enum HitZone
    {
        Normal,
        Bullseye
    }

    [Header("Static Elements")]
    [SerializeField] private GameObject staticDot;

    [Header("Weapon Lines (+ pattern)")]
    [SerializeField] private GameObject weaponLinesRoot;
    [Tooltip("Four RectTransforms for weapon lines: top, bottom, left, right (in that order).")]
    [SerializeField] private RectTransform[] weaponLines;
    [Tooltip("Distance in pixels to move lines away from center when shooting.")]
    [SerializeField] private float recoilDistance = 10f;
    [Tooltip("Duration for returning lines to original position when shooting stops.")]
    [SerializeField] private float returnDuration = 0.2f;

    [Header("Hit Lines (X pattern - shown on hit)")]
    [SerializeField] private GameObject normalHitLinesRoot;
    [SerializeField] private GameObject bullseyeHitLinesRoot;
    [SerializeField] private float normalHitLinesDuration = 1.5f;
    [SerializeField] private float bullseyeHitLinesDuration = 1.5f;

    [Header("Kill Lines (X pattern - shown on kill)")]
    [SerializeField] private GameObject killLinesRoot;
    [SerializeField] private float killLinesDuration = 1.5f;

    private Coroutine normalHitLinesCoroutine;
    private Coroutine bullseyeHitLinesCoroutine;
    private Coroutine killLinesCoroutine;
    private bool weaponEquipped;
    private bool isShooting;
    private bool isAiming;
    private Vector2[] originalPositions;
    private Tween[] returnTweens;

    private void Awake()
    {
        if (weaponLinesRoot != null)
        {
            weaponLinesRoot.SetActive(false);
        }

        if (normalHitLinesRoot != null)
        {
            normalHitLinesRoot.SetActive(false);
        }

        if (bullseyeHitLinesRoot != null)
        {
            bullseyeHitLinesRoot.SetActive(false);
        }

        if (killLinesRoot != null)
        {
            killLinesRoot.SetActive(false);
        }

        // Cache original positions of weapon lines
        if (weaponLines != null && weaponLines.Length > 0)
        {
            originalPositions = new Vector2[weaponLines.Length];
            returnTweens = new Tween[weaponLines.Length];
            for (int i = 0; i < weaponLines.Length; i++)
            {
                if (weaponLines[i] != null)
                {
                    originalPositions[i] = weaponLines[i].anchoredPosition;
                }
            }
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
    /// Called when shooting starts. Moves weapon lines away from center.
    /// </summary>
    public void StartShooting()
    {
        if (!weaponEquipped || weaponLines == null || weaponLines.Length != 4)
        {
            return;
        }

        if (isShooting)
        {
            return; // Already shooting
        }

        isShooting = true;

        // Kill any ongoing return tweens
        KillReturnTweens();

        // Move each line away from center
        // Assuming order: 0=top, 1=bottom, 2=left, 3=right
        if (weaponLines[0] != null) // Top - move up
        {
            weaponLines[0].anchoredPosition = originalPositions[0] + Vector2.up * recoilDistance;
        }
        if (weaponLines[1] != null) // Bottom - move down
        {
            weaponLines[1].anchoredPosition = originalPositions[1] + Vector2.down * recoilDistance;
        }
        if (weaponLines[2] != null) // Left - move left
        {
            weaponLines[2].anchoredPosition = originalPositions[2] + Vector2.left * recoilDistance;
        }
        if (weaponLines[3] != null) // Right - move right
        {
            weaponLines[3].anchoredPosition = originalPositions[3] + Vector2.right * recoilDistance;
        }
    }

    /// <summary>
    /// Called when shooting stops. Returns weapon lines to original position.
    /// </summary>
    public void StopShooting()
    {
        if (!weaponEquipped || weaponLines == null || weaponLines.Length != 4)
        {
            return;
        }

        if (!isShooting)
        {
            return; // Not shooting
        }

        isShooting = false;

        // Kill any ongoing return tweens
        KillReturnTweens();

        // Animate each line back to original position
        for (int i = 0; i < weaponLines.Length && i < originalPositions.Length; i++)
        {
            if (weaponLines[i] != null)
            {
                returnTweens[i] = weaponLines[i].DOAnchorPos(originalPositions[i], returnDuration)
                    .SetEase(Ease.OutQuad);
            }
        }
    }

    /// <summary>
    /// Shows hit lines for configured duration based on hit zone (shown on any hit).
    /// </summary>
    public void ShowHitLines(HitZone zone)
    {
        GameObject targetRoot = zone == HitZone.Bullseye ? bullseyeHitLinesRoot : normalHitLinesRoot;
        float duration = zone == HitZone.Bullseye ? bullseyeHitLinesDuration : normalHitLinesDuration;
        Coroutine currentCoroutine = zone == HitZone.Bullseye ? bullseyeHitLinesCoroutine : normalHitLinesCoroutine;

        if (targetRoot == null)
        {
            return;
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        Coroutine newCoroutine = StartCoroutine(HitLinesRoutine(targetRoot, duration));
        
        if (zone == HitZone.Bullseye)
        {
            bullseyeHitLinesCoroutine = newCoroutine;
        }
        else
        {
            normalHitLinesCoroutine = newCoroutine;
        }
    }

    /// <summary>
    /// Shows kill lines for configured duration (shown when target HP reaches 0).
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

        killLinesCoroutine = StartCoroutine(KillLinesRoutine(killLinesRoot, killLinesDuration));
    }

    private void UpdateWeaponLinesVisibility()
    {
        if (weaponLinesRoot != null)
        {
            // Show weapon lines only when weapon is equipped and not aiming
            weaponLinesRoot.SetActive(weaponEquipped && !isAiming);
        }
    }

    /// <summary>
    /// Sets whether player is aiming. Hides weapon lines when aiming.
    /// </summary>
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
        UpdateWeaponLinesVisibility();
    }

    private IEnumerator HitLinesRoutine(GameObject root, float duration)
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        float waitTime = Mathf.Max(0f, duration);
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        if (root != null)
        {
            root.SetActive(false);
        }

        if (root == normalHitLinesRoot)
        {
            normalHitLinesCoroutine = null;
        }
        else if (root == bullseyeHitLinesRoot)
        {
            bullseyeHitLinesCoroutine = null;
        }
    }

    private IEnumerator KillLinesRoutine(GameObject root, float duration)
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        float waitTime = Mathf.Max(0f, duration);
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        if (root != null)
        {
            root.SetActive(false);
        }

        killLinesCoroutine = null;
    }

    private void KillReturnTweens()
    {
        if (returnTweens == null)
        {
            return;
        }

        for (int i = 0; i < returnTweens.Length; i++)
        {
            if (returnTweens[i] != null && returnTweens[i].IsActive())
            {
                returnTweens[i].Kill();
                returnTweens[i] = null;
            }
        }
    }

    private void OnDisable()
    {
        if (normalHitLinesCoroutine != null)
        {
            StopCoroutine(normalHitLinesCoroutine);
            normalHitLinesCoroutine = null;
        }

        if (bullseyeHitLinesCoroutine != null)
        {
            StopCoroutine(bullseyeHitLinesCoroutine);
            bullseyeHitLinesCoroutine = null;
        }

        if (killLinesCoroutine != null)
        {
            StopCoroutine(killLinesCoroutine);
            killLinesCoroutine = null;
        }

        KillReturnTweens();
        StopShooting(); // Reset weapon lines to original position

        if (normalHitLinesRoot != null)
        {
            normalHitLinesRoot.SetActive(false);
        }

        if (bullseyeHitLinesRoot != null)
        {
            bullseyeHitLinesRoot.SetActive(false);
        }

        if (killLinesRoot != null)
        {
            killLinesRoot.SetActive(false);
        }
    }
}

