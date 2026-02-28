/*
 * ============================================================
 * SCRIPT:      CardUI.cs
 * GAMEOBJECT:  Card Prefab instance (spawned at runtime under
 *              CardRowPanel by CardUIManager)
 * ------------------------------------------------------------
 * FUNCTION:
 *   Attached to each card prefab instance. Displays splash art,
 *   card name, and one quick-info line relevant to the card type.
 *   The entire card is a button — clicking toggles the staged
 *   selection state via RoundManager. Hover detection is
 *   delegated to CardHoverHandler on the same GameObject.
 *   Does NOT execute card effects on click — effects are
 *   deferred until Next Round is confirmed.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardUIManager       -- calls Populate() when spawning cards;
 *                          calls SetSelectedVisual() to sync
 *                          visuals after staging changes
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   Populate()          --> Called by CardUIManager.SpawnCards()
 *   SetSelectedVisual() --> Called by CardUIManager after a
 *                          staging toggle to refresh visuals
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- hooks up card button onClick listener.
 *   No Update(). Destroyed and re-instantiated each round by
 *   CardUIManager. Consider object pooling if performance
 *   degrades with large card counts.
 * ============================================================
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI References — assign in Inspector")]
    public Image cardArtImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardQuickInfoText;
    public GameObject selectedOverlay;
    public Button cardButton; // The invisible full-card button

    [Header("Category Colours")]
    public Color sellerColour = new Color(0.2f, 0.6f, 0.9f);
    public Color buyerColour = new Color(0.9f, 0.6f, 0.2f);
    public Color conservatorColour = new Color(0.4f, 0.8f, 0.4f);
    public Color contractorColour = new Color(0.7f, 0.4f, 0.9f);
    public Color freelancerColour = new Color(0.9f, 0.3f, 0.3f);

    [Header("Runtime State")]
    public CardData assignedCard;
    public bool isStaged = false;

    private CardHoverHandler hoverHandler;

    private void Awake()
    {
        // CardHoverHandler and CardVisualController live on the
        // CardVisual child object, so search in children
        hoverHandler = GetComponentInChildren<CardHoverHandler>();
        cardButton.onClick.AddListener(OnCardClicked);

        if (selectedOverlay != null)
            selectedOverlay.SetActive(false);
    }

    /// <summary>
    /// Populates the card face with data. Shows splash art, name,
    /// and a single relevant quick-info line based on card category.
    /// Called by CardUIManager when spawning cards each round.
    /// </summary>
    public void Populate(CardData card)
    {
        assignedCard = card;
        isStaged = false;

        if (selectedOverlay != null)
            selectedOverlay.SetActive(false);

        // ── Card art ──
        if (cardArtImage != null)
        {
            cardArtImage.sprite = card.cardArt != null ? card.cardArt : null;
            cardArtImage.color = Color.white;
        }

        // ── Name ──
        cardNameText.text = card.cardName;

        // ── Quick info line ──
        cardQuickInfoText.text = GetQuickInfo(card);

        // ── Pass data to hover handler ──
        if (hoverHandler != null)
            hoverHandler.SetCardData(card);
    }

    /// <summary>
    /// Returns a single short info string relevant to the card's category.
    /// This is the only information shown on the card face besides the name.
    /// Add new cases here as new card categories are introduced.
    /// </summary>
    private string GetQuickInfo(CardData card)
    {
        if (card.category == null) return string.Empty;

        switch (card.category.categoryName)
        {
            case "Seller":
                return $"{card.itemBuyCost}g";

            case "Buyer":
                return $"Wants: {card.buyerDesiredItemType}";

            case "Conservator":
                return $"Expert: {card.conservatorExpertise}";

            case "Contractor":
                return card.cardDescription.Length > 40
                    ? card.cardDescription.Substring(0, 40) + "..."
                    : card.cardDescription;

            case "Freelancer":
                return card.cardDescription.Length > 40
                    ? card.cardDescription.Substring(0, 40) + "..."
                    : card.cardDescription;

            // ── Add new cases here ──
            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// Called when the player clicks anywhere on the card.
    /// Toggles staged selection state via RoundManager.
    /// Card effects are NOT executed here — deferred to Next Round.
    /// </summary>
    private void OnCardClicked()
    {
        if (isStaged)
        {
            // Already staged — unstage it
            bool unstaged = RoundManager.Instance.UnstageCard(assignedCard);
            if (unstaged)
                SetSelectedVisual(false);
        }
        else
        {
            // Not staged — try to stage it
            bool staged = RoundManager.Instance.StageCard(assignedCard);
            if (staged)
                SetSelectedVisual(true);
        }

        CardUIManager.Instance.UpdateHUD();
    }

    /// <summary>
    /// Shows or hides the selected overlay to reflect staged state.
    /// Called by OnCardClicked and by CardUIManager after round resets.
    /// </summary>
    public void SetSelectedVisual(bool selected)
    {
        isStaged = selected;

        if (selectedOverlay != null)
            selectedOverlay.SetActive(selected);

        // Notify visual controller so staged raise is applied
        CardVisualController visualController = GetComponentInChildren<CardVisualController>();
        if (visualController != null)
            visualController.SetStaged(selected);
    }
}