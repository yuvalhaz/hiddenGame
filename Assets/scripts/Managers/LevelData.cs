using UnityEngine;

/// <summary>
/// נתוני Level - ScriptableObject שמכיל את כל המידע על level בודד
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "Hidden Game/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    [Tooltip("Level number (1, 2, 3...)")]
    public int levelNumber = 1;

    [Tooltip("Level display name")]
    public string levelName = "Level 1";

    [Tooltip("Scene name to load for this level")]
    public string sceneName = "Level1";

    [Header("Unlock Requirements")]
    [Tooltip("Is this level unlocked by default? (Level 1 should be true)")]
    public bool unlockedByDefault = false;

    [Tooltip("Which level must be completed to unlock this one? (0 = none)")]
    public int requiredPreviousLevel = 0;

    [Header("Visual")]
    [Tooltip("Thumbnail/preview image for level selection")]
    public Sprite levelThumbnail;

    [Tooltip("Background color for level button")]
    public Color buttonColor = Color.white;

    [Header("Gameplay")]
    [Tooltip("Number of items to find in this level")]
    public int totalItems = 7;

    [Tooltip("Optional: specific item IDs for this level")]
    public string[] itemIds;

    /// <summary>
    /// Check if this level is unlocked
    /// </summary>
    public bool IsUnlocked()
    {
        if (unlockedByDefault)
            return true;

        if (requiredPreviousLevel == 0)
            return true;

        // Check if previous level is completed
        string key = $"Level_{requiredPreviousLevel}_Completed";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// Mark this level as unlocked
    /// </summary>
    public void Unlock()
    {
        string key = $"Level_{levelNumber}_Unlocked";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Check if this level is completed
    /// </summary>
    public bool IsCompleted()
    {
        string key = $"Level_{levelNumber}_Completed";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// Mark this level as completed
    /// </summary>
    public void MarkCompleted()
    {
        string key = $"Level_{levelNumber}_Completed";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();

        Debug.Log($"[LevelData] Level {levelNumber} marked as completed!");
    }

    /// <summary>
    /// Get progress key for this level
    /// </summary>
    public string GetProgressKey()
    {
        return $"Level_{levelNumber}_Progress";
    }

    /// <summary>
    /// Reset this level's progress
    /// </summary>
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey($"Level_{levelNumber}_Completed");
        PlayerPrefs.DeleteKey($"Level_{levelNumber}_Unlocked");
        PlayerPrefs.DeleteKey(GetProgressKey());
        PlayerPrefs.Save();
    }
}
