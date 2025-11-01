using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HintDialog : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup dialogGroup;

    [Header("🎯 Hint System")]
    [SerializeField] private VisualHintSystem hintSystem; // ← חיבור למערכת הרמזים החדשה!

    [Header("Events")]
    public UnityEvent onHintGranted;
    public UnityEvent onClosed;

    private Vector2 originalAnchoredPosition;
    private RectTransform rectTransform;

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
    }

    private void OnEnable()
    {
        HideImmediate();
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
        }
        else
        {
            Debug.LogError("[HintDialog] ❌ VisualHintSystem לא מחובר!");
        }
    }

    private void ShowImmediate()
    {
        if (dialogGroup == null) return;

        Debug.Log($"[HintDialog] 🟢 ShowImmediate");

        // ✅ החזר את החלון למיקום המקורי (RectTransform!)
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
            Debug.Log($"[HintDialog] ✅ Restored anchoredPosition to {originalAnchoredPosition}");
        }

        dialogGroup.alpha = 1f;
        dialogGroup.interactable = true;
        dialogGroup.blocksRaycasts = true;
    }

    private void HideImmediate()
    {
        if (dialogGroup == null) return;

        Debug.Log($"[HintDialog] 🔴 HideImmediate - moving dialog off-screen");

        dialogGroup.alpha = 0f;
        dialogGroup.interactable = false;
        dialogGroup.blocksRaycasts = false;

        // ✅ הזז את החלון הרחק מחוץ למסך! (RectTransform anchoredPosition)
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(50000f, 50000f);
            Debug.Log($"[HintDialog] ✅ Moved off-screen: anchoredPosition = {rectTransform.anchoredPosition}");
        }
    }
}
