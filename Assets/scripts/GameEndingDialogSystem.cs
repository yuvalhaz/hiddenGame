using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// מערכת פשוטה לסיום משחק - מציגה 3 בועות דיבור
/// </summary>
public class GameEndingDialogSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] dialogBubbles; // 3 בועות דיבור
    [SerializeField] private Button nextButton;
    [SerializeField] private Text buttonText;

    [Header("Settings")]
    [SerializeField] private string sceneToLoad = "MainMenu"; // סצנה לטעון בסוף
    [SerializeField] private bool quitGameInsteadOfLoadScene = false; // לצאת מהמשחק במקום לטעון סצנה

    private int currentDialog = 0;

    void Start()
    {
        // הסתר את כל הבועות
        foreach (var bubble in dialogBubbles)
            if (bubble != null) bubble.SetActive(false);

        // הצג את הבועה הראשונה
        ShowCurrentDialog();

        // חבר את הכפתור
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
    }

    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }

    private void ShowCurrentDialog()
    {
        // הסתר את כל הבועות
        foreach (var bubble in dialogBubbles)
            if (bubble != null) bubble.SetActive(false);

        // הצג את הבועה הנוכחית
        if (currentDialog < dialogBubbles.Length && dialogBubbles[currentDialog] != null)
            dialogBubbles[currentDialog].SetActive(true);

        // עדכן טקסט כפתור
        if (buttonText != null)
        {
            buttonText.text = (currentDialog == dialogBubbles.Length - 1) ? "סיום" : "המשך";
        }
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
            Debug.Log("[GameEndingDialogSystem] 📺 מציג פרסומת לפני סיום...");

            bool adFinished = false;

            RewardedAdsManager.Instance.ShowRewarded(
                onReward: () =>
                {
                    Debug.Log("[GameEndingDialogSystem] 📺 פרסומת הושלמה!");
                },
                onClosed: (completed) =>
                {
                    Debug.Log($"[GameEndingDialogSystem] 📺 פרסומת נסגרה. הושלמה: {completed}");
                    adFinished = true;
                },
                onFailed: (error) =>
                {
                    Debug.LogWarning($"[GameEndingDialogSystem] 📺 פרסומת נכשלה: {error}");
                    adFinished = true;
                },
                onOpened: () =>
                {
                    Debug.Log("[GameEndingDialogSystem] 📺 פרסומת נפתחה");
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
                Debug.LogWarning("[GameEndingDialogSystem] ⏰ פרסומת timeout!");

            // המתן רגע אחרי הפרסומת
            yield return new WaitForSeconds(0.5f);
        }

        // בצע את הפעולה המבוקשת
        if (quitGameInsteadOfLoadScene)
        {
            Debug.Log("[GameEndingDialogSystem] 🚪 יוצא מהמשחק...");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        else
        {
            Debug.Log($"[GameEndingDialogSystem] 🔄 טוען סצנה: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    /// <summary>
    /// קריאה מבחוץ להתחלת הדיאלוג
    /// </summary>
    public void StartEndingDialog()
    {
        currentDialog = 0;
        ShowCurrentDialog();
    }
}
