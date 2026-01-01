using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Game startup loader - checks tutorial completion and loads appropriate scene
/// Attach this to a GameObject in your initial loader/startup scene
/// </summary>
public class GameLoader : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string tutorialSceneName = "Tutorial";
    [Tooltip("Scene name for the tutorial level (Level0)")]

    [SerializeField] private string levelSelectionSceneName = "LevelSelection";
    [Tooltip("Scene name for the level selection menu")]

    [Header("Settings")]
    [SerializeField] private float loadDelay = 0.5f;
    [Tooltip("Delay before loading next scene (for splash screen, etc.)")]

    [SerializeField] private bool debugMode = true;

    private void Start()
    {
        StartCoroutine(LoadAppropriateScene());
    }

    private IEnumerator LoadAppropriateScene()
    {
        // Optional delay for splash screen
        if (loadDelay > 0)
        {
            yield return new WaitForSeconds(loadDelay);
        }

        // Check if tutorial is completed
        string sceneToLoad = LevelCompleteController.GetStartupScene(tutorialSceneName, levelSelectionSceneName);

        if (debugMode)
        {
            Debug.Log($"[GameLoader] Loading scene: {sceneToLoad}");
        }

        // Load the appropriate scene
        SceneManager.LoadScene(sceneToLoad);
    }

    /// <summary>
    /// Call this to reset tutorial completion (for testing)
    /// </summary>
    [ContextMenu("🔄 Reset Tutorial Flag")]
    public void ResetTutorialCompletion()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 0);
        PlayerPrefs.Save();
        Debug.Log("[GameLoader] Tutorial completion flag reset!");
    }

    /// <summary>
    /// Call this to mark tutorial as completed (for testing)
    /// </summary>
    [ContextMenu("✅ Mark Tutorial Complete")]
    public void MarkTutorialComplete()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log("[GameLoader] Tutorial marked as completed!");
    }
}
