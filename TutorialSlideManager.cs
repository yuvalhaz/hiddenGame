using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialSlideManager : MonoBehaviour
{
    public static TutorialSlideManager Instance;

    [Header("Tutorial Slides")]
    [SerializeField] private GameObject stage1Slide;
    [SerializeField] private GameObject stage2Slide;
    [SerializeField] private GameObject stage3Slide;
    [SerializeField] private GameObject stage4Slide; // "Click the hint button"
    [SerializeField] private GameObject stage5Slide; // Fifth item

    [Header("Hint Button Control")]
    [Tooltip("The hint button GameObject - will be shown only from stage 4")]
    [SerializeField] private GameObject hintButtonObject;

    [Header("Timing Settings")]
    [SerializeField] private float delayBeforeFirstSlide = 1f;
    [SerializeField] private float delayBetweenSlides = 3f;

    [Header("Settings")]
    [SerializeField] private bool skipIfCompleted = true;
    [SerializeField] private string levelSelectionSceneName = "LevelSelection";
    [SerializeField] private float delayBeforeLoadingMenu = 5f;
    [Tooltip("Delay in seconds before loading level selection after tutorial completes")]

    private int currentStage = 0;
    private bool isTransitioning = false;
    private bool hintClickedInStage4 = false; // Track if hint was clicked during stage 4
    
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

        // Hide hint button initially - will be shown in stage 4
        if (hintButtonObject != null)
        {
            hintButtonObject.SetActive(false);
            Debug.Log("[TutorialSlideManager] Hint button hidden - will appear in stage 4");
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
                // Show hint button since tutorial is complete
                if (hintButtonObject != null)
                {
                    hintButtonObject.SetActive(true);
                }
                enabled = false;
                return;
            }
        }

        // Check how many items are already placed and start from appropriate stage
        int settledCount = CountSettledItems();
        int startStage = settledCount + 1; // If 0 settled → stage 1, if 1 settled → stage 2, etc.

        if (startStage > 5)
        {
            Debug.Log("[TutorialSlideManager] All items already placed - completing tutorial");
            CompleteTutorial();
            return;
        }

        // If starting from stage 4 or later, show hint button immediately
        if (startStage >= 4 && hintButtonObject != null)
        {
            hintButtonObject.SetActive(true);
            Debug.Log("[TutorialSlideManager] Starting from stage 4+ - hint button visible");
        }

        Debug.Log($"[TutorialSlideManager] Found {settledCount} items already placed - starting from stage {startStage}");

        // Wait 3 seconds then show the appropriate stage
        StartCoroutine(ShowStageAfterDelay(startStage, delayBeforeFirstSlide));
    }

    /// <summary>
    /// Count how many items are already settled/placed
    /// </summary>
    private int CountSettledItems()
    {
        int count = 0;

        // Use GameProgressManager which persists between sessions
        if (GameProgressManager.Instance != null)
        {
            DropSpot[] allDropSpots = DropSpotCache.GetAll();

            foreach (DropSpot spot in allDropSpots)
            {
                if (spot != null && GameProgressManager.Instance.IsItemPlaced(spot.spotId))
                {
                    count++;
                    Debug.Log($"[TutorialSlideManager] Found placed item (from save): {spot.spotId}");
                }
            }
        }
        else
        {
            // Fallback to runtime check if no GameProgressManager
            DropSpot[] allDropSpots = DropSpotCache.GetAll();

            foreach (DropSpot spot in allDropSpots)
            {
                if (spot != null && spot.IsSettled)
                {
                    count++;
                    Debug.Log($"[TutorialSlideManager] Found settled item (runtime): {spot.spotId}");
                }
            }
        }

        return count;
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

            case 4:
                // Stage 4: Teach player to use hint button
                // IMPORTANT: Show the hint button for the first time!
                if (hintButtonObject != null)
                {
                    hintButtonObject.SetActive(true);
                    Debug.Log("[TutorialSlideManager] 💡 Hint button now visible!");
                }

                if (stage4Slide != null)
                {
                    stage4Slide.SetActive(true);
                    hintClickedInStage4 = false; // Reset hint click flag
                    Debug.Log("[TutorialSlideManager] Showing Stage 4: Click the hint button");
                }
                else
                {
                    Debug.LogWarning("[TutorialSlideManager] Stage 4 slide is not assigned - completing tutorial");
                    CompleteTutorial();
                }
                break;

            case 5:
                // Stage 5: Fifth item
                if (stage5Slide != null)
                {
                    stage5Slide.SetActive(true);
                    Debug.Log("[TutorialSlideManager] Showing Stage 5");
                }
                else
                {
                    Debug.LogWarning("[TutorialSlideManager] Stage 5 slide is not assigned - completing tutorial");
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

        // Special handling for stage 4: Must click hint first
        if (currentStage == 4 && !hintClickedInStage4)
        {
            Debug.LogWarning("[TutorialSlideManager] Stage 4: Player placed item without clicking hint first!");
            // Item is already placed, but we should have required hint click first
            // Continue anyway but log the warning
        }

        // Immediately hide current slide
        HideAllSlides();

        // Wait 3 seconds before showing next stage
        int nextStage = currentStage + 1;
        StartCoroutine(ShowStageAfterDelay(nextStage, delayBetweenSlides));
    }

    /// <summary>
    /// Called from HintButton when hint button is clicked.
    /// Hook this up to the HintButton's onPressed event in the Unity Inspector.
    /// </summary>
    public void OnHintButtonClicked()
    {
        Debug.Log($"[TutorialSlideManager] Hint button clicked (Current stage: {currentStage})");

        // Special handling for stage 4: Hide the tutorial slide when hint is clicked
        if (currentStage == 4 && !hintClickedInStage4)
        {
            hintClickedInStage4 = true;
            HideAllSlides();
            Debug.Log("[TutorialSlideManager] ✅ Stage 4: Hint button clicked! Player can now place the item.");
        }
    }

    /// <summary>
    /// Check if dragging is allowed. Called from DraggableButton before starting drag.
    /// In stage 4, player must click hint button before dragging.
    /// </summary>
    public bool CanStartDrag()
    {
        // Stage 4: Must click hint button first!
        if (currentStage == 4 && !hintClickedInStage4)
        {
            Debug.Log("[TutorialSlideManager] ⚠️ Stage 4: Cannot drag yet - must click hint button first!");
            return false;
        }

        // All other stages: allow dragging
        return true;
    }
    
    /// <summary>
    /// הסתר את כל השקופיות
    /// </summary>
    void HideAllSlides()
    {
        if (stage1Slide != null) stage1Slide.SetActive(false);
        if (stage2Slide != null) stage2Slide.SetActive(false);
        if (stage3Slide != null) stage3Slide.SetActive(false);
        if (stage4Slide != null) stage4Slide.SetActive(false);
        if (stage5Slide != null) stage5Slide.SetActive(false);
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

        // Make sure hint button stays visible after tutorial
        if (hintButtonObject != null)
        {
            hintButtonObject.SetActive(true);
            Debug.Log("[TutorialSlideManager] Hint button will remain visible");
        }

        // Try to trigger level complete controller if it exists
        LevelCompleteController levelComplete = FindObjectOfType<LevelCompleteController>();
        if (levelComplete != null)
        {
            Debug.Log("[TutorialSlideManager] Triggering LevelCompleteController...");
            levelComplete.TriggerLevelComplete();
        }
        else
        {
            Debug.Log("[TutorialSlideManager] No LevelCompleteController found - loading level selection after delay");
            StartCoroutine(LoadLevelSelectionAfterDelay());
        }

        // כבה את הסקריפט
        enabled = false;
    }

    /// <summary>
    /// Load level selection scene after delay
    /// </summary>
    private IEnumerator LoadLevelSelectionAfterDelay()
    {
        Debug.Log($"[TutorialSlideManager] Waiting {delayBeforeLoadingMenu} seconds before loading level selection...");
        yield return new WaitForSeconds(delayBeforeLoadingMenu);

        Debug.Log($"[TutorialSlideManager] Loading {levelSelectionSceneName}...");
        SceneManager.LoadScene(levelSelectionSceneName);
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
