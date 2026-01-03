using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI לבחירת Levels - מציג רשימת levels, נועל/פותח לפי התקדמות
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
    [SerializeField] private float buttonAnimationDelay = 0.35f;
    [SerializeField] private float buttonPopDuration = 0.3f;
    [SerializeField] private AnimationCurve buttonPopCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("🔊 Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip backgroundMusic;
    [Tooltip("Background music for level selection screen (looped)")]
    [SerializeField] private AudioClip buttonPopSound;
    [Tooltip("Sound when button pops in during animation")]
    [SerializeField] private AudioClip buttonClickSound;
    [Tooltip("Sound when level button is clicked")]
    [SerializeField] private AudioClip lockedButtonSound;
    [Tooltip("Sound when trying to click a locked button")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.6f;

    private List<Button> levelButtons = new List<Button>();

    private void Awake()
    {
        // Initialize audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = true;
            }
        }
    }

    private void Start()
    {
        // Play background music
        if (audioSource != null && backgroundMusic != null)
        {
            audioSource.volume = musicVolume;
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

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
    }

    private void OnDestroy()
    {
        // Stop music when leaving scene
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Play a sound effect
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    /// <summary>
    /// Setup manually placed buttons from the scene
    /// </summary>
    private void SetupManualButtons()
    {
        levelButtons.Clear();

        if (manualLevelButtons.Count == 0)
        {
            Debug.LogError("[LevelSelectionUI] No manual buttons assigned! Drag your buttons into the Manual Level Buttons list.");
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
                Debug.LogWarning($"[LevelSelectionUI] Button {i + 1} is null!");
                continue;
            }

            int levelNumber = i + 1;
            SetupButton(button, levelNumber);
            levelButtons.Add(button);
        }
    }

    /// <summary>
    /// Generate buttons automatically
    /// </summary>
    private void GenerateLevelButtons()
    {
        if (levelButtonPrefab == null || levelButtonContainer == null)
        {
            Debug.LogError("[LevelSelectionUI] Missing prefab or container for auto-generation!");
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
            Debug.LogError("[LevelSelectionUI] Button component missing on prefab!");
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

        // Find and show/hide the lock GameObject
        Transform lockTransform = button.transform.Find("lock");
        if (lockTransform != null)
        {
            lockTransform.gameObject.SetActive(!isUnlocked);
        }

        // Set text - keep original name and add number
        if (buttonText != null)
        {
            // Store original text (the custom name the user set)
            string originalName = buttonText.text;

            // If text is empty or already has a number pattern, use level number only
            if (string.IsNullOrEmpty(originalName) || originalName == $"{levelNumber}" || originalName == $"{levelNumber}\n✓")
            {
                originalName = "";
            }

            if (isCompleted)
            {
                if (string.IsNullOrEmpty(originalName))
                {
                    buttonText.text = $"{levelNumber}\n✓";
                }
                else
                {
                    buttonText.text = $"{originalName}\n{levelNumber} ✓";
                }
            }
            else
            {
                if (string.IsNullOrEmpty(originalName))
                {
                    buttonText.text = $"{levelNumber}";
                }
                else
                {
                    buttonText.text = $"{originalName}\n{levelNumber}";
                }
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

        // Setup button click - make all buttons clickable (locked buttons will play sound)
        button.interactable = true;

        // Remove old listeners to prevent duplicates
        button.onClick.RemoveAllListeners();

        int capturedLevelNum = levelNumber;
        bool capturedIsUnlocked = isUnlocked;
        button.onClick.AddListener(() => OnLevelButtonClicked(capturedLevelNum, capturedIsUnlocked));
    }

    /// <summary>
    /// Check if level is unlocked (Level 1 always unlocked, others need previous level complete)
    /// </summary>
    private bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber == 1)
            return true;

        return IsLevelCompleted(levelNumber - 1);
    }

    /// <summary>
    /// Check if level is completed
    /// </summary>
    private bool IsLevelCompleted(int levelNumber)
    {
        string key = $"Level_{levelNumber}_Completed";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private void OnLevelButtonClicked(int levelNumber, bool isUnlocked)
    {
        if (!isUnlocked)
        {
            // Play locked button sound
            PlaySound(lockedButtonSound);
            return;
        }

        // Play button click sound
        PlaySound(buttonClickSound);

        LoadLevel(levelNumber);
    }

    private void LoadLevel(int levelNumber)
    {
        string sceneName = $"{levelScenePrefix}{levelNumber}";

        // Save which level we're loading (0-indexed for CurrentLevel)
        PlayerPrefs.SetInt("CurrentLevel", levelNumber - 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator AnimateButtonsSequence()
    {
        float currentDelay = 0f;

        // First 2 buttons
        for (int i = 0; i < 2 && i < levelButtons.Count; i++)
        {
            if (levelButtons[i] != null)
            {
                StartCoroutine(AnimateButtonPopIn(levelButtons[i].transform, currentDelay));
                currentDelay += buttonAnimationDelay;
            }
        }

        // Wait for animations to finish (delay + animation duration) + pause
        yield return new WaitForSeconds(currentDelay + buttonPopDuration + 0.3f);
        currentDelay = 0f;

        // Next 3 buttons (buttons 2, 3, 4)
        for (int i = 2; i < 5 && i < levelButtons.Count; i++)
        {
            if (levelButtons[i] != null)
            {
                StartCoroutine(AnimateButtonPopIn(levelButtons[i].transform, currentDelay));
                currentDelay += buttonAnimationDelay;
            }
        }

        // Wait for animations to finish (delay + animation duration) + pause
        yield return new WaitForSeconds(currentDelay + buttonPopDuration + 0.3f);
        currentDelay = 0f;

        // Rest of the buttons (from button 5 onwards)
        for (int i = 5; i < levelButtons.Count; i++)
        {
            if (levelButtons[i] != null)
            {
                StartCoroutine(AnimateButtonPopIn(levelButtons[i].transform, currentDelay));
                currentDelay += buttonAnimationDelay;
            }
        }

        yield return null;
    }

    private IEnumerator AnimateButtonPopIn(Transform buttonTransform, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Play button pop sound
        PlaySound(buttonPopSound);

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

    /// <summary>
    /// Mark a level as complete (call this when level is finished!)
    /// </summary>
    public static void MarkLevelComplete(int levelNumber)
    {
        string key = $"Level_{levelNumber}_Completed";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }
}
