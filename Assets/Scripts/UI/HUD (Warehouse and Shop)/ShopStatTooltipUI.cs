/*
 * ============================================================
 * SCRIPT:      ShopStatTooltipUI.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Controls the tooltip popup that appears when the player
 *   hovers over a stat element in the Shop Stats panel.
 *   Receives a title and description string from
 *   ShopStatTooltipTrigger and populates the popup panel.
 *
 *   Positioning mirrors HoverPopupUI: the popup anchors below
 *   the hovered element using the same canvas-space calculation,
 *   clamped to stay within the canvas bounds.
 *
 *   ShowTooltip() and HideTooltip() are the two entry points.
 *   Add animation logic inside those methods when ready without
 *   needing to touch ShopStatTooltipTrigger.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   ShopStatTooltipTrigger  -- calls ShowTooltip() and
 *                              HideTooltip() on pointer events
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   ShowTooltip()   --> ShopStatTooltipTrigger after hover delay
 *   HideTooltip()   --> ShopStatTooltipTrigger on pointer exit
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). Positioned once on show, not every frame.
 * ============================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopStatTooltipUI : MonoBehaviour
{
    public static ShopStatTooltipUI Instance { get; private set; }

    [Header("Panel Reference")]
    public GameObject tooltipPanel;

    [Header("UI Fields — assign in Inspector")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Positioning")]
    [Tooltip("Reference to the Canvas for screen-space calculations.")]
    public Canvas parentCanvas;

    [Tooltip("Vertical gap in pixels between the hovered element and the tooltip.")]
    public float verticalOffset = 8f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// Shows the tooltip populated with the given title and description,
    /// positioned below the source RectTransform.
    /// Add animation logic here when ready.
    /// </summary>
    public void ShowTooltip(string title, string description, RectTransform sourceRect)
    {
        titleText.text = title;
        descriptionText.text = description;

        PositionBelowRect(sourceRect);
        tooltipPanel.SetActive(true);
    }

    /// <summary>
    /// Hides the tooltip.
    /// Add animation logic here when ready.
    /// </summary>
    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    // ── Positioning ──────────────────────────────────────────

    private void PositionBelowRect(RectTransform sourceRect)
    {
        RectTransform tooltipRT = tooltipPanel.GetComponent<RectTransform>();
        RectTransform canvasRT = parentCanvas.GetComponent<RectTransform>();

        // Get the bottom-centre of the source element in screen space
        Vector3[] corners = new Vector3[4];
        sourceRect.GetWorldCorners(corners);
        Vector3 bottomCenterWorld = (corners[0] + corners[3]) * 0.5f;

        Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : parentCanvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, bottomCenterWorld);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPoint, cam, out Vector2 localPoint);

        // Anchor tooltip top-centre to that point, offset downward
        tooltipRT.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRT.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRT.pivot = new Vector2(0.5f, 1f);

        Vector2 anchoredPos = localPoint + new Vector2(0f, -verticalOffset);

        // Clamp horizontally so tooltip stays inside the canvas
        float halfWidth = tooltipRT.rect.width * 0.5f;
        float leftLimit = canvasRT.rect.xMin + halfWidth;
        float rightLimit = canvasRT.rect.xMax - halfWidth;
        anchoredPos.x = Mathf.Clamp(anchoredPos.x, leftLimit, rightLimit);

        // Clamp vertically so tooltip doesn't exceed canvas bounds
        float topLimit = canvasRT.rect.yMax;
        float bottomLimit = canvasRT.rect.yMin + tooltipRT.rect.height;
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, bottomLimit, topLimit);

        tooltipRT.anchoredPosition = anchoredPos;
    }

    //List<ActiveFreelancer> active = FreelancerManager.Instance.activeFreelancers;
    //System.Text.StringBuilder sb = new System.Text.StringBuilder();
    //sb.AppendLine($"{active.Count}");
    //foreach (ActiveFreelancer f in active)
    //sb.AppendLine($"  • {f.cardName} — {f.roundsRemaining} round(s)");
}