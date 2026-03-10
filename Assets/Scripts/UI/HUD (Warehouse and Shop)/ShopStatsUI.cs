/*
 * ============================================================
 * SCRIPT:      ShopStatsUI.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Controls the Shop Stats panel and co-ordinates the joint
 *   open/close behaviour of both Shop and Warehouse panels.
 *
 *   ShopPanel is a child of WarehousePanel in the hierarchy,
 *   anchored to WarehousePanel's top edge. It moves with the
 *   Warehouse for free — PanelStackManager does NOT position
 *   it directly.
 *
 *   SHOP BUTTON (bottom-right) — controls both panels:
 *     Open:  calls SetPanelOpen("Warehouse", true,
 *                               topOffset: shopPanelHeight)
 *            Warehouse slides to Y = -shopPanelHeight.
 *            Shop (child, Pos Y = +shopPanelHeight above
 *            Warehouse top) lands at Y = 0 (canvas top).
 *            Card row displaces by warehouseHeight + shopHeight.
 *     Close: calls SetPanelOpen("Warehouse", false, 0)
 *            Both panels slide back above canvas.
 *            Card row returns to base.
 *
 *   WAREHOUSE BUTTON — controlled by WarehousePanelUI:
 *     If Shop is open when Warehouse button is clicked,
 *     WarehousePanelUI calls ShopStatsUI.ForceClose() first
 *     so both panels close together cleanly.
 *     If Shop is closed, Warehouse toggles normally
 *     (topOffset = 0, Warehouse slides to Y = 0 only).
 *
 *   DISPLAY:
 *     Panel contains: Round number, Gold (with temporary gold),
 *     Selected counter, Auction countdown, Income, Reputation,
 *     Floor Space, Warehouse count, Freelancers, Menu button.
 *
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardUIManager          -- calls RefreshAllStats() via
 *                             UpdateHUD()
 *   WarehousePanelUI       -- calls ForceClose() when
 *                             Warehouse button closes both
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   RefreshAllStats()  --> CardUIManager.UpdateHUD() and events
 *   ForceClose()       --> WarehousePanelUI when closing both
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). All updates are event-driven.
 *   Panel position driven entirely by hierarchy.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopStatsUI : MonoBehaviour
{
    public static ShopStatsUI Instance { get; private set; }

    // ── Panel ────────────────────────────────────────────────

    [Header("Panel")]
    [Tooltip("The ShopPanel GameObject. Must be a direct child of " +
             "WarehousePanel in the hierarchy.")]
    public GameObject shopPanel;

    [Tooltip("Height of the shop panel in pixels. " +
             "Must match the ShopPanel RectTransform height exactly. " +
             "Passed to PanelStackManager as the topOffset when opening " +
             "both panels together.")]
    public float panelHeight = 180f;

    // ── Shop button ──────────────────────────────────────────

    [Header("Shop Button")]
    [Tooltip("The bottom-right button that opens/closes both panels.")]
    public Button shopButton;

    // ── Stats: primary ───────────────────────────────────────

    [Header("Primary Stats")]
    [Tooltip("Current gold and temporary gold breakdown.")]
    public TextMeshProUGUI goldText;

    [Tooltip("Current round number.")]
    public TextMeshProUGUI roundText;

    [Tooltip("Rounds until next auction, or 'AUCTION'.")]
    public TextMeshProUGUI bossCountdownText;

    [Tooltip("Effects the next boss round (auction) will have")]
    public TextMeshProUGUI nextBossEffectsText;

    [Tooltip("Icons representing the current card selections")]
    public Image[] selections = new Image[2];
    public Color disabledColor = Color.white;
    public Color enabledColor = Color.white;

    // ── Stats: secondary ─────────────────────────────────────

    [Header("Secondary Stats")]
    public TextMeshProUGUI reputationText;
    public TextMeshProUGUI floorSpaceText;
    public TextMeshProUGUI warehouseText;
    public TextMeshProUGUI freelancersText;

    // ── Menu button ──────────────────────────────────────────

    [Header("Menu Button")]
    [Tooltip("Right-side menu button. Currently unwired. " +
             "Will navigate to Settings / Main Menu in a future version.")]
    public Button menuButton;

    // ── Internal state ───────────────────────────────────────

    private bool isOpen = false;
    public bool IsOpen => isOpen;

    private const string WAREHOUSE_ID = "Warehouse";

    // ── Lifecycle ────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        shopButton.onClick.AddListener(ToggleShopPanel);

        // All data subscriptions unchanged
        EconomyManager.Instance.onGoldChanged.AddListener(RefreshAllStats);
        ShopManager.Instance.onShopStatsChanged.AddListener(RefreshAllStats);
        RoundManager.Instance.onRoundStart.AddListener(RefreshAllStats);
        FreelancerManager.Instance.onFreelancerReturned.AddListener((_) => RefreshAllStats());

        RefreshAllStats();
    }

    // ── Toggle ───────────────────────────────────────────────

    /// <summary>
    /// Called by the Shop button. Opens or closes both Shop and Warehouse
    /// panels together. When opening, passes panelHeight as the topOffset
    /// so the Warehouse slides down far enough that Shop lands at the
    /// canvas top edge. When closing, topOffset returns to 0.
    /// </summary>
    public void ToggleShopPanel()
    {
        isOpen = !isOpen;

        // topOffset = panelHeight when opening so Shop clears the canvas edge.
        // topOffset = 0 (default) when closing — Warehouse returns to its
        // standard closed position (above canvas, button peeking below).
        PanelStackManager.Instance.SetPanelOpen(
            WAREHOUSE_ID, isOpen, isOpen ? panelHeight : 0f);

        if (isOpen)
        {
            RefreshAllStats();
            WarehousePanelUI.Instance.NotifyOpen();
        }
        else
        {
            WarehousePanelUI.Instance.NotifyClose();
        }

    }

    /// <summary>
    /// Forces the Shop closed without toggling. Called by WarehousePanelUI
    /// when the Warehouse button is pressed while the Shop is open,
    /// so both panels close cleanly together.
    /// Resets isOpen and the button label without triggering another
    /// SetPanelOpen call (the caller handles that).
    /// </summary>
    public void ForceClose()
    {
        isOpen = false;
        // Caller (WarehousePanelUI) is responsible for calling
        // PanelStackManager.SetPanelOpen("Warehouse", false).
    }

    // ── Refresh ──────────────────────────────────────────────

    /// <summary>
    /// Refreshes all stat displays. Safe to call at any time including
    /// while the panel is off screen — text is updated in memory and
    /// will be correct when the panel next slides into view.
    /// </summary>
    public void RefreshAllStats()
    {
        RefreshPrimaryStats();
        RefreshSecondaryStats();
    }

    private void RefreshPrimaryStats()
    {
        if (goldText != null)
        {
            int real = EconomyManager.Instance.currentGold;
            int temp = EconomyManager.Instance.temporaryGold;

            if (temp > 0)
                goldText.text = $"{real}g <color=#90EE90>+{temp}g</color>";
            else if (temp < 0)
                goldText.text = $"{real}g <color=#FF6B6B>{temp}g</color>";
            else
                goldText.text = $"{real}g";
        }

        if (roundText != null)
            roundText.text = $"Day {RoundManager.Instance.currentRound}";

        RefreshSelectionCircles();

        if (bossCountdownText != null)
        {
            int interval = RoundManager.Instance.bossRoundInterval;
            int current = RoundManager.Instance.currentRound;
            int remaining = interval - (current % interval);

            if (current % interval == 0 && current > 0)
                remaining = 0;

            bossCountdownText.text = remaining == 0
                ? "AUCTION"
                : $"Auction in {remaining} days";
        }
    }

    private void RefreshSecondaryStats()
    {

        if (reputationText != null)
            reputationText.text = $"{ShopManager.Instance.reputation}";

        if (floorSpaceText != null)
            floorSpaceText.text =
                $"{ShopManager.Instance.floorSpace}";

        if (warehouseText != null)
            warehouseText.text =
                $"{InventoryManager.Instance.items.Count}" +
                $" / {InventoryManager.Instance.maxSlots}";

        if (freelancersText != null)
            RefreshFreelancerText();
    }

    private void RefreshFreelancerText()
    {
        List<ActiveFreelancer> active = FreelancerManager.Instance.activeFreelancers;

        if (active.Count == 0)
        {
            freelancersText.text = "None";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Freelancers:");
        //foreach (ActiveFreelancer f in active)
        //    sb.AppendLine($"  • {f.cardName} — {f.roundsRemaining} round(s)");

        freelancersText.text = sb.ToString().TrimEnd();
    }

    private void RefreshSelectionCircles()
    {
        selections[0].color = disabledColor;
        selections[1].color = disabledColor;
        // Setting the circle colour ------------------------------------------
        if (RoundManager.Instance.stagedCards.Count == 1)
            selections[0].color = enabledColor;
        else if (RoundManager.Instance.stagedCards.Count == 2)
        {
            selections[0].color = enabledColor;
            selections[1].color = enabledColor;
        }
        else
        {
            selections[0].color = disabledColor;
            selections[1].color= disabledColor;
        }
            
        
    }
}