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

    [Header("Runtime Stats")]
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

    // ========================================================================================================
    // METHODS

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


        foreach (InventoryItem item in currentLot)                
            InventoryManager.Instance.TryRemoveItem(item);         // Remove sold items from inventory immediately

        if (currentLot.Count == 0)      // If no items are selected, call ResolveAuction() and exit with finalSellingAmount = 0
        {
            finalSellingAmount = 0;
            Debug.Log("[AuctionManager] Empty lot confirmed. Final selling amount: 0.");
            ResolveAuction();
            return;
        }

        finalSellingAmount = CalculateFinalSellingAmount(currentLot);    // Call the CalculateFinalSellingAmount() method
        Debug.Log($"[AuctionManager] Lot confirmed. Final selling amount: {finalSellingAmount}g.");

        onLotConfirmed?.Invoke();
    }

    /// <summary>
    /// Returns the final selling amount. Called by AuctionUI to drive
    /// the cosmetic bid sequence — the highest bid in the cutscene should
    /// equal this value.
    /// </summary>
    public int GetFinalSellingAmount() => finalSellingAmount;   // Simple getter for AuctionUI

    /// <summary>
    /// Resolves the auction by comparing the final selling amount against
    /// the current threshold. On pass: adds proceeds to gold, sets
    /// rentPendingNextRound so rent is deducted at the next round start,
    /// and advances the round. On fail: triggers game over.
    /// Called by AuctionUI after the cutscene ends or skip is pressed.
    /// </summary>
    public void ResolveAuction()
    {
        // An auction passes if the final selling amount plus current gold meets or exceeds the threshold.
        bool passed = (finalSellingAmount + EconomyManager.Instance.currentGold) >= EconomyManager.Instance.currentThreshold;
        lastAuctionPassed = passed;  // Set flag to passed

        Debug.Log($"[AuctionManager] Auction resolved. " +
                  $"Sold for {finalSellingAmount}g " +
                  $"against a threshold of {EconomyManager.Instance.currentThreshold}g.\n " +
                  $"Result: {(passed ? "PASS" : "FAIL")}");

        if (passed)
        {
            EconomyManager.Instance.AddAuctionProceeds(finalSellingAmount);   // Adds the final selling amount to the player's gold immediately on pass
            rentPendingNextRound = true;   // Sets true so gold will be removed at the beginning of the next round
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
        if (!rentPendingNextRound) return;  // Safeguard

        rentPendingNextRound = false;   // Sets boolean flag to false so this is not repeated in the next i.e. 9th round.
        EconomyManager.Instance.DeductRentAndBills();
        Debug.Log("[AuctionManager] Rent and bills deducted for this cycle.");
    }

    // =========================================================================================================
    // FINAL SELLING AMOUNT CALCULATION ------------------------------------------------------------------------

    /// <summary>
    /// Master calculation method. Runs all three stages and returns
    /// the final selling amount rounded up to the nearest int ending in 0 or 5.
    /// </summary>
    private int CalculateFinalSellingAmount(List<InventoryItem> lot)  // pass a full lot to the method
    {
        float lotBasePrice = CalculateStageOne(lot);      // Stage 1
        float multiplier = CalculateStageTwo(lot);        // Stage 2
        float bossCondition = CalculateStageThree();      // Stage 3

        float raw = lotBasePrice * multiplier * bossCondition;  // Stage 4 - Multiplying and rounding up to nearest multiple of 5.
        return RoundToNearestMultipleOfFive(raw);
    }

    /// <summary>
    /// Stage 1 — Calculates the Lot Base Price.
    /// For each item: a = base value, b = weighted random roll (0.8 – 2.0),
    /// c = a * b. Returns the sum of all c values.
    /// </summary>
    private float CalculateStageOne(List<InventoryItem> lot)   // STAGE 1 ========================================
    {
        float lotBasePrice = 0f;

        foreach (InventoryItem item in lot)                // For each item in the lot
        {
            float a = CalculateItemBaseValue(item);        // Step 1
            float b = RollWeightedMultiplier();            // Step 2
            float c = a * b;                               // Step 3

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
    private float CalculateItemBaseValue(InventoryItem item) // STAGE 1, STEP 1 --------------------------------
    {
        if (item.isAppraised)            // No calculations or rolls if item is already appraised.
            return item.appraisedValue;

        float buyPlusTwenty = item.purchasePrice * 1.2f;       // Standard base for unappraised items with 20% profit margin added (so the actual least they can get for it based on the roll is the buy price)
        float trueValue = item.sourceCard != null ? item.sourceCard.itemTrueValue : 0f;
        return Mathf.Min(buyPlusTwenty, trueValue);   // Return the lower of the two values to prevent unappraised items from being worth more than their true value, while still giving a profit margin on the buy price if the true value is very high.
    }

    /// <summary>
    /// Rolls the weighted random multiplier 'b'.
    /// 66% chance to sample uniformly from 0.8–1.2.
    /// 33% chance to sample uniformly from 1.2–2.0.
    /// </summary>
    private float RollWeightedMultiplier()                  // STAGE 1, STEP 2 --------------------------------
    {
        float roll = Random.value; // From 0.0 to 1.0
        if (roll < 0.01f)                         // 1% chance for item to go "unsold" and contribute 0 to the lot price
            return 0.0f;
        else if (roll < 0.6667f)                      // 66% change to
            return Random.Range(0.8f, 1.2f);     // Roll a value between 80% and 120% of the base price
        else                                     // Or a 33% chance to
            return Random.Range(1.2f, 2.0f);     // Roll a value between 120% and 200% of the base price
    }

    /// <summary>
    /// Stage 2 — Calculates the lot multiplier g = d + e + f, capped at 3.0.
    /// d = 1.0 base. e = subCategory pair bonus. f = shared tag bonus.
    /// Items with subCategory == None are excluded from both e and f.
    /// </summary>
    private float CalculateStageTwo(List<InventoryItem> lot) // STAGE 2 ========================================
    {
        float d = 1.0f;
        float e = CalculateSubCategoryBonus(lot);     // Step 1
        float f = CalculateTagBonus(lot);             // Step 2 
        float g = Mathf.Min(d + e + f, 3.0f);         // Step 3 - Cap it at 3.0f

        Debug.Log($"[AuctionManager] Stage 2 — d={d}, e={e:F2}, f={f:F2}, g={g:F2}");
        return g;
    }

    /// <summary>
    /// Calculates 'e' — per matching subCategory pair across the lot.
    /// For each pair sharing a subCategory: adds 0.4f if Provenance also matches,
    /// 0.2f if Provenance does not match or is unset.
    /// Uses explicit pair iteration (i, j) to allow per-pair Provenance comparison.
    /// Items with subCategory == None are excluded.
    /// </summary>
    private float CalculateSubCategoryBonus(List<InventoryItem> lot) // STAGE 2, STEP 1 --------------------------------
    {
        // Filter out items with no subCategory before iterating pairs
        // Safeguard against null sourceCard references at the same time
        List<InventoryItem> eligibleItems = new List<InventoryItem>();
        foreach (InventoryItem item in lot)
        {
            if (item.sourceCard == null) continue;                              // Safeguard against null reference
            if (item.sourceCard.subCategory == CardSubCategory.None) continue;  // Same as above
            eligibleItems.Add(item);
        }

        float e = 0f;

        // Explicit pair iteration — compare every unique pair (i, j) where i < j
        // to avoid counting the same pair twice e.g. (0,1) and (1,0)
        for (int i = 0; i < eligibleItems.Count; i++)
        {
            for (int j = i + 1; j < eligibleItems.Count; j++)
            {
                InventoryItem itemA = eligibleItems[i];
                InventoryItem itemB = eligibleItems[j];

                // Only proceed if subCategories match
                if (itemA.sourceCard.subCategory != itemB.sourceCard.subCategory) continue;

                // Check if Provenance also matches — both must be non-empty to qualify
                string provA = itemA.sourceCard.provenance;
                string provB = itemB.sourceCard.provenance;

                bool provenanceMatches = !string.IsNullOrWhiteSpace(provA) &&
                                         !string.IsNullOrWhiteSpace(provB) &&
                                         provA.Trim().ToLower() == provB.Trim().ToLower();

                if (provenanceMatches)
                    e += 0.4f;  // SubCategory AND Provenance match
                else
                    e += 0.2f;  // SubCategory match only
            }
        }

        return e;
    }

    /// <summary>
    /// Calculates 'f' — 0.1f per unique tag that appears in 2 or more items
    /// in the lot. Each qualifying tag contributes exactly 0.1f once.
    /// Items with subCategory == None are excluded from tag matching.
    /// </summary>
    private float CalculateTagBonus(List<InventoryItem> lot)         // STAGE 2, STEP 2 --------------------------------
    {
        Dictionary<string, int> tagCounts = new Dictionary<string, int>();  // Single dictionary that holds all unique tags as keys and counts as values.

        foreach (InventoryItem item in lot)
        {
            if (item.sourceCard == null) continue;                               // Safeguards
            if (item.sourceCard.subCategory == CardSubCategory.None) continue;
            if (item.sourceCard.tags == null) continue;

            // Use a HashSet to avoid counting the same tag twice
            // for one item that has duplicate tag entries
            HashSet<string> seenForThisItem = new HashSet<string>();

            foreach (string tag in item.sourceCard.tags)           // For each item, add unique tags as keys and increment counts of tags seen before.
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;    // Safeguard against empty or whitespace tags
                string normalisedTag = tag.Trim().ToLower();     // Remove leading/trailing whitespace and convert to lowercase

                if (seenForThisItem.Contains(normalisedTag)) continue;
                seenForThisItem.Add(normalisedTag);

                if (!tagCounts.ContainsKey(normalisedTag))
                    tagCounts[normalisedTag] = 0;          // If the tag is not yet in the dictionary, add it with a starting count of 0
                tagCounts[normalisedTag]++;                // Increment the count for that tag
            }
        }

        float f = 0f;
        foreach (int count in tagCounts.Values)       // For each unique tag, if it appears in 2 or more items, add 0.1f to 'f' once.
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
    private float CalculateStageThree()       // STAGE 3 ======================================== PLACEHOLDER ====================
    {
        // ── Add boss round condition logic here in future ──
        return 1.0f;
    }

    /// <summary>
    /// Rounds a float up to the nearest integer ending in 0 or 5.
    /// E.g. 347 → 350, 351 → 355, 343 → 345.
    /// </summary>
    private int RoundToNearestMultipleOfFive(float value) // STAGE 4 — FINAL ROUNDING ========================================
    {
        int ceiled = Mathf.CeilToInt(value);         // "Round up to nearest integer if float
        int remainder = ceiled % 5;                  // Divide by 5 and get remainder to nearest multiple of 5    
        if (remainder == 0) return ceiled;           // If 0, number is already good. Return it
        return ceiled + (5 - remainder);             // If not, add the difference to get to the nearest multiple of 5 above the number.
    }
}