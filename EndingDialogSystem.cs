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

    [Header("📊 Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bubblePopSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    [Header("Settings")]
    [SerializeField] private string sceneToLoad = "MainMenu";
    [SerializeField] private bool quitGameInsteadOfLoadScene = false;
    
    [Header("🎬 Ad Settings")]
    [SerializeField] private bool showAdAfterDialog = true;
    [Tooltip("Show rewarded ad after dialog finishes")]
    [SerializeField] private bool skipAdsInEditor = true;
    [Tooltip("Skip ads when running in Unity Editor")]

    private int currentDialog = 0;
    private Coroutine autoAdvanceCoroutine = null;
    private bool skipRequested = false;
    private int bubblesPopped = 0;
    private bool[] bubbleStates;

    void Start()
    {
        bubbleStates = new bool[imageAnimators.Length];

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

                SpriteRenderer[] childSprites = imageAnimators[i].GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sprite in childSprites)
                {
                    if (sprite.GetComponent<Collider2D>() == null)
                    {
                        sprite.gameObject.AddComponent<BoxCollider2D>();
                        Debug.Log($"[EndingDialogController] Added collider to sprite: {sprite.name}");
                    }

                    var button = sprite.GetComponent<Button>();
                    if (button == null)
                    {
                        button = sprite.gameObject.AddComponent<Button>();
                    }

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnBubbleClicked(bubbleIndex));

                    Debug.Log($"[EndingDialogController] Added click handler to sprite bubble {bubbleIndex}: {sprite.name}");
                }

                UnityEngine.UI.Image[] childImages = imageAnimators[i].GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var image in childImages)
                {
                    image.raycastTarget = true;

                    var button = image.GetComponent<Button>();
                    if (button == null)
                    {
                        button = image.gameObject.AddComponent<Button>();
                    }

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnBubbleClicked(bubbleIndex));

                    Debug.Log($"[EndingDialogController] Added click handler to UI bubble {bubbleIndex}: {image.name}");
                }
            }
        }
    }

    private void OnBubbleClicked(int bubbleIndex)
    {
        if (bubbleStates[bubbleIndex])
        {
            Debug.Log($"[EndingDialogController] Bubble {bubbleIndex} already popped!");
            return;
        }

        Debug.Log($"[EndingDialogController] Bubble {bubbleIndex} clicked!");

        bubbleStates[bubbleIndex] = true;
        bubblesPopped++;

        StartCoroutine(PopBubble(bubbleIndex));
        PlayBubbleSound();

        if (bubblesPopped >= imageAnimators.Length)
        {
            Debug.Log("[EndingDialogController] 🎉 All bubbles popped! Moving to next scene...");

            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            StartCoroutine(WaitAndEndGame());
        }
    }

    private System.Collections.IEnumerator PopBubble(int bubbleIndex)
    {
        if (imageAnimators[bubbleIndex] == null)
            yield break;

        Transform bubbleTransform = imageAnimators[bubbleIndex].transform;
        Vector3 originalScale = bubbleTransform.localScale;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            float scale = Mathf.Lerp(1f, 0f, progress);
            if (progress < 0.3f)
            {
                scale = Mathf.Lerp(1f, 1.2f, progress / 0.3f);
            }
            else
            {
                scale = Mathf.Lerp(1.2f, 0f, (progress - 0.3f) / 0.7f);
            }

            bubbleTransform.localScale = originalScale * scale;
            yield return null;
        }

        imageAnimators[bubbleIndex].gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator WaitAndEndGame()
    {
        yield return new WaitForSeconds(0.5f);
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
        
        // ✅ Skip ads in Editor if enabled
        #if UNITY_EDITOR
        if (skipAdsInEditor)
        {
            Debug.Log("[EndingDialogController] ⏭️ Skipping ad in Editor");
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
                float timeout = 5f; // Much shorter timeout
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
            Debug.Log("[EndingDialogController] No ads to show, proceeding to next scene");
        }

        // ✅ ALWAYS load next scene or quit
        Debug.Log("[EndingDialogController] 🚀 Loading next scene/quitting...");
        
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
            Debug.Log($"[EndingDialogController] Loading scene: {sceneToLoad}");
            
            // ✅ Use LevelManager if available
            if (LevelManager.Instance != null)
            {
                Debug.Log("[EndingDialogController] Using LevelManager to advance");
                LevelManager.Instance.AdvanceToNextLevel();
            }
            else
            {
                // Fallback: Load scene directly
                SceneManager.LoadScene(sceneToLoad);
            }
        }
    }

    public void StartEndingDialog()
    {
        Debug.Log("[EndingDialogController] ✅ StartEndingDialog called!");
        
        currentDialog = 0;
        skipRequested = false;
        bubblesPopped = 0;

        if (bubbleStates == null || bubbleStates.Length != imageAnimators.Length)
        {
            bubbleStates = new bool[imageAnimators.Length];
        }
        else
        {
            for (int i = 0; i < bubbleStates.Length; i++)
            {
                bubbleStates[i] = false;
            }
        }

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
