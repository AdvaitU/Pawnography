/*
 * ============================================================
 * SCRIPT:      FloatingText.cs
 * GAMEOBJECT:  Spawned at runtime as a child of MainCanvas
 * ------------------------------------------------------------
 * FUNCTION:
 *   A self-contained floating text element that spawns below
 *   a target card, plays a scale punch animation on appear,
 *   holds for a configurable duration, then fades out and
 *   destroys itself. Spawned and positioned by FloatingTextManager.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   FloatingTextManager -- spawns and initialises via Show()
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   Show()              --> Called by FloatingTextManager
 *                          immediately after instantiation
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Update() runs only for the lifetime of the text element
 *   (spawnBurstDuration + holdDuration + fadeDuration seconds).
 *   Destroys itself after completing — no pooling needed at
 *   current scale.
 * ============================================================
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Text to display.")]
    public string message = "Not Enough Gold!";

    [Tooltip("Colour of the text.")]
    public Color textColour = new Color(1f, 0.25f, 0.25f, 1f); // red

    [Tooltip("Font size of the floating text.")]
    public float fontSize = 22f;

    [Tooltip("Peak scale during the spawn burst punch.")]
    public float burstScale = 1.4f;

    [Tooltip("Duration of the spawn scale punch in seconds.")]
    public float burstDuration = 0.25f;

    [Tooltip("How long the text holds at full opacity after the burst.")]
    public float holdDuration = 0.8f;

    [Tooltip("How long the fade out takes in seconds.")]
    public float fadeDuration = 0.4f;

    // Internal refs
    private TextMeshProUGUI textComponent;
    private RectTransform rectTransform;

    // Animation state
    private float burstTimer;
    private float holdTimer;
    private float fadeTimer;

    private enum State { Burst, Hold, Fade, Done }
    private State state = State.Burst;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textComponent = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Initialises and starts the floating text animation.
    /// Called by FloatingTextManager immediately after instantiation.
    /// </summary>
    public void Show(string text, Color colour, float size)
    {
        message = text;
        textColour = colour;
        fontSize = size;

        if (textComponent != null)
        {
            textComponent.text = message;
            textComponent.color = textColour;
            textComponent.fontSize = fontSize;
        }

        // Start at zero scale for the punch to feel snappy
        rectTransform.localScale = Vector3.zero;
        burstTimer = burstDuration;
        state = State.Burst;
    }

    private void Update()
    {
        switch (state)
        {
            case State.Burst:
                UpdateBurst();
                break;

            case State.Hold:
                UpdateHold();
                break;

            case State.Fade:
                UpdateFade();
                break;

            case State.Done:
                Destroy(gameObject);
                break;
        }
    }

    /// <summary>
    /// Scale punch — grows from 0 to burstScale then back to 1
    /// over burstDuration using a sine curve.
    /// </summary>
    private void UpdateBurst()
    {
        burstTimer -= Time.deltaTime;
        float t = 1f - Mathf.Clamp01(burstTimer / burstDuration);

        // Sine curve: 0 → burstScale → 1
        float scale = Mathf.Lerp(0f, burstScale, Mathf.Sin(t * Mathf.PI));
        // After the peak, settle to 1
        if (t > 0.5f)
            scale = Mathf.Lerp(burstScale, 1f, (t - 0.5f) * 2f);

        rectTransform.localScale = Vector3.one * Mathf.Max(scale, 0f);

        if (burstTimer <= 0f)
        {
            rectTransform.localScale = Vector3.one;
            holdTimer = holdDuration;
            state = State.Hold;
        }
    }

    /// <summary>
    /// Hold — text sits at full opacity for holdDuration seconds.
    /// </summary>
    private void UpdateHold()
    {
        holdTimer -= Time.deltaTime;
        if (holdTimer <= 0f)
        {
            fadeTimer = fadeDuration;
            state = State.Fade;
        }
    }

    /// <summary>
    /// Fade — alpha lerps from 1 to 0 over fadeDuration seconds.
    /// </summary>
    private void UpdateFade()
    {
        fadeTimer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(fadeTimer / fadeDuration);

        if (textComponent != null)
        {
            Color c = textComponent.color;
            c.a = alpha;
            textComponent.color = c;
        }

        if (fadeTimer <= 0f)
            state = State.Done;
    }
}