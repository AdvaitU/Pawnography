/*
 * ============================================================
 * SCRIPT:      RoundManager.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Manages round progression. Tracks staged card selections
 *   as StagedCardData objects that carry metadata (chosen items
 *   for buyers, purchase confirmation for sellers). Staged
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
 *   Awake() -- singleton setup. Start() intentionally empty.
 *   No Update().
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

    [Tooltip("Staged card selections with metadata. Confirmed on Next Round.")]
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
            Debug.Log($"[RoundManager] Round {currentRound} started. " +
                      $"Drew {currentRoundCards.Count} cards.");
            onRoundStart?.Invoke();
        }
    }

    /// <summary>
    /// Stages a card with empty metadata. Called by CardUI on click.
    /// Returns the new StagedCardData so CardUI can populate metadata.
    /// Returns null if the card is already staged or the max is reached.
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

    /// <summary>
    /// Removes a card from the staged list. Returns the removed
    /// StagedCardData so CardUI can clean up metadata (e.g. free
    /// a chosen item back to the inventory display).
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

    /// <summary>
    /// Returns the StagedCardData for a given card, or null if not staged.
    /// </summary>
    public StagedCardData GetStagedData(CardData card)
    {
        return stagedCards.Find(s => s.card == card);
    }

    /// <summary>
    /// Called by CardUIManager when Next Round is clicked.
    /// Executes all staged card effects then advances the round.
    /// </summary>
    public void ProcessAndEndRound()
    {
        Debug.Log($"[RoundManager] Processing {stagedCards.Count} staged selection(s).");

        List<StagedCardData> toProcess = new List<StagedCardData>(stagedCards);

        foreach (StagedCardData staged in toProcess)
            CardInteractionManager.Instance.ExecuteCardEffect(staged);

        stagedCards.Clear();
        onRoundEnd?.Invoke();
        StartNewRound();
    }

    public void TriggerGameOver()
    {
        Debug.Log("[RoundManager] GAME OVER.");
        onGameOver?.Invoke();
    }
}