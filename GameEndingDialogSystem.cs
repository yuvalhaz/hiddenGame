using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// מערכת פשוטה לסיום משחק - מציגה 3 בועות דיבור
/// </summary>
public class EndingDialogController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] dialogBubbles; // 3 בועות דיבור
    [SerializeField] private Button nextButton;
    [SerializeField] private Text buttonText;

    [Header("Animation Settings")]
    [SerializeField] private float delayBetweenBubbles = 0.3f; // זמן המתנה בין בועות (שניות)
    [SerializeField] private float animationDuration = 0.5f; // משך אנימציית pop-in
    [SerializeField] private float allBubblesDisplayTime = 2.0f; // כמה זמן כל הבועות נשארות על המסך אחרי הבועה האחרונה
    [SerializeField] private bool autoAdvance = true; // להעביר אוטומטית בין בועות או לחכות ללחיצה

    [Header("Animation Integration")]
    [SerializeField] private AnimationClip[] levelEndAnimations; // אנימציות שירוצו כשהלבל נגמר (אופציונלי)
    [SerializeField] private GameObject[] animationTargets; // GameObjects שעליהם האנימציות ירוצו (אופציונלי, אם ריק ירוצו על אובייקט זה)

    [Header("🔊 Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bubblePopSound;
    [Tooltip("Sound to play when each bubble appears")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    [Header("Settings")]
    [SerializeField] private string sceneToLoad = "MainMenu"; // סצנה לטעון בסוף
    [SerializeField] private bool quitGameInsteadOfLoadScene = false; // לצאת מהמשחק במקום לטעון סצנה

    private int currentDialog = 0;
    private Coroutine autoAdvanceCoroutine = null;

    void Start()
    {
        // הסתר את כל הבועות בהתחלה
        foreach (var bubble in dialogBubbles)
        {
            if (bubble != null)
            {
                bubble.SetActive(false);
                // אתחל את ה-scale לאפס בהתחלה
                bubble.transform.localScale = Vector3.zero;
            }
        }

        // חבר את הכפתור
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
            // הסתר את הכפתור אם זה מצב אוטומטי
            if (autoAdvance)
                nextButton.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }

    private void ShowCurrentDialog()
    {
        Debug.Log($"[EndingDialogController] ShowCurrentDialog() - showing dialog {currentDialog}");

        // ✅ לא מסתירים בועות קודמות! רק מציגים את הנוכחית
        // הצג את הבועה הנוכחית עם אנימציה
        if (currentDialog < dialogBubbles.Length && dialogBubbles[currentDialog] != null)
        {
            dialogBubbles[currentDialog].SetActive(true);
            StartCoroutine(AnimateBubblePopIn(dialogBubbles[currentDialog]));

            // 🔊 Play sound when bubble appears
            PlayBubbleSound();

            Debug.Log($"[EndingDialogController] ✅ Bubble {currentDialog} is now popping in! Previous bubbles stay visible.");
        }
        else
        {
            Debug.LogError($"[EndingDialogController] ❌ Cannot show dialog {currentDialog} - out of bounds or null!");
        }

        // עדכן טקסט כפתור (אם לא במצב אוטומטי)
        if (!autoAdvance && buttonText != null)
        {
            buttonText.text = (currentDialog == dialogBubbles.Length - 1) ? "סיום" : "המשך";
        }
    }

    /// <summary>
    /// Plays all level-end animations when level completes
    /// </summary>
    private void TriggerLevelEndAnimators()
    {
        if (levelEndAnimations == null || levelEndAnimations.Length == 0)
            return;

        Debug.Log($"[EndingDialogController] 🎬 Playing {levelEndAnimations.Length} level-end animations");

        for (int i = 0; i < levelEndAnimations.Length; i++)
        {
            AnimationClip clip = levelEndAnimations[i];
            if (clip == null) continue;

            // Get target GameObject (or use this object if not specified)
            GameObject target = (animationTargets != null && i < animationTargets.Length && animationTargets[i] != null)
                ? animationTargets[i]
                : gameObject;

            // Get or add Animation component
            Animation anim = target.GetComponent<Animation>();
            if (anim == null)
            {
                anim = target.AddComponent<Animation>();
            }

            // Add clip and play
            anim.AddClip(clip, clip.name);
            anim.Play(clip.name);

            Debug.Log($"[EndingDialogController] 🎬 Playing animation '{clip.name}' on '{target.name}'");
        }
    }

    /// <summary>
    /// אנימציית pop-up bounce קומית
    /// </summary>
    private IEnumerator AnimateBubblePopIn(GameObject bubble)
    {
        Transform t = bubble.transform;
        Vector3 targetScale = Vector3.one;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;

            // Bounce effect - overshoot ואז התייצבות
            float bounce = Mathf.Sin(progress * Mathf.PI * 0.5f); // 0 → 1 smooth
            float overshoot = 1f + Mathf.Sin(progress * Mathf.PI) * 0.3f; // קפיצה של 30%

            t.localScale = targetScale * bounce * overshoot;

            yield return null;
        }

        // וודא שנגמר בגודל המדויק
        t.localScale = targetScale;

        Debug.Log($"[EndingDialogController] 💥 Bubble {currentDialog} pop animation complete!");
    }

    /// <summary>
    /// Play sound effect when bubble pops in
    /// </summary>
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
        Debug.Log($"[EndingDialogController] 🔊 Playing bubble pop sound {currentDialog}");
    }

    private void OnNextClicked()
    {
        currentDialog++;

        if (currentDialog >= dialogBubbles.Length)
        {
            // סיימנו את כל הבועות - סיים את המשחק
            EndGame();
        }
        else
        {
            // עבור לבועה הבאה
            ShowCurrentDialog();
        }
    }

    private void EndGame()
    {
        StartCoroutine(EndGameCoroutine());
    }

    private IEnumerator EndGameCoroutine()
    {
        // המתן רגע קטן
        yield return new WaitForSeconds(0.3f);

        // 📺 בדוק אם יש להציג פרסומת לפני סיום
        if (RewardedAdsManager.Instance != null)
        {
            Debug.Log("[EndingDialogController] 📺 מציג פרסומת לפני סיום...");

            bool adFinished = false;

            RewardedAdsManager.Instance.ShowRewarded(
                onReward: () =>
                {
                    Debug.Log("[EndingDialogController] 📺 פרסומת הושלמה!");
                },
                onClosed: (completed) =>
                {
                    Debug.Log($"[EndingDialogController] 📺 פרסומת נסגרה. הושלמה: {completed}");
                    adFinished = true;
                },
                onFailed: (error) =>
                {
                    Debug.LogWarning($"[EndingDialogController] 📺 פרסומת נכשלה: {error}");
                    adFinished = true;
                },
                onOpened: () =>
                {
                    Debug.Log("[EndingDialogController] 📺 פרסומת נפתחה");
                }
            );

            // המתן עד שהפרסומת תסתיים
            float timeout = 60f;
            float elapsed = 0f;

            while (!adFinished && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (elapsed >= timeout)
                Debug.LogWarning("[EndingDialogController] ⏰ פרסומת timeout!");

            // המתן רגע אחרי הפרסומת
            yield return new WaitForSeconds(0.5f);
        }

        // בצע את הפעולה המבוקשת
        if (quitGameInsteadOfLoadScene)
        {
            Debug.Log("[EndingDialogController] 🚪 יוצא מהמשחק...");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        else
        {
            Debug.Log($"[EndingDialogController] 🔄 טוען סצנה: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    /// <summary>
    /// קריאה מבחוץ להתחלת הדיאלוג
    /// </summary>
    public void StartEndingDialog()
    {
        Debug.Log("[EndingDialogController] 🎬 StartEndingDialog() called!");
        Debug.Log($"[EndingDialogController] Dialog bubbles count: {dialogBubbles.Length}");

        currentDialog = 0;

        // 🎬 Trigger all level-end animators when dialog starts
        TriggerLevelEndAnimators();

        // אם במצב אוטומטי - הפעל את הקורוטינה האוטומטית
        if (autoAdvance)
        {
            if (autoAdvanceCoroutine != null)
                StopCoroutine(autoAdvanceCoroutine);

            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceDialogs());
        }
        else
        {
            // במצב ידני - הצג רק את הבועה הראשונה
            ShowCurrentDialog();
        }

        Debug.Log($"[EndingDialogController] After StartEndingDialog - auto advance: {autoAdvance}");
    }

    /// <summary>
    /// קורוטינה שמעבירה אוטומטית בין הבועות
    /// </summary>
    private IEnumerator AutoAdvanceDialogs()
    {
        Debug.Log("[EndingDialogController] 🎬 Starting auto-advance sequence");

        for (int i = 0; i < dialogBubbles.Length; i++)
        {
            currentDialog = i;
            ShowCurrentDialog();

            Debug.Log($"[EndingDialogController] Showing bubble {i}/{dialogBubbles.Length - 1}");

            // ✅ המתן 0.3 שניות לפני הבועה הבאה (כל הבועות נשארות על המסך!)
            yield return new WaitForSeconds(delayBetweenBubbles);
        }

        Debug.Log("[EndingDialogController] ✅ All bubbles shown! Displaying all together...");

        // ✅ כל הבועות על המסך - המתן את הזמן שהן נשארות ביחד
        yield return new WaitForSeconds(allBubblesDisplayTime);

        Debug.Log("[EndingDialogController] Starting end game sequence...");

        // סיים את המשחק
        yield return new WaitForSeconds(0.5f);
        EndGame();
    }
}