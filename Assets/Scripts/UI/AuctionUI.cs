/*
 * ============================================================
 * SCRIPT:      AuctionUI.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Owns all auction UI presentation. Manages three sequential
 *   phases: (1) Item selection popup — player chooses up to N
 *   items for the lot. (2) Cutscene — auction house background,
 *   bidder sprites, animated bid bubbles with TMP amounts, and
 *   a skip button. (3) Results popup — shows sold amount vs
 *   threshold, doubles as the Game Over screen on fail.
 *   Subscribes to AuctionManager events to know when to
 *   transition between phases.
 *   Background sprite and bidder sprites are assigned in the
 *   Inspector and swapped at runtime.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   AuctionManager      -- fires onAuctionBegin, onLotConfirmed,
 *                          onAuctionResolved which this script
 *                          subscribes to
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   None — all entry points are via AuctionManager events.
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Cutscene uses Coroutines for bid sequencing.
 *   Bidder sprites are pooled from a fixed Inspector list.
 *   No Update() — all state transitions are coroutine-driven.
 * ============================================================
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AuctionUI : MonoBehaviour
{
    public static AuctionUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    // BACKGROUND
    // ─────────────────────────────────────────────

    [Header("Background")]
    [Tooltip("The main scene background Image component to swap during the auction.")]
    public Image sceneBackground;

    [Tooltip("The normal gameplay background sprite.")]
    public Sprite normalBackgroundSprite;

    [Tooltip("The auction house background sprite shown during the auction round.")]
    public Sprite auctionBackgroundSprite;

    // ─────────────────────────────────────────────
    // ITEM SELECTION POPUP
    // ─────────────────────────────────────────────

    [Header("Item Selection Popup")]
    [Tooltip("Root panel for the item selection popup.")]
    public GameObject selectionPopupPanel;

    [Tooltip("Container for the item slot buttons in the selection popup.")]
    public Transform selectionSlotContainer;

    [Tooltip("Prefab for a single selectable item slot in the auction selection popup.")]
    public GameObject auctionSlotPrefab;

    [Tooltip("Confirm button that submits the lot selection.")]
    public Button confirmLotButton;

    [Tooltip("Label on the confirm button — updated to show selection count.")]
    public TextMeshProUGUI confirmButtonLabel;

    [Tooltip("Text showing how many items have been selected vs the max.")]
    public TextMeshProUGUI selectionCountText;

    // ─────────────────────────────────────────────
    // CUTSCENE
    // ─────────────────────────────────────────────

    [Header("Cutscene")]
    [Tooltip("Root panel shown during the cutscene.")]
    public GameObject cutscenePanel;

    [Tooltip("Placeholder sprites for bidder characters. " +
             "Assign at least as many as maxBids. " +
             "Will be replaced with final art later.")]
    public List<Sprite> bidderSprites = new List<Sprite>();

    [Tooltip("Prefab for a bidder — contains an Image for the sprite and a " +
             "child BidBubble panel with a TextMeshProUGUI for the bid amount.")]
    public GameObject bidderPrefab;

    [Tooltip("Container inside the cutscene panel where bidders are spawned.")]
    public RectTransform bidderContainer;

    [Tooltip("Button in the bottom right corner to skip the cutscene.")]
    public Button skipButton;

    // ─────────────────────────────────────────────
    // RESULTS POPUP / GAME OVER
    // ─────────────────────────────────────────────

    [Header("Results / Game Over Popup")]
    [Tooltip("Root panel for the results screen. Doubles as Game Over screen.")]
    public GameObject resultsPanel;

    [Tooltip("Title text — 'Auction Complete' on pass, 'Game Over' on fail.")]
    public TextMeshProUGUI resultsTitleText;

    [Tooltip("Shows the sold amount vs threshold.")]
    public TextMeshProUGUI resultsSoldText;

    [Tooltip("Shows pass or fail verdict.")]
    public TextMeshProUGUI resultsVerdictText;

    [Tooltip("Button to continue to next round. Only shown on pass.")]
    public Button continueButton;

    [Tooltip("Button to return to main menu or restart. Only shown on fail.")]
    public Button gameOverButton;

    // ─────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────

    private List<InventoryItem> selectedItems = new List<InventoryItem>();
    private List<GameObject> activeSelectionSlots = new List<GameObject>();
    private List<GameObject> activeBidders = new List<GameObject>();
    private bool cutsceneSkipped = false;

    // ─────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        AuctionManager.Instance.onAuctionBegin.AddListener(OnAuctionBegin);
        AuctionManager.Instance.onLotConfirmed.AddListener(OnLotConfirmed);
        AuctionManager.Instance.onAuctionResolved.AddListener(OnAuctionResolved);

        confirmLotButton.onClick.AddListener(OnConfirmLotClicked);
        skipButton.onClick.AddListener(OnSkipClicked);
        continueButton.onClick.AddListener(OnContinueClicked);
        gameOverButton.onClick.AddListener(OnGameOverClicked);

        // Start hidden
        selectionPopupPanel.SetActive(false);
        cutscenePanel.SetActive(false);
        resultsPanel.SetActive(false);
        skipButton.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // PHASE 1 — ITEM SELECTION
    // ─────────────────────────────────────────────

    /// <summary>
    /// Subscribed to AuctionManager.onAuctionBegin.
    /// Swaps the background and opens the item selection popup.
    /// </summary>
    private void OnAuctionBegin()
    {
        SwapBackground(toAuction: true);
        selectedItems.Clear();
        OpenSelectionPopup();
    }

    /// <summary>
    /// Builds and displays the item selection popup from current inventory.
    /// </summary>
    private void OpenSelectionPopup()
    {
        // Clear previous slots
        foreach (GameObject slot in activeSelectionSlots)
            Destroy(slot);
        activeSelectionSlots.Clear();

        List<InventoryItem> inventory = InventoryManager.Instance.items;

        foreach (InventoryItem item in inventory)
        {
            GameObject slotObj = Instantiate(auctionSlotPrefab, selectionSlotContainer);
            activeSelectionSlots.Add(slotObj);
            SetupSelectionSlot(slotObj, item);
        }

        RefreshSelectionUI();
        selectionPopupPanel.SetActive(true);
    }

    /// <summary>
    /// Configures a single selection slot for the given inventory item.
    /// Hooks up the toggle click to select or deselect the item.
    /// </summary>
    private void SetupSelectionSlot(GameObject slotObj, InventoryItem item)
    {
        // Reuse WarehouseSlot component for art and name references
        WarehouseSlot slot = slotObj.GetComponent<WarehouseSlot>();
        Button slotButton = slotObj.GetComponent<Button>();
        GameObject selectedOverlay = slotObj.transform.Find("SelectedOverlay")?.gameObject;

        if (slot != null)
        {
            bool hasArt = item.sourceCard != null && item.sourceCard.cardArt != null;
            if (slot.artImage != null)
            {
                slot.artImage.sprite = hasArt ? item.sourceCard.cardArt : null;
                slot.artImage.color = hasArt ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            }
            if (slot.nameText != null)
            {
                string valueStr = item.isAppraised
                    ? $"{item.appraisedValue}g"
                    : $"{item.purchasePrice}g (unappraised)";
                slot.nameText.text = $"{item.cardName}\n{valueStr}";
            }
        }

        if (selectedOverlay != null)
            selectedOverlay.SetActive(false);

        if (slotButton != null)
        {
            slotButton.onClick.AddListener(() =>
            {
                ToggleItemSelection(item, selectedOverlay);
            });
        }
    }

    /// <summary>
    /// Toggles an item's selection state in the lot.
    /// Enforces the maxLotSize cap — new selections are blocked if full.
    /// </summary>
    private void ToggleItemSelection(InventoryItem item, GameObject overlay)
    {
        if (selectedItems.Contains(item))
        {
            selectedItems.Remove(item);
            if (overlay != null) overlay.SetActive(false);
        }
        else
        {
            if (selectedItems.Count >= AuctionManager.Instance.maxLotSize)
            {
                Debug.Log("[AuctionUI] Max lot size reached.");
                return;
            }
            selectedItems.Add(item);
            if (overlay != null) overlay.SetActive(true);
        }

        RefreshSelectionUI();
    }

    /// <summary>
    /// Updates the selection count text and confirm button label.
    /// </summary>
    private void RefreshSelectionUI()
    {
        int max = AuctionManager.Instance.maxLotSize;
        int current = selectedItems.Count;

        if (selectionCountText != null)
            selectionCountText.text = $"Selected: {current} / {max}";

        if (confirmButtonLabel != null)
            confirmButtonLabel.text = current == 0
                ? "Send Empty Lot"
                : $"Confirm Lot ({current} item{(current == 1 ? "" : "s")})";
    }

    /// <summary>
    /// Called when the confirm lot button is clicked.
    /// Closes the selection popup and passes the selection to AuctionManager.
    /// </summary>
    private void OnConfirmLotClicked()
    {
        selectionPopupPanel.SetActive(false);
        AuctionManager.Instance.ConfirmLotSelection(selectedItems);
    }

    // ─────────────────────────────────────────────
    // PHASE 2 — CUTSCENE
    // ─────────────────────────────────────────────

    /// <summary>
    /// Subscribed to AuctionManager.onLotConfirmed.
    /// Begins the cutscene coroutine.
    /// </summary>
    private void OnLotConfirmed()
    {
        cutsceneSkipped = false;
        cutscenePanel.SetActive(true);
        skipButton.gameObject.SetActive(true);
        StartCoroutine(PlayBidSequence());
    }

    /// <summary>
    /// Plays the bid sequence coroutine. Generates between minBids and maxBids
    /// bids, each increasing toward the final selling amount, with random delays.
    /// Spawns a bidder sprite and bid bubble for each bid.
    /// Ends by calling AuctionManager.ResolveAuction() after a 1-second delay.
    /// </summary>
    private IEnumerator PlayBidSequence()
    {
        int finalAmount = AuctionManager.Instance.GetFinalSellingAmount();
        int bidCount = Random.Range(AuctionManager.Instance.minBids,
                                    AuctionManager.Instance.maxBids + 1);

        // Generate all bid amounts upfront so the sequence is predetermined
        List<int> bids = GenerateBidSequence(finalAmount, bidCount);

        // Clear any previous bidders
        foreach (GameObject b in activeBidders) Destroy(b);
        activeBidders.Clear();

        foreach (int bidAmount in bids)
        {
            if (cutsceneSkipped) break;

            SpawnBidder(bidAmount);

            float delay = Random.Range(AuctionManager.Instance.minBidDelay,
                                       AuctionManager.Instance.maxBidDelay);
            yield return new WaitForSeconds(delay);
        }

        // Final pause before results
        if (!cutsceneSkipped)
            yield return new WaitForSeconds(1f);

        EndCutscene();
    }

    /// <summary>
    /// Generates a list of bid amounts that rise from a starting value
    /// to finalAmount. Each intermediate bid is a random rounded value
    /// (ending in 0 or 5) between the previous bid and finalAmount.
    /// The first bid starts between the item base value and finalAmount.
    /// The last bid is always exactly finalAmount.
    /// </summary>
    private List<int> GenerateBidSequence(int finalAmount, int bidCount)
    {
        List<int> bids = new List<int>();

        if (finalAmount <= 0 || bidCount <= 0)
        {
            bids.Add(0);
            return bids;
        }

        // First bid: random rounded value between 50% and 90% of finalAmount
        int firstBidRaw = Mathf.RoundToInt(Random.Range(finalAmount * 0.5f,
                                                         finalAmount * 0.9f));
        bids.Add(RoundToNearestFiveOrZero(firstBidRaw));

        // Intermediate bids: each between previous bid and finalAmount
        for (int i = 1; i < bidCount - 1; i++)
        {
            int prev = bids[bids.Count - 1];
            if (prev >= finalAmount) break;

            int nextRaw = Mathf.RoundToInt(Random.Range(prev, finalAmount));
            bids.Add(RoundToNearestFiveOrZero(nextRaw));
        }

        // Final bid is always the exact finalAmount
        bids.Add(finalAmount);

        return bids;
    }

    /// <summary>
    /// Spawns a bidder prefab at a random position within the bidder container
    /// and sets its bid bubble text to the given amount.
    /// </summary>
    private void SpawnBidder(int bidAmount)
    {
        if (bidderPrefab == null || bidderContainer == null) return;

        GameObject bidderObj = Instantiate(bidderPrefab, bidderContainer);
        activeBidders.Add(bidderObj);

        // Random position within the container bounds
        float halfW = bidderContainer.rect.width * 0.5f;
        float halfH = bidderContainer.rect.height * 0.5f;
        RectTransform rt = bidderObj.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = new Vector2(
                Random.Range(-halfW * 0.8f, halfW * 0.8f),
                Random.Range(-halfH * 0.8f, halfH * 0.8f));

        // Assign a random bidder sprite if available
        Image bidderImage = bidderObj.GetComponentInChildren<Image>();
        if (bidderImage != null && bidderSprites.Count > 0)
            bidderImage.sprite = bidderSprites[Random.Range(0, bidderSprites.Count)];

        // Set bid bubble text
        TextMeshProUGUI bidText = bidderObj.GetComponentInChildren<TextMeshProUGUI>();
        if (bidText != null)
            bidText.text = $"{bidAmount}g";
    }

    /// <summary>
    /// Called when the skip button is clicked. Sets the skip flag and
    /// immediately ends the cutscene.
    /// </summary>
    private void OnSkipClicked()
    {
        cutsceneSkipped = true;
        StopAllCoroutines();
        EndCutscene();
    }

    /// <summary>
    /// Cleans up the cutscene panel and triggers auction resolution
    /// after a 1-second delay.
    /// </summary>
    private void EndCutscene()
    {
        skipButton.gameObject.SetActive(false);
        cutscenePanel.SetActive(false);

        foreach (GameObject b in activeBidders) Destroy(b);
        activeBidders.Clear();

        StartCoroutine(DelayedResolve());
    }

    private IEnumerator DelayedResolve()
    {
        yield return new WaitForSeconds(1f);
        AuctionManager.Instance.ResolveAuction();
    }

    // ─────────────────────────────────────────────
    // PHASE 3 — RESULTS / GAME OVER
    // ─────────────────────────────────────────────

    /// <summary>
    /// Subscribed to AuctionManager.onAuctionResolved.
    /// Shows the results popup configured for pass or fail.
    /// </summary>
    private void OnAuctionResolved(bool passed)
    {
        int sold = AuctionManager.Instance.GetFinalSellingAmount();
        int threshold = EconomyManager.Instance.currentThreshold;

        resultsTitleText.text = passed ? "Auction Complete!" : "Game Over";
        resultsSoldText.text = $"Sold for: {sold}g\nThreshold: {threshold}g";
        resultsVerdictText.text = passed
            ? "You met the threshold. On to the next week!"
            : "You didn't make enough. The landlord takes the shop.";

        continueButton.gameObject.SetActive(passed);
        gameOverButton.gameObject.SetActive(!passed);

        resultsPanel.SetActive(true);
    }

    /// <summary>
    /// Called when the continue button is clicked on a passed auction.
    /// Restores the background and advances to the next round.
    /// </summary>
    private void OnContinueClicked()
    {
        resultsPanel.SetActive(false);
        SwapBackground(toAuction: false);
        RoundManager.Instance.StartNewRound();
    }

    /// <summary>
    /// Called when the game over button is clicked.
    /// Placeholder — expand when the run progression screen is built.
    /// </summary>
    private void OnGameOverClicked()
    {
        Debug.Log("[AuctionUI] Game Over confirmed. " +
                  "Load run progression screen here when built.");
        // ── Hook into run progression / main menu scene load here ──
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Swaps the scene background between normal gameplay and auction house.
    /// </summary>
    private void SwapBackground(bool toAuction)
    {
        if (sceneBackground == null) return;
        sceneBackground.sprite = toAuction ? auctionBackgroundSprite : normalBackgroundSprite;
    }

    /// <summary>
    /// Rounds an int up to the nearest integer ending in 0 or 5.
    /// Mirrors AuctionManager.RoundToNearestFiveOrZero for use in bid generation.
    /// </summary>
    private int RoundToNearestFiveOrZero(int value)
    {
        int remainder = value % 5;
        if (remainder == 0) return value;
        return value + (5 - remainder);
    }
}