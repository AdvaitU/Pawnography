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
    /// Opens a confirmation popup for purchasing an item from a Seller.
    /// onConfirm is called if the player accepts, onCancel if they back out.
    /// </summary>
    /*public void OpenSellerConfirmation(CardData card, Action onConfirm, Action onCancel)
    {
        string valueStr = card.valueIsHidden ? "???" : $"{card.itemTrueValue}g";
        OpenPopup(
            $"Buy: {card.cardName}",
            $"The seller is asking {card.itemBuyCost}g for this item.\n" +
            $"Estimated value: {valueStr}\n\nDo you want to buy it?"
        );

        AddButton("Buy", () => { ClosePopup(); onConfirm(); },
            new Color(0.3f, 0.75f, 0.3f)); // green

        AddButton("Cancel", () => { ClosePopup(); onCancel(); },
            new Color(0.8f, 0.3f, 0.3f)); // red
    }*/

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
            $"Offering: {buyerCard.buyerOfferedPrice}g\n\n" +
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
            $"Accuracy: {Mathf.RoundToInt(conservatorCard.appraisalAccuracy * 100)}%\n\n" +
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

    /// <summary>
    /// Opens a contractor confirmation popup showing the before and after stat values.
    /// upgradeResult is the pipe-delimited string returned by ShopManager.ApplyUpgrade()
    /// in the format "StatName|beforeValue|afterValue".
    /// </summary>
    public void OpenContractorConfirmation(CardData card, string upgradeResult, Action onConfirm)
    {
        // Parse the result string from ShopManager
        string[] parts = upgradeResult.Split('|');
        string statName = parts.Length > 0 ? parts[0] : "Stat";
        string beforeVal = parts.Length > 1 ? parts[1] : "?";
        string afterVal = parts.Length > 2 ? parts[2] : "?";

        OpenPopup(
            $"Contractor: {card.cardName}",
            $"{card.cardDescription}\n\n" +
            $"{statName}:  {beforeVal}  →  {afterVal}"
        );

        AddButton("OK", () => { ClosePopup(); onConfirm(); },
            new Color(0.3f, 0.75f, 0.3f));
    }

    /// <summary>
    /// Opens a confirmation popup for a Freelancer being sent out.
    /// </summary>
    public void OpenFreelancerConfirmation(CardData card, Action onConfirm, Action onCancel)
    {
        OpenPopup(
            $"Send out: {card.cardName}",
            $"This freelancer will return in {card.roundsToReturn} rounds with an item " +
            $"worth between {card.freelancerMinItemValue}g and {card.freelancerMaxItemValue}g.\n\n" +
            $"Send them out?"
        );

        AddButton("Send Out", () => { ClosePopup(); onConfirm(); },
            new Color(0.3f, 0.75f, 0.3f));

        AddButton("Cancel", () => { ClosePopup(); onCancel(); },
            new Color(0.6f, 0.6f, 0.6f));
    }
}