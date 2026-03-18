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
    [SerializeField] private Text titleText;

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

    [Header("🎁 Bonus Level Settings")]
    [SerializeField] private List<int> bonusLevelNumbers = new List<int>();
    [Tooltip("Level numbers that are bonus levels (unlocked by watching rewarded ad)")]
    [SerializeField] private List<Button> manualBonusLevelButtons = new List<Button>();
    [Tooltip("Drag bonus level buttons here. Each button maps to the matching bonusLevelNumbers entry.")]
    [SerializeField] private Sprite bonusLockedIcon;
    [Tooltip("Icon for locked bonus levels (e.g., video/ad icon)")]
    [SerializeField] private Color bonusLockedColor = new Color(1f, 0.8f, 0.2f, 1f);
    [Tooltip("Color for locked bonus level buttons (gold/yellow)")]
    [SerializeField] private string bonusScenePrefix = "Bonus";
    [Tooltip("Scene name prefix for bonus levels (e.g., Bonus1, Bonus2). Set in Build Settings accordingly.")]
    [SerializeField] private BonusLevelDialog bonusLevelDialog;
    [Tooltip("Reference to the bonus level popup dialog (asks player to watch ad)")]

    [Header("📄 Page Navigation")]
    [SerializeField] private List<GameObject> pages = new List<GameObject>();
    [Tooltip("Drag your page GameObjects here in order (Page1, Page2, Page3, etc.). Each page contains its own level buttons.")]
    [SerializeField] private Button nextPageButton;
    [Tooltip("Arrow button to go to next page")]
    [SerializeField] private Button prevPageButton;
    [Tooltip("Arrow button to go to previous page")]
    [SerializeField] private List<Image> pageDots = new List<Image>();
    [Tooltip("Page indicator dots (optional). Drag Image objects here, one per page.")]
    [SerializeField] private Color activeDotColor = Color.white;
    [SerializeField] private Color inactiveDotColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private float pageSlideSpeed = 8f;
    [Tooltip("Speed of page slide animation (higher = faster)")]

    [Header("🖼️ Page Backgrounds")]
    [SerializeField] private Image backgroundImage;
    [Tooltip("Reference to the background Image component in the scene")]
    [SerializeField] private List<Sprite> pageBackgrounds = new List<Sprite>();
    [Tooltip("Background image per page. Index 0 = Page 1, Index 1 = Page 2, etc.")]

    [Header("🔘 Buttons Per Page")]
    [SerializeField] private List<Button> page1Buttons = new List<Button>();
    [Tooltip("Drag buttons that belong to Page 1")]
    [SerializeField] private List<Button> page2Buttons = new List<Button>();
    [Tooltip("Drag buttons that belong to Page 2")]
    [SerializeField] private List<Button> page3Buttons = new List<Button>();
    [Tooltip("Drag buttons that belong to Page 3")]

    private int currentPage = 0;
    private bool isPageAnimating = false;

    [Header("✨ Animation Settings")]
    [SerializeField] private bool animateButtonsOnStart = true;
    [SerializeField] private float buttonAnimationDelay = 0.5f;
    [SerializeField] private float buttonPopDuration = 0.3f;
    [SerializeField] private AnimationCurve buttonPopCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float lockShakeDelay = 0.3f;
    [Tooltip("Delay before lock shake animation starts (in seconds)")]

    [Header("🔊 Audio Settings")]
    [SerializeField] private AudioSource musicAudioSource;
    [Tooltip("Audio source for background music (will be created automatically if not assigned)")]
    [SerializeField] private AudioSource sfxAudioSource;
    [Tooltip("Audio source for sound effects (will be created automatically if not assigned)")]
    [SerializeField] private AudioClip backgroundMusic;
    [Tooltip("Default background music (used if no per-page music is set)")]
    [SerializeField] private List<AudioClip> pageMusic = new List<AudioClip>();
    [Tooltip("Background music per page. Index 0 = Page 1, Index 1 = Page 2, etc. Falls back to backgroundMusic if empty.")]
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
        // Initialize music audio source
        if (musicAudioSource == null)
        {
            musicAudioSource = gameObject.AddComponent<AudioSource>();
            musicAudioSource.playOnAwake = false;
            musicAudioSource.loop = true;
            Debug.Log("[LevelSelectionUI] Created music AudioSource");
        }

        // Initialize SFX audio source
        if (sfxAudioSource == null)
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            Debug.Log("[LevelSelectionUI] Created SFX AudioSource");
        }
    }

    private void Start()
    {
        // Setup first page visuals
        PlayPageMusic(0);
        UpdatePageBackground(0);

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

        // Setup page navigation
        SetupPages();

        // Animate only the first page's buttons
        if (animateButtonsOnStart)
        {
            AnimateCurrentPageButtons();
        }
    }

    // ==================== Page Navigation ====================

    private void SetupPages()
    {
        if (pages.Count <= 1) return;

        // Show only first page
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == 0);
        }

        currentPage = 0;

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveAllListeners();
            nextPageButton.onClick.AddListener(NextPage);
        }

        if (prevPageButton != null)
        {
            prevPageButton.onClick.RemoveAllListeners();
            prevPageButton.onClick.AddListener(PreviousPage);
        }

        UpdatePageUI();
    }

    public void NextPage()
    {
        if (currentPage >= pages.Count - 1 || isPageAnimating) return;
        PlaySound(buttonClickSound);
        StartCoroutine(SlidePage(currentPage, currentPage + 1));
    }

    public void PreviousPage()
    {
        if (currentPage <= 0 || isPageAnimating) return;
        PlaySound(buttonClickSound);
        StartCoroutine(SlidePage(currentPage, currentPage - 1));
    }

    private IEnumerator SlidePage(int fromPage, int toPage)
    {
        isPageAnimating = true;

        GameObject fromObj = pages[fromPage];
        GameObject toObj = pages[toPage];

        if (fromObj == null || toObj == null)
        {
            isPageAnimating = false;
            yield break;
        }

        float direction = toPage > fromPage ? -1f : 1f;

        RectTransform fromRect = fromObj.GetComponent<RectTransform>();
        RectTransform toRect = toObj.GetComponent<RectTransform>();

        if (fromRect == null || toRect == null)
        {
            fromObj.SetActive(false);
            toObj.SetActive(true);
            currentPage = toPage;
            UpdatePageUI();
            PlayPageMusic(toPage);
            UpdatePageBackground(toPage);
            isPageAnimating = false;
            yield break;
        }

        // Get canvas width for slide distance
        float slideDistance = 1200f;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
                slideDistance = canvasRect.rect.width;
        }

        Vector2 fromStart = fromRect.anchoredPosition;
        Vector2 toStart = new Vector2(-direction * slideDistance, fromStart.y);
        toRect.anchoredPosition = toStart;
        toObj.SetActive(true);

        // Hide buttons on incoming page before animation
        if (animateButtonsOnStart)
        {
            List<Button> toPageButtons = GetPageButtons(toPage);
            if (toPageButtons.Count == 0 && toObj != null)
            {
                toPageButtons = new List<Button>(toObj.GetComponentsInChildren<Button>(true));
            }
            foreach (Button btn in toPageButtons)
            {
                if (btn != null)
                    btn.transform.localScale = Vector3.zero;
            }
        }

        float elapsed = 0f;
        float duration = 1f / pageSlideSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            fromRect.anchoredPosition = Vector2.Lerp(fromStart, new Vector2(direction * slideDistance, fromStart.y), t);
            toRect.anchoredPosition = Vector2.Lerp(toStart, fromStart, t);
            yield return null;
        }

        fromObj.SetActive(false);
        fromRect.anchoredPosition = fromStart;
        toRect.anchoredPosition = fromStart;

        currentPage = toPage;
        UpdatePageUI();
        PlayPageMusic(toPage);
        UpdatePageBackground(toPage);

        // Animate buttons on the new page
        if (animateButtonsOnStart)
        {
            AnimateCurrentPageButtons();
        }

        isPageAnimating = false;
    }

    /// <summary>
    /// Get the buttons list for a specific page index
    /// </summary>
    private List<Button> GetPageButtons(int pageIndex)
    {
        switch (pageIndex)
        {
            case 0: return page1Buttons;
            case 1: return page2Buttons;
            case 2: return page3Buttons;
            default: return new List<Button>();
        }
    }

    /// <summary>
    /// Animate only the buttons on the current page
    /// </summary>
    private void AnimateCurrentPageButtons()
    {
        // Try per-page button lists first
        List<Button> buttons = GetPageButtons(currentPage);

        // If no manual list, try finding buttons as children of the page GameObject
        if (buttons.Count == 0 && currentPage < pages.Count && pages[currentPage] != null)
        {
            buttons = new List<Button>(pages[currentPage].GetComponentsInChildren<Button>(true));
        }

        // Final fallback: no pages at all, animate everything
        if (buttons.Count == 0)
        {
            if (pages.Count == 0)
            {
                StartCoroutine(AnimateButtonsSequence());
            }
            return;
        }

        float delay = 0f;
        foreach (Button btn in buttons)
        {
            if (btn == null) continue;
            btn.transform.localScale = Vector3.zero;
            StartCoroutine(AnimateButtonPopIn(btn.transform, delay));
            delay += buttonAnimationDelay * 0.5f;
        }
    }

    /// <summary>
    /// Play the music assigned to a specific page, or fallback to default
    /// </summary>
    /// <summary>
    /// Update background image for the given page
    /// </summary>
    private void UpdatePageBackground(int pageIndex)
    {
        if (backgroundImage == null) return;
        if (pageIndex >= 0 && pageIndex < pageBackgrounds.Count && pageBackgrounds[pageIndex] != null)
        {
            backgroundImage.sprite = pageBackgrounds[pageIndex];
        }
    }

    private void PlayPageMusic(int pageIndex)
    {
        if (musicAudioSource == null) return;

        AudioClip clip = null;

        // Try per-page music first
        if (pageIndex >= 0 && pageIndex < pageMusic.Count && pageMusic[pageIndex] != null)
        {
            clip = pageMusic[pageIndex];
        }
        else if (backgroundMusic != null)
        {
            clip = backgroundMusic;
        }

        if (clip == null)
        {
            musicAudioSource.Stop();
            return;
        }

        // Don't restart if already playing the same clip
        if (musicAudioSource.clip == clip && musicAudioSource.isPlaying)
            return;

        musicAudioSource.volume = musicVolume;
        musicAudioSource.clip = clip;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    private void UpdatePageUI()
    {
        if (prevPageButton != null)
            prevPageButton.gameObject.SetActive(currentPage > 0);
        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(currentPage < pages.Count - 1);

        for (int i = 0; i < pageDots.Count; i++)
        {
            if (pageDots[i] != null)
                pageDots[i].color = (i == currentPage) ? activeDotColor : inactiveDotColor;
        }
    }

    // ==================== End Page Navigation ====================

    private void OnDestroy()
    {
        // Stop music when leaving scene
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
    }

    /// <summary>
    /// Play a sound effect
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip, soundVolume);
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

        // Setup each regular button
        for (int i = 0; i < manualLevelButtons.Count && i < totalLevels; i++)
        {
            Button button = manualLevelButtons[i];
            if (button == null)
            {
                Debug.LogWarning($"[LevelSelectionUI] Button {i + 1} is null!");
                continue;
            }

            int levelNumber = i + 1;
            SetupButton(button, levelNumber, false);
            levelButtons.Add(button);
        }

        // Setup bonus level buttons from separate list
        SetupManualBonusButtons();
    }

    /// <summary>
    /// Setup manually placed bonus level buttons from the separate list
    /// </summary>
    private void SetupManualBonusButtons()
    {
        if (manualBonusLevelButtons.Count == 0) return;

        for (int i = 0; i < manualBonusLevelButtons.Count; i++)
        {
            Button button = manualBonusLevelButtons[i];
            if (button == null)
            {
                Debug.LogWarning($"[LevelSelectionUI] Bonus button {i} is null!");
                continue;
            }

            // Map to the corresponding bonus level number
            int levelNumber;
            if (i < bonusLevelNumbers.Count)
            {
                levelNumber = bonusLevelNumbers[i];
            }
            else
            {
                Debug.LogWarning($"[LevelSelectionUI] Bonus button {i} has no matching bonusLevelNumbers entry!");
                continue;
            }

            SetupButton(button, levelNumber, true);
            levelButtons.Add(button);
            Debug.Log($"[LevelSelectionUI] Bonus button setup: index {i} -> Level {levelNumber}");
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

        SetupButton(button, levelNumber, IsBonusLevel(levelNumber));
        levelButtons.Add(button);
    }

    /// <summary>
    /// Setup a button's appearance and click handler
    /// </summary>
    private void SetupButton(Button button, int levelNumber, bool isBonus)
    {
        Image buttonImage = button.GetComponent<Image>();
        Text buttonText = button.GetComponentInChildren<Text>();

        bool isUnlocked = IsLevelUnlocked(levelNumber);
        bool isCompleted = IsLevelCompleted(levelNumber);

        // Find and show/hide the lock GameObject
        Transform lockTransform = button.transform.Find("lock parent");
        if (lockTransform != null)
        {
            // Bonus levels: show lock only if not unlocked via ad
            // Regular levels: show lock if not unlocked
            lockTransform.gameObject.SetActive(!isUnlocked);
        }

        // Hide "watch ad" instruction text on bonus buttons when unlocked
        Transform watchAdText = button.transform.Find("Text (watchAd)");
        if (watchAdText != null)
        {
            watchAdText.gameObject.SetActive(!(isBonus && isUnlocked));
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
            else if (!isUnlocked && isBonus)
            {
                // Bonus level locked - show special ad/bonus icon
                if (bonusLockedIcon != null)
                    buttonImage.sprite = bonusLockedIcon;
                else if (lockedIcon != null)
                    buttonImage.sprite = lockedIcon;
                buttonImage.color = bonusLockedColor;
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
        bool capturedIsBonus = isBonus;
        Button capturedButton = button;
        button.onClick.AddListener(() => OnLevelButtonClicked(capturedLevelNum, capturedIsUnlocked, capturedButton, capturedIsBonus));
    }

    /// <summary>
    /// Check if this level number is a bonus level
    /// </summary>
    private bool IsBonusLevel(int levelNumber)
    {
        return bonusLevelNumbers.Contains(levelNumber);
    }

    /// <summary>
    /// Check if a bonus level has been unlocked by watching an ad
    /// </summary>
    private bool IsBonusLevelUnlocked(int levelNumber)
    {
        string key = $"BonusLevel_{levelNumber}_Unlocked";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// Mark a bonus level as unlocked (after watching rewarded ad)
    /// </summary>
    public static void UnlockBonusLevel(int levelNumber)
    {
        string key = $"BonusLevel_{levelNumber}_Unlocked";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Debug.Log($"[LevelSelectionUI] 🎁 Bonus Level {levelNumber} unlocked!");
    }

    /// <summary>
    /// Check if level is unlocked (Level 1 always unlocked, others need previous level complete)
    /// Bonus levels require watching a rewarded ad
    /// </summary>
    private bool IsLevelUnlocked(int levelNumber)
    {
        // Bonus levels: always check via ad unlock first, regardless of level number
        if (IsBonusLevel(levelNumber))
            return IsBonusLevelUnlocked(levelNumber);

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

    private void OnLevelButtonClicked(int levelNumber, bool isUnlocked, Button button, bool isBonus = false)
    {
        if (!isUnlocked)
        {
            // Bonus level: show rewarded ad to unlock
            if (isBonus)
            {
                PlaySound(buttonClickSound);
                ShowRewardedAdForBonusLevel(levelNumber, button);
                return;
            }

            // Regular locked level: play error sound and shake
            PlaySound(lockedButtonSound);

            Transform lockTransform = button.transform.Find("lock parent");
            if (lockTransform != null)
            {
                StartCoroutine(ShakeLock(lockTransform));
            }

            return;
        }

        // Play button click sound
        PlaySound(buttonClickSound);

        LoadLevel(levelNumber);
    }

    /// <summary>
    /// Show a rewarded ad to unlock a bonus level
    /// </summary>
    private void ShowRewardedAdForBonusLevel(int levelNumber, Button button)
    {
        Debug.Log($"[LevelSelectionUI] 🎬 Bonus Level {levelNumber} clicked, showing dialog...");

        // Show dialog first - when user confirms, show the ad
        if (bonusLevelDialog != null)
        {
            bonusLevelDialog.Open(() =>
            {
                // User clicked "Watch Ad" in the dialog
                StartBonusAd(levelNumber, button);
            });
        }
        else
        {
            // No dialog assigned - show ad directly (fallback)
            Debug.LogWarning("[LevelSelectionUI] BonusLevelDialog not assigned! Showing ad directly.");
            StartBonusAd(levelNumber, button);
        }
    }

    private void StartBonusAd(int levelNumber, Button button)
    {
        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogWarning("[LevelSelectionUI] RewardedAdsManager not found! Unlocking bonus level directly.");
            OnBonusAdRewardGranted(levelNumber, button);
            return;
        }

        if (!RewardedAdsManager.Instance.IsReady())
        {
            Debug.Log("[LevelSelectionUI] Ad not ready, preloading...");
            RewardedAdsManager.Instance.Preload((loaded) =>
            {
                if (loaded)
                {
                    ShowBonusAdNow(levelNumber, button);
                }
                else
                {
                    Debug.LogWarning("[LevelSelectionUI] Failed to load ad for bonus level");
                    PlaySound(lockedButtonSound);
                }
            });
            return;
        }

        ShowBonusAdNow(levelNumber, button);
    }

    private void ShowBonusAdNow(int levelNumber, Button button)
    {
        RewardedAdsManager.Instance.ShowRewarded(
            onReward: () =>
            {
                Debug.Log($"[LevelSelectionUI] 🎁 Ad reward received for Bonus Level {levelNumber}!");
                OnBonusAdRewardGranted(levelNumber, button);
            },
            onClosed: (completed) =>
            {
                if (!completed)
                {
                    Debug.Log("[LevelSelectionUI] Ad closed without completing");
                }
            },
            onFailed: (error) =>
            {
                Debug.LogWarning($"[LevelSelectionUI] Ad failed: {error}");
                PlaySound(lockedButtonSound);
            }
        );
    }

    private void OnBonusAdRewardGranted(int levelNumber, Button button)
    {
        // Unlock the bonus level
        UnlockBonusLevel(levelNumber);

        // Refresh button appearance
        SetupButton(button, levelNumber, true);

        // Auto-load the bonus level
        LoadLevel(levelNumber);
    }

    private void LoadLevel(int levelNumber)
    {
        string sceneName;
        if (IsBonusLevel(levelNumber))
        {
            int bonusIndex = bonusLevelNumbers.IndexOf(levelNumber) + 1;
            sceneName = $"{bonusScenePrefix}{bonusIndex}";
        }
        else
        {
            sceneName = $"{levelScenePrefix}{levelNumber}";
        }

        // Check if scene exists in build settings
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            // Save which level we're loading (0-indexed for CurrentLevel)
            PlayerPrefs.SetInt("CurrentLevel", levelNumber - 1);
            PlayerPrefs.Save();

            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"[LevelSelectionUI] Scene '{sceneName}' not found in Build Settings! Add it via File -> Build Profiles.");
        }
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

    private IEnumerator ShakeLock(Transform lockTransform)
    {
        // Wait before starting shake
        yield return new WaitForSeconds(lockShakeDelay);

        Vector3 originalRotation = lockTransform.localEulerAngles;
        float shakeDuration = 0.6f;
        float shakeAmount = 15f; // degrees
        int shakeCount = 3;
        float timePerShake = shakeDuration / shakeCount;

        for (int i = 0; i < shakeCount; i++)
        {
            float elapsed = 0f;
            while (elapsed < timePerShake)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / timePerShake;

                // Shake left and right using sine wave
                float angle = Mathf.Sin(t * Mathf.PI * 2) * shakeAmount * (1f - t);
                lockTransform.localEulerAngles = new Vector3(
                    originalRotation.x,
                    originalRotation.y,
                    originalRotation.z + angle
                );

                yield return null;
            }
        }

        // Reset to original rotation
        lockTransform.localEulerAngles = originalRotation;
    }

    /// <summary>
    /// Refresh all button visuals to match current PlayerPrefs state
    /// Call after resetting progress to update lock/unlock appearance
    /// </summary>
    public void RefreshAllButtons()
    {
        Debug.Log("[LevelSelectionUI] RefreshAllButtons called!");

        // Re-setup regular buttons
        for (int i = 0; i < manualLevelButtons.Count && i < totalLevels; i++)
        {
            Button button = manualLevelButtons[i];
            if (button == null) continue;
            int levelNumber = i + 1;
            Debug.Log($"[LevelSelectionUI] Refreshing regular button {levelNumber}, unlocked={IsLevelUnlocked(levelNumber)}");
            SetupButton(button, levelNumber, false);
        }

        // Re-setup bonus buttons
        for (int i = 0; i < manualBonusLevelButtons.Count; i++)
        {
            Button button = manualBonusLevelButtons[i];
            if (button == null || i >= bonusLevelNumbers.Count) continue;
            int levelNumber = bonusLevelNumbers[i];
            Debug.Log($"[LevelSelectionUI] Refreshing bonus button {levelNumber}, unlocked={IsLevelUnlocked(levelNumber)}, PlayerPrefs BonusLevel_{levelNumber}_Unlocked={PlayerPrefs.GetInt($"BonusLevel_{levelNumber}_Unlocked", 0)}");
            SetupButton(button, levelNumber, true);
        }

        Debug.Log("[LevelSelectionUI] All buttons refreshed!");
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
