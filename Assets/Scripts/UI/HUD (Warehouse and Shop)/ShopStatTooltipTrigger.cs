/*
 * ============================================================
 * SCRIPT:      ShopStatTooltipTrigger.cs
 * GAMEOBJECT:  Each hoverable stat element inside ShopPanel
 * ------------------------------------------------------------
 * FUNCTION:
 *   Detects pointer enter and exit on a Shop Stats panel
 *   element. After a short configurable delay, shows the
 *   ShopStatTooltipUI popup with the assigned title and
 *   description for that stat.
 *
 *   Place this component on each TextMeshPro or container
 *   GameObject inside ShopPanel that should have a tooltip.
 *   Set tooltipTitle and tooltipDescription in the Inspector.
 *
 *   The GameObject must have a RectTransform (true for all
 *   UI elements) — the tooltip is positioned relative to it.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   ShopStatTooltipUI   -- called via ShowTooltip() /
 *                          HideTooltip()
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). Single coroutine for the hover delay,
 *   cancelled immediately on pointer exit.
 * ============================================================
 */

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShopStatTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip Content")]
    [Tooltip("Short label shown as the tooltip header. " +
             "Keep it to one or two words matching the stat name.")]
    public string tooltipTitle = "Stat Name";

    [Tooltip("Longer explanation shown in the tooltip body. " +
             "Use placeholder text for now — replace with final copy later.")]
    [TextArea(2, 5)]
    public string tooltipDescription = "Placeholder description for this stat.";

    [Header("Timing")]
    [Tooltip("Delay in seconds before the tooltip appears after hover. " +
             "Match hoverDelay on CardHoverHandler and WarehouseSlotHover " +
             "for consistency across the UI.")]
    public float hoverDelay = 0.1f;

    private Coroutine hoverCoroutine;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);

        hoverCoroutine = StartCoroutine(ShowAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        ShopStatTooltipUI.Instance.HideTooltip();
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(hoverDelay);

        ShopStatTooltipUI.Instance.ShowTooltip(
            tooltipTitle,
            tooltipDescription,
            GetComponent<RectTransform>());
    }
}