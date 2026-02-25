using UnityEngine;

/// <summary>
/// The central handler for all card selection interactions.
/// When a card is clicked, CardUI calls HandleCardSelected() here.
/// This manager decides what popup to show, what to do on confirm,
/// and whether to finalise or cancel the selection with RoundManager.
/// Attach to the GameManager GameObject.
/// </summary>
public class CardInteractionManager : MonoBehaviour
{
    public static CardInteractionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Entry point called by CardUI when the player clicks Select on a card.
    /// Routes to the correct handler based on category name.
    /// Add new cases here when new card categories are introduced.
    /// </summary>
    public void HandleCardSelected(CardData card, CardUI cardUI)
    {
        if (card.category == null)
        {
            Debug.LogWarning($"[CardInteractionManager] Card '{card.cardName}' has no category assigned.");
            return;
        }

        switch (card.category.categoryName)
        {
            case "Seller":
                HandleSeller(card, cardUI);
                break;

            case "Buyer":
                HandleBuyer(card, cardUI);
                break;

            case "Conservator":
                HandleConservator(card, cardUI);
                break;

            case "Contractor":
                HandleContractor(card, cardUI);
                break;

            case "Freelancer":
                HandleFreelancer(card, cardUI);
                break;

            // ── Add new card category cases here ──

            default:
                Debug.LogWarning($"[CardInteractionManager] No handler for category '{card.category.categoryName}'.");
                break;
        }
    }

    // ─────────────────────────────────────────────
    // SELLER
    // ─────────────────────────────────────────────

    private void HandleSeller(CardData card, CardUI cardUI)
    {
        // Check inventory space BEFORE showing the purchase popup
        if (!InventoryManager.Instance.HasSpace())
        {
            // Warehouse full — open warning and auto-open inventory so player can sell
            PopupManager.Instance.OpenWarehouseFullWarning(() =>
            {
                InventoryUI.Instance.OpenInventory();
            });
            return; // Do NOT register a selection
        }

        // Inventory has space — show purchase confirmation
        PopupManager.Instance.OpenSellerConfirmation(
            card,
            onConfirm: () =>
            {
                // Player confirmed purchase — register the selection and add item
                bool accepted = RoundManager.Instance.TrySelectCard(card);
                if (accepted)
                {
                    InventoryManager.Instance.TryAddItem(card);
                    cardUI.SetSelectedVisual(true);
                    CardUIManager.Instance.UpdateHUD();
                }
            },
            onCancel: () =>
            {
                // Player backed out — do nothing, selection is not registered
                Debug.Log($"[CardInteractionManager] Purchase of '{card.cardName}' cancelled.");
            }
        );
    }

    // ─────────────────────────────────────────────
    // BUYER
    // ─────────────────────────────────────────────

    private void HandleBuyer(CardData card, CardUI cardUI)
    {
        PopupManager.Instance.OpenBuyerItemSelection(
            card,
            InventoryManager.Instance.items,
            onItemChosen: (item) =>
            {
                // Player chose an item to sell
                bool accepted = RoundManager.Instance.TrySelectCard(card);
                if (accepted)
                {
                    InventoryManager.Instance.TryRemoveItem(item);
                    cardUI.SetSelectedVisual(true);
                    CardUIManager.Instance.UpdateHUD();

                    Debug.Log($"[CardInteractionManager] Sold '{item.cardName}' to buyer '{card.cardName}' " +
                              $"for {card.buyerOfferedPrice}g.");

                    // ── Gold will be added here when economy system is built ──
                }
            },
            onCancel: () =>
            {
                Debug.Log($"[CardInteractionManager] Buyer interaction cancelled.");
            }
        );
    }

    // ─────────────────────────────────────────────
    // CONSERVATOR / EXPERT
    // ─────────────────────────────────────────────

    private void HandleConservator(CardData card, CardUI cardUI)
    {
        PopupManager.Instance.OpenConservatorItemSelection(
            card,
            InventoryManager.Instance.items,
            onItemChosen: (item) =>
            {
                // Apply appraisal with accuracy modifier
                // Accuracy of 1.0 = perfect value, lower = random deviation
                float accuracy = card.appraisalAccuracy;
                float deviation = 1f - accuracy; // e.g. 0.2 accuracy means up to 20% off

                // Generate appraised value within the accuracy range
                float multiplier = UnityEngine.Random.Range(1f - deviation, 1f + deviation);
                int appraisedValue = Mathf.RoundToInt(item.sourceCard.itemTrueValue * multiplier);

                item.isAppraised = true;
                item.appraisedValue = appraisedValue;

                bool accepted = RoundManager.Instance.TrySelectCard(card);
                if (accepted)
                {
                    cardUI.SetSelectedVisual(true);
                    CardUIManager.Instance.UpdateHUD();
                }

                Debug.Log($"[CardInteractionManager] '{item.cardName}' appraised at {appraisedValue}g " +
                          $"(true value: {item.sourceCard.itemTrueValue}g, accuracy: {accuracy * 100}%).");

                // Refresh inventory UI if it's open
                InventoryUI.Instance.RefreshInventoryDisplay();
            },
            onCancel: () =>
            {
                Debug.Log($"[CardInteractionManager] Appraisal cancelled.");
            }
        );
    }

    // ─────────────────────────────────────────────
    // CONTRACTOR
    // ─────────────────────────────────────────────

    private void HandleContractor(CardData card, CardUI cardUI)
    {
        // Apply the upgrade immediately via ShopManager and get the before/after result string
        string upgradeResult = ShopManager.Instance.ApplyUpgrade(card);

        // Show confirmation popup with before → after values
        PopupManager.Instance.OpenContractorConfirmation(card, upgradeResult, onConfirm: () =>
        {
            bool accepted = RoundManager.Instance.TrySelectCard(card);
            if (accepted)
            {
                cardUI.SetSelectedVisual(true);
                CardUIManager.Instance.UpdateHUD();
            }
        });
    }

    // ─────────────────────────────────────────────
    // FREELANCER
    // ─────────────────────────────────────────────

    private void HandleFreelancer(CardData card, CardUI cardUI)
    {
        PopupManager.Instance.OpenFreelancerConfirmation(
            card,
            onConfirm: () =>
            {
                bool accepted = RoundManager.Instance.TrySelectCard(card);
                if (accepted)
                {
                    FreelancerManager.Instance.SendOutFreelancer(card);
                    cardUI.SetSelectedVisual(true);
                    CardUIManager.Instance.UpdateHUD();
                }
            },
            onCancel: () =>
            {
                Debug.Log($"[CardInteractionManager] Freelancer '{card.cardName}' not sent out.");
            }
        );
    }
}