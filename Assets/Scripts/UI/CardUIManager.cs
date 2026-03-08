/*
 * ============================================================
 * SCRIPT:      CardUIManager.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Manages the card display area each round. Subscribes to
 *   RoundManager events to clear and respawn cards. Handles
 *   the Next Round button � which now calls
 *   RoundManager.ProcessAndEndRound() to execute all staged
 *   selections before advancing. Updates the HUD to show
 *   how many cards are currently staged.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardInteractionManager -- calls UpdateHUD() after card
 *                          interaction popups complete
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   UpdateHUD()         --> Called by CardUI.OnCardClicked()
 *                          and CardInteractionManager after
 *                          any interaction
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Start() -- hooks button and subscribes to events.
 *   No Update(). Uses Instantiate/Destroy per round � consider
 *   object pooling if card counts grow large.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUIManager : MonoBehaviour
{
    public static CardUIManager Instance { get; private set; }

    [Header("References � assign in Inspector")]
    public GameObject cardPrefab;
    public Transform cardRowParent;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI selectionsText;
    public Button nextRoundButton;
    [Tooltip("HUD text showing current gold balance.")]

    [Header("Runtime State")]
    public List<CardUI> activeCardUIs = new List<CardUI>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        nextRoundButton.onClick.AddListener(OnNextRoundClicked);

        RoundManager.Instance.onRoundStart.AddListener(OnRoundStart);
        RoundManager.Instance.onStagedSelectionsChanged.AddListener(UpdateHUD);
        EconomyManager.Instance.onGoldChanged.AddListener(UpdateHUD);

        OnRoundStart();
    }

    private void OnRoundStart()
    {
        // Auction rounds are handled entirely by AuctionManager and AuctionUI.
        if (RoundManager.Instance.isBossRound) return;  // Don't fire the rest of the events if it is an Auction round

        ClearCards();
        SpawnCards();
        UpdateHUD();
    }

    private void ClearCards()
    {
        foreach (CardUI card in activeCardUIs)
            if (card != null) Destroy(card.gameObject);

        activeCardUIs.Clear();
    }

    private void SpawnCards()
    {
        foreach (CardData cardData in RoundManager.Instance.currentRoundCards)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardRowParent);
            CardUI cardUI = cardObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                cardUI.Populate(cardData);
                activeCardUIs.Add(cardUI);
            }
            else
            {
                Debug.LogWarning("[CardUIManager] Card prefab missing CardUI component.");
            }
        }

        // Force the layout group to immediately calculate positions
        // so CardVisualController can cache correct rest positions
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
            cardRowParent.GetComponent<RectTransform>()
        );
    }

    /// <summary>
    /// Updates the round number and staged selection count in the HUD.
    /// </summary>
    public void UpdateHUD()
    {
        if (roundText != null)
            roundText.text = $"Round: {RoundManager.Instance.currentRound}";

        if (selectionsText != null)
            selectionsText.text = $"Selected: {RoundManager.Instance.stagedCards.Count}" +
                                  $" / {RoundManager.Instance.maxSelectionsPerRound}";

        // Keep ShopStatsUI in sync whenever the HUD updates
        if (ShopStatsUI.Instance != null)
            ShopStatsUI.Instance.RefreshAllStats();
    }

    /// <summary>
    /// Called when the Next Round button is clicked.
    /// Triggers processing of all staged card selections before advancing.
    /// </summary>
    private void OnNextRoundClicked()
    {
        RoundManager.Instance.ProcessAndEndRound();
    }
}