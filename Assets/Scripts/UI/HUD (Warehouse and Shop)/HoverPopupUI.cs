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

using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

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
    public void ShowPopup(CardData card, RectTransform sourceRect = null, bool revealValue = false, int overrideDisplayValue = -1)
    {
        PopulatePopup(card, revealValue, overrideDisplayValue);
        PositionPopupBelowRect(sourceRect);
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
    /// Gets the necessary info for the freelancer based on switch case statement for Freelancer type
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    private void GetFreelancerInfo2(CardData card, string costText, string extraInfoText, string valueText)
    {
        switch (card.freelancerType)
        {
            case FreelancerType.None: 
                costText = string.Empty;
                extraInfoText = string.Empty;
                valueText = string.Empty;
                break;
            case FreelancerType.FetchItem:
                costText = $"Cost: {card.freelancerCost}g";
                extraInfoText = $"Tenure: {card.roundsToReturn} days";
                valueText = $"Function: Fetch a random item with value equal to or under {card.freelancerMaxItemValue}g when it returns. Item will automatically be added to the warehouse.";
                break;
            case FreelancerType.AutoAppraiser:
                costText = $"Cost: {card.freelancerCost}g";
                extraInfoText = $"Tenure: {card.roundsToReturn} days";
                valueText = $"Function: Auto-appraise all {ExtensionMethods.ToTitleCase(card.conservatorExpertise)} that customers come in to sell during their tenure.";
                break;
            case FreelancerType.LoanShark:
                costText = $"Cost: None";
                extraInfoText = $"Tenure: {card.roundsToReturn} days";
                valueText = $"Function: Loans {card.loanAmount}g that needs to be repaid (without interest). Failure to do so might cost the shop its reputation.";
                break;
            case FreelancerType.TempFloorSpaceBonus:
                costText = $"Cost: {card.freelancerCost}g";
                extraInfoText = $"Tenure: {card.roundsToReturn} days";
                valueText = $"Function: Temporarily upgrade the Shop's Floor Space by {card.freelancerMeasure} to allow more customers each day.";
                break;
            case FreelancerType.TempWarehouseBonus:
                costText = $"Cost: {card.freelancerCost}g";
                extraInfoText = $"Tenure: {card.roundsToReturn} days";
                valueText = $"Function: Temporarily upgrade the Warehouse Space by {card.freelancerMeasure}. Any items left in the temporary storage at the end of the tenure will disappear.";
                break;
            case FreelancerType.TempReputationBonus:
                costText = $"Cost: {card.freelancerCost}g";
                extraInfoText = $"Tenure: {card.roundsToReturn} days";
                valueText = $"Function: Temporarily boost the shop's reputation by {card.freelancerMeasure}.";
                break;
            default:
                break;
        }
    }

    private string GetFreelancerInfo(CardData card)
    {
        switch (card.freelancerType)
        {
            case FreelancerType.None: return string.Empty;
            case FreelancerType.FetchItem: return $"Function: Fetch a random item with value under {card.freelancerMaxItemValue + 1}.";
            case FreelancerType.AutoAppraiser: return $"Function: Auto-appraise all {ExtensionMethods.ToTitleCase(card.conservatorExpertise)} on the floor";
            case FreelancerType.LoanShark: return $"Function: Loans {card.loanAmount}g without interest. Failure to repay in full costs the shop reputation.";
            case FreelancerType.TempFloorSpaceBonus: return $"Function: Temporarily upgrade the Shop's Floor Space by {card.freelancerMeasure}";
            case FreelancerType.TempWarehouseBonus: return $"Function: Temporarily upgrade the Warehouse Space by {card.freelancerMeasure}.";
            case FreelancerType.TempReputationBonus: return$"Function: Temporarily boost the shop's reputation by {card.freelancerMeasure}.";
            default: return "???";
        }
    }

    /// <summary>
    /// Fills all popup text fields based on the card's category and data.
    /// </summary>
    private void PopulatePopup(CardData card, bool revealValue = false, int overrideDisplayValue = -1)
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
                hoverDescriptionText.text = $"{ExtensionMethods.ToTitleCase(card.subCategory)}, {card.provenance}\n" +
                    $"{card.cardDescription}";
                hoverCostText.gameObject.SetActive(true);
                hoverValueText.gameObject.SetActive(true);
                hoverCostText.text = $"Selling for {card.itemBuyCost}g";

                // Use revealValue override OR the SO's own flag — whichever reveals it
                bool showValue = revealValue || !card.valueIsHidden;

                if (showValue)
                {
                    // Use the conservator-adjusted runtime value if one was passed,
                    // otherwise fall back to the SO's base true value.
                    int displayValue = overrideDisplayValue >= 0
                                       ? overrideDisplayValue
                                       : card.itemTrueValue;
                    hoverValueText.text = $"Value: {displayValue}g";
                }
                else
                {
                    hoverValueText.text = "Value: ???";
                }
                hoverCategoryBanner.color = Color.blue;
                break;

            case "Buyer":
                hoverCostText.gameObject.SetActive(true);
                if (card.buyerDesiredItemType == CardSubCategory.None) hoverCostText.text = "Looking for anything that will catch their eye.";
                else hoverCostText.text = $"Looking for {ExtensionMethods.ToTitleCase(card.buyerDesiredItemType)}";
                hoverCategoryBanner.color = Color.green;
                break;

            case "Conservator":
                hoverCostText.gameObject.SetActive(true);
                hoverExtraInfoText.gameObject.SetActive(true);
                if(card.isConservator) hoverExtraInfoText.text = $"Can appraise and raise the right item's value by {card.conservatorUpgradePercentage}%";
                else hoverExtraInfoText.text = "Can identify the right item's value accurately.";
                hoverCostText.text = $"Expertise: {ExtensionMethods.ToTitleCase(card.conservatorExpertise)}";
                hoverCategoryBanner.color = Color.magenta;
                break;

            case "Contractor":
                hoverCostText.gameObject.SetActive(true);
                hoverValueText.gameObject.SetActive(true);
                hoverExtraInfoText.gameObject.SetActive(true);
                hoverCostText.text = $"Upgrades: {card.upgradeType}";
                hoverValueText.text = card.upgradeType == ContractorUpgradeType.UnlockCategory
                    ? $"Unlocks: {(card.categoryToUnlock != null ? card.categoryToUnlock.categoryName : "?")}"
                    : card.upgradeType == ContractorUpgradeType.HireStaff
                        ? $"Identifies: {card.staffIdentifiesItemType}"
                        : $"Amount: +{card.upgradeAmount}";
                hoverExtraInfoText.text = $"Cost: {EconomyManager.Instance.GetContractorCost(card)}g";
                hoverCategoryBanner.color = Color.yellow;
                break;

            case "Freelancer":
                hoverCostText.gameObject.SetActive(true);
                hoverExtraInfoText.gameObject.SetActive(true);
                hoverValueText.gameObject.SetActive(true);
                hoverCostText.text = $"Cost: {card.freelancerCost}g";
                hoverExtraInfoText.text = $"Tenure: {card.roundsToReturn} days";
                hoverValueText.text = GetFreelancerInfo(card);
                hoverCategoryBanner.color = Color.black;
                break;

                // ── Add new cases here as new card categories are introduced ──
        }
    }


    /// <summary>
    /// Positions the popup at a fixed location in the bottom-left corner
    /// of the screen with a configurable padding offset.
    /// Called once when the popup is shown rather than every frame.
    /// </summary>
    private void PositionPopupBelowRect(RectTransform sourceRect = null)
    {
        RectTransform rt = popupPanel.GetComponent<RectTransform>();
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        if (sourceRect == null)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(screenPadding.x, screenPadding.y);
            return;
        }

        // Compute bottom-center world point of the source RectTransform
        Vector3[] corners = new Vector3[4];
        sourceRect.GetWorldCorners(corners);
        Vector3 bottomLeft = corners[0];
        Vector3 bottomRight = corners[3];
        Vector3 bottomCenterWorld = (bottomLeft + bottomRight) * 0.5f;

        // Convert to canvas-local coordinates
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            bottomCenterWorld);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out Vector2 localPoint);

        // Place the popup so its top-center sits at the bottom-center of the source,
        // offset downwards by screenPadding.y
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 1f);

        Vector2 anchoredPos = localPoint + new Vector2(0f, -screenPadding.y);

        // Clamp horizontally so popup stays on canvas
        float halfWidth = rt.rect.width * 0.5f;
        float leftLimit = canvasRect.rect.xMin + halfWidth;
        float rightLimit = canvasRect.rect.xMax - halfWidth;
        anchoredPos.x = Mathf.Clamp(anchoredPos.x, leftLimit, rightLimit);

        // Clamp vertically so popup doesn't go off the top of the canvas
        float topLimit = canvasRect.rect.yMax - 0f;
        float bottomLimit = canvasRect.rect.yMin + rt.rect.height;
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, bottomLimit, topLimit);

        rt.anchoredPosition = anchoredPos;
    }

}