/*
 * ============================================================
 * SCRIPT:      CardInteractionManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Executes card effects at round end using StagedCardData
 *   which carries pre-selected metadata (chosen items, purchase
 *   confirmation). Buyer pays the higher of offered price vs
 *   item true value. No grace round — boss round failure is
 *   immediate game over.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   RoundManager  -- calls ExecuteCardEffect() for each staged
 *                    card during ProcessAndEndRound()
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   ExecuteCardEffect() --> Called by RoundManager
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- singleton setup only. No Update().
 * ============================================================
 */

using UnityEngine;

public class CardInteractionManager : MonoBehaviour
{
    public static CardInteractionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Entry point called by RoundManager for each staged card.
    /// Now receives StagedCardData to access pre-selected metadata.
    /// </summary>
    public void ExecuteCardEffect(StagedCardData staged)
    {
        CardData card = staged.card;

        if (card.category == null)
        {
            Debug.LogWarning($"[CardInteractionManager] '{card.cardName}' has no category.");
            return;
        }

        switch (card.category.categoryName)
        {
            case "Seller": ExecuteSeller(staged); break;
            case "Buyer": ExecuteBuyer(staged); break;
            case "Conservator": ExecuteConservator(card); break;
            case "Contractor": ExecuteContractor(card); break;
            case "Freelancer": ExecuteFreelancer(card); break;
            // ── Add new cases here ──
            default:
                Debug.LogWarning($"[CardInteractionManager] No handler for " +
                                 $"'{card.category.categoryName}'.");
                break;
        }
    }

    // ─────────────────────────────────────────────
    // SELLER
    // Purchase was pre-confirmed via popup on card click.
    // ─────────────────────────────────────────────

    private void ExecuteSeller(StagedCardData staged)
    {
        CardData card = staged.card;

        if (!staged.purchaseConfirmed)
        {
            Debug.LogWarning($"[CardInteractionManager] Seller '{card.cardName}' " +
                             $"executed without purchase confirmation — skipping.");
            return;
        }

        if (!InventoryManager.Instance.HasSpace())
        {
            PopupManager.Instance.OpenWarehouseFullWarning(() =>
                WarehousePanelUI.Instance.OpenWarehouse());
            return;
        }

        EconomyManager.Instance.TrySpendGold(card.itemBuyCost, $"Buy {card.cardName}");
        InventoryManager.Instance.TryAddItem(card);
        CardUIManager.Instance.UpdateHUD();
        Debug.Log($"[CardInteractionManager] Bought '{card.cardName}' for {card.itemBuyCost}g.");
    }

    // ─────────────────────────────────────────────
    // BUYER
    // Item was pre-chosen via popup on card click.
    // Pays the higher of offered price vs item true value.
    // ─────────────────────────────────────────────

    private void ExecuteBuyer(StagedCardData staged)
    {
        CardData card = staged.card;

        if (staged.chosenItem == null)
        {
            Debug.LogWarning($"[CardInteractionManager] Buyer '{card.cardName}' " +
                             $"executed with no chosen item — skipping.");
            return;
        }

        InventoryItem item = staged.chosenItem;

        // Pay the higher of the buyer's offered price or the item's true value
        int trueValue = item.sourceCard != null ? item.sourceCard.itemTrueValue : 0;
        int payout = Mathf.Max(card.buyerOfferedPrice, trueValue);

        InventoryManager.Instance.TryRemoveItem(item);
        EconomyManager.Instance.AddGold(payout, $"Sell {item.cardName} to {card.cardName}");
        CardUIManager.Instance.UpdateHUD();

        Debug.Log($"[CardInteractionManager] Sold '{item.cardName}' to '{card.cardName}' " +
                  $"for {payout}g (offered: {card.buyerOfferedPrice}g, " +
                  $"true value: {trueValue}g).");
    }

    // ─────────────────────────────────────────────
    // CONSERVATOR
    // ─────────────────────────────────────────────

    private void ExecuteConservator(CardData card)
    {
        PopupManager.Instance.OpenConservatorItemSelection(
            card,
            InventoryManager.Instance.items,
            onItemChosen: (item) =>
            {
                ApplyConservatorToItem(card, item);
                CardUIManager.Instance.UpdateHUD();
                InventoryUI.Instance.RefreshInventoryDisplay();
            },
            onCancel: () => Debug.Log("[CardInteractionManager] Appraisal cancelled.")
        );
    }

    private void ApplyConservatorToItem(CardData conservator, InventoryItem item)
    {
        float deviation = 1f - conservator.appraisalAccuracy;
        float multiplier = UnityEngine.Random.Range(1f - deviation, 1f + deviation);
        int appraisedValue = Mathf.RoundToInt(item.sourceCard.itemTrueValue * multiplier);

        item.isAppraised = true;
        item.appraisedValue = appraisedValue;

        bool isExpertise = !string.IsNullOrEmpty(conservator.conservatorExpertise) &&
                           conservator.conservatorExpertise == item.sourceCard.subCategory;

        int conditionBonus = isExpertise
            ? conservator.appraisalLevel
            : Mathf.RoundToInt(conservator.appraisalLevel * conservator.nonExpertiseMultiplier);

        item.sourceCard.itemCondition = Mathf.Clamp(
            item.sourceCard.itemCondition + conditionBonus, 0, 100);

        Debug.Log($"[CardInteractionManager] '{item.cardName}' appraised at {appraisedValue}g. " +
                  $"Condition +{conditionBonus}. " +
                  $"New condition: {item.sourceCard.itemCondition}.");
    }

    /// <summary>
    /// Helper Method -- Finds the RectTransform of the active CardUI displaying the given card.
    /// Used to position floating text below the correct card.
    /// Returns null if the card UI is not found.
    /// </summary>
    private RectTransform GetCardRect(CardData card)
    {
        foreach (CardUI cardUI in CardUIManager.Instance.activeCardUIs)
        {
            if (cardUI.assignedCard == card)
                return cardUI.GetComponentInChildren<RectTransform>();
        }
        return null;
    }

    // ─────────────────────────────────────────────
    // CONTRACTOR
    // ─────────────────────────────────────────────

    private void ExecuteContractor(CardData card)
    {
        int cost = EconomyManager.Instance.GetContractorCost(card);

        if (!EconomyManager.Instance.CanAfford(cost))
        {
            FloatingTextManager.Instance.ShowNotEnoughGold(GetCardRect(card));
            Debug.Log($"[CardInteractionManager] Cannot afford contractor '{card.cardName}'.");
            return;
        }

        EconomyManager.Instance.TrySpendGold(cost, $"Hire {card.cardName}");
        ShopManager.Instance.ApplyUpgrade(card);
        CardUIManager.Instance.UpdateHUD();
    }

    // ─────────────────────────────────────────────
    // FREELANCER
    // ─────────────────────────────────────────────

    private void ExecuteFreelancer(CardData card)
    {
        int cost = EconomyManager.Instance.GetFreelancerCost(card);

        if (!EconomyManager.Instance.CanAfford(cost))
        {
            FloatingTextManager.Instance.ShowNotEnoughGold(GetCardRect(card));
            Debug.Log($"[CardInteractionManager] Cannot afford freelancer '{card.cardName}'.");
            return;
        }

        EconomyManager.Instance.TrySpendGold(cost, $"Send {card.cardName}");
        FreelancerManager.Instance.SendOutFreelancer(card);
        CardUIManager.Instance.UpdateHUD();
    }
}