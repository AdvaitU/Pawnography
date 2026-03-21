/*
 * ============================================================
 * SCRIPT:      PopupShake.cs
 * GAMEOBJECT:  PopupPanel and HoverPopupPanel
 * ------------------------------------------------------------
 * FUNCTION:
 *   Plays a DOTween scale punch when the popup is enabled.
 *   Idle positional shake is handled separately — see
 *   the idle animation system when implemented.
 * ------------------------------------------------------------
 */

using UnityEngine;
using DG.Tweening;

public class PopupShake : MonoBehaviour
{
    [Header("Spawn Burst")]
    [Tooltip("How much larger the popup scales up to at the peak of the spawn animation.")]
    public float spawnBurstScale = 1.2f;

    [Tooltip("Total duration of the spawn scale punch in seconds.")]
    public float spawnBurstDuration = 0.35f;

    [Tooltip("Easing applied to the popup spawn burst scale punch.")]
    public Ease spawnBurstEase = Ease.OutQuad;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        rectTransform.localScale = Vector3.one;
        rectTransform.DOKill();

        // Scale punch: grow to spawnBurstScale then snap back to 1
        rectTransform
            .DOPunchScale(Vector3.one * (spawnBurstScale - 1f),
                          spawnBurstDuration,
                          vibrato: 1,
                          elasticity: 0.5f)
            .SetEase(spawnBurstEase);
    }

    private void OnDisable()
    {
        rectTransform.DOKill();
        if (rectTransform != null)
            rectTransform.localScale = Vector3.one;
    }
}