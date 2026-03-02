/*
 * ============================================================
 * SCRIPT:      WarehouseSlot.cs
 * GAMEOBJECT:  WarehouseSlotPrefab instance
 * ------------------------------------------------------------
 * FUNCTION:
 *   Holds direct Inspector references to the slot's child UI
 *   elements so WarehousePanelUI does not need to use
 *   Transform.Find() with fragile name strings.
 *   Populated by WarehousePanelUI.PopulateSlot().
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   WarehousePanelUI    -- reads references in PopulateSlot()
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:   None (data container only)
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No runtime methods. Pure reference container.
 * ============================================================
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WarehouseSlot : MonoBehaviour
{
    public Image artImage;
    public TextMeshProUGUI nameText;
}