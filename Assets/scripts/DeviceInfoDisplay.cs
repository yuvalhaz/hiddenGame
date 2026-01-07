using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// מציג מידע על המכשיר על המסך - שימושי לבדיקות
/// הוסף לסצנה עם Canvas + Text component
/// </summary>
public class DeviceInfoDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text displayText;
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private bool updateEveryFrame = false;

    [Header("Display Options")]
    [SerializeField] private bool showDeviceType = true;
    [SerializeField] private bool showDPI = true;
    [SerializeField] private bool showResolution = true;
    [SerializeField] private bool showPerformance = true;
    [SerializeField] private bool showThresholds = true;

    private bool isVisible = false;

    void Start()
    {
        if (displayText == null)
        {
            displayText = GetComponent<Text>();
        }

        if (showOnStart)
        {
            ShowDeviceInfo();
        }
        else
        {
            HideDeviceInfo();
        }
    }

    void Update()
    {
        // לחץ על D במקלדת כדי להציג/להסתיר
        if (Input.GetKeyDown(KeyCode.D))
        {
            ToggleDisplay();
        }

        // עדכון רציף אם מבוקש
        if (updateEveryFrame && isVisible)
        {
            UpdateDeviceInfo();
        }
    }

    public void ShowDeviceInfo()
    {
        isVisible = true;
        if (displayText != null)
        {
            displayText.gameObject.SetActive(true);
            UpdateDeviceInfo();
        }
    }

    public void HideDeviceInfo()
    {
        isVisible = false;
        if (displayText != null)
        {
            displayText.gameObject.SetActive(false);
        }
    }

    public void ToggleDisplay()
    {
        if (isVisible)
        {
            HideDeviceInfo();
        }
        else
        {
            ShowDeviceInfo();
        }
    }

    private void UpdateDeviceInfo()
    {
        if (displayText == null) return;

        string info = "=== DEVICE INFO ===\n";

        if (showDeviceType)
        {
            info += $"Model: {SystemInfo.deviceModel}\n";
            info += $"OS: {SystemInfo.operatingSystem}\n";

            if (DeviceCompatibilityManager.Instance != null)
            {
                var deviceType = DeviceCompatibilityManager.Instance.GetDeviceType();
                info += $"Type: {deviceType}\n";

                // סמן Xiaomi בצבע אדום אם אפשר
                if (deviceType == DeviceCompatibilityManager.DeviceType.Xiaomi)
                {
                    info += "<color=red>⚠️ XIAOMI DEVICE</color>\n";
                }
            }
            info += "\n";
        }

        if (showDPI)
        {
            info += $"Screen DPI: {Screen.dpi:F0}\n";
            if (DeviceCompatibilityManager.Instance != null)
            {
                float dpiScale = DeviceCompatibilityManager.Instance.GetDPIScaleFactor();
                info += $"DPI Scale: {dpiScale:F2}x\n";
            }
            info += "\n";
        }

        if (showResolution)
        {
            info += $"Resolution: {Screen.width}x{Screen.height}\n";
            info += $"Refresh Rate: {Screen.currentResolution.refreshRate}Hz\n";
            info += "\n";
        }

        if (showPerformance)
        {
            info += $"FPS: {(1f / Time.deltaTime):F0}\n";
            info += $"Memory: {SystemInfo.systemMemorySize}MB\n";
            info += $"CPU: {SystemInfo.processorCount} cores\n";
            info += "\n";
        }

        if (showThresholds && DeviceCompatibilityManager.Instance != null)
        {
            float baseDrag = 50f;
            float baseDrop = 150f;
            float adjustedDrag = DeviceCompatibilityManager.Instance.GetAdjustedDragThreshold(baseDrag);
            float adjustedDrop = DeviceCompatibilityManager.Instance.GetAdjustedDropThreshold(baseDrop);

            info += "=== THRESHOLDS ===\n";
            info += $"Drag: {baseDrag:F0} → {adjustedDrag:F0}px\n";
            info += $"Drop: {baseDrop:F0} → {adjustedDrop:F0}px\n";
            info += "\n";
        }

        info += "Press D to hide";

        displayText.text = info;
    }
}
