/*
 * ============================================================
 * SCRIPT:      PopupShake.cs
 * GAMEOBJECT:  PopupPanel and HoverPopupPanel
 * ------------------------------------------------------------
 * FUNCTION:
 *   Applies a subtle continuous idle shake to a popup panel.
 *   When first enabled, plays a scale punch animation that
 *   grows the popup larger and returns it to normal size over
 *   spawnBurstDuration seconds, then settles into idle shake.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   Nothing — self-contained component, runs on its own.
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:   None
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Update() runs only while the GameObject is active.
 *   Popups are disabled when hidden so no wasted updates.
 * ============================================================
 */

using UnityEngine;

public class PopupShake : MonoBehaviour
{
    [Header("Idle Shake")]
    [Tooltip("Horizontal shake amplitude in pixels at rest.")]
    public float amplitudeX = 1.5f;

    [Tooltip("Vertical shake amplitude in pixels at rest.")]
    public float amplitudeY = 2.5f;

    [Tooltip("Speed of horizontal oscillation.")]
    public float speedX = 0.8f;

    [Tooltip("Speed of vertical oscillation.")]
    public float speedY = 1.1f;

    [Header("Spawn Burst")]
    [Tooltip("How much larger the popup scales up to at the peak of the spawn animation.")]
    public float spawnBurstScale = 1.2f;

    [Tooltip("Total duration of the spawn scale punch in seconds.")]
    public float spawnBurstDuration = 0.5f;

    // Internal state
    private RectTransform rectTransform;
    private Vector2 originPosition;
    private float phaseX;
    private float phaseY;
    private float spawnBurstTimer = 0f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        originPosition = rectTransform.anchoredPosition;

        phaseX = Random.Range(0f, Mathf.PI * 2f);
        phaseY = Random.Range(0f, Mathf.PI * 2f);

        // Trigger scale punch on every show
        spawnBurstTimer = spawnBurstDuration;

        // Reset scale in case it was mid-animation when last hidden
        rectTransform.localScale = Vector3.one;
    }

    private void Update()
    {
        // ── Spawn scale punch ──
        if (spawnBurstTimer > 0f)
        {
            spawnBurstTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(spawnBurstTimer / spawnBurstDuration);
            // Sine curve: starts at 1, peaks at spawnBurstScale at t=0.5, returns to 1
            float burst = 1f + (spawnBurstScale - 1f) * Mathf.Sin(t * Mathf.PI);
            rectTransform.localScale = Vector3.one * burst;
        }
        else
        {
            rectTransform.localScale = Vector3.one;
        }

        // ── Idle shake (runs alongside the burst) ──
        float offsetX = Mathf.Sin(Time.time * speedX + phaseX) * amplitudeX;
        float offsetY = Mathf.Sin(Time.time * speedY + phaseY) * amplitudeY;

        rectTransform.anchoredPosition = new Vector2(
            originPosition.x + offsetX,
            originPosition.y + offsetY
        );
    }

    private void OnDisable()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originPosition;
            rectTransform.localScale = Vector3.one;
        }
    }
}