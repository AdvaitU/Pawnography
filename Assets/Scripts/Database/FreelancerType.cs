/*
 * ============================================================
 * SCRIPT:      FreelancerType.cs
 * GAMEOBJECT:  Not present on any GameObject.
 *              Enum used by CardData and FreelancerManager.
 * ------------------------------------------------------------
 * FUNCTION:
 *   Defines all freelancer behaviour types.
 *   The enum value implies the execution mode:
 *     One-shot on expiry:  FetchItem, LoanShark
 *     Per-round passive:   TempFloorSpaceBonus,
 *                          TempWarehouseBonus,
 *                          TempReputationBonus
 *     Per-round active:    AutoAppraiser
 * ============================================================
 */

public enum FreelancerType
{
    None,                   // Default
    FetchItem,              // Fetches item over n turns - Uses min and max defined in CardData
    LoanShark,              // Adds gold and takes it back with interest after n rounds
    AutoAppraiser,          // Automatically reveals value of all items of subCategory that show up in the shop floor
    TempFloorSpaceBonus,    // Increases number of customers per day
    TempWarehouseBonus,     // Increases amount of items that can be stored in the warehouse
    TempReputationBonus     // Increases reputation
}