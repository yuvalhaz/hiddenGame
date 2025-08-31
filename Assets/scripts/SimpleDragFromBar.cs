using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class SimpleDragFromBar : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Canvas / Raycast")]
    [Tooltip("ה-Canvas העליון שבו רץ ה-UI (חובה GraphicRaycaster + EventSystem בסצנה).")]
    public Canvas topCanvas;

    [Header("Identity (התאמת פריט->נקודה)")]
    [Tooltip("צריך להתאים ל-spotId של DropSpot תואם.")]
    public string itemId;

    [Header("Drag Visual")]
    [Tooltip("Prefab עם Image + CanvasGroup (raycastTarget=false). אם ריק – ניצור Image זמני.")]
    public RectTransform dragVisualPrefab;
    [Tooltip("אפשר להחליף Sprite לגרירה; אם ריק ניקח מה-Image שעל הכפתור.")]
    public Sprite overrideSprite;
    [Tooltip("סקייל של תמונת הגרירה בזמן גרירה.")]
    public float dragVisualScale = 1f;

    [Header("Feedback")]
    public AudioSource sfxSource;
    public AudioClip correctSfx;
    public AudioClip wrongSfx;

    [Header("Behavior")]
    [Tooltip("להסיר את כפתור הבר לאחר מיקום נכון.")]
    public bool removeBarButtonOnSuccess = true;

    [Header("UI Confetti")]
    [Tooltip("כמות חתיכות קונפטי לבורסט UI")]
    public int confettiCount = 110;
    [Tooltip("משך חיים ממוצע לחתיכת קונפטי UI (שניות)")]
    public float confettiDuration = 1.2f;

    [Header("Hearts / Bar boundary")]
    [Tooltip("ה-RectTransform של אזור הבר התחתון. כשעוברים החוצה ממנו – יורד לב.")]
    public RectTransform barArea;
    [Tooltip("כאשר אין לבבות – לחסום התחלת גרירה?")]
    public bool blockDragWhenNoHearts = false;
    [Tooltip("רפרנס למנהל הלבבות (אם ריק, יילקח HeartsManager.Instance).")]
    public HeartsManager hearts;

    // --- Runtime ---
    private RectTransform _selfRT;
    private CanvasGroup _selfCG;
    private RectTransform _activeDrag;   // ויז׳ואל זמני בזמן גרירה
    private Image _activeDragImage;

    // לבבות: דגלים כדי לגרוע פעם אחת לכל גרירה
    private bool _spentHeartThisDrag;
    private bool _wasInsideBarAtLastCheck;

    // מצלמת ה-UI (לתיקון בדיקות מסך ב-Screen Space - Camera)
    private Camera UICamera =>
        (topCanvas != null && topCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            ? topCanvas.worldCamera
            : null;

    // Get camera for pointer conversions (fallback ל-UICamera אם pressEventCamera ריק)
    private Camera CamFor(PointerEventData ed) => ed.pressEventCamera != null ? ed.pressEventCamera : UICamera;

    void Awake()
    {
        _selfRT = GetComponent<RectTransform>();
        _selfCG = GetComponent<CanvasGroup>();
        if (topCanvas == null) topCanvas = GetComponentInParent<Canvas>();
        if (hearts == null) hearts = HeartsManager.Instance;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (blockDragWhenNoHearts && hearts != null && !hearts.HasHearts())
            return;

        if (topCanvas == null)
        {
            Debug.LogError("[SimpleDragFromBar] topCanvas לא הוגדר.");
            return;
        }

        // יצירת ויז׳ואל לגרירה
        if (dragVisualPrefab != null)
        {
            _activeDrag = Instantiate(dragVisualPrefab, topCanvas.transform);
            _activeDrag.localScale = Vector3.one * dragVisualScale;
            _activeDragImage = _activeDrag.GetComponentInChildren<Image>();
        }
        else
        {
            GameObject go = new GameObject("DragVisual", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _activeDrag = go.GetComponent<RectTransform>();
            _activeDrag.SetParent(topCanvas.transform, false);
            _activeDrag.localScale = Vector3.one * dragVisualScale;
            _activeDragImage = go.GetComponent<Image>();
        }

        // קביעת ספרייט
        Image srcImage = GetComponent<Image>();
        if (_activeDragImage != null)
        {
            _activeDragImage.raycastTarget = false;
            _activeDragImage.preserveAspect = true;
            _activeDragImage.sprite = overrideSprite != null ? overrideSprite : (srcImage != null ? srcImage.sprite : null);
        }

        // לא לחסום רייקאסטים
        var cg = _activeDrag.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        // גודל הוויז׳ואל
        _activeDrag.sizeDelta = (srcImage != null) ? srcImage.rectTransform.rect.size : _selfRT.rect.size;

        // מיקום התחלתי לפי המצביע
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)topCanvas.transform, eventData.position, CamFor(eventData), out var localPoint))
        {
            _activeDrag.anchoredPosition = localPoint;
        }

        if (_selfCG != null) _selfCG.alpha = 0.6f;

        // מעקב יציאה מהבר — אתחול
        _spentHeartThisDrag = false;
        _wasInsideBarAtLastCheck = IsPointInBar(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_activeDrag == null || topCanvas == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)topCanvas.transform, eventData.position, CamFor(eventData), out var localPoint))
        {
            _activeDrag.anchoredPosition = localPoint;
        }

        // הורדת לב בעת היציאה הראשונה מהבר
        if (barArea != null && hearts != null && !_spentHeartThisDrag)
        {
            bool nowInside = IsPointInBar(eventData.position);
            if (_wasInsideBarAtLastCheck && !nowInside)
            {
                hearts.LoseHeart(1);
                _spentHeartThisDrag = true;
            }
            _wasInsideBarAtLastCheck = nowInside;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_activeDrag == null)
        {
            if (_selfCG != null) _selfCG.alpha = 1f;
            return;
        }

        // רייקאסט UI למציאת DropSpot מתאים
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        DropSpot foundSpot = null;
        foreach (var r in results)
        {
            if (r.gameObject == null) continue;
            var spot = r.gameObject.GetComponentInParent<DropSpot>();
            if (spot != null && spot.Accepts(itemId)) { foundSpot = spot; break; }
        }

        if (foundSpot != null)
        {
            // הצלחה: מצמידים את הפריט לספוט
            foundSpot.SettleItem(_activeDrag);

            // קונפטי UI (עובד גם ב-Overlay)
            SpawnUIConfetti(foundSpot);

            if (sfxSource != null && correctSfx != null) sfxSource.PlayOneShot(correctSfx);

            if (removeBarButtonOnSuccess) Destroy(gameObject);
            else if (_selfCG != null) _selfCG.alpha = 1f;

            _activeDrag = null; // עכשיו חי תחת ה-Spot
        }
        else
        {
            if (sfxSource != null && wrongSfx != null) sfxSource.PlayOneShot(wrongSfx);

            Destroy(_activeDrag.gameObject);
            _activeDrag = null;

            if (_selfCG != null) _selfCG.alpha = 1f;
        }
    }

    private void SpawnUIConfetti(DropSpot spot)
    {
        if (topCanvas == null || spot == null) return;

        RectTransform spotRT = spot.GetComponent<RectTransform>();
        if (spotRT == null) spotRT = spot.transform as RectTransform;
        if (spotRT == null)
        {
            Debug.LogWarning("[SimpleDragFromBar] DropSpot אינו RectTransform – קונפטי UI לא יונפק.");
            return;
        }

        UIConfetti.Burst(topCanvas, spotRT, confettiCount, confettiDuration);
    }

    private bool IsPointInBar(Vector2 screenPos)
    {
        if (barArea == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(barArea, screenPos, UICamera);
    }
}
