using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PlacedItemData
{
    public string itemId;
    public string spriteName;
}

[System.Serializable]
public class GameProgressData
{
    public List<PlacedItemData> placedItems = new List<PlacedItemData>();
    public int currentLevel = 0;
    public int totalItemsPlaced = 0;
    
    // For future features
    public float totalPlayTime = 0f;
    public System.DateTime lastPlayDate;
}

public class GameProgressManager : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveInterval = 10f; // seconds
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool resetOnStart = false; // For testing
    
    private GameProgressData progressData;
    private float autoSaveTimer = 0f;
    
    public static GameProgressManager Instance { get; private set; }
    
    // Events
    public System.Action<string> OnItemPlaced;
    public System.Action<string> OnItemRemoved;
    public System.Action OnProgressLoaded;
    public System.Action OnProgressSaved;

    private const string SAVE_KEY = "GameProgress";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeProgress();

            // ✅ הוסף את זה!
            if (!resetOnStart)
            {
                LoadProgress();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        if (resetOnStart)
        {
            ResetAllProgress();
        }
        else
        {
            // LoadProgress כבר רץ ב-Awake
            StartCoroutine(DelayedApplyProgress());
        }
    }


    void Update()
    {
        // ✅ לחץ R במקלדת כדי לאפס בזמן Play (לבדיקות)
        if (Input.GetKeyDown(KeyCode.R) && debugMode)
        {
            Debug.Log("🔄 R pressed - Resetting game!");
            ResetAllProgress();
        }

        if (autoSave)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                SaveProgress();
                autoSaveTimer = 0f;
            }
        }
    }


    [ContextMenu("📋 Show Saved Items")]
    private void ShowSavedItems()
    {
        Debug.Log("========================================");
        Debug.Log("=== SAVED ITEMS ===");
        Debug.Log("========================================");

        if (progressData == null || progressData.placedItems == null)
        {
            Debug.Log("❌ No saved data!");
            return;
        }

        Debug.Log($"Total items saved: {progressData.placedItems.Count}");

        foreach (var item in progressData.placedItems)
        {
            Debug.Log($"✅ Saved: {item.itemId}");
        }

        Debug.Log("========================================");

        // בדוק ספציפית את spot03
        bool spot03Saved = progressData.placedItems.Any(item => item.itemId == "spot03");
        Debug.Log($"🔍 Is spot03 saved? {(spot03Saved ? "YES ✅" : "NO ❌")}");

        Debug.Log("========================================");
    }


    private IEnumerator DelayedApplyProgress()
    {
        yield return null; // ✅ רק frame אחד מספיק!

        Debug.Log($"[GameProgressManager] === STARTING APPLY PROGRESS ===");
        ApplyProgressToScene();
        Debug.Log($"[GameProgressManager] === FINISHED APPLY PROGRESS ===");
    }


    private void InitializeProgress()
    {
        progressData = new GameProgressData();
    }

    private void LoadProgress()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            try
            {
                string jsonData = PlayerPrefs.GetString(SAVE_KEY);
                progressData = JsonUtility.FromJson<GameProgressData>(jsonData);
                
                if (debugMode)
                    Debug.Log($"[GameProgressManager] Progress loaded: {progressData.placedItems.Count} items placed, level {progressData.currentLevel}");
                    
                OnProgressLoaded?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameProgressManager] Failed to load progress: {e.Message}");
                InitializeProgress(); // Reset to default
            }
        }
        else
        {
            if (debugMode)
                Debug.Log("[GameProgressManager] No save data found, starting fresh");
            InitializeProgress();
        }
    }

    private void SaveProgress()
    {
        try
        {
            progressData.lastPlayDate = System.DateTime.Now;
            string jsonData = JsonUtility.ToJson(progressData, true);
            PlayerPrefs.SetString(SAVE_KEY, jsonData);
            PlayerPrefs.Save();
            
            if (debugMode)
                Debug.Log($"[GameProgressManager] Progress saved: {progressData.placedItems.Count} items");
                
            OnProgressSaved?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameProgressManager] Failed to save progress: {e.Message}");
        }
    }

    public void MarkItemAsPlaced(string itemId, Sprite itemSprite = null)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("[GameProgressManager] Cannot mark item as placed - itemId is null or empty!");
            return;
        }

        if (progressData == null)
        {
            Debug.LogWarning("[GameProgressManager] progressData was null in MarkItemAsPlaced, initializing now");
            InitializeProgress();
        }

        if (progressData.placedItems == null)
        {
            Debug.LogWarning("[GameProgressManager] placedItems was null, initializing...");
            progressData.placedItems = new List<PlacedItemData>();
        }

        // Check if already exists
        if (progressData.placedItems.Any(item => item.itemId == itemId))
        {
            if (debugMode)
                Debug.Log($"[GameProgressManager] Item {itemId} already marked as placed");
            return;
        }

        var placedItem = new PlacedItemData
        {
            itemId = itemId,
            spriteName = itemSprite ? itemSprite.name : ""
        };

        progressData.placedItems.Add(placedItem);
        progressData.totalItemsPlaced++;

        if (debugMode)
            Debug.Log($"[GameProgressManager] Item marked as placed: {itemId}");

        try
        {
            OnItemPlaced?.Invoke(itemId);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GameProgressManager] OnItemPlaced event error: {e.Message}");
        }

        if (autoSave)
            SaveProgress();
    }

    public void RemoveItemPlacement(string itemId)
    {
        if (progressData == null || progressData.placedItems == null)
        {
            Debug.LogWarning("[GameProgressManager] Cannot remove item - progressData not initialized!");
            return;
        }

        var itemToRemove = progressData.placedItems.FirstOrDefault(item => item.itemId == itemId);
        if (itemToRemove != null)
        {
            progressData.placedItems.Remove(itemToRemove);

            if (debugMode)
                Debug.Log($"[GameProgressManager] Item placement removed: {itemId}");

            OnItemRemoved?.Invoke(itemId);

            if (autoSave)
                SaveProgress();
        }
    }

    public bool IsItemPlaced(string itemId)
    {
        // Initialize progressData if null (can happen during OnValidate before Awake)
        if (progressData == null)
        {
            if (debugMode)
                Debug.LogWarning("[GameProgressManager] IsItemPlaced called but progressData is null! Initializing now.");
            InitializeProgress();
        }

        if (progressData.placedItems == null)
        {
            if (debugMode)
                Debug.LogWarning("[GameProgressManager] IsItemPlaced called but placedItems list is null! Initializing now.");
            progressData.placedItems = new List<PlacedItemData>();
            return false;
        }

        if (string.IsNullOrEmpty(itemId))
        {
            if (debugMode)
                Debug.LogWarning("[GameProgressManager] IsItemPlaced called with null or empty itemId!");
            return false;
        }

        return progressData.placedItems.Any(item => item.itemId == itemId);
    }

    // ✅ UPDATED: Works with DraggableButton instead of SimpleDragFromBar
    private void ApplyProgressToScene()
    {
        Debug.Log($"=== APPLY PROGRESS START ===");
        Debug.Log($"[GameProgressManager] Items to restore: {progressData.placedItems.Count}");

        if (progressData.placedItems.Count == 0)
        {
            Debug.Log("[GameProgressManager] No items to restore");
            return;
        }

        DraggableButton[] allDragButtons = FindObjectsOfType<DraggableButton>(true);
        DropSpot[] allDropSpots = FindObjectsOfType<DropSpot>(true);

        Debug.Log($"[GameProgressManager] Found {allDragButtons.Length} buttons, {allDropSpots.Length} spots");

        // ✅ שמור את כל ה-bars
        var allBars = new System.Collections.Generic.HashSet<ScrollableButtonBar>();

        foreach (var placedItem in progressData.placedItems)
        {
            Debug.Log($"[GameProgressManager] 🔄 Restoring: {placedItem.itemId}");

            DraggableButton dragButton = System.Array.Find(allDragButtons, btn => btn.GetButtonID() == placedItem.itemId);
            DropSpot dropSpot = System.Array.Find(allDropSpots, spot => spot.spotId == placedItem.itemId);

            if (dropSpot != null)
            {
                Debug.Log($"[GameProgressManager] ✅ Found spot: {placedItem.itemId}");

                if (dragButton != null)
                {
                    // ✅ שמור את ה-bar לפני המחיקה
                    var bar = dragButton.GetComponentInParent<ScrollableButtonBar>();
                    if (bar != null)
                    {
                        allBars.Add(bar);
                    }

                    Debug.Log($"[GameProgressManager] 🗑️ Destroying button: {placedItem.itemId}");
                    Destroy(dragButton.gameObject);
                }

                dropSpot.IsSettled = true;

                var revealController = dropSpot.GetComponent<ImageRevealController>();
                if (revealController != null)
                {
                    revealController.RevealInstant();
                    Debug.Log($"[GameProgressManager] ✅ Revealed: {placedItem.itemId}");
                }
            }
            else
            {
                Debug.LogWarning($"[GameProgressManager] ❌ Spot not found: {placedItem.itemId}");
            }
        }

        // ✅ עדכן את כל ה-bars
        if (allBars.Count > 0)
        {
            StartCoroutine(RefreshAllBars(allBars));
        }

        Debug.Log($"=== APPLY PROGRESS END ===");
    }

    [ContextMenu("🔴 FULL RESET - Delete Everything")]
    private void FullResetEverything()
    {
        Debug.Log("=== FULL RESET START ===");

        // 1. מחק את כל השמירה
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("✅ PlayerPrefs deleted");

        // 2. אפס את המידע בזיכרון
        progressData = new GameProgressData();
        Debug.Log("✅ Progress data reset");

        // 3. אפס את כל ה-DropSpots
        DropSpot[] allDropSpots = FindObjectsOfType<DropSpot>(true);
        foreach (DropSpot spot in allDropSpots)
        {
            spot.IsSettled = false;

            var revealController = spot.GetComponent<ImageRevealController>();
            if (revealController != null)
            {
                revealController.ResetReveal();
            }
        }
        Debug.Log($"✅ Reset {allDropSpots.Length} DropSpots");

        // 4. הצג את כל הכפתורים
        DraggableButton[] allButtons = FindObjectsOfType<DraggableButton>(true);
        foreach (DraggableButton btn in allButtons)
        {
            btn.gameObject.SetActive(true);
        }
        Debug.Log($"✅ Showed {allButtons.Length} buttons");

        Debug.Log("=== ✅ FULL RESET COMPLETE ===");
        Debug.Log("⚠️ Press PLAY to see the changes!");
    }

    // ✅ פונקציה חדשה
    private IEnumerator RefreshAllBars(System.Collections.Generic.HashSet<ScrollableButtonBar> bars)
    {
        yield return null; // המתן frame אחד כדי שהכפתורים יימחקו

        Debug.Log($"[GameProgressManager] Refreshing {bars.Count} button bars");

        foreach (var bar in bars)
        {
            if (bar != null)
            {
                bar.RefreshBar();
            }
        }
    }



    // ✅ UPDATED: Works with DraggableButton
    private void ApplyItemPlacement(DraggableButton dragButton, DropSpot dropSpot)
    {
        Debug.Log($"[GameProgressManager] Applying placement for: {dropSpot.spotId}");

        // ✅ שמור את ה-ScrollableButtonBar לפני שמוחקים
        ScrollableButtonBar buttonBar = null;
        int buttonIndex = -1;

        if (dragButton != null)
        {
            // ✅ קבל את הפרטים לפני המחיקה
            buttonBar = dragButton.GetComponentInParent<ScrollableButtonBar>();

            // ✅ מצא את האינדקס של הכפתור
            if (buttonBar != null)
            {
                var allButtons = buttonBar.GetComponentsInChildren<DraggableButton>(true);
                for (int i = 0; i < allButtons.Length; i++)
                {
                    if (allButtons[i] == dragButton)
                    {
                        buttonIndex = i;
                        break;
                    }
                }
            }

            Debug.Log($"[GameProgressManager] Destroying button: {dragButton.GetButtonID()}");
            Destroy(dragButton.gameObject);
        }

        // ✅ עדכן את ה-bar אחרי המחיקה
        if (buttonBar != null && buttonIndex >= 0)
        {
            // המתן frame אחד כדי שהכפתור באמת יימחק
            StartCoroutine(UpdateBarAfterDestroy(buttonBar, buttonIndex));
        }

        // סמן את ה-spot כמושם
        dropSpot.IsSettled = true;

        // הצג את התמונה
        var revealController = dropSpot.GetComponent<ImageRevealController>();
        if (revealController != null)
        {
            revealController.RevealInstant();
            Debug.Log($"[GameProgressManager] ✅ Revealed: {dropSpot.spotId}");
        }
        else
        {
            Debug.LogWarning($"[GameProgressManager] No RevealController on {dropSpot.spotId}!");
        }
    }


    // ✅ פונקציה חדשה
    private IEnumerator UpdateBarAfterDestroy(ScrollableButtonBar bar, int index)
    {
        yield return null; // המתן frame אחד

        if (bar != null)
        {
            Debug.Log($"[GameProgressManager] Updating bar after button {index} destroyed");

            // ✅ סמן את הכפתור כלא פעיל ב-bar
            var barScript = bar.GetType();
            var buttonStatesField = barScript.GetField("buttonStates",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (buttonStatesField != null)
            {
                var buttonStates = (System.Collections.Generic.List<bool>)buttonStatesField.GetValue(bar);
                if (index < buttonStates.Count)
                {
                    buttonStates[index] = false;
                }
            }

            // ✅ קרא ל-RecalculateAllPositions
            var method = barScript.GetMethod("RecalculateAllPositions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                method.Invoke(bar, null);
                Debug.Log($"[GameProgressManager] ✅ Bar updated!");
            }
        }
    }


    public GameProgressData GetProgressData()
    {
        if (progressData == null)
        {
            InitializeProgress();
        }
        return progressData;
    }

    // ✅ UPDATED: Works with DraggableButton
    public void ResetAllProgress()
    {
        progressData = new GameProgressData();
        PlayerPrefs.DeleteKey(SAVE_KEY);
        
        if (debugMode)
            Debug.Log("[GameProgressManager] All progress reset");
            
        DropSpot[] allDropSpots = FindObjectsOfType<DropSpot>();
        foreach (DropSpot spot in allDropSpots)
        {
            spot.ResetSpot();
        }
        
        // ✅ Find DraggableButton instead of SimpleDragFromBar
        DraggableButton[] allDragButtons = FindObjectsOfType<DraggableButton>();
        foreach (DraggableButton button in allDragButtons)
        {
            button.gameObject.SetActive(true);
        }
    }

    public void ForceSave()
    {
        SaveProgress();
    }

    public int GetTotalItemsPlaced()
    {
        if (progressData == null)
        {
            InitializeProgress();
        }
        return progressData.totalItemsPlaced;
    }

    public int GetCurrentLevelItemsPlaced()
    {
        if (progressData == null)
        {
            InitializeProgress();
        }
        return progressData.placedItems?.Count ?? 0;
    }
}