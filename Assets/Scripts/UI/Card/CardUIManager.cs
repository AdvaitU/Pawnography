/*
 * ============================================================
 * SCRIPT:      CardUIManager.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Manages the card display area each round. Subscribes to
 *   RoundManager events to clear and respawn cards. Handles
 *   the Next Round button, which calls
 *   RoundManager.ProcessAndEndRound() to execute all staged
 *   selections before advancing.
 *
 *   Round number, gold, and selections text have moved into
 *   ShopPanel (owned by ShopStatsUI). UpdateHUD() delegates
 *   all stat display to ShopStatsUI.RefreshAllStats().
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardInteractionManager -- calls UpdateHUD() after card
 *                             interactions complete
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   UpdateHUD()  --> CardUI.OnCardClicked() and
 *                    CardInteractionManager after interactions
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). Uses Instantiate/Destroy per round.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardUIManager : MonoBehaviour
{
    public static CardUIManager Instance { get; private set; }

    [Header("References — assign in Inspector")]
    public GameObject cardPrefab;
    public Transform cardRowParent;
    public Button nextRoundButton;

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
        if (RoundManager.Instance.isBossRound) return;

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

        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
            cardRowParent.GetComponent<RectTransform>());
    }

    /// <summary>
    /// Delegates all stat display to ShopStatsUI, which now owns
    /// the round, gold, and selections text fields inside ShopPanel.
    /// </summary>
    public void UpdateHUD()
    {
        if (ShopStatsUI.Instance != null)
            ShopStatsUI.Instance.RefreshAllStats();
    }

    private void OnNextRoundClicked()
    {
        RoundManager.Instance.ProcessAndEndRound();
    }
}