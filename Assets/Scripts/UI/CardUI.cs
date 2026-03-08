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

using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        RoundManager.Instance.onDependentUnstaged.AddListener(OnDependentUnstaged);
        hoverHandler = GetComponentInChildren<CardHoverHandler>();
        cardButton.onClick.AddListener(OnCardClicked);

        if (selectedOverlay != null)
            selectedOverlay.SetActive(false);
    }

    private string ToTitleCase(CardSubCategory subCat)
    {
        string subCategoryName = subCat.ToString();

        if (string.IsNullOrEmpty(subCategoryName) || subCat == CardSubCategory.None) return "???";
        string spaced = Regex.Replace(subCategoryName, "([a-z])([A-Z])", "$1 $2");                    // 1. Insert a space before each uppercase letter
        return $"{char.ToUpper(spaced[0]) + spaced.Substring(1)}s";                                   // 2. Capitalize the first letter of the entire string
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
            case "Seller": return $"Selling for {card.itemBuyCost}g";
            case "Buyer": return $"Looking for {ToTitleCase(card.buyerDesiredItemType)}";
            case "Conservator": return $"Expert in {ToTitleCase(card.conservatorExpertise)}";
            case "Contractor":
            case "Freelancer":
                return card.cardDescription.Length > 40
                    ? card.cardDescription.Substring(0, 40) + "..."
                    : card.cardDescription;
            default: return string.Empty;
        }
    }

    /// <summary>
    /// Unstages all currently staged seller cards and clears their visuals.
    /// Called when a buyer is unstaged and temporary gold is removed,
    /// since sellers that were only affordable due to that temporary gold
    /// can no longer be guaranteed affordable.
    /// </summary>
    private void UnstageAllSellers()
    {
        // Collect sellers to unstage — iterate activeCardUIs to find
        // all staged seller CardUI instances
        List<CardUI> sellerUIs = CardUIManager.Instance.activeCardUIs.FindAll(
            c => c.isStaged &&
                 c.assignedCard.category != null &&
                 c.assignedCard.category.categoryName == "Seller");

        foreach (CardUI sellerUI in sellerUIs)
        {
            RoundManager.Instance.UnstageCard(sellerUI.assignedCard);
            sellerUI.SetSelectedVisual(false);
            Debug.Log($"[CardUI] Auto-unstaged seller '{sellerUI.assignedCard.cardName}' " +
                      $"— buyer was removed.");
        }

        CardUIManager.Instance.UpdateHUD();
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
            RoundManager.Instance.UnstageCard(assignedCard);
            EconomyManager.Instance.RecalculateTemporaryGold();
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

            case "Conservator":
                HandleConservatorClick();
                break;

            case "Contractor":
                HandleContractorClick();
                break;

            case "Freelancer":
                HandleFreelancerClick();
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
    /// Called when this card is automatically unstaged because its pending
    /// seller target was removed. Clears the staged visual.
    /// </summary>
    private void OnDependentUnstaged(CardData unstaged)
    {
        if (unstaged != assignedCard) return;
        SetSelectedVisual(false);
        CardUIManager.Instance.UpdateHUD();
        Debug.Log($"[CardUI] '{assignedCard.cardName}' auto-deselected — " +
                  $"pending seller target was removed.");
    }

    /// <summary>
    /// Calculates the expected gold payout from a staged buyer based on
    /// its chosen item. Used to add and remove temporary gold when the
    /// buyer is staged or unstaged.
    /// </summary>
    private int GetBuyerExpectedPayout(StagedCardData staged)
    {
        if (staged.chosenItem == null) return 0;
        if (staged.chosenItem.isAppraised) return staged.chosenItem.appraisedValue;
        return staged.chosenItem.purchasePrice +
               Mathf.RoundToInt(staged.chosenItem.purchasePrice * 0.2f);
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
            EconomyManager.Instance.RecalculateTemporaryGold();
            SetSelectedVisual(true);
            CardUIManager.Instance.UpdateHUD();
        }
    }

    /// <summary>
    /// Opens a buyer item selection popup. Card stages only if item chosen.
    /// </summary>
    private void HandleBuyerClick()
    {
        List<StagedCardData> pendingSellers = RoundManager.Instance.stagedCards
            .FindAll(s => s.card.category?.categoryName == "Seller");

        PopupManager.Instance.OpenBuyerItemSelection(
            assignedCard,
            InventoryManager.Instance.items,
            pendingSellers,
            onItemChosen: (item, pendingSeller) =>
            {
                StagedCardData staged = RoundManager.Instance.StageCard(assignedCard);
                if (staged != null)
                {
                    staged.chosenItem = item;
                    staged.pendingSellerTarget = pendingSeller;
                    EconomyManager.Instance.RecalculateTemporaryGold();
                    SetSelectedVisual(true);
                    CardUIManager.Instance.UpdateHUD();
                }
            },
            onCancel: () => Debug.Log("[CardUI] Buyer item selection cancelled.")
        );
    }

    /// <summary>
    /// Opens a conservator item selection popup. Card stages only if an item is chosen.
    /// Mirrors HandleBuyerClick() — choice is stored in staged.chosenItem and
    /// executed at round end by CardInteractionManager.
    /// </summary>
    private void HandleConservatorClick()
    {
        List<StagedCardData> pendingSellers = RoundManager.Instance.stagedCards
            .FindAll(s => s.card.category != null &&
                          s.card.category.categoryName == "Seller");

        PopupManager.Instance.OpenConservatorItemSelection(
            assignedCard,
            InventoryManager.Instance.items,
            pendingSellers,
            onItemChosen: (item, pendingSeller) =>
            {
                StagedCardData staged = RoundManager.Instance.StageCard(assignedCard);
                if (staged != null)
                {
                    staged.chosenItem = item;
                    staged.pendingSellerTarget = pendingSeller;
                    SetSelectedVisual(true);
                    CardUIManager.Instance.UpdateHUD();
                }
            },
            onCancel: () =>
            {
                Debug.Log($"[CardUI] Conservator item selection cancelled.");
            }
        );
    }

    /// <summary>
    /// Stages a contractor card and deducts its cost from temporary gold display.
    /// Affordability already checked before this is called.
    /// </summary>
    private void HandleContractorClick()
    {
        StagedCardData staged = RoundManager.Instance.StageCard(assignedCard);
        if (staged != null)
        {
            EconomyManager.Instance.RecalculateTemporaryGold();
            SetSelectedVisual(true);
            CardUIManager.Instance.UpdateHUD();
        }
    }

    /// <summary>
    /// Stages a freelancer card and deducts its cost from temporary gold display.
    /// Affordability already checked before this is called.
    /// </summary>
    private void HandleFreelancerClick()
    {
        StagedCardData staged = RoundManager.Instance.StageCard(assignedCard);
        if (staged != null)
        {
            EconomyManager.Instance.RecalculateTemporaryGold();
            SetSelectedVisual(true);
            CardUIManager.Instance.UpdateHUD();
        }
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