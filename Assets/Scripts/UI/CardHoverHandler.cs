/*
 * ============================================================
 * SCRIPT:      CardHoverHandler.cs
 * GAMEOBJECT:  Card Prefab instance (same GameObject as CardUI)
 * ------------------------------------------------------------
 * FUNCTION:
 *   Detects pointer enter and exit on the card. On enter,
 *   immediately notifies CardVisualController to begin hover
 *   animations, then starts a delay coroutine before showing
 *   the detail popup via HoverPopupUI. On exit, cancels the
 *   coroutine, hides the popup, and tells CardVisualController
 *   to return to idle state.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardUI              -- CardHoverHandler is on the same
 *                          prefab; CardUI calls SetCardData()
 *                          during Populate()
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   SetCardData()       --> Called by CardUI.Populate()
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). Uses a single Coroutine for the popup delay.
 *   CardVisualController handles its own Update() for animation.
 * ============================================================
 */

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Delay in seconds before the hover popup appears. " +
             "Card animations begin immediately regardless of this value.")]
    public float hoverDelay = 0.5f;

    private CardData cardData;
    private Coroutine hoverCoroutine;
    private CardVisualController visualController;

    private void Awake()
    {
        visualController = GetComponent<CardVisualController>();
    }

    /// <summary>
    /// Called by CardUI.Populate() to assign card data to this handler.
    /// </summary>
    public void SetCardData(CardData data)
    {
        cardData = data;
    }

    /// <summary>
    /// Fires when cursor enters the card.
    /// Starts visual hover immediately, popup after delay.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Notify visual controller immediately — no delay on animations
        if (visualController != null)
            visualController.SetHovered(true);

        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);

        hoverCoroutine = StartCoroutine(ShowPopupAfterDelay());
    }

    /// <summary>
    /// Fires when cursor exits the card.
    /// Cancels popup delay and returns card to idle visual state.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (visualController != null)
            visualController.SetHovered(false);

        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        HoverPopupUI.Instance.HidePopup();
    }

    private IEnumerator ShowPopupAfterDelay()
    {
        yield return new WaitForSeconds(hoverDelay);

        if (cardData != null)
            HoverPopupUI.Instance.ShowPopup(cardData);
    }
}