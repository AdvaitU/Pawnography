/*
 * ============================================================
 * SCRIPT:      RoundManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Manages round progression. Tracks staged card selections
 *   as StagedCardData objects that carry metadata. Staged
 *   selections can be toggled freely before being confirmed
 *   via the Next Round button. On Next Round, all staged cards
 *   are passed to CardInteractionManager for execution.
 *   Boss rounds are detected every N rounds.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   ShopManager         -- calls StartNewRound() after syncing
 *                          stats; sets cardsPerRound on upgrade
 *   CardUIManager       -- calls ProcessAndEndRound() on Next
 *                          Round; reads stagedCards for HUD
 *   CardUI              -- calls StageCard(), UnstageCard(),
 *                          GetStagedData()
 *   CardInteractionManager -- reads StagedCardData metadata
 *                          during ExecuteCardEffect()
 *   FreelancerManager   -- subscribes to onRoundStart
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   StartNewRound()       --> Called by ShopManager.Start()
 *   ProcessAndEndRound()  --> Called by CardUIManager
 *   StageCard()           --> Called by CardUI on click
 *   UnstageCard()         --> Called by CardUI on deselect
 *   GetStagedData()       --> Called by CardUI to read metadata
 *   TriggerGameOver()     --> Called by EconomyManager on fail
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- singleton setup. No Update().
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }  // Set instance

    [Header("Round Settings")]
    [Tooltip("Number of cards to spawn per round")]
    public int cardsPerRound = 3;           // Defaults to 3, but set by ShopManager dynamically
    [Tooltip("Number of cards that can be selected per round")]
    public int maxSelectionsPerRound = 2;
    [Tooltip("Interval between boss rounds. 8 means every 8th round is a boss round starting from Round 8.")]
    public int bossRoundInterval = 8;

    [Header("Runtime State")]
    public int currentRound = 0;
    public bool isBossRound = false;

    [Header("Current Round")]
    [Tooltip("Cards present in the current round")]
    public List<CardData> currentRoundCards = new List<CardData>();

    [Tooltip("Cards that have been staged i.e. selected but functionality" + 
        " waiting to be executed till Next Round button is clicked")]
    public List<StagedCardData> stagedCards = new List<StagedCardData>();

    [Header("Events")]
    public UnityEvent onRoundStart;
    public UnityEvent onRoundEnd;
    public UnityEvent onBossRoundStart;
    public UnityEvent onStagedSelectionsChanged;
    public UnityEvent onGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // First round triggered by ShopManager after stat sync
    }

    // METHODS ======================================================================================================================

    // StartNewRound() method ----------------------------------------------------------------------
    // Called by ShopManager. Conditional for boss round executed here. Invokes the relevant event.

    /// <summary>
    /// Increments the round counter, clears staged cards, and checks
    /// whether this is a boss round. Boss rounds fire onBossRoundStart
    /// only. Normal rounds draw cards and fire onRoundStart.
    /// Called by ShopManager.Start() for the first round and by
    /// ProcessAndEndRound() for all subsequent rounds.
    /// </summary>
    public void StartNewRound()
    {
        currentRound++;      // Raise round number
        stagedCards.Clear(); // Clear list of Staged Cards (called after execution is managed by CardInteractionManager)

        isBossRound = (currentRound % bossRoundInterval == 0);

        if (isBossRound)
        {
            Debug.Log($"[RoundManager] BOSS ROUND {currentRound}");
            onBossRoundStart?.Invoke();                                // Invoke Boss Round event if isBossRound is true - check done before other round checks.
        }
        else
        {
            currentRoundCards = CardDatabase.Instance.DrawRoundCards(cardsPerRound);
            Debug.Log($"[RoundManager] Round {currentRound} started. " +
                      $"Drew {currentRoundCards.Count} cards.");
            onRoundStart?.Invoke();
        }
    }

    // StageCard (card) -----------------------------------------------------------------------------------
    // Stages a card with empty metadata. Called by CardUI on click.
    // Returns the new StagedCardData so CardUI can populate metadata.
    // Returns null if the card is already staged or the max is reached.

    /// <summary>
    /// Stages a card with empty metadata. Returns the new StagedCardData
    /// so the caller can populate type-specific fields (chosenItem,
    /// purchaseConfirmed). Returns null if the card is already staged
    /// or the max selection count has been reached.
    /// </summary>
    public StagedCardData StageCard(CardData card)
    {
        if (GetStagedData(card) != null)
        {
            Debug.Log($"[RoundManager] '{card.cardName}' is already staged.");
            return null;
        }

        if (stagedCards.Count >= maxSelectionsPerRound)
        {
            Debug.Log($"[RoundManager] Max selections reached.");
            return null;
        }

        StagedCardData staged = new StagedCardData(card);
        stagedCards.Add(staged);
        Debug.Log($"[RoundManager] Staged '{card.cardName}'. " +
                  $"({stagedCards.Count}/{maxSelectionsPerRound})");
        onStagedSelectionsChanged?.Invoke();
        return staged;
    }

    // UnstageCard() -  Removes a card from the staged list. ----------------------------------------------
    // Returns the removed StagedCardData so CardUI can clean up metadata (e.g. free a chosen item back to the inventory display).
    // Called by CardUI on deselect

    /// <summary>
    /// Removes a card from the staged list. Returns the removed
    /// StagedCardData so the caller can clean up metadata
    /// (e.g. free a chosen item back to the inventory display).
    /// Returns null if the card was not staged.
    /// </summary>
    public StagedCardData UnstageCard(CardData card)
    {
        StagedCardData staged = GetStagedData(card);
        if (staged == null)
        {
            Debug.Log($"[RoundManager] '{card.cardName}' is not staged.");
            return null;
        }

        stagedCards.Remove(staged);
        Debug.Log($"[RoundManager] Unstaged '{card.cardName}'. " +
                  $"({stagedCards.Count}/{maxSelectionsPerRound})");
        onStagedSelectionsChanged?.Invoke();
        return staged;
    }

    // GetStagedData() - Getter function that simply returns the staged card data. -----------------------
    // Returns the StagedCardData for a given card, or null if not staged.
    // Called by CardUI to read staged card data to display

    /// <summary>
    /// Returns the StagedCardData for a given CardData, or null if
    /// that card is not currently staged.
    /// </summary>
    public StagedCardData GetStagedData(CardData card)
    {
        return stagedCards.Find(s => s.card == card);
    }

    // ProcessAndEndRound() - Called by CardUIManager when 'Next Round' button is clicked. ----------------
    // Executes all staged card effects then advances the round.

    /// <summary>
    /// Executes all staged card effects via CardInteractionManager,
    /// clears the staged list, fires onRoundEnd, then immediately
    /// calls StartNewRound(). Called by CardUIManager when the
    /// Next Round button is clicked.
    /// </summary>
    public void ProcessAndEndRound()
    {
        Debug.Log($"[RoundManager] Processing {stagedCards.Count} staged selection(s).");

        List<StagedCardData> toProcess = new List<StagedCardData>(stagedCards);        // Make a list of all staged cards

        foreach (StagedCardData staged in toProcess)
            CardInteractionManager.Instance.ExecuteCardEffect(staged);  // Call on CardInteractionManager to execute effects of all staged cards

        stagedCards.Clear();          // Clear cache
        onRoundEnd?.Invoke();         // Invoke OnRoundEnd event
        StartNewRound();
    }

    // TriggerGameOver() -----------------------------------------------------------------------------------
    // Currently does nothing except logging, more functionality to be added here.

    /// <summary>
    /// Triggers the game over state. Fires onGameOver.
    /// Called by EconomyManager when the boss round income
    /// threshold is not met. Expand this method when building
    /// the Game Over screen and run progression system.
    /// </summary>
    public void TriggerGameOver()
    {
        Debug.Log("[RoundManager] GAME OVER.");
        onGameOver?.Invoke();
    }

}