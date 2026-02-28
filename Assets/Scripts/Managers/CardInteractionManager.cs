/*
 * ============================================================
 * SCRIPT:      CardInteractionManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Executes card effects when called by RoundManager during
 *   ProcessAndEndRound(). Each staged card is executed in
 *   sequence with no confirmation popups — the hover popup
 *   in HoverPopupUI already gives the player all the info
 *   they need before committing. Buyer and Conservator cards
 *   still open item-selection popups since those require the
 *   player to choose a specific inventory item.
 *   Add new category cases to ExecuteCardEffect() as new
 *   card types are introduced.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   RoundManager        -- calls ExecuteCardEffect() for each
 *                          staged card during ProcessAndEndRound()
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   ExecuteCardEffect() --> Called by RoundManager.ProcessAndEndRound()
 *                          for each staged card
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- singleton setup only. No Start(), no Update().
 *   All logic is triggered by RoundManager on round end.
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
    /// Entry point called by RoundManager.ProcessAndEndRound() for each staged card.
    /// Routes to the correct execution handler based on category name.
    /// Add new cases here when new card categories are introduced.
    /// </summary>
    public void ExecuteCardEffect(CardData card)
    {
        if (card.category == null)
        {
            Debug.LogWarning($"[CardInteractionManager] '{card.cardName}' has no category.");
            return;
        }

        switch (card.category.categoryName)
        {
            case "Seller": ExecuteSeller(card); break;
            case "Buyer": ExecuteBuyer(card); break;
            case "Conservator": ExecuteConservator(card); break;
            case "Contractor": ExecuteContractor(card); break;
            case "Freelancer": ExecuteFreelancer(card); break;
            // ── Add new cases here ──
            default:
                Debug.LogWarning($"[CardInteractionManager] No handler for '{card.category.categoryName}'.");
                break;
        }
    }

    // ─────────────────────────────────────────────
    // SELLER
    // No confirmation popup — item is bought immediately.
    // If inventory is full, a warning opens so the player
    // can sell something first.
    // ─────────────────────────────────────────────

    private void ExecuteSeller(CardData card)
    {
        if (!InventoryManager.Instance.HasSpace())
        {
            // Inventory full — open warning with shortcut to warehouse
            PopupManager.Instance.OpenWarehouseFullWarning(() =>
            {
                InventoryUI.Instance.OpenInventory();
            });
            return;
        }

        // Buy immediately — no confirmation required
        InventoryManager.Instance.TryAddItem(card);
        CardUIManager.Instance.UpdateHUD();
        Debug.Log($"[CardInteractionManager] Bought '{card.cardName}' for {card.itemBuyCost}g.");
        // ── Gold deduction added here when economy system is built ──
    }

    // ─────────────────────────────────────────────
    // BUYER
    // Still needs an item selection popup — the player must
    // choose which inventory item to sell.
    // ─────────────────────────────────────────────

    private void ExecuteBuyer(CardData card)
    {
        PopupManager.Instance.OpenBuyerItemSelection(
            card,
            InventoryManager.Instance.items,
            onItemChosen: (item) =>
            {
                InventoryManager.Instance.TryRemoveItem(item);
                CardUIManager.Instance.UpdateHUD();
                Debug.Log($"[CardInteractionManager] Sold '{item.cardName}' " +
                          $"to '{card.cardName}' for {card.buyerOfferedPrice}g.");
                // ── Gold addition added here when economy system is built ──
            },
            onCancel: () =>
            {
                Debug.Log($"[CardInteractionManager] Buyer interaction cancelled.");
            }
        );
    }

    // ─────────────────────────────────────────────
    // CONSERVATOR
    // Still needs an item selection popup — the player must
    // choose which inventory item to appraise.
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
            onCancel: () =>
            {
                Debug.Log($"[CardInteractionManager] Appraisal cancelled.");
            }
        );
    }

    /// <summary>
    /// Applies appraisal and condition improvement to the chosen item.
    /// Full condition bonus if item subCategory matches conservator expertise,
    /// reduced bonus (nonExpertiseMultiplier) otherwise.
    /// </summary>
    private void ApplyConservatorToItem(CardData conservator, InventoryItem item)
    {
        // ── Value appraisal ──
        float deviation = 1f - conservator.appraisalAccuracy;
        float multiplier = UnityEngine.Random.Range(1f - deviation, 1f + deviation);
        int appraisedValue = Mathf.RoundToInt(item.sourceCard.itemTrueValue * multiplier);

        item.isAppraised = true;
        item.appraisedValue = appraisedValue;

        // ── Condition improvement ──
        bool isExpertise = !string.IsNullOrEmpty(conservator.conservatorExpertise) &&
                           conservator.conservatorExpertise == item.sourceCard.subCategory;

        int conditionBonus = isExpertise
            ? conservator.appraisalLevel
            : Mathf.RoundToInt(conservator.appraisalLevel * conservator.nonExpertiseMultiplier);

        item.sourceCard.itemCondition = Mathf.Clamp(
            item.sourceCard.itemCondition + conditionBonus, 0, 100);

        Debug.Log($"[CardInteractionManager] '{item.cardName}' appraised at {appraisedValue}g. " +
                  $"Condition +{conditionBonus} " +
                  $"({(isExpertise ? "full expertise" : "partial")}). " +
                  $"New condition: {item.sourceCard.itemCondition}.");
    }

    // ─────────────────────────────────────────────
    // CONTRACTOR
    // No confirmation popup — upgrade applied immediately.
    // ─────────────────────────────────────────────

    private void ExecuteContractor(CardData card)
    {
        string upgradeResult = ShopManager.Instance.ApplyUpgrade(card);
        CardUIManager.Instance.UpdateHUD();
        Debug.Log($"[CardInteractionManager] Contractor '{card.cardName}' applied: {upgradeResult}.");
    }

    // ─────────────────────────────────────────────
    // FREELANCER
    // No confirmation popup — freelancer sent out immediately.
    // ─────────────────────────────────────────────

    private void ExecuteFreelancer(CardData card)
    {
        FreelancerManager.Instance.SendOutFreelancer(card);
        CardUIManager.Instance.UpdateHUD();
        Debug.Log($"[CardInteractionManager] Freelancer '{card.cardName}' sent out.");
    }
}