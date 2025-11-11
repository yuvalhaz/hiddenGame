using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// מערכת בועות דיבור לסיום המשחק
/// מציגה 3 בועות דיבור ברצף ומסיימת את המשחק
/// </summary>
public class GameEndingDialogSystem : MonoBehaviour
{
    [Header("🎨 UI References")]
    [SerializeField] private GameObject dialogPanel;
    [Tooltip("הפאנל הראשי של בועת הדיבור")]

    [SerializeField] private Text dialogText;
    [Tooltip("הטקסט שמוצג בבועת הדיבור")]

    [SerializeField] private Button nextButton;
    [Tooltip("כפתור 'המשך' לבועה הבאה")]

    [SerializeField] private Text buttonText;
    [Tooltip("טקסט הכפתור (משתנה בבועה האחרונה)")]

    [Header("💬 Dialog Messages")]
    [SerializeField] private string[] dialogMessages = new string[3]
    {
        "כל הכבוד! סיימת את המשחק!",
        "מצאת את כל התמונות הנסתרות!",
        "תודה ששיחקת!"
    };

    [Header("🔘 Button Text")]
    [SerializeField] private string continueButtonText = "המשך";
    [SerializeField] private string finishButtonText = "סיום";

    [Header("⚙️ Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [Tooltip("מהירות הקלדת הטקסט (שניות לכל תו)")]

    [SerializeField] private bool useTypingEffect = true;
    [Tooltip("האם להשתמש באפקט הקלדה או להציג את כל הטקסט בבת אחת")]

    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float animationDuration = 0.3f;

    [Header("🎵 Audio (Optional)")]
    [SerializeField] private AudioClip dialogAppearSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip typingSound;
    [SerializeField] private AudioSource audioSource;

    [Header("🎬 Game End Action")]
    [SerializeField] private GameEndAction endAction = GameEndAction.QuitGame;
    [SerializeField] private string sceneToLoad = "MainMenu";
    [Tooltip("שם הסצנה לטעינה אם בחרת LoadScene")]

    [Header("🐛 Debug")]
    [SerializeField] private bool debugMode = true;

    public enum GameEndAction
    {
        QuitGame,           // יוצא מהמשחק
        LoadScene,          // טוען סצנה אחרת
        DisableAndStay,     // פשוט סוגר את הדיאלוג ונשאר
        RestartCurrentScene // מתחיל מחדש את הסצנה הנוכחית
    }

    private int currentDialogIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    void Awake()
    {
        if (debugMode)
            Debug.Log("[GameEndingDialogSystem] מתאתחל...");

        // בדיקות
        if (dialogPanel == null)
            Debug.LogError("[GameEndingDialogSystem] ❌ Dialog Panel לא מחובר!");

        if (dialogText == null)
            Debug.LogError("[GameEndingDialogSystem] ❌ Dialog Text לא מחובר!");

        if (nextButton == null)
            Debug.LogError("[GameEndingDialogSystem] ❌ Next Button לא מחובר!");

        // הוסף AudioSource אם חסר
        if (audioSource == null && (dialogAppearSound != null || buttonClickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // חבר את הכפתור
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);

        // הסתר את הדיאלוג בהתחלה
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextButtonClicked);
    }

    /// <summary>
    /// מתחיל את רצף בועות הדיבור
    /// </summary>
    public void StartEndingDialog()
    {
        if (debugMode)
            Debug.Log("[GameEndingDialogSystem] 🎬 מתחיל רצף סיום משחק");

        currentDialogIndex = 0;
        ShowDialog(0);
    }

    /// <summary>
    /// מציג בועת דיבור ספציפית
    /// </summary>
    private void ShowDialog(int index)
    {
        if (index < 0 || index >= dialogMessages.Length)
        {
            Debug.LogError($"[GameEndingDialogSystem] ❌ אינדקס לא חוקי: {index}");
            return;
        }

        if (debugMode)
            Debug.Log($"[GameEndingDialogSystem] 💬 מציג דיאלוג {index + 1}/{dialogMessages.Length}");

        currentDialogIndex = index;

        // עדכן טקסט כפתור
        if (buttonText != null)
        {
            bool isLastDialog = (index == dialogMessages.Length - 1);
            buttonText.text = isLastDialog ? finishButtonText : continueButtonText;
        }

        // הצג את הפאנל
        if (dialogPanel != null && !dialogPanel.activeSelf)
        {
            dialogPanel.SetActive(true);
            StartCoroutine(AnimateDialogAppear());
        }

        // צליל
        if (dialogAppearSound != null && audioSource != null)
            audioSource.PlayOneShot(dialogAppearSound);

        // הצג טקסט
        if (useTypingEffect)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(dialogMessages[index]));
        }
        else
        {
            if (dialogText != null)
                dialogText.text = dialogMessages[index];
        }
    }

    /// <summary>
    /// אפקט אנימציה להופעת הדיאלוג
    /// </summary>
    private IEnumerator AnimateDialogAppear()
    {
        if (dialogPanel == null) yield break;

        Transform t = dialogPanel.transform;
        Vector3 originalScale = t.localScale;
        t.localScale = Vector3.zero;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = scaleCurve.Evaluate(elapsed / animationDuration);
            t.localScale = originalScale * progress;
            yield return null;
        }

        t.localScale = originalScale;
    }

    /// <summary>
    /// אפקט הקלדה לטקסט
    /// </summary>
    private IEnumerator TypeText(string message)
    {
        isTyping = true;

        if (dialogText != null)
            dialogText.text = "";

        foreach (char c in message)
        {
            if (dialogText != null)
                dialogText.text += c;

            // צליל הקלדה
            if (typingSound != null && audioSource != null && !audioSource.isPlaying)
                audioSource.PlayOneShot(typingSound, 0.3f);

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    /// <summary>
    /// נקרא כשלוחצים על כפתור "המשך" או "סיום"
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (debugMode)
            Debug.Log($"[GameEndingDialogSystem] 🔘 נלחץ כפתור, אינדקס נוכחי: {currentDialogIndex}");

        // צליל
        if (buttonClickSound != null && audioSource != null)
            audioSource.PlayOneShot(buttonClickSound);

        // אם עדיין מקליד - דלג על האנימציה והצג הכל
        if (isTyping && useTypingEffect)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            if (dialogText != null)
                dialogText.text = dialogMessages[currentDialogIndex];

            isTyping = false;
            return;
        }

        // האם זו הבועה האחרונה?
        if (currentDialogIndex >= dialogMessages.Length - 1)
        {
            // בועה אחרונה - סיים את המשחק
            EndGame();
        }
        else
        {
            // עבור לבועה הבאה
            ShowDialog(currentDialogIndex + 1);
        }
    }

    /// <summary>
    /// מסיים את המשחק לפי ההגדרות
    /// </summary>
    private void EndGame()
    {
        if (debugMode)
            Debug.Log($"[GameEndingDialogSystem] 🏁 מסיים משחק - פעולה: {endAction}");

        StartCoroutine(EndGameCoroutine());
    }

    private IEnumerator EndGameCoroutine()
    {
        // אנימציה של היעלמות (אופציונלי)
        if (dialogPanel != null)
        {
            Transform t = dialogPanel.transform;
            Vector3 startScale = t.localScale;
            float elapsed = 0f;

            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float progress = 1f - (elapsed / (animationDuration * 0.5f));
                t.localScale = startScale * progress;
                yield return null;
            }
        }

        // המתן רגע קטן
        yield return new WaitForSeconds(0.5f);

        // בצע את הפעולה המבוקשת
        switch (endAction)
        {
            case GameEndAction.QuitGame:
                if (debugMode)
                    Debug.Log("[GameEndingDialogSystem] 🚪 יוצא מהמשחק...");

                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
                break;

            case GameEndAction.LoadScene:
                if (debugMode)
                    Debug.Log($"[GameEndingDialogSystem] 🔄 טוען סצנה: {sceneToLoad}");

                SceneManagement.LoadScene(sceneToLoad);
                break;

            case GameEndAction.RestartCurrentScene:
                if (debugMode)
                    Debug.Log("[GameEndingDialogSystem] 🔄 מתחיל מחדש את הסצנה");

                SceneManagement.LoadScene(SceneManager.GetActiveScene().name);
                break;

            case GameEndAction.DisableAndStay:
                if (debugMode)
                    Debug.Log("[GameEndingDialogSystem] ✅ סוגר דיאלוג ונשאר");

                if (dialogPanel != null)
                    dialogPanel.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// מאפשר לקרוא מבחוץ כדי לדלג לדיאלוג האחרון
    /// </summary>
    public void SkipToLastDialog()
    {
        ShowDialog(dialogMessages.Length - 1);
    }

    /// <summary>
    /// מאפשר לקרוא מבחוץ כדי לסגור את הדיאלוג
    /// </summary>
    public void CloseDialog()
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(false);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        isTyping = false;
    }

    #if UNITY_EDITOR
    [ContextMenu("🎬 Test Ending Dialog")]
    private void TestEndingDialog()
    {
        StartEndingDialog();
    }
    #endif
}
