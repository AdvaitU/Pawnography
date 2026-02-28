/*
 * ============================================================
 * SCRIPT:      HoverPopupUI.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Controls the hover detail popup panel that appears when
 *   the cursor dwells over a card. Receives a CardData reference
 *   from CardHoverHandler and populates all detail fields.
 *   ShowPopup() and HidePopup() are the two entry points —
 *   animation logic should be added inside these methods when
 *   ready, without needing to change CardHoverHandler.
 *   The popup follows the mouse position with a configurable
 *   offset so it does not obscure the card being inspected.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardHoverHandler    -- calls ShowPopup() and HidePopup()
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   ShowPopup()         --> Called by CardHoverHandler after
 *                          hover delay elapses
 *   HidePopup()         --> Called by CardHoverHandler when
 *                          cursor leaves the card
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Update() -- runs only to reposition the popup panel to
 *   follow the mouse while the popup is visible. Consider
 *   disabling this component when no cards are on screen.
 * ============================================================
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HoverPopupUI : MonoBehaviour
{
    public static HoverPopupUI Instance { get; private set; }

    [Header("Panel Reference")]
    public GameObject popupPanel;

    [Header("UI Fields — assign in Inspector")]
    public TextMeshProUGUI hoverCardNameText;
    public TextMeshProUGUI hoverCategoryText;
    public TextMeshProUGUI hoverDescriptionText;
    public TextMeshProUGUI hoverCostText;
    public TextMeshProUGUI hoverValueText;
    public TextMeshProUGUI hoverConditionText;
    public TextMeshProUGUI hoverExtraInfoText;
    public Image hoverCategoryBanner;

    [Tooltip("Reference to the Canvas for screen space calculations.")]
    public Canvas parentCanvas;

    [Tooltip("Padding from the bottom-right edge of the screen in pixels.")]
    public Vector2 screenPadding = new Vector2(20f, 20f);

    private bool isVisible = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        popupPanel.SetActive(false);
    }

    /// <summary>
    /// Shows the hover popup populated with data from the given card.
    /// Add animation logic here when ready — e.g. a fade-in or scale tween.
    /// The popup will display correctly before any animation is added.
    /// </summary>
    public void ShowPopup(CardData card)
    {
        PopulatePopup(card);
        PositionAtBottomLeft();
        popupPanel.SetActive(true);
        isVisible = true;
    }

    /// <summary>
    /// Hides the hover popup.
    /// Add animation logic here when ready — e.g. a fade-out before SetActive(false).
    /// </summary>
    public void HidePopup()
    {
        isVisible = false;
        popupPanel.SetActive(false);

        // ── Animation hook ──
        // When ready, play hide animation here before deactivating.
    }

    /// <summary>
    /// Fills all popup text fields based on the card's category and data.
    /// </summary>
    private void PopulatePopup(CardData card)
    {
        hoverCardNameText.text = card.cardName;
        hoverCategoryText.text = card.category != null ? card.category.categoryName : "Unknown";
        hoverDescriptionText.text = card.cardDescription;

        // Reset optional fields
        hoverCostText.gameObject.SetActive(false);
        hoverValueText.gameObject.SetActive(false);
        hoverConditionText.gameObject.SetActive(false);
        hoverExtraInfoText.gameObject.SetActive(false);

        if (card.category == null) return;

        switch (card.category.categoryName)
        {
            case "Seller":
                hoverCostText.gameObject.SetActive(true);
                hoverValueText.gameObject.SetActive(true);
                hoverConditionText.gameObject.SetActive(true);
                hoverCostText.text = $"Buy Cost: {card.itemBuyCost}g";
                hoverValueText.text = card.valueIsHidden ? "Value: ???" : $"Value: {card.itemTrueValue}g";
                hoverConditionText.text = $"Condition: {card.itemCondition}";
                break;

            case "Buyer":
                hoverCostText.gameObject.SetActive(true);
                hoverValueText.gameObject.SetActive(true);
                hoverCostText.text = $"Wants: {card.buyerDesiredItemType}";
                hoverValueText.text = $"Offers: {card.buyerOfferedPrice}g";
                break;

            case "Conservator":
                hoverCostText.gameObject.SetActive(true);
                hoverValueText.gameObject.SetActive(true);
                hoverExtraInfoText.gameObject.SetActive(true);
                hoverCostText.text = $"Expertise: {card.conservatorExpertise}";
                hoverValueText.text = $"Appraisal Level: +{card.appraisalLevel} Condition";
                hoverExtraInfoText.text = $"Accuracy: {Mathf.RoundToInt(card.appraisalAccuracy * 100)}%";
                break;

            case "Contractor":
                hoverCostText.gameObject.SetActive(true);
                hoverValueText.gameObject.SetActive(true);
                hoverCostText.text = $"Upgrades: {card.upgradeType}";
                hoverValueText.text = card.upgradeType == ContractorUpgradeType.UnlockCategory
                    ? $"Unlocks: {(card.categoryToUnlock != null ? card.categoryToUnlock.categoryName : "?")}"
                    : card.upgradeType == ContractorUpgradeType.HireStaff
                        ? $"Identifies: {card.staffIdentifiesItemType}"
                        : $"Amount: +{card.upgradeAmount}";
                break;

            case "Freelancer":
                hoverCostText.gameObject.SetActive(true);
                hoverValueText.gameObject.SetActive(true);
                hoverCostText.text = $"Returns in: {card.roundsToReturn} rounds";
                hoverValueText.text = $"Item value: {card.freelancerMinItemValue}-{card.freelancerMaxItemValue}g";
                break;

                // ── Add new cases here as new card categories are introduced ──
        }
    }


    /// <summary>
    /// Positions the popup at a fixed location in the bottom-left corner
    /// of the screen with a configurable padding offset.
    /// Called once when the popup is shown rather than every frame.
    /// </summary>
    private void PositionAtBottomLeft()
    {
        RectTransform rt = popupPanel.GetComponent<RectTransform>();

        // Anchor and pivot to bottom-left so position is relative to that corner
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);

        // Position with padding inset from the bottom-left corner
        rt.anchoredPosition = new Vector2(screenPadding.x, screenPadding.y);
    }
}