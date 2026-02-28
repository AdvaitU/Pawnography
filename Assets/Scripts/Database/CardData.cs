using UnityEngine;

/*
 * ============================================================
 * SCRIPT:      CardData.cs
 * GAMEOBJECT:  Not present on any GameObject.
 *              Exists as a ScriptableObject asset in
 *              Assets/ScriptableObjects/Cards/
 * ------------------------------------------------------------
 * FUNCTION:
 *   Defines all data for a single card — identity, category,
 *   spawn settings, and type-specific fields for Seller, Buyer,
 *   Conservator, Contractor, and Freelancer cards. One asset
 *   per card. All card-specific parameters other systems need
 *   are read directly from this asset.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardDatabase        -- stored in allCards list, read during
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
    [Header("Identity")]
    public string cardName;
    [TextArea] public string cardDescription;
    public Sprite cardArt;

    [Header("Category")]
    [Tooltip("The top-level category this card belongs to.")]
    public CardCategory category;

    [Tooltip("Sub-category label, e.g. 'Antique Seller', 'Electronics Buyer'. " +
             "Used for grouping within a category.")]
    public string subCategory;

    [Header("Spawning")]
    [Tooltip("If false, this specific card will never spawn, regardless of category settings.")]
    public bool canSpawn = true;

    [Tooltip("Relative weight within its category for spawn selection.")]
    [Range(0f, 100f)] public float spawnWeight = 10f;

    [Header("Tracking (Runtime)")]
    public int timesSpawnedThisRun = 0;

    // ── SELLER specific fields ──
    [Header("Seller / Item Fields")]
    [Tooltip("If this card offers an item for sale, set its base buy cost here.")]
    public int itemBuyCost = 0;
    [Tooltip("The estimated resale value of the item. May be hidden until appraised.")]
    public int itemTrueValue = 0;
    [Tooltip("Whether the item's true value is hidden until a conservator/expert appraises it.")]
    public bool valueIsHidden = true;

    [Tooltip("Condition of the item on a scale of 0-100. " +
             "Affects resale value and can be improved by Conservators.")]
    [Range(0, 100)] public int itemCondition = 50;


    // ── BUYER specific fields ──
    [Header("Buyer Fields")]
    [Tooltip("What sub-category of item this buyer is looking for.")]
    public string buyerDesiredItemType;

    [Tooltip("How much this buyer will pay for the right item.")]
    public int buyerOfferedPrice = 0;

    // ── CONSERVATOR / EXPERT fields ──
    [Header("Conservator / Expert Fields")]
    [Tooltip("Accuracy of this expert's appraisal as a 0-1 multiplier (1 = perfect).")]
    [Range(0f, 1f)] public float appraisalAccuracy = 1f;

    [Tooltip("The item sub-category this conservator specialises in " +
             "(e.g. Antiques, Jewellery). Full condition bonus applied " +
             "to matching items, reduced bonus for non-matching.")]
    public string conservatorExpertise;

    [Tooltip("How much this conservator raises an item's condition " +
             "when the item matches their expertise sub-category.")]
    public int appraisalLevel = 10;

    [Tooltip("Multiplier applied to appraisalLevel when the item does NOT " +
             "match this conservator's expertise. E.g. 0.7 = 70% of full bonus.")]
    [Range(0f, 1f)] public float nonExpertiseMultiplier = 0.7f;

    [Header("Contractor Fields")]
    public ContractorUpgradeType upgradeType = ContractorUpgradeType.None;
    public int upgradeAmount = 0;
    public CardCategory categoryToUnlock;
    public string subCategoryToUnlock;
    public string staffIdentifiesItemType;

    [Tooltip("Gold cost to hire this contractor. If 0, cost is auto-calculated " +
             "from upgradeAmount by EconomyManager.")]
    public int contractorCost = 0;

    [Header("Freelancer Fields")]
    public int roundsToReturn = 3;
    public int freelancerMinItemValue = 0;
    public int freelancerMaxItemValue = 0;

    [Tooltip("Gold cost to send this freelancer out. If 0, cost is auto-calculated " +
             "as a percentage of their average item value by EconomyManager.")]
    public int freelancerCost = 0;

    // ── Add new card-type fields below this line as the game grows ──
}