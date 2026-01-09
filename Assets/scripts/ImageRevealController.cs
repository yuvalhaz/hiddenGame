using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImageRevealController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image placeholderImage;

    [Header("Configuration")]
    [SerializeField] private ItemRevealConfig config;

    [Header("Optional Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip customRevealSound;

    private bool isRevealed = false;
    private Coroutine revealCoroutine;


    private void Start()
    {
        var dropSpot = GetComponent<DropSpot>();

        if (dropSpot != null &&
            GameProgressManager.Instance != null &&
            GameProgressManager.Instance.IsItemPlaced(dropSpot.spotId))
        {
            // Item already placed - reveal instantly
            dropSpot.IsSettled = true;

            if (placeholderImage != null)
            {
                placeholderImage.color = new Color(1, 1, 1, 0);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = Color.white;
            }

            isRevealed = true;
        }
        else
        {
            // Item not placed - hide background
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(1f, 1f, 1f, 0f);
            }

            if (placeholderImage != null)
            {
                placeholderImage.color = Color.white;
            }
        }
    }

    public void Reveal()
    {
        if (isRevealed)
        {
            Debug.LogWarning($"[ImageRevealController] {gameObject.name} already revealed");
            return;
        }

        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
        }

        revealCoroutine = StartCoroutine(RevealSequence());
    }

    private IEnumerator RevealSequence()
    {
        isRevealed = true;
        PlayRevealSound();
        yield return StartCoroutine(AnimateReveal());

        revealCoroutine = null;
    }


    private IEnumerator AnimateReveal()
    {
        float duration = config ? config.revealDuration : 0.5f;
        AnimationCurve curve = config ? config.revealCurve : AnimationCurve.EaseInOut(0, 0, 1, 1);

        Color startBgColor = backgroundImage ? backgroundImage.color : Color.clear;
        Color targetBgColor = Color.white;

        Color startPlaceholderColor = placeholderImage ? placeholderImage.color : Color.white;
        Color targetPlaceholderColor = new Color(1, 1, 1, 0);

        Vector3 originalScale = transform.localScale;
        float scaleAmount = (config && config.useScalePop) ? config.scalePopAmount : 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveValue = curve.Evaluate(t);

            if (backgroundImage != null)
            {
                backgroundImage.color = Color.Lerp(startBgColor, targetBgColor, curveValue);
            }

            if (placeholderImage != null)
            {
                placeholderImage.color = Color.Lerp(startPlaceholderColor, targetPlaceholderColor, curveValue);
            }

            if (scaleAmount > 0)
            {
                float scale = 1f + (Mathf.Sin(t * Mathf.PI) * scaleAmount);
                transform.localScale = originalScale * scale;
            }

            yield return null;
        }

        // Finalize
        if (backgroundImage != null)
            backgroundImage.color = targetBgColor;

        if (placeholderImage != null)
            placeholderImage.color = targetPlaceholderColor;

        transform.localScale = originalScale;
    }

    private void PlayRevealSound()
    {
        AudioClip clipToPlay = customRevealSound ?? (config?.revealSound);

        if (clipToPlay == null) return;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        audioSource.volume = 0.5f;
        audioSource.PlayOneShot(clipToPlay);
    }

    public void ResetReveal()
    {
        isRevealed = false;

        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        // ✅ הסתר הכל
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (placeholderImage != null)
        {
            placeholderImage.color = Color.white;
        }

        transform.localScale = Vector3.one;
    }

    public bool IsRevealed => isRevealed;

    public void RevealInstant()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white;
        }

        if (placeholderImage != null)
            placeholderImage.color = new Color(1, 1, 1, 0);

        isRevealed = true;
    }


    public Image GetBackgroundImage()
    {
        return backgroundImage;
    }
}
