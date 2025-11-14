using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Manages crosshair elements: static dot, weapon lines (animated on shot), and kill lines (shown on target hit).
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

    [Header("Kill Lines (X pattern)")]
    [SerializeField] private GameObject normalKillLinesRoot;
    [SerializeField] private GameObject bullseyeKillLinesRoot;
    [SerializeField] private float normalKillLinesDuration = 1.5f;
    [SerializeField] private float bullseyeKillLinesDuration = 1.5f;

    private Coroutine normalKillLinesCoroutine;
    private Coroutine bullseyeKillLinesCoroutine;
    private bool weaponEquipped;
    private bool isShooting;
    private Vector2[] originalPositions;
    private Tween[] returnTweens;

    private void Awake()
    {
        if (weaponLinesRoot != null)
        {
            weaponLinesRoot.SetActive(false);
        }

        if (normalKillLinesRoot != null)
        {
            normalKillLinesRoot.SetActive(false);
        }

        if (bullseyeKillLinesRoot != null)
        {
            bullseyeKillLinesRoot.SetActive(false);
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
    /// Shows kill lines for configured duration based on hit zone.
    /// </summary>
    public void ShowKillLines(HitZone zone)
    {
        GameObject targetRoot = zone == HitZone.Bullseye ? bullseyeKillLinesRoot : normalKillLinesRoot;
        float duration = zone == HitZone.Bullseye ? bullseyeKillLinesDuration : normalKillLinesDuration;
        Coroutine currentCoroutine = zone == HitZone.Bullseye ? bullseyeKillLinesCoroutine : normalKillLinesCoroutine;

        if (targetRoot == null)
        {
            return;
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        Coroutine newCoroutine = StartCoroutine(KillLinesRoutine(targetRoot, duration));
        
        if (zone == HitZone.Bullseye)
        {
            bullseyeKillLinesCoroutine = newCoroutine;
        }
        else
        {
            normalKillLinesCoroutine = newCoroutine;
        }
    }

    private void UpdateWeaponLinesVisibility()
    {
        if (weaponLinesRoot != null)
        {
            weaponLinesRoot.SetActive(weaponEquipped);
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

        if (root == normalKillLinesRoot)
        {
            normalKillLinesCoroutine = null;
        }
        else if (root == bullseyeKillLinesRoot)
        {
            bullseyeKillLinesCoroutine = null;
        }
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
        if (normalKillLinesCoroutine != null)
        {
            StopCoroutine(normalKillLinesCoroutine);
            normalKillLinesCoroutine = null;
        }

        if (bullseyeKillLinesCoroutine != null)
        {
            StopCoroutine(bullseyeKillLinesCoroutine);
            bullseyeKillLinesCoroutine = null;
        }

        KillReturnTweens();
        StopShooting(); // Reset weapon lines to original position

        if (normalKillLinesRoot != null)
        {
            normalKillLinesRoot.SetActive(false);
        }

        if (bullseyeKillLinesRoot != null)
        {
            bullseyeKillLinesRoot.SetActive(false);
        }
    }
}

