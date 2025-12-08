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
    [Header("Level System")]
    [SerializeField] private LevelData currentLevelData;
    [Tooltip("Optional: Assign the current level's LevelData for per-level saving")]

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

    /// <summary>
    /// Get the save key for the current level (if levelData is assigned)
    /// </summary>
    private string GetCurrentSaveKey()
    {
        if (currentLevelData != null)
        {
            return currentLevelData.GetProgressKey();
        }
        return SAVE_KEY; // Fallback to global progress
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeProgress();

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
            StartCoroutine(DelayedApplyProgress());
        }
    }


    void Update()
    {
        // ✅ לחץ R במקלדת כדי לאפס בזמן Play (לבדיקות)
        if (Input.GetKeyDown(KeyCode.R) && debugMode)
        {
            Debug.Log("🔄 R pressed - Resetting game!");
            ResetCurrentLevelOnly();
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
    }


    private IEnumerator DelayedApplyProgress()
    {
        yield return null;

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
        string saveKey = GetCurrentSaveKey();

        if (PlayerPrefs.HasKey(saveKey))
        {
            try
            {
                string jsonData = PlayerPrefs.GetString(saveKey);
                progressData = JsonUtility.FromJson<GameProgressData>(jsonData);

                if (debugMode)
                {
                    string levelInfo = currentLevelData != null ? $" for {currentLevelData.levelName}" : "";
                    Debug.Log($"[GameProgressManager] Progress loaded{levelInfo}: {progressData.placedItems.Count} items placed (Key: {saveKey})");
                }

                OnProgressLoaded?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameProgressManager] Failed to load progress: {e.Message}");
                InitializeProgress();
            }
        }
        else
        {
            if (debugMode)
            {
                string levelInfo = currentLevelData != null ? $" for {currentLevelData.levelName}" : "";
                Debug.Log($"[GameProgressManager] No save data found{levelInfo}, starting fresh (Key: {saveKey})");
            }
            InitializeProgress();
        }
    }

    private void SaveProgress()
    {
        try
        {
            progressData.lastPlayDate = System.DateTime.Now;
            string jsonData = JsonUtility.ToJson(progressData, true);

            string saveKey = GetCurrentSaveKey();
            PlayerPrefs.SetString(saveKey, jsonData);
            PlayerPrefs.Save();

            if (debugMode)
            {
                string levelInfo = currentLevelData != null ? $" for {currentLevelData.levelName}" : "";
                Debug.Log($"[GameProgressManager] Progress saved{levelInfo}: {progressData.placedItems.Count} items (Key: {saveKey})");
            }

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

        if (progressData.placedItems.Any(item => item.itemId == itemId))
        {
            if (debugMode)
                Debug.Log($"[GameProgressManager] Item {itemId} already marked as placed");
            return;
        }

        PlacedItemData newItem = new PlacedItemData
        {
            itemId = itemId,
            spriteName = itemSprite != null ? itemSprite.name : ""
        };
        
        progressData.placedItems.Add(newItem);
        progressData.totalItemsPlaced++;
        
        if (debugMode)
            Debug.Log($"[GameProgressManager] ✅ Marked item as placed: {itemId}");
        
        OnItemPlaced?.Invoke(itemId);
        SaveProgress();
    }

    public void RemoveItemPlacement(string itemId)
    {
        if (progressData.placedItems == null)
            return;
            
        var itemToRemove = progressData.placedItems.FirstOrDefault(item => item.itemId == itemId);
        if (itemToRemove != null)
        {
            progressData.placedItems.Remove(itemToRemove);
            OnItemRemoved?.Invoke(itemId);
            SaveProgress();
            
            if (debugMode)
                Debug.Log($"[GameProgressManager] Removed item placement: {itemId}");
        }
    }

    public bool IsItemPlaced(string itemId)
    {
        if (progressData == null)
        {
            Debug.LogWarning("[GameProgressManager] IsItemPlaced called but progressData is null!");
            InitializeProgress();
            return false;
        }
        
        if (progressData.placedItems == null)
        {
            return false;
        }
        
        return progressData.placedItems.Any(item => item.itemId == itemId);
    }

    private void ApplyProgressToScene()
    {
        if (progressData == null || progressData.placedItems == null || progressData.placedItems.Count == 0)
        {
            Debug.Log("[GameProgressManager] No progress to apply");
            return;
        }

        DraggableButton[] allDragButtons = FindObjectsOfType<DraggableButton>(true);
        DropSpot[] allDropSpots = FindObjectsOfType<DropSpot>(true);

        Debug.Log($"[GameProgressManager] Found {allDragButtons.Length} buttons, {allDropSpots.Length} spots");

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

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("✅ PlayerPrefs deleted");

        progressData = new GameProgressData();
        Debug.Log("✅ Progress data reset");

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

        DraggableButton[] allButtons = FindObjectsOfType<DraggableButton>(true);
        foreach (DraggableButton btn in allButtons)
        {
            btn.gameObject.SetActive(true);
        }
        Debug.Log($"✅ Showed {allButtons.Length} buttons");

        Debug.Log("=== ✅ FULL RESET COMPLETE ===");
        Debug.Log("⚠️ Reload the scene to see changes!");
    }

    private IEnumerator RefreshAllBars(System.Collections.Generic.HashSet<ScrollableButtonBar> bars)
    {
        yield return null;

        Debug.Log($"[GameProgressManager] Refreshing {bars.Count} button bars");

        foreach (var bar in bars)
        {
            if (bar != null)
            {
                bar.RefreshBar();
            }
        }
    }

    private void ApplyItemPlacement(DraggableButton dragButton, DropSpot dropSpot)
    {
        Debug.Log($"[GameProgressManager] Applying placement for: {dropSpot.spotId}");

        ScrollableButtonBar buttonBar = null;
        int buttonIndex = -1;

        if (dragButton != null)
        {
            buttonBar = dragButton.GetComponentInParent<ScrollableButtonBar>();

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

        if (buttonBar != null && buttonIndex >= 0)
        {
            StartCoroutine(UpdateBarAfterDestroy(buttonBar, buttonIndex));
        }

        dropSpot.IsSettled = true;

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

    private IEnumerator UpdateBarAfterDestroy(ScrollableButtonBar bar, int index)
    {
        yield return null;

        if (bar != null)
        {
            Debug.Log($"[GameProgressManager] Updating bar after button {index} destroyed");

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
        return progressData;
    }

    /// <summary>
    /// Reset ONLY the current level's progress (not all levels)
    /// </summary>
    [ContextMenu("🔄 Reset Current Level Only")]
    public void ResetCurrentLevelOnly()
    {
        Debug.Log("[GameProgressManager] 🔄 Resetting current level only...");
        
        // 1. Reset in-memory data
        progressData = new GameProgressData();
        
        // 2. Delete current level-specific key
        string currentKey = GetCurrentSaveKey();
        PlayerPrefs.DeleteKey(currentKey);
        PlayerPrefs.Save();
        Debug.Log($"[GameProgressManager] ✅ Deleted key: {currentKey}");
        
        // 3. Reset all DropSpots in scene
        DropSpot[] allDropSpots = FindObjectsOfType<DropSpot>();
        foreach (DropSpot spot in allDropSpots)
        {
            spot.ResetSpot();
        }
        Debug.Log($"[GameProgressManager] ✅ Reset {allDropSpots.Length} drop spots");
        
        // 4. Show all DraggableButtons
        DraggableButton[] allDragButtons = FindObjectsOfType<DraggableButton>();
        foreach (DraggableButton button in allDragButtons)
        {
            button.gameObject.SetActive(true);
        }
        Debug.Log($"[GameProgressManager] ✅ Showed {allDragButtons.Length} buttons");
        
        Debug.Log("[GameProgressManager] 🎉 CURRENT LEVEL RESET COMPLETE!");
    }

    /// <summary>
    /// Reset ALL progress for ALL levels
    /// </summary>
    [ContextMenu("🗑️ Reset ALL Levels Progress")]
    public void ResetAllProgress()
    {
        Debug.Log("[GameProgressManager] 🔄 Starting FULL reset...");
        
        // 1. Reset in-memory data
        progressData = new GameProgressData();
        
        // 2. Delete global save key
        PlayerPrefs.DeleteKey(SAVE_KEY);
        Debug.Log($"[GameProgressManager] ✅ Deleted global key: {SAVE_KEY}");
        
        // 3. Delete current level-specific key
        string currentKey = GetCurrentSaveKey();
        if (currentKey != SAVE_KEY)
        {
            PlayerPrefs.DeleteKey(currentKey);
            Debug.Log($"[GameProgressManager] ✅ Deleted level key: {currentKey}");
        }
        
        // 4. Delete all possible level keys (Level_0_Progress through Level_19_Progress)
        int deletedCount = 0;
        for (int i = 0; i < 20; i++)
        {
            string levelKey = $"Level_{i}_Progress";
            if (PlayerPrefs.HasKey(levelKey))
            {
                PlayerPrefs.DeleteKey(levelKey);
                deletedCount++;
                Debug.Log($"[GameProgressManager] ✅ Deleted key: {levelKey}");
            }
        }
        Debug.Log($"[GameProgressManager] Deleted {deletedCount} level-specific keys");
        
        PlayerPrefs.Save();
        
        if (debugMode)
            Debug.Log("[GameProgressManager] All PlayerPrefs deleted");
            
        // 5. Reset all DropSpots in scene
        DropSpot[] allDropSpots = FindObjectsOfType<DropSpot>();
        foreach (DropSpot spot in allDropSpots)
        {
            spot.ResetSpot();
        }
        Debug.Log($"[GameProgressManager] ✅ Reset {allDropSpots.Length} drop spots");
        
        // 6. Show all DraggableButtons
        DraggableButton[] allDragButtons = FindObjectsOfType<DraggableButton>();
        foreach (DraggableButton button in allDragButtons)
        {
            button.gameObject.SetActive(true);
        }
        Debug.Log($"[GameProgressManager] ✅ Showed {allDragButtons.Length} buttons");
        
        Debug.Log("[GameProgressManager] 🎉 FULL RESET COMPLETE!");
    }

    public void ForceSave()
    {
        SaveProgress();
    }

    public int GetTotalItemsPlaced()
    {
        return progressData.totalItemsPlaced;
    }

    public int GetCurrentLevelItemsPlaced()
    {
        return progressData.placedItems.Count;
    }
}
