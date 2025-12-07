using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// מטפל בסיום Level - מציג מסך ניצחון, פותח level הבא, וטוען scene
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
    [SerializeField] private float autoLoadDelay = 3f;
    [Tooltip("Auto-load next level after X seconds (0 = disabled)")]

    [Header("🎬 Ending Dialog")]
    [SerializeField] private EndingDialogController endingDialog;
    [Tooltip("Optional: Play ending dialog before completion screen")]
    [SerializeField] private bool useEndingDialog = false;

    private bool isCompleted = false;

    private void Start()
    {
        // Hide completion panel at start
        if (completionPanel != null)
            completionPanel.SetActive(false);

        // Setup buttons
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(LoadNextLevel);

        if (menuButton != null)
            menuButton.onClick.AddListener(LoadMenu);

        // Listen to completion events
        if (GameProgressManager.Instance != null)
        {
            // You can trigger level complete from outside
        }
    }

    private void OnDestroy()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(LoadNextLevel);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(LoadMenu);
    }

    /// <summary>
    /// Call this when the player completes all items in the level
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

        // Start ending dialog
        endingDialog.StartEndingDialog();

        // Wait for dialog to finish (estimate based on your settings)
        // You might want to add a callback system to EndingDialogController
        float dialogDuration = 10f; // Adjust based on your dialog settings
        yield return new WaitForSeconds(dialogDuration);

        // Now show completion screen
        CompleteLevelImmediate();
    }

    private void CompleteLevelImmediate()
    {
        // Mark level as completed
        if (currentLevelData != null)
        {
            currentLevelData.MarkCompleted();
            Debug.Log($"[LevelCompleteController] ✅ {currentLevelData.levelName} completed!");
        }

        // Unlock next level
        if (nextLevelData != null)
        {
            nextLevelData.Unlock();
            Debug.Log($"[LevelCompleteController] 🔓 {nextLevelData.levelName} unlocked!");
        }

        // Show completion UI
        ShowCompletionScreen();

        // Auto-load if enabled
        if (autoLoadDelay > 0)
        {
            StartCoroutine(AutoLoadAfterDelay());
        }
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
            if (nextLevelData != null)
            {
                nextLevelButton.gameObject.SetActive(true);
                var buttonText = nextLevelButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = $"Next: {nextLevelData.levelName}";
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

        if (nextLevelData != null)
        {
            LoadNextLevel();
        }
        else
        {
            LoadMenu();
        }
    }

    private void LoadNextLevel()
    {
        if (nextLevelData != null)
        {
            Debug.Log($"[LevelCompleteController] Loading {nextLevelData.levelName}...");
            SceneManager.LoadScene(nextLevelData.sceneName);
        }
        else
        {
            Debug.LogWarning("[LevelCompleteController] No next level data assigned!");
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
    /// Call this from GameProgressManager or DropSpotBatchManager
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
