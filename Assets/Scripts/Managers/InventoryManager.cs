/*
 * ============================================================
 * SCRIPT:      InventoryManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Manages the player's item inventory (warehouse). Stores
 *   InventoryItem instances at runtime, enforces the slot
 *   limit, and fires UnityEvents when the inventory changes.
 *   Also checks ShopManager for hired staff auto-identification
 *   when a new item is added. InventoryItem is defined in this
 *   file as a plain serializable class.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardInteractionManager -- calls TryAddItem() for Seller cards,
 *                          TryRemoveItem() for Buyer cards, reads
 *                          items list for Buyer and Conservator
 *                          item selection popups
 *   ShopManager         -- calls ExpandSlots() when a Warehouse
 *                          contractor is applied; sets maxSlots
 *                          directly on Start() and ResetShop()
 *   FreelancerManager   -- calls TryAddItem() when a freelancer
 *                          returns with an item
 *   InventoryUI         -- reads items list and maxSlots in
 *                          RefreshInventoryDisplay()
 *   PopupManager        -- reads items list to populate buyer
 *                          and conservator item selection lists
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   TryAddItem()        --> Called by CardInteractionManager
 *                          (Seller flow) and FreelancerManager
 *                          (freelancer return)
 *   TryRemoveItem()     --> Called by CardInteractionManager
 *                          (Buyer flow)
 *   ExpandSlots()       --> Called by ShopManager when a
 *                          Warehouse contractor upgrade is applied
 *   HasSpace()          --> Called by CardInteractionManager
 *                          before showing the Seller popup
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- singleton setup only. No Update().
 *   items list is iterated by UI on every inventory change event.
 *   If inventory becomes very large, consider caching UI updates.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Rendering.GPUSort;

/// <summary>
/// Manages the player's item inventory (warehouse slots).
/// Items are stored as InventoryItem instances wrapping a CardData reference.
/// Attach to the same persistent GameObject.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // MEMBERS ======================================================================
    [Header("Inventory Settings")]
    [Tooltip("Starting number of warehouse slots. Upgradeable via contractors.")]
    public int maxSlots = 5;

    [Header("Runtime State")]
    [Tooltip("The list of items currently in the player's inventory. " +
             "Each item wraps a reference to its source CardData and runtime state like appraisal status.")]
    public List<InventoryItem> items = new List<InventoryItem>();

    // EVENTS ======================================================================
    public UnityEvent onInventoryChanged;
    public UnityEvent onInventoryFull;

    // METHODS =====================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // HasSpace() - Getter that checks whether there is any space to add to the inventory.
    // Called by the CardInteractionManager before showing the Seller card popup,
    // and by the popup itself before confirming a purchase.
    /// <summary>
    /// Returns true if the inventory has at least one free slot.
    /// </summary>
    public bool HasSpace() => items.Count < maxSlots;

    /// <summary>
    /// Attempts to add a new item to inventory.
    /// Returns true on success, false if inventory is full.
    /// </summary>
    public bool TryAddItem(CardData sourceCard)
    {
        if (!HasSpace())  // Failsafe using helper getter method.
        {
            Debug.Log("[InventoryManager] Inventory full — cannot add item.");
            onInventoryFull?.Invoke();
            return false;
        }

        InventoryItem newItem = new InventoryItem(sourceCard);  // Create new item of type InventoryItem, which wraps the source CardData.

        // Check if hired staff can auto-identify this item type
        // subCategory on the card corresponds to the item type (e.g. "Antiques")
        //if (!string.IsNullOrEmpty(sourceCard.subCategory) &&
        //ShopManager.Instance.CanAutoIdentify(sourceCard.subCategory))
        if (sourceCard.subCategory != CardSubCategory.None && ShopManager.Instance.CanAutoIdentify(sourceCard.subCategory))
            {
            newItem.isAppraised = true;
            newItem.appraisedValue = sourceCard.itemTrueValue;
            Debug.Log($"[InventoryManager] '{sourceCard.cardName}' auto-identified by staff " +
                      $"as worth {sourceCard.itemTrueValue}g.");
        }

        items.Add(newItem);  // Adds to item list
        Debug.Log($"[InventoryManager] Added '{sourceCard.cardName}' to inventory. ({items.Count}/{maxSlots} slots used)");
        onInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes a specific InventoryItem (e.g. after selling it).
    /// </summary>
    public bool TryRemoveItem(InventoryItem item)
    {
        if (!items.Contains(item))
        {
            Debug.LogWarning("[InventoryManager] Tried to remove an item not in inventory.");
            return false;
        }

        items.Remove(item);  // Removes from item list
        Debug.Log($"[InventoryManager] Removed '{item.cardName}' from inventory. ({items.Count}/{maxSlots} slots used)");
        onInventoryChanged?.Invoke();
        return true;
    }

    // ExpandSlots() - Increases the maxSlots by a given amount, called by contractor upgrades.
    /// <summary>
    /// Expands the warehouse by a given number of slots (called by contractor upgrades).
    /// </summary>
    public void ExpandSlots(int additionalSlots)
    {
        maxSlots += additionalSlots;
        Debug.Log($"[InventoryManager] Warehouse expanded to {maxSlots} slots.");
        onInventoryChanged?.Invoke();
    }
}


// ==========================================================================================================================
// ------------------------------------------------- INVENTORY ITEM CLASS ---------------------------------------------------
// ==========================================================================================================================

/// <summary>
/// Runtime representation of an item sitting in the player's warehouse.
/// Wraps the source CardData and adds runtime-only state like appraisal status.
/// This is a plain class (not a MonoBehaviour) — lives only in memory during a run.
/// </summary>
[System.Serializable]
public class InventoryItem
{
    [Header("Source Data")]
    public string cardName;
    public CardData sourceCard; // Reference to the original card definition

    [Header("Item State")]
    [Tooltip("The price the player paid to acquire this item.")]
    public int purchasePrice;

    [Tooltip("True once a conservator/expert has appraised this item.")]
    public bool isAppraised = false;

    [Tooltip("The appraised value — may differ from sourceCard.itemTrueValue depending on appraiser accuracy.")]
    public int appraisedValue = 0;

    [Tooltip("If true, this item is reserved for an active freelancer or buyer deal.")]
    public bool isReserved = false;

    // Constructor — initialises from a CardData asset
    public InventoryItem(CardData card)
    {
        sourceCard = card;
        cardName = card.cardName;
        purchasePrice = card.itemBuyCost;
        // True value is hidden until appraised
        isAppraised = false;
        appraisedValue = 0;
    }
}