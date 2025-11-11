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
    [SerializeField] private float delayBetweenBubbles = 2.5f; // זמן המתנה בין בועות (שניות)
    [SerializeField] private float animationDuration = 0.5f; // משך אנימציית pop-in
    [SerializeField] private float bubbleDisplayTime = 2.0f; // כמה זמן כל בועה נשארת על המסך
    [SerializeField] private bool autoAdvance = true; // להעביר אוטומטית בין בועות או לחכות ללחיצה

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

        // הסתר את כל הבועות
        foreach (var bubble in dialogBubbles)
        {
            if (bubble != null)
            {
                bubble.SetActive(false);
                bubble.transform.localScale = Vector3.zero;
            }
        }

        // הצג את הבועה הנוכחית עם אנימציה
        if (currentDialog < dialogBubbles.Length && dialogBubbles[currentDialog] != null)
        {
            dialogBubbles[currentDialog].SetActive(true);
            StartCoroutine(AnimateBubblePopIn(dialogBubbles[currentDialog]));
            Debug.Log($"[EndingDialogController] ✅ Bubble {currentDialog} is now visible with animation");
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

            // המתן את משך האנימציה + זמן התצוגה
            yield return new WaitForSeconds(animationDuration + bubbleDisplayTime);
        }

        Debug.Log("[EndingDialogController] ✅ All bubbles shown! Starting end game sequence...");

        // כל הבועות הוצגו - סיים את המשחק
        yield return new WaitForSeconds(0.5f);
        EndGame();
    }
}
