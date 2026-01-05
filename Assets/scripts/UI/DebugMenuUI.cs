using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug menu with buttons to reset progress, clear data, etc.
/// Only shows in Unity Editor and Development Builds
/// </summary>
public class DebugMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button resetFirstTimeButton;
    [SerializeField] private Button clearAllDataButton;
    [SerializeField] private Button showDebugInfoButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text statusText;
    
    [Header("Settings")]
    [SerializeField] private bool showOnlyInEditor = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;
    
    private void Start()
    {
        // Hide in release builds unless disabled
        #if !UNITY_EDITOR
        if (showOnlyInEditor && !Debug.isDebugBuild)
        {
            gameObject.SetActive(false);
            return;
        }
        #endif
        
        // Setup buttons
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);
        
        if (resetFirstTimeButton != null)
            resetFirstTimeButton.onClick.AddListener(OnResetFirstTime);
        
        if (clearAllDataButton != null)
            clearAllDataButton.onClick.AddListener(OnClearAllData);
        
        if (showDebugInfoButton != null)
            showDebugInfoButton.onClick.AddListener(OnShowDebugInfo);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(() => SetPanelActive(false));
        
        // Start with panel closed
        SetPanelActive(false);
        
        UpdateStatusText("Debug Menu Ready");
    }
    
    private void Update()
    {
        // Toggle with keyboard
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }
    
    private void TogglePanel()
    {
        if (debugPanel != null)
        {
            SetPanelActive(!debugPanel.activeSelf);
        }
    }
    
    private void SetPanelActive(bool active)
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(active);
        }
    }
    
    private void OnResetFirstTime()
    {
        PlayerPrefs.SetInt("IsFirstTime", 1);
        PlayerPrefs.Save();
        
        UpdateStatusText("✅ איפוס פעם ראשונה!\nההפעלה הבאה תציג טוטוריאל");
        Debug.Log("✅ <color=green>First Time Flag Reset!</color>");
    }
    
    private void OnClearAllData()
    {
        // Ask for confirmation
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        UpdateStatusText("✅ כל הנתונים נמחקו!\nכל ההתקדמות אופסה");
        Debug.Log("✅ <color=yellow>All PlayerPrefs cleared!</color>");
        
        // Also try to reset current level if GameProgressManager exists
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.ResetAllProgress();
        }
    }
    
    private void OnShowDebugInfo()
    {
        string info = "=== מידע דיבוג ===\n";
        info += $"פעם ראשונה: {PlayerPrefs.GetInt("IsFirstTime", 1)}\n";
        info += $"לבל נוכחי: {PlayerPrefs.GetInt("CurrentLevel", 0)}\n";
        
        // Show completed levels
        info += "\nלבלים שהושלמו:\n";
        bool foundAny = false;
        for (int i = 0; i <= 10; i++)
        {
            string key = $"Level_{i}_Completed";
            if (PlayerPrefs.HasKey(key) && PlayerPrefs.GetInt(key) == 1)
            {
                info += $"- Level {i} ✓\n";
                foundAny = true;
            }
        }
        
        if (!foundAny)
        {
            info += "אין לבלים שהושלמו\n";
        }
        
        info += "==================";
        
        UpdateStatusText(info);
        Debug.Log(info);
    }
    
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveAllListeners();
        
        if (resetFirstTimeButton != null)
            resetFirstTimeButton.onClick.RemoveAllListeners();
        
        if (clearAllDataButton != null)
            clearAllDataButton.onClick.RemoveAllListeners();
        
        if (showDebugInfoButton != null)
            showDebugInfoButton.onClick.RemoveAllListeners();
        
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
    }
}
