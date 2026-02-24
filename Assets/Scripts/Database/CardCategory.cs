using UnityEngine;

/// <summary>
/// ScriptableObject representing a top-level card category (e.g. Seller, Buyer, Conservator).
/// Create one asset per category via Assets > Create > PawnShop > Card Category.
/// </summary>
[CreateAssetMenu(fileName = "NewCardCategory", menuName = "PawnShop/Card Category")]
public class CardCategory : ScriptableObject
{
    [Header("Identity")]
    public string categoryName;
    [TextArea] public string categoryDescription;

    [Header("Spawning")]
    [Tooltip("Master toggle — if false, no cards from this category will ever spawn.")]
    public bool canSpawn = true;

    [Tooltip("Relative weight used when randomly picking which category spawns next. " +
             "Higher = more likely. Does not need to sum to 100.")]
    [Range(0f, 100f)] public float spawnWeight = 20f;

    [Header("Tracking (Runtime — view in Play Mode)")]
    [Tooltip("How many cards from this category have spawned this run. Reset on new game.")]
    public int totalSpawnedThisRun = 0;

    // ── You can add future per-category fields here, e.g.:
    // public int maxPerRun;
    // public int minRoundToUnlock;
}