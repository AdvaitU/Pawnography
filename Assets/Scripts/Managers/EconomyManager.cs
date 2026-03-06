/*
 * ============================================================
 * SCRIPT:      EconomyManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Tracks the player's gold balance and total income earned
 *   this run. Handles all transactions — purchases, sales,
 *   and service costs. Enforces affordability checks and
 *   manages the grace round system when the player cannot
 *   afford a card. Calculates auto-costs for contractor and
 *   freelancer cards based on their stats when no manual
 *   cost is set. Exposes the boss round income threshold
 *   and escalation logic.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardInteractionManager -- calls TrySpendGold(), AddGold(),
 *                          CanAfford(), GetContractorCost(),
 *                          GetFreelancerCost()
 *   RoundManager           -- calls CheckBossRoundThreshold()
 *                          when a boss round ends
 *   CardUI / HoverPopupUI  -- calls GetContractorCost() and
 *                          GetFreelancerCost() for display
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   TrySpendGold()         --> Called by CardInteractionManager
 *                            for purchases and service costs
 *   AddGold()              --> Called by CardInteractionManager
 *                            when a sale completes
 *   CanAfford()            --> Called by CardInteractionManager
 *                            before executing a card effect
 *   GetContractorCost()    --> Called by CardInteractionManager,
 *                            CardUI, HoverPopupUI
 *   GetFreelancerCost()    --> Called by CardInteractionManager,
 *                            CardUI, HoverPopupUI
 *   CheckBossRoundThreshold() --> Called by RoundManager at
 *                            the end of a boss round
 *   ResetEconomy()         --> Called on new run start
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- singleton setup. Start() -- subscribes to
 *   RoundManager boss round event. No Update().
 * ============================================================
 */

using UnityEngine;
using UnityEngine.Events;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    // MEMBERS =====================================================================================

    // STARTING VALUES ------------------------------------------

    [Header("Starting Values")]
    [Tooltip("How much gold the player starts with.")]
    public int startingGold = 100;

    // RUNTIME STATS ------------------------------------------

    [Header("Runtime State — view in Play Mode")]
    public int currentGold = 0;

    [Tooltip("Temporary gold from pending buyer sales. Added to currentGold display " +
         "only — not real gold until the buyer executes at round end.")]
    public int temporaryGold = 0;

    [Tooltip("Total gold earned from sales this run. " +
             "Used to check boss round income threshold.")]
    public int totalIncomeThisRun = 0;

    [Tooltip("Total gold earned from sales since the last boss round. " +
             "Reset after each boss round check.")]
    public int incomeThisCycle = 0;

    // BOSS ROUND THRESHOLD -------------------------------------------

    [Header("Boss Round Threshold")]
    [Tooltip("Income the player must earn per cycle to pass the first boss round.")]
    public int baseThreshold = 200;

    [Tooltip("Flat amount added to the threshold after each boss round.")]
    public int thresholdIncreaseFlat = 50;

    [Tooltip("Multiplier applied to the threshold after each boss round. " +
             "E.g. 1.1 = threshold grows by 10% each cycle on top of the flat increase.")]
    public float thresholdScalingMultiplier = 1.1f;

    [Tooltip("Current threshold the player must hit this cycle. " +
             "Updated after each boss round.")]
    public int currentThreshold = 0;

    [Tooltip("How many boss rounds have been completed this run.")]
    public int bossRoundsCompleted = 0;

    // AUTO-COST SETTINGS -------------------------------------------

    [Header("Auto-Cost Settings")]
    [Tooltip("Contractor cost is calculated as upgradeAmount multiplied by this value " +
             "when contractorCost on the CardData is 0.")]
    public float contractorCostPerUpgradeUnit = 10f;

    [Tooltip("Freelancer cost is calculated as this percentage of their average item value " +
             "when freelancerCost on the CardData is 0. E.g. 0.3 = 30% of average value.")]
    [Range(0f, 1f)] public float freelancerCostPercentage = 0.3f;


    // EVENTS ======================================================================================

    [Header("Events")]
    public UnityEvent onGoldChanged;
    public UnityEvent onBossRoundPassed;
    public UnityEvent onBossRoundFailed;

    // METHODS =====================================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ResetEconomy();
    }

    private void Start()
    {
        currentGold = startingGold;  
        currentThreshold = baseThreshold;

        onGoldChanged?.Invoke();
        Debug.Log($"[EconomyManager] Starting gold: {currentGold}g. " +
                  $"First boss round threshold: {currentThreshold}g.");
    }

    // TrySpendGold() and AddGold() are the main methods for modifying the player's gold balance.
    // TrySpendGold() -----------------------------------------------------
    // Simply compares current gold to amount passed as parameter. If higher, it returns true. If lower, it returns false and logs a message to the console. 
    
    /// <summary>
    /// Attempts to spend gold. Returns true if successful, false if insufficient funds.
    /// Pass a description for debug logging (e.g. "Buy Antique Clock").
    /// </summary>
    public bool TrySpendGold(int amount, string description = "")
    {
        if (amount <= 0) return true; // If free

        if (currentGold < amount)     // If current balance is lower than amount, return false.
        {
            Debug.Log($"[EconomyManager] Cannot afford '{description}' " +
                      $"— costs {amount}g, have {currentGold}g.");
            return false;
        }

        currentGold -= amount;  // If current balance is above amount, deduct amount from current balance.
        Debug.Log($"[EconomyManager] Spent {amount}g on '{description}'. " +
                  $"Remaining: {currentGold}g.");
        onGoldChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Adds gold to the player's balance and tracks it as income.
    /// Pass a description for debug logging (e.g. "Sold Antique Clock").
    /// </summary>
    public void AddGold(int amount, string description = "")
    {
        if (amount <= 0) return;

        currentGold += amount;
        totalIncomeThisRun += amount;
        incomeThisCycle += amount;

        Debug.Log($"[EconomyManager] Earned {amount}g from '{description}'. " +
                  $"Balance: {currentGold}g. Cycle income: {incomeThisCycle}g.");
        onGoldChanged?.Invoke();
    }

    // CanAfford() -----------------------------------------------------
    // Simple affordance check compares current gold (+ any temporary gold) to amount passed as parameter.
    /// <summary>
    /// Returns true if the player can afford the given amount using
    /// both real gold and temporary gold from staged buyers.
    /// </summary>
    public bool CanAfford(int amount)
    {
        return EffectiveGold() >= amount;
    }

    // Temporary Gold Methods -----------------------------------------------

    /// <summary>
    /// Adds temporary gold from a staged buyer's expected payout.
    /// Does not change currentGold — only affects display and affordability checks.
    /// Fires onGoldChanged so the HUD updates immediately.
    /// </summary>
    public void AddTemporaryGold(int amount)
    {
        temporaryGold += amount;
        Debug.Log($"[EconomyManager] Temporary gold added: {amount}g. " +
                  $"Total temporary: {temporaryGold}g.");
        onGoldChanged?.Invoke();
    }

    /// <summary>
    /// Removes temporary gold when a buyer is unstaged.
    /// Clamps to 0 to avoid negative temporary gold.
    /// Fires onGoldChanged so the HUD updates immediately.
    /// </summary>
    public void RemoveTemporaryGold(int amount)
    {
        temporaryGold = Mathf.Max(0, temporaryGold - amount);
        Debug.Log($"[EconomyManager] Temporary gold removed: {amount}g. " +
                  $"Total temporary: {temporaryGold}g.");
        onGoldChanged?.Invoke();
    }

    /// <summary>
    /// Clears all temporary gold. Called at round end after all
    /// buyers have executed so no stale temporary values persist.
    /// </summary>
    public void ClearTemporaryGold()
    {
        temporaryGold = 0;
        onGoldChanged?.Invoke();
    }

    /// <summary>
    /// Returns the effective gold available for affordability checks —
    /// real currentGold plus any temporary gold from staged buyers.
    /// </summary>
    public int EffectiveGold() => currentGold + temporaryGold;

    // Cost Calculators -----------------------------------------------------
    // Cost Calculators for Contractors and Freelancers that auto-calculate costs based on type when no cost is set
    // Create the auto-calculation logic for contractor and freelancers below - currently incomplete.

    /// <summary>
    /// Returns the gold cost to hire a contractor.
    /// Uses the manual contractorCost on CardData if set (> 0),
    /// otherwise auto-calculates from upgradeAmount.
    /// </summary>
    public int GetContractorCost(CardData card)
    {
        if (card.fixedCost)
            return card.contractorCost;

        // Auto-calculate based on upgrade type and amount
        switch (card.upgradeType)
        {
            case ContractorUpgradeType.WarehouseSlots:
            case ContractorUpgradeType.FloorSpace:
            case ContractorUpgradeType.Reputation:
                return Mathf.Max(1, Mathf.RoundToInt(card.upgradeAmount
                       * contractorCostPerUpgradeUnit));

            case ContractorUpgradeType.UnlockCategory:
            case ContractorUpgradeType.UnlockSubCategory:
                // Flat cost for unlock contractors — tweak contractorCostPerUpgradeUnit
                // to scale this, or set contractorCost manually on the asset
                return Mathf.RoundToInt(contractorCostPerUpgradeUnit * 5f);

            case ContractorUpgradeType.HireStaff:
                return Mathf.RoundToInt(contractorCostPerUpgradeUnit * 3f);

            default:
                return card.contractorCost;
        }
    }

    /// <summary>
    /// Returns the gold cost to send a freelancer out.
    /// Uses the manual freelancerCost on CardData if set (> 0),
    /// otherwise auto-calculates as a percentage of average item value.
    /// </summary>
    public int GetFreelancerCost(CardData card)
    {
        if (card.fixedCost)
            return card.freelancerCost;

        int averageValue = (card.freelancerMinItemValue + card.freelancerMaxItemValue) / 2;
        return Mathf.Max(1, Mathf.RoundToInt(averageValue * freelancerCostPercentage));
    }

    // ─────────────────────────────────────────────
    // AUCTION
    // ─────────────────────────────────────────────

    /// <summary>
    /// Adds the final auction selling amount to the player's gold.
    /// Called by AuctionManager when the auction resolves successfully.
    /// </summary>
    public void AddAuctionProceeds(int amount)              // Called when the auction is over, but the game is not
    {
        AddGold(amount, "Auction proceeds");
    }

    /// <summary>
    /// Deducts the rent and bills amount from the player's gold.
    /// Amount is equal to currentThreshold at time of calling.
    /// Called by AuctionManager at the start of the round immediately
    /// following a passed auction round.
    /// Escalates the threshold for the next auction cycle.
    /// </summary>
    public void DeductRentAndBills()
    {
        int rent = currentThreshold;      // First, sets rent as the current threshold

        // Escalate threshold for next cycle before deducting
        int newThreshold = Mathf.RoundToInt(
            (currentThreshold + thresholdIncreaseFlat) * thresholdScalingMultiplier);
        currentThreshold = newThreshold;                                 // Then increases the threshold for the next auction
        incomeThisCycle = 0;
        bossRoundsCompleted++;

        TrySpendGold(rent, "Rent and bills");

        Debug.Log($"[EconomyManager] Rent and bills paid: {rent}g. " +
                  $"Next auction threshold: {currentThreshold}g.");

        onGoldChanged?.Invoke();
    }

    // ─────────────────────────────────────────────
    // RESET
    // ─────────────────────────────────────────────

    /// <summary>
    /// Resets all economy state to starting values.
    /// Call when starting a new run.
    /// </summary>
    public void ResetEconomy()
    {
        currentGold = startingGold;
        totalIncomeThisRun = 0;
        incomeThisCycle = 0;
        currentThreshold = baseThreshold;
        bossRoundsCompleted = 0;
        onGoldChanged?.Invoke();
        Debug.Log("[EconomyManager] Economy reset.");
    }
}