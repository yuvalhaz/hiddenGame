using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private Text loadingText;      // "Loading..." - will blink
    [SerializeField] private Text percentText;      // "45%" - numbers only
    
    [Header("Settings")]
    [SerializeField] private float loadingDuration = 2f;
    [Tooltip("How long to show the loading screen")]
    
    [SerializeField] private float blinkSpeed = 0.5f;
    [Tooltip("How fast the 'Loading...' text blinks")]
    
    private const string FIRST_TIME_KEY = "IsFirstTime";
    
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
        // בדוק אם זו הפעם הראשונה
        bool isFirstTime = PlayerPrefs.GetInt(FIRST_TIME_KEY, 1) == 1;
        
        string targetScene;
        if (isFirstTime)
        {
            targetScene = "Level0";
            // ⚠️ DON'T mark as completed here! Only mark after tutorial is finished.
            Debug.Log("[LoadingManager] 🎓 First time launch - loading tutorial (Level0)");
        }
        else
        {
            targetScene = "LevelSelection";
            Debug.Log("[LoadingManager] 🔄 Returning user - loading level selection");
        }
        
        // אנימציית טעינה
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
        
        // וודא שהגענו ל-100%
        if (loadingSlider != null)
            loadingSlider.value = 1f;
        
        if (percentText != null)
            percentText.text = "100%";
        
        yield return new WaitForSeconds(0.2f);
        
        // טען את הסצנה
        Debug.Log($"[LoadingManager] Loading scene: {targetScene}");
        SceneManager.LoadScene(targetScene);
    }
    
    /// <summary>
    /// Reset first time flag for testing
    /// </summary>
    [ContextMenu("🔄 Reset First Time Flag")]
    public void ResetFirstTimeFlag()
    {
        PlayerPrefs.SetInt(FIRST_TIME_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("✅ First time flag reset! Next launch will show tutorial.");
    }
}
