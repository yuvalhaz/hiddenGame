using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingDialogController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject bubbleMaster; // האובייקט הראשי שמכיל הכל
    [SerializeField] private Animator[] imageAnimators; // 4 תמונות דמויות עם Animator
    [SerializeField] private Button nextButton;
    [SerializeField] private Text buttonText;

    [Header("Animation Settings")]
    [SerializeField] private float delayBetweenBubbles = 0.3f;
    [SerializeField] private float allBubblesDisplayTime = 2.0f;
    [SerializeField] private bool autoAdvance = true;
    [SerializeField] private bool allowClickToSkip = true;
    [Tooltip("Allow clicking on bubbles to skip to ad and next scene")]

    [Header("🔊 Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bubblePopSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    [Header("Settings")]
    [SerializeField] private string sceneToLoad = "MainMenu";
    [SerializeField] private bool quitGameInsteadOfLoadScene = false;

    private int currentDialog = 0;
    private Coroutine autoAdvanceCoroutine = null;
    private bool skipRequested = false;
    private int bubblesPopped = 0; // כמה בועות כבר נלחצו
    private bool[] bubbleStates; // מצב כל בועה - פעילה או נעלמה

    void Start()
    {
        // אתחל מערך מצבי הבועות
        bubbleStates = new bool[imageAnimators.Length];

        // כבה את BubbleMaster בהתחלה
        if (bubbleMaster != null)
        {
            bubbleMaster.SetActive(false);
        }

        // כבה את כל קומפוננטי ה-Animator
        foreach (var animator in imageAnimators)
        {
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
            if (autoAdvance)
                nextButton.gameObject.SetActive(false);
        }

        // הוסף לחיצה על בועות אם מופעל
        if (allowClickToSkip)
        {
            SetupBubbleClickListeners();
        }
    }

    private void SetupBubbleClickListeners()
    {
        for (int i = 0; i < imageAnimators.Length; i++)
        {
            if (imageAnimators[i] != null)
            {
                int bubbleIndex = i; // Capture index for closure

                // הפרנט הוא רק Transform ריק - אל תוסיף לו כלום
                // הוסף רק לילדים (הבועות עצמן)

                // ✅ עבור על כל הילדים - Sprites
                SpriteRenderer[] childSprites = imageAnimators[i].GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sprite in childSprites)
                {
                    // הוסף Collider2D אם אין
                    if (sprite.GetComponent<Collider2D>() == null)
                    {
                        sprite.gameObject.AddComponent<BoxCollider2D>();
                        Debug.Log($"[EndingDialogController] Added collider to sprite: {sprite.name}");
                    }

                    // הוסף Button component ל-Sprite (זה יעבוד רק אם יש EventSystem)
                    var button = sprite.GetComponent<Button>();
                    if (button == null)
                    {
                        button = sprite.gameObject.AddComponent<Button>();
                    }

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnBubbleClicked(bubbleIndex));

                    Debug.Log($"[EndingDialogController] Added click handler to sprite bubble {bubbleIndex}: {sprite.name}");
                }

                // ✅ עבור על כל הילדים - UI Images
                UnityEngine.UI.Image[] childImages = imageAnimators[i].GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var image in childImages)
                {
                    image.raycastTarget = true; // ודא ש-raycast מופעל

                    // הוסף Button component לכל Image
                    var button = image.GetComponent<Button>();
                    if (button == null)
                    {
                        button = image.gameObject.AddComponent<Button>();
                    }

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnBubbleClicked(bubbleIndex));

                    Debug.Log($"[EndingDialogController] Added click handler to UI bubble {bubbleIndex}: {image.name}");
                }
            }
        }
    }

    private void OnBubbleClicked(int bubbleIndex)
    {
        // בדוק אם הבועה כבר נלחצה
        if (bubbleStates[bubbleIndex])
        {
            Debug.Log($"[EndingDialogController] Bubble {bubbleIndex} already popped!");
            return;
        }

        Debug.Log($"[EndingDialogController] Bubble {bubbleIndex} clicked!");

        // סמן שהבועה נלחצה
        bubbleStates[bubbleIndex] = true;
        bubblesPopped++;

        // הפעל אפקט פופ (אנימציה של היעלמות)
        StartCoroutine(PopBubble(bubbleIndex));

        // נגן צליל אם קיים
        PlayBubbleSound();

        // בדוק אם כל הבועות נלחצו
        if (bubblesPopped >= imageAnimators.Length)
        {
            Debug.Log("[EndingDialogController] All bubbles popped! Moving to ad...");

            // עצור את ה-auto advance
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            // המתן קצת ואז עבור לפרסומת
            StartCoroutine(WaitAndEndGame());
        }
    }

    private System.Collections.IEnumerator PopBubble(int bubbleIndex)
    {
        if (imageAnimators[bubbleIndex] == null)
            yield break;

        Transform bubbleTransform = imageAnimators[bubbleIndex].transform;
        Vector3 originalScale = bubbleTransform.localScale;

        // אנימציה של היעלמות - התכווצות מהירה
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // קצת קפיצה ואז התכווצות
            float scale = Mathf.Lerp(1f, 0f, progress);
            if (progress < 0.3f)
            {
                scale = Mathf.Lerp(1f, 1.2f, progress / 0.3f); // קפיצה קלה
            }
            else
            {
                scale = Mathf.Lerp(1.2f, 0f, (progress - 0.3f) / 0.7f); // התכווצות
            }

            bubbleTransform.localScale = originalScale * scale;
            yield return null;
        }

        // כבה את הבועה לגמרי
        imageAnimators[bubbleIndex].gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator WaitAndEndGame()
    {
        // המתן קצת אחרי שהבועה האחרונה נעלמה
        yield return new WaitForSeconds(0.5f);

        // עבור לפרסומת וסצנה הבאה
        EndGame();
    }

    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }

    private void ShowCurrentDialog()
    {
        // הדלק את ה-Animator של הבועה הנוכחית
        if (currentDialog < imageAnimators.Length && imageAnimators[currentDialog] != null)
        {
            imageAnimators[currentDialog].enabled = true;
            PlayBubbleSound();
            Debug.Log($"[EndingDialogController] 🎬 Enabled Animator {currentDialog}");
        }

        if (!autoAdvance && buttonText != null)
        {
            buttonText.text = (currentDialog == imageAnimators.Length - 1) ? "סיום" : "המשך";
        }
    }

    private void PlayBubbleSound()
    {
        if (bubblePopSound == null) return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        audioSource.PlayOneShot(bubblePopSound, soundVolume);
    }

    private void OnNextClicked()
    {
        currentDialog++;

        if (currentDialog >= imageAnimators.Length)
            EndGame();
        else
            ShowCurrentDialog();
    }

    private void EndGame()
    {
        StartCoroutine(EndGameCoroutine());
    }

    private IEnumerator EndGameCoroutine()
    {
        yield return new WaitForSeconds(0.3f);

        if (RewardedAdsManager.Instance != null)
        {
            bool adFinished = false;

            // בדוק אם הפרסומת מוכנה
            if (!RewardedAdsManager.Instance.IsReady())
            {
                Debug.LogWarning("[EndingDialogController] Ad not ready, skipping");
                adFinished = true;
            }
            else
            {
                RewardedAdsManager.Instance.ShowRewarded(
                    onClosed: (completed) => { adFinished = true; },
                    onFailed: (error) => { adFinished = true; }
                );

                float timeout = 60f;
                float elapsed = 0f;

                while (!adFinished && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (elapsed >= timeout)
                {
                    Debug.LogWarning("[EndingDialogController] Ad timeout!");
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        if (quitGameInsteadOfLoadScene)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    public void StartEndingDialog()
    {
        currentDialog = 0;
        skipRequested = false;
        bubblesPopped = 0; // אפס את מונה הבועות

        // אפס את מצבי הבועות
        if (bubbleStates == null || bubbleStates.Length != imageAnimators.Length)
        {
            bubbleStates = new bool[imageAnimators.Length];
        }
        else
        {
            for (int i = 0; i < bubbleStates.Length; i++)
            {
                bubbleStates[i] = false;
            }
        }

        // הדלק את BubbleMaster והבועות
        if (bubbleMaster != null)
        {
            bubbleMaster.SetActive(true);
            Debug.Log("[EndingDialogController] ✅ BubbleMaster activated");
        }

        // ודא שכל הבועות פעילות
        foreach (var animator in imageAnimators)
        {
            if (animator != null)
            {
                animator.gameObject.SetActive(true);
                animator.transform.localScale = Vector3.one; // החזר לגודל מקורי
            }
        }

        if (autoAdvance)
        {
            if (autoAdvanceCoroutine != null)
                StopCoroutine(autoAdvanceCoroutine);

            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceDialogs());
        }
        else
        {
            ShowCurrentDialog();
        }
    }

    private IEnumerator AutoAdvanceDialogs()
    {
        // הצג את כל הבועות אחת אחרי השנייה
        for (int i = 0; i < imageAnimators.Length; i++)
        {
            currentDialog = i;
            ShowCurrentDialog();
            yield return new WaitForSeconds(delayBetweenBubbles);
        }

        // עכשיו כל הבועות מוצגות
        // השחקן צריך ללחוץ עליהן כדי להמשיך
        // OnBubbleClicked יטפל בכל השאר
        Debug.Log("[EndingDialogController] All bubbles shown. Waiting for player clicks...");
    }
}
