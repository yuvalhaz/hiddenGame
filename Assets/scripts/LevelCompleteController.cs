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

    [Header("Tutorial Settings")]
    [SerializeField] private bool isTutorialLevel = false;
    [Tooltip("Set to true for tutorial levels - will not require LevelManager")]

    [Header("🔊 Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip levelCompleteSound;
    [Tooltip("Sound effect when level is completed")]
    [SerializeField] private AudioClip victoryMusic;
    [Tooltip("Background music for completion screen (optional, looped)")]
    [SerializeField] private AudioClip buttonClickSound;
    [Tooltip("Sound when clicking buttons")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.6f;

    private bool isCompleted = false;

    private void Start()
    {
        // Initialize audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }
        }

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
        // Stop audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(OnNextLevelButtonClicked);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(LoadMenu);
    }

    /// <summary>
    /// Play a sound effect
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume);
        }
    }

    /// <summary>
    /// Play victory music (looped)
    /// </summary>
    private void PlayVictoryMusic()
    {
        if (audioSource != null && victoryMusic != null)
        {
            audioSource.clip = victoryMusic;
            audioSource.volume = musicVolume;
            audioSource.loop = true;
            audioSource.Play();
        }
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

        // Play level complete sound
        PlaySound(levelCompleteSound);

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

        // ✅ Disable our own next button so it doesn't also fire when EndingDialog's NEXT is clicked
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(OnNextLevelButtonClicked);
            nextLevelButton.gameObject.SetActive(false);
            Debug.Log("[LevelCompleteController] Disabled nextLevelButton (EndingDialog handles NEXT)");
        }

        // ✅ השלם את הלבל כאן - לפני הבועות!
        if (LevelManager.Instance != null)
        {
            // Schedule push notification reminder for next level
            if (PushNotificationManager.Instance != null)
            {
                PushNotificationManager.Instance.ScheduleLevelCompleteReminder(
                    LevelManager.Instance.GetCurrentLevelNumber());
            }

            Debug.Log("[LevelCompleteController] ✅ Completing level and advancing pointer...");
            LevelManager.Instance.CompleteCurrentLevelAndAdvancePointer();
        }
        else
        {
            // Tutorial levels or standalone levels without LevelManager
            Debug.LogWarning("[LevelCompleteController] ⚠️ LevelManager is NULL (Tutorial or standalone level - this is OK)");
        }

        // Save tutorial completion flag
        if (isTutorialLevel)
        {
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.SetInt("IsFirstTime", 0); // For LoadingManager
            PlayerPrefs.Save();
            Debug.Log("[LevelCompleteController] ✅ Tutorial marked as completed!");
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

        // Save tutorial completion flag
        if (isTutorialLevel)
        {
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.SetInt("IsFirstTime", 0); // For LoadingManager
            PlayerPrefs.Save();
            Debug.Log("[LevelCompleteController] ✅ Tutorial marked as completed!");
        }

        // Show completion screen
        ShowCompletionScreen();

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

        // Play victory music
        PlayVictoryMusic();

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
        PlaySound(buttonClickSound);
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
        PlaySound(buttonClickSound);
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
