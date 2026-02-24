using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attached to each card prefab instance.
/// Receives a CardData reference and populates all UI elements accordingly.
/// Also handles the visual selected state and the Select button click.
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("UI References — assign in Inspector")]
    public Image categoryBanner;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI valueText;
    public Button selectButton;
    public GameObject selectedOverlay;

    [Tooltip("The Image component that displays the card's unique artwork.")]
    public Image cardArtImage;

    [Header("Category Colours — assign in Inspector")]
    [Tooltip("Colour for Seller category banner")]
    public Color sellerColour = new Color(0.2f, 0.6f, 0.9f);   // blue
    [Tooltip("Colour for Buyer category banner")]
    public Color buyerColour = new Color(0.9f, 0.6f, 0.2f);    // orange
    [Tooltip("Colour for Conservator category banner")]
    public Color conservatorColour = new Color(0.4f, 0.8f, 0.4f); // green
    [Tooltip("Colour for Contractor category banner")]
    public Color contractorColour = new Color(0.7f, 0.4f, 0.9f);  // purple
    [Tooltip("Colour for Freelancer category banner")]
    public Color freelancerColour = new Color(0.9f, 0.3f, 0.3f);  // red

    [Header("Runtime State")]
    public CardData assignedCard;
    public bool isSelected = false;

    private void Awake()
    {
        // Hook up the select button click
        selectButton.onClick.AddListener(OnSelectButtonClicked);

        // Make sure the selected overlay starts hidden
        if (selectedOverlay != null)
            selectedOverlay.SetActive(false);
    }

    /// <summary>
    /// Call this to populate the card with data from a CardData ScriptableObject.
    /// Called by CardUIManager when spawning cards for a new round.
    /// </summary>
    public void Populate(CardData card)
    {
        assignedCard = card;
        isSelected = false;

        if (selectedOverlay != null)
            selectedOverlay.SetActive(false);

        // ── Text fields ──
        cardNameText.text = card.cardName;
        cardDescriptionText.text = card.cardDescription;
        categoryText.text = card.category != null ? card.category.categoryName : "Unknown";

        // ── Category banner colour ──
        if (categoryBanner != null && card.category != null)
            categoryBanner.color = GetCategoryColour(card.category.categoryName);

        // ── Card art ──
        // If the CardData has a sprite assigned, display it.
        // If not, the image stays white (its default colour) acting as an empty placeholder.
        if (cardArtImage != null)
        {
            if (card.cardArt != null)
            {
                cardArtImage.sprite = card.cardArt;
                cardArtImage.color = Color.white; // Ensure full visibility
            }
            else
            {
                cardArtImage.sprite = null;
                cardArtImage.color = Color.white; // Plain white placeholder
            }
        }

        // ── Stat fields — shown conditionally based on category ──
        UpdateStatFields(card);
    }

    /// <summary>
    /// Populates the cost and value text fields based on what kind of card this is.
    /// Extend this switch block when you add new card categories.
    /// </summary>
    private void UpdateStatFields(CardData card)
    {
        // Hide both by default, then show relevant ones
        costText.gameObject.SetActive(false);
        valueText.gameObject.SetActive(false);

        if (card.category == null) return;

        switch (card.category.categoryName)
        {
            case "Seller":
                costText.gameObject.SetActive(true);
                valueText.gameObject.SetActive(true);
                costText.text = $"Buy Cost: {card.itemBuyCost}g";
                // Hide true value until appraised
                valueText.text = card.valueIsHidden ? "Value: ???" : $"Value: {card.itemTrueValue}g";
                break;

            case "Buyer":
                valueText.gameObject.SetActive(true);
                valueText.text = $"Offers: {card.buyerOfferedPrice}g";
                costText.gameObject.SetActive(true);
                costText.text = $"Wants: {card.buyerDesiredItemType}";
                break;

            case "Conservator":
                valueText.gameObject.SetActive(true);
                valueText.text = $"Accuracy: {Mathf.RoundToInt(card.appraisalAccuracy * 100)}%";
                break;

            case "Contractor":
                costText.gameObject.SetActive(true);
                valueText.gameObject.SetActive(true);
                costText.text = $"Upgrades: {card.upgradeTargetStat}";
                valueText.text = $"Amount: +{card.upgradeAmount}";
                break;

            case "Freelancer":
                costText.gameObject.SetActive(true);
                valueText.gameObject.SetActive(true);
                costText.text = $"Returns in: {card.roundsToReturn} rounds";
                valueText.text = $"Item value: {card.freelancerMinItemValue}-{card.freelancerMaxItemValue}g";
                break;

                // ── Add new cases here as you introduce new card categories ──
        }
    }

    /// <summary>
    /// Returns the banner colour associated with a given category name.
    /// Add new entries here when you add new categories.
    /// </summary>
    private Color GetCategoryColour(string categoryName)
    {
        switch (categoryName)
        {
            case "Seller": return sellerColour;
            case "Buyer": return buyerColour;
            case "Conservator": return conservatorColour;
            case "Contractor": return contractorColour;
            case "Freelancer": return freelancerColour;
            default: return Color.grey;
        }
    }

    /// <summary>
    /// Called when the player clicks the Select button on this card.
    /// Passes the selection attempt to RoundManager.
    /// </summary>
    private void OnSelectButtonClicked()
    {
        if (isSelected) return; // Already selected, ignore

        bool accepted = RoundManager.Instance.TrySelectCard(assignedCard);

        if (accepted)
        {
            SetSelectedVisual(true);
        }
    }

    /// <summary>
    /// Enables or disables the selected overlay and locks the button.
    /// </summary>
    public void SetSelectedVisual(bool selected)
    {
        isSelected = selected;

        if (selectedOverlay != null)
            selectedOverlay.SetActive(selected);

        // Disable the button after selection so it can't be clicked again
        selectButton.interactable = !selected;
    }
}