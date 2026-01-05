using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingDialogController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject bubbleMaster;
    [SerializeField] private Animator[] imageAnimators;
    [SerializeField] private Button nextButton;
    [SerializeField] private Text buttonText;

    [Header("Animation Settings")]
    [SerializeField] private float delayBetweenBubbles = 0.3f;
    [SerializeField] private float allBubblesDisplayTime = 2.0f;
    [SerializeField] private bool autoAdvance = true;
    [SerializeField] private bool allowClickToSkip = true;
    [Tooltip("Allow clicking on bubbles to skip to ad and next scene")]

    [Header("🔊 Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bubblePopSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    [Header("Settings")]
    [SerializeField] private string levelSelectionScene = "LevelSelection";
    [SerializeField] private bool quitGameInsteadOfLoadScene = false;
    
    [Header("🎓 Tutorial Mode")]
    [SerializeField] private bool isTutorialMode = false;
    [Tooltip("Enable this for Level0 - skips all ads and goes straight to LevelSelection")]
    
    [Header("🎬 Ad Settings")]
    [SerializeField] private bool showAdAfterDialog = true;
    [Tooltip("Show rewarded ad after dialog finishes")]
    [SerializeField] private bool skipAdsInEditor = true;
    [Tooltip("Skip ads when running in Unity Editor")]

    private int currentDialog = 0;
    private Coroutine autoAdvanceCoroutine = null;
    private bool skipRequested = false;

    void Start()
    {
        if (isTutorialMode)
        {
            Debug.Log("[EndingDialogController] 🎓 TUTORIAL MODE - No ads will be shown");
        }

        if (bubbleMaster != null)
        {
            bubbleMaster.SetActive(false);
        }

        foreach (var animator in imageAnimators)
        {
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
            if (autoAdvance)
                nextButton.gameObject.SetActive(false);
        }

        if (allowClickToSkip)
        {
            SetupBubbleClickListeners();
        }
    }

    private void SetupBubbleClickListeners()
    {
        for (int i = 0; i < imageAnimators.Length; i++)
        {
            if (imageAnimators[i] != null)
            {
                int bubbleIndex = i;
                GameObject bubbleObject = imageAnimators[i].gameObject;
                
                // מטפל רק ב-UI Buttons
                var button = bubbleObject.GetComponent<Button>();
                if (button == null)
                {
                    button = bubbleObject.AddComponent<Button>();
                    Debug.Log($"[EndingDialogController] Added Button to bubble {bubbleIndex}");
                }
                
                // בדוק אם יש Image ו-set raycast target
                var image = bubbleObject.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.raycastTarget = true;
                }
                
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnBubbleClicked(bubbleIndex));
                
                Debug.Log($"[EndingDialogController] ✅ Added click handler to bubble {bubbleIndex}: {bubbleObject.name}");
            }
        }
    }

    public void OnBubbleClicked(int bubbleIndex)
    {
        Debug.Log($"[EndingDialogController] 🎯 Bubble {bubbleIndex} clicked! Completing level and returning to menu...");
        
        // עצור כל קורוטינות
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
        
        // נגן צליל
        PlayBubbleSound();
        
        // השלם את הלבל וחזור לתפריט
        EndGame();
    }

    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }

    private void ShowCurrentDialog()
    {
        if (currentDialog < imageAnimators.Length && imageAnimators[currentDialog] != null)
        {
            imageAnimators[currentDialog].enabled = true;
            PlayBubbleSound();
            Debug.Log($"[EndingDialogController] 🎬 Enabled Animator {currentDialog}");
        }

        if (!autoAdvance && buttonText != null)
        {
            buttonText.text = (currentDialog == imageAnimators.Length - 1) ? "סיום" : "המשך";
        }
    }

    private void PlayBubbleSound()
    {
        if (bubblePopSound == null) return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        audioSource.PlayOneShot(bubblePopSound, soundVolume);
    }

    private void OnNextClicked()
    {
        currentDialog++;

        if (currentDialog >= imageAnimators.Length)
            EndGame();
        else
            ShowCurrentDialog();
    }

    private void EndGame()
    {
        Debug.Log("[EndingDialogController] 🎬 EndGame called!");
        StartCoroutine(EndGameCoroutine());
    }

    private IEnumerator EndGameCoroutine()
    {
        yield return new WaitForSeconds(0.3f);

        bool shouldShowAd = showAdAfterDialog;
        
        // ✅ TUTORIAL MODE - Skip all ads
        if (isTutorialMode)
        {
            Debug.Log("[EndingDialogController] 🎓 Tutorial mode - skipping ads completely");
            shouldShowAd = false;
        }
        
        // ✅ Skip ads in Editor if enabled
        #if UNITY_EDITOR
        if (skipAdsInEditor)
        {
            Debug.Log("[EndingDialogController] ⭐️ Skipping ad in Editor");
            shouldShowAd = false;
        }
        #endif

        // ✅ Show ad if enabled
        if (shouldShowAd && RewardedAdsManager.Instance != null)
        {
            Debug.Log("[EndingDialogController] 📺 Checking if ad is ready...");
            
            if (RewardedAdsManager.Instance.IsReady())
            {
                Debug.Log("[EndingDialogController] 📺 Ad is ready! Showing...");
                
                bool adFinished = false;

                RewardedAdsManager.Instance.ShowRewarded(
                    onReward: () =>
                    {
                        Debug.Log("[EndingDialogController] Ad reward received!");
                        adFinished = true;
                    },
                    onClosed: (completed) => 
                    { 
                        Debug.Log("[EndingDialogController] Ad closed");
                        adFinished = true; 
                    },
                    onFailed: (error) => 
                    { 
                        Debug.LogWarning($"[EndingDialogController] Ad failed: {error}");
                        adFinished = true; 
                    }
                );

                // ✅ Wait for ad with shorter timeout
                float timeout = 5f;
                float elapsed = 0f;

                while (!adFinished && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (elapsed >= timeout)
                {
                    Debug.LogWarning("[EndingDialogController] ⏰ Ad timeout! Continuing anyway...");
                }
                
                Debug.Log("[EndingDialogController] Ad finished or timed out");
            }
            else
            {
                Debug.LogWarning("[EndingDialogController] Ad not ready, skipping");
            }

            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            if (isTutorialMode)
            {
                Debug.Log("[EndingDialogController] 🎓 Tutorial completed - going to LevelSelection");
                
                // ✅ Mark tutorial as completed so it won't show again
                PlayerPrefs.SetInt("IsFirstTime", 0);
                PlayerPrefs.Save();
                Debug.Log("[EndingDialogController] ✅ Marked IsFirstTime = 0");
            }
            else
            {
                Debug.Log("[EndingDialogController] No ads to show, proceeding to complete level");
            }
        }

        // ✅ רק טוען LevelSelection - הלבל כבר הושלם!
        Debug.Log("[EndingDialogController] 🔙 Loading LevelSelection scene...");
        
        if (quitGameInsteadOfLoadScene)
        {
            Debug.Log("[EndingDialogController] Quitting game...");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene(levelSelectionScene);
        }
    }

    public void StartEndingDialog()
    {
        Debug.Log("[EndingDialogController] ✅ StartEndingDialog called!");
        
        if (isTutorialMode)
        {
            Debug.Log("[EndingDialogController] 🎓 Starting tutorial ending dialog");
        }
        
        currentDialog = 0;
        skipRequested = false;

        if (bubbleMaster != null)
        {
            bubbleMaster.SetActive(true);
            Debug.Log("[EndingDialogController] ✅ BubbleMaster activated");
        }

        foreach (var animator in imageAnimators)
        {
            if (animator != null)
            {
                animator.gameObject.SetActive(true);
                animator.transform.localScale = Vector3.one;
            }
        }

        if (autoAdvance)
        {
            if (autoAdvanceCoroutine != null)
                StopCoroutine(autoAdvanceCoroutine);

            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceDialogs());
        }
        else
        {
            ShowCurrentDialog();
        }
    }

    private IEnumerator AutoAdvanceDialogs()
    {
        Debug.Log("[EndingDialogController] Starting auto-advance dialogs...");
        
        for (int i = 0; i < imageAnimators.Length; i++)
        {
            currentDialog = i;
            ShowCurrentDialog();
            yield return new WaitForSeconds(delayBetweenBubbles);
        }

        Debug.Log("[EndingDialogController] All bubbles shown. Waiting for player clicks...");
    }
}
