/*
 * ============================================================
 * SCRIPT:      WarehousePanelUI.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Controls the collapsible warehouse panel that slides down
 *   from the top of the screen. The panel is always active —
 *   visibility is controlled entirely by its Y position via
 *   PanelStackManager, which lerps it in and out of view.
 *
 *   The Warehouse button is a child of the panel, anchored to
 *   the panel's bottom edge. When the panel is closed (above
 *   the canvas), the button peeks fully below the canvas top
 *   edge as a pull-handle.
 *
 *   ShopPanel is also a child of this panel (anchored to its
 *   top edge). It rides along silently whenever the Warehouse
 *   moves. Its visibility is managed by ShopStatsUI.
 *
 *   WAREHOUSE BUTTON behaviour:
 *     Shop closed → toggle Warehouse only (topOffset = 0).
 *                   Warehouse slides to Y = 0; Shop remains
 *                   above the canvas.
 *     Shop open   → close both panels. Calls
 *                   ShopStatsUI.ForceClose() to reset its
 *                   state and label, then closes the Warehouse.
 *                   Both panels slide back above canvas.
 *
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   InventoryManager       -- RefreshWarehouse() subscribed to
 *                             onInventoryChanged event
 *   CardInteractionManager -- calls OpenWarehouse() when
 *                             inventory is full
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   OpenWarehouse()  --> CardInteractionManager (inventory full)
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). Slot UI rebuilt on inventory change (panel
 *   open only). Max ~10 slots — rebuild cost negligible.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WarehousePanelUI : MonoBehaviour
{
    public static WarehousePanelUI Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────

    [Header("Panel References")]
    public GameObject warehousePanel;
    public Button warehouseButton;
    public TextMeshProUGUI warehouseButtonLabel;

    [Tooltip("Height of the warehouse panel in pixels. " +
             "Must match the panel RectTransform height exactly.")]
    public float panelHeight = 180f;

    [Header("Slot Container")]
    public Transform slotContainer;
    public GameObject slotPrefab;

    [Header("Slot Sizing")]
    public float slotWidth = 140f;
    public float slotHeight = 150f;

    // ── Internal state ───────────────────────────────────────

    private bool isPanelOpen = false;
    private List<GameObject> activeSlots = new List<GameObject>();

    private const string PANEL_ID = "Warehouse";

    // ── Lifecycle ────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        warehouseButton.onClick.AddListener(ToggleWarehousePanel);

        // Registers the panel and snaps it to closed position
        // (anchoredPosition.y = panelHeight) immediately.
        // ShopPanel (child) is snapped above canvas too by this move.
        PanelStackManager.Instance.RegisterPanel(PANEL_ID, warehousePanel, panelHeight);

        InventoryManager.Instance.onInventoryChanged.AddListener(RefreshWarehouse);
    }

    /// <summary>
    /// Called by ShopStatsUI when it opens both panels together.
    /// Sets isPanelOpen so RefreshWarehouse() doesn't no-op,
    /// without triggering a redundant PanelStackManager call.
    /// </summary>
    public void NotifyOpen()
    {
        isPanelOpen = true;
        warehouseButtonLabel.text = "Warehouse";
        RefreshWarehouse();
    }

    /// <summary>
    /// Called by ShopStatsUI when it closes both panels together.
    /// Keeps isPanelOpen in sync without touching PanelStackManager.
    /// </summary>
    public void NotifyClose()
    {
        isPanelOpen = false;
        warehouseButtonLabel.text = "Warehouse";
    }

    // ── Toggle ───────────────────────────────────────────────

    /// <summary>
    /// Called by the Warehouse button.
    ///
    /// If the Shop is open: close both panels together.
    ///   ShopStatsUI.ForceClose() resets Shop state and label.
    ///   Then close the Warehouse (topOffset 0 — both are going
    ///   back above the canvas regardless).
    ///
    /// If the Shop is closed: toggle Warehouse only.
    ///   topOffset = 0 so Warehouse open Y = 0.
    ///   Shop stays above canvas (Warehouse Y = 0 means Shop,
    ///   child at +shopHeight above Warehouse top, is still
    ///   above the canvas edge).
    /// </summary>
    public void ToggleWarehousePanel()
    {
        bool shopIsOpen = ShopStatsUI.Instance != null && ShopStatsUI.Instance.IsOpen;

        if (shopIsOpen)
        {
            // Close both panels
            ShopStatsUI.Instance.ForceClose();
            isPanelOpen = false;
            warehouseButtonLabel.text = "Warehouse";
            PanelStackManager.Instance.SetPanelOpen(PANEL_ID, false);
            return;
        }

        // Normal Warehouse-only toggle
        isPanelOpen = !isPanelOpen;

        // topOffset = 0: Warehouse open Y = 0. Shop is above the canvas.
        PanelStackManager.Instance.SetPanelOpen(PANEL_ID, isPanelOpen, 0f);

        if (isPanelOpen)
            RefreshWarehouse();
    }

    // ── Slot refresh ─────────────────────────────────────────

    /// <summary>
    /// Rebuilds slot display from current inventory.
    /// No-ops if Warehouse is closed.
    /// </summary>
    public void RefreshWarehouse()
    {
        if (!isPanelOpen) return;

        foreach (GameObject slot in activeSlots)
            Destroy(slot);
        activeSlots.Clear();

        int maxSlots = InventoryManager.Instance.maxSlots;
        List<InventoryItem> items = InventoryManager.Instance.items;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);

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

    // ── Slot population ──────────────────────────────────────

    private void PopulateSlot(GameObject slotObj, InventoryItem item)
    {
        WarehouseSlot slot = slotObj.GetComponent<WarehouseSlot>();
        WarehouseSlotHover hoverHandler = slotObj.GetComponent<WarehouseSlotHover>();

        if (hoverHandler != null)
            hoverHandler.SetItemData(item);

        slot.PlayEntryAnimation(item != null);

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
            slot.nameText.text = "";
    }

    // ── Programmatic open ────────────────────────────────────

    /// <summary>
    /// Opens the Warehouse panel if not already open.
    /// Called by CardInteractionManager when inventory is full.
    /// Only opens Warehouse (not Shop) — topOffset = 0.
    /// </summary>
    public void OpenWarehouse()
    {
        if (!isPanelOpen)
            ToggleWarehousePanel();
    }
}