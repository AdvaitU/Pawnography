/*
 * ============================================================
 * SCRIPT:      ButtonPunchFeedback.cs
 * GAMEOBJECT:  Any Button GameObject
 * ------------------------------------------------------------
 * FUNCTION:
 *   Adds a DOTween scale punch on click to any Button.
 *   Self-contained — attach and configure in Inspector.
 *   Works on Next Round, Warehouse, Shop Stats buttons,
 *   or any other Button in the project.
 * ============================================================
 */

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class ButtonPunchFeedback : MonoBehaviour
{
    [Tooltip("Scale punch amount on click.")]
    public float punchScale = 0.2f;

    [Tooltip("Duration of the punch animation.")]
    public float punchDuration = 0.25f;
    public Ease punchEase = Ease.OutElastic;

    [Tooltip("Optional: tint the button this colour briefly on click.")]
    public bool useColourFlash = false;
    public Color flashColour = new Color(1f, 1f, 0.6f, 1f);
    public float flashDuration = 0.2f;
    public Ease flashEase = Ease.OutQuad;

    private RectTransform rectTransform;
    private Image buttonImage;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>();
        GetComponent<Button>().onClick.AddListener(PlayFeedback);
    }

    private void OnDestroy()
    {
        rectTransform.DOKill();
        if (buttonImage != null) buttonImage.DOKill();
    }

    private void PlayFeedback()
    {
        rectTransform.DOKill();
        rectTransform.DOPunchScale(Vector3.one * punchScale,
                                    punchDuration,
                                    vibrato: 1,
                                    elasticity: 0.5f)
                     .SetEase(punchEase);

        if (useColourFlash && buttonImage != null)
        {
            Color original = buttonImage.color;
            buttonImage.DOKill();
            buttonImage.color = flashColour;
            buttonImage.DOColor(original, flashDuration)
                       .SetEase(flashEase);
        }
    }
}