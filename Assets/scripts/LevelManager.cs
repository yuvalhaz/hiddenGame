using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [Tooltip("Configure levels directly in code - each level should have exactly 7 items")]
    [SerializeField] private bool showCurrentLevelInfo = true;
    
    [Header("References")]
    [SerializeField] private GameProgressManager progressManager;
    [SerializeField] private RewardedAdsManager adsManager;
    [SerializeField] private GameEndingDialogSystem endingDialog;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private int currentLevelIndex = 0;
    
    public static LevelManager Instance { get; private set; }

    // Events
    public System.Action<int> OnLevelChanged;
    public System.Action<int> OnLevelCompleted;

    // ===== LEVEL CONFIGURATION IN CODE =====
    // Define your levels here - each level should have exactly 7 itemIds
    private Dictionary<int, List<string>> levelConfig = new Dictionary<int, List<string>>()
    {
        // Level 0 (first level)
        { 0, new List<string> { "spot00", "spot01", "spot02", "spot03", "spot04", "spot05", "spot06" } },
        
        // Level 1
        { 1, new List<string> { "item8", "item9", "item10", "item11", "item12", "item13", "item14" } },
        
        // Level 2  
        { 2, new List<string> { "item15", "item16", "item17", "item18", "item19", "item20", "item21" } },
        
        // Level 3
        { 3, new List<string> { "item22", "item23", "item24", "item25", "item26", "item27", "item28" } },
        
        // Add more levels as needed...
        // { 4, new List<string> { "item29", "item30", "item31", "item32", "item33", "item34", "item35" } },
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Find references if not assigned
        if (!progressManager) progressManager = FindObjectOfType<GameProgressManager>();
        if (!adsManager) adsManager = FindObjectOfType<RewardedAdsManager>();
        if (!endingDialog) endingDialog = FindObjectOfType<GameEndingDialogSystem>();
        
        ValidateLevels();
    }

    private void Start()
    {
        LoadCurrentLevel();
        RefreshAvailableItems();
    }

    private void ValidateLevels()
    {
        foreach (var level in levelConfig)
        {
            if (level.Value.Count != 7)
            {
                Debug.LogWarning($"[LevelManager] Level {level.Key} doesn't have exactly 7 items! Has {level.Value.Count}");
            }
        }
        
        if (debugMode)
            Debug.Log($"[LevelManager] Configured {levelConfig.Count} levels");
    }

    private void LoadCurrentLevel()
    {
        currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel", 0);
        
        // Make sure we don't go beyond available levels
        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levelConfig.Count - 1);
        
        if (debugMode)
            Debug.Log($"[LevelManager] Loaded level: {currentLevelIndex}");
    }

    private void SaveCurrentLevel()
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevelIndex);
        PlayerPrefs.Save();
    }

    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }

    public int GetTotalLevels()
    {
        return levelConfig.Count;
    }

    public bool IsItemAllowedInCurrentLevel(string itemId)
    {
        if (!levelConfig.ContainsKey(currentLevelIndex))
            return true; // Fallback: allow all items
        
        return levelConfig[currentLevelIndex].Contains(itemId);
    }

    public List<string> GetCurrentLevelItemIds()
    {
        if (levelConfig.ContainsKey(currentLevelIndex))
            return new List<string>(levelConfig[currentLevelIndex]);
        
        return new List<string>();
    }

    /// <summary>
    /// Called when an item is successfully placed. Checks if level is complete.
    /// </summary>
    public void OnItemPlaced(string itemId)
    {
        if (!IsItemAllowedInCurrentLevel(itemId))
        {
            Debug.LogWarning($"[LevelManager] Item {itemId} was placed but doesn't belong to current level!");
            return;
        }

        // Check if current level is complete
        if (IsCurrentLevelComplete())
        {
            CompleteCurrentLevel();
        }
    }

    private bool IsCurrentLevelComplete()
    {
        if (!levelConfig.ContainsKey(currentLevelIndex) || progressManager == null)
            return false;

        var currentLevelItems = levelConfig[currentLevelIndex];
        foreach (string itemId in currentLevelItems)
        {
            if (!progressManager.IsItemPlaced(itemId))
                return false;
        }
        
        return true;
    }

    private void CompleteCurrentLevel()
    {
        if (debugMode)
            Debug.Log($"[LevelManager] Level {currentLevelIndex} completed!");

        // Fire event
        OnLevelCompleted?.Invoke(currentLevelIndex);

        // Show ad if ads manager is available
        if (adsManager != null && adsManager.IsReady())
        {
            adsManager.ShowRewarded(
                onReward: () =>
                {
                    if (debugMode) Debug.Log("[LevelManager] Ad reward received");
                },
                onClosed: (completed) =>
                {  // FIX: Accept the bool parameter
                    AdvanceToNextLevel();
                },
                onFailed: (error) =>
                {
                    Debug.LogWarning($"[LevelManager] Ad failed: {error}");
                    AdvanceToNextLevel(); // Continue anyway
                }
            );
        }
        else
        {
            // No ads or ads not ready - advance immediately
            AdvanceToNextLevel();
        }
    }


    private void AdvanceToNextLevel()
    {
        if (currentLevelIndex < levelConfig.Count - 1)
        {
            currentLevelIndex++;
            SaveCurrentLevel();
            
            if (debugMode)
                Debug.Log($"[LevelManager] Advanced to level {currentLevelIndex}");
            
            OnLevelChanged?.Invoke(currentLevelIndex);
            RefreshAvailableItems();
        }
        else
        {
            if (debugMode)
                Debug.Log("[LevelManager] All levels completed!");

            // Show game ending dialog
            if (endingDialog != null)
            {
                Debug.Log("[LevelManager] 🎉 Showing game ending dialog!");
                endingDialog.StartEndingDialog();
            }
            else
            {
                Debug.LogWarning("[LevelManager] ⚠️ No ending dialog assigned!");
            }
        }
    }

    /// <summary>
    /// Refreshes which items are visible/interactable based on current level
    /// </summary>
    /// <summary>
    /// Refreshes which items are visible/interactable based on current level
    /// </summary>
    private void RefreshAvailableItems()
    {
        var currentLevelItems = GetCurrentLevelItemIds();

        // ✅ CHANGED: Find DraggableButton instead of SimpleDragFromBar
        var allDragButtons = FindObjectsOfType<DraggableButton>();

        foreach (var dragButton in allDragButtons)
        {
            string buttonID = dragButton.GetButtonID();
            bool shouldBeVisible = currentLevelItems.Contains(buttonID);

            // Only show items that belong to current level AND haven't been placed yet
            if (progressManager != null && progressManager.IsItemPlaced(buttonID))
            {
                shouldBeVisible = false; // Item already placed, don't show in bottom bar
            }

            // Hide/show the button
            dragButton.gameObject.SetActive(shouldBeVisible);

            if (debugMode && shouldBeVisible)
                Debug.Log($"[LevelManager] Made item {buttonID} available for level {currentLevelIndex}");
        }
    }


    /// <summary>
    /// Reset current level progress (for testing)
    /// </summary>
    [ContextMenu("Reset Current Level")]
    public void ResetCurrentLevel()
    {
        if (progressManager != null && levelConfig.ContainsKey(currentLevelIndex))
        {
            var currentLevelItems = levelConfig[currentLevelIndex];
            foreach (string itemId in currentLevelItems)
            {
                progressManager.RemoveItemPlacement(itemId);
            }
        }
        
        RefreshAvailableItems();
    }

    /// <summary>
    /// Reset all progress and go back to level 0 (for testing)
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        currentLevelIndex = 0;
        SaveCurrentLevel();
        
        if (progressManager != null)
            progressManager.ResetAllProgress();
            
        RefreshAvailableItems();
        OnLevelChanged?.Invoke(currentLevelIndex);
    }

    /// <summary>
    /// Skip to next level (for testing)
    /// </summary>
    [ContextMenu("Skip Level")]
    public void SkipLevel()
    {
        if (currentLevelIndex < levelConfig.Count - 1)
        {
            AdvanceToNextLevel();
        }
    }

    // Public getters for UI
    public string GetCurrentLevelName()
    {
        return $"Level {currentLevelIndex + 1}";
    }

    public string GetLevelProgress()
    {
        if (progressManager == null || !levelConfig.ContainsKey(currentLevelIndex))
            return "0/7";
            
        var currentLevelItems = levelConfig[currentLevelIndex];
        int placedCount = 0;
        
        foreach (string itemId in currentLevelItems)
        {
            if (progressManager.IsItemPlaced(itemId))
                placedCount++;
        }
        
        return $"{placedCount}/7";
    }

    // Method to add new levels in code easily
    public void AddLevel(int levelIndex, List<string> itemIds)
    {
        if (itemIds.Count != 7)
        {
            Debug.LogWarning($"[LevelManager] Cannot add level {levelIndex} - must have exactly 7 items!");
            return;
        }
        
        levelConfig[levelIndex] = new List<string>(itemIds);
        
        if (debugMode)
            Debug.Log($"[LevelManager] Added level {levelIndex} with items: {string.Join(", ", itemIds)}");
    }

    private void OnValidate()
    {
        if (showCurrentLevelInfo && Application.isPlaying)
        {
            // Show current level info in inspector during play
            name = $"LevelManager - Level {currentLevelIndex + 1} ({GetLevelProgress()})";
        }
    }
}