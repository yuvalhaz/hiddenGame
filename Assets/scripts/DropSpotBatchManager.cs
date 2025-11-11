using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages batches of DropSpots with flexible batch sizes
/// </summary>
public class DropSpotBatchManager : MonoBehaviour
{
    // סוגי אנימציות זמינים
    public enum AnimationType
    {
        ScaleBounce,    // גדל עם קפיצה
        ScaleSmooth,    // גדל חלק
        PopIn,          // קופץ פנימה
        SlideDown,      // מחליק מלמעלה
        Rotate          // מסתובב תוך כדי הופעה
    }

    [Header("🎯 Batch Mode")]
    [SerializeField] private bool useCustomBatchSizes = false;
    [Tooltip("Enable to define different sizes for each batch")]
    
    [Header("⚙️ Simple Mode (All Batches Same Size)")]
    [SerializeField] private int numberOfBatches = 3;
    [SerializeField] private int spotsPerBatch = 7;
    
    [Header("🔧 Advanced Mode (Custom Size Per Batch)")]
    [SerializeField] private List<int> customBatchSizes = new List<int>() { 5, 7, 10 };
    [Tooltip("Define size for each batch: [Batch 0: 5 spots, Batch 1: 7 spots, Batch 2: 10 spots]")]
    
    [Header("📋 DropSpot References")]
    [SerializeField] private List<DropSpot> allDropSpots = new List<DropSpot>();
    
    [Header("🎉 Completion Messages")]
    [SerializeField] private bool showCompletionMessage = true;
    
    [Header("📺 UI References")]
    [SerializeField] private GameObject completionPanel;
    [Tooltip("The panel/canvas that contains the message")]
    [SerializeField] private UnityEngine.UI.Text completionText;
    [Tooltip("Legacy Unity UI Text component")]
    
    [SerializeField] private float messageDuration = 2f;
    [Tooltip("How long to show the message (seconds)")]
    
    [Header("📊 Progress Bar UI")]
    [SerializeField] private UnityEngine.UI.Text gameProgressText;
    [Tooltip("Text showing game progress in percentage (e.g., 'GAME PROGRESS: 35%')")]
    [SerializeField] private UnityEngine.UI.Text currentBatchProgressText;
    [Tooltip("Text showing current batch progress (e.g., '3/7')")]
    [SerializeField] private UnityEngine.UI.Slider progressSlider;
    [Tooltip("Optional slider to show visual progress")]
    [SerializeField] private bool updateProgressUI = true;
    [Tooltip("Enable/disable progress UI updates")]
    
    [Header("🎬 Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private AnimationType animationType = AnimationType.ScaleBounce;
    [SerializeField] private float animationDuration = 0.5f;
    [Tooltip("Duration of the entrance animation")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("🔊 Audio Settings")]
    [SerializeField] private bool playSound = true;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool useRandomSound = true;
    [SerializeField] private List<AudioClip> completionSounds = new List<AudioClip>();
    [SerializeField] private AudioClip singleSound;
    [Tooltip("Use this if you want only one sound (when useRandomSound = false)")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;
    
    [Header("💬 Message Content")]
    [SerializeField] private bool useCustomMessagesPerBatch = false;
    [Tooltip("Different message for each batch")]
    [SerializeField] private List<string> customMessagesPerBatch = new List<string>() 
    { 
        "GREAT JOB!", 
        "AMAZING!", 
        "PERFECT!" 
    };
    
    [Header("🎲 Random Messages Mode")]
    [SerializeField] private List<string> randomMessages = new List<string>()
    {
        "GREAT!",
        "AWESOME!",
        "FANTASTIC!",
        "WONDERFUL!",
        "EXCELLENT!",
        "AMAZING!",
        "SUPER!",
        "PERFECT!",
        "BRILLIANT!",
        "INCREDIBLE!"
    };
    
    [Header("🎨 Random Colors")]
    [SerializeField] private bool useRandomColors = true;
    [SerializeField] private List<Color> messageColors = new List<Color>()
    {
        new Color(1f, 0.84f, 0f),      // זהב
        new Color(1f, 0.27f, 0f),      // כתום-אדום
        new Color(0.2f, 0.8f, 0.2f),   // ירוק
        new Color(0f, 0.75f, 1f),      // כחול בהיר
        new Color(1f, 0.41f, 0.71f),   // ורוד
        new Color(0.58f, 0f, 0.83f),   // סגול
        new Color(1f, 1f, 0f),         // צהוב
        new Color(1f, 0.5f, 0f)        // כתום
    };
    
    [Header("📺 Ad Settings")]
    [SerializeField] private bool showAdsOnBatchComplete = true;
    [Tooltip("Show ads when completing batches")]
    [SerializeField] private int adFrequency = 1;
    [Tooltip("Show ad every X batches (1 = every batch, 2 = every 2 batches)")]
    [SerializeField] private bool skipAdOnFirstBatch = false;
    [Tooltip("Don't show ad after completing the first batch")]
    [SerializeField] private float delayBeforeAd = 0.5f;
    [Tooltip("Extra delay after message disappears before showing ad")]
    [SerializeField] private bool waitForAdToClose = true;
    [Tooltip("Wait for ad to close before revealing next batch")]
    
    [Header("💬 Ending Dialog")]
    [SerializeField] private GameEndingDialogSystem endingDialogSystem;
    [Tooltip("בועות הדיבור שיופיעו בסוף המשחק")]

    [Header("🐛 Debug")]
    [SerializeField] private bool debugMode = true;

    private int currentBatch = 0;
    private int totalPlacedInCurrentBatch = 0;
    private int batchesCompleted = 0;
    
    private Coroutine hideMessageCoroutine = null;
    private bool isShowingMessage = false;

    private void OnValidate()
    {
        int required = GetTotalRequiredSpots();
        
        if (allDropSpots.Count > 0 && allDropSpots.Count != required)
        {
            Debug.LogWarning($"[DropSpotBatchManager] ⚠️ Mismatch! Required: {required} spots, Found: {allDropSpots.Count} spots in list");
        }

        if (useCustomBatchSizes && customBatchSizes.Count == 0)
        {
            Debug.LogError("[DropSpotBatchManager] ❌ Custom mode enabled but no batch sizes defined!");
        }
    }

    private void Awake()
    {
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
            completionPanel.transform.localScale = Vector3.one;
            completionPanel.transform.localRotation = Quaternion.identity;
        }
        
        isShowingMessage = false;
        hideMessageCoroutine = null;
        batchesCompleted = 0;
        
        if (audioSource == null && playSound)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                if (debugMode)
                    Debug.Log("[DropSpotBatchManager] ✅ Created AudioSource component");
            }
        }
        
        if (allDropSpots.Count == 0)
        {
            allDropSpots.AddRange(FindObjectsOfType<DropSpot>(true));

            if (debugMode)
                Debug.Log($"[DropSpotBatchManager] Auto-found {allDropSpots.Count} DropSpots");
        }

        int required = GetTotalRequiredSpots();
        if (allDropSpots.Count < required)
        {
            Debug.LogError($"[DropSpotBatchManager] ❌ NOT ENOUGH SPOTS! Required: {required}, Found: {allDropSpots.Count}");
        }
        else if (allDropSpots.Count > required)
        {
            Debug.LogWarning($"[DropSpotBatchManager] ⚠️ TOO MANY SPOTS! Required: {required}, Found: {allDropSpots.Count} (extras will be ignored)");
        }

        PrintBatchConfiguration();
    }

    private void Start()
    {
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return null;
        
        if (debugMode)
        {
            Debug.Log("========================================");
            Debug.Log("[DropSpotBatchManager] 🚀 INITIALIZING");
            Debug.Log("========================================");
        }
        
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnItemPlaced -= OnItemPlaced;
            GameProgressManager.Instance.OnItemPlaced += OnItemPlaced;
            
            if (debugMode)
                Debug.Log("[DropSpotBatchManager] ✅ Connected to event");
        }
        else
        {
            Debug.LogError("[DropSpotBatchManager] ❌ GameProgressManager is NULL!");
        }
        
        CalculateCurrentBatch();
        
        if (debugMode)
        {
            Debug.Log($"[DropSpotBatchManager] currentBatch = {currentBatch}");
            Debug.Log($"[DropSpotBatchManager] totalPlaced = {totalPlacedInCurrentBatch}");
            Debug.Log($"[DropSpotBatchManager] Need {GetBatchSize(currentBatch) - totalPlacedInCurrentBatch} more");
        }

        HideAllDropSpots();
        RevealBatch(currentBatch);
        
        // ✅ עדכן את ה-Progress UI בהתחלה
        UpdateProgressUI();
        
        if (debugMode)
            Debug.Log("[DropSpotBatchManager] ✅ Init complete!");
    }

    private void CalculateCurrentBatch()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning("[DropSpotBatchManager] Cannot calculate - no GameProgressManager");
            return;
        }

        if (debugMode)
            Debug.Log("[DropSpotBatchManager] 🔄 Calculating current batch...");

        for (int batch = 0; batch < GetTotalBatches(); batch++)
        {
            int startIdx = GetBatchStartIndex(batch);
            int batchSize = GetBatchSize(batch);
            int placedCount = 0;

            for (int i = startIdx; i < startIdx + batchSize && i < allDropSpots.Count; i++)
            {
                if (allDropSpots[i] != null && GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
                {
                    placedCount++;
                    if (debugMode)
                        Debug.Log($"  ✅ {allDropSpots[i].spotId} placed");
                }
            }

            if (debugMode)
                Debug.Log($"Batch {batch}: {placedCount}/{batchSize}");

            if (placedCount < batchSize)
            {
                currentBatch = batch;
                totalPlacedInCurrentBatch = placedCount;
                if (debugMode)
                    Debug.Log($"🎯 Current = Batch {currentBatch}");
                return;
            }
        }

        currentBatch = GetTotalBatches();
        totalPlacedInCurrentBatch = 0;
        if (debugMode)
            Debug.Log("🏆 All batches complete!");
    }

    private void OnDestroy()
    {
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnItemPlaced -= OnItemPlaced;
        }
    }

    private void OnItemPlaced(string itemId)
    {
        if (debugMode)
        {
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"🔔 Item placed: {itemId}");
            Debug.Log($"Before: batch={currentBatch}, placed={totalPlacedInCurrentBatch}");
        }
        
        totalPlacedInCurrentBatch++;
        int batchSize = GetBatchSize(currentBatch);
        
        // ✅ עדכן את ה-UI
        UpdateProgressUI();
        
        if (debugMode)
            Debug.Log($"Progress: {totalPlacedInCurrentBatch}/{batchSize}");
        
        if (totalPlacedInCurrentBatch >= batchSize)
        {
            if (debugMode)
            {
                Debug.Log("🎉🎉🎉 BATCH COMPLETE! 🎉🎉🎉");
                Debug.Log($"showMessage = {showCompletionMessage}");
            }
            
            int completedBatch = currentBatch;
            batchesCompleted++;

            bool isLastBatch = (completedBatch == GetTotalBatches() - 1);
            bool willShowAd = ShouldShowAdNow(completedBatch);

            if (debugMode)
            {
                Debug.Log($"🔍 Batch {completedBatch} completed!");
                Debug.Log($"🔍 isLastBatch = {isLastBatch} (completed={completedBatch}, total={GetTotalBatches()})");
                Debug.Log($"🔍 willShowAd = {willShowAd}");
                Debug.Log($"🔍 showCompletionMessage = {showCompletionMessage}");
            }

            if (showCompletionMessage)
            {
                ShowCompletionMessage(completedBatch, willShowAd);
            }

            currentBatch++;
            totalPlacedInCurrentBatch = 0;

            // ✅ עדכן UI אחרי מעבר לבאטץ' הבא
            UpdateProgressUI();

            // ✅ בדוק אם צריך להציג פרסומת
            // אם זה הבאטץ' האחרון, הפרסומת תופיע אחרי בועות הדיבור
            if (willShowAd && !isLastBatch)
            {
                if (debugMode)
                    Debug.Log($"📺 Will show ad after message for batch {completedBatch}");

                StartCoroutine(ShowAdAndContinue());
            }
            else if (!isLastBatch)
            {
                // אין פרסומת ולא באטץ' אחרון - המשך לבאטץ' הבא
                if (currentBatch < GetTotalBatches())
                {
                    StartCoroutine(RevealNextBatchDelayed());
                }
            }
            else
            {
                if (debugMode)
                    Debug.Log($"🏁 This was the LAST batch! Ending dialog will be triggered after message");
            }
            // אם זה באטץ' אחרון, HideMessageAfterDelay יפעיל את בועות הדיבור
            
            if (debugMode)
                Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }
    }

    private bool ShouldShowAdNow(int completedBatchIndex)
    {
        if (!showAdsOnBatchComplete)
        {
            if (debugMode)
                Debug.Log("📺 Ads disabled");
            return false;
        }
        
        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogWarning("📺 RewardedAdsManager not found!");
            return false;
        }
        
        if (skipAdOnFirstBatch && completedBatchIndex == 0)
        {
            if (debugMode)
                Debug.Log("📺 Skipping ad on first batch");
            return false;
        }
        
        if (adFrequency <= 0)
        {
            if (debugMode)
                Debug.Log("📺 Ad frequency is 0");
            return false;
        }
        
        bool shouldShow = (batchesCompleted % adFrequency) == 0;
        
        if (debugMode)
            Debug.Log($"📺 Completed: {batchesCompleted}, Freq: {adFrequency}, Show: {shouldShow}");
        
        return shouldShow;
    }

    private IEnumerator ShowAdAndContinue()
    {
        if (debugMode)
            Debug.Log("📺 Waiting for completion message to finish...");
        
        // ✅ חכה עד שהודעת העידוד תיעלם לגמרי
        float messageTime = messageDuration;
        if (useAnimation)
            messageTime += animationDuration * 0.5f;
        
        yield return new WaitForSeconds(messageTime + delayBeforeAd);
        
        if (debugMode)
            Debug.Log("📺 Message finished. Showing ad now...");
        
        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogWarning("📺 RewardedAdsManager missing!");
            if (currentBatch < GetTotalBatches())
                StartCoroutine(RevealNextBatchDelayed());
            yield break;
        }
        
        bool adClosed = false;
        
        RewardedAdsManager.Instance.ShowRewarded(
            onReward: () =>
            {
                if (debugMode)
                    Debug.Log("📺 Ad reward granted!");
            },
            onClosed: (completed) =>
            {
                if (debugMode)
                    Debug.Log($"📺 Ad closed. Completed: {completed}");
                adClosed = true;
            },
            onFailed: (error) =>
            {
                Debug.LogWarning($"📺 Ad failed: {error}");
                adClosed = true;
            },
            onOpened: () =>
            {
                if (debugMode)
                    Debug.Log("📺 Ad opened!");
            }
        );
        
        if (waitForAdToClose)
        {
            float timeout = 60f;
            float elapsed = 0f;
            
            while (!adClosed && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (elapsed >= timeout)
                Debug.LogWarning("📺 Ad timeout!");
        }
        
        if (debugMode)
            Debug.Log("📺 Ad finished. Continuing...");
        
        if (currentBatch < GetTotalBatches())
        {
            StartCoroutine(RevealNextBatchDelayed());
        }
        else
        {
            if (debugMode)
                Debug.Log("🏆 All batches complete!");
        }
    }

    private void ShowCompletionMessage(int batch, bool willShowAd = false)
    {
        if (debugMode)
            Debug.Log($"💬 ShowCompletionMessage({batch}, willShowAd={willShowAd})");
        
        if (isShowingMessage)
        {
            if (hideMessageCoroutine != null)
                StopCoroutine(hideMessageCoroutine);
            if (completionPanel != null)
                completionPanel.SetActive(false);
        }
        
        isShowingMessage = true;
        
        string message = GetCompletionMessage(batch);
        
        if (completionPanel == null || completionText == null)
        {
            Debug.LogError("❌ Panel or Text is NULL!");
            isShowingMessage = false;
            return;
        }
        
        completionPanel.transform.localScale = Vector3.one;
        completionPanel.transform.localRotation = Quaternion.identity;
        
        completionText.text = message;
        
        if (useRandomColors && messageColors.Count > 0)
        {
            completionText.color = messageColors[Random.Range(0, messageColors.Count)];
        }
        
        completionPanel.SetActive(true);
        
        if (useAnimation)
            StartCoroutine(AnimateMessage());
        
        if (playSound)
            PlayCompletionSound();
        
        // בדוק אם זה הבאטץ' האחרון
        bool isLastBatch = (batch == GetTotalBatches() - 1);

        hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay(isLastBatch));

        Debug.Log($"<color=yellow>🎉 {message} 🎉</color>");
    }

    private IEnumerator HideMessageAfterDelay(bool isLastBatch = false)
    {
        yield return new WaitForSeconds(messageDuration);

        if (useAnimation && completionPanel != null)
            yield return StartCoroutine(AnimateMessageOut());

        if (completionPanel != null)
            completionPanel.SetActive(false);

        isShowingMessage = false;
        hideMessageCoroutine = null;

        if (debugMode)
            Debug.Log($"🔍 HideMessageAfterDelay: isLastBatch={isLastBatch}, endingDialogSystem={(endingDialogSystem != null ? "EXISTS" : "NULL")}");

        // אם זה הבאטץ' האחרון - הפעל בועות דיבור
        if (isLastBatch && endingDialogSystem != null)
        {
            if (debugMode)
                Debug.Log("🎬 Starting ending dialog bubbles!");

            endingDialogSystem.StartEndingDialog();
        }
        else if (isLastBatch && endingDialogSystem == null)
        {
            Debug.LogError("❌ Last batch completed but endingDialogSystem is NULL! Assign GameEndingDialogSystem in inspector!");
        }
    }

    private IEnumerator AnimateMessage()
    {
        if (completionPanel == null) yield break;
        
        Transform t = completionPanel.transform;
        Vector3 originalScale = t.localScale;
        Vector3 originalPos = t.localPosition;
        Quaternion originalRot = t.localRotation;
        
        float elapsed = 0f;
        
        switch (animationType)
        {
            case AnimationType.ScaleBounce:
                t.localScale = Vector3.zero;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / animationDuration;
                    float bounce = Mathf.Sin(progress * Mathf.PI * 0.5f);
                    float overshoot = 1f + Mathf.Sin(progress * Mathf.PI) * 0.2f;
                    t.localScale = originalScale * bounce * overshoot;
                    yield return null;
                }
                break;
                
            case AnimationType.ScaleSmooth:
                t.localScale = Vector3.zero;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = scaleCurve.Evaluate(elapsed / animationDuration);
                    t.localScale = originalScale * progress;
                    yield return null;
                }
                break;
                
            case AnimationType.PopIn:
                t.localScale = originalScale * 1.5f;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / animationDuration;
                    float easeOut = 1f - Mathf.Pow(1f - progress, 3f);
                    t.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, easeOut);
                    yield return null;
                }
                break;
                
            case AnimationType.SlideDown:
                Vector3 startPos = originalPos + Vector3.up * 300f;
                t.localPosition = startPos;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / animationDuration;
                    float easeOut = 1f - Mathf.Pow(1f - progress, 3f);
                    t.localPosition = Vector3.Lerp(startPos, originalPos, easeOut);
                    yield return null;
                }
                break;
                
            case AnimationType.Rotate:
                t.localScale = Vector3.zero;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / animationDuration;
                    t.localScale = originalScale * scaleCurve.Evaluate(progress);
                    t.localRotation = Quaternion.Euler(0, 0, (1f - progress) * 360f);
                    yield return null;
                }
                break;
        }
        
        t.localScale = originalScale;
        t.localPosition = originalPos;
        t.localRotation = originalRot;
    }

    private IEnumerator AnimateMessageOut()
    {
        if (completionPanel == null) yield break;
        
        Transform t = completionPanel.transform;
        Vector3 start = t.localScale;
        float duration = animationDuration * 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(start, Vector3.zero, elapsed / duration);
            yield return null;
        }
    }

    private void PlayCompletionSound()
    {
        if (audioSource == null) return;
        
        AudioClip clip = null;
        
        if (useRandomSound && completionSounds.Count > 0)
            clip = completionSounds[Random.Range(0, completionSounds.Count)];
        else if (singleSound != null)
            clip = singleSound;
        
        if (clip != null)
            audioSource.PlayOneShot(clip, soundVolume);
    }

    private string GetCompletionMessage(int batch)
    {
        if (useCustomMessagesPerBatch && batch < customMessagesPerBatch.Count)
            return customMessagesPerBatch[batch];
        
        if (randomMessages.Count > 0)
            return randomMessages[Random.Range(0, randomMessages.Count)];
        
        return "GREAT!";
    }

    private IEnumerator RevealNextBatchDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        RevealBatch(currentBatch);
    }

    private void HideAllDropSpots()
    {
        foreach (var spot in allDropSpots)
        {
            if (spot != null)
            {
                if (GameProgressManager.Instance != null && GameProgressManager.Instance.IsItemPlaced(spot.spotId))
                {
                    spot.gameObject.SetActive(true);
                    var reveal = spot.GetComponent<ImageRevealController>();
                    if (reveal != null && !reveal.IsRevealed)
                        reveal.RevealInstant();
                }
                else
                {
                    spot.gameObject.SetActive(false);
                }
            }
        }
    }

    // ✅ עדכון ה-Progress UI
    private void UpdateProgressUI()
    {
        if (!updateProgressUI) return;
        
        // חישוב אחוזי התקדמות במשחק
        float gameProgressPercentage = CalculateGameProgressPercentage();
        
        // עדכון טקסט "GAME PROGRESS"
        if (gameProgressText != null)
        {
            gameProgressText.text = $"GAME PROGRESS: {gameProgressPercentage:F0}%";
        }
        
        // עדכון התקדמות ב-batch הנוכחי
        if (currentBatchProgressText != null)
        {
            int currentBatchSize = GetBatchSize(currentBatch);
            currentBatchProgressText.text = $"{totalPlacedInCurrentBatch}/{currentBatchSize}";
        }
        
        // עדכון סליידר
        if (progressSlider != null)
        {
            int currentBatchSize = GetBatchSize(currentBatch);
            if (currentBatchSize > 0)
            {
                progressSlider.maxValue = currentBatchSize;
                progressSlider.value = totalPlacedInCurrentBatch;
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"[ProgressUI] Game Progress: {gameProgressPercentage:F1}%, Current Batch: {totalPlacedInCurrentBatch}/{GetBatchSize(currentBatch)}");
        }
    }

    // ✅ חישוב אחוזי התקדמות במשחק
    private float CalculateGameProgressPercentage()
    {
        int totalRequired = GetTotalRequiredSpots();
        if (totalRequired == 0)
            return 0f;
        
        int totalPlaced = 0;
        
        if (GameProgressManager.Instance != null)
        {
            // ספור כמה פריטים כבר הושמו
            for (int i = 0; i < allDropSpots.Count && i < totalRequired; i++)
            {
                if (allDropSpots[i] != null && 
                    GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
                {
                    totalPlaced++;
                }
            }
        }
        
        float percentage = (float)totalPlaced / totalRequired * 100f;
        return Mathf.Clamp(percentage, 0f, 100f);
    }

    // ✅ חישוב כמה פריטים נותרו בסך הכל (לשימוש פנימי)
    private int CalculateTotalRemainingItems()
    {
        if (GameProgressManager.Instance == null)
            return GetTotalRequiredSpots();
        
        int totalPlaced = 0;
        
        // ספור כמה פריטים כבר הושמו בכל ה-batches
        for (int i = 0; i < allDropSpots.Count; i++)
        {
            if (allDropSpots[i] != null && 
                GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
            {
                totalPlaced++;
            }
        }
        
        int totalRequired = GetTotalRequiredSpots();
        int remaining = totalRequired - totalPlaced;
        
        return Mathf.Max(0, remaining);
    }

    private void RevealBatch(int batch)
    {
        int start = GetBatchStartIndex(batch);
        int size = GetBatchSize(batch);
        int end = start + size;

        if (debugMode)
            Debug.Log($"Revealing batch {batch}: spots {start}-{end - 1}");

        for (int i = start; i < end && i < allDropSpots.Count; i++)
        {
            if (allDropSpots[i] == null) continue;
            
            if (GameProgressManager.Instance != null && GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
                continue;
            
            allDropSpots[i].gameObject.SetActive(true);
        }
        
        // ✅ עדכן UI אחרי חשיפת batch
        UpdateProgressUI();
    }

    private int GetTotalRequiredSpots()
    {
        return useCustomBatchSizes ? customBatchSizes.Sum() : numberOfBatches * spotsPerBatch;
    }

    private int GetBatchSize(int batch)
    {
        if (useCustomBatchSizes && batch < customBatchSizes.Count)
            return customBatchSizes[batch];
        return spotsPerBatch;
    }

    private int GetBatchStartIndex(int batch)
    {
        if (useCustomBatchSizes)
        {
            int start = 0;
            for (int i = 0; i < batch && i < customBatchSizes.Count; i++)
                start += customBatchSizes[i];
            return start;
        }
        return batch * spotsPerBatch;
    }

    private int GetTotalBatches()
    {
        return useCustomBatchSizes ? customBatchSizes.Count : numberOfBatches;
    }

    public int GetCurrentBatchIndex()
    {
        return currentBatch;
    }

    public List<DropSpot> GetCurrentBatchAvailableSpots()
    {
        List<DropSpot> availableSpots = new List<DropSpot>();

        int start = GetBatchStartIndex(currentBatch);
        int size = GetBatchSize(currentBatch);
        int end = start + size;

        for (int i = start; i < end && i < allDropSpots.Count; i++)
        {
            if (allDropSpots[i] == null) continue;

            // רק spots שעוד לא הושמו
            if (GameProgressManager.Instance != null && !GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
            {
                availableSpots.Add(allDropSpots[i]);
            }
        }

        return availableSpots;
    }

    [ContextMenu("🎨 Test Message")]
    private void TestMessage()
    {
        if (completionText == null || completionPanel == null)
        {
            Debug.LogError("❌ UI not assigned!");
            return;
        }
        ShowCompletionMessage(0);
    }

    [ContextMenu("📺 Test Ad")]
    private void TestAd()
    {
        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogError("❌ RewardedAdsManager not found!");
            return;
        }
        
        Debug.Log("📺 Testing ad...");
        RewardedAdsManager.Instance.ShowRewarded(
            onReward: () => Debug.Log("✅ Reward!"),
            onClosed: (completed) => Debug.Log($"✅ Closed: {completed}"),
            onFailed: (error) => Debug.LogError($"❌ Failed: {error}"),
            onOpened: () => Debug.Log("📺 Opened!")
        );
    }

    [ContextMenu("🎬 Test Complete Flow")]
    private void TestCompleteFlow()
    {
        if (completionText == null || completionPanel == null)
        {
            Debug.LogError("❌ UI not assigned!");
            return;
        }
        
        Debug.Log("🎬 Testing: Message → Ad → Next Batch");
        StartCoroutine(TestFlowCoroutine());
    }
    
    private IEnumerator TestFlowCoroutine()
    {
        ShowCompletionMessage(0);
        
        float messageTime = messageDuration;
        if (useAnimation)
            messageTime += animationDuration * 0.5f;
        
        Debug.Log($"⏰ Waiting {messageTime}s...");
        yield return new WaitForSeconds(messageTime);
        
        Debug.Log("✅ Message done!");
        
        if (RewardedAdsManager.Instance != null)
        {
            Debug.Log("📺 Showing ad...");
            RewardedAdsManager.Instance.ShowRewarded();
        }
    }

    [ContextMenu("📊 Show Status")]
    private void ShowStatus()
    {
        Debug.Log("========================================");
        Debug.Log($"Batch: {currentBatch}/{GetTotalBatches()}");
        Debug.Log($"Progress: {totalPlacedInCurrentBatch}/{GetBatchSize(currentBatch)}");
        Debug.Log($"Completed: {batchesCompleted} batches");
        Debug.Log($"Game Progress: {CalculateGameProgressPercentage():F1}%");
        Debug.Log($"Items Remaining: {CalculateTotalRemainingItems()}");
        Debug.Log($"Ads: {showAdsOnBatchComplete}");
        Debug.Log($"Frequency: every {adFrequency} batches");
        Debug.Log("========================================");
    }

    [ContextMenu("🔍 Check UI")]
    private void CheckUI()
    {
        Debug.Log("========================================");
        Debug.Log("=== UI SETUP ===");
        Debug.Log($"Completion Panel: {(completionPanel != null ? "✅" : "❌")}");
        Debug.Log($"Completion Text: {(completionText != null ? "✅" : "❌")}");
        Debug.Log($"Game Progress Text: {(gameProgressText != null ? "✅" : "❌")}");
        Debug.Log($"Batch Progress Text: {(currentBatchProgressText != null ? "✅" : "❌")}");
        Debug.Log($"Progress Slider: {(progressSlider != null ? "✅" : "❌")}");
        
        if (completionPanel != null)
            Debug.Log($"Panel Active: {completionPanel.activeSelf}");
        
        Debug.Log("========================================");
    }

    [ContextMenu("🔄 Force Update Progress UI")]
    private void ForceUpdateProgressUI()
    {
        Debug.Log("🔄 Manually updating Progress UI...");
        UpdateProgressUI();
        Debug.Log("✅ Progress UI updated!");
    }

    [ContextMenu("🔧 Show Config")]
    private void PrintBatchConfiguration()
    {
        Debug.Log("=== CONFIG ===");
        Debug.Log($"Mode: {(useCustomBatchSizes ? "Custom" : "Simple")}");
        Debug.Log($"Batches: {GetTotalBatches()}");
        Debug.Log($"Total spots: {GetTotalRequiredSpots()}");
    }
}