using UnityEngine;

public class TutorialSlideManager : MonoBehaviour
{
    public static TutorialSlideManager Instance;
    
    [Header("Tutorial Slides")]
    [SerializeField] private GameObject stage1Slide;
    [SerializeField] private GameObject stage2Slide;
    [SerializeField] private GameObject stage3Slide;
    
    [Header("Settings")]
    [SerializeField] private bool skipIfCompleted = true;
    
    private int currentStage = 0;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[TutorialSlideManager] Multiple instances detected!");
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // בדוק אם השחקן כבר עבר את הטוטוריאל
        if (skipIfCompleted)
        {
            bool tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
            
            if (tutorialCompleted)
            {
                // כבר עבר - הסתר הכל ואל תתחיל
                Debug.Log("[TutorialSlideManager] Tutorial already completed - skipping");
                HideAllSlides();
                enabled = false; // כבה את הסקריפט
                return;
            }
        }
        
        // פעם ראשונה - הצג שלב 1
        ShowStage(1);
    }
    
    /// <summary>
    /// הצג שקופית לפי מספר שלב
    /// </summary>
    public void ShowStage(int stageNumber)
    {
        currentStage = stageNumber;
        
        // הסתר את כל השקופיות
        HideAllSlides();
        
        // הצג את השקופית הנכונה
        switch (stageNumber)
        {
            case 1:
                if (stage1Slide != null)
                {
                    stage1Slide.SetActive(true);
                    Debug.Log("[TutorialSlideManager] Showing Stage 1: Drag elephant to outline");
                }
                else
                {
                    Debug.LogWarning("[TutorialSlideManager] Stage 1 slide is not assigned!");
                }
                break;
                
            case 2:
                if (stage2Slide != null)
                {
                    stage2Slide.SetActive(true);
                    Debug.Log("[TutorialSlideManager] Showing Stage 2");
                }
                else
                {
                    Debug.LogWarning("[TutorialSlideManager] Stage 2 slide is not assigned - completing tutorial");
                    CompleteTutorial();
                }
                break;
                
            case 3:
                if (stage3Slide != null)
                {
                    stage3Slide.SetActive(true);
                    Debug.Log("[TutorialSlideManager] Showing Stage 3");
                }
                else
                {
                    Debug.LogWarning("[TutorialSlideManager] Stage 3 slide is not assigned - completing tutorial");
                    CompleteTutorial();
                }
                break;
                
            default:
                // הטוטוריאל הסתיים!
                CompleteTutorial();
                break;
        }
    }
    
    /// <summary>
    /// נקרא מ-DropSpot כשפריט נכון הונח
    /// </summary>
    public void OnCorrectDrop(string itemName)
    {
        Debug.Log($"[TutorialSlideManager] Correct drop detected: {itemName} (Current stage: {currentStage})");
        
        // עבור לשלב הבא
        ShowStage(currentStage + 1);
    }
    
    /// <summary>
    /// הסתר את כל השקופיות
    /// </summary>
    void HideAllSlides()
    {
        if (stage1Slide != null) stage1Slide.SetActive(false);
        if (stage2Slide != null) stage2Slide.SetActive(false);
        if (stage3Slide != null) stage3Slide.SetActive(false);
    }
    
    /// <summary>
    /// סיים את הטוטוריאל ושמור
    /// </summary>
    void CompleteTutorial()
    {
        Debug.Log("[TutorialSlideManager] ✅ Tutorial completed!");
        
        // שמור שהטוטוריאל הושלם
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        
        HideAllSlides();
        
        // כבה את הסקריפט
        enabled = false;
    }
    
    /// <summary>
    /// סיים את הטוטוריאל מיד (ללא שמירה)
    /// </summary>
    public void SkipTutorial()
    {
        Debug.Log("[TutorialSlideManager] Tutorial skipped by user");
        CompleteTutorial();
    }
    
    // 🔧 כלי לבדיקות
    [ContextMenu("Reset Tutorial (Show Again)")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialCompleted");
        PlayerPrefs.Save();
        currentStage = 0;
        enabled = true;
        ShowStage(1);
        Debug.Log("[TutorialSlideManager] 🔄 Tutorial reset - will show on next run!");
    }
    
    [ContextMenu("Complete Tutorial Now")]
    public void ForceCompleteTutorial()
    {
        CompleteTutorial();
    }
}
