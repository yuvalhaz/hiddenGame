using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI לבחירת Levels - מציג רשימת levels, נועל/פותח לפי התקדמות
/// עם עיצוב גרפי יפה, לוגו, ואנימציות
/// Supports custom positioning for buttons on map backgrounds
/// </summary>
public class LevelSelectionUI : MonoBehaviour
{
    [System.Serializable]
    public class LevelButtonPosition
    {
        public int levelNumber = 1;
        public Vector2 position = Vector2.zero;
        [Tooltip("Position on the map (anchored position)")]
    }

    [Header("🎨 Visual Settings")]
    [SerializeField] private Image gameLogo;
    [Tooltip("לוגו המשחק בראש המסך")]

    [SerializeField] private Text titleText;
    [Tooltip("כותרת המסך (לדוגמה: 'בחר שלב')")]

    [SerializeField] private Image backgroundImage;
    [Tooltip("תמונת רקע למסך")]

    [Header("Level Configuration")]
    [SerializeField] private int totalLevels = 10;
    [Tooltip("מספר ה-levels במשחק")]

    [SerializeField] private string levelScenePrefix = "Level";
    [Tooltip("קידומת שם ה-scene (Level1, Level2, וכו')")]

    [Header("UI References")]
    [SerializeField] private Transform levelButtonContainer;
    [Tooltip("Parent transform for level buttons - should be a RectTransform for custom positioning")]

    [SerializeField] private GameObject levelButtonPrefab;
    [Tooltip("Prefab for level button - should have Image, Text, Button")]

    [Header("🗺️ Custom Button Positions")]
    [SerializeField] private bool useCustomPositions = false;
    [Tooltip("Enable to position buttons manually on the map. Disable to use automatic layout.")]

    [SerializeField] private List<LevelButtonPosition> customButtonPositions = new List<LevelButtonPosition>();
    [Tooltip("Define custom X,Y positions for each level button on your map")]

    [Header("🎨 Button Styling")]
    [SerializeField] private Sprite lockedIcon;
    [SerializeField] private Sprite unlockedIcon;
    [SerializeField] private Sprite completedIcon;
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color unlockedColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color completedColor = new Color(0.3f, 1f, 0.3f, 1f);

    [Header("✨ Animation Settings")]
    [SerializeField] private bool animateButtonsOnStart = true;
    [SerializeField] private float buttonAnimationDelay = 0.05f;
    [Tooltip("עיכוב בין כל כפתור באנימציה")]

    [SerializeField] private float buttonPopDuration = 0.3f;
    [SerializeField] private AnimationCurve buttonPopCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private List<Button> levelButtons = new List<Button>();

    private void Start()
    {
        // Set title if available
        if (titleText != null)
        {
            titleText.text = "בחר שלב";
        }

        GenerateLevelButtons();

        // Animate buttons on start
        if (animateButtonsOnStart)
        {
            StartCoroutine(AnimateButtonsSequence());
        }
    }

    // OnGUI removed - use your own UI buttons and call the public methods:
    // RefreshButtons(), UnlockAllLevels(), LockAllLevels(), ResetAllProgress(), UnlockLevel(int)
    // Just create buttons in Unity and assign these methods to OnClick events!

    private void GenerateLevelButtons()
    {
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

        // Create button for each level (1-10)
        for (int i = 0; i < totalLevels; i++)
        {
            CreateLevelButton(i);
        }

        if (debugMode)
            Debug.Log($"[LevelSelectionUI] Created {levelButtons.Count} level buttons");
    }

    private void CreateLevelButton(int levelIndex)
    {
        int levelNumber = levelIndex + 1; // 1-10

        // Instantiate button
        GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
        buttonObj.name = $"LevelButton_{levelNumber}";

        // Apply custom position if enabled
        if (useCustomPositions)
        {
            ApplyCustomPosition(buttonObj, levelNumber);
        }

        // Hide initially for animation
        if (animateButtonsOnStart)
        {
            buttonObj.transform.localScale = Vector3.zero;
        }

        // Get components
        Button button = buttonObj.GetComponent<Button>();
        Image buttonImage = buttonObj.GetComponent<Image>();
        Text buttonText = buttonObj.GetComponentInChildren<Text>();

        if (button == null)
        {
            Debug.LogError($"[LevelSelectionUI] Button component missing on prefab!");
            return;
        }

        // Check unlock status
        bool isUnlocked = IsLevelUnlocked(levelNumber);
        bool isCompleted = IsLevelCompleted(levelNumber);

        // Set text
        if (buttonText != null)
        {
            if (isCompleted)
            {
                buttonText.text = $"{levelNumber}\n✓";
            }
            else if (isUnlocked)
            {
                buttonText.text = $"{levelNumber}";
            }
            else
            {
                buttonText.text = $"🔒";
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
        }

        // Setup button click
        button.interactable = isUnlocked;
        int capturedLevelNum = levelNumber;
        button.onClick.AddListener(() => OnLevelButtonClicked(capturedLevelNum));

        levelButtons.Add(button);

        if (debugMode)
        {
            string status = isCompleted ? "Completed" : (isUnlocked ? "Unlocked" : "Locked");
            Debug.Log($"[LevelSelectionUI] Level {levelNumber} - {status}");
        }
    }

    private void ApplyCustomPosition(GameObject buttonObj, int levelNumber)
    {
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning($"[LevelSelectionUI] Button {levelNumber} doesn't have RectTransform!");
            return;
        }

        // Find position for this level
        LevelButtonPosition positionData = customButtonPositions.Find(p => p.levelNumber == levelNumber);

        if (positionData != null)
        {
            // Set anchors to center for consistent positioning
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Apply custom position
            rectTransform.anchoredPosition = positionData.position;

            if (debugMode)
                Debug.Log($"[LevelSelectionUI] Level {levelNumber} positioned at {positionData.position}");
        }
        else
        {
            Debug.LogWarning($"[LevelSelectionUI] No custom position defined for Level {levelNumber}!");
        }
    }

    private bool IsLevelUnlocked(int levelNumber)
    {
        // Level 1 is always unlocked
        if (levelNumber == 1)
            return true;

        // Check if previous level is completed
        return IsLevelCompleted(levelNumber - 1);
    }

    private bool IsLevelCompleted(int levelNumber)
    {
        string key = $"Level_{levelNumber}_Completed";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private void OnLevelButtonClicked(int levelNumber)
    {
        if (!IsLevelUnlocked(levelNumber))
        {
            Debug.LogWarning($"[LevelSelectionUI] Level {levelNumber} is locked!");
            return;
        }

        Debug.Log($"[LevelSelectionUI] Loading Level {levelNumber}...");
        LoadLevel(levelNumber);
    }

    private void LoadLevel(int levelNumber)
    {
        string sceneName = $"{levelScenePrefix}{levelNumber}";

        // Save which level we're loading
        PlayerPrefs.SetInt("CurrentLevel", levelNumber - 1); // 0-based for LevelManager
        PlayerPrefs.Save();

        // Load the scene
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// אנימציה של הכפתורים - pop-in אחד אחרי השני
    /// </summary>
    private IEnumerator AnimateButtonsSequence()
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            if (levelButtons[i] != null)
            {
                StartCoroutine(AnimateButtonPopIn(levelButtons[i].transform, i * buttonAnimationDelay));
            }
        }

        yield return null;
    }

    private IEnumerator AnimateButtonPopIn(Transform buttonTransform, float delay)
    {
        // Wait for delay
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        Vector3 targetScale = Vector3.one;

        while (elapsed < buttonPopDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / buttonPopDuration;
            float curveValue = buttonPopCurve.Evaluate(t);

            // Add bounce effect
            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            buttonTransform.localScale = targetScale * curveValue * bounce;

            yield return null;
        }

        buttonTransform.localScale = targetScale;
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
        for (int i = 1; i <= totalLevels; i++)
        {
            PlayerPrefs.SetInt($"Level_{i}_Completed", 1);
        }
        PlayerPrefs.Save();
        RefreshButtons();
        Debug.Log("[LevelSelectionUI] All levels unlocked!");
    }

    /// <summary>
    /// Reset all progress (for testing)
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        for (int i = 1; i <= totalLevels; i++)
        {
            PlayerPrefs.DeleteKey($"Level_{i}_Completed");
            PlayerPrefs.DeleteKey($"Level_{i}_Unlocked");
        }
        PlayerPrefs.Save();
        RefreshButtons();
        Debug.Log("[LevelSelectionUI] All progress reset!");
    }

    /// <summary>
    /// Lock all levels except level 1 (for testing)
    /// </summary>
    [ContextMenu("Lock All Levels")]
    public void LockAllLevels()
    {
        for (int i = 2; i <= totalLevels; i++)
        {
            PlayerPrefs.DeleteKey($"Level_{i}_Completed");
            PlayerPrefs.DeleteKey($"Level_{i}_Unlocked");
        }
        PlayerPrefs.Save();
        RefreshButtons();
        Debug.Log("[LevelSelectionUI] All levels locked (except Level 1)!");
    }

    /// <summary>
    /// Unlock specific level (for testing)
    /// </summary>
    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber <= 1)
        {
            Debug.Log($"[LevelSelectionUI] Level {levelNumber} is always unlocked");
            return;
        }

        // Mark previous level as completed to unlock this one
        PlayerPrefs.SetInt($"Level_{levelNumber - 1}_Completed", 1);
        PlayerPrefs.Save();
        RefreshButtons();
        Debug.Log($"[LevelSelectionUI] Level {levelNumber} unlocked!");
    }

    /// <summary>
    /// Initialize custom position list with default values (for setup)
    /// </summary>
    [ContextMenu("Initialize Position List")]
    private void InitializePositionList()
    {
        customButtonPositions.Clear();
        for (int i = 1; i <= totalLevels; i++)
        {
            customButtonPositions.Add(new LevelButtonPosition
            {
                levelNumber = i,
                position = Vector2.zero // You'll need to set these manually
            });
        }
        Debug.Log($"[LevelSelectionUI] Initialized {totalLevels} position entries. Set custom positions in inspector!");
    }
}
