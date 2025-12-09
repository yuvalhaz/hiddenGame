using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI לבחירת Levels - מציג רשימת levels, נועל/פותח לפי התקדמות
/// עם עיצוב גרפי יפה, לוגו, ואנימציות
/// </summary>
public class LevelSelectionUI : MonoBehaviour
{
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
    [Tooltip("Parent transform for level buttons (usually a GridLayoutGroup)")]

    [SerializeField] private GameObject levelButtonPrefab;
    [Tooltip("Prefab for level button - should have Image, Text, Button")]

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
    [SerializeField] private bool showDebugGUI = true;
    [SerializeField] private float guiScale = 2.5f;
    [SerializeField] private Vector2 guiPosition = new Vector2(10, 10);

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

    private void OnGUI()
    {
        if (!showDebugGUI) return;

        // Scale GUI to make it bigger and more readable
        GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));

        GUILayout.BeginArea(new Rect(guiPosition.x, guiPosition.y, 400, 600));
        GUILayout.Box("🎮 LEVEL SELECTION DEBUG");

        GUILayout.Space(10);

        // === LEVEL STATUS ===
        GUILayout.Label("=== LEVEL STATUS ===");
        for (int i = 1; i <= Mathf.Min(totalLevels, 5); i++)
        {
            bool completed = IsLevelCompleted(i);
            bool unlocked = IsLevelUnlocked(i);
            string status = completed ? "✅ Completed" : (unlocked ? "🔓 Unlocked" : "🔒 Locked");
            GUILayout.Label($"Level {i}: {status}");
        }
        if (totalLevels > 5)
        {
            GUILayout.Label($"... ({totalLevels} total levels)");
        }

        GUILayout.Space(10);

        // === ACTIONS ===
        GUILayout.Label("=== ACTIONS ===");

        if (GUILayout.Button("🔄 Refresh Buttons"))
        {
            RefreshButtons();
        }

        if (GUILayout.Button("🔓 Unlock All Levels"))
        {
            UnlockAllLevels();
        }

        if (GUILayout.Button("🔒 Lock All Levels"))
        {
            LockAllLevels();
        }

        if (GUILayout.Button("🗑️ Reset All Progress"))
        {
            ResetAllProgress();
        }

        GUILayout.Space(10);

        // === SPECIFIC LEVEL UNLOCK ===
        GUILayout.Label("=== QUICK UNLOCK ===");

        if (GUILayout.Button("Unlock Level 1"))
        {
            UnlockLevel(1);
        }

        if (GUILayout.Button("Unlock Levels 1-3"))
        {
            for (int i = 1; i <= 3; i++) UnlockLevel(i);
        }

        if (GUILayout.Button("Unlock Levels 1-5"))
        {
            for (int i = 1; i <= 5; i++) UnlockLevel(i);
        }

        GUILayout.Space(10);

        // === INFO ===
        GUILayout.Label("=== INFO ===");
        GUILayout.Label($"Total Levels: {totalLevels}");
        GUILayout.Label($"Buttons Created: {levelButtons.Count}");

        GUILayout.EndArea();
    }

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
}
