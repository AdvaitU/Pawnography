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

    [Header("Positioning")]
    [Tooltip("Reference to the Canvas for screen space calculations.")]
    public Canvas parentCanvas;

    [Tooltip("Padding below the source card in pixels.")]
    public Vector2 screenPadding = new Vector2(20f, 20f);

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
    /// with a title and body message, positioned below the source card.
    /// Pass null for sourceRect to fall back to bottom-left corner.
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
    /// Adds a row button representing an inventory item or pending seller.
    /// Pass isClaimed = true to show the row greyed out and unclickable.
    /// </summary>
    public void AddItemRow(InventoryItem item, Action<InventoryItem> onItemSelected,
                           bool isClaimed = false)
    {
        GameObject rowObj = Instantiate(itemRowPrefab, buttonContainer);
        TextMeshProUGUI rowText = rowObj.GetComponentInChildren<TextMeshProUGUI>();
        Button rowBtn = rowObj.GetComponent<Button>();

        if (rowText != null)
        {
            string valueStr = item.isAppraised ? $"{item.appraisedValue}g" : "???";
            rowText.text = $"{item.cardName} - Value: {valueStr}, Paid: {item.purchasePrice}g";

            if (isClaimed)
                rowText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        if (rowBtn != null)
        {
            rowBtn.interactable = !isClaimed;
            if (!isClaimed)
                rowBtn.onClick.AddListener(() => onItemSelected(item));
        }
    }

    /// <summary>
    /// Adds a row button representing a pending seller card (not yet in inventory).
    /// Shows a "Pending purchase" label. Pass isClaimed = true to grey it out.
    /// </summary>
    public void AddPendingSellerRow(StagedCardData pendingSeller,
                                    Action<StagedCardData> onSellerSelected,
                                    bool isClaimed = false)
    {
        GameObject rowObj = Instantiate(itemRowPrefab, buttonContainer);
        TextMeshProUGUI rowText = rowObj.GetComponentInChildren<TextMeshProUGUI>();
        Button rowBtn = rowObj.GetComponent<Button>();

        if (rowText != null)
        {
            string valueStr = pendingSeller.card.valueIsHidden
                ? "???"
                : $"{pendingSeller.card.itemTrueValue}g";
            rowText.text = $"{pendingSeller.card.cardName} - Value: {valueStr},  " +
                           $"Cost: {pendingSeller.card.itemBuyCost}g  " +
                           $" *";

            rowText.color = isClaimed
                ? new Color(0.5f, 0.5f, 0.5f, 1f)
                : new Color(0.9f, 0.7f, 0.2f, 1f); // gold tint for pending
        }

        if (rowBtn != null)
        {
            rowBtn.interactable = !isClaimed;
            if (!isClaimed)
                rowBtn.onClick.AddListener(() => onSellerSelected(pendingSeller));
        }
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
    /// Returns true if the given pending seller StagedCardData is already
    /// targeted by another staged card that is not the current card.
    /// Used to grey out claimed pending sellers in item selection popups.
    /// </summary>
    private bool IsClaimedByAnotherCard(StagedCardData pendingSeller, CardData currentCard)
    {
        foreach (StagedCardData staged in RoundManager.Instance.stagedCards)
        {
            if (staged.card == currentCard) continue;
            if (staged.pendingSellerTarget == pendingSeller) return true;
        }
        return false;
    }

    /// <summary>
    /// Opens an item selection popup for a Buyer card. Lists all inventory
    /// items and any pending seller cards staged this round. Pending sellers
    /// already claimed by another card are shown greyed and unclickable.
    /// onItemChosen passes the chosen InventoryItem and the StagedCardData
    /// if a pending seller was chosen (null otherwise).
    /// </summary>
    public void OpenBuyerItemSelection(CardData buyerCard, List<InventoryItem> inventory,
        List<StagedCardData> pendingSellers,
        Action<InventoryItem, StagedCardData> onItemChosen, Action onCancel)
    {
        OpenPopup(
            $"Sell to: {buyerCard.cardName}",
            $"Looking for: {buyerCard.buyerDesiredItemType}\n\n" +
            $"Choose an item from your warehouse to sell:"
        );

        bool hasAnything = inventory.Count > 0 || pendingSellers.Count > 0;

        if (!hasAnything)
        {
            bodyText.text += "\n\n(Your warehouse is empty and no items are pending purchase.)";
            AddButton("Close", () => { ClosePopup(); onCancel(); },
                new Color(0.6f, 0.6f, 0.6f));
            return;
        }

        // Existing inventory items
        foreach (InventoryItem item in inventory)
            AddItemRow(item, (chosen) => { ClosePopup(); onItemChosen(chosen, null); });

        // Pending seller items
        foreach (StagedCardData pending in pendingSellers)
        {
            bool isClaimed = IsClaimedByAnotherCard(pending, buyerCard);
            AddPendingSellerRow(pending,
                (chosen) => { ClosePopup(); onItemChosen(null, chosen); },
                isClaimed);
        }

        AddButton("Cancel", () => { ClosePopup(); onCancel(); },
            new Color(0.6f, 0.6f, 0.6f));
    }

    /// <summary>
    /// Opens an item selection popup for a Conservator card. Lists unappraised
    /// inventory items and any pending seller cards staged this round.
    /// Pending sellers already claimed by another card are shown greyed
    /// and unclickable.
    /// onItemChosen passes the chosen InventoryItem and the StagedCardData
    /// if a pending seller was chosen (null otherwise).
    /// </summary>
    public void OpenConservatorItemSelection(CardData conservatorCard,
        List<InventoryItem> inventory, List<StagedCardData> pendingSellers,
        Action<InventoryItem, StagedCardData> onItemChosen, Action onCancel)
    {
        List<InventoryItem> unappraisedItems = inventory.FindAll(i => !i.isAppraised);

        OpenPopup(
            $"Appraise with: {conservatorCard.cardName}",
            $"Expertise: {conservatorCard.conservatorExpertise}\n\n" +
            $"Choose an item to appraise:"
        );

        bool hasAnything = unappraisedItems.Count > 0 || pendingSellers.Count > 0;

        if (!hasAnything)
        {
            bodyText.text += "\n\n(No unappraised items in warehouse and " +
                             "no items are pending purchase.)";
            AddButton("Close", () => { ClosePopup(); onCancel(); },
                new Color(0.6f, 0.6f, 0.6f));
            return;
        }

        // Unappraised inventory items
        foreach (InventoryItem item in unappraisedItems)
            AddItemRow(item, (chosen) => { ClosePopup(); onItemChosen(chosen, null); });

        // Pending seller items
        foreach (StagedCardData pending in pendingSellers)
        {
            bool isClaimed = IsClaimedByAnotherCard(pending, conservatorCard);
            AddPendingSellerRow(pending,
                (chosen) => { ClosePopup(); onItemChosen(null, chosen); },
                isClaimed);
        }

        AddButton("Cancel", () => { ClosePopup(); onCancel(); },
            new Color(0.6f, 0.6f, 0.6f));
    }
}