using UnityEngine;
using System.Collections;

public class TutorialSlideManager : MonoBehaviour
{
    public static TutorialSlideManager Instance;

    [Header("Tutorial Slides")]
    [SerializeField] private GameObject stage1Slide;
    [SerializeField] private GameObject stage2Slide;
    [SerializeField] private GameObject stage3Slide;

    [Header("Timing Settings")]
    [SerializeField] private float delayBeforeFirstSlide = 3f;
    [SerializeField] private float delayBetweenSlides = 3f;

    [Header("Settings")]
    [SerializeField] private bool skipIfCompleted = true;

    private int currentStage = 0;
    private bool isTransitioning = false;
    
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
        // Check if tutorial already completed
        if (skipIfCompleted)
        {
            bool tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

            if (tutorialCompleted)
            {
                Debug.Log("[TutorialSlideManager] Tutorial already completed - skipping");
                HideAllSlides();
                enabled = false;
                return;
            }
        }

        // First time - wait 3 seconds then show stage 1
        StartCoroutine(ShowStageAfterDelay(1, delayBeforeFirstSlide));
    }

    /// <summary>
    /// Show slide after delay (coroutine)
    /// </summary>
    private IEnumerator ShowStageAfterDelay(int stageNumber, float delay)
    {
        isTransitioning = true;

        Debug.Log($"[TutorialSlideManager] Waiting {delay} seconds before showing stage {stageNumber}...");
        yield return new WaitForSeconds(delay);

        ShowStageImmediate(stageNumber);
        isTransitioning = false;
    }
    
    /// <summary>
    /// Show slide immediately (no delay)
    /// </summary>
    public void ShowStage(int stageNumber)
    {
        ShowStageImmediate(stageNumber);
    }

    /// <summary>
    /// Internal method to show slide immediately
    /// </summary>
    private void ShowStageImmediate(int stageNumber)
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
    /// Called from DropSpot when correct item is placed
    /// </summary>
    public void OnCorrectDrop(string itemName)
    {
        Debug.Log($"[TutorialSlideManager] Correct drop detected: {itemName} (Current stage: {currentStage})");

        // Immediately hide current slide
        HideAllSlides();

        // Wait 3 seconds before showing next stage
        int nextStage = currentStage + 1;
        StartCoroutine(ShowStageAfterDelay(nextStage, delayBetweenSlides));
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
