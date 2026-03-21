/*
 * ============================================================
 * SCRIPT:      FloatingText.cs
 * GAMEOBJECT:  Spawned at runtime as a child of MainCanvas
 * ------------------------------------------------------------
 * FUNCTION:
 *   A self-contained floating text element. On Show(), plays
 *   a scale punch, holds at full opacity, fades out, then
 *   destroys itself. Driven entirely by a DOTween Sequence —
 *   no Update() loop.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   FloatingTextManager -- spawns and initialises via Show()
 * ============================================================
 */

using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    [Header("Settings")]
    public string message = "Not Enough Gold!";
    public Color textColour = new Color(1f, 0.25f, 0.25f, 1f);
    public float fontSize = 22f;

    [Header("Spawn Burst")]
    public float burstScale = 1.4f;
    public float burstDuration = 0.25f;
    public Ease burstEase = Ease.OutBack;

    [Header("Hold")]
    public float holdDuration = 0.8f;

    [Header("Fade")]
    public float fadeDuration = 0.4f;
    public Ease fadeEase = Ease.InQuad;

    private TextMeshProUGUI textComponent;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textComponent = GetComponentInChildren<TextMeshProUGUI>();

        // CanvasGroup drives the fade — add one if not present
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Initialises and starts the floating text animation sequence.
    /// Called by FloatingTextManager immediately after instantiation.
    /// </summary>
    public void Show(string text, Color colour, float size)
    {
        message = text;
        textColour = colour;
        fontSize = size;

        if (textComponent != null)
        {
            textComponent.text = message;
            textComponent.color = textColour;
            textComponent.fontSize = fontSize;
        }

        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 1f;

        Sequence seq = DOTween.Sequence();

        // Burst — scale punch from zero
        seq.Append(rectTransform.DOScale(1f, burstDuration)
                                .SetEase(burstEase));

        // Hold — pause at full opacity
        seq.AppendInterval(holdDuration);

        // Fade out
        seq.Append(canvasGroup.DOFade(0f, fadeDuration)
                              .SetEase(fadeEase));

        // Destroy on complete
        seq.OnComplete(() => Destroy(gameObject));
    }
}