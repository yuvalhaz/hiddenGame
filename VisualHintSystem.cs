using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// מערכת רמזים ויזואלית - עם אנימציית גדילה ביעד!
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
    [SerializeField] private float ghostMidScale = 1.0f;
    [Tooltip("גודל בזמן הטיסה")]
    [SerializeField] private float ghostTargetScale = 1.5f;
    [Tooltip("גודל סופי ביעד - כמו הכפתור האמיתי!")]
    [SerializeField] private float flyDuration = 1.5f;
    [SerializeField] private float growDuration = 0.5f;
    [Tooltip("משך אנימציית הגדילה ביעד")]
    [SerializeField] private float arcHeight = 100f;
    [SerializeField] private float ghostImageAlpha = 0.7f;
    
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
            
        if (dropSpotsContainer == null)
            Debug.LogError("❌ [VisualHintSystem] Drop Spots Container לא מחובר!");
        else
            Debug.Log($"✅ [VisualHintSystem] Drop Spots Container מחובר: {dropSpotsContainer.name}");
        
        // AudioSource (אופציונלי)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (hintStartSound != null || hintArriveSound != null || hintReturnSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("✅ [VisualHintSystem] AudioSource נוסף אוטומטית");
        }
        
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
        if (buttonBar == null || mainCanvas == null || dropSpotsContainer == null)
        {
            Debug.LogError("❌ [VisualHintSystem] חסרים חיבורים נדרשים!");
            Debug.Log("───────────────────────────────────────────\n");
            return;
        }
        
        Debug.Log("✅ [VisualHintSystem] כל הבדיקות עברו - מחפש כפתורים זמינים...");
        
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
    
    private DropSpot FindMatchingDropSpot(string buttonID)
    {
        if (dropSpotsContainer == null) return null;
        
        DropSpot[] allSpots = dropSpotsContainer.GetComponentsInChildren<DropSpot>(includeInactive: false);
        
        foreach (var spot in allSpots)
        {
            if (spot == null) continue;
            if (spot.spotId == buttonID && !spot.IsSettled)
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
        
        // אפקט זוהר על הכפתור המקורי
        AddGlowEffect(button.gameObject);
        
        // יצירת Ghost Image
        currentGhostImage = CreateGhostImage(button);
        
        if (currentGhostImage == null)
        {
            Debug.LogError("❌ [VisualHintSystem] נכשל ביצירת Ghost Image!");
            isHintActive = false;
            yield break;
        }
        
        Debug.Log("✅ Ghost Image נוצר");
        
        if (hintStartSound != null && audioSource != null)
            audioSource.PlayOneShot(hintStartSound);
        
        RectTransform ghostRT = currentGhostImage.GetComponent<RectTransform>();
        RectTransform buttonRT = button.GetComponent<RectTransform>();
        RectTransform targetRT = targetSpot.GetComponent<RectTransform>();
        
        Vector3 startPos = buttonRT.position;
        Vector3 endPos = targetRT.position;
        
        // שלב 1: טיסה ליעד
        Debug.Log("🚀 שלב 1/4: טיסה ליעד...");
        float elapsed = 0f;
        
        while (elapsed < flyDuration)
        {
            if (ghostRT == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            
            // תנועה בקשת
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            ghostRT.position = currentPos;
            
            // שינוי גודל - מגדיל עד ghostMidScale
            float scale = Mathf.Lerp(ghostStartScale, ghostMidScale, t);
            ghostRT.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        ghostRT.position = endPos;
        ghostRT.localScale = Vector3.one * ghostMidScale;
        Debug.Log("✅ הגיע ליעד!");
        
        // שלב 2: אנימציית גדילה ביעד! 🎉
        Debug.Log($"📈 שלב 2/4: גדילה ביעד ({ghostMidScale} → {ghostTargetScale})...");
        
        if (hintArriveSound != null && audioSource != null)
            audioSource.PlayOneShot(hintArriveSound);
        
        // אפקט פעימה על היעד
        AddPulseEffect(targetSpot.gameObject);
        
        elapsed = 0f;
        while (elapsed < growDuration)
        {
            if (ghostRT == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / growDuration;
            
            // EaseOutQuad - בדיוק כמו ב-DraggableButton!
            float easedT = EaseOutQuad(t);
            
            float scale = Mathf.Lerp(ghostMidScale, ghostTargetScale, easedT);
            ghostRT.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        ghostRT.localScale = Vector3.one * ghostTargetScale;
        Debug.Log("✅ גדל למקסימום!");
        
        // שלב 3: המתנה ביעד
        Debug.Log("⏸️ שלב 3/4: המתנה ביעד (0.5 שניות)...");
        yield return new WaitForSeconds(0.5f);
        
        // שלב 4: חזרה לבר
        Debug.Log("🔙 שלב 4/4: חזרה לבר...");
        elapsed = 0f;
        startPos = ghostRT.position;
        
        CanvasGroup ghostCG = currentGhostImage.GetComponent<CanvasGroup>();
        
        while (elapsed < flyDuration * 0.7f)
        {
            if (ghostRT == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / (flyDuration * 0.7f);
            
            ghostRT.position = Vector3.Lerp(startPos, buttonRT.position, t);
            
            // מקטין חזרה תוך כדי חזרה
            float scale = Mathf.Lerp(ghostTargetScale, ghostStartScale, t);
            ghostRT.localScale = Vector3.one * scale;
            
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
    
    private GameObject CreateGhostImage(DraggableButton button)
    {
        GameObject ghost = new GameObject("HintGhost");
        ghost.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform ghostRT = ghost.AddComponent<RectTransform>();
        Image ghostImage = ghost.AddComponent<Image>();
        CanvasGroup ghostCG = ghost.AddComponent<CanvasGroup>();
        
        // העתקת תמונה מהכפתור
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null && buttonImage.sprite != null)
        {
            ghostImage.sprite = buttonImage.sprite;
        }
        
        // הגדרות
        RectTransform buttonRT = button.GetComponent<RectTransform>();
        ghostRT.sizeDelta = buttonRT.sizeDelta;
        ghostRT.position = buttonRT.position;
        ghostRT.localScale = Vector3.one * ghostStartScale;
        
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
