using UnityEngine;

/// <summary>
/// ScriptableObject representing a single card definition.
/// Create one asset per card via Assets > Create > PawnShop > Card Data.
/// Each card belongs to one CardCategory and one sub-category string.
/// </summary>
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

    // ── SELLER specific fields (leave at default if card is not a Seller) ──
    [Header("Seller / Item Fields")]
    [Tooltip("If this card offers an item for sale, set its base buy cost here.")]
    public int itemBuyCost = 0;

    [Tooltip("The estimated resale value of the item. May be hidden until appraised.")]
    public int itemTrueValue = 0;

    [Tooltip("Whether the item's true value is hidden until a conservator/expert appraises it.")]
    public bool valueIsHidden = true;

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

    // ── CONTRACTOR fields ──
    [Header("Contractor Fields")]
    [Tooltip("Which type of upgrade this contractor applies.")]
    public ContractorUpgradeType upgradeType = ContractorUpgradeType.None;

    [Tooltip("Numeric amount for stat upgrades (e.g. +1 warehouse slot, +5 reputation).")]
    public int upgradeAmount = 0;

    [Tooltip("For UnlockCategory: drag the CardCategory asset to unlock here.")]
    public CardCategory categoryToUnlock;

    [Tooltip("For UnlockSubCategory: the sub-category string to unlock (must match subCategory field on CardData assets).")]
    public string subCategoryToUnlock;

    [Tooltip("For HireStaff: which item type this staff member can auto-identify. " +
             "Must match the subCategory field on Seller CardData assets exactly. " +
             "e.g. Antiques, Electronics, Jewellery, Art, Weapons, Collectibles, Books & Documents")]
    public string staffIdentifiesItemType;

    // ── FREELANCER fields ──
    [Header("Freelancer Fields")]
    [Tooltip("How many rounds this freelancer takes to return with an item.")]
    public int roundsToReturn = 3;

    [Tooltip("Minimum gold value of item the freelancer might acquire.")]
    public int freelancerMinItemValue = 0;

    [Tooltip("Maximum gold value of item the freelancer might acquire.")]
    public int freelancerMaxItemValue = 0;

    // ── Add new card-type fields below this line as the game grows ──
}