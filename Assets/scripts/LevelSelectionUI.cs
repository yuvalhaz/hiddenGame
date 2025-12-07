using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// UI לבחירת Levels - מציג רשימת levels, נועל/פותח לפי התקדמות
/// </summary>
public class LevelSelectionUI : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private LevelData[] allLevels;
    [Tooltip("Array of all levels in order")]

    [Header("UI Prefabs")]
    [SerializeField] private GameObject levelButtonPrefab;
    [Tooltip("Prefab for level button - should have Image, Text, Button")]

    [SerializeField] private Transform levelButtonContainer;
    [Tooltip("Parent transform for level buttons (usually a GridLayoutGroup)")]

    [Header("Lock Settings")]
    [SerializeField] private Sprite lockedIcon;
    [SerializeField] private Sprite unlockedIcon;
    [SerializeField] private Sprite completedIcon;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color completedColor = Color.green;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private List<Button> levelButtons = new List<Button>();

    private void Start()
    {
        GenerateLevelButtons();
    }

    private void GenerateLevelButtons()
    {
        if (allLevels == null || allLevels.Length == 0)
        {
            Debug.LogError("[LevelSelectionUI] No levels assigned!");
            return;
        }

        if (levelButtonPrefab == null || levelButtonContainer == null)
        {
            Debug.LogError("[LevelSelectionUI] Missing prefab or container!");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }
        levelButtons.Clear();

        // Create button for each level
        for (int i = 0; i < allLevels.Length; i++)
        {
            LevelData levelData = allLevels[i];
            if (levelData == null)
            {
                Debug.LogWarning($"[LevelSelectionUI] Level {i} is null!");
                continue;
            }

            CreateLevelButton(levelData, i);
        }

        if (debugMode)
            Debug.Log($"[LevelSelectionUI] Created {levelButtons.Count} level buttons");
    }

    private void CreateLevelButton(LevelData levelData, int index)
    {
        // Instantiate button
        GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
        buttonObj.name = $"LevelButton_{levelData.levelNumber}";

        // Get components
        Button button = buttonObj.GetComponent<Button>();
        Image buttonImage = buttonObj.GetComponent<Image>();
        Text buttonText = buttonObj.GetComponentInChildren<Text>();

        if (button == null)
        {
            Debug.LogError($"[LevelSelectionUI] Button component missing on prefab!");
            return;
        }

        // Setup button appearance
        bool isUnlocked = levelData.IsUnlocked();
        bool isCompleted = levelData.IsCompleted();

        // Set text
        if (buttonText != null)
        {
            if (isCompleted)
            {
                buttonText.text = $"{levelData.levelName}\n✓ Completed";
            }
            else if (isUnlocked)
            {
                buttonText.text = levelData.levelName;
            }
            else
            {
                buttonText.text = $"{levelData.levelName}\n🔒 Locked";
            }
        }

        // Set color/icon
        if (buttonImage != null)
        {
            if (isCompleted && completedIcon != null)
            {
                buttonImage.sprite = completedIcon;
                buttonImage.color = completedColor;
            }
            else if (isUnlocked && unlockedIcon != null)
            {
                buttonImage.sprite = unlockedIcon;
                buttonImage.color = unlockedColor;
            }
            else if (!isUnlocked && lockedIcon != null)
            {
                buttonImage.sprite = lockedIcon;
                buttonImage.color = lockedColor;
            }

            // Use thumbnail if available
            if (levelData.levelThumbnail != null)
            {
                buttonImage.sprite = levelData.levelThumbnail;
            }
        }

        // Setup button click
        button.interactable = isUnlocked;
        button.onClick.AddListener(() => OnLevelButtonClicked(levelData));

        levelButtons.Add(button);

        if (debugMode)
        {
            string status = isCompleted ? "Completed" : (isUnlocked ? "Unlocked" : "Locked");
            Debug.Log($"[LevelSelectionUI] {levelData.levelName} - {status}");
        }
    }

    private void OnLevelButtonClicked(LevelData levelData)
    {
        if (!levelData.IsUnlocked())
        {
            Debug.LogWarning($"[LevelSelectionUI] {levelData.levelName} is locked!");
            return;
        }

        Debug.Log($"[LevelSelectionUI] Loading {levelData.levelName}...");
        LoadLevel(levelData);
    }

    private void LoadLevel(LevelData levelData)
    {
        if (string.IsNullOrEmpty(levelData.sceneName))
        {
            Debug.LogError($"[LevelSelectionUI] {levelData.levelName} has no scene name!");
            return;
        }

        // Optional: Save which level we're loading
        PlayerPrefs.SetInt("CurrentLevel", levelData.levelNumber);
        PlayerPrefs.Save();

        // Load the scene
        SceneManager.LoadScene(levelData.sceneName);
    }

    /// <summary>
    /// Refresh all buttons (call this after completing a level)
    /// </summary>
    [ContextMenu("Refresh Buttons")]
    public void RefreshButtons()
    {
        GenerateLevelButtons();
    }

    /// <summary>
    /// Unlock all levels (for testing)
    /// </summary>
    [ContextMenu("Unlock All Levels")]
    public void UnlockAllLevels()
    {
        foreach (LevelData level in allLevels)
        {
            if (level != null)
            {
                level.Unlock();
            }
        }
        RefreshButtons();
        Debug.Log("[LevelSelectionUI] All levels unlocked!");
    }

    /// <summary>
    /// Reset all progress (for testing)
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        foreach (LevelData level in allLevels)
        {
            if (level != null)
            {
                level.ResetProgress();
            }
        }

        // Make sure level 1 is unlocked
        if (allLevels.Length > 0 && allLevels[0] != null)
        {
            allLevels[0].Unlock();
        }

        RefreshButtons();
        Debug.Log("[LevelSelectionUI] All progress reset!");
    }
}
