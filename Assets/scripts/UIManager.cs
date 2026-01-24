using UnityEngine;

/// <summary>
/// Manages all UI panels in the game
/// Ensures only one panel is open at a time (Settings or Hint Dialog)
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Try to find existing instance in scene
                instance = FindObjectOfType<UIManager>();

                if (instance == null)
                {
                    // Create new instance
                    GameObject go = new GameObject("UIManager");
                    instance = go.AddComponent<UIManager>();
                }
            }
            return instance;
        }
    }

    // Track which panel is currently open
    private enum OpenPanel
    {
        None,
        Settings,
        HintDialog
    }

    private OpenPanel currentOpenPanel = OpenPanel.None;

    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Request to open Settings panel
    /// Returns true if allowed, false if another panel is open
    /// </summary>
    public bool RequestOpenSettings()
    {
        Debug.Log($"[UIManager] RequestOpenSettings - Current panel: {currentOpenPanel}");

        if (currentOpenPanel == OpenPanel.HintDialog)
        {
            Debug.LogWarning("[UIManager] ❌ Cannot open Settings - Hint Dialog is open");
            return false;
        }

        if (currentOpenPanel == OpenPanel.Settings)
        {
            // Already open, allow (for toggle behavior)
            Debug.Log("[UIManager] Settings already open - allowing");
            return true;
        }

        currentOpenPanel = OpenPanel.Settings;
        Debug.Log("[UIManager] ✅ Settings panel opened - State updated");
        return true;
    }

    /// <summary>
    /// Request to open Hint Dialog
    /// Returns true if allowed, false if another panel is open
    /// </summary>
    public bool RequestOpenHintDialog()
    {
        Debug.Log($"[UIManager] RequestOpenHintDialog - Current panel: {currentOpenPanel}");

        if (currentOpenPanel == OpenPanel.Settings)
        {
            Debug.LogWarning("[UIManager] ❌ Cannot open Hint Dialog - Settings is open");
            return false;
        }

        if (currentOpenPanel == OpenPanel.HintDialog)
        {
            // Already open, allow (for toggle behavior)
            Debug.Log("[UIManager] Hint Dialog already open - allowing");
            return true;
        }

        currentOpenPanel = OpenPanel.HintDialog;
        Debug.Log("[UIManager] ✅ Hint Dialog opened - State updated");
        return true;
    }

    /// <summary>
    /// Notify that Settings panel was closed
    /// </summary>
    public void NotifySettingsClosed()
    {
        Debug.Log($"[UIManager] NotifySettingsClosed called - Current panel: {currentOpenPanel}");

        if (currentOpenPanel == OpenPanel.Settings)
        {
            currentOpenPanel = OpenPanel.None;
            Debug.Log("[UIManager] ✅ Settings panel closed - State reset to None");
        }
        else
        {
            Debug.LogWarning($"[UIManager] ⚠️ NotifySettingsClosed called but current panel is: {currentOpenPanel}");
        }
    }

    /// <summary>
    /// Notify that Hint Dialog was closed
    /// </summary>
    public void NotifyHintDialogClosed()
    {
        Debug.Log($"[UIManager] NotifyHintDialogClosed called - Current panel: {currentOpenPanel}");

        if (currentOpenPanel == OpenPanel.HintDialog)
        {
            currentOpenPanel = OpenPanel.None;
            Debug.Log("[UIManager] ✅ Hint Dialog closed - State reset to None");
        }
        else
        {
            Debug.LogWarning($"[UIManager] ⚠️ NotifyHintDialogClosed called but current panel is: {currentOpenPanel}");
        }
    }

    /// <summary>
    /// Close any open panel
    /// </summary>
    public void CloseAllPanels()
    {
        currentOpenPanel = OpenPanel.None;
        Debug.Log("[UIManager] All panels closed");
    }

    /// <summary>
    /// Check if Settings is currently open
    /// </summary>
    public bool IsSettingsOpen()
    {
        return currentOpenPanel == OpenPanel.Settings;
    }

    /// <summary>
    /// Check if Hint Dialog is currently open
    /// </summary>
    public bool IsHintDialogOpen()
    {
        return currentOpenPanel == OpenPanel.HintDialog;
    }

    /// <summary>
    /// Check if any panel is open
    /// </summary>
    public bool IsAnyPanelOpen()
    {
        return currentOpenPanel != OpenPanel.None;
    }
}
