/*
 * ============================================================
 * SCRIPT:      CardDatabase.cs
 * GAMEOBJECT:  GameManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Central registry holding all CardCategory and CardData
 *   assets. Runs the two-step weighted random spawn algorithm
 *   (step 1: pick category by weight, step 2: pick card within
 *   category by weight). Draws a hand of cards each round and
 *   tracks spawn counts. Resets tracking counters on Awake to
 *   prevent stale values persisting between Editor Play sessions.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   RoundManager        -- calls DrawRoundCards() at the start
 *                          of each round
 *   ShopManager         -- reads allCards in UnlockSubCategory()
 *                          and allCategories in Start()
 *   CardDatabaseEditor  -- reads allCards and allCategories for
 *                          auto-populate buttons
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   DrawRoundCards()    --> Called by RoundManager.StartNewRound()
 *                          to get the hand of cards for a round
 *   BuildLookup()       --> Called internally on Awake; can be
 *                          called manually if cards are added
 *                          at runtime
 *   ResetAllSpawnTracking() --> Called on Awake to clear stale
 *                          counters;
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Awake() -- builds the category→cards dictionary lookup and
 *   resets spawn counters. Runs once. No Update().
 *   BuildLookup() iterates all cards on startup — Could pose 
 *   problems if the card pool becomes very large (500+)
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{

    // MEMBERS ==============================================================================================================
    public static CardDatabase Instance { get; private set; }   // Creates an instance of the CardDatabase in the scene

    [Header("Database — Use Editor Script to Auto-Populate")]
    [Tooltip("All CardCategory ScriptableObject assets go here." + "" +
        "Objects not here will be ignored by the game")]
    public List<CardCategory> allCategories = new List<CardCategory>();

    [Tooltip("All CardData ScriptableObject assets go here." +
        "Objects not here will be ignored by the game")]
    public List<CardData> allCards = new List<CardData>();

    // ── Cached lookup: category → cards that belong to it ──
    private Dictionary<CardCategory, List<CardData>> cardsByCategory;  // Used in BuildLookup() - Dictionary with CardCategory as keys and Lists of CardData objects by category as the values to optimise the sorting in the later methods.

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);  // Puts the GameManager in DontDestroyOnLoad

        BuildLookup();

        // Always reset spawn tracking when the game starts. ScriptableObject values persist between Editor Play sessions, so stale counters from a previous session can cause spawn issues.
        ResetAllSpawnTracking();  // Uncomment to make them persist if required.
    }

    // METHODS ==============================================================================================================

    // BuildLookup() - Builds a fast dictionary lookup from category → list of eligible cards.
    // Called once on Awake; Only call again manually if you add cards at runtime (rare case scenario).
    // Two step process to create Categories as keys and Lists of cards by category as values. Dictionary ensures the second step of the card selection process can use only cards with the right category key to make it a lot more efficient.

    /// <summary>
    /// Builds the internal category→cards dictionary for fast lookup.
    /// Called once on Awake. Only call again manually if cards are
    /// added to allCards at runtime.
    /// </summary>
    public void BuildLookup()
    {
        cardsByCategory = new Dictionary<CardCategory, List<CardData>>();

        foreach (CardCategory cat in allCategories)  // Looks at allCategories and creates List of CardData for each
        {
            cardsByCategory[cat] = new List<CardData>();
        }

        foreach (CardData card in allCards)      // Looks at all the cards in allCards and adds them to the relevant key in cardsByCategory 
        {
            // Guard against null card entries in the list
            if (card == null)
            {
                Debug.LogWarning("[CardDatabase] Null entry found in allCards list — check the CardDatabase Inspector for empty slots.");
                continue;
            }

            if (card.category != null && cardsByCategory.ContainsKey(card.category))
            {
                cardsByCategory[card.category].Add(card);
            }
            else
            {
                Debug.LogWarning($"[CardDatabase] Card '{card.cardName}' has a missing or unregistered category. It will not spawn.");
            }
        }
    }

    // PickRandomCategory() - Step 1 rounds spawning random cards -----------------------------------------------------------------------------
    // Uses the dictionary created in BuildLookup() to quickly choose random category cards.
    // Returns a CardCategory for PickRandomCardFromCategory() to use.
    // This is Step 1 of the 2 step algorithm - Picks a category using weighted random selection.
    // Only categories with canSpawn = true and spawnWeight > 0 are eligible.
    // Returns null if no eligible category exists.

    /// <summary>
    /// Step 1 of the two-step spawn algorithm. Picks a CardCategory
    /// using weighted random selection. Only categories with
    /// canSpawn = true and spawnWeight > 0 are eligible.
    /// Returns null if no eligible category exists.
    /// </summary>
    public CardCategory PickRandomCategory()
    {
        
        List<CardCategory> eligible = new List<CardCategory>(); // Build the eligible pool for sorting
        float totalWeight = 0f;   // Since the total weight does not need to add to a 100.

        // Creat eligible categories list
        foreach (CardCategory cat in allCategories)  // Categories are inserted into the Inspector manually or using editor script. Only these categories will be looked at.
        {
            if (cat.canSpawn && cat.spawnWeight > 0f)
            {
                eligible.Add(cat);
                totalWeight += cat.spawnWeight;
            }
        }

        // Log warning if no categories are eligible i.e. none of them have a spawn rate above 0. 
        if (eligible.Count == 0)
        {
            Debug.LogWarning("[CardDatabase] No eligible categories to spawn from.");
            return null;
        }

        // This is the random roll - Step 1 of the algorithm. (Weighted random roll)
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        // Weighted Roll Explanation -------------------------------------------------
        // For each eligible category, spawn weight is added to cumulative. If the random roll is less than (or equal to) the cumulative so far, 
        // For example, there are 5 categories with each having a spawn weight of 20f. The random roll is 15f. For the first category, the roll < cumulative so far, so it is returned.
        // If instead the roll is 80f, then the first 3 categories won't be returned at all - Only the 4th one will be. 
        // Hence if all spawn weights are equal, there is actually a random 1/5 chance for each category to be returned.

        // If instead, the 5 categories have spawn weights of 10f, 20f, 30f, 40f, and 50f respectively, the roll will be bwtween 0f and 150f.
        // If the roll is 5f, the first category will be returned. But on a scale from 0f to 150f, the chance of the roll being under 10f is 1/15.

        // So the categories effectively creates stacks of windows based on their weight. The higher the spawn weight, the larger the window. 
        // And because roll gives an equal chance if spawning anywhere in the total weight window, the larger each category's window, the higher the rate of it spawning.
        foreach (CardCategory cat in eligible)
        {
            cumulative += cat.spawnWeight;
            if (roll <= cumulative)
                return cat;
        }

        return eligible[^1]; // Fallback (should not normally be reached)
    }

    // PickRandomCardFromCategory() - Step 2 of the algorithm once category is chosen --------------------------------------------------------------
    // Given a category, picks a specific card using weighted random selection.
    // Only cards with canSpawn = true and spawnWeight > 0 are eligible.
    // Returns null if no eligible card exists in the category.
    // Called using the CardCategory chosen in Step 1 as an argument.

    /// <summary>
    /// Step 2 of the two-step spawn algorithm. Given a category,
    /// picks a specific CardData using weighted random selection.
    /// Only cards with canSpawn = true and spawnWeight > 0 are eligible.
    /// Returns null if no eligible card exists in the category.
    /// </summary>
    public CardData PickRandomCardFromCategory(CardCategory category)
    {
        if (!cardsByCategory.ContainsKey(category))    // Failsafe - If no cards of chosen category are present in the Dictionary i.e. Dictionary does not have a key of that CardCategory, return null and log a warning.
        {
            Debug.LogWarning($"[CardDatabase] Category '{category.categoryName}' not found in lookup.");
            return null;
        }

        List<CardData> eligible = new List<CardData>();
        float totalWeight = 0f;

        // Create a list of eligible cards from the dictionary
        foreach (CardData card in cardsByCategory[category])
        {
            if (card.canSpawn && card.spawnWeight > 0f)
            {
                eligible.Add(card);
                totalWeight += card.spawnWeight;
            }
        }

        if (eligible.Count == 0)  // Failsafe
        {
            Debug.LogWarning($"[CardDatabase] No eligible cards in category '{category.categoryName}'.");
            return null;
        }

        // Same weighted roll as Category selection method
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

    // PickRandomCard() ------------------------------------------------------------------------
    // Convenience method that simply runs the above two methods one by one
    // Failsafe to return null if no category is chosen by PickRandomCategory
    // Can be called as many times as the cards to spawn by DrawRoundCards(noOfCards)

    /// <summary>
    /// Convenience wrapper that runs PickRandomCategory() then
    /// PickRandomCardFromCategory(). Returns null if no category
    /// or no card is found.
    /// </summary>
    public CardData PickRandomCard()
    {
        CardCategory category = PickRandomCategory();
        if (category == null) return null;
        return PickRandomCardFromCategory(category);
    }

    // DrawRoundCards() --------------------------------------------------------------------------
    // Calls PickRandomCard n times with n being the int count supplied to it as a parameter.
    // Returns a list of CardData objects to then be rendered.
    // Called by RoundManager() who then supplies CardData objects to UI and Interaction Handlers as well
    // Default count is 3 i.e. starting count of the game.

    /// <summary>
    /// Draws a hand of n cards by calling PickRandomCard() n times.
    /// Prevents duplicates if the eligible pool is large enough.
    /// Logs a warning if fewer than count cards could be drawn.
    /// Called by RoundManager.StartNewRound().
    /// </summary>
    public List<CardData> DrawRoundCards(int count = 3)
    {
        List<CardData> drawn = new List<CardData>();
        int maxAttempts = count * 10;   // Failsafe - How many attempts will be made to draw cards. Increase or make method maxAttempts agnostic to always draw cards.
        int attempts = 0;

        // Count how many eligible cards exist in total
        int eligibleCount = 0;
        foreach (CardData card in allCards)
            if (card != null && card.canSpawn && card.spawnWeight > 0f) eligibleCount++;

        while (drawn.Count < count && attempts < maxAttempts)
        {
            attempts++;
            CardData card = PickRandomCard();

            if (card == null)  // Failsafe
            {
                Debug.LogWarning("[CardDatabase] PickRandomCard() returned null.");
                break;
            }

            // Only enforce duplicate prevention if the pool is large enough.
            // If the eligible pool is smaller than the requested count,
            // duplicates are allowed as a fallback to always fill the hand.
            if (eligibleCount >= count)
            {
                if (!drawn.Contains(card))
                {
                    drawn.Add(card);
                    card.timesSpawnedThisRun++;
                    card.category.totalSpawnedThisRun++;
                }
            }
            else
            {
                drawn.Add(card);
                card.timesSpawnedThisRun++;
                card.category.totalSpawnedThisRun++;
            }
        }

        if (drawn.Count < count)
            Debug.LogWarning($"[CardDatabase] Only drew {drawn.Count}/{count} cards. Consider adding more cards or adjusting weights.");

        return drawn;
    }

    // ResetAllSpawnTracking -------------------------------------------------------
    // Resets the totalSpawnedThisRun to 0 in Awake() i.e. when new run begins

    /// <summary>
    /// Resets timesSpawnedThisRun on all cards and totalSpawnedThisRun
    /// on all categories to 0. Called on Awake to prevent stale
    /// ScriptableObject values persisting between Editor Play sessions.
    /// </summary>
    public void ResetAllSpawnTracking()
    {
        foreach (CardCategory cat in allCategories)
            cat.totalSpawnedThisRun = 0;

        foreach (CardData card in allCards)
            card.timesSpawnedThisRun = 0;
    }
}