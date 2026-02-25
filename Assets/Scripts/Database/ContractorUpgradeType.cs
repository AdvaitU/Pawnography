/// <summary>
/// All possible upgrade types a Contractor card can apply.
/// Add new entries here as new contractor types are introduced.
/// When adding a new type, also add a corresponding case in:
///   - ShopManager.ApplyUpgrade()
///   - CardInteractionManager.ApplyContractorUpgrade()
/// </summary>
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