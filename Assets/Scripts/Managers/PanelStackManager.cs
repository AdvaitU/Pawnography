/*
 * ============================================================
 * SCRIPT:      PanelStackManager.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Tracks all collapsible panels below the HUD and their
 *   open/closed state. When any panel opens or closes, it
 *   recalculates the total vertical offset needed and smoothly
 *   animates the card row down or up accordingly.
 *   Panels register themselves on Start via RegisterPanel().
 *   The order panels appear below the HUD matches the order
 *   they were opened � first opened sits closest to the HUD.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   ShopStatsUI         -- calls RegisterPanel() and
 *                          SetPanelOpen() on toggle
 *   WarehousePanelUI    -- calls RegisterPanel() and
 *                          SetPanelOpen() on toggle
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   RegisterPanel()     --> Called by each panel UI script
 *                          on Start to join the stack
 *   SetPanelOpen()      --> Called by each panel UI script
 *                          when toggled open or closed
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Update() -- runs only while isAnimating is true.
 *   Stops itself once the card row reaches its target.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;

public class PanelStackManager : MonoBehaviour
{
    public static PanelStackManager Instance { get; private set; }

    [Header("Card Row")]
    [Tooltip("The RectTransform of the card row that slides down " +
             "when panels are opened.")]
    public RectTransform cardRowRect;

    [Tooltip("Height of the HUD bar in pixels. Panels stack below this offset.")]
    public float hudHeight = 80f;

    [Tooltip("Speed of the card row slide animation.")]
    public float slideSpeed = 8f;

    // Registered panels in the order they were registered
    // Each entry: (panelGameObject, panelHeight, isOpen)
    private List<PanelEntry> panels = new List<PanelEntry>();

    // The open order list � panels are added here when opened,
    // removed when closed. Determines visual stacking order.
    private List<PanelEntry> openOrder = new List<PanelEntry>();

    private float cardRowBaseY = 0f;
    private float cardRowTargetY = 0f;
    private bool isAnimating = false;

    [System.Serializable]
    public class PanelEntry
    {
        public GameObject panelObject;
        public float panelHeight;
        public bool isOpen;
        public string panelId;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (cardRowRect != null)
            cardRowBaseY = cardRowRect.anchoredPosition.y;
    }

    private void Update()
    {
        if (!isAnimating) return;

        float current = cardRowRect.anchoredPosition.y;
        float next = Mathf.Lerp(current, cardRowTargetY, Time.deltaTime * slideSpeed);
        cardRowRect.anchoredPosition = new Vector2(cardRowRect.anchoredPosition.x, next);

        if (Mathf.Abs(next - cardRowTargetY) < 0.5f)
        {
            cardRowRect.anchoredPosition = new Vector2(
                cardRowRect.anchoredPosition.x, cardRowTargetY);
            isAnimating = false;
        }
    }

    /// <summary>
    /// Called by each panel UI script on Start to register itself
    /// with the stack manager.
    /// </summary>
    public void RegisterPanel(string panelId, GameObject panelObject, float panelHeight)
    {
        // Avoid duplicate registration
        if (panels.Exists(p => p.panelId == panelId)) return;

        panels.Add(new PanelEntry
        {
            panelId = panelId,
            panelObject = panelObject,
            panelHeight = panelHeight,
            isOpen = false
        });

        Debug.Log($"[PanelStackManager] Registered panel '{panelId}' " +
                  $"(height: {panelHeight}px).");
    }

    /// <summary>
    /// Called by a panel UI script when it is toggled open or closed.
    /// Repositions all open panels and recalculates card row offset.
    /// </summary>
    public void SetPanelOpen(string panelId, bool open)
    {
        PanelEntry entry = panels.Find(p => p.panelId == panelId);
        if (entry == null)
        {
            Debug.LogWarning($"[PanelStackManager] Panel '{panelId}' not registered.");
            return;
        }

        entry.isOpen = open;

        if (open && !openOrder.Contains(entry))
            openOrder.Add(entry);
        else if (!open)
            openOrder.Remove(entry);

        RepositionPanels();
        RecalculateCardRowTarget();
    }

    /// <summary>
    /// Repositions all open panels in the order they were opened,
    /// stacking them directly below the HUD bar.
    /// Panels are anchored top-stretch so negative Pos Y pushes them down.
    /// </summary>
    private void RepositionPanels()
    {
        // Start stacking immediately below the HUD bar
        float currentY = -hudHeight;

        foreach (PanelEntry entry in openOrder)
        {
            RectTransform rt = entry.panelObject.GetComponent<RectTransform>();
            if (rt == null) continue;

            rt.anchoredPosition = new Vector2(0f, currentY);
            currentY -= entry.panelHeight;
        }
    }

    /// <summary>
    /// Recalculates and applies the card row Y target based on
    /// the total height of all open panels.
    /// </summary>
    private void RecalculateCardRowTarget()
    {
        float totalOffset = 0f;
        foreach (PanelEntry entry in openOrder)
            totalOffset += entry.panelHeight;

        cardRowTargetY = cardRowBaseY - totalOffset;
        isAnimating = true;
    }
}