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
        if (currentOpenPanel == OpenPanel.HintDialog)
        {
            Debug.Log("[UIManager] Cannot open Settings - Hint Dialog is open");
            return false;
        }

        if (currentOpenPanel == OpenPanel.Settings)
        {
            // Already open, allow (for toggle behavior)
            return true;
        }

        currentOpenPanel = OpenPanel.Settings;
        Debug.Log("[UIManager] Settings panel opened");
        return true;
    }

    /// <summary>
    /// Request to open Hint Dialog
    /// Returns true if allowed, false if another panel is open
    /// </summary>
    public bool RequestOpenHintDialog()
    {
        if (currentOpenPanel == OpenPanel.Settings)
        {
            Debug.Log("[UIManager] Cannot open Hint Dialog - Settings is open");
            return false;
        }

        if (currentOpenPanel == OpenPanel.HintDialog)
        {
            // Already open, allow (for toggle behavior)
            return true;
        }

        currentOpenPanel = OpenPanel.HintDialog;
        Debug.Log("[UIManager] Hint Dialog opened");
        return true;
    }

    /// <summary>
    /// Notify that Settings panel was closed
    /// </summary>
    public void NotifySettingsClosed()
    {
        if (currentOpenPanel == OpenPanel.Settings)
        {
            currentOpenPanel = OpenPanel.None;
            Debug.Log("[UIManager] Settings panel closed");
        }
    }

    /// <summary>
    /// Notify that Hint Dialog was closed
    /// </summary>
    public void NotifyHintDialogClosed()
    {
        if (currentOpenPanel == OpenPanel.HintDialog)
        {
            currentOpenPanel = OpenPanel.None;
            Debug.Log("[UIManager] Hint Dialog closed");
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
