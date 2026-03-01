using UnityEngine;

/*
 * ============================================================
 * SCRIPT:      CardCategory.cs
 * GAMEOBJECT:  Not present on any GameObject.
 *              Exists as a ScriptableObject asset in
 *              Assets/ScriptableObjects/Categories/
 * ------------------------------------------------------------
 * FUNCTION:
 *   Defines a top-level card category (e.g. Seller, Buyer,
 *   Conservator, Contractor, Freelancer). Stores the category
 *   name, spawn weight, canSpawn toggle, and a runtime counter
 *   tracking how many cards from this category have spawned.
 *   One asset per category.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardData            -- holds a reference to its parent category
 *   CardDatabase        -- reads canSpawn and spawnWeight in
 *                          PickRandomCategory()
 *   CardUI              -- reads categoryName to set banner colour
 *                          and label text
 *   CardInteractionManager -- reads categoryName to route card
 *                          selection to the correct handler
 *   ShopManager         -- toggles canSpawn in UnlockCategory()
 *                          and registers categories in
 *                          unlockedCategories list
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:   None (data container only)
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No runtime methods (no Start, Awake, Update).
 *   Pure data asset — no optimisation concerns.
 * ============================================================
 */

[CreateAssetMenu(fileName = "NewCardCategory", menuName = "PawnShop/Card Category")]
public class CardCategory : ScriptableObject
{
    [Header("General Scope")]
    public string categoryName;
    [TextArea] public string categoryDescription;

    [Header("Spawning")]
    [Tooltip("If false, no cards from this category will ever spawn.")]
    public bool canSpawn = true;

    [Tooltip("Relative weight used when randomly picking which category spawns next. " +
             "Does not need to sum to 100.")]
    [Range(0f, 100f)] public float spawnWeight = 20f;

    [Header("Tracking")]
    [Tooltip("How many cards from this category have spawned this run. Reset on new game.")]
    public int totalSpawnedThisRun = 0;

    // public int maxPerRun;
    // public int minRoundToUnlock;
}