using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// מטפל בסיום Level - מציג מסך ניצחון, פותח level הבא, וטוען scene
/// Works with LevelManager for proper level progression
/// </summary>
public class LevelCompleteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelData currentLevelData;
    [Tooltip("The LevelData for this specific level")]

    [SerializeField] private LevelData nextLevelData;
    [Tooltip("Optional: The next level to unlock")]

    [Header("UI")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private Text completionText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button menuButton;

    [Header("Settings")]
    [SerializeField] private string levelSelectionSceneName = "LevelSelection";
    [SerializeField] private float autoLoadDelay = 5f;
    [Tooltip("Auto-load level selection after X seconds (0 = disabled)")]
    [SerializeField] private bool goToLevelSelectionInsteadOfNextLevel = true;
    [Tooltip("If true, goes to level selection after delay. If false, advances to next level.")]

    [Header("🎬 Ending Dialog")]
    [SerializeField] private EndingDialogController endingDialog;
    [Tooltip("Optional: Play ending dialog before completion screen")]
    [SerializeField] private bool useEndingDialog = true;

    [Header("Ad Integration")]
    [SerializeField] private bool showAdOnCompletion = false;
    [Tooltip("Show rewarded ad after level completion")]
    [SerializeField] private bool skipAdsInEditor = true;
    [Tooltip("Skip ads when running in Unity Editor")]

    private bool isCompleted = false;

    private void Start()
    {
        // Hide completion panel at start
        if (completionPanel != null)
            completionPanel.SetActive(false);

        // Setup buttons
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);

        if (menuButton != null)
            menuButton.onClick.AddListener(LoadMenu);
    }

    private void OnDestroy()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(OnNextLevelButtonClicked);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(LoadMenu);
    }

    /// <summary>
    /// Call this when the player completes all items in the level
    /// This is called by LevelManager when level is complete
    /// </summary>
    public void TriggerLevelComplete()
    {
        if (isCompleted)
        {
            Debug.LogWarning("[LevelCompleteController] Level already completed!");
            return;
        }

        isCompleted = true;
        Debug.Log("[LevelCompleteController] 🎉 Level Complete triggered!");

        // Use ending dialog if enabled
        if (useEndingDialog && endingDialog != null)
        {
            StartCoroutine(PlayEndingDialogThenComplete());
        }
        else
        {
            CompleteLevelImmediate();
        }
    }

    private IEnumerator PlayEndingDialogThenComplete()
    {
        Debug.Log("[LevelCompleteController] Playing ending dialog...");

        // ✅ השלם את הלבל כאן - לפני הבועות!
        if (LevelManager.Instance != null)
        {
            Debug.Log("[LevelCompleteController] ✅ Completing level and advancing pointer...");
            LevelManager.Instance.CompleteCurrentLevelAndAdvancePointer();
        }
        else
        {
            Debug.LogError("[LevelCompleteController] ❌ LevelManager is NULL!");
        }

        // Start ending dialog
        endingDialog.StartEndingDialog();

        // EndingDialogController will just load LevelSelection when bubble is clicked
        yield break;
    }

    private void CompleteLevelImmediate()
    {
        // Mark level as completed (if using LevelData system)
        if (currentLevelData != null)
        {
            currentLevelData.MarkCompleted();
            Debug.Log($"[LevelCompleteController] ✅ {currentLevelData.levelName} completed!");
        }

        // Unlock next level (if using LevelData system)
        if (nextLevelData != null)
        {
            nextLevelData.Unlock();
            Debug.Log($"[LevelCompleteController] 🔓 {nextLevelData.levelName} unlocked!");
        }

        // Show completion screen
        ShowCompletionScreen();

        // Show ad if enabled, then proceed
        if (showAdOnCompletion)
        {
            StartCoroutine(ShowAdThenProceed());
        }
        else if (autoLoadDelay > 0)
        {
            StartCoroutine(AutoLoadAfterDelay());
        }
    }

    private IEnumerator ShowAdThenProceed()
    {
        // Wait a bit for UI to be visible
        yield return new WaitForSeconds(1.5f);

        // ✅ Check if we should skip ads (in Editor)
        #if UNITY_EDITOR
        if (skipAdsInEditor)
        {
            Debug.Log("[LevelCompleteController] ⏭️ Skipping ad in Editor");
            ProceedToNextLevel();
            yield break;
        }
        #endif

        bool adFinished = false;

        // Try to show ad
        if (RewardedAdsManager.Instance != null && RewardedAdsManager.Instance.IsReady())
        {
            Debug.Log("[LevelCompleteController] 📺 Showing rewarded ad...");
            
            RewardedAdsManager.Instance.ShowRewarded(
                onReward: () =>
                {
                    Debug.Log("[LevelCompleteController] Ad reward received!");
                    adFinished = true;
                },
                onClosed: (completed) =>
                {
                    Debug.Log("[LevelCompleteController] Ad closed");
                    adFinished = true;
                },
                onFailed: (error) =>
                {
                    Debug.LogWarning($"[LevelCompleteController] Ad failed: {error}");
                    adFinished = true;
                }
            );

            // ✅ Wait for ad with timeout
            float timeout = 5f; // Shorter timeout for testing
            float elapsed = 0f;
            while (!adFinished && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogWarning("[LevelCompleteController] ⏰ Ad timed out! Continuing anyway...");
            }
        }
        else
        {
            Debug.Log("[LevelCompleteController] No ads available, proceeding...");
        }
        
        // ✅ Always proceed, regardless of ad status
        ProceedToNextLevel();
    }

    private void ShowCompletionScreen()
    {
        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
        }

        if (completionText != null)
        {
            if (currentLevelData != null)
            {
                completionText.text = $"{currentLevelData.levelName} Complete!";
            }
            else
            {
                completionText.text = "Level Complete!";
            }
        }

        // Enable/disable next button based on availability
        if (nextLevelButton != null)
        {
            if (nextLevelData != null || LevelManager.Instance != null)
            {
                nextLevelButton.gameObject.SetActive(true);
                var buttonText = nextLevelButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    if (nextLevelData != null)
                        buttonText.text = $"Next: {nextLevelData.levelName}";
                    else
                        buttonText.text = "Next Level";
                }
            }
            else
            {
                nextLevelButton.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator AutoLoadAfterDelay()
    {
        yield return new WaitForSeconds(autoLoadDelay);

        // Check if we should go to level selection or next level
        if (goToLevelSelectionInsteadOfNextLevel)
        {
            Debug.Log($"[LevelCompleteController] Auto-loading level selection after {autoLoadDelay} seconds");
            LoadMenu();
        }
        else
        {
            ProceedToNextLevel();
        }
    }

    private void OnNextLevelButtonClicked()
    {
        Debug.Log("[LevelCompleteController] Next button clicked");
        ProceedToNextLevel();
    }

    /// <summary>
    /// Proceed to next level - uses LevelManager if available
    /// </summary>
    private void ProceedToNextLevel()
    {
        // ✅ Check if LevelManager exists
        if (LevelManager.Instance != null)
        {
            Debug.Log("[LevelCompleteController] Using LevelManager to advance to next level");
            LevelManager.Instance.AdvanceToNextLevel();
        }
        else if (nextLevelData != null)
        {
            // Fallback: Use LevelData system
            Debug.Log($"[LevelCompleteController] Loading {nextLevelData.levelName}...");
            SceneManager.LoadScene(nextLevelData.sceneName);
        }
        else
        {
            // No system available - go to menu
            Debug.LogWarning("[LevelCompleteController] No next level system available, returning to menu");
            LoadMenu();
        }
    }

    private void LoadMenu()
    {
        Debug.Log($"[LevelCompleteController] Loading {levelSelectionSceneName}...");
        SceneManager.LoadScene(levelSelectionSceneName);
    }

    /// <summary>
    /// Public method to check if all items are placed
    /// Can be called from outside if needed
    /// </summary>
    public void CheckLevelCompletion(int placedCount, int totalCount)
    {
        if (placedCount >= totalCount && !isCompleted)
        {
            Debug.Log($"[LevelCompleteController] All items placed ({placedCount}/{totalCount})!");
            TriggerLevelComplete();
        }
    }
}
