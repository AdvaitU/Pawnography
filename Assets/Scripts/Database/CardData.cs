using UnityEngine;

/*
 * ============================================================
 * SCRIPT:      CardData.cs
 * GAMEOBJECT:  Not present on any GameObject.
 *              Exists as a ScriptableObject asset in
 *              Assets/ScriptableObjects/Cards/
 * ------------------------------------------------------------
 * FUNCTION:
 *   Defines all the data for Card types
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardData            -- stored in allCards list, read during
 *                          spawn selection
 *   CardUI              -- reads display fields in Populate()
 *                          and PopulateHover()
 *   CardInteractionManager -- reads type-specific fields in
 *                          each handler
 *   InventoryManager    -- passed into InventoryItem constructor
 *                          in TryAddItem()
 *   FreelancerManager   -- reads roundsToReturn, min/maxItemValue
 *   ShopManager         -- reads upgradeType and related fields
 *   PopupManager        -- reads display fields for popup body
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:   None (data container only)
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No runtime methods. Pure data asset.
 * ============================================================
 */

[CreateAssetMenu(fileName = "NewCard", menuName = "PawnShop/Card Data")]
public class CardData : ScriptableObject
{
    // GENERAL SCOPE  MEMBERS ==============================================================================================================

    [Header("General Scope")]
    public string cardName;
    [TextArea] public string cardDescription;
    public Sprite cardArt;

    [Header("Category")]
    [Tooltip("The top-level category this card belongs to.")]
    public CardCategory category;

    [Tooltip("Sub-category label for Item based on CardSubCategory")]
    public CardSubCategory subCategory;

    [Header("Spawning")]
    [Tooltip("Can be set false to lock card spawning, and true to unlock it")]
    public bool canSpawn = true;

    [Tooltip("Percentage-ish relative weight within its category for spawn selection. Does not need to add up to 100")]
    [Range(0f, 100f)] public float spawnWeight = 10f;

    [Header("Tracking (Runtime)")]
    public int timesSpawnedThisRun = 0;

    // CATEGORY SPECIFIC MEMBERS ==============================================================================================================


    // ── SELLER specific fields -------------------------------------------------------------------
    // Cards that offer items to buy in exchange for money instantly
    [Header("Seller/Item Card Fields")]
    [Tooltip("What it costs to buy the item when it is presented")]
    public int itemBuyCost = 0;
    [Tooltip("What is the true value of an item - can be higher or lower or the same as buy cost")]
    public int itemTrueValue = 0;
    [Tooltip("Should the value be hidden when the card is presented as a Seller card")]
    public bool valueIsHidden = true;

    [Tooltip("Condition of the item on a scale of 0-100. " +
             "Affects resale value and can be improved by Conservators.")]
    [Range(0, 100)] public int itemCondition = 50;


    // ── BUYER specific fields -------------------------------------------------------------------
    // Cards that offer money in exchange for items held in the warehouse instantly
    [Header("Buyer Card Fields")]
    [Tooltip("What sub-category of item this buyer is looking for.")]
    public CardSubCategory buyerDesiredItemType;

    [Tooltip("How much this buyer will pay for the right item.")]
    public int buyerOfferedPrice = 0;   // Deprecated - but kept in to not break functionality - You know how it is :(

    [Tooltip("Percentage increase for the right category of item")]
    [Range(0, 100)] public int buyerInterestPercentage = 30;           // Buyer will offer 30% more (i.e. 130% of) the price quoted

    [Tooltip("Percentage decrease for wrong category of item")]
    [Range(0, 100)] public int buyerDisinterestPercentage = 60;        // Buyer will offer 60% less (i.e. 40%) of the price quoted


    // ── CONSERVATOR / EXPERT fields ----------------------------------------------------------------
    // Cards that identify or upgrade the value of items held in the warehouse
    [Header("Conservator/Expert Card Fields")]

    [Tooltip("Is this a Conservator or just an Appraiser? True = Both, False = Just Appraisal")]
    public bool isConservator = false;

    [Tooltip("The item sub-category this conservator specialises in " +
             "(e.g. Antiques, Jewellery). Full condition bonus applied " +
             "to matching items, reduced bonus for non-matching.")]
    public CardSubCategory conservatorExpertise;

    [Tooltip("How much this conservator raises an item's condition " +
             "when the item matches their expertise sub-category.")]
    [Range(0, 200)] public int conservatorUpgradePercentage = 10;

    [Tooltip("Multiplier applied to appraisalLevel when the item does NOT " +
             "match this conservator's expertise.")]
    [Range(0f, 1f)] public float nonExpertiseMultiplier = 0.7f;      //0.7 = 70% of full bonus.


    // ── CONTRACTOR fields -------------------------------------------------------------------
    // Cards that upgrade various aspects of the shop
    [Header("Contractor Card Fields")]
    public ContractorUpgradeType upgradeType = ContractorUpgradeType.None;   // Uses enum defined in ContractorUpgradeType.cs
    public int upgradeAmount = 0;
    public CardSubCategory staffIdentifiesItemType;

    // Unlocker type contractor
    public CardCategory categoryToUnlock;     // If category needs to be unlocked via contractor card
    public CardSubCategory subCategoryToUnlock;        // Same for sub-category

    [Tooltip("Set true and then set the contractor cost below if fixed cost. Otherwise" +
        "the cost is auto-calculated based on the contractor type and upgrade amount")]
    public bool fixedCost = false;
    [Tooltip("Set fixed cost here.")]
    public int contractorCost = 0;



    // ── FREELANCER fields -------------------------------------------------------------------
    // Cards that can be hired to perform an action over n turns
    [Header("Freelancer Fields")]
    public int roundsToReturn = 3;
    public int freelancerMinItemValue = 0;          // Fields used to randomise item returned by FreelancerManager
    public int freelancerMaxItemValue = 0;

    [Tooltip("Use to override and set fixed cost. If 0, cost is auto-calculated " +
             "as a percentage of their average item value by EconomyManager.")]
    public int freelancerCost = 0;


    // ── Add new card-type fields below this line as the game grows --------------------------




}