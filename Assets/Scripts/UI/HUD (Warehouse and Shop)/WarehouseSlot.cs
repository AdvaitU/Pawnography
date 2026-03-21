/*
 * ============================================================
 * SCRIPT:      WarehouseSlot.cs
 * GAMEOBJECT:  WarehouseSlotPrefab instance
 * ------------------------------------------------------------
 * FUNCTION:
 *   Holds UI element references for WarehousePanelUI.
 *   Plays a DOTween entry animation when populated with an item.
 * ============================================================
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class WarehouseSlot : MonoBehaviour
{
    [Tooltip("The art image displayed in this warehouse slot.")]
    public Image artImage;

    [Tooltip("The item name label for this slot.")]
    public TextMeshProUGUI nameText;

    [Header("Entry Animation")]
    [Tooltip("Scale the slot punches to at spawn.")]
    public float entryBurstScale = 1.15f;

    [Tooltip("Duration of the entry punch in seconds.")]
    public float entryDuration = 0.3f;

    [Tooltip("Easing for the entry punch.")]
    public Ease entryEase = Ease.OutBack;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Plays a scale punch entry animation. Call after populating the slot
    /// with item data. Only fires for occupied slots — pass hasItem = false
    /// to skip animation on empty slots.
    /// </summary>
    public void PlayEntryAnimation(bool hasItem)
    {
        if (!hasItem) return;

        rectTransform.localScale = Vector3.zero;
        rectTransform.DOScale(1f, entryDuration)
                     .SetEase(entryEase);
    }
}