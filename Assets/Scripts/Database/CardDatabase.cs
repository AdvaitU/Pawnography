using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central database holding all CardCategory and CardData assets.
/// Attach this to a persistent GameObject (e.g. "GameManager").
/// Populate the two lists in the Inspector by dragging in your ScriptableObject assets.
/// </summary>
public class CardDatabase : MonoBehaviour
{
    public static CardDatabase Instance { get; private set; }

    [Header("Database — populate in Inspector")]
    [Tooltip("Drag all CardCategory ScriptableObject assets here.")]
    public List<CardCategory> allCategories = new List<CardCategory>();

    [Tooltip("Drag all CardData ScriptableObject assets here.")]
    public List<CardData> allCards = new List<CardData>();

    // ── Cached lookup: category → cards that belong to it ──
    private Dictionary<CardCategory, List<CardData>> cardsByCategory;

    private void Awake()
    {
        // Singleton setup — database persists across scenes
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookup();
    }

    /// <summary>
    /// Builds a fast dictionary lookup from category → list of eligible cards.
    /// Called once on Awake; call again manually if you add cards at runtime.
    /// </summary>
    public void BuildLookup()
    {
        cardsByCategory = new Dictionary<CardCategory, List<CardData>>();

        foreach (CardCategory cat in allCategories)
        {
            cardsByCategory[cat] = new List<CardData>();
        }

        foreach (CardData card in allCards)
        {
            if (card.category != null && cardsByCategory.ContainsKey(card.category))
            {
                cardsByCategory[card.category].Add(card);
            }
            else
            {
                Debug.LogWarning($"[CardDatabase] Card '{card.cardName}' has a missing or unregistered category.");
            }
        }
    }

    /// <summary>
    /// STEP 1: Picks a category using weighted random selection.
    /// Only categories with canSpawn = true and spawnWeight > 0 are eligible.
    /// Returns null if no eligible category exists.
    /// </summary>
    public CardCategory PickRandomCategory()
    {
        // Build the eligible pool
        List<CardCategory> eligible = new List<CardCategory>();
        float totalWeight = 0f;

        foreach (CardCategory cat in allCategories)
        {
            if (cat.canSpawn && cat.spawnWeight > 0f)
            {
                eligible.Add(cat);
                totalWeight += cat.spawnWeight;
            }
        }

        if (eligible.Count == 0)
        {
            Debug.LogWarning("[CardDatabase] No eligible categories to spawn from.");
            return null;
        }

        // Weighted random roll
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (CardCategory cat in eligible)
        {
            cumulative += cat.spawnWeight;
            if (roll <= cumulative)
                return cat;
        }

        // Fallback (should not normally be reached)
        return eligible[eligible.Count - 1];
    }

    /// <summary>
    /// STEP 2: Given a category, picks a specific card using weighted random selection.
    /// Only cards with canSpawn = true and spawnWeight > 0 are eligible.
    /// Returns null if no eligible card exists in the category.
    /// </summary>
    public CardData PickRandomCardFromCategory(CardCategory category)
    {
        if (!cardsByCategory.ContainsKey(category))
        {
            Debug.LogWarning($"[CardDatabase] Category '{category.categoryName}' not found in lookup.");
            return null;
        }

        List<CardData> eligible = new List<CardData>();
        float totalWeight = 0f;

        foreach (CardData card in cardsByCategory[category])
        {
            if (card.canSpawn && card.spawnWeight > 0f)
            {
                eligible.Add(card);
                totalWeight += card.spawnWeight;
            }
        }

        if (eligible.Count == 0)
        {
            Debug.LogWarning($"[CardDatabase] No eligible cards in category '{category.categoryName}'.");
            return null;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (CardData card in eligible)
        {
            cumulative += card.spawnWeight;
            if (roll <= cumulative)
                return card;
        }

        return eligible[eligible.Count - 1];
    }

    /// <summary>
    /// Convenience method that runs both steps and returns a single CardData.
    /// Returns null if anything fails.
    /// </summary>
    public CardData PickRandomCard()
    {
        CardCategory category = PickRandomCategory();
        if (category == null) return null;
        return PickRandomCardFromCategory(category);
    }

    /// <summary>
    /// Draws a set of cards for a new round. Attempts to avoid duplicates.
    /// </summary>
    /// <param name="count">Number of cards to draw (default 4).</param>
    public List<CardData> DrawRoundCards(int count = 4)
    {
        List<CardData> drawn = new List<CardData>();
        int maxAttempts = count * 10; // prevent infinite loop if pool is small
        int attempts = 0;

        while (drawn.Count < count && attempts < maxAttempts)
        {
            attempts++;
            CardData card = PickRandomCard();

            if (card == null) break;

            // Avoid showing the exact same card definition twice in one round
            if (!drawn.Contains(card))
            {
                drawn.Add(card);

                // Update spawn tracking on both the card and its category
                card.timesSpawnedThisRun++;
                card.category.totalSpawnedThisRun++;
            }
        }

        if (drawn.Count < count)
            Debug.LogWarning($"[CardDatabase] Only drew {drawn.Count}/{count} cards. Consider adding more cards or adjusting weights.");

        return drawn;
    }

    /// <summary>
    /// Resets all spawn tracking counters — call this when starting a new run.
    /// </summary>
    public void ResetAllSpawnTracking()
    {
        foreach (CardCategory cat in allCategories)
            cat.totalSpawnedThisRun = 0;

        foreach (CardData card in allCards)
            card.timesSpawnedThisRun = 0;
    }
}