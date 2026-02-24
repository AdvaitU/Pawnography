using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages the card display area each round.
/// Spawns card prefab instances, populates them with CardData,
/// and clears them when moving to the next round.
/// Also updates the HUD text fields.
/// </summary>
public class CardUIManager : MonoBehaviour
{
    public static CardUIManager Instance { get; private set; }

    [Header("References — assign in Inspector")]
    [Tooltip("The card prefab with CardUI script attached.")]
    public GameObject cardPrefab;

    [Tooltip("The horizontal layout group that holds the 4 card slots.")]
    public Transform cardRowParent;

    [Tooltip("HUD text showing current round number.")]
    public TextMeshProUGUI roundText;

    [Tooltip("HUD text showing selections remaining.")]
    public TextMeshProUGUI selectionsText;

    [Tooltip("The Next Round button.")]
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
        // Hook up the Next Round button
        nextRoundButton.onClick.AddListener(OnNextRoundClicked);

        // Subscribe to RoundManager events
        RoundManager.Instance.onRoundStart.AddListener(OnRoundStart);
        RoundManager.Instance.onCardSelected.AddListener(OnCardSelected);

        // Trigger initial display
        OnRoundStart();
    }

    /// <summary>
    /// Called at the start of each round. Clears old cards and spawns new ones.
    /// </summary>
    private void OnRoundStart()
    {
        ClearCards();
        SpawnCards();
        UpdateHUD();
    }

    /// <summary>
    /// Destroys all current card UI instances.
    /// </summary>
    private void ClearCards()
    {
        foreach (CardUI card in activeCardUIs)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        activeCardUIs.Clear();
    }

    /// <summary>
    /// Spawns one card prefab per card in the current round and populates it.
    /// </summary>
    private void SpawnCards()
    {
        List<CardData> roundCards = RoundManager.Instance.currentRoundCards;

        foreach (CardData cardData in roundCards)
        {
            // Instantiate under the card row parent so the layout group positions it
            GameObject cardObj = Instantiate(cardPrefab, cardRowParent);
            CardUI cardUI = cardObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                cardUI.Populate(cardData);
                activeCardUIs.Add(cardUI);
            }
            else
            {
                Debug.LogWarning("[CardUIManager] Card prefab is missing a CardUI component.");
            }
        }
    }

    /// <summary>
    /// Updates the round number and selections remaining text in the HUD.
    /// </summary>
    public void UpdateHUD()
    {
        if (roundText != null)
            roundText.text = $"Round: {RoundManager.Instance.currentRound}";

        if (selectionsText != null)
        {
            int remaining = RoundManager.Instance.maxSelectionsPerRound
                            - RoundManager.Instance.selectionsThisRound;
            selectionsText.text = $"Selections: {RoundManager.Instance.selectionsThisRound} / {RoundManager.Instance.maxSelectionsPerRound}";
        }
    }

    /// <summary>
    /// Called whenever a card is selected — updates the HUD counter.
    /// </summary>
    private void OnCardSelected()
    {
        UpdateHUD();
    }

    /// <summary>
    /// Called when the Next Round button is pressed.
    /// </summary>
    private void OnNextRoundClicked()
    {
        RoundManager.Instance.EndRound();
    }
}