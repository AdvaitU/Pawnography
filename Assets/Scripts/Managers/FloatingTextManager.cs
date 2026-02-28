/*
 * ============================================================
 * SCRIPT:      FloatingTextManager.cs
 * GAMEOBJECT:  UIManager
 * ------------------------------------------------------------
 * FUNCTION:
 *   Spawns FloatingText instances below a target card's
 *   RectTransform. Converts the card's world position into
 *   canvas space and positions the text just below it.
 *   Called by CardUI (on click) and CardInteractionManager
 *   (at execution time) when the player cannot afford a card.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   CardUI                 -- calls ShowNotEnoughGold() on
 *                            click when seller cannot be afforded
 *   CardInteractionManager -- calls ShowNotEnoughGold() at
 *                            execution time for contractor
 *                            and freelancer cards
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:
 *   ShowNotEnoughGold()    --> Called by CardUI and
 *                            CardInteractionManager
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No Update(). FloatingText instances manage their own
 *   lifecycle and destroy themselves after completing.
 * ============================================================
 */

using UnityEngine;
using TMPro;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("The canvas to spawn floating text under.")]
    public Canvas parentCanvas;

    [Header("Floating Text Prefab")]
    public GameObject floatingTextPrefab;

    [Header("Settings")]
    [Tooltip("How far below the card the text spawns in pixels.")]
    public float spawnOffsetY = -60f;

    [Tooltip("Text displayed when the player cannot afford a card.")]
    public string notEnoughGoldMessage = "Not Enough Gold!";

    [Tooltip("Colour of the not enough gold message.")]
    public Color notEnoughGoldColour = new Color(1f, 0.25f, 0.25f, 1f);

    [Tooltip("Font size of the floating text.")]
    public float fontSize = 22f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Spawns a Not Enough Gold floating text below the given card RectTransform.
    /// Pass the card's root RectTransform (the CardVisual child object).
    /// </summary>
    public void ShowNotEnoughGold(RectTransform cardRect)
    {
        SpawnFloatingText(notEnoughGoldMessage, notEnoughGoldColour, fontSize, cardRect);
    }

    /// <summary>
    /// Spawns a floating text element below the target RectTransform.
    /// Converts the target's canvas position and places the text
    /// just below it.
    /// </summary>
    private void SpawnFloatingText(string message, Color colour, float size,
                                   RectTransform targetRect)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning("[FloatingTextManager] floatingTextPrefab not assigned.");
            return;
        }

        // Instantiate under the canvas so it renders above everything
        GameObject obj = Instantiate(floatingTextPrefab, parentCanvas.transform);
        RectTransform rt = obj.GetComponent<RectTransform>();

        // Convert target world position to canvas local position
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.GetComponent<RectTransform>(),
            RectTransformUtility.WorldToScreenPoint(
                parentCanvas.worldCamera, targetRect.position),
            null,
            out canvasPos
        );

        // Position below the card
        rt.anchoredPosition = new Vector2(canvasPos.x, canvasPos.y + spawnOffsetY);

        // Initialise the text
        FloatingText floatingText = obj.GetComponent<FloatingText>();
        if (floatingText != null)
            floatingText.Show(message, colour, size);
    }
}