/*
 * ============================================================
 * SCRIPT:      FreelancerManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Tracks all active freelancers that have been sent out.
 *   Each round start, decrements all return countdowns via
 *   TickFreelancers(). Per-round effects (AutoAppraiser,
 *   temp stat bonuses) are applied each tick. One-shot effects
 *   (FetchItem, LoanShark) fire only when the countdown
 *   reaches zero.
 *
 *   AutoAppraiser: at round start, iterates currentRoundCards
 *   for matching Seller cards and adds them to
 *   appraisedSellerCards. Then calls RevealAppraisedValue()
 *   on matching CardUI instances directly so the player sees
 *   the true value on the card face immediately.
 *
 *   Temp stat bonuses: passive while active. Callers query
 *   GetTempFloorSpaceBonus(), GetTempWarehouseBonus(), or
 *   GetTempReputationBonus() and add the result at read time.
 *
 * ------------------------------------------------------------
 * DESIGN NOTES:
 *   GetTempFloorSpaceBonus() and GetTempWarehouseBonus() are
 *   exposed but NOT YET wired into RoundManager draw count or
 *   InventoryManager.HasSpace(). This is a follow-up task —
 *   wire them when you want temp bonuses to affect card draw
 *   count and warehouse capacity live.
 *
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardInteractionManager -- calls SendOutFreelancer(),
 *                          IsAppraisedByFreelancer()
 *   ShopStatsUI            -- subscribes to onFreelancerReturned
 *   CardUI                 -- RevealAppraisedValue() called
 *                          directly by this manager
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   SendOutFreelancer()         --> CardInteractionManager
 *   IsAppraisedByFreelancer()   --> CardInteractionManager
 *   GetTempFloorSpaceBonus()    --> Callers at read time
 *   GetTempWarehouseBonus()     --> Callers at read time
 *   GetTempReputationBonus()    --> Callers at read time
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FreelancerManager : MonoBehaviour
{
    public static FreelancerManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // RUNTIME STATE
    // ─────────────────────────────────────────────

    [Header("Active Freelancers")]
    public List<ActiveFreelancer> activeFreelancers = new List<ActiveFreelancer>();

    /// <summary>
    /// Cards marked for auto-appraisal this round by an active AutoAppraiser
    /// freelancer. Cleared at the start of each round before re-marking.
    /// ExecuteSeller() checks this to apply appraisal when the item enters
    /// inventory.
    /// </summary>
    [HideInInspector]
    public HashSet<CardData> appraisedSellerCards = new HashSet<CardData>();

    // ─────────────────────────────────────────────
    // EVENTS
    // ─────────────────────────────────────────────

    [Header("Events")]
    [Tooltip("Fired when a FetchItem freelancer returns with an item.")]
    public UnityEvent<ActiveFreelancer> onFreelancerReturned;

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
        RoundManager.Instance.onRoundStart.AddListener(TickFreelancers);
    }

    // ─────────────────────────────────────────────
    // SEND OUT
    // ─────────────────────────────────────────────

    /// <summary>
    /// Creates an ActiveFreelancer from the given CardData and adds it to
    /// the active list. Called by CardInteractionManager when a freelancer
    /// card is executed.
    /// </summary>
    public void SendOutFreelancer(CardData card)
    {
        ActiveFreelancer freelancer = new ActiveFreelancer(card);
        activeFreelancers.Add(freelancer);
        Debug.Log($"[FreelancerManager] '{card.cardName}' sent out " +
                  $"(Type: {card.freelancerType}, Measure: {card.freelancerMeasure}). " +
                  $"Returns in {card.roundsToReturn} rounds.");
    }

    // ─────────────────────────────────────────────
    // TICK
    // ─────────────────────────────────────────────

    /// <summary>
    /// Called each round start. Clears the appraisal HashSet, then for each
    /// active freelancer: applies per-round effects, decrements the countdown,
    /// and resolves one-shot effects on expiry.
    /// </summary>
    private void TickFreelancers()
    {
        appraisedSellerCards.Clear();

        List<ActiveFreelancer> expired = new List<ActiveFreelancer>();

        foreach (ActiveFreelancer f in activeFreelancers)
        {
            ApplyPerRoundEffect(f);

            f.roundsRemaining--;
            Debug.Log($"[FreelancerManager] '{f.cardName}' " +
                      $"({f.freelancerType}) — {f.roundsRemaining} round(s) remaining.");

            if (f.roundsRemaining <= 0)
                expired.Add(f);
        }

        // Resolve and remove expired freelancers after iterating
        foreach (ActiveFreelancer f in expired)
        {
            ResolveOnExpiry(f);
            activeFreelancers.Remove(f);
        }
    }

    // ─────────────────────────────────────────────
    // PER-ROUND EFFECTS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Applies effects that run every round while the freelancer is active.
    /// Temp stat bonuses are passive (counted by query methods) so only
    /// AutoAppraiser needs active per-round logic here.
    /// </summary>
    private void ApplyPerRoundEffect(ActiveFreelancer f)
    {
        switch (f.freelancerType)
        {
            case FreelancerType.AutoAppraiser:
                ApplyAutoAppraiser(f);
                break;

            // Passive types — counted by GetTempXBonus() query methods.
            // No active per-round logic needed here.
            case FreelancerType.TempFloorSpaceBonus:
            case FreelancerType.TempWarehouseBonus:
            case FreelancerType.TempReputationBonus:
                break;

            // One-shot types — no per-round effect.
            case FreelancerType.FetchItem:
            case FreelancerType.LoanShark:
            case FreelancerType.None:
            default:
                break;
        }
    }

    /// <summary>
    /// Marks any Seller cards in the current round whose subCategory matches
    /// the AutoAppraiser's conservatorExpertise. Then calls
    /// RevealAppraisedValue() on their CardUI instances so the player sees
    /// the true value immediately on the card face.
    /// </summary>
    private void ApplyAutoAppraiser(ActiveFreelancer f)
    {
        foreach (CardData card in RoundManager.Instance.currentRoundCards)
        {
            if (card.category == null) continue;
            if (card.category.categoryName != "Seller") continue;
            if (card.subCategory == CardSubCategory.None) continue;
            if (card.subCategory != f.conservatorExpertise) continue;

            appraisedSellerCards.Add(card);
            Debug.Log($"[FreelancerManager] AutoAppraiser '{f.cardName}' " +
                      $"marked '{card.cardName}' for appraisal " +
                      $"(subCategory: {card.subCategory}).");
        }
    }

    /// <summary>
    /// Called by CardUIManager immediately after SpawnCards() completes.
    /// Iterates activeCardUIs and calls RevealAppraisedValue() on any
    /// CardUI whose assignedCard is in appraisedSellerCards.
    /// Safe to call even if no AutoAppraiser is active — HashSet will
    /// simply be empty and nothing will happen.
    /// </summary>
    public void NotifyCardsSpawned()
    {
        foreach (CardUI cardUI in CardUIManager.Instance.activeCardUIs)
        {
            if (appraisedSellerCards.Contains(cardUI.assignedCard))
            {
                cardUI.RevealAppraisedValue();

                // ── Future visual effect hook ──
                // Replace or supplement this with an animation or
                // UI indicator when the visual effect is implemented.
            }
        }
    }

    // ─────────────────────────────────────────────
    // ONE-SHOT EXPIRY EFFECTS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Called when a freelancer's countdown reaches zero.
    /// Routes to the appropriate one-shot resolution by type.
    /// Per-round types (AutoAppraiser, temp bonuses) have no
    /// expiry effect beyond being removed from the active list.
    /// </summary>
    private void ResolveOnExpiry(ActiveFreelancer f)
    {
        switch (f.freelancerType)
        {
            case FreelancerType.FetchItem:
                ResolveFetchItem(f);
                break;

            case FreelancerType.LoanShark:
                ResolveLoanShark(f);
                break;

            case FreelancerType.AutoAppraiser:
            case FreelancerType.TempFloorSpaceBonus:
            case FreelancerType.TempWarehouseBonus:
            case FreelancerType.TempReputationBonus:
                Debug.Log($"[FreelancerManager] '{f.cardName}' " +
                          $"({f.freelancerType}) expired and has been removed.");
                break;

            case FreelancerType.None:
            default:
                Debug.LogWarning($"[FreelancerManager] '{f.cardName}' expired " +
                                 $"with unhandled type '{f.freelancerType}'.");
                break;
        }
    }

    /// <summary>
    /// Generates a random item within the freelancer's value range and
    /// attempts to add it to the player's inventory.
    /// </summary>
    private void ResolveFetchItem(ActiveFreelancer f)
    {
        // Build eligible pool — items with trueValue at or below the cap
        List<CardData> eligible = CardDatabase.Instance.fetchedItems.FindAll(
            item => item != null && item.itemTrueValue <= f.maxItemValue);

        if (eligible.Count == 0)
        {
            // Fallback — no items met the value filter, pick from the full list
            Debug.LogWarning($"[FreelancerManager] '{f.cardName}' found no items " +
                             $"with trueValue <= {f.maxItemValue}g — " +
                             $"falling back to full FetchedItems list.");

            eligible = CardDatabase.Instance.fetchedItems.FindAll(item => item != null);

            if (eligible.Count == 0)
            {
                Debug.LogWarning($"[FreelancerManager] '{f.cardName}' found nothing — " +
                                 $"FetchedItems list is empty. Check CardDatabase.");
                onFreelancerReturned?.Invoke(f);
                return;
            }
        }

        // Pick a random item from the eligible pool
        CardData chosenCard = eligible[Random.Range(0, eligible.Count)];
        f.returnedItemValue = chosenCard.itemTrueValue;

        Debug.Log($"[FreelancerManager] '{f.cardName}' returned with " +
                  $"'{chosenCard.cardName}' (trueValue: {chosenCard.itemTrueValue}g).");

        bool added = InventoryManager.Instance.TryAddItem(chosenCard);

        if (added)
        {
            // Mark the item as already appraised — freelancer knows what they found
            InventoryItem addedItem = InventoryManager.Instance.items.Find(
                i => i.sourceCard == chosenCard);

            if (addedItem != null)
            {
                addedItem.isAppraised = true;
                addedItem.appraisedValue = chosenCard.itemTrueValue;
                addedItem.valueIsRevealed = true;
            }
        }
        else
        {
            Debug.LogWarning($"[FreelancerManager] Could not add " +
                             $"'{chosenCard.cardName}' — warehouse full.");
        }

        onFreelancerReturned?.Invoke(f);
    }

    /// <summary>
    /// Attempts to collect the loan repayment. If the player cannot afford
    /// it, deducts freelancerMeasure reputation as a penalty instead.
    /// </summary>
    private void ResolveLoanShark(ActiveFreelancer f)
    {
        int currentGold = EconomyManager.Instance.currentGold;

        if (currentGold >= f.loanAmount)
        {
            // Player can afford repayment — deduct loan and reward reputation
            EconomyManager.Instance.TrySpendGold(
                f.loanAmount, $"Loan repayment to {f.cardName}");

            int repBefore = ShopManager.Instance.reputation;
            ShopManager.Instance.reputation =
                Mathf.Min(ShopManager.Instance.maxReputation,
                          ShopManager.Instance.reputation + f.freelancerMeasure);

            Debug.Log($"[FreelancerManager] '{f.cardName}' loan repaid: {f.loanAmount}g. " +
                      $"Reputation: {repBefore} → {ShopManager.Instance.reputation} " +
                      $"(+{f.freelancerMeasure}).");
        }
        else
        {
            // Player cannot afford repayment — drain all gold and penalise reputation
            EconomyManager.Instance.TrySpendGold(
                currentGold, $"Loan default to {f.cardName} — all gold taken");

            int repBefore = ShopManager.Instance.reputation;
            ShopManager.Instance.reputation =
                Mathf.Max(0, ShopManager.Instance.reputation - f.freelancerMeasure);

            Debug.LogWarning($"[FreelancerManager] '{f.cardName}' loan defaulted! " +
                             $"All gold taken ({currentGold}g). " +
                             $"Reputation: {repBefore} → {ShopManager.Instance.reputation} " +
                             $"(-{f.freelancerMeasure}).");
        }

        ShopManager.Instance.onShopStatsChanged?.Invoke();
    }

    // ─────────────────────────────────────────────
    // QUERY — APPRAISAL
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns true if the given CardData has been marked for auto-appraisal
    /// by an active AutoAppraiser this round. Called by
    /// CardInteractionManager.ExecuteSeller() after TryAddItem() succeeds.
    /// </summary>
    public bool IsAppraisedByFreelancer(CardData card)
    {
        return appraisedSellerCards.Contains(card);
    }

    // ─────────────────────────────────────────────
    // QUERY — TEMP STAT BONUSES
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns the total temporary floor space bonus from all active
    /// TempFloorSpaceBonus freelancers. Add to ShopManager.floorSpace
    /// at read time when needed.
    /// NOTE: Not yet wired into RoundManager draw count — follow-up task.
    /// </summary>
    public int GetTempFloorSpaceBonus()
    {
        int total = 0;
        foreach (ActiveFreelancer f in activeFreelancers)
            if (f.freelancerType == FreelancerType.TempFloorSpaceBonus)
                total += f.freelancerMeasure;
        return total;
    }

    /// <summary>
    /// Returns the total temporary warehouse slot bonus from all active
    /// TempWarehouseBonus freelancers. Add to InventoryManager.maxSlots
    /// at read time when needed.
    /// NOTE: Not yet wired into InventoryManager.HasSpace() — follow-up task.
    /// </summary>
    public int GetTempWarehouseBonus()
    {
        int total = 0;
        foreach (ActiveFreelancer f in activeFreelancers)
            if (f.freelancerType == FreelancerType.TempWarehouseBonus)
                total += f.freelancerMeasure;
        return total;
    }

    /// <summary>
    /// Returns the total temporary reputation bonus from all active
    /// TempReputationBonus freelancers. Add to ShopManager.reputation
    /// at read time when needed.
    /// </summary>
    public int GetTempReputationBonus()
    {
        int total = 0;
        foreach (ActiveFreelancer f in activeFreelancers)
            if (f.freelancerType == FreelancerType.TempReputationBonus)
                total += f.freelancerMeasure;
        return total;
    }
}


// ==========================================================
// ACTIVE FREELANCER CLASS
// ==========================================================

/// <summary>
/// Runtime data container for a freelancer that has been sent out.
/// Tracks the return countdown, type, and all data needed to resolve
/// the effect on tick or expiry.
/// Not a MonoBehaviour — lives only in memory during a run.
/// </summary>
[System.Serializable]
public class ActiveFreelancer
{
    public string cardName;
    public FreelancerType freelancerType;
    public int freelancerMeasure;
    public CardSubCategory conservatorExpertise;
    public int roundsRemaining;

    // FetchItem only
    public int maxItemValue;
    public int returnedItemValue;
    // LoanShark only
    public int loanAmount;

    public ActiveFreelancer(CardData card)
    {
        cardName = card.cardName;
        freelancerType = card.freelancerType;
        freelancerMeasure = card.freelancerMeasure;
        conservatorExpertise = card.conservatorExpertise;
        roundsRemaining = card.roundsToReturn;
        maxItemValue = card.freelancerMaxItemValue;
        returnedItemValue = 0;
        loanAmount = card.loanAmount;
    }
}