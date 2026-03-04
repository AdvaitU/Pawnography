/*
 * ============================================================
 * SCRIPT:      CardUI.cs
 * GAMEOBJECT:  Card Prefab root (spawned under CardRowPanel)
 * ------------------------------------------------------------
 * FUNCTION:
 *   Displays splash art, name, and quick info for a card.
 *   Clicking the card routes by category:
 *   - Seller: opens confirmation popup, stages on confirm
 *   - Buyer: opens item selection popup, stages on item chosen
 *   - Others: stage immediately on click, unstage on re-click
 *   Deselecting a Buyer card frees the chosen item.
 *   Card effects are NOT executed here — deferred to Next Round.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardUIManager       -- calls Populate(), SetSelectedVisual()
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   Populate()          --> Called by CardUIManager.SpawnCards()
 *   SetSelectedVisual() --> Called after staging state changes
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- hooks button listener. No Update().
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
    public Button cardButton;

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
        hoverHandler = GetComponentInChildren<CardHoverHandler>();
        cardButton.onClick.AddListener(OnCardClicked);

        if (selectedOverlay != null)
            selectedOverlay.SetActive(false);
    }

    /// <summary>
    /// Populates the card face. Called by CardUIManager each round.
    /// </summary>
    public void Populate(CardData card)
    {
        assignedCard = card;
        isStaged = false;

        if (selectedOverlay != null)
            selectedOverlay.SetActive(false);

        if (cardArtImage != null)
        {
            cardArtImage.sprite = card.cardArt != null ? card.cardArt : null;
            cardArtImage.color = Color.white;
        }

        cardNameText.text = card.cardName;
        cardQuickInfoText.text = GetQuickInfo(card);

        if (hoverHandler != null)
            hoverHandler.SetCardData(card);
    }

    private string GetQuickInfo(CardData card)
    {
        if (card.category == null) return string.Empty;

        switch (card.category.categoryName)
        {
            case "Seller": return $"{card.itemBuyCost}g";
            case "Buyer": return $"Wants: {card.buyerDesiredItemType}";
            case "Conservator": return $"Expert: {card.conservatorExpertise}";
            case "Contractor":
            case "Freelancer":
                return card.cardDescription.Length > 40
                    ? card.cardDescription.Substring(0, 40) + "..."
                    : card.cardDescription;
            default: return string.Empty;
        }
    }

    /// <summary>
    /// Routes click by category. Checks affordability for all card types
    /// before any staging or popup. Shows floating text and blocks
    /// selection if the player cannot afford the card.
    /// </summary>
    private void OnCardClicked()
    {
        if (assignedCard.category == null) return;

        if (isStaged)
        {
            // ── DESELECT ──
            StagedCardData removed = RoundManager.Instance.UnstageCard(assignedCard);

            if (removed != null && removed.chosenItem != null)
                Debug.Log($"[CardUI] Freed chosen item '{removed.chosenItem.cardName}'.");

            SetSelectedVisual(false);
            CardUIManager.Instance.UpdateHUD();
            return;
        }

        // ── AFFORDABILITY CHECK ──
        // Run before any staging or popup for all card types that cost gold.
        // If the player cannot afford the card, show floating text and block.
        if (!CanAffordCard(assignedCard))
        {
            FloatingTextManager.Instance.ShowNotEnoughGold(
                GetComponentInChildren<RectTransform>());
            return;
        }

        // ── SELECT ──
        switch (assignedCard.category.categoryName)
        {
            case "Seller":
                HandleSellerClick();
                break;

            case "Buyer":
                HandleBuyerClick();
                break;

            default:
                StagedCardData staged = RoundManager.Instance.StageCard(assignedCard);
                if (staged != null)
                {
                    SetSelectedVisual(true);
                    CardUIManager.Instance.UpdateHUD();
                }
                break;
        }
    }

    /// <summary>
    /// Returns true if the player can afford the given card.
    /// Checks the relevant cost based on category.
    /// Free card types (Conservator, Buyer) always return true.
    /// Add new cases here as new card categories with costs are introduced.
    /// </summary>
    private bool CanAffordCard(CardData card)
    {
        if (card.category == null) return true;

        switch (card.category.categoryName)
        {
            case "Seller":
                return EconomyManager.Instance.CanAfford(card.itemBuyCost);

            case "Contractor":
                return EconomyManager.Instance.CanAfford(
                    EconomyManager.Instance.GetContractorCost(card));

            case "Freelancer":
                return EconomyManager.Instance.CanAfford(
                    EconomyManager.Instance.GetFreelancerCost(card));

            // Buyer and Conservator are free to select — no gold check needed
            case "Buyer":
            case "Conservator":
            default:
                return true;
        }
    }

    /// <summary>
    /// Stages the seller card immediately on click — no confirmation popup.
    /// Affordability and inventory space already checked before this is called.
    /// </summary>
    private void HandleSellerClick()
    {
        if (!InventoryManager.Instance.HasSpace())
        {
            PopupManager.Instance.OpenWarehouseFullWarning(() =>
                WarehousePanelUI.Instance.OpenWarehouse());
            return;
        }

        StagedCardData staged = RoundManager.Instance.StageCard(assignedCard);
        if (staged != null)
        {
            staged.purchaseConfirmed = true;
            SetSelectedVisual(true);
            CardUIManager.Instance.UpdateHUD();
        }
    }

    /// <summary>
    /// Opens a buyer item selection popup. Card stages only if item chosen.
    /// </summary>
    private void HandleBuyerClick()
    {
        PopupManager.Instance.OpenBuyerItemSelection(
            assignedCard,
            InventoryManager.Instance.items,
            onItemChosen: (item) =>
            {
                StagedCardData staged = RoundManager.Instance.StageCard(assignedCard);
                if (staged != null)
                {
                    staged.chosenItem = item;
                    SetSelectedVisual(true);
                    CardUIManager.Instance.UpdateHUD();
                }
            },
            onCancel: () =>
            {
                Debug.Log($"[CardUI] Buyer item selection cancelled.");
            }

        );
    }

    public void SetSelectedVisual(bool selected)
    {
        isStaged = selected;

        if (selectedOverlay != null)
            selectedOverlay.SetActive(selected);

        CardVisualController visualController = GetComponentInChildren<CardVisualController>();
        if (visualController != null)
            visualController.SetStaged(selected);
    }
}