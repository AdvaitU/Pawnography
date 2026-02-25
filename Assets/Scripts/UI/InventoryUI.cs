using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the inventory screen panel.
/// Reads from InventoryManager and displays current items in warehouse slots.
/// </summary>
public class InventoryUI : MonoBehaviour
{

    public static InventoryUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("References — assign in Inspector")]
    public GameObject inventoryScreen;
    public Button openInventoryButton;
    public Button closeInventoryButton;

    [Tooltip("List of slot root GameObjects (Slot_1 through Slot_5). " +
             "Drag them in here in order.")]
    public List<GameObject> slotObjects = new List<GameObject>();

    [Tooltip("TextMeshPro label inside each slot showing the item name. " +
             "Must match the order of slotObjects.")]
    public List<TextMeshProUGUI> slotNameTexts = new List<TextMeshProUGUI>();

    [Tooltip("Optional: secondary text inside each slot for item value.")]
    public List<TextMeshProUGUI> slotValueTexts = new List<TextMeshProUGUI>();

    private void Start()
    {
        openInventoryButton.onClick.AddListener(OpenInventory);
        closeInventoryButton.onClick.AddListener(CloseInventory);

        // Subscribe to inventory changes so the UI always stays current
        InventoryManager.Instance.onInventoryChanged.AddListener(RefreshInventoryDisplay);

        // Start hidden
        inventoryScreen.SetActive(false);
    }

    /// <summary>
    /// Opens the inventory screen and refreshes the display.
    /// </summary>
    public void OpenInventory()
    {
        inventoryScreen.SetActive(true);
        RefreshInventoryDisplay();
    }

    /// <summary>
    /// Closes the inventory screen.
    /// </summary>
    public void CloseInventory()
    {
        inventoryScreen.SetActive(false);
    }

    /// <summary>
    /// Reads the current inventory state from InventoryManager and updates all slot displays.
    /// Called automatically whenever the inventory changes.
    /// </summary>
    public void RefreshInventoryDisplay()
    {
        List<InventoryItem> items = InventoryManager.Instance.items;
        int totalSlots = InventoryManager.Instance.maxSlots;

        for (int i = 0; i < slotObjects.Count; i++)
        {
            // If this slot index is beyond the player's current max, hide the slot entirely
            // (relevant if you later allow expanding past the initial 5)
            bool slotUnlocked = i < totalSlots;
            slotObjects[i].SetActive(slotUnlocked);

            if (!slotUnlocked) continue;

            // Check if there's an item in this slot index
            bool hasItem = i < items.Count;

            // ── Slot name text ──
            if (slotNameTexts[i] != null)
                slotNameTexts[i].text = hasItem ? items[i].cardName : "Empty";

            // ── Optional value text ──
            if (slotValueTexts.Count > i && slotValueTexts[i] != null)
            {
                if (hasItem)
                {
                    // Show appraised value if known, otherwise show ???
                    slotValueTexts[i].text = items[i].isAppraised
                        ? $"Value: {items[i].appraisedValue}g"
                        : "Value: ???";
                }
                else
                {
                    slotValueTexts[i].text = "";
                }
            }
        }
    }
}