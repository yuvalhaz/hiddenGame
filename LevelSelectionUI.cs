using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// UI לבחירת Levels - מציג רשימת levels, נועל/פותח לפי התקדמות
/// עם עיצוב גרפי יפה, לוגו, ואנימציות
/// Supports both manual button placement and automatic generation
/// NOW WITH COMPREHENSIVE DEBUG TOOLS!
/// </summary>
public class LevelSelectionUI : MonoBehaviour
{
    public enum ButtonMode
    {
        ManualButtons,      // Connect pre-placed buttons from scene
        AutoGenerate        // Generate buttons automatically
    }

    [Header("🎮 Button Mode")]
    [SerializeField] private ButtonMode buttonMode = ButtonMode.ManualButtons;
    [Tooltip("Manual: Connect your own buttons. Auto: Generate buttons automatically")]

    [Header("🎨 Visual Settings")]
    [SerializeField] private Image gameLogo;
    [SerializeField] private Text titleText;
    [SerializeField] private Image backgroundImage;

    [Header("Level Configuration")]
    [SerializeField] private int totalLevels = 10;
    [SerializeField] private string levelScenePrefix = "Level";

    [Header("📌 Manual Buttons (if using Manual Mode)")]
    [SerializeField] private List<Button> manualLevelButtons = new List<Button>();
    [Tooltip("Drag your pre-placed buttons here in order (Level 1, Level 2, etc.)")]

    [Header("🤖 Auto-Generate Settings (if using Auto Mode)")]
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private GameObject levelButtonPrefab;

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
    [SerializeField] private float buttonPopDuration = 0.3f;
    [SerializeField] private AnimationCurve buttonPopCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("🐛 Debug Settings")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool enableDebugOverlay = true;
    [SerializeField] private KeyCode debugToggleKey = KeyCode.F1;
    [SerializeField] private KeyCode quickUnlockAllKey = KeyCode.F2;
    [SerializeField] private KeyCode quickResetKey = KeyCode.F3;
    [SerializeField] private bool showDebugGUI = false;
    [SerializeField] private bool logStateChanges = true;
    [SerializeField] private bool showPerformanceMetrics = false;

    private List<Button> levelButtons = new List<Button>();

    // Debug tracking
    private float lastRefreshTime;
    private int refreshCount;
    private Dictionary<int, LevelDebugInfo> levelDebugInfo = new Dictionary<int, LevelDebugInfo>();

    private class LevelDebugInfo
    {
        public int levelNumber;
        public bool isUnlocked;
        public bool isCompleted;
        public int clickCount;
        public float lastClickTime;
        public string playerPrefsKey;
    }

    private void Start()
    {
        if (titleText != null)
        {
            titleText.text = "בחר שלב";
        }

        if (buttonMode == ButtonMode.ManualButtons)
        {
            SetupManualButtons();
        }
        else
        {
            GenerateLevelButtons();
        }

        if (animateButtonsOnStart)
        {
            StartCoroutine(AnimateButtonsSequence());
        }

        InitializeDebugTools();
    }

    private void Update()
    {
        HandleDebugInput();
    }

    /// <summary>
    /// Setup manually placed buttons from the scene
    /// </summary>
    private void SetupManualButtons()
    {
        levelButtons.Clear();

        if (manualLevelButtons.Count == 0)
        {
            DebugLogError("No manual buttons assigned! Drag your buttons into the Manual Level Buttons list.");
            return;
        }

        // Hide buttons initially for animation
        if (animateButtonsOnStart)
        {
            foreach (Button btn in manualLevelButtons)
            {
                if (btn != null)
                {
                    btn.transform.localScale = Vector3.zero;
                }
            }
        }

        // Setup each button
        for (int i = 0; i < manualLevelButtons.Count && i < totalLevels; i++)
        {
            Button button = manualLevelButtons[i];
            if (button == null)
            {
                DebugLogWarning($"Button {i + 1} is null!");
                continue;
            }

            int levelNumber = i + 1;
            SetupButton(button, levelNumber);
            levelButtons.Add(button);
        }

        DebugLog($"Setup {levelButtons.Count} manual buttons");
    }

    /// <summary>
    /// Generate buttons automatically (old method)
    /// </summary>
    private void GenerateLevelButtons()
    {
        if (levelButtonPrefab == null || levelButtonContainer == null)
        {
            DebugLogError("Missing prefab or container for auto-generation!");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }
        levelButtons.Clear();

        // Create button for each level
        for (int i = 0; i < totalLevels; i++)
        {
            CreateLevelButton(i);
        }

        DebugLog($"Created {levelButtons.Count} level buttons");
    }

    private void CreateLevelButton(int levelIndex)
    {
        int levelNumber = levelIndex + 1;

        GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
        buttonObj.name = $"LevelButton_{levelNumber}";

        if (animateButtonsOnStart)
        {
            buttonObj.transform.localScale = Vector3.zero;
        }

        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            DebugLogError($"Button component missing on prefab!");
            return;
        }

        SetupButton(button, levelNumber);
        levelButtons.Add(button);
    }

    /// <summary>
    /// Setup a button's appearance and click handler
    /// </summary>
    private void SetupButton(Button button, int levelNumber)
    {
        Image buttonImage = button.GetComponent<Image>();
        Text buttonText = button.GetComponentInChildren<Text>();

        bool isUnlocked = IsLevelUnlocked(levelNumber);
        bool isCompleted = IsLevelCompleted(levelNumber);

        // Track debug info
        UpdateLevelDebugInfo(levelNumber, isUnlocked, isCompleted);

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

        // Remove old listeners to prevent duplicates
        button.onClick.RemoveAllListeners();

        int capturedLevelNum = levelNumber;
        button.onClick.AddListener(() => OnLevelButtonClicked(capturedLevelNum));

        string status = isCompleted ? "Completed" : (isUnlocked ? "Unlocked" : "Locked");
        DebugLog($"Level {levelNumber} - {status}");
    }

    private bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber == 1)
            return true;

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
            DebugLogWarning($"Level {levelNumber} is locked!");
            return;
        }

        // Track click in debug info
        if (levelDebugInfo.ContainsKey(levelNumber))
        {
            levelDebugInfo[levelNumber].clickCount++;
            levelDebugInfo[levelNumber].lastClickTime = Time.time;
        }

        DebugLog($"Loading Level {levelNumber}...");
        LoadLevel(levelNumber);
    }

    private void LoadLevel(int levelNumber)
    {
        string sceneName = $"{levelScenePrefix}{levelNumber}";
        PlayerPrefs.SetInt("CurrentLevel", levelNumber - 1);
        PlayerPrefs.Save();

        if (logStateChanges)
        {
            DebugLog($"Saved CurrentLevel = {levelNumber - 1}, Loading scene: {sceneName}");
        }

        SceneManager.LoadScene(sceneName);
    }

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
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        Vector3 targetScale = Vector3.one;

        while (elapsed < buttonPopDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / buttonPopDuration;
            float curveValue = buttonPopCurve.Evaluate(t);

            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            buttonTransform.localScale = targetScale * curveValue * bounce;

            yield return null;
        }

        buttonTransform.localScale = targetScale;
    }

    #region Context Menu Commands

    [ContextMenu("Refresh Buttons")]
    public void RefreshButtons()
    {
        lastRefreshTime = Time.time;
        refreshCount++;

        if (buttonMode == ButtonMode.ManualButtons)
        {
            SetupManualButtons();
        }
        else
        {
            GenerateLevelButtons();
        }

        DebugLog($"Buttons refreshed (count: {refreshCount})");
    }

    [ContextMenu("Unlock All Levels")]
    public void UnlockAllLevels()
    {
        for (int i = 1; i <= totalLevels; i++)
        {
            PlayerPrefs.SetInt($"Level_{i}_Completed", 1);
        }
        PlayerPrefs.Save();
        RefreshButtons();
        DebugLog("All levels unlocked!");
    }

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
        DebugLog("All progress reset!");
    }

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
        DebugLog("All levels locked (except Level 1)!");
    }

    [ContextMenu("🐛 Show Debug Report")]
    public void ShowDebugReport()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("=== LEVEL SELECTION DEBUG REPORT ===");
        report.AppendLine($"Total Levels: {totalLevels}");
        report.AppendLine($"Button Mode: {buttonMode}");
        report.AppendLine($"Buttons Created: {levelButtons.Count}");
        report.AppendLine($"Refresh Count: {refreshCount}");
        report.AppendLine();

        report.AppendLine("Level States:");
        for (int i = 1; i <= totalLevels; i++)
        {
            bool unlocked = IsLevelUnlocked(i);
            bool completed = IsLevelCompleted(i);
            string key = $"Level_{i}_Completed";
            int clicks = levelDebugInfo.ContainsKey(i) ? levelDebugInfo[i].clickCount : 0;

            report.AppendLine($"  Level {i}: {(completed ? "✓ Completed" : (unlocked ? "🔓 Unlocked" : "🔒 Locked"))} | Clicks: {clicks} | Key: {key}");
        }

        report.AppendLine();
        report.AppendLine("PlayerPrefs Data:");
        for (int i = 1; i <= totalLevels; i++)
        {
            string key = $"Level_{i}_Completed";
            int value = PlayerPrefs.GetInt(key, -1);
            if (value != -1)
            {
                report.AppendLine($"  {key} = {value}");
            }
        }

        Debug.Log(report.ToString());
    }

    [ContextMenu("🐛 Unlock Next Level")]
    public void DebugUnlockNext()
    {
        for (int i = 1; i <= totalLevels; i++)
        {
            if (!IsLevelUnlocked(i))
            {
                UnlockLevel(i);
                DebugLog($"Unlocked next level: {i}");
                return;
            }
        }
        DebugLog("All levels already unlocked!");
    }

    [ContextMenu("🐛 Complete Random Level")]
    public void DebugCompleteRandom()
    {
        int randomLevel = Random.Range(1, totalLevels + 1);
        PlayerPrefs.SetInt($"Level_{randomLevel}_Completed", 1);
        PlayerPrefs.Save();
        RefreshButtons();
        DebugLog($"Randomly completed Level {randomLevel}");
    }

    [ContextMenu("🐛 Simulate Progress (50%)")]
    public void DebugSimulateProgress50()
    {
        ResetAllProgress();
        int halfLevels = Mathf.CeilToInt(totalLevels * 0.5f);
        for (int i = 1; i <= halfLevels; i++)
        {
            PlayerPrefs.SetInt($"Level_{i}_Completed", 1);
        }
        PlayerPrefs.Save();
        RefreshButtons();
        DebugLog($"Simulated 50% progress ({halfLevels} levels completed)");
    }

    [ContextMenu("🐛 Toggle Debug GUI")]
    public void ToggleDebugGUI()
    {
        showDebugGUI = !showDebugGUI;
        DebugLog($"Debug GUI: {(showDebugGUI ? "ON" : "OFF")}");
    }

    #endregion

    #region Debug Tools

    private void InitializeDebugTools()
    {
        if (!enableDebugOverlay && !debugMode)
            return;

        // Initialize debug info for all levels
        for (int i = 1; i <= totalLevels; i++)
        {
            UpdateLevelDebugInfo(i, IsLevelUnlocked(i), IsLevelCompleted(i));
        }

        DebugLog("Debug tools initialized");
        DebugLog($"Press {debugToggleKey} to toggle debug GUI");
        DebugLog($"Press {quickUnlockAllKey} to unlock all levels");
        DebugLog($"Press {quickResetKey} to reset progress");
    }

    private void UpdateLevelDebugInfo(int levelNumber, bool isUnlocked, bool isCompleted)
    {
        if (!levelDebugInfo.ContainsKey(levelNumber))
        {
            levelDebugInfo[levelNumber] = new LevelDebugInfo
            {
                levelNumber = levelNumber,
                playerPrefsKey = $"Level_{levelNumber}_Completed"
            };
        }

        var info = levelDebugInfo[levelNumber];

        if (logStateChanges && (info.isUnlocked != isUnlocked || info.isCompleted != isCompleted))
        {
            DebugLog($"Level {levelNumber} state changed - Unlocked: {info.isUnlocked}->{isUnlocked}, Completed: {info.isCompleted}->{isCompleted}");
        }

        info.isUnlocked = isUnlocked;
        info.isCompleted = isCompleted;
    }

    private void HandleDebugInput()
    {
        if (!enableDebugOverlay && !debugMode)
            return;

        if (Input.GetKeyDown(debugToggleKey))
        {
            ToggleDebugGUI();
        }

        if (Input.GetKeyDown(quickUnlockAllKey))
        {
            UnlockAllLevels();
        }

        if (Input.GetKeyDown(quickResetKey))
        {
            ResetAllProgress();
        }

        // Secret combo: Hold Shift + U to unlock all
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.U))
        {
            UnlockAllLevels();
        }

        // Secret combo: Hold Shift + R to reset all
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R))
        {
            ResetAllProgress();
        }

        // Secret combo: Hold Shift + N to unlock next
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.N))
        {
            DebugUnlockNext();
        }
    }

    private void OnGUI()
    {
        if (!showDebugGUI)
            return;

        GUI.skin.box.fontSize = 12;
        GUI.skin.label.fontSize = 11;
        GUI.skin.button.fontSize = 11;

        float panelWidth = 350;
        float panelHeight = Screen.height - 20;
        GUILayout.BeginArea(new Rect(10, 10, panelWidth, panelHeight), GUI.skin.box);

        GUILayout.Label("🐛 LEVEL SELECTION DEBUG PANEL", GUI.skin.box);

        GUILayout.Space(5);
        GUILayout.Label($"Mode: {buttonMode} | Buttons: {levelButtons.Count}/{totalLevels}");
        GUILayout.Label($"Refreshes: {refreshCount} | Last: {(Time.time - lastRefreshTime):F1}s ago");

        if (showPerformanceMetrics)
        {
            GUILayout.Label($"FPS: {(1f / Time.deltaTime):F0}");
            GUILayout.Label($"Memory: {(System.GC.GetTotalMemory(false) / 1024f / 1024f):F1} MB");
        }

        GUILayout.Space(10);

        // Quick Actions
        GUILayout.Label("⚡ Quick Actions:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Unlock All"))
            UnlockAllLevels();
        if (GUILayout.Button("Reset All"))
            ResetAllProgress();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Unlock Next"))
            DebugUnlockNext();
        if (GUILayout.Button("50% Progress"))
            DebugSimulateProgress50();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh"))
            RefreshButtons();
        if (GUILayout.Button("Report"))
            ShowDebugReport();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Level States
        GUILayout.Label("📊 Level States:", GUI.skin.box);

        GUILayout.BeginVertical(GUI.skin.box);
        for (int i = 1; i <= Mathf.Min(totalLevels, 15); i++) // Limit display to prevent overflow
        {
            bool unlocked = IsLevelUnlocked(i);
            bool completed = IsLevelCompleted(i);
            int clicks = levelDebugInfo.ContainsKey(i) ? levelDebugInfo[i].clickCount : 0;

            string status = completed ? "✓" : (unlocked ? "🔓" : "🔒");
            string clickInfo = clicks > 0 ? $" | Clicks: {clicks}" : "";

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{status} Lvl {i}{clickInfo}", GUILayout.Width(150));

            if (GUILayout.Button("✓", GUILayout.Width(30)))
            {
                PlayerPrefs.SetInt($"Level_{i}_Completed", 1);
                PlayerPrefs.Save();
                RefreshButtons();
            }

            if (GUILayout.Button("🔒", GUILayout.Width(30)))
            {
                if (i > 1)
                {
                    PlayerPrefs.DeleteKey($"Level_{i}_Completed");
                    PlayerPrefs.Save();
                    RefreshButtons();
                }
            }

            if (GUILayout.Button("▶", GUILayout.Width(30)))
            {
                LoadLevel(i);
            }

            GUILayout.EndHorizontal();
        }

        if (totalLevels > 15)
        {
            GUILayout.Label($"... and {totalLevels - 15} more levels");
        }

        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Settings
        GUILayout.Label("⚙️ Settings:", GUI.skin.box);
        logStateChanges = GUILayout.Toggle(logStateChanges, "Log State Changes");
        showPerformanceMetrics = GUILayout.Toggle(showPerformanceMetrics, "Show Performance");

        GUILayout.Space(10);
        GUILayout.Label($"Press {debugToggleKey} to close", GUI.skin.label);

        GUILayout.EndArea();
    }

    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[LevelSelectionUI] {message}");
        }
    }

    private void DebugLogWarning(string message)
    {
        if (debugMode)
        {
            Debug.LogWarning($"[LevelSelectionUI] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        Debug.LogError($"[LevelSelectionUI] {message}");
    }

    #endregion

    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber <= 1)
        {
            DebugLog($"Level {levelNumber} is always unlocked");
            return;
        }

        PlayerPrefs.SetInt($"Level_{levelNumber - 1}_Completed", 1);
        PlayerPrefs.Save();
        RefreshButtons();
        DebugLog($"Level {levelNumber} unlocked!");
    }
}
