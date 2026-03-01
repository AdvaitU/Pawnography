/*
 * ============================================================
 * SCRIPT:      CardInteractionManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Executes card effects at round end using StagedCardData
 *   which carries pre-selected metadata (chosen items, purchase
 *   confirmation). 
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

    // METHODS ==============================================================================================

    // ExecuteCardEffect(stagedCard) --------------------------------------------------------
    // Wrapper method that contains a simple switch case to choose which method to execute
    // Executes the effect for each staged card when Next Round is clicked
    // Called by RoundManager for each staged card.
    public void ExecuteCardEffect(StagedCardData staged)
    {
        CardData card = staged.card;   // Creates local version of cached card from StagedCardData object that contains the CardData object

        if (card.category == null)  // Failsafe
        {
            Debug.LogWarning($"[CardInteractionManager] '{card.cardName}' has no category.");
            return;
        }

        switch (card.category.categoryName)                       // Strings need to match exact category name because CardCategory does not contain an enum
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

    // SELLER CARDS -----------------------------------------------------------------------------

    private void ExecuteSeller(StagedCardData staged)
    {
        CardData card = staged.card;   // Local card data copied from 

        if (!staged.purchaseConfirmed)   // Failsafe
        {
            Debug.LogWarning($"[CardInteractionManager] Seller '{card.cardName}' " +
                             $"executed without purchase confirmation — skipping.");
            return;
        }

        if (!InventoryManager.Instance.HasSpace())         // If there is no space in the inventory
        {
            PopupManager.Instance.OpenWarehouseFullWarning(() =>         
                WarehousePanelUI.Instance.OpenWarehouse());               // Load Popup showing that
            return;
        }

        EconomyManager.Instance.TrySpendGold(card.itemBuyCost, $"Buy {card.cardName}");        // Calls the Economy Manager to do gold spending
        InventoryManager.Instance.TryAddItem(card);                                            // Adds the item to the Inventory Manager
        CardUIManager.Instance.UpdateHUD();                                                    // Updates CardUI Manager to update the HUD - remove gold
        Debug.Log($"[CardInteractionManager] Bought '{card.cardName}' for {card.itemBuyCost}g.");
    }

    // BUYER CARDS --------------------------------------------------------------------------------

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

        // Pay the lower of the buyer's offered price or the item's true value
        int trueValue = item.sourceCard != null ? item.sourceCard.itemTrueValue : 0;
        int payout = Mathf.Min(card.buyerOfferedPrice, trueValue);

        InventoryManager.Instance.TryRemoveItem(item);                                           // Remove the item from the inventory
        EconomyManager.Instance.AddGold(payout, $"Sell {item.cardName} to {card.cardName}");     // Add gold to pocket
        CardUIManager.Instance.UpdateHUD();                                                      // Update gold value in HUD

        Debug.Log($"[CardInteractionManager] Sold '{item.cardName}' to '{card.cardName}' " +
                  $"for {payout}g (offered: {card.buyerOfferedPrice}g, " +
                  $"true value: {trueValue}g).");
    }

    // CONSERVATOR/EXPERT CARDS ----------------------------------------------------------------------

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


    // ----------------------------------------------------------------------------------------------

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


    // CONTRACTOR CARDS -----------------------------------------------------------------------------

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
        ShopManager.Instance.ApplyUpgrade(card);                                 // Relevant Upgrade applied
        CardUIManager.Instance.UpdateHUD();
    }


    // FREELANCER CARDS -----------------------------------------------------------------------------

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