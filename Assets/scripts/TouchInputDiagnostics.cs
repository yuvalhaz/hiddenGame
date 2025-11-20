using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// סקריפט אבחון ותיקון אוטומטי של בעיות מגע ב-Unity
/// הוסף את הסקריפט לכל GameObject בסצנה והוא יבדוק ויתקן בעיות נפוצות
/// </summary>
public class TouchInputDiagnostics : MonoBehaviour
{
    [Header("Auto-Fix Settings")]
    [SerializeField] private bool autoFixOnStart = true;
    [SerializeField] private bool showDetailedLogs = true;

    [Header("Status (Runtime)")]
    [SerializeField] private bool eventSystemExists = false;
    [SerializeField] private bool canvasConfigured = false;
    [SerializeField] private bool graphicRaycasterExists = false;

    void Start()
    {
        if (autoFixOnStart)
        {
            RunDiagnostics();
        }
    }

    [ContextMenu("🔍 Run Touch Input Diagnostics")]
    public void RunDiagnostics()
    {
        Debug.Log("========================================");
        Debug.Log("=== TOUCH INPUT DIAGNOSTICS START ===");
        Debug.Log("========================================");

        CheckAndFixEventSystem();
        CheckAndFixCanvas();
        CheckDraggableButtons();
        CheckDropSpots();

        Debug.Log("========================================");
        Debug.Log("=== TOUCH INPUT DIAGNOSTICS END ===");
        Debug.Log("========================================");

        if (eventSystemExists && canvasConfigured && graphicRaycasterExists)
        {
            Debug.Log("✅ ALL CHECKS PASSED - Touch input should work!");
        }
        else
        {
            Debug.LogWarning("⚠️ Some issues were found. Check the logs above.");
        }
    }

    private void CheckAndFixEventSystem()
    {
        Debug.Log("--- Checking EventSystem ---");

        EventSystem eventSystem = FindObjectOfType<EventSystem>();

        if (eventSystem == null)
        {
            Debug.LogWarning("❌ No EventSystem found in scene!");
            Debug.Log("🔧 Creating EventSystem...");

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();

            Debug.Log("✅ EventSystem created!");
            eventSystemExists = true;
        }
        else
        {
            Debug.Log("✅ EventSystem exists");
            eventSystemExists = true;

            // בדוק שיש InputModule
            StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule == null)
            {
                Debug.LogWarning("⚠️ No StandaloneInputModule found!");
                Debug.Log("🔧 Adding StandaloneInputModule...");
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("✅ StandaloneInputModule added!");
            }
            else
            {
                Debug.Log("✅ StandaloneInputModule exists");
            }
        }
    }

    private void CheckAndFixCanvas()
    {
        Debug.Log("--- Checking Canvas ---");

        Canvas[] canvases = FindObjectsOfType<Canvas>();

        if (canvases.Length == 0)
        {
            Debug.LogError("❌ No Canvas found in scene!");
            canvasConfigured = false;
            return;
        }

        Debug.Log($"Found {canvases.Length} Canvas(es)");

        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"\n🎨 Checking Canvas: {canvas.name}");
            Debug.Log($"   - RenderMode: {canvas.renderMode}");
            Debug.Log($"   - Sorting Order: {canvas.sortingOrder}");

            // בדוק GraphicRaycaster
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogWarning($"⚠️ Canvas '{canvas.name}' missing GraphicRaycaster!");
                Debug.Log("🔧 Adding GraphicRaycaster...");
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("✅ GraphicRaycaster added!");
                graphicRaycasterExists = true;
            }
            else
            {
                Debug.Log("✅ GraphicRaycaster exists");
                Debug.Log($"   - Ignore Reversed Graphics: {raycaster.ignoreReversedGraphics}");
                Debug.Log($"   - Blocking Objects: {raycaster.blockingObjects}");
                graphicRaycasterExists = true;
            }

            // בדוק CanvasScaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Debug.Log($"✅ CanvasScaler exists");
                Debug.Log($"   - UI Scale Mode: {scaler.uiScaleMode}");
                Debug.Log($"   - Reference Resolution: {scaler.referenceResolution}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Canvas '{canvas.name}' missing CanvasScaler - UI might not scale properly");
            }

            canvasConfigured = true;
        }
    }

    private void CheckDraggableButtons()
    {
        Debug.Log("\n--- Checking DraggableButtons ---");

        DraggableButton[] buttons = FindObjectsOfType<DraggableButton>(true);
        Debug.Log($"Found {buttons.Length} DraggableButton(s)");

        if (buttons.Length == 0)
        {
            Debug.LogWarning("⚠️ No DraggableButtons found in scene!");
            return;
        }

        int issueCount = 0;

        foreach (DraggableButton button in buttons)
        {
            if (!button.gameObject.activeInHierarchy)
            {
                if (showDetailedLogs)
                    Debug.Log($"⏸️ Button '{button.name}' is inactive (buttonID: {button.GetButtonID()})");
                continue;
            }

            // בדוק Image component
            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogWarning($"❌ Button '{button.name}' missing Image component!");
                issueCount++;
            }
            else if (!image.raycastTarget)
            {
                Debug.LogWarning($"⚠️ Button '{button.name}' - Image.raycastTarget is FALSE! Touch won't work!");
                Debug.Log($"🔧 Setting raycastTarget to TRUE...");
                image.raycastTarget = true;
                Debug.Log("✅ Fixed!");
                issueCount++;
            }

            // בדוק CanvasGroup
            CanvasGroup cg = button.GetComponent<CanvasGroup>();
            if (cg != null && cg.blocksRaycasts == false)
            {
                Debug.LogWarning($"⚠️ Button '{button.name}' - CanvasGroup.blocksRaycasts is FALSE!");
                Debug.Log($"🔧 Setting blocksRaycasts to TRUE...");
                cg.blocksRaycasts = true;
                Debug.Log("✅ Fixed!");
            }

            if (showDetailedLogs)
                Debug.Log($"✅ Button '{button.name}' (ID: {button.GetButtonID()}) looks good");
        }

        if (issueCount == 0)
        {
            Debug.Log("✅ All DraggableButtons configured correctly!");
        }
        else
        {
            Debug.Log($"⚠️ Fixed {issueCount} issue(s) with DraggableButtons");
        }
    }

    private void CheckDropSpots()
    {
        Debug.Log("\n--- Checking DropSpots ---");

        DropSpot[] spots = FindObjectsOfType<DropSpot>(true);
        Debug.Log($"Found {spots.Length} DropSpot(s)");

        if (spots.Length == 0)
        {
            Debug.LogWarning("⚠️ No DropSpots found in scene!");
            return;
        }

        int issueCount = 0;

        foreach (DropSpot spot in spots)
        {
            if (string.IsNullOrEmpty(spot.spotId))
            {
                Debug.LogError($"❌ DropSpot '{spot.name}' has empty spotId!");
                issueCount++;
            }

            // בדוק ImageRevealController
            ImageRevealController reveal = spot.GetComponent<ImageRevealController>();
            if (reveal == null)
            {
                Debug.LogWarning($"⚠️ DropSpot '{spot.name}' (ID: {spot.spotId}) missing ImageRevealController!");
                issueCount++;
            }

            if (showDetailedLogs)
                Debug.Log($"✅ DropSpot '{spot.name}' (ID: {spot.spotId}) - IsSettled: {spot.IsSettled}");
        }

        if (issueCount == 0)
        {
            Debug.Log("✅ All DropSpots configured correctly!");
        }
        else
        {
            Debug.Log($"⚠️ Found {issueCount} issue(s) with DropSpots");
        }
    }

    [ContextMenu("🔬 Test Touch Input")]
    public void TestTouchInput()
    {
        Debug.Log("========================================");
        Debug.Log("=== TESTING TOUCH INPUT ===");
        Debug.Log("========================================");

        if (Input.touchSupported)
        {
            Debug.Log("✅ Device supports touch input");
        }
        else
        {
            Debug.Log("⚠️ Device does not support touch (using mouse instead)");
        }

        Debug.Log($"Touch count: {Input.touchCount}");
        Debug.Log($"Mouse position: {Input.mousePosition}");
        Debug.Log($"Mouse button down: {Input.GetMouseButton(0)}");

        if (EventSystem.current != null)
        {
            Debug.Log($"✅ EventSystem.current exists");
            Debug.Log($"   - Is Pointer Over GameObject: {EventSystem.current.IsPointerOverGameObject()}");

            // בדוק את ה-Raycasts
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            Debug.Log($"   - Raycast results: {results.Count}");
            foreach (var result in results)
            {
                Debug.Log($"      * Hit: {result.gameObject.name}");
            }
        }
        else
        {
            Debug.LogError("❌ EventSystem.current is NULL!");
        }

        Debug.Log("========================================");
    }

    void Update()
    {
        // אופציונלי: הצג לוג כשיש מגע
        if (Input.touchCount > 0 && showDetailedLogs)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log($"👆 Touch detected at: {touch.position}");
            }
        }

        // או עבור עכבר (לבדיקות בעורך)
        if (Input.GetMouseButtonDown(0) && showDetailedLogs)
        {
            Debug.Log($"🖱️ Mouse click at: {Input.mousePosition}");
        }
    }
}
