using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages level progression, completion, and scene loading
/// ✅ Works with LevelSelectionUI and GameProgressManager
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string levelSelectionScene = "LevelSelection";
    [SerializeField] private string levelScenePrefix = "Level";
    [Tooltip("Level scenes should be named: Level1, Level2, Level3, etc.")]

    [Header("Level Settings")]
    [SerializeField] private int totalLevels = 10;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    public static LevelManager Instance { get; private set; }

    private int currentLevelNumber = 0; // 0-indexed internally

    private void Awake()
    {
        Debug.Log("🔵🔵🔵 [LevelManager] Awake called! 🔵🔵🔵");
        Debug.Log($"[LevelManager] GameObject name: {gameObject.name}");
        Debug.Log($"[LevelManager] Current scene: {SceneManager.GetActiveScene().name}");
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅✅✅ [LevelManager] Instance created and set to DontDestroyOnLoad! ✅✅✅");
            Debug.Log($"[LevelManager] Instance reference: {Instance}");
            LoadCurrentLevelFromPrefs();
        }
        else
        {
            Debug.LogWarning("❌❌❌ [LevelManager] Duplicate found! Destroying this instance! ❌❌❌");
            Debug.LogWarning($"[LevelManager] Existing Instance: {Instance}");
            Debug.LogWarning($"[LevelManager] This GameObject: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Load which level we're currently on from PlayerPrefs
    /// </summary>
    private void LoadCurrentLevelFromPrefs()
    {
        currentLevelNumber = PlayerPrefs.GetInt("CurrentLevel", 0);
        if (debugMode)
        {
            Debug.Log($"[LevelManager] Current level loaded: {currentLevelNumber} (Level {currentLevelNumber + 1})");
        }
    }

    /// <summary>
    /// Get the current level number (1-indexed for display)
    /// </summary>
    public int GetCurrentLevelNumber()
    {
        return currentLevelNumber + 1; // Convert to 1-indexed
    }

    /// <summary>
    /// ✅ Call this when a level is completed!
    /// This marks the level as done and unlocks the next one
    /// </summary>
    public void MarkCurrentLevelComplete()
    {
        int levelNumber = currentLevelNumber + 1; // Convert to 1-indexed
        
        Debug.Log($"[LevelManager] ⭐ MarkCurrentLevelComplete called!");
        Debug.Log($"[LevelManager] Current level index: {currentLevelNumber}");
        Debug.Log($"[LevelManager] Marking Level {levelNumber} as complete");
        
        // Mark as complete using LevelSelectionUI's system
        LevelSelectionUI.MarkLevelComplete(levelNumber);
        
        // Verify it was saved
        string key = $"Level_{levelNumber}_Completed";
        int savedValue = PlayerPrefs.GetInt(key, -1);
        Debug.Log($"[LevelManager] Verification - {key} = {savedValue}");
        
        if (debugMode)
        {
            Debug.Log($"[LevelManager] ✅ Level {levelNumber} marked as complete!");
        }
    }

    /// <summary>
    /// ✅ Complete current level and prepare next (without loading scene)
    /// Use this when you want to return to level selection instead of auto-loading next level
    /// </summary>
    public void CompleteCurrentLevelAndAdvancePointer()
    {
        Debug.Log($"[LevelManager] 🎯 CompleteCurrentLevelAndAdvancePointer called!");
        Debug.Log($"[LevelManager] Current level BEFORE: {currentLevelNumber} (Level {currentLevelNumber + 1})");
        
        // Mark current level as complete first
        MarkCurrentLevelComplete();

        // Move to next level pointer
        currentLevelNumber++;
        Debug.Log($"[LevelManager] Current level AFTER increment: {currentLevelNumber} (Level {currentLevelNumber + 1})");

        // Check if we've completed all levels
        if (currentLevelNumber >= totalLevels)
        {
            Debug.Log($"[LevelManager] 🎉 All levels completed!");
            currentLevelNumber = 0; // Reset to first level
        }
        
        // Save new level pointer
        PlayerPrefs.SetInt("CurrentLevel", currentLevelNumber);
        PlayerPrefs.Save();
        Debug.Log($"[LevelManager] ✅ Saved CurrentLevel = {currentLevelNumber} (ready for Level {currentLevelNumber + 1})");
    }

    /// <summary>
    /// ✅ Advance to the next level (called by EndingDialogSystem)
    /// </summary>
    public void AdvanceToNextLevel()
    {
        Debug.Log($"[LevelManager] 🚀 AdvanceToNextLevel called!");
        Debug.Log($"[LevelManager] Current level BEFORE: {currentLevelNumber} (Level {currentLevelNumber + 1})");
        
        // Mark current level as complete first
        MarkCurrentLevelComplete();

        // Move to next level
        currentLevelNumber++;
        Debug.Log($"[LevelManager] Current level AFTER increment: {currentLevelNumber} (Level {currentLevelNumber + 1})");

        // Check if we've completed all levels
        if (currentLevelNumber >= totalLevels)
        {
            Debug.Log($"[LevelManager] 🎉 All levels completed! Returning to level selection.");
            
            // Reset to first level and return to selection
            currentLevelNumber = 0;
            PlayerPrefs.SetInt("CurrentLevel", currentLevelNumber);
            PlayerPrefs.Save();
            
            LoadLevelSelection();
        }
        else
        {
            // Save new level and load it
            PlayerPrefs.SetInt("CurrentLevel", currentLevelNumber);
            PlayerPrefs.Save();

            Debug.Log($"[LevelManager] Saved CurrentLevel = {currentLevelNumber}");
            Debug.Log($"[LevelManager] Advancing to Level {currentLevelNumber + 1}");

            LoadCurrentLevel();
        }
    }

    /// <summary>
    /// Load the current level scene
    /// </summary>
    public void LoadCurrentLevel()
    {
        string sceneName = $"{levelScenePrefix}{currentLevelNumber + 1}";
        
        if (debugMode)
        {
            Debug.Log($"[LevelManager] Loading scene: {sceneName}");
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Load a specific level by number (1-indexed)
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        currentLevelNumber = levelNumber - 1; // Convert to 0-indexed
        PlayerPrefs.SetInt("CurrentLevel", currentLevelNumber);
        PlayerPrefs.Save();

        LoadCurrentLevel();
    }

    /// <summary>
    /// Return to level selection screen
    /// </summary>
    public void LoadLevelSelection()
    {
        if (debugMode)
        {
            Debug.Log($"[LevelManager] Loading level selection: {levelSelectionScene}");
        }

        SceneManager.LoadScene(levelSelectionScene);
    }

    /// <summary>
    /// Restart the current level
    /// </summary>
    public void RestartCurrentLevel()
    {
        if (debugMode)
        {
            Debug.Log($"[LevelManager] Restarting Level {currentLevelNumber + 1}");
        }

        // Clear the current level's progress
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.ResetCurrentLevelOnly();
        }

        LoadCurrentLevel();
    }

    /// <summary>
    /// Check if a level is completed
    /// </summary>
    public bool IsLevelCompleted(int levelNumber)
    {
        string key = $"Level_{levelNumber}_Completed";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// Get total number of completed levels
    /// </summary>
    public int GetCompletedLevelsCount()
    {
        int count = 0;
        for (int i = 1; i <= totalLevels; i++)
        {
            if (IsLevelCompleted(i))
            {
                count++;
            }
        }
        return count;
    }

    #region Debug Methods

    [ContextMenu("🎮 Complete Current Level")]
    public void DebugCompleteCurrentLevel()
    {
        MarkCurrentLevelComplete();
        Debug.Log($"[LevelManager] DEBUG: Completed Level {currentLevelNumber + 1}");
    }

    [ContextMenu("➡️ Advance to Next Level")]
    public void DebugAdvanceToNext()
    {
        AdvanceToNextLevel();
    }

    [ContextMenu("🔄 Restart Current Level")]
    public void DebugRestartLevel()
    {
        RestartCurrentLevel();
    }

    [ContextMenu("🏠 Go to Level Selection")]
    public void DebugGoToLevelSelection()
    {
        LoadLevelSelection();
    }

    [ContextMenu("📊 Show Progress")]
    public void DebugShowProgress()
    {
        Debug.Log("=== LEVEL PROGRESS ===");
        Debug.Log($"Current Level: {currentLevelNumber + 1}");
        Debug.Log($"Completed Levels: {GetCompletedLevelsCount()}/{totalLevels}");
        
        for (int i = 1; i <= totalLevels; i++)
        {
            bool completed = IsLevelCompleted(i);
            Debug.Log($"  Level {i}: {(completed ? "✓ Complete" : "○ Not Complete")}");
        }
    }

    #endregion

    #region GameDebugTools Compatibility

    /// <summary>
    /// Get current level name (for GameDebugTools)
    /// </summary>
    public string GetCurrentLevelName()
    {
        return $"{levelScenePrefix}{currentLevelNumber + 1}";
    }

    /// <summary>
    /// Get level progress percentage (for GameDebugTools)
    /// </summary>
    public float GetLevelProgress()
    {
        if (GameProgressManager.Instance != null)
        {
            var progressData = GameProgressManager.Instance.GetProgressData();
            if (progressData != null)
            {
                // This would need to know total items in level
                // For now return simple completed count
                return progressData.placedItems.Count;
            }
        }
        return 0f;
    }

    /// <summary>
    /// Reset current level (for GameDebugTools)
    /// </summary>
    public void ResetCurrentLevel()
    {
        RestartCurrentLevel();
    }

    /// <summary>
    /// Reset all progress (for GameDebugTools)
    /// </summary>
    public void ResetAllProgress()
    {
        // Reset level completion
        for (int i = 1; i <= totalLevels; i++)
        {
            PlayerPrefs.DeleteKey($"Level_{i}_Completed");
        }
        
        // Reset current level to 0
        currentLevelNumber = 0;
        PlayerPrefs.SetInt("CurrentLevel", 0);
        PlayerPrefs.Save();

        // Reset GameProgressManager if available
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.ResetAllProgress();
        }

        if (debugMode)
        {
            Debug.Log("[LevelManager] All progress reset!");
        }
    }

    /// <summary>
    /// Skip to next level (for GameDebugTools)
    /// </summary>
    public void SkipLevel()
    {
        if (debugMode)
        {
            Debug.Log($"[LevelManager] Skipping Level {currentLevelNumber + 1}");
        }
        
        AdvanceToNextLevel();
    }

    /// <summary>
    /// Get total number of levels (for GameDebugTools)
    /// </summary>
    public int GetTotalLevels()
    {
        return totalLevels;
    }

    #endregion
}
