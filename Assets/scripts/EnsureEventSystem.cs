using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// מבטיח שיש EventSystem בסצנה - נדרש למגע ולעכבר
/// הוסף את הסקריפט לכל GameObject בסצנה (למשל על GameProgressManager)
/// </summary>
[DefaultExecutionOrder(-100)] // רץ לפני כולם
public class EnsureEventSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool createIfMissing = true;
    [SerializeField] private bool showDebugLogs = true;

    void Awake()
    {
        CheckEventSystem();
    }

    private void CheckEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();

        if (eventSystem == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ [EnsureEventSystem] No EventSystem found in scene!");

            if (createIfMissing)
            {
                if (showDebugLogs)
                    Debug.Log("🔧 [EnsureEventSystem] Creating EventSystem...");

                GameObject go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();

                if (showDebugLogs)
                    Debug.Log("✅ [EnsureEventSystem] EventSystem created successfully!");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("✅ [EnsureEventSystem] EventSystem already exists");

            // ודא שיש StandaloneInputModule
            StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning("⚠️ [EnsureEventSystem] No StandaloneInputModule - adding it...");

                eventSystem.gameObject.AddComponent<StandaloneInputModule>();

                if (showDebugLogs)
                    Debug.Log("✅ [EnsureEventSystem] StandaloneInputModule added!");
            }
        }
    }

    [ContextMenu("Force Check EventSystem")]
    public void ForceCheck()
    {
        CheckEventSystem();
    }
}
