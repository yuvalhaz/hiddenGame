using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Loading screen manager - shows loading animation and redirects to tutorial or level selection
/// Attach this to a GameObject in your loading screen scene
/// </summary>
public class GameLoader : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private Text loadingText;      // "Loading..." - will blink
    [SerializeField] private Text percentText;      // "45%" - numbers only

    [Header("Scene Names")]
    [SerializeField] private string tutorialSceneName = "Level0";
    [Tooltip("Scene name for the tutorial level")]

    [SerializeField] private string levelSelectionSceneName = "LevelSelection";
    [Tooltip("Scene name for the level selection menu")]

    [Header("Settings")]
    [SerializeField] private float loadingDuration = 2f;
    [Tooltip("How long to show the loading screen")]

    [SerializeField] private float blinkSpeed = 0.5f;
    [Tooltip("How fast the 'Loading...' text blinks")]

    [SerializeField] private bool debugMode = true;

    private void Start()
    {
        // Start blinking "Loading..." text
        if (loadingText != null)
        {
            StartCoroutine(BlinkLoadingText());
        }

        StartCoroutine(LoadGame());
    }

    private IEnumerator BlinkLoadingText()
    {
        // Make sure text starts visible
        if (loadingText != null)
        {
            loadingText.enabled = true;
        }

        while (true)
        {
            yield return new WaitForSeconds(blinkSpeed);

            if (loadingText != null)
            {
                loadingText.enabled = !loadingText.enabled; // Toggle on/off
            }
        }
    }

    private IEnumerator LoadGame()
    {
        // Check if tutorial is completed
        string targetScene = LevelCompleteController.GetStartupScene(tutorialSceneName, levelSelectionSceneName);

        // Loading animation
        float elapsed = 0f;
        while (elapsed < loadingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / loadingDuration;

            // Update slider
            if (loadingSlider != null)
                loadingSlider.value = progress;

            // Update percentage text
            if (percentText != null)
                percentText.text = Mathf.RoundToInt(progress * 100) + "%";

            yield return null;
        }

        // Ensure we reach 100%
        if (loadingSlider != null)
            loadingSlider.value = 1f;

        if (percentText != null)
            percentText.text = "100%";

        yield return new WaitForSeconds(0.2f);

        // Load the scene
        if (debugMode)
        {
            Debug.Log($"[GameLoader] Loading scene: {targetScene}");
        }

        SceneManager.LoadScene(targetScene);
    }

    /// <summary>
    /// Call this to reset tutorial completion (for testing)
    /// </summary>
    [ContextMenu("🔄 Reset Tutorial Flag")]
    public void ResetTutorialCompletion()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 0);
        PlayerPrefs.Save();
        Debug.Log("[GameLoader] Tutorial completion flag reset! Next launch will show tutorial.");
    }

    /// <summary>
    /// Call this to mark tutorial as completed (for testing)
    /// </summary>
    [ContextMenu("✅ Mark Tutorial Complete")]
    public void MarkTutorialComplete()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log("[GameLoader] Tutorial marked as completed! Next launch will skip to level selection.");
    }
}
