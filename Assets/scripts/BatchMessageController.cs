using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles completion messages, animations, and visual celebrations
/// </summary>
public class BatchMessageController : MonoBehaviour
{
    // Animation types available
    public enum AnimationType
    {
        ScaleBounce,    // Scale with bounce
        ScaleSmooth,    // Smooth scale
        PopIn,          // Pop in effect
        SlideDown,      // Slide from top
        Rotate          // Rotate while appearing
    }

    [Header("UI References")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private Text completionText;
    [SerializeField] private Canvas canvas;
    [Tooltip("Canvas for confetti effect (optional)")]

    [Header("Message Settings")]
    [SerializeField] private bool showCompletionMessage = true;
    [SerializeField] private float messageDuration = 2f;
    [Tooltip("How long to show the message (seconds)")]

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private AnimationType animationType = AnimationType.ScaleBounce;
    [SerializeField] private float animationDuration = 0.5f;
    [Tooltip("Duration of the entrance animation")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Message Content")]
    [SerializeField] private bool useCustomMessagesPerBatch = false;
    [Tooltip("Different message for each batch")]
    [SerializeField] private List<string> customMessagesPerBatch = new List<string>()
    {
        "GREAT JOB!",
        "AMAZING!",
        "PERFECT!"
    };

    [Header("Random Messages Mode")]
    [SerializeField] private List<string> randomMessages = new List<string>()
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

    [Header("Random Colors")]
    [SerializeField] private bool useRandomColors = true;
    [SerializeField] private List<Color> messageColors = new List<Color>()
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

    [Header("Audio Settings")]
    [SerializeField] private bool playSound = true;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool useRandomSound = true;
    [SerializeField] private List<AudioClip> completionSounds = new List<AudioClip>();
    [SerializeField] private AudioClip singleSound;
    [Tooltip("Use this if you want only one sound (when useRandomSound = false)")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // State
    private Coroutine hideMessageCoroutine = null;
    private bool isShowingMessage = false;
    private System.Action onMessageComplete;

    private void Awake()
    {
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
            completionPanel.transform.localScale = Vector3.one;
            completionPanel.transform.localRotation = Quaternion.identity;
        }

        isShowingMessage = false;
        hideMessageCoroutine = null;

        // Initialize audio source
        if (audioSource == null && playSound)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                if (debugMode)
                    Debug.Log("[BatchMessageController] ✅ Created AudioSource component");
            }
        }

        // Find Canvas automatically if not assigned
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas != null && debugMode)
                Debug.Log("[BatchMessageController] ✅ Auto-found Canvas for confetti");
        }
    }

    /// <summary>
    /// Show completion message for a batch
    /// </summary>
    public void ShowCompletionMessage(int batchIndex, System.Action onComplete = null)
    {
        if (!showCompletionMessage) return;

        onMessageComplete = onComplete;

        if (debugMode)
            Debug.Log($"💬 ShowCompletionMessage({batchIndex})");

        if (isShowingMessage)
        {
            if (hideMessageCoroutine != null)
                StopCoroutine(hideMessageCoroutine);
            if (completionPanel != null)
                completionPanel.SetActive(false);
        }

        isShowingMessage = true;

        string message = GetCompletionMessage(batchIndex);

        if (completionPanel == null || completionText == null)
        {
            Debug.LogError("❌ Panel or Text is NULL!");
            isShowingMessage = false;
            onMessageComplete?.Invoke();
            return;
        }

        completionPanel.transform.localScale = Vector3.one;
        completionPanel.transform.localRotation = Quaternion.identity;

        completionText.text = message;

        if (useRandomColors && messageColors.Count > 0)
        {
            completionText.color = messageColors[Random.Range(0, messageColors.Count)];
        }

        completionPanel.SetActive(true);

        if (useAnimation)
            StartCoroutine(AnimateMessage());

        PlaySound();

        hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay());

        Debug.Log($"<color=yellow>🎉 {message} 🎉</color>");
    }

    /// <summary>
    /// Show custom message (e.g., "WELL DONE!" for level complete)
    /// </summary>
    public void ShowCustomMessage(string customMessage, bool showConfetti = true, System.Action onComplete = null)
    {
        onMessageComplete = onComplete;

        if (debugMode)
            Debug.Log($"💬 ShowCustomMessage: {customMessage}");

        if (isShowingMessage)
        {
            if (hideMessageCoroutine != null)
                StopCoroutine(hideMessageCoroutine);
            if (completionPanel != null)
                completionPanel.SetActive(false);
        }

        isShowingMessage = true;

        if (completionPanel == null || completionText == null)
        {
            Debug.LogError("❌ Panel or Text is NULL!");
            isShowingMessage = false;
            onMessageComplete?.Invoke();
            return;
        }

        completionPanel.transform.localScale = Vector3.one;
        completionPanel.transform.localRotation = Quaternion.identity;

        completionText.text = customMessage;

        if (useRandomColors && messageColors.Count > 0)
        {
            completionText.color = messageColors[Random.Range(0, messageColors.Count)];
        }

        completionPanel.SetActive(true);

        if (useAnimation)
            StartCoroutine(AnimateMessage());

        PlaySound();

        // Show confetti for special messages
        if (showConfetti && canvas != null && completionPanel != null)
        {
            var panelRT = completionPanel.GetComponent<RectTransform>();
            if (panelRT != null)
            {
                UIConfetti.Burst(canvas, panelRT, count: 150, duration: 1.5f);

                if (debugMode)
                    Debug.Log("🎊 Confetti burst triggered!");
            }
        }

        hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay());

        Debug.Log($"<color=yellow>🎉 {customMessage} 🎉</color>");
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);

        if (useAnimation && completionPanel != null)
            yield return StartCoroutine(AnimateMessageOut());

        if (completionPanel != null)
            completionPanel.SetActive(false);

        isShowingMessage = false;
        hideMessageCoroutine = null;

        // Notify completion
        onMessageComplete?.Invoke();
        onMessageComplete = null;
    }

    private IEnumerator AnimateMessage()
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

    private IEnumerator AnimateMessageOut()
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

    private string GetCompletionMessage(int batch)
    {
        if (useCustomMessagesPerBatch && batch < customMessagesPerBatch.Count)
            return customMessagesPerBatch[batch];

        if (randomMessages.Count > 0)
            return randomMessages[Random.Range(0, randomMessages.Count)];

        return "GREAT!";
    }

    private void PlaySound()
    {
        if (!playSound || audioSource == null) return;

        AudioClip clip = null;

        if (useRandomSound && completionSounds.Count > 0)
            clip = completionSounds[Random.Range(0, completionSounds.Count)];
        else if (singleSound != null)
            clip = singleSound;

        if (clip != null)
        {
            audioSource.volume = soundVolume;
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Check if currently showing a message
    /// </summary>
    public bool IsShowingMessage()
    {
        return isShowingMessage;
    }

    [ContextMenu("🎨 Test Message")]
    private void TestMessage()
    {
        if (completionText == null || completionPanel == null)
        {
            Debug.LogError("❌ UI not assigned!");
            return;
        }
        ShowCompletionMessage(0);
    }
}
