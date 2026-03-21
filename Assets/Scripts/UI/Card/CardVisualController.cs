/*
 * ============================================================
 * SCRIPT:      CardVisualController.cs
 * GAMEOBJECT:  CardVisual child object inside the Card Prefab
 * ------------------------------------------------------------
 * FUNCTION:
 *   Handles all visual animation for a card.
 *   The parent root object is controlled by the Horizontal
 *   Layout Group — this script only ever touches its own
 *   local transform and the SelectedOverlay CanvasGroup,
 *   so there is no conflict with the layout group.
 *
 *   - Idle float:    gentle sine wave bob at rest (Update)
 *   - Hover raise:   DOTween DOLocalMoveY on SetHovered()
 *   - Hover scale:   DOTween DOScale on SetHovered()
 *   - Hover tilt:    per-frame lerp toward mouse (Update)
 *   - Staged raise:  DOTween DOLocalMoveY on SetStaged()
 *   - Spawn burst:   DOPunchScale + DOPunchRotation via
 *                    TriggerSpawnBurst(), called by
 *                    CardUIManager after stagger delay
 *   - Overlay fade:  DOFade on CanvasGroup via
 *                    SetOverlayVisible()
 *
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardHoverHandler    -- calls SetHovered(true/false)
 *   CardUI              -- calls SetStaged(true/false),
 *                          SetOverlayVisible(true/false)
 *   CardUIManager       -- calls TriggerSpawnBurst() after
 *                          stagger delay on round start
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   SetHovered()            --> CardHoverHandler
 *   SetStaged()             --> CardUI
 *   SetOverlayVisible()     --> CardUI
 *   TriggerSpawnBurst()     --> CardUIManager
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Update() runs every frame for idle float and tilt only.
 *   All discrete state transitions use DOTween tweens.
 *   DOKill() called in OnDestroy to prevent tween leaks.
 * ============================================================
 */

using UnityEngine;
using DG.Tweening;

public class CardVisualController : MonoBehaviour
{
    // ── Shadow ───────────────────────────────────────────────

    [Header("Shadow")]
    [Tooltip("Drag the Shadow component from CardArtImage here.")]
    public UnityEngine.UI.Shadow cardShadow;

    [Tooltip("The base shadow offset set in the Inspector on the Shadow component.")]
    public Vector2 baseShadowOffset = new Vector2(5f, -15f);

    [Tooltip("Multiplier applied to the Y shadow offset when fully hovered.")]
    public float hoverShadowMultiplier = 3f;

    // ── Idle Float ───────────────────────────────────────────

    [Header("Idle Float")]
    public float idleFloatAmplitude = 8f;
    public float idleFloatSpeed = 1.2f;

    [Tooltip("Randomised on Start so cards bob out of sync.")]
    public float idlePhaseOffset = 0f;
    public bool randomisePhaseOffset = true;

    // ── Hover Raise ──────────────────────────────────────────

    [Header("Hover Raise")]
    public float hoverRaiseAmount = 30f;
    public float hoverRaiseDuration = 0.15f;
    public Ease hoverRaiseEase = Ease.OutCubic;
    public Ease hoverDropEase = Ease.InCubic;

    // ── Hover Scale ──────────────────────────────────────────

    [Header("Hover Scale")]
    public float hoverScaleMultiplier = 1.08f;
    public float hoverScaleDuration = 0.15f;
    public Ease hoverScaleEase = Ease.OutCubic;

    // ── Hover Tilt ───────────────────────────────────────────

    [Header("Hover Tilt")]
    public float maxTiltAngle = 15f;
    public float tiltSpeed = 10f;

    // ── Selected State ───────────────────────────────────────

    [Header("Selected State")]
    public float selectedRaiseAmount = 20f;
    public float stagedRaiseDuration = 0.2f;
    public Ease stagedRaiseEase = Ease.OutBack;

    // ── Selected Overlay ─────────────────────────────────────

    [Header("Selected Overlay")]
    [Tooltip("Drag the SelectedOverlay GameObject here. " +
             "It must have a CanvasGroup component with Alpha = 0 set in the prefab.")]
    public GameObject selectedOverlay;

    public float overlayFadeInDuration = 0.2f;
    public Ease overlayFadeInEase = Ease.OutQuad;
    public float overlayFadeOutDuration = 0.15f;
    public Ease overlayFadeOutEase = Ease.InQuad;

    // ── Spawn Burst ──────────────────────────────────────────

    [Header("Spawn Burst")]
    [Tooltip("How much larger the card scales up at the peak of the spawn animation.")]
    public float spawnBurstScale = 1.25f;

    [Tooltip("Total duration of the spawn scale punch in seconds.")]
    public float spawnBurstDuration = 0.5f;
    public Ease spawnBurstEase = Ease.OutQuad;

    [Tooltip("Minimum rotation angle in degrees during the spawn burst.")]
    public float spawnBurstRotationMin = 5f;

    [Tooltip("Maximum rotation angle in degrees during the spawn burst.")]
    public float spawnBurstRotationMax = 15f;

    // ── Private state ────────────────────────────────────────

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private CanvasGroup overlayCanvasGroup;

    private bool isHovered = false;
    private bool isStaged = false;
    private bool isBursting = false;

    private float currentYOffset = 0f;  // Idle sine contribution only
    private float stagedYOffset = 0f;   // Tracked for tween targeting
    private Vector3 currentTilt = Vector3.zero;

    // ── Lifecycle ────────────────────────────────────────────

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (selectedOverlay != null)
        {
            overlayCanvasGroup = selectedOverlay.GetComponent<CanvasGroup>();
            // CanvasGroup must be added to SelectedOverlay in the prefab Inspector
            // with Alpha set to 0 as the default value.
            selectedOverlay.SetActive(true); // Always active — alpha drives visibility
        }
    }

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (randomisePhaseOffset)
            idlePhaseOffset = Random.Range(0f, Mathf.PI * 2f);

        rectTransform.localPosition = Vector3.zero;
        rectTransform.localScale = Vector3.one;

        // Burst is triggered externally by CardUIManager after stagger delay.
        // Do not call TriggerSpawnBurst() here.
    }

    private void OnDestroy()
    {
        rectTransform.DOKill();
        if (overlayCanvasGroup != null)
            overlayCanvasGroup.DOKill();
    }

    // ── Update ───────────────────────────────────────────────

    private void Update()
    {
        if (isHovered)
            UpdateHoverTilt();
        else
        {
            UpdateIdleFloat();
            ReturnTiltToNeutral();
        }

        ApplyTransforms();
    }

    // ── Public API ───────────────────────────────────────────

    /// <summary>
    /// Called by CardHoverHandler on pointer enter/exit.
    /// Triggers hover raise and scale tweens.
    /// </summary>
    public void SetHovered(bool hovered)
    {
        isHovered = hovered;

        // Ignore hover input while the spawn burst is playing —
        // tweening position or scale mid-burst causes the card
        // to get stuck in a partial animation state.
        if (isBursting) return;

        float targetY = hovered
            ? hoverRaiseAmount + stagedYOffset
            : stagedYOffset;

        Ease yEase = hovered ? hoverRaiseEase : hoverDropEase;

        rectTransform.DOKill(false);
        rectTransform.DOLocalMoveY(targetY, hoverRaiseDuration)
                     .SetEase(yEase);

        float targetScale = hovered ? hoverScaleMultiplier : 1f;
        rectTransform.DOScale(targetScale, hoverScaleDuration)
                     .SetEase(hoverScaleEase);
    }

    /// <summary>
    /// Called by CardUI when the card is staged or unstaged.
    /// Triggers the staged raise tween and updates the Y target
    /// so hover raise accounts for the staged offset correctly.
    /// </summary>
    public void SetStaged(bool staged)
    {
        isStaged = staged;
        stagedYOffset = staged ? selectedRaiseAmount : 0f;

        float targetY = isHovered
            ? hoverRaiseAmount + stagedYOffset
            : stagedYOffset;

        rectTransform.DOLocalMoveY(targetY, stagedRaiseDuration)
                     .SetEase(stagedRaiseEase);
    }

    /// <summary>
    /// Called by CardUI to fade the selection overlay in or out.
    /// Requires SelectedOverlay to have a CanvasGroup with Alpha = 0
    /// set in the prefab Inspector.
    /// </summary>
    public void SetOverlayVisible(bool visible)
    {
        Debug.Log("SetOverlayVisible: " + visible);
        if (overlayCanvasGroup == null) return;

        overlayCanvasGroup.DOKill();
        float targetAlpha = visible ? 1f : 0f;
        float duration = visible ? overlayFadeInDuration : overlayFadeOutDuration;
        Ease ease = visible ? overlayFadeInEase : overlayFadeOutEase;

        overlayCanvasGroup.DOFade(targetAlpha, duration).SetEase(ease);
    }

    /// <summary>
    /// Plays the spawn burst scale punch and random rotation punch.
    /// Called by CardUIManager after the per-card stagger delay,
    /// NOT from Start() — so it fires correctly on every round.
    /// </summary>
    public void TriggerSpawnBurst()
    {
        isBursting = true;
        rectTransform.DOKill();
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        float angle = Random.Range(spawnBurstRotationMin, spawnBurstRotationMax)
                      * (Random.value > 0.5f ? 1f : -1f);

        rectTransform
            .DOPunchScale(Vector3.one * (spawnBurstScale - 1f),
                          spawnBurstDuration,
                          vibrato: 1,
                          elasticity: 0.5f)
            .SetEase(spawnBurstEase)
            .OnComplete(() =>
            {
                isBursting = false;
                rectTransform.localRotation = Quaternion.identity;
                currentTilt = Vector3.zero;
            });

        rectTransform
            .DOPunchRotation(new Vector3(0f, 0f, angle),
                             spawnBurstDuration,
                             vibrato: 1,
                             elasticity: 0.5f)
            .SetEase(spawnBurstEase);
    }

    // ── Private Update Methods ───────────────────────────────

    private void UpdateIdleFloat()
    {
        // Idle float drives only the sine Y contribution.
        // Hover raise and staged raise are owned by DOTween tweens.
        currentYOffset = Mathf.Sin(Time.time * idleFloatSpeed + idlePhaseOffset)
                         * idleFloatAmplitude;
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

    // ── Apply ────────────────────────────────────────────────

    /// <summary>
    /// Writes idle float Y offset and tilt to the transform each frame.
    /// DOTween owns scale and the hover/staged Y position directly —
    /// we only add the idle sine delta on top when not hovered,
    /// and skip scale/rotation writes while burst is active.
    /// </summary>
    private void ApplyTransforms()
    {
        // Add idle sine offset on top of whatever Y DOTween has set,
        // but only when not hovered (DOTween owns Y fully while hovered).
        if (!isHovered)
        {
            float baseY = stagedYOffset + currentYOffset;
            rectTransform.localPosition = new Vector3(0f, baseY, 0f);
        }

        // Tilt — DOTween does not own rotation except during burst
        if (!isBursting)
            rectTransform.localRotation = Quaternion.Euler(currentTilt);

        // Scale — fully owned by DOTween (hover and burst tweens)
        // Nothing written here to avoid fighting tweens.

        // ── Shadow ──
        if (cardShadow != null)
        {
            float raiseNorm = Mathf.Clamp01(
                rectTransform.localPosition.y / hoverRaiseAmount);

            float shadowY = Mathf.Lerp(
                baseShadowOffset.y,
                baseShadowOffset.y * hoverShadowMultiplier,
                raiseNorm
            );

            cardShadow.effectDistance = new Vector2(baseShadowOffset.x, shadowY);
        }
    }
}