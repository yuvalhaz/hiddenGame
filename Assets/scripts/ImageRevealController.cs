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

    // ✅ תיקון: הסתר הכל מיד ב-Awake!
    // ✅ אל תסתיר ב-Awake - תן ל-Start להחליט!
    private void Awake()
    {
        // ✅ לא עושים כלום כאן!
    }


    private void Start()
    {
        var dropSpot = GetComponent<DropSpot>();
        string spotId = dropSpot != null ? dropSpot.spotId : "UNKNOWN";

        // ✅ דיבאג מפורט!
        Debug.Log($"🔍 [{spotId}] ImageRevealController.Start() - BEGIN");

        if (dropSpot != null &&
            GameProgressManager.Instance != null &&
            GameProgressManager.Instance.IsItemPlaced(dropSpot.spotId))
        {
            Debug.Log($"✅ [{spotId}] Item is PLACED - revealing instantly!");

            dropSpot.IsSettled = true;

            if (placeholderImage != null)
            {
                placeholderImage.color = new Color(1, 1, 1, 0);
                Debug.Log($"✅ [{spotId}] Placeholder hidden");
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = Color.white;
                backgroundImage.raycastTarget = false;
                Debug.Log($"✅ [{spotId}] Background revealed!");
            }

            isRevealed = true;
        }
        else
        {
            Debug.Log($"❌ [{spotId}] Item NOT placed - hiding");

            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(1f, 1f, 1f, 0f);
            }

            if (placeholderImage != null)
            {
                placeholderImage.color = Color.white;
            }
        }

        Debug.Log($"🔍 [{spotId}] ImageRevealController.Start() - END");
    }



    private IEnumerator CheckAfterDelay()
    {
        // ✅ המתן frame אחד
        yield return null;

        var dropSpot = GetComponent<DropSpot>();

        if (dropSpot != null &&
            GameProgressManager.Instance != null &&
            GameProgressManager.Instance.IsItemPlaced(dropSpot.spotId))
        {
            // ✅ הפריט שמור - הצג הכל מיד!
            dropSpot.IsSettled = true;

            // ✅ הסתר placeholder
            if (placeholderImage != null)
            {
                placeholderImage.color = new Color(1, 1, 1, 0);
            }

            // ✅ הצג תמונה
            if (backgroundImage != null)
            {
                backgroundImage.color = Color.white;
            }

            isRevealed = true;

            Debug.Log($"[ImageRevealController] {dropSpot.spotId} restored instantly!");
        }
        // ✅ אם לא שמור - הכל כבר מוסתר מ-Awake, אין צורך לעשות כלום!
    }

    // ✅ מחק את InitializeHiddenState() - לא צריך יותר!

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

        // ✅ אחרי שהאנימציה נגמרה - כבה raycast!
        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = false;
        }

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
            backgroundImage.raycastTarget = false; // ✅ כבה raycast!
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