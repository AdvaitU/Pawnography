/*
 * ============================================================
 * SCRIPT:      ShopManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Central store for all shop stats: Warehouse Slots,
 *   Reputation (placeholder), Floor Space, Unlocked Categories,
 *   Unlocked Sub-Categories, and Hired Staff. All contractor
 *   upgrades are routed through ApplyUpgrade() which returns a
 *   before/after string for the confirmation popup. Syncs
 *   starting values to InventoryManager and RoundManager on
 *   Start(), then triggers the first round. ResetShop() can
 *   be called to return all stats to starting values on a
 *   new run.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardInteractionManager -- calls ApplyUpgrade() in
 *                          HandleContractor()
 *   InventoryManager    -- calls CanAutoIdentify() in TryAddItem()
 *                          to check for hired staff
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   ApplyUpgrade()      --> Called by CardInteractionManager
 *                          HandleContractor() — applies the
 *                          upgrade and returns a before/after
 *                          string for the popup
 *   CanAutoIdentify()   --> Called by InventoryManager.TryAddItem()
 *                          to check if hired staff can identify
 *                          a newly acquired item's type
 *   ResetShop()         --> To be called by a future GameManager
 *                          or RunManager when starting a new run
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- singleton setup.
 *   Start() -- syncs stats to InventoryManager and RoundManager,
 *   registers unlocked categories, then triggers the first round.
 *   This is the authoritative Start() that kicks off gameplay —
 *   if initialisation order issues arise with future managers,
 *   review script execution order in Project Settings.
 *   No Update().
 * ============================================================
 */


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Central store for all shop stats. All contractor upgrades route through here.
/// Attach to the GameManager GameObject.
/// Other systems should read shop stats from ShopManager.Instance rather than
/// storing their own copies, to keep everything in sync.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // SHOP STATS
    // ─────────────────────────────────────────────

    [Header("Warehouse")]
    [Tooltip("Current maximum number of inventory slots. Synced with InventoryManager.")]
    public int warehouseSlots = 5;

    [Header("Reputation")]
    [Tooltip("Shop reputation. Starts at 0. Higher values will affect seller quality " +
             "once that system is built. Placeholder for now.")]
    public int reputation = 0;

    [Tooltip("Maximum reputation cap. Adjust as needed.")]
    public int maxReputation = 100;

    [Header("Floor Space")]
    [Tooltip("How many cards are shown per round. Starts at 4. " +
             "Increasing this shows more cards per round.")]
    public int floorSpace = 4;

    [Tooltip("Maximum cards that can be shown per round regardless of floor space upgrades.")]
    public int maxFloorSpace = 8;

    [Header("Unlocked Categories")]
    [Tooltip("Categories that are currently unlocked and eligible to spawn. " +
             "Populated at runtime — do not edit manually. " +
             "To start a category as locked, set canSpawn = false on its asset.")]
    public List<CardCategory> unlockedCategories = new List<CardCategory>();

    [Tooltip("Sub-category strings that have been explicitly unlocked by contractors.")]
    public List<CardSubCategory> unlockedSubCategories = new List<CardSubCategory>();

    [Header("Hired Staff")]
    [Tooltip("List of item types that hired staff can permanently auto-identify. " +
             "e.g. 'Antiques', 'Electronics'. Populated as staff are hired.")]
    public List<CardSubCategory> staffIdentifiedTypes = new List<CardSubCategory>();

    // ─────────────────────────────────────────────
    // EVENTS
    // ─────────────────────────────────────────────

    [Header("Events")]
    public UnityEvent onShopStatsChanged;
    public UnityEvent<string> onCategoryUnlocked;   // Passes category name
    public UnityEvent<CardSubCategory> onStaffHired;         // Passes item type identifier

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Sync all stats to dependent managers before the first round starts
        InventoryManager.Instance.maxSlots = warehouseSlots;
        RoundManager.Instance.cardsPerRound = floorSpace;

        foreach (CardCategory cat in CardDatabase.Instance.allCategories)
        {
            if (cat.canSpawn)
                unlockedCategories.Add(cat);
        }

        // Now that everything is synced, it is safe to start the first round
        RoundManager.Instance.StartNewRound();
    }

    // ─────────────────────────────────────────────
    // UPGRADE APPLICATION
    // ─────────────────────────────────────────────

    /// <summary>
    /// Applies a contractor upgrade based on the upgrade type on the CardData.
    /// Returns a string describing what changed for the confirmation popup.
    /// Format: "StatName|beforeValue|afterValue" — parsed by PopupManager.
    /// Add new cases here as new ContractorUpgradeTypes are introduced.
    /// </summary>
    public string ApplyUpgrade(CardData card)
    {
        switch (card.upgradeType)
        {
            case ContractorUpgradeType.WarehouseSlots:
                return UpgradeWarehouseSlots(card.upgradeAmount);

            case ContractorUpgradeType.Reputation:
                return UpgradeReputation(card.upgradeAmount);

            case ContractorUpgradeType.FloorSpace:
                return UpgradeFloorSpace(card.upgradeAmount);

            case ContractorUpgradeType.UnlockCategory:
                return UnlockCategory(card.categoryToUnlock);

            case ContractorUpgradeType.UnlockSubCategory:
                return UnlockSubCategory(card.subCategoryToUnlock);

            case ContractorUpgradeType.HireStaff:
                return HireStaff(card.staffIdentifiesItemType);

            default:
                Debug.LogWarning($"[ShopManager] No upgrade handler for type '{card.upgradeType}'.");
                return "Unknown|0|0";
        }
    }

    // ─────────────────────────────────────────────
    // INDIVIDUAL UPGRADE METHODS
    // ─────────────────────────────────────────────
    /// <summary>
    /// Increases warehouse slot count and syncs with InventoryManager.
    /// Returns a "StatName|before|after" string for popup display.
    /// </summary>
    private string UpgradeWarehouseSlots(int amount)
    {
        int before = warehouseSlots;
        warehouseSlots += amount;
        // Keep InventoryManager in sync
        InventoryManager.Instance.ExpandSlots(amount);
        onShopStatsChanged?.Invoke();
        Debug.Log($"[ShopManager] Warehouse expanded: {before} → {warehouseSlots}");
        return $"Warehouse Slots|{before}|{warehouseSlots}";
    }

    /// <summary>
    /// Increases shop reputation, clamped to maxReputation.
    /// Returns a "StatName|before|after" string for popup display.
    /// </summary>
    private string UpgradeReputation(int amount)
    {
        int before = reputation;
        reputation = Mathf.Clamp(reputation + amount, 0, maxReputation);
        onShopStatsChanged?.Invoke();
        Debug.Log($"[ShopManager] Reputation increased: {before} → {reputation}");
        return $"Reputation|{before}|{reputation}";
        // ── Future: hook reputation into seller quality spawn weighting here ──
    }

    /// <summary>
    /// Increases floor space (cards per round), clamped to maxFloorSpace.
    /// Syncs with RoundManager.cardsPerRound immediately.
    /// Returns a "StatName|before|after" string for popup display.
    /// </summary>
    private string UpgradeFloorSpace(int amount)
    {
        int before = floorSpace;
        floorSpace = Mathf.Clamp(floorSpace + amount, 1, maxFloorSpace);
        // Keep RoundManager in sync so next round draws the correct number of cards
        RoundManager.Instance.cardsPerRound = floorSpace;
        onShopStatsChanged?.Invoke();
        Debug.Log($"[ShopManager] Floor space increased: {before} → {floorSpace}");
        return $"Floor Space (Cards per Round)|{before}|{floorSpace}";
    }

    /// <summary>
    /// Sets canSpawn = true on the given CardCategory and adds it to
    /// unlockedCategories. Returns a "StatName|before|after" string.
    /// </summary>
    private string UnlockCategory(CardCategory category)
    {
        if (category == null)
        {
            Debug.LogWarning("[ShopManager] UnlockCategory called with null category.");
            return "Category Unlock|failed|failed";
        }

        if (unlockedCategories.Contains(category))
        {
            Debug.Log($"[ShopManager] Category '{category.categoryName}' is already unlocked.");
            return $"Category Unlock|{category.categoryName}|Already Unlocked";
        }

        category.canSpawn = true;
        unlockedCategories.Add(category);
        onCategoryUnlocked?.Invoke(category.categoryName);
        onShopStatsChanged?.Invoke();
        Debug.Log($"[ShopManager] Category unlocked: {category.categoryName}");
        return $"New Category Unlocked|—|{category.categoryName}";
    }

    /// <summary>
    /// Adds the sub-category to unlockedSubCategories and enables
    /// canSpawn on all CardData assets with a matching subCategory.
    /// Returns a "StatName|before|after" string.
    /// </summary>
    private string UnlockSubCategory(CardSubCategory subCategory)
    {
        if (subCategory == CardSubCategory.None)
        {
            Debug.LogWarning("[ShopManager] UnlockSubCategory called with empty string.");
            return "SubCategory Unlock|failed|failed";
        }

        if (unlockedSubCategories.Contains(subCategory))
        {
            Debug.Log($"[ShopManager] Sub-category '{subCategory}' is already unlocked.");
            return $"Sub-Category Unlock|{subCategory}|Already Unlocked";
        }

        unlockedSubCategories.Add(subCategory);

        // Enable canSpawn on all cards matching this sub-category
        foreach (CardData card in CardDatabase.Instance.allCards)
        {
            if (card.subCategory == subCategory)
                card.canSpawn = true;
        }

        onShopStatsChanged?.Invoke();
        Debug.Log($"[ShopManager] Sub-category unlocked: {subCategory}");
        return $"New Sub-Category Unlocked|—|{subCategory}";
    }

    /// <summary>
    /// Adds the item type to staffIdentifiedTypes. Items of this type
    /// will be auto-identified when added to inventory via TryAddItem().
    /// Returns a "StatName|before|after" string.
    /// </summary>
    private string HireStaff(CardSubCategory itemType)
    {
        if (itemType == CardSubCategory.None)
        {
            Debug.LogWarning("[ShopManager] HireStaff called with empty item type.");
            return "Staff|failed|failed";
        }

        if (staffIdentifiedTypes.Contains(itemType))
        {
            Debug.Log($"[ShopManager] Staff for '{itemType}' already hired.");
            return $"Staff ({itemType})|Already Hired|Already Hired";
        }

        staffIdentifiedTypes.Add(itemType);
        onStaffHired?.Invoke(itemType);
        onShopStatsChanged?.Invoke();
        Debug.Log($"[ShopManager] Staff hired — auto-identifies: {itemType}");
        return $"Staff Hired|—|Auto-identifies {itemType}";
    }

    // ─────────────────────────────────────────────
    // QUERY HELPERS
    // Called by other systems to check shop state.
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns true if the shop has hired staff that can identify the given item type.
    /// Called by InventoryManager / appraisal logic when an item is acquired.
    /// </summary>
    public bool CanAutoIdentify(CardSubCategory itemType)
    {
        return staffIdentifiedTypes.Contains(itemType);
    }

    /// <summary>
    /// Resets all shop stats to starting values. Call when starting a new run.
    /// </summary>
    public void ResetShop()
    {
        warehouseSlots = 5;
        reputation = 0;
        floorSpace = 4;
        unlockedCategories.Clear();
        unlockedSubCategories.Clear();
        staffIdentifiedTypes.Clear();

        // Re-sync dependent managers
        InventoryManager.Instance.maxSlots = warehouseSlots;
        RoundManager.Instance.cardsPerRound = floorSpace;

        onShopStatsChanged?.Invoke();
        Debug.Log("[ShopManager] Shop reset to starting values.");
    }
}