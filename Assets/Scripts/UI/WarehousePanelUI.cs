/*
 * ============================================================
 * SCRIPT:      WarehousePanelUI.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Controls the collapsible warehouse panel below the HUD.
 *   Displays inventory items as a horizontal row of slots,
 *   each showing item art (if available), name, and condition.
 *   Empty slots show a placeholder. Registers with
 *   PanelStackManager so the card row adjusts correctly when
 *   this panel is open alongside the shop panel.
 *   Replaces the old full-screen inventory overlay for in-game
 *   warehouse viewing.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   InventoryManager    -- RefreshWarehouse() subscribed to
 *                          onInventoryChanged event
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   RefreshWarehouse()  --> Called by InventoryManager event
 *                          and can be called manually
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). Slot UI is rebuilt on every inventory change.
 *   With a max of ~10 slots this is negligible.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WarehousePanelUI : MonoBehaviour
{
    public static WarehousePanelUI Instance { get; private set; }

    [Header("Panel References")]
    public GameObject warehousePanel;
    public Button warehouseButton;
    public TextMeshProUGUI warehouseButtonLabel;

    [Tooltip("Height of the warehouse panel in pixels. " +
             "Should match ShopPanel height.")]
    public float panelHeight = 180f;

    [Header("Slot Container")]
    [Tooltip("The horizontal layout group container for item slots.")]
    public Transform slotContainer;

    [Tooltip("Prefab for a single warehouse slot.")]
    public GameObject slotPrefab;

    [Header("Slot Sizing")]
    [Tooltip("Width of each slot in pixels.")]
    public float slotWidth = 140f;

    [Tooltip("Height of each slot in pixels. Should fit within panel height.")]
    public float slotHeight = 150f;

    // Internal state
    private bool isPanelOpen = false;
    private List<GameObject> activeSlots = new List<GameObject>();

    private const string PANEL_ID = "Warehouse";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        warehouseButton.onClick.AddListener(ToggleWarehousePanel);

        // Register with PanelStackManager
        PanelStackManager.Instance.RegisterPanel(PANEL_ID, warehousePanel, panelHeight);

        // Subscribe to inventory changes
        InventoryManager.Instance.onInventoryChanged.AddListener(RefreshWarehouse);

        warehousePanel.SetActive(false);
    }

    /// <summary>
    /// Toggles the warehouse panel open or closed.
    /// Notifies PanelStackManager to update card row position.
    /// </summary>
    public void ToggleWarehousePanel()
    {
        isPanelOpen = !isPanelOpen;
        warehousePanel.SetActive(isPanelOpen);

        warehouseButtonLabel.text = isPanelOpen ? "Warehouse ▲" : "Warehouse ▼";

        PanelStackManager.Instance.SetPanelOpen(PANEL_ID, isPanelOpen);

        if (isPanelOpen)
            RefreshWarehouse();
    }

    /// <summary>
    /// Rebuilds the slot display from current inventory state.
    /// Called on every inventory change and on panel open.
    /// </summary>
    public void RefreshWarehouse()
    {
        if (!isPanelOpen) return;

        // Clear existing slots
        foreach (GameObject slot in activeSlots)
            Destroy(slot);
        activeSlots.Clear();

        int maxSlots = InventoryManager.Instance.maxSlots;
        List<InventoryItem> items = InventoryManager.Instance.items;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);

            // Set slot size via LayoutElement
            LayoutElement le = slotObj.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = slotWidth;
                le.preferredHeight = slotHeight;
                le.minWidth = slotWidth;
            }

            bool hasItem = i < items.Count;
            PopulateSlot(slotObj, hasItem ? items[i] : null);
            activeSlots.Add(slotObj);
        }
    }

    /// <summary>
    /// Fills a slot with item data or shows an empty placeholder.
    /// </summary>
    private void PopulateSlot(GameObject slotObj, InventoryItem item)
    {
        WarehouseSlot slot = slotObj.GetComponent<WarehouseSlot>();
        WarehouseSlotHover hoverHandler = slotObj.GetComponent<WarehouseSlotHover>();

        if (hoverHandler != null)
            hoverHandler.SetItemData(item);

        if (slot == null)
        {
            Debug.LogWarning("[WarehousePanelUI] Slot prefab missing WarehouseSlot component.");
            return;
        }

        if (item == null)
        {
            if (slot.artImage != null)
            {
                slot.artImage.sprite = null;
                slot.artImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            }
            if (slot.nameText != null) slot.nameText.text = "Empty";
            return;
        }

        if (slot.artImage != null)
        {
            bool hasArt = item.sourceCard != null && item.sourceCard.cardArt != null;
            slot.artImage.sprite = hasArt ? item.sourceCard.cardArt : null;
            slot.artImage.color = hasArt ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        if (slot.nameText != null)
        {
            slot.nameText.text = "";   // Leave Empty
        }
            

    }

    /// <summary>
    /// Opens the warehouse panel programmatically.
    /// Used by CardInteractionManager when inventory is full.
    /// </summary>
    public void OpenWarehouse()
    {
        if (!isPanelOpen)
            ToggleWarehousePanel();
    }
}