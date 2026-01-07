using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// מנהל תאימות מכשירים - מזהה את סוג המכשיר ומתאים הגדרות בהתאם
/// פותר בעיות ספציפיות למכשירי Xiaomi ומכשירים אחרים
/// </summary>
public class DeviceCompatibilityManager : MonoBehaviour
{
    [Header("Device Detection")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool forceDeviceType = false;
    [SerializeField] private DeviceType forcedType = DeviceType.Generic;

    [Header("DPI Settings")]
    [SerializeField] private float baseDPI = 160f; // Android baseline DPI
    [SerializeField] private float dpiScaleFactor = 1f;

    [Header("Touch Settings")]
    [SerializeField] private float touchSensitivityMultiplier = 1f;
    [SerializeField] private int touchDragThresholdPixels = 10;

    [Header("Performance Settings")]
    [SerializeField] private bool enablePerformanceMode = false;
    [SerializeField] private int targetFrameRate = 60;

    public static DeviceCompatibilityManager Instance { get; private set; }

    public enum DeviceType
    {
        Generic,
        Xiaomi,
        Samsung,
        Huawei,
        LowEnd,
        HighEnd
    }

    private DeviceType currentDeviceType;
    private float screenDPI;
    private bool isInitialized = false;

    // Events
    public System.Action<DeviceType> OnDeviceDetected;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDevice();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ApplyDeviceSettings();
        ConfigureEventSystem();
    }

    private void InitializeDevice()
    {
        // קבל את ה-DPI של המסך
        screenDPI = Screen.dpi;
        if (screenDPI == 0)
        {
            screenDPI = baseDPI; // fallback
            Debug.LogWarning($"[DeviceCompatibility] Screen.dpi returned 0, using base DPI: {baseDPI}");
        }

        // חשב DPI scale factor
        dpiScaleFactor = screenDPI / baseDPI;

        // זהה את סוג המכשיר
        if (forceDeviceType)
        {
            currentDeviceType = forcedType;
            Debug.Log($"[DeviceCompatibility] Forced device type: {currentDeviceType}");
        }
        else
        {
            currentDeviceType = DetectDeviceType();
        }

        isInitialized = true;

        if (debugMode)
        {
            LogDeviceInfo();
        }

        OnDeviceDetected?.Invoke(currentDeviceType);
    }

    private DeviceType DetectDeviceType()
    {
        string deviceModel = SystemInfo.deviceModel.ToLower();
        string deviceName = SystemInfo.deviceName.ToLower();

        if (debugMode)
        {
            Debug.Log($"[DeviceCompatibility] Detecting device...");
            Debug.Log($"[DeviceCompatibility] Model: {SystemInfo.deviceModel}");
            Debug.Log($"[DeviceCompatibility] Name: {SystemInfo.deviceName}");
        }

        // זיהוי Xiaomi
        if (deviceModel.Contains("xiaomi") ||
            deviceModel.Contains("redmi") ||
            deviceModel.Contains("poco") ||
            deviceModel.Contains("mi ") ||
            deviceName.Contains("xiaomi") ||
            deviceName.Contains("redmi"))
        {
            Debug.Log($"[DeviceCompatibility] ⚠️ Xiaomi device detected!");
            return DeviceType.Xiaomi;
        }

        // זיהוי Samsung
        if (deviceModel.Contains("samsung") || deviceModel.Contains("galaxy"))
        {
            return DeviceType.Samsung;
        }

        // זיהוי Huawei
        if (deviceModel.Contains("huawei") || deviceModel.Contains("honor"))
        {
            return DeviceType.Huawei;
        }

        // זיהוי לפי ביצועים
        int systemMemory = SystemInfo.systemMemorySize;
        int processorCount = SystemInfo.processorCount;

        if (systemMemory < 3000 || processorCount < 4)
        {
            return DeviceType.LowEnd;
        }
        else if (systemMemory > 6000 && processorCount >= 8)
        {
            return DeviceType.HighEnd;
        }

        return DeviceType.Generic;
    }

    private void ApplyDeviceSettings()
    {
        switch (currentDeviceType)
        {
            case DeviceType.Xiaomi:
                ApplyXiaomiSettings();
                break;

            case DeviceType.Samsung:
                ApplySamsungSettings();
                break;

            case DeviceType.LowEnd:
                ApplyLowEndSettings();
                break;

            case DeviceType.HighEnd:
                ApplyHighEndSettings();
                break;

            default:
                ApplyGenericSettings();
                break;
        }

        // הגדרות frame rate
        Application.targetFrameRate = targetFrameRate;
    }

    private void ApplyXiaomiSettings()
    {
        Debug.Log("[DeviceCompatibility] 🔧 Applying Xiaomi-specific settings");

        // Xiaomi devices need higher touch sensitivity due to MIUI optimizations
        touchSensitivityMultiplier = 1.5f;

        // MIUI has aggressive touch handling, increase drag threshold
        touchDragThresholdPixels = 15;

        // MIUI often throttles apps, request sustained performance
        enablePerformanceMode = true;

        // Xiaomi devices benefit from slightly lower frame rate for battery
        targetFrameRate = 60;

        Debug.Log($"[DeviceCompatibility] ✅ Xiaomi settings applied:");
        Debug.Log($"  - Touch sensitivity: {touchSensitivityMultiplier}x");
        Debug.Log($"  - Drag threshold: {touchDragThresholdPixels}px");
    }

    private void ApplySamsungSettings()
    {
        Debug.Log("[DeviceCompatibility] 🔧 Applying Samsung settings");
        touchSensitivityMultiplier = 1.0f;
        touchDragThresholdPixels = 10;
        targetFrameRate = 60;
    }

    private void ApplyLowEndSettings()
    {
        Debug.Log("[DeviceCompatibility] 🔧 Applying Low-End device settings");
        enablePerformanceMode = false;
        targetFrameRate = 30; // Lower frame rate for better performance
        touchSensitivityMultiplier = 1.2f;
    }

    private void ApplyHighEndSettings()
    {
        Debug.Log("[DeviceCompatibility] 🔧 Applying High-End device settings");
        enablePerformanceMode = false;
        targetFrameRate = 60;
        touchSensitivityMultiplier = 1.0f;
    }

    private void ApplyGenericSettings()
    {
        Debug.Log("[DeviceCompatibility] 🔧 Applying Generic settings");
        touchSensitivityMultiplier = 1.0f;
        touchDragThresholdPixels = 10;
        targetFrameRate = 60;
    }

    private void ConfigureEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            var inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule != null)
            {
                // התאם את drag threshold של EventSystem בהתאם למכשיר
                int adjustedThreshold = Mathf.RoundToInt(touchDragThresholdPixels * dpiScaleFactor);

                // Note: Unity's StandaloneInputModule doesn't expose drag threshold in older versions
                // This is handled in DraggableButton instead

                if (debugMode)
                {
                    Debug.Log($"[DeviceCompatibility] EventSystem configured");
                    Debug.Log($"[DeviceCompatibility] Adjusted drag threshold: {adjustedThreshold}px");
                }
            }
        }
    }

    /// <summary>
    /// המר ערך פיקסלים קבוע לערך מותאם DPI
    /// </summary>
    public float ConvertPixelsToDPI(float pixels)
    {
        return pixels * dpiScaleFactor;
    }

    /// <summary>
    /// קבל drag threshold מותאם למכשיר
    /// </summary>
    public float GetAdjustedDragThreshold(float baseThreshold)
    {
        float adjusted = baseThreshold * dpiScaleFactor * touchSensitivityMultiplier;

        if (debugMode)
        {
            Debug.Log($"[DeviceCompatibility] Drag threshold: {baseThreshold} → {adjusted}");
            Debug.Log($"  - DPI scale: {dpiScaleFactor}");
            Debug.Log($"  - Touch multiplier: {touchSensitivityMultiplier}");
        }

        return adjusted;
    }

    /// <summary>
    /// קבל drop distance threshold מותאם למכשיר
    /// </summary>
    public float GetAdjustedDropThreshold(float baseThreshold)
    {
        return baseThreshold * dpiScaleFactor;
    }

    /// <summary>
    /// בדוק אם המכשיר צריך אופטימיזציות ביצועים
    /// </summary>
    public bool ShouldReduceEffects()
    {
        return currentDeviceType == DeviceType.LowEnd || enablePerformanceMode;
    }

    /// <summary>
    /// קבל מכפיל מהירות אנימציות
    /// </summary>
    public float GetAnimationSpeedMultiplier()
    {
        switch (currentDeviceType)
        {
            case DeviceType.LowEnd:
                return 1.5f; // מהיר יותר על מכשירים חלשים
            case DeviceType.Xiaomi:
                return 1.2f; // קצת יותר מהיר על Xiaomi
            default:
                return 1.0f;
        }
    }

    public DeviceType GetDeviceType()
    {
        return currentDeviceType;
    }

    public float GetDPIScaleFactor()
    {
        return dpiScaleFactor;
    }

    public float GetScreenDPI()
    {
        return screenDPI;
    }

    private void LogDeviceInfo()
    {
        Debug.Log("========================================");
        Debug.Log("=== DEVICE COMPATIBILITY INFO ===");
        Debug.Log("========================================");
        Debug.Log($"Device Type: {currentDeviceType}");
        Debug.Log($"Device Model: {SystemInfo.deviceModel}");
        Debug.Log($"Device Name: {SystemInfo.deviceName}");
        Debug.Log($"OS: {SystemInfo.operatingSystem}");
        Debug.Log($"Screen Resolution: {Screen.width}x{Screen.height}");
        Debug.Log($"Screen DPI: {screenDPI}");
        Debug.Log($"DPI Scale Factor: {dpiScaleFactor:F2}x");
        Debug.Log($"Memory: {SystemInfo.systemMemorySize} MB");
        Debug.Log($"Processor: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
        Debug.Log($"Graphics: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"Touch Sensitivity: {touchSensitivityMultiplier}x");
        Debug.Log($"Touch Drag Threshold: {touchDragThresholdPixels}px");
        Debug.Log($"Target Frame Rate: {targetFrameRate} FPS");
        Debug.Log("========================================");
    }

    /// <summary>
    /// בדיקת תאימות - קרא מ-Inspector או קוד
    /// </summary>
    [ContextMenu("Test Device Detection")]
    public void TestDeviceDetection()
    {
        LogDeviceInfo();
    }

    /// <summary>
    /// אפס הגדרות למצב ברירת מחדל
    /// </summary>
    [ContextMenu("Reset To Default Settings")]
    public void ResetToDefault()
    {
        forceDeviceType = false;
        ApplyDeviceSettings();
        Debug.Log("[DeviceCompatibility] Reset to default settings");
    }
}
