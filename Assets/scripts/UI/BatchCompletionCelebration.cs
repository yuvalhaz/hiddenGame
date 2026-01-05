using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles visual and audio celebration when a batch is completed.
/// Includes message display, animations, sounds, and color variations.
/// </summary>
[System.Serializable]
public class BatchCompletionCelebration
{
    public enum AnimationType
    {
        ScaleBounce,
        ScaleSmooth,
        PopIn,
        SlideDown,
        Rotate
    }

    [Header("UI References")]
    public GameObject completionPanel;
    public Text completionText;

    [Header("Animation")]
    public bool useAnimation = true;
    public AnimationType animationType = AnimationType.ScaleBounce;
    public float animationDuration = 0.5f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    public bool playSound = true;
    public AudioSource audioSource;
    public bool useRandomSound = true;
    public List<AudioClip> completionSounds = new List<AudioClip>();
    public AudioClip singleSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Messages")]
    public bool useCustomMessagesPerBatch = false;
    public List<string> customMessagesPerBatch = new List<string>()
    {
        "GREAT JOB!",
        "AMAZING!",
        "PERFECT!"
    };

    public List<string> randomMessages = new List<string>()
    {
        "GREAT!",
        "AWESOME!",
        "FANTASTIC!",
        "WONDERFUL!",
        "EXCELLENT!",
        "AMAZING!",
        "SUPER!",
        "PERFECT!",
        "BRILLIANT!",
        "INCREDIBLE!"
    };

    [Header("Colors")]
    public bool useRandomColors = true;
    public List<Color> messageColors = new List<Color>()
    {
        new Color(1f, 0.84f, 0f),      // Gold
        new Color(1f, 0.27f, 0f),      // Orange-Red
        new Color(0.2f, 0.8f, 0.2f),   // Green
        new Color(0f, 0.75f, 1f),      // Light Blue
        new Color(1f, 0.41f, 0.71f),   // Pink
        new Color(0.58f, 0f, 0.83f),   // Purple
        new Color(1f, 1f, 0f),         // Yellow
        new Color(1f, 0.5f, 0f)        // Orange
    };

    [Header("Display")]
    public float messageDuration = 2f;

    private bool isShowing = false;
    private Coroutine hideCoroutine;

    /// <summary>
    /// Initialize and hide the panel.
    /// </summary>
    public void Initialize(MonoBehaviour host)
    {
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
            completionPanel.transform.localScale = Vector3.one;
            completionPanel.transform.localRotation = Quaternion.identity;
        }

        if (audioSource == null && playSound)
        {
            audioSource = host.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = host.gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    /// <summary>
    /// Show celebration for completed batch.
    /// </summary>
    public void Show(int batchIndex, MonoBehaviour host)
    {
        if (completionPanel == null || completionText == null)
        {
            Debug.LogError("[BatchCompletionCelebration] Panel or Text is NULL!");
            return;
        }

        // Stop previous message if showing
        if (isShowing && hideCoroutine != null)
        {
            host.StopCoroutine(hideCoroutine);
            completionPanel.SetActive(false);
        }

        isShowing = true;

        // Reset transforms
        completionPanel.transform.localScale = Vector3.one;
        completionPanel.transform.localRotation = Quaternion.identity;

        // Set message
        string message = GetMessage(batchIndex);
        completionText.text = message;

        // Set color
        if (useRandomColors && messageColors.Count > 0)
        {
            completionText.color = messageColors[Random.Range(0, messageColors.Count)];
        }

        // Show panel
        completionPanel.SetActive(true);

        // Animate
        if (useAnimation)
        {
            host.StartCoroutine(AnimateIn());
        }

        // Play sound
        if (playSound)
        {
            PlaySound();
        }

        // Schedule hide
        hideCoroutine = host.StartCoroutine(HideAfterDelay(host));

        Debug.Log($"<color=yellow>🎉 {message} 🎉</color>");
    }

    private string GetMessage(int batchIndex)
    {
        if (useCustomMessagesPerBatch && batchIndex < customMessagesPerBatch.Count)
        {
            return customMessagesPerBatch[batchIndex];
        }

        if (randomMessages.Count > 0)
        {
            return randomMessages[Random.Range(0, randomMessages.Count)];
        }

        return "GREAT!";
    }

    private void PlaySound()
    {
        if (audioSource == null) return;

        AudioClip clip = null;

        if (useRandomSound && completionSounds.Count > 0)
        {
            clip = completionSounds[Random.Range(0, completionSounds.Count)];
        }
        else if (singleSound != null)
        {
            clip = singleSound;
        }

        if (clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    private IEnumerator AnimateIn()
    {
        if (completionPanel == null) yield break;

        Transform t = completionPanel.transform;
        Vector3 originalScale = t.localScale;
        Vector3 originalPos = t.localPosition;
        Quaternion originalRot = t.localRotation;

        float elapsed = 0f;

        switch (animationType)
        {
            case AnimationType.ScaleBounce:
                t.localScale = Vector3.zero;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / animationDuration;
                    float bounce = Mathf.Sin(progress * Mathf.PI * 0.5f);
                    float overshoot = 1f + Mathf.Sin(progress * Mathf.PI) * 0.2f;
                    t.localScale = originalScale * bounce * overshoot;
                    yield return null;
                }
                break;

            case AnimationType.ScaleSmooth:
                t.localScale = Vector3.zero;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = scaleCurve.Evaluate(elapsed / animationDuration);
                    t.localScale = originalScale * progress;
                    yield return null;
                }
                break;

            case AnimationType.PopIn:
                t.localScale = originalScale * 1.5f;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / animationDuration;
                    float easeOut = 1f - Mathf.Pow(1f - progress, 3f);
                    t.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, easeOut);
                    yield return null;
                }
                break;

            case AnimationType.SlideDown:
                Vector3 startPos = originalPos + Vector3.up * 300f;
                t.localPosition = startPos;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / animationDuration;
                    float easeOut = 1f - Mathf.Pow(1f - progress, 3f);
                    t.localPosition = Vector3.Lerp(startPos, originalPos, easeOut);
                    yield return null;
                }
                break;

            case AnimationType.Rotate:
                t.localScale = Vector3.zero;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / animationDuration;
                    t.localScale = originalScale * scaleCurve.Evaluate(progress);
                    t.localRotation = Quaternion.Euler(0, 0, (1f - progress) * 360f);
                    yield return null;
                }
                break;
        }

        t.localScale = originalScale;
        t.localPosition = originalPos;
        t.localRotation = originalRot;
    }

    private IEnumerator HideAfterDelay(MonoBehaviour host)
    {
        yield return new WaitForSeconds(messageDuration);

        if (useAnimation && completionPanel != null)
        {
            yield return host.StartCoroutine(AnimateOut());
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }

        isShowing = false;
        hideCoroutine = null;
    }

    private IEnumerator AnimateOut()
    {
        if (completionPanel == null) yield break;

        Transform t = completionPanel.transform;
        Vector3 start = t.localScale;
        float duration = animationDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(start, Vector3.zero, elapsed / duration);
            yield return null;
        }
    }

    /// <summary>
    /// Get total time for message display including animations.
    /// </summary>
    public float GetTotalDisplayTime()
    {
        float time = messageDuration;
        if (useAnimation)
        {
            time += animationDuration * 0.5f; // Add exit animation time
        }
        return time;
    }

    public bool IsShowing => isShowing;
}
