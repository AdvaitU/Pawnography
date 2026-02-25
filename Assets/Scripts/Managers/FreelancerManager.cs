using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks all active freelancers that have been sent out.
/// Each round, decrements their return countdown.
/// When a freelancer returns, generates a random item and adds it to inventory.
/// Attach to the GameManager GameObject.
/// </summary>
public class FreelancerManager : MonoBehaviour
{
    public static FreelancerManager Instance { get; private set; }

    [Header("Active Freelancers — view in Play Mode")]
    public List<ActiveFreelancer> activeFreelancers = new List<ActiveFreelancer>();

    [Header("Events")]
    [Tooltip("Fired when a freelancer returns with an item. Subscribe in UI to notify the player.")]
    public UnityEvent<ActiveFreelancer> onFreelancerReturned;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Decrement freelancer countdowns at the start of each new round
        RoundManager.Instance.onRoundStart.AddListener(TickFreelancers);
    }

    /// <summary>
    /// Sends a freelancer out. Called by CardInteractionManager when a freelancer card
    /// is confirmed.
    /// </summary>
    public void SendOutFreelancer(CardData card)
    {
        ActiveFreelancer freelancer = new ActiveFreelancer(card);
        activeFreelancers.Add(freelancer);
        Debug.Log($"[FreelancerManager] '{card.cardName}' sent out. Returns in {card.roundsToReturn} rounds.");
    }

    /// <summary>
    /// Called each round start. Decrements all active freelancer countdowns.
    /// Any that reach 0 are resolved and removed.
    /// </summary>
    private void TickFreelancers()
    {
        List<ActiveFreelancer> returned = new List<ActiveFreelancer>();

        foreach (ActiveFreelancer f in activeFreelancers)
        {
            f.roundsRemaining--;
            Debug.Log($"[FreelancerManager] '{f.cardName}' returns in {f.roundsRemaining} round(s).");

            if (f.roundsRemaining <= 0)
                returned.Add(f);
        }

        foreach (ActiveFreelancer f in returned)
            ResolveFreelancer(f);
    }

    /// <summary>
    /// Resolves a returned freelancer — generates a random item value and
    /// attempts to add it to the player's inventory.
    /// </summary>
    private void ResolveFreelancer(ActiveFreelancer freelancer)
    {
        activeFreelancers.Remove(freelancer);

        // Generate a random item value within the freelancer's range
        int itemValue = Random.Range(freelancer.minItemValue, freelancer.maxItemValue + 1);
        freelancer.returnedItemValue = itemValue;

        Debug.Log($"[FreelancerManager] '{freelancer.cardName}' returned with an item worth {itemValue}g.");

        // Create a runtime CardData placeholder for the found item
        // We create a temporary ScriptableObject instance to represent the found item
        CardData foundItem = ScriptableObject.CreateInstance<CardData>();
        foundItem.cardName = $"Item (from {freelancer.cardName})";
        foundItem.itemTrueValue = itemValue;
        foundItem.itemBuyCost = 0; // Acquired for free
        foundItem.valueIsHidden = false; // Freelancer already knows what it is

        bool added = InventoryManager.Instance.TryAddItem(foundItem);

        if (!added)
            Debug.LogWarning($"[FreelancerManager] Could not add returned item — warehouse full.");

        // Fire event so UI can notify the player
        onFreelancerReturned?.Invoke(freelancer);
    }
}

/// <summary>
/// Runtime data for a freelancer that has been sent out.
/// Tracks countdown and the result when they return.
/// </summary>
[System.Serializable]
public class ActiveFreelancer
{
    public string cardName;
    public int roundsRemaining;
    public int minItemValue;
    public int maxItemValue;
    public int returnedItemValue; // Set when they return

    public ActiveFreelancer(CardData card)
    {
        cardName = card.cardName;
        roundsRemaining = card.roundsToReturn;
        minItemValue = card.freelancerMinItemValue;
        maxItemValue = card.freelancerMaxItemValue;
    }
}