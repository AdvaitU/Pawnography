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
    /// <summary>
    /// Spends gold, adds the purchased item to inventory, and updates
    /// the HUD. Requires staged.purchaseConfirmed to be true.
    /// Re-checks inventory space and shows the warehouse-full warning
    /// popup if needed.
    /// </summary>
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
    /// <summary>
    /// Calculates the sell price (appraisedValue if appraised, otherwise
    /// itemBuyCost + 20%), removes the chosen item from inventory,
    /// and adds gold to the player's balance. Requires staged.chosenItem.
    /// </summary>
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

        int itemSellCost = item.sourceCard.itemBuyCost + Mathf.RoundToInt(item.sourceCard.itemBuyCost * item.sourceCard.buyerProfitPercentage);  // Sell cost is 20% more by default
        if (item.isAppraised) itemSellCost = item.appraisedValue;
        

        InventoryManager.Instance.TryRemoveItem(item);                                           // Remove the item from the inventory
        EconomyManager.Instance.AddGold(itemSellCost, $"Sell {item.cardName} to {card.cardName}");     // Add gold to pocket
        CardUIManager.Instance.UpdateHUD();                                                      // Update gold value in HUD

        Debug.Log($"[CardInteractionManager] Sold '{item.cardName}' to '{card.cardName}' " +
                  $"for {itemSellCost}g.");
    }

    // CONSERVATOR/EXPERT CARDS ----------------------------------------------------------------------
    /// <summary>
    /// Opens the conservator item selection popup. On item chosen,
    /// calls ApplyConservatorToItem() and refreshes the HUD.
    /// </summary>
    private void ExecuteConservator(CardData card)
    {
        PopupManager.Instance.OpenConservatorItemSelection(
            card,
            InventoryManager.Instance.items,
            onItemChosen: (item) =>
            {
                ApplyConservatorToItem(card, item);
                CardUIManager.Instance.UpdateHUD();
                //InventoryUI.Instance.RefreshInventoryDisplay();
            },
            onCancel: () => Debug.Log("[CardInteractionManager] Appraisal cancelled.")
        );
    }
    /// <summary>
    /// Applies conservator appraisal and optional restoration to an
    /// inventory item. If the conservator's expertise matches the item's
    /// subCategory and isConservator is true, adds upgradePercentage%
    /// to itemTrueValue and sets it as appraisedValue. Otherwise applies
    /// nonExpertiseMultiplier to itemTrueValue.
    /// </summary>
    private void ApplyConservatorToItem(CardData conservator, InventoryItem item)
    {
        item.isAppraised = true;
        item.valueIsRevealed = true;    // Sets the value to be visible if it was hidden before

        bool isExpertise = conservator.conservatorExpertise != CardSubCategory.None &&
                           item.sourceCard.subCategory != CardSubCategory.None &&
                           conservator.conservatorExpertise == item.sourceCard.subCategory;  // Is the conservator an expert in the item's subcategory?

        if (isExpertise)  // If conservator is an expert in that sub category.
        {
            if (conservator.isConservator == true)    // If it is both upgrade and appraisal, apply the upgrade first.
            {
                int addedValue = Mathf.RoundToInt(item.sourceCard.itemTrueValue * conservator.conservatorUpgradePercentage / 100f);  // Calculate the added value from the upgrade percentage
                item.appraisedValue = item.sourceCard.itemTrueValue + addedValue;  // Set the appraised value to the new true value after upgrade
            }
            else
            {
                item.appraisedValue = item.sourceCard.itemTrueValue;  // If it's not an upgrade, just set the appraised value to the true value
            }
             
        }
        else
        {
            item.appraisedValue = Mathf.RoundToInt(item.sourceCard.itemTrueValue * conservator.nonExpertiseMultiplier);  // If not an expertise, apply the non-expertise multiplier to the true value for the appraised value
        }
        
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
    /// <summary>
    /// Re-checks affordability at execution time, spends gold, and applies
    /// the contractor upgrade via ShopManager.ApplyUpgrade().
    /// Note: OpenContractorConfirmation() popup is currently not called here.
    /// </summary>
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
    /// <summary>
    /// Re-checks affordability at execution time, spends gold, and sends
    /// the freelancer out via FreelancerManager.SendOutFreelancer().
    /// </summary>
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