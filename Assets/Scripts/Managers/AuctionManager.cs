/*
 * ============================================================
 * SCRIPT:      AuctionManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Owns all auction state and logic. Calculates the final
 *   selling amount for a lot using a three-stage algorithm:
 *   Stage 1 calculates a weighted base price per item and sums
 *   them into a Lot Base Price. Stage 2 calculates a multiplier
 *   based on subCategory pair matches and shared tags across
 *   the lot. Stage 3 applies a boss round condition multiplier
 *   (currently a placeholder of 1.0). The final selling amount
 *   is rounded up to the nearest integer ending in 0 or 5.
 *   Coordinates with AuctionUI for all visual presentation.
 *   Coordinates with EconomyManager for gold transactions.
 *   Coordinates with RoundManager for round progression.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   RoundManager        -- calls BeginAuction() when an auction
 *                          round is detected
 *   AuctionUI           -- calls ConfirmLotSelection(),
 *                          GetFinalSellingAmount(), and
 *                          ResolveAuction() at appropriate
 *                          points in the UI flow
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   BeginAuction()          --> Called by RoundManager when
 *                               an auction round starts
 *   ConfirmLotSelection()   --> Called by AuctionUI when the
 *                               player confirms their lot
 *   GetFinalSellingAmount() --> Called by AuctionUI to drive
 *                               the cutscene bid sequence
 *   ResolveAuction()        --> Called by AuctionUI after the
 *                               cutscene or skip button
 *   OnRentRoundStart()      --> Subscribed to onRoundStart,
 *                               fires once on the round after
 *                               a passed auction to deduct rent
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). All logic is event-driven or called directly
 *   by AuctionUI. Calculation methods run once per auction.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AuctionManager : MonoBehaviour
{
    public static AuctionManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // SETTINGS
    // ─────────────────────────────────────────────

    [Header("Auction Settings")]
    [Tooltip("Maximum number of items the player can put into the auction lot.")]
    public int maxLotSize = 3;

    [Tooltip("Minimum number of bids in the cutscene bid sequence.")]
    public int minBids = 5;

    [Tooltip("Maximum number of bids in the cutscene bid sequence.")]
    public int maxBids = 10;

    [Tooltip("Minimum delay in seconds between bids in the cutscene.")]
    public float minBidDelay = 1f;

    [Tooltip("Maximum delay in seconds between bids in the cutscene.")]
    public float maxBidDelay = 3f;

    // ─────────────────────────────────────────────
    // RUNTIME STATE
    // ─────────────────────────────────────────────

    [Header("Runtime State — view in Play Mode")]
    [Tooltip("Items the player has selected for the current auction lot.")]
    public List<InventoryItem> currentLot = new List<InventoryItem>();

    [Tooltip("The calculated final selling amount for the current lot.")]
    public int finalSellingAmount = 0;

    [Tooltip("True if the most recent auction was passed.")]
    public bool lastAuctionPassed = false;

    [Tooltip("True if the next round start should trigger rent deduction.")]
    private bool rentPendingNextRound = false;

    // ─────────────────────────────────────────────
    // EVENTS
    // ─────────────────────────────────────────────

    [Header("Events")]
    [Tooltip("Fired when the auction round begins. AuctionUI subscribes to show the background and selection popup.")]
    public UnityEvent onAuctionBegin;

    [Tooltip("Fired when the lot is confirmed and the final selling amount is calculated. " +
             "AuctionUI subscribes to begin the cutscene.")]
    public UnityEvent onLotConfirmed;

    [Tooltip("Fired when the auction resolves. Passes true on pass, false on fail.")]
    public UnityEvent<bool> onAuctionResolved;

    // ─────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        RoundManager.Instance.onRoundStart.AddListener(OnRentRoundStart);
    }

    // ─────────────────────────────────────────────
    // METHODS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Entry point called by RoundManager when an auction round is detected.
    /// Clears any previous lot state and fires onAuctionBegin so AuctionUI
    /// can show the background change and item selection popup.
    /// </summary>
    public void BeginAuction()
    {
        currentLot.Clear();
        finalSellingAmount = 0;
        lastAuctionPassed = false;

        Debug.Log("[AuctionManager] Auction round begun.");
        onAuctionBegin?.Invoke();
    }

    /// <summary>
    /// Called by AuctionUI when the player confirms their lot selection.
    /// Removes selected items from inventory, calculates the final selling
    /// amount, and fires onLotConfirmed so AuctionUI can begin the cutscene.
    /// If the lot is empty, finalSellingAmount is 0 and the cutscene is
    /// skipped directly to resolution.
    /// </summary>
    public void ConfirmLotSelection(List<InventoryItem> selectedItems)
    {
        currentLot = new List<InventoryItem>(selectedItems);

        // Remove sold items from inventory immediately
        foreach (InventoryItem item in currentLot)
            InventoryManager.Instance.TryRemoveItem(item);

        if (currentLot.Count == 0)
        {
            finalSellingAmount = 0;
            Debug.Log("[AuctionManager] Empty lot confirmed. Final selling amount: 0.");
            ResolveAuction();
            return;
        }

        finalSellingAmount = CalculateFinalSellingAmount(currentLot);
        Debug.Log($"[AuctionManager] Lot confirmed. Final selling amount: {finalSellingAmount}g.");

        onLotConfirmed?.Invoke();
    }

    /// <summary>
    /// Returns the final selling amount. Called by AuctionUI to drive
    /// the cosmetic bid sequence — the highest bid in the cutscene should
    /// equal this value.
    /// </summary>
    public int GetFinalSellingAmount() => finalSellingAmount;

    /// <summary>
    /// Resolves the auction by comparing the final selling amount against
    /// the current threshold. On pass: adds proceeds to gold, sets
    /// rentPendingNextRound so rent is deducted at the next round start,
    /// and advances the round. On fail: triggers game over.
    /// Called by AuctionUI after the cutscene ends or skip is pressed.
    /// </summary>
    public void ResolveAuction()
    {
        bool passed = finalSellingAmount >= EconomyManager.Instance.currentThreshold;
        lastAuctionPassed = passed;

        Debug.Log($"[AuctionManager] Auction resolved. " +
                  $"Sold: {finalSellingAmount}g / " +
                  $"Threshold: {EconomyManager.Instance.currentThreshold}g. " +
                  $"Result: {(passed ? "PASS" : "FAIL")}");

        if (passed)
        {
            EconomyManager.Instance.AddAuctionProceeds(finalSellingAmount);
            rentPendingNextRound = true;
        }

        onAuctionResolved?.Invoke(passed);

        if (!passed)
            RoundManager.Instance.TriggerGameOver();
    }

    /// <summary>
    /// Subscribed to RoundManager.onRoundStart. On the first normal round
    /// after a passed auction, deducts rent and bills via EconomyManager
    /// then unsubscribes itself for that cycle.
    /// </summary>
    private void OnRentRoundStart()
    {
        if (!rentPendingNextRound) return;

        rentPendingNextRound = false;
        EconomyManager.Instance.DeductRentAndBills();
        Debug.Log("[AuctionManager] Rent and bills deducted for this cycle.");
    }

    // ─────────────────────────────────────────────
    // FINAL SELLING AMOUNT CALCULATION
    // ─────────────────────────────────────────────

    /// <summary>
    /// Master calculation method. Runs all three stages and returns
    /// the final selling amount rounded up to the nearest int ending in 0 or 5.
    /// </summary>
    private int CalculateFinalSellingAmount(List<InventoryItem> lot)
    {
        float lotBasePrice = CalculateStageOne(lot);
        float multiplier = CalculateStageTwo(lot);
        float bossCondition = CalculateStageThree();

        float raw = lotBasePrice * multiplier * bossCondition;
        return RoundToNearestFiveOrZero(raw);
    }

    /// <summary>
    /// Stage 1 — Calculates the Lot Base Price.
    /// For each item: a = base value, b = weighted random roll (0.8–2.0),
    /// c = a * b. Returns the sum of all c values.
    /// </summary>
    private float CalculateStageOne(List<InventoryItem> lot)
    {
        float lotBasePrice = 0f;

        foreach (InventoryItem item in lot)
        {
            float a = CalculateItemBaseValue(item);
            float b = RollWeightedMultiplier();
            float c = a * b;

            Debug.Log($"[AuctionManager] Stage 1 — '{item.cardName}': " +
                      $"a={a}, b={b:F2}, c={c:F2}");

            lotBasePrice += c;
        }

        Debug.Log($"[AuctionManager] Stage 1 — Lot Base Price: {lotBasePrice:F2}");
        return lotBasePrice;
    }

    /// <summary>
    /// Calculates the base value 'a' for a single item.
    /// If appraised: a = appraisedValue.
    /// If not appraised: a = lower of (buyPrice + 20%) or itemTrueValue.
    /// </summary>
    private float CalculateItemBaseValue(InventoryItem item)
    {
        if (item.isAppraised)
            return item.appraisedValue;

        float buyPlusTwenty = item.purchasePrice * 1.2f;
        float trueValue = item.sourceCard != null ? item.sourceCard.itemTrueValue : 0f;
        return Mathf.Min(buyPlusTwenty, trueValue);
    }

    /// <summary>
    /// Rolls the weighted random multiplier 'b'.
    /// 66% chance to sample uniformly from 0.8–1.2.
    /// 33% chance to sample uniformly from 1.2–2.0.
    /// </summary>
    private float RollWeightedMultiplier()
    {
        float roll = Random.value; // 0.0 to 1.0
        if (roll < 0.6667f)
            return Random.Range(0.8f, 1.2f);
        else
            return Random.Range(1.2f, 2.0f);
    }

    /// <summary>
    /// Stage 2 — Calculates the lot multiplier g = d + e + f, capped at 3.0.
    /// d = 1.0 base. e = subCategory pair bonus. f = shared tag bonus.
    /// Items with subCategory == None are excluded from both e and f.
    /// </summary>
    private float CalculateStageTwo(List<InventoryItem> lot)
    {
        float d = 1.0f;
        float e = CalculateSubCategoryBonus(lot);
        float f = CalculateTagBonus(lot);
        float g = Mathf.Min(d + e + f, 3.0f);

        Debug.Log($"[AuctionManager] Stage 2 — d={d}, e={e:F2}, f={f:F2}, g={g:F2}");
        return g;
    }

    /// <summary>
    /// Calculates 'e' — 0.2f per matching subCategory pair across the lot.
    /// Uses combinatorial pair counting: for n items sharing a subCategory,
    /// adds n*(n-1)/2 pairs * 0.2f.
    /// Items with subCategory == None are excluded.
    /// </summary>
    private float CalculateSubCategoryBonus(List<InventoryItem> lot)
    {
        // Count how many items belong to each subCategory
        Dictionary<CardSubCategory, int> categoryCounts =
            new Dictionary<CardSubCategory, int>();

        foreach (InventoryItem item in lot)
        {
            if (item.sourceCard == null) continue;
            if (item.sourceCard.subCategory == CardSubCategory.None) continue;

            CardSubCategory sub = item.sourceCard.subCategory;
            if (!categoryCounts.ContainsKey(sub))
                categoryCounts[sub] = 0;
            categoryCounts[sub]++;
        }

        // For each group of n items sharing a subCategory,
        // number of pairs = n * (n - 1) / 2
        float e = 0f;
        foreach (int count in categoryCounts.Values)
        {
            int pairs = count * (count - 1) / 2;
            e += pairs * 0.2f;
        }

        return e;
    }

    /// <summary>
    /// Calculates 'f' — 0.1f per unique tag that appears in 2 or more items
    /// in the lot. Each qualifying tag contributes exactly 0.1f once.
    /// Items with subCategory == None are excluded from tag matching.
    /// </summary>
    private float CalculateTagBonus(List<InventoryItem> lot)
    {
        Dictionary<string, int> tagCounts = new Dictionary<string, int>();

        foreach (InventoryItem item in lot)
        {
            if (item.sourceCard == null) continue;
            if (item.sourceCard.subCategory == CardSubCategory.None) continue;
            if (item.sourceCard.tags == null) continue;

            // Use a HashSet to avoid counting the same tag twice
            // for one item that has duplicate tag entries
            HashSet<string> seenForThisItem = new HashSet<string>();

            foreach (string tag in item.sourceCard.tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                string normalisedTag = tag.Trim().ToLower();

                if (seenForThisItem.Contains(normalisedTag)) continue;
                seenForThisItem.Add(normalisedTag);

                if (!tagCounts.ContainsKey(normalisedTag))
                    tagCounts[normalisedTag] = 0;
                tagCounts[normalisedTag]++;
            }
        }

        float f = 0f;
        foreach (int count in tagCounts.Values)
        {
            if (count >= 2)
                f += 0.1f;
        }

        return f;
    }

    /// <summary>
    /// Stage 3 — Applies the boss round condition multiplier 'h'.
    /// Currently a placeholder returning 1.0f.
    /// Replace with condition-specific logic when boss conditions are designed.
    /// </summary>
    private float CalculateStageThree()
    {
        // ── Add boss round condition logic here in future ──
        return 1.0f;
    }

    /// <summary>
    /// Rounds a float up to the nearest integer ending in 0 or 5.
    /// E.g. 347 → 350, 351 → 355, 343 → 345.
    /// </summary>
    private int RoundToNearestFiveOrZero(float value)
    {
        int ceiled = Mathf.CeilToInt(value);
        int remainder = ceiled % 5;
        if (remainder == 0) return ceiled;
        return ceiled + (5 - remainder);
    }
}