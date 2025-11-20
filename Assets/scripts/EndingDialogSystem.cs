using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingDialogController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject bubbleMaster; // האובייקט הראשי שמכיל הכל
    [SerializeField] private Animator[] imageAnimators; // 4 תמונות דמויות עם Animator
    [SerializeField] private Button nextButton;
    [SerializeField] private Text buttonText;

    [Header("Animation Settings")]
    [SerializeField] private float delayBetweenBubbles = 0.3f;
    [SerializeField] private float allBubblesDisplayTime = 2.0f;
    [SerializeField] private bool autoAdvance = true;

    [Header("🔊 Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bubblePopSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    [Header("Settings")]
    [SerializeField] private string sceneToLoad = "MainMenu";
    [SerializeField] private bool quitGameInsteadOfLoadScene = false;

    private int currentDialog = 0;
    private Coroutine autoAdvanceCoroutine = null;

    void Start()
    {
        // כבה את BubbleMaster בהתחלה
        if (bubbleMaster != null)
        {
            bubbleMaster.SetActive(false);
        }

        // כבה את כל קומפוננטי ה-Animator
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
    }

    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }

    private void ShowCurrentDialog()
    {
        // הדלק את ה-Animator של הבועה הנוכחית
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
        StartCoroutine(EndGameCoroutine());
    }

    private IEnumerator EndGameCoroutine()
    {
        yield return new WaitForSeconds(0.3f);

        if (RewardedAdsManager.Instance != null)
        {
            bool adFinished = false;

            // בדוק אם הפרסומת מוכנה
            if (!RewardedAdsManager.Instance.IsReady())
            {
                Debug.LogWarning("[EndingDialogController] Ad not ready, skipping");
                adFinished = true;
            }
            else
            {
                RewardedAdsManager.Instance.ShowRewarded(
                    onClosed: (completed) => { adFinished = true; },
                    onFailed: (error) => { adFinished = true; }
                );

                float timeout = 60f;
                float elapsed = 0f;

                while (!adFinished && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (elapsed >= timeout)
                {
                    Debug.LogWarning("[EndingDialogController] Ad timeout!");
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        if (quitGameInsteadOfLoadScene)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    public void StartEndingDialog()
    {
        currentDialog = 0;

        // הדלק את BubbleMaster
        if (bubbleMaster != null)
        {
            bubbleMaster.SetActive(true);
            Debug.Log("[EndingDialogController] ✅ BubbleMaster activated");
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
        for (int i = 0; i < imageAnimators.Length; i++)
        {
            currentDialog = i;
            ShowCurrentDialog();
            yield return new WaitForSeconds(delayBetweenBubbles);
        }

        yield return new WaitForSeconds(allBubblesDisplayTime);
        yield return new WaitForSeconds(0.5f);
        EndGame();
    }
}
