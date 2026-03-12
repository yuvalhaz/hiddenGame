using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingDialogController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Text buttonText;

    [Header("Animation Settings")]
    [SerializeField] private float buttonAppearDelay = 0.3f;
    [SerializeField] private float popInDuration = 0.4f;
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonAppearSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    [Header("Settings")]
    [SerializeField] private string levelSelectionScene = "LevelSelection";
    [SerializeField] private bool quitGameInsteadOfLoadScene = false;

    [Header("Tutorial Mode")]
    [SerializeField] private bool isTutorialMode = false;
    [Tooltip("Enable this for Level0 - goes straight to LevelSelection")]

    private Coroutine pulseCoroutine;
    private bool isTransitioning = false;

    void Start()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
            nextButton.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }

    public void StartEndingDialog()
    {
        Debug.Log("[EndingDialogController] StartEndingDialog called!");
        StartCoroutine(ShowNextButton());
    }

    private IEnumerator ShowNextButton()
    {
        yield return new WaitForSeconds(buttonAppearDelay);

        if (nextButton == null) yield break;

        nextButton.gameObject.SetActive(true);
        RectTransform rt = nextButton.GetComponent<RectTransform>();
        Vector3 originalScale = rt.localScale;
        rt.localScale = Vector3.zero;

        PlaySound(buttonAppearSound);

        // Pop-in with elastic overshoot
        float elapsed = 0f;
        while (elapsed < popInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popInDuration;
            float ease = 1f - Mathf.Pow(2f, -10f * t) * Mathf.Cos(t * Mathf.PI * 2.5f);
            rt.localScale = originalScale * Mathf.Max(0f, ease);
            yield return null;
        }
        rt.localScale = originalScale;

        // Idle pulse
        pulseCoroutine = StartCoroutine(PulseAnimation(rt, originalScale));
    }

    private IEnumerator PulseAnimation(RectTransform rt, Vector3 baseScale)
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime * pulseSpeed;
            float scale = 1f + (pulseScale - 1f) * (Mathf.Sin(time) * 0.5f + 0.5f);
            rt.localScale = baseScale * scale;
            yield return null;
        }
    }

    private void OnNextClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        Debug.Log("[EndingDialogController] Next button clicked!");

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        StartCoroutine(ClickAnimationThenProceed());
    }

    private IEnumerator ClickAnimationThenProceed()
    {
        if (nextButton != null)
        {
            RectTransform rt = nextButton.GetComponent<RectTransform>();
            Vector3 originalScale = rt.localScale;

            // Quick squash on click
            float duration = 0.15f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 1f - 0.2f * Mathf.Sin(t * Mathf.PI);
                rt.localScale = originalScale * scale;
                yield return null;
            }
            rt.localScale = originalScale;
        }

        yield return new WaitForSeconds(0.1f);

        if (isTutorialMode)
        {
            Debug.Log("[EndingDialogController] Tutorial completed - going to LevelSelection");
            PlayerPrefs.SetInt("IsFirstTime", 0);
            PlayerPrefs.Save();
        }

        if (quitGameInsteadOfLoadScene)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        else if (LevelManager.Instance != null && !isTutorialMode)
        {
            Debug.Log("[EndingDialogController] Loading next level...");
            LevelManager.Instance.LoadCurrentLevel();
        }
        else
        {
            Debug.Log("[EndingDialogController] Loading LevelSelection...");
            SceneManager.LoadScene(levelSelectionScene);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        audioSource.PlayOneShot(clip, soundVolume);
    }
}
