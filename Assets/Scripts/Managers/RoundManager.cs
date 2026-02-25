using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages round progression, card selection limits, and boss round detection.
/// Attach to the same persistent GameObject as CardDatabase.
/// </summary>
public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Round Settings")]
    [Tooltip("How many cards are dealt each round.")]
    public int cardsPerRound = 4;

    [Tooltip("Maximum cards the player can select per round before auto-advancing.")]
    public int maxSelectionsPerRound = 2;

    [Tooltip("Every N rounds, a boss (auction) round occurs.")]
    public int bossRoundInterval = 8;

    [Header("Runtime State — view in Play Mode")]
    public int currentRound = 0;
    public int selectionsThisRound = 0;
    public bool isBossRound = false;

    [Header("Current Round Cards")]
    public List<CardData> currentRoundCards = new List<CardData>();
    public List<CardData> selectedCards = new List<CardData>();

    // ── Events other systems can subscribe to ──
    [Header("Events")]
    public UnityEvent onRoundStart;
    public UnityEvent onRoundEnd;
    public UnityEvent onBossRoundStart;
    public UnityEvent onCardSelected;
    public UnityEvent onGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // First round is now started by ShopManager after it finishes
        // syncing all stats, to avoid initialisation order issues.
        // See ShopManager.Start()
    }

    /// <summary>
    /// Advances to the next round. Called by the "Next Round" button or automatically
    /// after the player selects their maximum number of cards.
    /// </summary>
    public void StartNewRound()
    {
        currentRound++;
        selectionsThisRound = 0;
        selectedCards.Clear();

        // Determine if this is a boss round
        isBossRound = (currentRound % bossRoundInterval == 0);

        if (isBossRound)
        {
            Debug.Log($"[RoundManager] BOSS ROUND {currentRound} — Auction begins!");
            onBossRoundStart?.Invoke();
            // Boss round logic will be handled separately — hook into onBossRoundStart
        }
        else
        {
            // Draw 4 new cards from the database
            currentRoundCards = CardDatabase.Instance.DrawRoundCards(cardsPerRound);
            Debug.Log($"[RoundManager] Round {currentRound} started. Drew {currentRoundCards.Count} cards.");
            onRoundStart?.Invoke();
        }
    }

    /// <summary>
    /// Called when the player clicks on a card to select it.
    /// Returns true if the selection was accepted.
    /// </summary>
    public bool TrySelectCard(CardData card)
    {
        // Guard: can't select if already at max or card not in current round
        if (selectionsThisRound >= maxSelectionsPerRound)
        {
            Debug.Log("[RoundManager] Already at max selections for this round.");
            return false;
        }

        if (!currentRoundCards.Contains(card))
        {
            Debug.LogWarning("[RoundManager] Attempted to select a card not in the current round.");
            return false;
        }

        if (selectedCards.Contains(card))
        {
            Debug.Log("[RoundManager] Card already selected.");
            return false;
        }

        selectedCards.Add(card);
        selectionsThisRound++;

        Debug.Log($"[RoundManager] Selected card: {card.cardName} ({selectionsThisRound}/{maxSelectionsPerRound})");
        onCardSelected?.Invoke();

        // Auto-advance if max selections reached
        if (selectionsThisRound >= maxSelectionsPerRound)
        {
            Debug.Log("[RoundManager] Max selections reached — advancing to next round.");
            EndRound();
        }

        return true;
    }

    /// <summary>
    /// Ends the current round and triggers the next one.
    /// Called automatically at max selections, or manually via the Next Round button.
    /// </summary>
    public void EndRound()
    {
        onRoundEnd?.Invoke();
        StartNewRound();
    }

    /// <summary>
    /// Call this when the player fails a boss round auction threshold.
    /// </summary>
    public void TriggerGameOver()
    {
        Debug.Log("[RoundManager] GAME OVER.");
        onGameOver?.Invoke();
    }

    /// <summary>
    /// Cancels a previously accepted card selection, refunding the selection count.
    /// Called if the player backs out of a confirmation popup or cannot complete the action.
    /// </summary>
    public void CancelCardSelection(CardData card)
    {
        if (!selectedCards.Contains(card))
        {
            Debug.LogWarning("[RoundManager] Tried to cancel a selection that was never made.");
            return;
        }

        selectedCards.Remove(card);
        selectionsThisRound--;
        selectionsThisRound = Mathf.Max(0, selectionsThisRound); // Safety clamp

        Debug.Log($"[RoundManager] Selection cancelled for '{card.cardName}'. ({selectionsThisRound}/{maxSelectionsPerRound})");

        // Notify UI to update the HUD counter
        onCardSelected?.Invoke();
    }
}