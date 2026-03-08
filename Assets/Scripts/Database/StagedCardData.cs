/*
 * ============================================================
 * SCRIPT:      StagedCardData.cs
 * GAMEOBJECT:  Not present on any GameObject.
 *              Plain C# class used at runtime only.
 * ------------------------------------------------------------
 * FUNCTION:
 *   Wraps a staged CardData with any metadata needed to execute
 *   it at round end. For Buyer cards this stores the chosen
 *   inventory item. For Seller cards this confirms the purchase
 *   was pre-approved via popup. Other card types use only the
 *   card reference with no extra metadata.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   RoundManager        -- stores List<StagedCardData> instead
 *                          of List<CardData>
 *   CardUI              -- creates StagedCardData on click
 *   CardInteractionManager -- reads metadata during execution
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:   None (data container only)
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Plain C# class � no MonoBehaviour, no runtime methods.
 * ============================================================
 */

using UnityEngine;

/// <summary>
/// Wraps a staged card with any pre-selection metadata needed
/// to execute its effect at round end.
/// </summary>
[System.Serializable]
public class StagedCardData
{
    public CardData card;

    [Tooltip("For Buyer cards � the inventory item the player chose to sell.")]
    public InventoryItem chosenItem;

    [Tooltip("For Conservator and Buyer cards � set when the player targets a " +
         "pending seller card instead of an existing inventory item. " +
         "Resolved to an InventoryItem at execution time after the seller runs.")]
    public StagedCardData pendingSellerTarget;

    [Tooltip("For Seller cards � true if the player confirmed the purchase via popup.")]
    public bool purchaseConfirmed;

    public StagedCardData(CardData card)
    {
        this.card = card;
        chosenItem = null;
        purchaseConfirmed = false;
    }
}