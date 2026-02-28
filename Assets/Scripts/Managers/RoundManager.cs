/*
 * ============================================================
 * SCRIPT:      RoundManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Manages round progression. Tracks staged (pending) card
 *   selections that the player can toggle freely before
 *   confirming via the Next Round button. When Next Round is
 *   clicked, CardUIManager triggers ProcessAndEndRound() which
 *   passes all staged selections to CardInteractionManager for
 *   execution, then advances to the next round.
 *   Boss rounds are detected every N rounds.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   ShopManager         -- calls StartNewRound() after syncing
 *                          stats; sets cardsPerRound on upgrade
 *   CardUIManager       -- calls ProcessAndEndRound() on Next
 *                          Round button click; reads staged
 *                          selections to update HUD
 *   CardInteractionManager -- calls StageCard() and UnstageCard()
 *                          during card toggle interactions;
 *                          reads stagedCards on confirmation
 *   FreelancerManager   -- subscribes to onRoundStart to tick
 *                          freelancer countdowns
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   StartNewRound()       --> Called by ShopManager.Start()
 *   ProcessAndEndRound()  --> Called by CardUIManager when
 *                            Next Round button is clicked
 *   StageCard()           --> Called by CardInteractionManager
 *                            when a card is toggled on
 *   UnstageCard()         --> Called by CardInteractionManager
 *                            when a card is toggled off
 *   TriggerGameOver()     --> Called when boss round failed
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- singleton setup. Start() intentionally empty.
 *   No Update(). All logic is event-driven.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Round Settings")]
    public int cardsPerRound = 4;
    public int maxSelectionsPerRound = 2;
    public int bossRoundInterval = 8;

    [Header("Runtime State — view in Play Mode")]
    public int currentRound = 0;
    public bool isBossRound = false;

    [Header("Current Round")]
    public List<CardData> currentRoundCards = new List<CardData>();

    [Tooltip("Cards the player has toggled on this round but not yet confirmed. " +
             "Confirmed when Next Round is clicked.")]
    public List<CardData> stagedCards = new List<CardData>();

    [Header("Events")]
    public UnityEvent onRoundStart;
    public UnityEvent onRoundEnd;
    public UnityEvent onBossRoundStart;
    public UnityEvent onStagedSelectionsChanged; // Fired when staged list changes — UI listens to update HUD
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

    /// <summary>
    /// Begins a new round. Clears staged selections and draws new cards.
    /// </summary>
    public void StartNewRound()
    {
        currentRound++;
        stagedCards.Clear();

        isBossRound = (currentRound % bossRoundInterval == 0);

        if (isBossRound)
        {
            Debug.Log($"[RoundManager] BOSS ROUND {currentRound}");
            onBossRoundStart?.Invoke();
        }
        else
        {
            currentRoundCards = CardDatabase.Instance.DrawRoundCards(cardsPerRound);
            Debug.Log($"[RoundManager] Round {currentRound} started. Drew {currentRoundCards.Count} cards.");
            onRoundStart?.Invoke();
        }
    }

    /// <summary>
    /// Stages a card as a pending selection. Called when the player clicks an unselected card.
    /// Returns true if the card was successfully staged.
    /// </summary>
    public bool StageCard(CardData card)
    {
        if (stagedCards.Contains(card))
        {
            Debug.Log($"[RoundManager] '{card.cardName}' is already staged.");
            return false;
        }

        if (stagedCards.Count >= maxSelectionsPerRound)
        {
            Debug.Log($"[RoundManager] Max selections ({maxSelectionsPerRound}) already staged.");
            return false;
        }

        stagedCards.Add(card);
        Debug.Log($"[RoundManager] Staged '{card.cardName}'. ({stagedCards.Count}/{maxSelectionsPerRound})");
        onStagedSelectionsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes a card from the staged selection. Called when the player clicks a selected card.
    /// </summary>
    public bool UnstageCard(CardData card)
    {
        if (!stagedCards.Contains(card))
        {
            Debug.Log($"[RoundManager] '{card.cardName}' is not staged.");
            return false;
        }

        stagedCards.Remove(card);
        Debug.Log($"[RoundManager] Unstaged '{card.cardName}'. ({stagedCards.Count}/{maxSelectionsPerRound})");
        onStagedSelectionsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Called by CardUIManager when Next Round is clicked.
    /// Passes all staged cards to CardInteractionManager for execution,
    /// then advances to the next round.
    /// </summary>
    public void ProcessAndEndRound()
    {
        Debug.Log($"[RoundManager] Processing {stagedCards.Count} staged selection(s).");

        // Process a copy of the list since interactions may modify it
        List<CardData> toProcess = new List<CardData>(stagedCards);

        foreach (CardData card in toProcess)
        {
            CardInteractionManager.Instance.ExecuteCardEffect(card);
        }

        stagedCards.Clear();
        onRoundEnd?.Invoke();
        StartNewRound();
    }

    /// <summary>
    /// Call when the player fails a boss round auction threshold.
    /// </summary>
    public void TriggerGameOver()
    {
        Debug.Log("[RoundManager] GAME OVER.");
        onGameOver?.Invoke();
    }
}