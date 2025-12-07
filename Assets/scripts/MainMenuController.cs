using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// מניו ראשי למשחק - מטפל בניווט בין מסכים
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Scene Names")]
    [SerializeField] private string levelSelectionSceneName = "LevelSelection";
    [SerializeField] private string firstLevelSceneName = "Level1";
    [SerializeField] private string settingsSceneName = "Settings";

    [Header("Settings")]
    [SerializeField] private bool showContinueButtonOnlyIfHasProgress = true;

    private void Start()
    {
        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);

            // Hide continue button if no progress
            if (showContinueButtonOnlyIfHasProgress && !HasGameProgress())
            {
                continueButton.gameObject.SetActive(false);
            }
        }

        if (levelSelectButton != null)
            levelSelectButton.onClick.AddListener(OnLevelSelectClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);

        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);

        if (levelSelectButton != null)
            levelSelectButton.onClick.RemoveListener(OnLevelSelectClicked);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitClicked);
    }

    /// <summary>
    /// התחל משחק חדש - אפס התקדמות וטען level 1
    /// </summary>
    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuController] Starting new game...");

        // Reset progress
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Load first level
        SceneManager.LoadScene(firstLevelSceneName);
    }

    /// <summary>
    /// המשך משחק - טען את ה-level האחרון ששוחק
    /// </summary>
    private void OnContinueClicked()
    {
        Debug.Log("[MainMenuController] Continuing game...");

        // Get last played level
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        string sceneName = $"Level{currentLevel + 1}";

        // Try to load the scene
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            // Fallback to level selection
            Debug.LogWarning($"[MainMenuController] Cannot load scene {sceneName}, going to level selection");
            SceneManager.LoadScene(levelSelectionSceneName);
        }
    }

    /// <summary>
    /// בחירת level - טען את מסך בחירת ה-levels
    /// </summary>
    private void OnLevelSelectClicked()
    {
        Debug.Log("[MainMenuController] Opening level selection...");
        SceneManager.LoadScene(levelSelectionSceneName);
    }

    /// <summary>
    /// הגדרות - טען את מסך ההגדרות
    /// </summary>
    private void OnSettingsClicked()
    {
        Debug.Log("[MainMenuController] Opening settings...");

        if (!string.IsNullOrEmpty(settingsSceneName))
        {
            SceneManager.LoadScene(settingsSceneName);
        }
        else
        {
            Debug.LogWarning("[MainMenuController] Settings scene name not configured!");
        }
    }

    /// <summary>
    /// יציאה מהמשחק
    /// </summary>
    private void OnExitClicked()
    {
        Debug.Log("[MainMenuController] Exiting game...");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// בדוק אם יש התקדמות שמורה במשחק
    /// </summary>
    private bool HasGameProgress()
    {
        // Check if there's any saved data
        if (PlayerPrefs.HasKey("GameProgress"))
            return true;

        // Check if any level is completed
        for (int i = 1; i <= 10; i++)
        {
            string key = $"Level_{i}_Completed";
            if (PlayerPrefs.GetInt(key, 0) == 1)
                return true;
        }

        // Check if current level is beyond 0
        if (PlayerPrefs.GetInt("CurrentLevel", 0) > 0)
            return true;

        return false;
    }

    /// <summary>
    /// Reset all game progress (for testing)
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[MainMenuController] All progress reset!");

        // Refresh continue button visibility
        if (continueButton != null && showContinueButtonOnlyIfHasProgress)
        {
            continueButton.gameObject.SetActive(HasGameProgress());
        }
    }
}
