using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

public class HintDialog : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup dialogGroup;

    [Header("🎯 Hint System")]
    [SerializeField] private VisualHintSystem hintSystem; // ← חיבור למערכת הרמזים החדשה!

    [Header("🔊 Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [Tooltip("Sound to play when dialog opens")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    [Header("Events")]
    public UnityEvent onHintGranted;
    public UnityEvent onClosed;

    private Vector2 originalAnchoredPosition;
    private RectTransform rectTransform;
    private bool isShowingHint = false; // ✅ דגל שמונע פתיחה בזמן רמז

    private void Awake()
    {
        if (dialogGroup == null) dialogGroup = GetComponent<CanvasGroup>();
        if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAd);
        if (closeButton != null)   closeButton.onClick.AddListener(Close);

        // ✅ שמור את ה-RectTransform וה-anchoredPosition המקורי
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
            Debug.Log($"[HintDialog] Saved original anchoredPosition: {originalAnchoredPosition}");
        }
        else
        {
            Debug.LogError("[HintDialog] ❌ RectTransform not found!");
        }

        // ✅ אם לא מחובר ידנית, נסה למצוא אוטומטית
        if (hintSystem == null)
        {
            hintSystem = FindObjectOfType<VisualHintSystem>();
            if (hintSystem != null)
            {
                Debug.Log("[HintDialog] מצא VisualHintSystem אוטומטית!");
            }
            else
            {
                Debug.LogWarning("[HintDialog] לא נמצא VisualHintSystem בסצנה!");
            }
        }

        // ✅ הסתר את הדיאלוג בהתחלה
        HideImmediate();
    }

    private void OnEnable()
    {
        // ✅ אל תעשה כלום ב-OnEnable - זה גורם לבעיות!
        // HideImmediate() ייקרא רק כאשר Close() או OnWatchAd() נקראים
    }

    private void OnDestroy()
    {
        if (watchAdButton != null) watchAdButton.onClick.RemoveListener(OnWatchAd);
        if (closeButton != null)   closeButton.onClick.RemoveListener(Close);

        if (RewardedAdsManager.Instance != null)
            RewardedAdsManager.Instance.OnRewardGranted -= HandleReward;
    }

    public void Open()
    {
        // ✅ אם הרמז פועל - אל תפתח!
        if (isShowingHint)
        {
            Debug.Log("[HintDialog] 🚫 Cannot open - hint is currently showing!");
            return;
        }

        // ✅ בדיקה: האם יש כפתורים זמינים לרמז?
        if (hintSystem != null && !hintSystem.HasAvailableButtons())
        {
            Debug.Log("[HintDialog] אין כפתורים זמינים לרמז - כל הכפתורים כבר הוצבו!");
            // אופציה: להציג הודעה למשתמש או לא לפתוח את הדיאלוג
            return;
        }

        ShowImmediate();
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        HideImmediate();
        onClosed?.Invoke();
    }

    private void OnWatchAd()
    {
        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogWarning("[HintDialog] RewardedAdsManager missing in scene.");
            return;
        }

        // ✅ סמן שהרמז מתחיל - זה ימנע מ-Open() לפתוח מחדש!
        isShowingHint = true;
        Debug.Log("[HintDialog] 🎯 Hint starting - dialog locked");

        HideImmediate();

#if UNITY_EDITOR
        // ✅ במצב עריכה (Unity Editor) - דלג על הפרסומת ותן רמז מיד!
        Debug.Log("[HintDialog] 🧪 Unity Editor mode - skipping ad, triggering hint immediately");
        HandleReward();
#else
        // ✅ במכשיר אמיתי - הצג פרסומת
        RewardedAdsManager.Instance.OnRewardGranted -= HandleReward;
        RewardedAdsManager.Instance.OnRewardGranted += HandleReward;
        RewardedAdsManager.Instance.ShowRewarded();
#endif
    }

    private void HandleReward()
    {
        Debug.Log("[HintDialog] ✅ הפרסומת הסתיימה - מעניק רמז!");

        if (RewardedAdsManager.Instance != null)
            RewardedAdsManager.Instance.OnRewardGranted -= HandleReward;

        HideImmediate();
        onHintGranted?.Invoke();

        // ✅ מפעיל את מערכת הרמזים החדשה!
        if (hintSystem != null)
        {
            Debug.Log("[HintDialog] מפעיל VisualHintSystem...");
            hintSystem.TriggerHint();

            // ✅ אחרי 5 שניות (זמן שהרמז מסתיים), נבטל את הנעילה
            StartCoroutine(UnlockDialogAfterHint());
        }
        else
        {
            Debug.LogError("[HintDialog] ❌ VisualHintSystem לא מחובר!");
            isShowingHint = false; // בטל נעילה אם אין רמז
        }
    }

    private IEnumerator UnlockDialogAfterHint()
    {
        // ✅ חכה 5 שניות (זמן שהרמז רץ)
        yield return new WaitForSeconds(5f);

        isShowingHint = false;
        Debug.Log("[HintDialog] 🔓 Hint finished - dialog unlocked");
    }

    private void ShowImmediate()
    {
        if (dialogGroup == null) return;

        Debug.Log($"[HintDialog] 🟢 ShowImmediate - showing dialog");

        // ✅ רק CanvasGroup - אל תגע ב-children!
        dialogGroup.alpha = 1f;
        dialogGroup.interactable = true;
        dialogGroup.blocksRaycasts = true;

        // 🔊 Play open sound
        PlayOpenSound();

        Debug.Log($"[HintDialog] ✅ Dialog is now visible and interactive");
    }

    private void PlayOpenSound()
    {
        if (openSound == null) return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        audioSource.PlayOneShot(openSound, soundVolume);
        Debug.Log("[HintDialog] 🔊 Playing open sound");
    }

    private void HideImmediate()
    {
        if (dialogGroup == null) return;

        Debug.Log($"[HintDialog] 🔴 HideImmediate - hiding dialog");

        // ✅ רק CanvasGroup - אל תגע ב-children!
        // זה הדרך הנכונה לעבוד עם UI ב-Unity
        dialogGroup.alpha = 0f;
        dialogGroup.interactable = false;
        dialogGroup.blocksRaycasts = false;

        Debug.Log($"[HintDialog] ✅ Dialog hidden via CanvasGroup only");
    }
}
