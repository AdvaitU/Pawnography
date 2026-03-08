/*
 * ============================================================
 * SCRIPT:      ShopStatsUI.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Controls the collapsible shop stats panel. Registers with
 *   PanelStackManager so card row adjusts correctly when open
 *   alongside the warehouse panel. Updates all permanent HUD
 *   stats (gold, boss countdown) and all expanded panel stats.
 *   Card row animation is fully delegated to PanelStackManager.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardUIManager       -- calls RefreshAllStats() via UpdateHUD()
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   RefreshAllStats()   --> Called by CardUIManager.UpdateHUD()
 *                          and subscribed events
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). All updates are event-driven.
 *   Card row animation handled by PanelStackManager.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopStatsUI : MonoBehaviour
{
    public static ShopStatsUI Instance { get; private set; }

    [Header("HUD — Permanent Stats")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI bossCountdownText;
    public Button shopButton;
    public TextMeshProUGUI shopButtonLabel;

    [Header("Shop Panel")]
    public GameObject shopPanel;

    [Tooltip("Height of the shop panel in pixels. Must match ShopPanel RectTransform height.")]
    public float panelHeight = 180f;

    [Header("Shop Panel Stat Texts — assign in Inspector")]
    public TextMeshProUGUI incomeText;
    public TextMeshProUGUI reputationText;
    public TextMeshProUGUI floorSpaceText;
    public TextMeshProUGUI warehouseText;
    public TextMeshProUGUI freelancersText;

    private bool isPanelOpen = false;
    private const string PANEL_ID = "Shop";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        shopButton.onClick.AddListener(ToggleShopPanel);

        // Register with PanelStackManager
        PanelStackManager.Instance.RegisterPanel(PANEL_ID, shopPanel, panelHeight);

        // Subscribe to events
        EconomyManager.Instance.onGoldChanged.AddListener(RefreshAllStats);
        ShopManager.Instance.onShopStatsChanged.AddListener(RefreshAllStats);
        RoundManager.Instance.onRoundStart.AddListener(RefreshAllStats);
        FreelancerManager.Instance.onFreelancerReturned.AddListener((_) => RefreshAllStats());

        shopPanel.SetActive(false);
        RefreshAllStats();
    }

    public void ToggleShopPanel()
    {
        isPanelOpen = !isPanelOpen;
        shopPanel.SetActive(isPanelOpen);
        shopButtonLabel.text = isPanelOpen ? "Shop ▲" : "Shop ▼";

        // Delegate card row movement to PanelStackManager
        PanelStackManager.Instance.SetPanelOpen(PANEL_ID, isPanelOpen);

        if (isPanelOpen)
            RefreshAllStats();
    }

    public void RefreshAllStats()
    {
        RefreshHUDStats();
        if (isPanelOpen)
            RefreshPanelStats();
    }

    private void RefreshHUDStats()
    {
        if (goldText != null)
        {
            int real = EconomyManager.Instance.currentGold;
            int temp = EconomyManager.Instance.temporaryGold;

            if (temp > 0)
                goldText.text = $"💰 {real}g <color=#90EE90>+{temp}g</color>";
            else if (temp < 0)
                goldText.text = $"💰 {real}g <color=#FF6B6B>{temp}g</color>"; // temp is already negative so no minus needed
            else
                goldText.text = $"💰 {real}g";
        }

        if (bossCountdownText != null)
        {
            int interval = RoundManager.Instance.bossRoundInterval;
            int current = RoundManager.Instance.currentRound;
            int roundsUntilAuction = interval - (current % interval);

            if (current % interval == 0 && current > 0)
                roundsUntilAuction = 0;

            bossCountdownText.text = roundsUntilAuction == 0
                ? "AUCTION"
                : $"Auction in {roundsUntilAuction}";
        }
    }

    private void RefreshPanelStats()
    {
        if (incomeText != null)
            incomeText.text = $"Income: {EconomyManager.Instance.incomeThisCycle}g" +
                              $" / {EconomyManager.Instance.currentThreshold}g";

        if (reputationText != null)
            reputationText.text = $"Reputation: {ShopManager.Instance.reputation}";

        if (floorSpaceText != null)
            floorSpaceText.text = $"Floor Space: {ShopManager.Instance.floorSpace} cards/round";

        if (warehouseText != null)
            warehouseText.text = $"Warehouse: {InventoryManager.Instance.items.Count}" +
                                 $" / {InventoryManager.Instance.maxSlots}";

        if (freelancersText != null)
            RefreshFreelancerText();
    }

    private void RefreshFreelancerText()
    {
        List<ActiveFreelancer> active = FreelancerManager.Instance.activeFreelancers;

        if (active.Count == 0)
        {
            freelancersText.text = "Freelancers: None";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Freelancers:");
        foreach (ActiveFreelancer f in active)
            sb.AppendLine($"  • {f.cardName} — {f.roundsRemaining} round(s)");

        freelancersText.text = sb.ToString().TrimEnd();
    }
}