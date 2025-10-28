using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// מערכת רמזים ויזואלית - עם אנימציית גדילה ביעד + תמונה אמיתית בגודל מלא!
/// </summary>
public class VisualHintSystem : MonoBehaviour
{
    [Header("🔗 חיבורים נדרשים")]
    [SerializeField] private ScrollableButtonBar buttonBar;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GameObject dropSpotsContainer;
    
    [Header("⌨️ הגדרות מקש (אופציונלי)")]
    [SerializeField] private bool enableKeyboardHint = false;
    [SerializeField] private KeyCode hintKey = KeyCode.H;
    
    [Header("🎨 הגדרות אנימציה")]
    [SerializeField] private float ghostStartScale = 0.3f;
    [Tooltip("גודל התחלתי - 30% מהגודל האמיתי")]
    [SerializeField] private float flyDuration = 1.5f;
    [SerializeField] private float growDuration = 0.5f;
    [Tooltip("משך אנימציית הגדילה ביעד")]
    [SerializeField] private float arcHeight = 100f;
    [SerializeField] private float ghostImageAlpha = 0.9f;
    
    [Header("⏱️ Cooldown")]
    [SerializeField] private float hintCooldown = 3f;
    
    [Header("🎵 אפקטים (אופציונלי)")]
    [SerializeField] private AudioClip hintStartSound;
    [SerializeField] private AudioClip hintArriveSound;
    [SerializeField] private AudioClip hintReturnSound;
    
    // משתנים פנימיים
    private bool isHintActive = false;
    private float lastHintTime = -999f;
    private GameObject currentGhostImage;
    private AudioSource audioSource;
    
    // ✅ Cache של DropSpots
    private static Dictionary<string, DropSpot> dropSpotCache;
    
    void Awake()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("🔷 [VisualHintSystem] מערכת רמזים מתאתחלת!");
        Debug.Log("═══════════════════════════════════════════");
        
        // בדיקת חיבורים
        if (buttonBar == null)
            Debug.LogError("❌ [VisualHintSystem] Button Bar לא מחובר!");
        else
            Debug.Log($"✅ [VisualHintSystem] Button Bar מחובר: {buttonBar.name}");
            
        if (mainCanvas == null)
            Debug.LogError("❌ [VisualHintSystem] Main Canvas לא מחובר!");
        else
            Debug.Log($"✅ [VisualHintSystem] Main Canvas מחובר: {mainCanvas.name}");
        
        // AudioSource (אופציונלי)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (hintStartSound != null || hintArriveSound != null || hintReturnSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("✅ [VisualHintSystem] AudioSource נוסף אוטומטית");
        }
        
        // ✅ רענן את ה-cache
        RefreshDropSpotCache();
        
        Debug.Log("═══════════════════════════════════════════\n");
    }
    
    void Start()
    {
        Debug.Log("🔷 [VisualHintSystem] המערכת מוכנה!");
        if (enableKeyboardHint)
        {
            Debug.Log($"🎮 לחץ על מקש '{hintKey}' כדי לקבל רמז\n");
        }
        else
        {
            Debug.Log("🎮 הרמזים מופעלים דרך Rewarded Ads בלבד\n");
        }
    }
    
    void Update()
    {
        // תמיכה אופציונלית במקש (למבחנים)
        if (enableKeyboardHint && Input.GetKeyDown(hintKey))
        {
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log($"🔥 [VisualHintSystem] לחצת על מקש {hintKey}!");
            Debug.Log("═══════════════════════════════════════════");
            
            TriggerHint();
        }
    }
    
    // ✅ רענון Cache של DropSpots
    private void RefreshDropSpotCache()
    {
        Debug.Log($"[VisualHintSystem] === REFRESH CACHE START ===");

        if (dropSpotCache == null)
        {
            dropSpotCache = new Dictionary<string, DropSpot>();
        }

        dropSpotCache.Clear();

        // ✅ מצא את כל ה-DropSpots (כולל לא פעילים)
        var allDropSpots = FindObjectsOfType<DropSpot>(true);

        Debug.Log($"[VisualHintSystem] Found {allDropSpots.Length} DropSpots in scene");

        foreach (var spot in allDropSpots)
        {
            if (!string.IsNullOrEmpty(spot.spotId))
            {
                if (!dropSpotCache.ContainsKey(spot.spotId))
                {
                    dropSpotCache[spot.spotId] = spot;
                    Debug.Log($"[VisualHintSystem] ✅ Cached: '{spot.spotId}'");
                }
                else
                {
                    Debug.LogWarning($"[VisualHintSystem] ⚠️ Duplicate spotId: '{spot.spotId}'");
                }
            }
        }

        Debug.Log($"[VisualHintSystem] === CACHE END === Total: {dropSpotCache.Count}");
    }
    
    // ✅ קבלת תמונה אמיתית מה-DropSpot
    private Sprite GetRealPhotoFromDropSpot(string buttonID)
    {
        if (dropSpotCache == null || dropSpotCache.Count == 0)
        {
            RefreshDropSpotCache();
        }

        if (dropSpotCache.TryGetValue(buttonID, out DropSpot spot))
        {
            var revealController = spot.GetComponent<ImageRevealController>();
            if (revealController != null)
            {
                var backgroundImage = revealController.GetBackgroundImage();
                
                if (backgroundImage != null && backgroundImage.sprite != null)
                {
                    Debug.Log($"[VisualHintSystem] ✅ Real photo found: {backgroundImage.sprite.name}");
                    return backgroundImage.sprite;
                }
            }
        }

        Debug.LogWarning($"[VisualHintSystem] ⚠️ No real photo for {buttonID}");
        return null;
    }
    
    // ✅ קבלת גודל התמונה האמיתית
    private Vector2 GetRealPhotoSizeFromDropSpot(string buttonID)
    {
        if (dropSpotCache == null || dropSpotCache.Count == 0)
        {
            RefreshDropSpotCache();
        }

        if (dropSpotCache.TryGetValue(buttonID, out DropSpot spot))
        {
            var revealController = spot.GetComponent<ImageRevealController>();
            if (revealController != null)
            {
                var backgroundImage = revealController.GetBackgroundImage();
                if (backgroundImage != null)
                {
                    var bgRT = backgroundImage.GetComponent<RectTransform>();
                    if (bgRT != null)
                    {
                        Vector2 size = bgRT.rect.size;
                        Debug.Log($"[VisualHintSystem] ✅ Real size: {size}");
                        return size;
                    }
                }
            }
            
            // fallback - גודל ה-DropSpot
            var spotRT = spot.GetComponent<RectTransform>();
            if (spotRT != null)
            {
                return spotRT.rect.size;
            }
        }

        return new Vector2(350f, 350f);
    }
    
    /// <summary>
    /// בדיקה האם יש כפתורים זמינים לרמז
    /// </summary>
    public bool HasAvailableButtons()
    {
        List<DraggableButton> available = FindAvailableButtons();
        return available.Count > 0;
    }
    
    /// <summary>
    /// מפעיל רמז - נקרא מ-HintDialog אחרי Rewarded Ad
    /// </summary>
    public void TriggerHint()
    {
        Debug.Log("───────────────────────────────────────────");
        Debug.Log("🎯 [VisualHintSystem] TriggerHint() נקרא!");
        
        // בדיקה 1: רמז כבר פעיל?
        if (isHintActive)
        {
            Debug.LogWarning("⏳ [VisualHintSystem] רמז כבר פעיל - מחכה שיסתיים");
            Debug.Log("───────────────────────────────────────────\n");
            return;
        }
        
        // בדיקה 2: Cooldown
        float timeSinceLastHint = Time.time - lastHintTime;
        if (timeSinceLastHint < hintCooldown)
        {
            float remaining = hintCooldown - timeSinceLastHint;
            Debug.LogWarning($"⏳ [VisualHintSystem] Cooldown - המתן {remaining:F1} שניות");
            Debug.Log("───────────────────────────────────────────\n");
            return;
        }
        
        // בדיקה 3: חיבורים
        if (buttonBar == null || mainCanvas == null)
        {
            Debug.LogError("❌ [VisualHintSystem] חסרים חיבורים נדרשים!");
            Debug.Log("───────────────────────────────────────────\n");
            return;
        }
        
        Debug.Log("✅ [VisualHintSystem] כל הבדיקות עברו - מחפש כפתורים זמינים...");
        
        // ✅ רענן cache
        RefreshDropSpotCache();
        
        // מציאת כפתורים זמינים
        List<DraggableButton> availableButtons = FindAvailableButtons();
        
        if (availableButtons.Count == 0)
        {
            Debug.LogWarning("❌ [VisualHintSystem] אין כפתורים זמינים להצגת רמז");
            Debug.Log("───────────────────────────────────────────\n");
            return;
        }
        
        Debug.Log($"✅ [VisualHintSystem] נמצאו {availableButtons.Count} כפתורים זמינים");
        
        // בחירת כפתור אקראי
        DraggableButton selectedButton = availableButtons[Random.Range(0, availableButtons.Count)];
        string buttonID = selectedButton.GetButtonID();
        
        Debug.Log($"🎲 [VisualHintSystem] נבחר כפתור: {buttonID}");
        
        // מציאת ה-DropSpot המתאים
        DropSpot targetSpot = FindMatchingDropSpot(buttonID);
        
        if (targetSpot == null)
        {
            Debug.LogError($"❌ [VisualHintSystem] לא נמצא DropSpot עבור {buttonID}");
            
            // ✅ הדפס מה יש ב-cache
            Debug.Log($"[VisualHintSystem] Available spots in cache:");
            foreach (var key in dropSpotCache.Keys)
            {
                Debug.Log($"  - '{key}'");
            }
            
            Debug.Log("───────────────────────────────────────────\n");
            return;
        }
        
        Debug.Log($"✅ [VisualHintSystem] נמצא יעד: {targetSpot.spotId}");
        Debug.Log("🎬 [VisualHintSystem] מתחיל אנימציית רמז...");
        Debug.Log("───────────────────────────────────────────\n");
        
        // הפעלת האנימציה
        StartCoroutine(ShowHintAnimation(selectedButton, targetSpot));
    }
    
    private List<DraggableButton> FindAvailableButtons()
    {
        List<DraggableButton> available = new List<DraggableButton>();
        
        if (buttonBar == null) return available;
        
        DraggableButton[] allButtons = buttonBar.GetComponentsInChildren<DraggableButton>(includeInactive: false);
        
        foreach (var btn in allButtons)
        {
            if (btn == null) continue;
            if (!btn.HasBeenPlaced())
            {
                available.Add(btn);
            }
        }
        
        return available;
    }
    
    // ✅ משתמש ב-cache
    private DropSpot FindMatchingDropSpot(string buttonID)
    {
        if (dropSpotCache == null || dropSpotCache.Count == 0)
        {
            RefreshDropSpotCache();
        }

        if (dropSpotCache.TryGetValue(buttonID, out DropSpot spot))
        {
            if (spot.gameObject.activeInHierarchy && !spot.IsSettled)
            {
                return spot;
            }
        }
        
        return null;
    }
    
    private IEnumerator ShowHintAnimation(DraggableButton button, DropSpot targetSpot)
    {
        isHintActive = true;
        lastHintTime = Time.time;
        
        Debug.Log("┌─────────────────────────────────────────┐");
        Debug.Log("│  🎬 אנימציית רמז - התחלה                │");
        Debug.Log("└─────────────────────────────────────────┘");
        
        string buttonID = button.GetButtonID();
        
        // אפקט זוהר על הכפתור המקורי
        AddGlowEffect(button.gameObject);
        
        // ✅ יצירת Ghost עם תמונה אמיתית!
        currentGhostImage = CreateGhostImage(button, buttonID);
        
        if (currentGhostImage == null)
        {
            Debug.LogError("❌ [VisualHintSystem] נכשל ביצירת Ghost Image!");
            isHintActive = false;
            yield break;
        }
        
        Debug.Log("✅ Ghost Image נוצר עם תמונה אמיתית!");
        
        if (hintStartSound != null && audioSource != null)
            audioSource.PlayOneShot(hintStartSound);
        
        RectTransform ghostRT = currentGhostImage.GetComponent<RectTransform>();
        RectTransform buttonRT = button.GetComponent<RectTransform>();
        RectTransform targetRT = targetSpot.GetComponent<RectTransform>();
        
        Vector3 startPos = buttonRT.position;
        Vector3 endPos = targetRT.position;
        
        // ✅ גודל אמיתי של התמונה
        Vector2 realPhotoSize = GetRealPhotoSizeFromDropSpot(buttonID);
        
        // שלב 1: טיסה ליעד + גדילה לגודל מלא
        Debug.Log("🚀 שלב 1/3: טיסה ליעד + גדילה לגודל מלא...");
        float elapsed = 0f;
        
        while (elapsed < flyDuration)
        {
            if (ghostRT == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            float easedT = EaseOutQuad(t);
            
            // תנועה בקשת
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, easedT);
            currentPos.y += Mathf.Sin(easedT * Mathf.PI) * arcHeight;
            ghostRT.position = currentPos;
            
            // ✅ גדילה לגודל מלא של התמונה
            Vector2 startSize = realPhotoSize * ghostStartScale;
            ghostRT.sizeDelta = Vector2.Lerp(startSize, realPhotoSize, easedT);
            
            yield return null;
        }
        
        ghostRT.position = endPos;
        ghostRT.sizeDelta = realPhotoSize;
        Debug.Log("✅ הגיע ליעד בגודל מלא!");
        
        // שלב 2: אפקט פעימה ביעד
        Debug.Log("💓 שלב 2/3: אפקט פעימה...");
        
        if (hintArriveSound != null && audioSource != null)
            audioSource.PlayOneShot(hintArriveSound);
        
        AddPulseEffect(targetSpot.gameObject);
        
        elapsed = 0f;
        while (elapsed < 0.7f)
        {
            if (ghostRT == null) yield break;
            
            elapsed += Time.deltaTime;
            float pulseT = Mathf.PingPong(elapsed * 3f, 1f);
            float scale = Mathf.Lerp(1f, 1.05f, pulseT);
            ghostRT.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        ghostRT.localScale = Vector3.one;
        Debug.Log("✅ פעימה הושלמה!");
        
        // שלב 3: חזרה לבר
        Debug.Log("🔙 שלב 3/3: חזרה לבר...");
        elapsed = 0f;
        startPos = ghostRT.position;
        Vector2 currentSize = ghostRT.sizeDelta;
        Vector2 endSize = realPhotoSize * ghostStartScale;
        
        CanvasGroup ghostCG = currentGhostImage.GetComponent<CanvasGroup>();
        
        while (elapsed < flyDuration * 0.7f)
        {
            if (ghostRT == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / (flyDuration * 0.7f);
            
            ghostRT.position = Vector3.Lerp(startPos, buttonRT.position, t);
            ghostRT.sizeDelta = Vector2.Lerp(currentSize, endSize, t);
            
            if (ghostCG != null)
                ghostCG.alpha = Mathf.Lerp(ghostImageAlpha, 0f, t);
            
            yield return null;
        }
        
        Debug.Log("✅ חזר לבר!");
        
        // ניקוי
        if (currentGhostImage != null)
        {
            Destroy(currentGhostImage);
            Debug.Log("🗑️ Ghost Image נמחק");
        }
        
        if (hintReturnSound != null && audioSource != null)
            audioSource.PlayOneShot(hintReturnSound);
        
        Debug.Log("┌─────────────────────────────────────────┐");
        Debug.Log("│  ✅ אנימציית רמז הושלמה!                │");
        Debug.Log("└─────────────────────────────────────────┘\n");
        
        isHintActive = false;
    }
    
    // ✅ יצירת Ghost עם תמונה אמיתית!
    private GameObject CreateGhostImage(DraggableButton button, string buttonID)
    {
        GameObject ghost = new GameObject("HintGhost_" + buttonID);
        ghost.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform ghostRT = ghost.AddComponent<RectTransform>();
        Image ghostImage = ghost.AddComponent<Image>();
        CanvasGroup ghostCG = ghost.AddComponent<CanvasGroup>();
        
        // ✅ קבל תמונה אמיתית מה-DropSpot!
        Sprite realPhoto = GetRealPhotoFromDropSpot(buttonID);
        
        if (realPhoto != null)
        {
            ghostImage.sprite = realPhoto;
            Debug.Log($"[VisualHintSystem] ✅ Using real photo!");
        }
        else
        {
            // fallback - תמונה מהכפתור
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null && buttonImage.sprite != null)
            {
                ghostImage.sprite = buttonImage.sprite;
                Debug.Log($"[VisualHintSystem] ⚠️ Fallback: button sprite");
            }
        }
        
        ghostImage.preserveAspect = true;
        ghostImage.raycastTarget = false;
        
        // ✅ גודל התחלתי קטן
        Vector2 realSize = GetRealPhotoSizeFromDropSpot(buttonID);
        ghostRT.sizeDelta = realSize * ghostStartScale;
        
        RectTransform buttonRT = button.GetComponent<RectTransform>();
        ghostRT.position = buttonRT.position;
        ghostRT.localScale = Vector3.one;
        
        ghostCG.alpha = ghostImageAlpha;
        ghostCG.blocksRaycasts = false;
        ghostCG.interactable = false;
        
        return ghost;
    }
    
    // EaseOutQuad - בדיוק כמו ב-DraggableButton!
    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
    
    private void AddGlowEffect(GameObject target)
    {
        Image img = target.GetComponent<Image>();
        if (img != null)
        {
            StartCoroutine(GlowCoroutine(img));
        }
    }
    
    private IEnumerator GlowCoroutine(Image img)
    {
        Color originalColor = img.color;
        float elapsed = 0f;
        
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.PingPong(elapsed * 4f, 1f);
            img.color = Color.Lerp(originalColor, Color.yellow, alpha * 0.3f);
            yield return null;
        }
        
        img.color = originalColor;
    }
    
    private void AddPulseEffect(GameObject target)
    {
        StartCoroutine(PulseCoroutine(target.transform));
    }
    
    private IEnumerator PulseCoroutine(Transform target)
    {
        Vector3 originalScale = target.localScale;
        float elapsed = 0f;
        
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float scale = 1f + Mathf.Sin(elapsed * 12f) * 0.1f;
            target.localScale = originalScale * scale;
            yield return null;
        }
        
        target.localScale = originalScale;
    }
}