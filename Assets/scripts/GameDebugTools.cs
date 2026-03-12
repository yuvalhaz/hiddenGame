using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 🛠️ UNIFIED TESTING & DEBUG TOOL
/// All game testing features in ONE place - no more hunting through multiple scripts!
/// </summary>
public class GameDebugTools : MonoBehaviour
{
    [Header("🔗 Auto-Find References")]
    [Tooltip("These will be found automatically if not assigned")]
    [SerializeField] private GameProgressManager progressManager;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private LevelCompleteController levelCompleteController;
    [SerializeField] private DropSpotBatchManager batchManager;
    [SerializeField] private BatchMessageController messageController;
    [SerializeField] private BatchAdController adController;
    [SerializeField] private RewardedAdsManager adsManager;
    [SerializeField] private ButtonSpotMatcher buttonSpotMatcher;
    [SerializeField] private VisualHintSystem hintSystem;

    [Header("⌨️ Keyboard Shortcuts")]
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField] private KeyCode resetCurrentLevelKey = KeyCode.R;
    [SerializeField] private KeyCode skipLevelKey = KeyCode.N;
    [SerializeField] private KeyCode completeLevelKey = KeyCode.C;
    [SerializeField] private KeyCode showHintKey = KeyCode.H;
    [SerializeField] private KeyCode testMessageKey = KeyCode.M;
    [SerializeField] private KeyCode testAdKey = KeyCode.A;
    [SerializeField] private KeyCode placeAllExceptLastKey = KeyCode.L;

    [Header("📊 Status Display")]
    [SerializeField] private bool showDebugGUI = true;
    [SerializeField] private Vector2 guiPosition = new Vector2(10, 10);
    [SerializeField] private float guiScale = 2.5f; // 🆕 Adjust this to change GUI size!

    private void Awake()
    {
        // Auto-find all references
        FindReferences();
    }

    private void FindReferences()
    {
        if (progressManager == null)
            progressManager = FindObjectOfType<GameProgressManager>();

        if (levelManager == null)
            levelManager = FindObjectOfType<LevelManager>();

        if (levelCompleteController == null)
            levelCompleteController = FindObjectOfType<LevelCompleteController>();

        if (batchManager == null)
            batchManager = FindObjectOfType<DropSpotBatchManager>();

        if (messageController == null)
            messageController = FindObjectOfType<BatchMessageController>();

        if (adController == null)
            adController = FindObjectOfType<BatchAdController>();

        if (adsManager == null)
            adsManager = FindObjectOfType<RewardedAdsManager>();

        if (buttonSpotMatcher == null)
            buttonSpotMatcher = FindObjectOfType<ButtonSpotMatcher>();

        if (hintSystem == null)
            hintSystem = FindObjectOfType<VisualHintSystem>();

        Debug.Log("[GameDebugTools] ✅ References found!");
    }

    private void Update()
    {
        if (!enableKeyboardShortcuts) return;

        // R - Reset current level
        if (Input.GetKeyDown(resetCurrentLevelKey))
        {
            ResetCurrentLevel();
        }

        // N - Next level (skip)
        if (Input.GetKeyDown(skipLevelKey))
        {
            SkipToNextLevel();
        }

        // C - Complete level (trigger level complete)
        if (Input.GetKeyDown(completeLevelKey))
        {
            TriggerLevelComplete();
        }

        // H - Show hint
        if (Input.GetKeyDown(showHintKey))
        {
            TriggerHint();
        }

        // M - Test message
        if (Input.GetKeyDown(testMessageKey))
        {
            TestCompletionMessage();
        }

        // A - Test ad
        if (Input.GetKeyDown(testAdKey))
        {
            TestAd();
        }

        // L - Place all items except last one
        if (Input.GetKeyDown(placeAllExceptLastKey))
        {
            PlaceAllExceptLastItem();
        }
    }

    private void OnGUI()
    {
        if (!showDebugGUI) return;

        // 🆕 Scale GUI to make it bigger and more readable
        GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));

        GUILayout.BeginArea(new Rect(guiPosition.x, guiPosition.y, 600, 1000));
        GUILayout.Box("🛠️ GAME DEBUG TOOLS");

        // === PROGRESS INFO ===
        GUILayout.Label("=== PROGRESS ===");
        if (progressManager != null)
        {
            var data = progressManager.GetProgressData();
            GUILayout.Label($"Items Placed: {data.placedItems.Count}");
        }

        if (levelManager != null)
        {
            GUILayout.Label($"Level: {levelManager.GetCurrentLevelName()}");
            GUILayout.Label($"Progress: {levelManager.GetLevelProgress()}");
        }

        if (batchManager != null)
        {
            int batch = batchManager.GetCurrentBatchIndex();
            int total = batchManager.GetTotalBatches();
            GUILayout.Label($"Batch: {batch}/{total}");
        }

        GUILayout.Space(10);

        // === RESET BUTTONS ===
        GUILayout.Label("=== RESET ===");
        if (GUILayout.Button("🔄 Reset Current Level"))
        {
            ResetCurrentLevel();
        }

        if (GUILayout.Button("🗑️ Reset ALL Progress"))
        {
            ResetAllProgress();
        }

        GUILayout.Space(10);

        // === LEVEL PROGRESSION ===
        GUILayout.Label("=== LEVEL CONTROL ===");
        if (GUILayout.Button("🎉 Complete Level (Test)"))
        {
            TriggerLevelComplete();
        }

        if (GUILayout.Button("⏭️ Skip to Next Level"))
        {
            SkipToNextLevel();
        }

        if (GUILayout.Button("🔓 Unlock All Levels"))
        {
            UnlockAllLevels();
        }

        GUILayout.Space(10);

        // === TESTING ===
        GUILayout.Label("=== TESTING ===");
        if (GUILayout.Button("📦 Place All EXCEPT Last Item"))
        {
            PlaceAllExceptLastItem();
        }

        if (GUILayout.Button("💬 Test Completion Message"))
        {
            TestCompletionMessage();
        }

        if (GUILayout.Button("📺 Test Ad"))
        {
            TestAd();
        }

        if (GUILayout.Button("💡 Trigger Hint"))
        {
            TriggerHint();
        }

        if (GUILayout.Button("🔍 Check Button/Spot Matches"))
        {
            CheckMatches();
        }

        GUILayout.Space(10);

        // === INFO ===
        GUILayout.Label("=== KEYBOARD SHORTCUTS ===");
        GUILayout.Label($"R - Reset Level");
        GUILayout.Label($"N - Skip to Next Level");
        GUILayout.Label($"C - Complete Level");
        GUILayout.Label($"H - Show Hint");
        GUILayout.Label($"M - Test Message");
        GUILayout.Label($"A - Test Ad");
        GUILayout.Label($"L - Place All Except Last");

        GUILayout.EndArea();
    }

    // ===== RESET FUNCTIONS =====

    [ContextMenu("🔄 Reset Current Level")]
    public void ResetCurrentLevel()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🔄 RESETTING CURRENT LEVEL");
        Debug.Log("═══════════════════════════════════════");

        if (progressManager != null)
        {
            progressManager.ResetCurrentLevelOnly();
        }

        if (levelManager != null)
        {
            levelManager.ResetCurrentLevel();
        }

        if (adController != null)
        {
            // ✅ FIX: BatchAdController doesn't have ResetBatchCounter()
            // We reset the ad timer instead:
            adController.ResetPlayTimerForTesting();
        }

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        Debug.Log("✅ Level reset complete!");
    }

    [ContextMenu("🗑️ Reset ALL Progress")]
    public void ResetAllProgress()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🗑️ RESETTING ALL PROGRESS");
        Debug.Log("═══════════════════════════════════════");

        // Nuclear option: delete ALL PlayerPrefs (including bonus level unlocks)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[GameDebugTools] ✅ PlayerPrefs.DeleteAll() - all keys cleared!");

        if (progressManager != null)
        {
            progressManager.ResetAllProgress();
        }

        if (levelManager != null)
        {
            levelManager.ResetAllProgress();
        }

        if (adController != null)
        {
            // ✅ FIX: BatchAdController doesn't have ResetBatchCounter()
            // We reset the ad timer instead:
            adController.ResetPlayTimerForTesting();
        }

        // Reload current scene to refresh all UI
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        Debug.Log("✅ All progress reset!");
    }

    // ===== LEVEL PROGRESSION =====

    [ContextMenu("⏭️ Skip to Next Level")]
    public void SkipToNextLevel()
    {
        Debug.Log("⏭️ Skipping to next level...");

        if (levelManager != null)
        {
            levelManager.SkipLevel();
        }
        else
        {
            Debug.LogWarning("❌ LevelManager not found!");
        }
    }

    [ContextMenu("🎉 Complete Level (Test)")]
    public void TriggerLevelComplete()
    {
        Debug.Log("🎉 Triggering level complete for testing...");

        if (levelCompleteController != null)
        {
            levelCompleteController.TriggerLevelComplete();
            Debug.Log("✅ Level complete triggered! Watch for WELL DONE message, ads, and level progression.");
        }
        else
        {
            Debug.LogWarning("❌ LevelCompleteController not found!");
            Debug.LogWarning("💡 Trying alternative method via LevelManager...");

            // Alternative: trigger via LevelManager if it exists
            if (levelManager != null)
            {
                levelManager.AdvanceToNextLevel();
            }
            else
            {
                Debug.LogError("❌ No level progression system found!");
            }
        }
    }

    [ContextMenu("🔓 Unlock All Levels")]
    public void UnlockAllLevels()
    {
        Debug.Log("🔓 Unlocking all levels...");

        // Find all LevelData assets and unlock them
        var allLevelData = FindObjectsOfType<LevelData>(true);
        foreach (var levelData in allLevelData)
        {
            levelData.Unlock();
        }

        Debug.Log($"✅ Unlocked {allLevelData.Length} levels!");
    }

    // ===== TESTING FUNCTIONS =====

    [ContextMenu("💬 Test Completion Message")]
    public void TestCompletionMessage()
    {
        if (messageController == null)
        {
            messageController = FindObjectOfType<BatchMessageController>();
        }

        if (messageController != null)
        {
            messageController.ShowCompletionMessage(0);
            Debug.Log("💬 Test message displayed!");
        }
        else
        {
            Debug.LogWarning("❌ BatchMessageController not found!");
        }
    }

    [ContextMenu("📺 Test Ad")]
    public void TestAd()
    {
        if (adsManager == null)
        {
            adsManager = FindObjectOfType<RewardedAdsManager>();
        }

        if (adsManager != null)
        {
            Debug.Log("📺 Testing ad...");
            adsManager.ShowRewarded(
                onReward: () => Debug.Log("✅ Reward!"),
                onClosed: (completed) => Debug.Log($"✅ Closed: {completed}"),
                onFailed: (error) => Debug.LogError($"❌ Failed: {error}"),
                onOpened: () => Debug.Log("📺 Opened!")
            );
        }
        else
        {
            Debug.LogWarning("❌ RewardedAdsManager not found!");
        }
    }

    [ContextMenu("💡 Trigger Hint")]
    public void TriggerHint()
    {
        if (hintSystem == null)
        {
            hintSystem = FindObjectOfType<VisualHintSystem>();
        }

        if (hintSystem != null)
        {
            if (hintSystem.HasAvailableButtons())
            {
                hintSystem.TriggerHint();
                Debug.Log("💡 Hint triggered!");
            }
            else
            {
                Debug.LogWarning("❌ No available buttons for hint!");
            }
        }
        else
        {
            Debug.LogWarning("❌ VisualHintSystem not found!");
        }
    }

    [ContextMenu("🔍 Check Button/Spot Matches")]
    public void CheckMatches()
    {
        if (buttonSpotMatcher == null)
        {
            buttonSpotMatcher = FindObjectOfType<ButtonSpotMatcher>();
        }

        if (buttonSpotMatcher != null)
        {
            // Use reflection to call CheckMatches
            var method = buttonSpotMatcher.GetType().GetMethod("CheckMatches");
            if (method != null)
            {
                method.Invoke(buttonSpotMatcher, null);
            }
        }
        else
        {
            Debug.LogWarning("❌ ButtonSpotMatcher not found!");
        }
    }

    [ContextMenu("📊 Show Full Status")]
    public void ShowFullStatus()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("📊 GAME STATUS");
        Debug.Log("════════════════════════════════════════");

        if (progressManager != null)
        {
            var data = progressManager.GetProgressData();
            Debug.Log($"✅ Items Placed: {data.placedItems.Count}");
            Debug.Log($"✅ Total Items: {data.totalItemsPlaced}");
        }

        if (levelManager != null)
        {
            Debug.Log($"✅ Current Level: {levelManager.GetCurrentLevelName()}");
            Debug.Log($"✅ Progress: {levelManager.GetLevelProgress()}");
            Debug.Log($"✅ Total Levels: {levelManager.GetTotalLevels()}");
        }

        if (batchManager != null)
        {
            int batch = batchManager.GetCurrentBatchIndex();
            int total = batchManager.GetTotalBatches();
            Debug.Log($"✅ Current Batch: {batch}/{total}");
        }

        if (adsManager != null)
        {
            Debug.Log($"✅ Ad Ready: {adsManager.IsReady()}");
        }

        Debug.Log("════════════════════════════════════════");
    }

    // ===== SAVED ITEMS =====

    [ContextMenu("📋 Show Saved Items")]
    public void ShowSavedItems()
    {
        if (progressManager == null)
        {
            Debug.LogWarning("❌ GameProgressManager not found!");
            return;
        }

        Debug.Log("========================================");
        Debug.Log("=== SAVED ITEMS ===");
        Debug.Log("========================================");

        var data = progressManager.GetProgressData();
        if (data == null || data.placedItems == null)
        {
            Debug.Log("❌ No saved data!");
            return;
        }

        Debug.Log($"Total items saved: {data.placedItems.Count}");

        foreach (var item in data.placedItems)
        {
            Debug.Log($"✅ Saved: {item.itemId}");
        }

        Debug.Log("========================================");
    }

    // ===== TESTING HELPERS =====

    [ContextMenu("📦 Place All Items EXCEPT Last One")]
    public void PlaceAllExceptLastItem()
    {
        // Start coroutine to place items with delays
        StartCoroutine(PlaceAllExceptLastCoroutine());
    }

    private System.Collections.IEnumerator PlaceAllExceptLastCoroutine()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("📦 PLACING ALL ITEMS EXCEPT THE LAST ONE");
        Debug.Log("📦 (Across ALL batches in the entire level!)");
        Debug.Log("════════════════════════════════════════");

        if (batchManager == null)
        {
            Debug.LogError("❌ DropSpotBatchManager not found!");
            yield break;
        }

        if (progressManager == null)
        {
            Debug.LogError("❌ GameProgressManager not found!");
            yield break;
        }

        // Get ALL drop spots in batch order from DropSpotBatchManager
        List<DropSpot> allSpots = batchManager.GetAllDropSpots();

        if (allSpots == null || allSpots.Count == 0)
        {
            Debug.LogWarning("❌ No drop spots found!");
            yield break;
        }

        if (allSpots.Count == 1)
        {
            Debug.LogWarning("⚠️ Only one spot - nothing to place!");
            yield break;
        }

        int totalBatches = batchManager.GetTotalBatches();
        int totalSpots = allSpots.Count;
        int spotsToPlace = totalSpots - 1;

        Debug.Log($"🎯 Level has {totalBatches} batches");
        Debug.Log($"📊 Total spots in ENTIRE level: {totalSpots}");
        Debug.Log($"📦 Placing {spotsToPlace} spots in BATCH ORDER (with delays for batch progression)");
        Debug.Log($"🎯 This should complete all batches except leaving ONE spot in the final batch");
        Debug.Log("");

        // Place spots one by one in batch order with delays
        for (int i = 0; i < spotsToPlace; i++)
        {
            DropSpot spot = allSpots[i];
            if (spot == null) continue;

            string spotId = spot.spotId;

            // Which batch is this spot in?
            int batchNum = 0;
            for (int b = 0; b < totalBatches; b++)
            {
                int start = batchManager.GetBatchStartIndex(b);
                int size = batchManager.GetBatchSize(b);
                if (i >= start && i < start + size)
                {
                    batchNum = b;
                    break;
                }
            }

            progressManager.MarkItemAsPlaced(spotId);
            Debug.Log($"  ✅ [{i + 1}/{spotsToPlace}] Batch {batchNum}: Placed spot '{spotId}'");

            // Wait to allow batch completion messages/animations
            yield return new WaitForSeconds(0.8f);
        }

        Debug.Log("");
        DropSpot lastSpot = allSpots[totalSpots - 1];
        Debug.Log($"🎯 LAST SPOT (not placed): {lastSpot.spotId}");
        Debug.Log($"💡 Now drag '{lastSpot.spotId}' manually to complete the FINAL batch and trigger level completion!");
        Debug.Log("════════════════════════════════════════");
    }
}