using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingDialogController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject bubbleMaster;
    [SerializeField] private Button nextButton;

    [Header("Animation Settings")]
    [SerializeField] private float appearTime = 0.25f;
    [SerializeField] private float floatAmount = 8f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("Click Animation")]
    [SerializeField] private float clickScale = 0.8f;
    [SerializeField] private float clickTime = 0.1f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bubblePopSound;

    [Header("Settings")]
    [SerializeField] private string levelSelectionScene = "LevelSelection";

    [Header("Tutorial Mode")]
    [SerializeField] private bool isTutorialMode = false;
    [Tooltip("Enable this for Level0 - goes straight to LevelSelection")]

    private RectTransform rect;
    private Vector2 startPos;
    private Coroutine floatCoroutine;
    private bool isLoading = false;

    private Vector2 originalPosition;

    void Start()
    {
        if (bubbleMaster != null)
        {
            rect = bubbleMaster.GetComponent<RectTransform>();
            originalPosition = rect.anchoredPosition;
            rect.localScale = Vector3.zero;
            bubbleMaster.SetActive(false);
        }

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
    }

    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }

    public void StartEndingDialog()
    {
        StartCoroutine(ShowNextBubble());
    }

    private IEnumerator ShowNextBubble()
    {
        yield return new WaitForSeconds(0.4f);

        if (bubbleMaster == null || rect == null)
            yield break;

        // Reset transform BEFORE activating to avoid flicker
        rect.localScale = Vector3.one * 0.05f;
        rect.anchoredPosition = originalPosition;
        bubbleMaster.SetActive(true);

        PlayBubbleSound();

        yield return StartCoroutine(PopAnimation());

        startPos = rect.anchoredPosition;
        if (floatCoroutine != null)
            StopCoroutine(floatCoroutine);
        floatCoroutine = StartCoroutine(FloatAnimation());
    }

    private IEnumerator PopAnimation()
    {
        float t = 0f;
        Vector3 from = Vector3.one * 0.05f;
        Vector3 to = Vector3.one;

        while (t < appearTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / appearTime);
            rect.localScale = Vector3.Lerp(from, to, p);
            yield return null;
        }
        rect.localScale = Vector3.one;
    }

    private IEnumerator FloatAnimation()
    {
        while (true)
        {
            float y = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            rect.anchoredPosition = startPos + new Vector2(0f, y);
            yield return null;
        }
    }

    private void PlayBubbleSound()
    {
        if (bubblePopSound == null) return;
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
            audioSource.PlayOneShot(bubblePopSound);
    }

    private void OnNextClicked()
    {
        if (isLoading) return;
        StartCoroutine(ClickAndLoadNext());
    }

    private IEnumerator ClickAndLoadNext()
    {
        isLoading = true;

        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
            floatCoroutine = null;
        }

        // Click animation (only if rect exists)
        if (rect != null)
        {
            Vector3 originalScale = rect.localScale;
            Vector3 targetScale = originalScale * clickScale;
            float t = 0f;

            while (t < clickTime)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / clickTime);
                rect.localScale = Vector3.Lerp(originalScale, targetScale, p);
                yield return null;
            }
            rect.localScale = targetScale;
        }

        yield return new WaitForSeconds(0.05f);

        // Handle tutorial mode
        if (isTutorialMode)
        {
            PlayerPrefs.SetInt("IsFirstTime", 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene(levelSelectionScene);
            yield break;
        }

        // Use LevelManager for proper level progression
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadCurrentLevel();
        }
        else
        {
            SceneManager.LoadScene(levelSelectionScene);
        }
    }
}
