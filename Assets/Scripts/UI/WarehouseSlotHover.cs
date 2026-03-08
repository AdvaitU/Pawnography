/*
 * ============================================================
 * SCRIPT:      WarehouseSlotHover.cs
 * GAMEOBJECT:  WarehouseSlotPrefab instance
 * ------------------------------------------------------------
 * FUNCTION:
 *   Detects pointer enter and exit on a warehouse slot.
 *   After the same configurable delay as CardHoverHandler,
 *   shows the HoverPopupUI with the slot's item data.
 *   Empty slots do not trigger the popup.
 *   SetItemData() is called by WarehousePanelUI when
 *   populating each slot.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   WarehousePanelUI    -- calls SetItemData() when populating
 *                          each slot during RefreshWarehouse()
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   SetItemData()       --> Called by WarehousePanelUI.PopulateSlot()
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). Uses a single Coroutine for the popup delay,
 *   cancelled immediately on pointer exit.
 * ============================================================
 */

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class WarehouseSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Delay in seconds before the hover popup appears. " +
             "Should match CardHoverHandler.hoverDelay for consistency.")]
    public float hoverDelay = 0.5f;

    // The inventory item this slot is currently displaying.
    // Null if the slot is empty � no popup shown for empty slots.
    private InventoryItem currentItem;
    private Coroutine hoverCoroutine;

    /// <summary>
    /// Called by WarehousePanelUI.PopulateSlot() to assign the
    /// inventory item this slot displays. Pass null for empty slots.
    /// </summary>
    public void SetItemData(InventoryItem item)
    {
        currentItem = item;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Don't show popup for empty slots
        if (currentItem == null || currentItem.sourceCard == null) return;

        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);

        hoverCoroutine = StartCoroutine(ShowPopupAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        HoverPopupUI.Instance.HidePopup();
    }

    private IEnumerator ShowPopupAfterDelay()
    {
        yield return new WaitForSeconds(hoverDelay);

        bool revealValue = currentItem.valueIsRevealed || currentItem.isAppraised;
        int overrideValue = currentItem.isAppraised ? currentItem.appraisedValue : -1;

        HoverPopupUI.Instance.ShowPopup(
            currentItem.sourceCard,
            GetComponent<RectTransform>(),
            revealValue,
            overrideValue);
    }
}
