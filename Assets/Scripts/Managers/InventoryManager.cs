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

    [Header("Inventory Settings")]
    [Tooltip("Starting number of warehouse slots. Upgradeable via contractors.")]
    public int maxSlots = 5;

    [Header("Runtime State")]
    public List<InventoryItem> items = new List<InventoryItem>();

    // ── Events ──
    public UnityEvent onInventoryChanged;
    public UnityEvent onInventoryFull;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

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
        if (!HasSpace())
        {
            Debug.Log("[InventoryManager] Inventory full — cannot add item.");
            onInventoryFull?.Invoke();
            return false;
        }

        InventoryItem newItem = new InventoryItem(sourceCard);
        items.Add(newItem);

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

        items.Remove(item);
        Debug.Log($"[InventoryManager] Removed '{item.cardName}' from inventory. ({items.Count}/{maxSlots} slots used)");
        onInventoryChanged?.Invoke();
        return true;
    }

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