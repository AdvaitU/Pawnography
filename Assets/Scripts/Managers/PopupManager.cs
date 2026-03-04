/*
 * ============================================================
 * SCRIPT:      PopupManager.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   A centre screen (with background overlay) reusable popup 
 *   panel that adapts its content and buttons depending on 
 *   the context it is opened in. 
 *   All card interaction flows that require player input route 
 *   through here. Buttons instantiated dynamically and 
 *   destroyed on close.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardUI              -- calls OpenWarehouseFullWarning()
 *                          and OpenBuyerItemSelection()
 *   CardInteractionManager -- calls OpenConservatorItemSelection(),
 *                          OpenWarehouseFullWarning()
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   OpenPopup()                    --> Internal; used by all
 *                                      pre-built flows
 *   ClosePopup()                   --> Called by button callbacks
 *   OpenWarehouseFullWarning()     --> Called by CardUI and
 *                                      CardInteractionManager
 *   OpenBuyerItemSelection()       --> Called by CardUI
 *   OpenConservatorItemSelection() --> Called by CardInteractionManager
 *   OpenContractorConfirmation()   --> Exists but currently unused
 *   OpenFreelancerConfirmation()   --> Exists but currently unused
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- singleton setup. No Update().
 *   Buttons are Instantiated and Destroyed per popup open/close.
 * ============================================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single reusable popup panel that adapts its content and buttons
/// depending on the context it is opened in.
/// All card interaction flows route through here.
/// Attach to the UIManager GameObject.
/// </summary>
public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("Popup Panel References — assign in Inspector")]
    public GameObject popupPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    [Tooltip("Container that holds the dynamic action buttons (Confirm, Cancel, item list etc.)")]
    public Transform buttonContainer;

    [Tooltip("Prefab for a generic text button used inside the popup.")]
    public GameObject buttonPrefab;

    [Tooltip("Prefab for an inventory item row button used in item selection lists.")]
    public GameObject itemRowPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Always start hidden
        popupPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // CORE OPEN / CLOSE
    // ─────────────────────────────────────────────

    /// <summary>
    /// Clears all previous buttons and content, then shows the popup
    /// with a title and body message.
    /// </summary>
    public void OpenPopup(string title, string body)
    {
        ClearButtons();
        titleText.text = title;
        bodyText.text = body;
        popupPanel.SetActive(true);
    }

    /// <summary>
    /// Closes and resets the popup.
    /// </summary>
    public void ClosePopup()
    {
        popupPanel.SetActive(false);
        ClearButtons();
    }

    /// <summary>
    /// Destroys all dynamically created buttons in the button container.
    /// </summary>
    private void ClearButtons()
    {
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
    }

    // ─────────────────────────────────────────────
    // BUTTON HELPERS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Adds a generic button to the popup with a label and click callback.
    /// </summary>
    public void AddButton(string label, Action onClick, Color? colour = null)
    {
        GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        Button btn = btnObj.GetComponent<Button>();

        if (btnText != null) btnText.text = label;
        if (colour.HasValue)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = colour.Value;
            btn.colors = cb;
        }

        btn.onClick.AddListener(() => onClick());
    }

    /// <summary>
    /// Adds a row button representing an inventory item.
    /// Used in item selection lists for buyer and conservator popups.
    /// </summary>
    public void AddItemRow(InventoryItem item, Action<InventoryItem> onItemSelected)
    {
        GameObject rowObj = Instantiate(itemRowPrefab, buttonContainer);
        TextMeshProUGUI rowText = rowObj.GetComponentInChildren<TextMeshProUGUI>();
        Button rowBtn = rowObj.GetComponent<Button>();

        if (rowText != null)
        {
            // Show item name and appraisal status
            string valueStr = item.isAppraised ? $"{item.appraisedValue}g" : "???";
            rowText.text = $"{item.cardName}  |  Value: {valueStr}  |  Paid: {item.purchasePrice}g";
        }

        rowBtn.onClick.AddListener(() => onItemSelected(item));
    }

    // ─────────────────────────────────────────────
    // PRE-BUILT POPUP FLOWS
    // Each method below opens the popup configured for a specific card type.
    // ─────────────────────────────────────────────

    /// <summary>
    /// Opens a popup informing the player their warehouse is full,
    /// with a button to open the inventory screen so they can sell something.
    /// onOpenInventory is called when the player clicks the inventory button.
    /// </summary>
    public void OpenWarehouseFullWarning(Action onOpenInventory)
    {
        OpenPopup(
            "Warehouse Full",
            "Your warehouse is full. You must sell an item before you can buy more."
        );

        AddButton("Go to Warehouse", () => { ClosePopup(); onOpenInventory(); },
            new Color(0.9f, 0.7f, 0.2f)); // gold

        AddButton("Cancel", () => { ClosePopup(); },
            new Color(0.6f, 0.6f, 0.6f)); // grey
    }

    /// <summary>
    /// Opens an item selection popup for a Buyer card.
    /// Lists all inventory items. Player taps one to sell it to the buyer.
    /// onItemChosen is called with the selected item.
    /// onCancel is called if they back out.
    /// </summary>
    public void OpenBuyerItemSelection(CardData buyerCard, List<InventoryItem> inventory,
        Action<InventoryItem> onItemChosen, Action onCancel)
    {
        OpenPopup(
            $"Sell to: {buyerCard.cardName}",
            $"Looking for: {buyerCard.buyerDesiredItemType}\n" +
            $"Choose an item from your warehouse to sell:"
        );

        if (inventory.Count == 0)
        {
            // No items to sell — just show a close button
            bodyText.text += "\n\n(Your warehouse is empty.)";
            AddButton("Close", () => { ClosePopup(); onCancel(); },
                new Color(0.6f, 0.6f, 0.6f));
            return;
        }

        foreach (InventoryItem item in inventory)
            AddItemRow(item, (chosen) => { ClosePopup(); onItemChosen(chosen); });

        AddButton("Cancel", () => { ClosePopup(); onCancel(); },
            new Color(0.6f, 0.6f, 0.6f));
    }

    /// <summary>
    /// Opens an item selection popup for a Conservator/Expert card.
    /// Lists only unappraised items. Player taps one to have it appraised.
    /// onItemChosen is called with the selected item.
    /// onCancel is called if they back out.
    /// </summary>
    public void OpenConservatorItemSelection(CardData conservatorCard,
        List<InventoryItem> inventory, Action<InventoryItem> onItemChosen, Action onCancel)
    {
        // Filter to only unappraised items
        List<InventoryItem> unappraisedItems = inventory.FindAll(i => !i.isAppraised);


        OpenPopup(
            $"Appraise with: {conservatorCard.cardName}",
            $"Expertise: {conservatorCard.conservatorExpertise}%\n\n" +
            $"Choose an item to appraise:"
        );

        if (unappraisedItems.Count == 0)
        {
            bodyText.text += "\n\n(No unappraised items in warehouse.)";
            AddButton("Close", () => { ClosePopup(); onCancel(); },
                new Color(0.6f, 0.6f, 0.6f));
            return;
        }

        foreach (InventoryItem item in unappraisedItems)
            AddItemRow(item, (chosen) => { ClosePopup(); onItemChosen(chosen); });

        AddButton("Cancel", () => { ClosePopup(); onCancel(); },
            new Color(0.6f, 0.6f, 0.6f));
    }
}