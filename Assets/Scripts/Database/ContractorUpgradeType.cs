/*
 * ============================================================
 * SCRIPT:      ContractorUpgradeType.cs
 * GAMEOBJECT:  Not present on any GameObject.
 * ------------------------------------------------------------
 * FUNCTION:
 *   Defines the enum used to demarcate contractor type.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   ShopManager         -- Used in ApplyUpgrade() in a switch 
 *                          statement
 *   CardInteractionManager -- In ApplyContractorUpgrade() that 
 *                             calls the ShopManager to apply 
 *                             upgrade
 *  CardData             -- To set upgrade type for Contractor 
 *                          cards
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:   None (data container only)
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No runtime methods. Pure data asset.
 * ============================================================
 */

public enum ContractorUpgradeType
{
    None,               // Default / unassigned
    WarehouseSlots,     // Increases inventory capacity
    Reputation,         // Increases shop reputation (affects seller quality)
    FloorSpace,         // Increases cards shown per round
    UnlockCategory,     // Unlocks a CardCategory so it can start spawning
    UnlockSubCategory,  // Unlocks a specific sub-category within a category
    HireStaff,          // Hires a permanent staff member who auto-identifies item types
}