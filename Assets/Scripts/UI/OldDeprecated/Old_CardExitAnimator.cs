/*
 * ============================================================
 * SCRIPT:      CardExitAnimator.cs
 * GAMEOBJECT:  Card Prefab instance (added at runtime by
 *              CardUIManager when a round ends)
 * ------------------------------------------------------------
 * FUNCTION:
 *   Plays a slide-out-to-the-right animation on a card when
 *   the round ends. Moves the card's RectTransform to a target
 *   X position off the right edge of the screen over
 *   exitDuration seconds, then destroys the GameObject.
 *   CardUIManager adds this component to all active cards
 *   when ClearCards() is called, then waits for the animation
 *   to complete before spawning new cards.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardUIManager       -- adds this component to each card
 *                          during ClearCards(), awaits
 *                          completion via onComplete callback
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   Play()              --> Called by CardUIManager immediately
 *                          after adding this component
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Update() runs only for exitDuration seconds then the
 *   GameObject is destroyed. No persistent cost.
 * ============================================================
 */

using System;
using UnityEngine;

public class CardExitAnimator : MonoBehaviour
{
    [Tooltip("How long the slide-out takes in seconds.")]
    public float exitDuration = 0.35f;

    [Tooltip("How far off the right edge of the screen to slide in pixels.")]
    public float exitOffsetX = 1400f;

    [Tooltip("Animation curve controlling the slide speed over time. " +
             "Leave as EaseIn for a natural flick-away feel.")]
    public AnimationCurve exitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float timer = 0f;
    private bool isPlaying = false;
    private Action onComplete;

    /// <summary>
    /// Starts the exit animation. onComplete is called when the
    /// animation finishes, immediately before the GameObject is destroyed.
    /// </summary>
    public void Play(Action onComplete = null)
    {
        // Animate the CardVisual child so it moves independently
        // of the layout group on the root, same pattern as CardVisualController
        Transform cardVisual = transform.Find("CardVisual");
        rectTransform = cardVisual != null
            ? cardVisual.GetComponent<RectTransform>()
            : GetComponent<RectTransform>();

        startPosition = rectTransform.anchoredPosition;
        targetPosition = new Vector2(startPosition.x + exitOffsetX, startPosition.y);

        this.onComplete = onComplete;
        timer = 0f;
        isPlaying = true;

        // Disable CardVisualController so it doesn't fight the exit animation
        CardVisualController vc = GetComponentInChildren<CardVisualController>();
        if (vc != null) vc.enabled = false;

        // Disable the card button so player can't click mid-animation
        CardUI cardUI = GetComponent<CardUI>();
        //if (cardUI != null && cardUI.cardButton != null)
            //cardUI.cardButton.interactable = false;
    }

    private void Update()
    {
        if (!isPlaying) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / exitDuration);
        float curved = exitCurve.Evaluate(t);

        rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, curved);

        if (t >= 1f)
        {
            isPlaying = false;
            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}