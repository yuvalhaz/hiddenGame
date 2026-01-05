using System.Collections;
using UnityEngine;

/// <summary>
/// Static utility class for drag-related animations.
/// Provides reusable animation coroutines.
/// </summary>
public static class DragAnimator
{
    /// <summary>
    /// Animate size of a RectTransform with smooth easing.
    /// </summary>
    public static IEnumerator AnimateSize(RectTransform target, Vector2 startSize, Vector2 endSize, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth easing
            float easedT = EaseOutQuad(t);

            target.sizeDelta = Vector2.Lerp(startSize, endSize, easedT);

            yield return null;
        }

        if (target != null)
        {
            target.sizeDelta = endSize;
        }
    }

    /// <summary>
    /// Animate the drag visual returning to the button bar.
    /// </summary>
    public static IEnumerator AnimateReturnToBar(
        RectTransform dragVisual,
        RectTransform targetButton,
        System.Action onComplete = null)
    {
        if (dragVisual == null || targetButton == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 startPos = dragVisual.position;
        Vector2 startSize = dragVisual.sizeDelta;
        Vector2 buttonSize = new Vector2(targetButton.rect.width, targetButton.rect.height);

        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration && dragVisual != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = EaseInOutQuad(t);

            Vector3 targetWorldPos = targetButton.position;

            if (dragVisual != null)
            {
                dragVisual.position = Vector3.Lerp(startPos, targetWorldPos, easedT);
                dragVisual.sizeDelta = Vector2.Lerp(startSize, buttonSize, easedT);

                // Fade out
                float alpha = 1f - (t * 0.5f);

                CanvasGroup cg = dragVisual.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = alpha;
                }
            }

            yield return null;
        }

        onComplete?.Invoke();
    }

    // Easing functions
    public static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
}
