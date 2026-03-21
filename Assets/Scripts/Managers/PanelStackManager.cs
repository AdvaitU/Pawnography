/*
 * ============================================================
 * SCRIPT:      PanelStackManager.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Manages root panels that slide in from the top of the
 *   canvas, and the card row displacement they cause.
 *   All panels and the card row share a single slideSpeed.
 *
 *   Currently one root panel is registered: "Warehouse".
 *   ShopPanel is NOT registered here — it is a hierarchy
 *   child of WarehousePanel and moves with it for free.
 *
 *   SetPanelOpen() accepts an optional topOffset parameter.
 *   When ShopStatsUI opens the Warehouse to bring both panels
 *   on screen, it passes topOffset = shopPanelHeight so the
 *   Warehouse slides down far enough that the Shop (sitting
 *   above it as a child) lands flush at the canvas top edge.
 *   Card row displacement also includes topOffset.
 *
 *   Panel position convention (top-stretch anchor, pivot top):
 *     Closed: anchoredPosition.y = +panelHeight
 *             Panel body above canvas; Warehouse button
 *             (child anchored to panel bottom) peeks below
 *             the canvas top edge.
 *     Open:   anchoredPosition.y = -(hudHeight + topOffset)
 *             Panel hangs down from the canvas top.
 *
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   WarehousePanelUI   -- RegisterPanel(), SetPanelOpen()
 *   ShopStatsUI        -- SetPanelOpen() with topOffset
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Update() runs only while isAnimating is true.
 *   Stops itself once all targets are within 0.5 px.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PanelStackManager : MonoBehaviour
{
    public static PanelStackManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────

    [Header("Card Row")]
    [Tooltip("The RectTransform of the card row that slides down " +
             "when panels are opened.")]
    public RectTransform cardRowRect;

    [Tooltip("Height of any fixed HUD bar above the panels. " +
             "Set to 0 when panels slide from the very top of the canvas.")]
    public float hudHeight = 0f;

    [Tooltip("Lerp speed shared by all panel animations and the card row.")]
    public float slideSpeed = 8f;

    [Tooltip("Easing applied to panel and card row slide animations.")]
    public Ease panelSlideEase = Ease.OutCubic;

    // ── Data ─────────────────────────────────────────────────

    private class PanelEntry
    {
        public string panelId;
        public RectTransform rt;
        public float panelHeight;
        public bool isOpen;
        public float topOffset;    // Extra downward push when open (e.g. shopPanelHeight)
        public float targetY;
    }

    private List<PanelEntry> panels = new List<PanelEntry>();
    private float cardRowBaseY = 0f;
    private float cardRowTargetY = 0f;

    // ── Lifecycle ────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (cardRowRect != null)
            cardRowBaseY = cardRowRect.anchoredPosition.y;

        // Snap any panels that registered before this Start() ran.
        // Panels registered after this point are snapped inside RegisterPanel().
        SnapAllToClosed();
    }

    // No Update - Using Tweening instead

    // ── Public API ───────────────────────────────────────────

    /// <summary>
    /// Registers a root panel. Snaps it to its closed position
    /// (anchoredPosition.y = panelHeight) immediately.
    /// </summary>
    public void RegisterPanel(string panelId, GameObject panelObject, float panelHeight)
    {
        if (panels.Exists(p => p.panelId == panelId)) return;

        RectTransform rt = panelObject.GetComponent<RectTransform>();

        PanelEntry entry = new PanelEntry
        {
            panelId = panelId,
            rt = rt,
            panelHeight = panelHeight,
            isOpen = false,
            topOffset = 0f,
            targetY = panelHeight
        };

        panels.Add(entry);

        if (rt != null)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, panelHeight);

        Debug.Log($"[PanelStackManager] Registered '{panelId}' " +
                  $"(height: {panelHeight}px). Closed at Y: {panelHeight}.");
    }

    /// <summary>
    /// Opens or closes a registered panel.
    ///
    /// topOffset (optional) pushes the open Y target further down by that
    /// many pixels and is included in the card row displacement.
    /// Pass topOffset = shopPanelHeight when opening Warehouse to also
    /// bring the Shop (child) into view at the canvas top edge.
    /// Pass topOffset = 0 (default) for a plain Warehouse-only open.
    /// </summary>
    public void SetPanelOpen(string panelId, bool open, float topOffset = 0f)
    {
        PanelEntry entry = panels.Find(p => p.panelId == panelId);
        if (entry == null)
        {
            Debug.LogWarning($"[PanelStackManager] Panel '{panelId}' not registered.");
            return;
        }

        entry.isOpen = open;
        entry.topOffset = open ? topOffset : 0f;

        RecalculateTargets();
        AnimateToTargets();
    }

    // ── Private helpers ──────────────────────────────────────

    /// <summary>
    /// Recalculates Y targets for all panels and the card row.
    ///   Closed → targetY = +panelHeight  (above canvas)
    ///   Open   → targetY = -(hudHeight + topOffset)
    /// Card row displacement = sum of (panelHeight + topOffset) for open panels.
    /// </summary>
    private void RecalculateTargets()
    {
        float cardDisplacement = 0f;

        foreach (PanelEntry entry in panels)
        {
            if (!entry.isOpen)
            {
                entry.targetY = entry.panelHeight;
                continue;
            }

            entry.targetY = -(hudHeight + entry.topOffset);
            cardDisplacement += entry.panelHeight + entry.topOffset;
        }

        cardRowTargetY = cardRowBaseY - cardDisplacement;
    }

    private void AnimateToTargets()
    {
        foreach (PanelEntry entry in panels)
        {
            if (entry.rt == null) continue;
            entry.rt.DOAnchorPosY(entry.targetY, 1f / slideSpeed)
                    .SetEase(panelSlideEase)
                    .SetUpdate(true);
        }

        if (cardRowRect != null)
        {
            cardRowRect.DOAnchorPosY(cardRowTargetY, 1f / slideSpeed)
                       .SetEase(panelSlideEase)
                       .SetUpdate(true);
        }
    }

    private void SnapAllToClosed()
    {
        foreach (PanelEntry entry in panels)
        {
            if (entry.rt == null) continue;
            entry.rt.DOKill();
            entry.targetY = entry.panelHeight;
            entry.rt.anchoredPosition =
                new Vector2(entry.rt.anchoredPosition.x, entry.panelHeight);
        }
    }
}