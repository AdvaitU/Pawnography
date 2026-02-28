/*
 * ============================================================
 * SCRIPT:      CardVisualController.cs
 * GAMEOBJECT:  CardVisual child object inside the Card Prefab
 * ------------------------------------------------------------
 * FUNCTION:
 *   Handles all visual animation for a card using localPosition
 *   and localRotation on the CardVisual child object. The parent
 *   root object is controlled by the Horizontal Layout Group.
 *   This script only ever touches its own local transform,
 *   so there is no conflict with the layout group.
 *   - Idle float: gentle sine wave bob at rest
 *   - Hover raise: card lifts on hover
 *   - Hover tilt: card tilts toward mouse position
 *   - Staged raise: selected cards sit higher than rest
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardHoverHandler    -- calls SetHovered(true/false)
 *   CardUI              -- calls SetStaged(true/false)
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   SetHovered()        --> Called by CardHoverHandler
 *   SetStaged()         --> Called by CardUI
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Update() runs every frame for float and tilt calculations.
 *   Only modifies localPosition and localRotation on its own
 *   transform — no layout group conflict possible.
 * ============================================================
 */

using UnityEngine;

public class CardVisualController : MonoBehaviour
{

    [Header("Shadow")]
    [Tooltip("Drag the Shadow component from CardArtImage here.")]
    public UnityEngine.UI.Shadow cardShadow;

    [Tooltip("The base shadow offset set in the Inspector on the Shadow component.")]
    public Vector2 baseShadowOffset = new Vector2(5f, -15f);

    [Tooltip("Multiplier applied to the Y shadow offset when fully hovered. " +
             "E.g. 3 means the shadow is 3x further at full hover raise.")]
    public float hoverShadowMultiplier = 3f;

    [Header("Idle Float")]
    public float idleFloatAmplitude = 8f;
    public float idleFloatSpeed = 1.2f;
    [Tooltip("Randomised on Start so cards bob out of sync.")]
    public float idlePhaseOffset = 0f;
    public bool randomisePhaseOffset = true;

    [Header("Hover Raise")]
    public float hoverRaiseAmount = 30f;
    public float hoverRaiseSpeed = 10f;
    public float hoverScaleMultiplier = 1.08f;
    public float hoverScaleSpeed = 10f;

    [Header("Hover Tilt")]
    public float maxTiltAngle = 15f;
    public float tiltSpeed = 10f;

    [Header("Selected State")]
    public float selectedRaiseAmount = 20f;

    [Header("Spawn Burst")]
    [Tooltip("How much larger the card scales up to at the peak of the spawn animation.")]
    public float spawnBurstScale = 1.25f;

    [Tooltip("Total duration of the spawn scale punch in seconds.")]
    public float spawnBurstDuration = 0.5f;

    private float spawnBurstTimer = 0f;

    // Tracks remaining burst time
    private float burstTimer = 0f;

    // ── Private state ──
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private bool isHovered = false;
    private bool isStaged = false;

    private float currentYOffset = 0f;
    private float currentScale = 1f;
    private Vector3 currentTilt = Vector3.zero;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (randomisePhaseOffset)
            idlePhaseOffset = Random.Range(0f, Mathf.PI * 2f);

        // Reset local position to zero — this is our rest position
        // We always animate relative to (0,0,0) local space
        rectTransform.localPosition = Vector3.zero;

        // Trigger spawn burst immediately on Start
        spawnBurstTimer = spawnBurstDuration;
    }

    private void Update()
    {
        if (isHovered)
        {
            UpdateHoverTilt();
            UpdateHoverRaise();
        }
        else
        {
            UpdateIdleFloat();
            ReturnTiltToNeutral();
        }

        UpdateScale();
        ApplyTransforms();
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
    }

    public void SetStaged(bool staged)
    {
        isStaged = staged;
    }

    private void UpdateIdleFloat()
    {
        float targetY = Mathf.Sin(Time.time * idleFloatSpeed + idlePhaseOffset)
                        * idleFloatAmplitude;

        if (isStaged)
            targetY += selectedRaiseAmount;

        currentYOffset = Mathf.Lerp(currentYOffset, targetY, Time.deltaTime * hoverRaiseSpeed);
    }

    private void UpdateHoverRaise()
    {
        float targetY = hoverRaiseAmount;

        if (isStaged)
            targetY += selectedRaiseAmount;

        currentYOffset = Mathf.Lerp(currentYOffset, targetY, Time.deltaTime * hoverRaiseSpeed);
    }

    private void UpdateHoverTilt()
    {
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            Input.mousePosition,
            parentCanvas.worldCamera,
            out localMousePos
        );

        float normX = Mathf.Clamp(localMousePos.x / (rectTransform.rect.width * 0.5f), -1f, 1f);
        float normY = Mathf.Clamp(localMousePos.y / (rectTransform.rect.height * 0.5f), -1f, 1f);

        Vector3 targetTilt = new Vector3(
            -normY * maxTiltAngle,
             normX * maxTiltAngle,
             0f
        );

        currentTilt = Vector3.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
    }

    private void ReturnTiltToNeutral()
    {
        currentTilt = Vector3.Lerp(currentTilt, Vector3.zero, Time.deltaTime * tiltSpeed);
    }

    private void UpdateScale()
    {
        float targetScale = isHovered ? hoverScaleMultiplier : 1f;

        // If spawn burst is still active, override scale with punch animation.
        // Uses a sine curve over the burst duration so it eases in and out cleanly.
        if (spawnBurstTimer > 0f)
        {
            spawnBurstTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(spawnBurstTimer / spawnBurstDuration);
            // Sine curve: starts at 1, peaks at spawnBurstScale at t=0.5, returns to 1
            float burst = 1f + (spawnBurstScale - 1f) * Mathf.Sin(t * Mathf.PI);
            currentScale = burst;
            return;
        }

        currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * hoverScaleSpeed);
    }

    /// <summary>
    /// Applies all animation values to this object's local transform only.
    /// Never touches the parent — no layout group conflict.
    /// </summary>
    private void ApplyTransforms()
    {
        rectTransform.localPosition = new Vector3(0f, currentYOffset, 0f);
        rectTransform.localRotation = Quaternion.Euler(currentTilt);
        rectTransform.localScale = Vector3.one * currentScale;

        // ── Shadow ──
        // Scale the Y offset of the shadow based on how high the card
        // is currently raised, giving the impression of a light source above.
        // When fully hovered, shadow Y is multiplied by hoverShadowMultiplier.
        if (cardShadow != null)
        {
            // Normalise current raise against max raise (0 = rest, 1 = fully hovered)
            float raiseNorm = Mathf.Clamp01(currentYOffset / hoverRaiseAmount);

            float shadowY = Mathf.Lerp(
                baseShadowOffset.y,
                baseShadowOffset.y * hoverShadowMultiplier,
                raiseNorm
            );

            cardShadow.effectDistance = new Vector2(baseShadowOffset.x, shadowY);
        }
    }
}