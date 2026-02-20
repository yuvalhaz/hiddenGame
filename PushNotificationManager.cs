using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

/// <summary>
/// Manages push notifications for Android and iOS.
/// Singleton pattern - persists between scenes.
/// Requires Unity Mobile Notifications package (com.unity.mobile-notifications).
/// </summary>
public class PushNotificationManager : MonoBehaviour
{
    public static PushNotificationManager Instance { get; private set; }

    [Header("Notification Settings")]
    [SerializeField] private bool notificationsEnabled = true;
    [SerializeField] private bool debugMode = false;

    [Header("24h - Return Reminder")]
    [SerializeField] private string returnReminderTitle = "We miss you!";
    [SerializeField] private string returnReminderText = "Your puzzles are waiting! Come back and find the hidden objects.";

    [Header("48h - Second Reminder")]
    [SerializeField] private string secondReminderTitle = "Your puzzles miss you!";
    [SerializeField] private string secondReminderText = "It's been a while... Your hidden objects are still waiting to be found!";

    [Header("Weekly - Recurring Reminder")]
    [SerializeField] private int weeklyRemindersCount = 4;
    [Tooltip("How many weekly reminders to schedule (e.g. 4 = up to 4 weeks)")]
    [SerializeField] private string weeklyReminderTitle = "Come back and play!";
    [SerializeField] private string weeklyReminderText = "New challenges await! Can you find all the hidden objects?";

    [Header("Level Progress Reminder (48h)")]
    [SerializeField] private string progressReminderTitle = "Unfinished puzzle!";
    [SerializeField] private string progressReminderText = "You were so close! Come back and finish your puzzle.";

    private const string NOTIFICATIONS_ENABLED_KEY = "NotificationsEnabled";
    private const string ANDROID_CHANNEL_ID = "hidden_game_channel";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved preference
        notificationsEnabled = PlayerPrefs.GetInt(NOTIFICATIONS_ENABLED_KEY, 1) == 1;

        InitializeNotifications();

        if (debugMode)
            Debug.Log($"[PushNotificationManager] Initialized. Notifications enabled: {notificationsEnabled}");
    }

    private void Start()
    {
        // Cancel any existing notifications when the app is opened
        CancelAllScheduledNotifications();

        if (debugMode)
            Debug.Log("[PushNotificationManager] Cleared pending notifications on app start");
    }

    /// <summary>
    /// Initialize the notification system per platform
    /// </summary>
    private void InitializeNotifications()
    {
#if UNITY_ANDROID
        // Create Android notification channel
        var channel = new AndroidNotificationChannel()
        {
            Id = ANDROID_CHANNEL_ID,
            Name = "Game Notifications",
            Importance = Importance.Default,
            Description = "Reminders and game updates",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        if (debugMode)
            Debug.Log("[PushNotificationManager] Android notification channel registered");
#endif

#if UNITY_IOS
        // Request authorization on iOS
        StartCoroutine(RequestIOSAuthorization());
#endif
    }

#if UNITY_IOS
    private System.Collections.IEnumerator RequestIOSAuthorization()
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            }

            if (debugMode)
            {
                Debug.Log($"[PushNotificationManager] iOS authorization: {req.Granted}");
                if (!string.IsNullOrEmpty(req.Error))
                    Debug.LogWarning($"[PushNotificationManager] iOS auth error: {req.Error}");
            }
        }
    }
#endif

    /// <summary>
    /// Called when the app is paused (going to background) or resumed
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // App going to background - schedule notifications
            if (notificationsEnabled)
            {
                ScheduleAllReminders();

                if (debugMode)
                    Debug.Log("[PushNotificationManager] App paused - notifications scheduled");
            }
        }
        else
        {
            // App resuming - cancel pending notifications
            CancelAllScheduledNotifications();

            if (debugMode)
                Debug.Log("[PushNotificationManager] App resumed - notifications cancelled");
        }
    }

    /// <summary>
    /// Called when the app is about to quit
    /// </summary>
    private void OnApplicationQuit()
    {
        if (notificationsEnabled)
        {
            ScheduleAllReminders();

            if (debugMode)
                Debug.Log("[PushNotificationManager] App quitting - notifications scheduled");
        }
    }

    // ===== SCHEDULING =====

    /// <summary>
    /// Schedule all reminders when app goes to background/quits:
    /// 24h  - "We miss you!"
    /// 48h  - Second nice message (or unfinished level reminder)
    /// 7d, 14d, 21d, 28d - Weekly "come back" reminders
    /// </summary>
    private void ScheduleAllReminders()
    {
        // 1. After 24 hours - "We miss you!"
        ScheduleNotification("return_reminder", returnReminderTitle, returnReminderText, 24);

        // 2. After 48 hours - second message (unfinished level or general)
        string title48 = secondReminderTitle;
        string text48 = secondReminderText;

        // If player has an unfinished level, customize the 48h message
        if (GameProgressManager.Instance != null)
        {
            var progressData = GameProgressManager.Instance.GetProgressData();
            if (progressData != null && progressData.placedItems != null && progressData.placedItems.Count > 0)
            {
                title48 = progressReminderTitle;
                text48 = progressReminderText;

                if (LevelManager.Instance != null)
                {
                    int levelNum = LevelManager.Instance.GetCurrentLevelNumber();
                    text48 = $"You were working on Level {levelNum}. Come back and complete it!";
                }
            }
        }
        ScheduleNotification("second_reminder", title48, text48, 48);

        // 3. Weekly reminders - every 7 days as long as they don't come back
        for (int i = 0; i < weeklyRemindersCount; i++)
        {
            float hoursDelay = (7 + i * 7) * 24f; // 168h, 336h, 504h, 672h (1w, 2w, 3w, 4w)
            ScheduleNotification($"weekly_reminder_{i}", weeklyReminderTitle, weeklyReminderText, hoursDelay);
        }

        if (debugMode)
        {
            Debug.Log("[PushNotificationManager] Scheduled: 24h, 48h, and weekly reminders");
            Debug.Log($"[PushNotificationManager] Weekly count: {weeklyRemindersCount} (up to {weeklyRemindersCount} weeks)");
        }
    }

    /// <summary>
    /// Schedule a single notification (cross-platform helper)
    /// </summary>
    private void ScheduleNotification(string identifier, string title, string text, float delayHours)
    {
        var fireTime = System.DateTime.Now.AddHours(delayHours);

#if UNITY_ANDROID
        var notification = new AndroidNotification()
        {
            Title = title,
            Text = text,
            FireTime = fireTime,
            SmallIcon = "icon_small",
            LargeIcon = "icon_large",
        };
        AndroidNotificationCenter.SendNotification(notification, ANDROID_CHANNEL_ID);
#endif

#if UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = System.TimeSpan.FromHours(delayHours),
            Repeats = false,
        };
        var notification = new iOSNotification()
        {
            Identifier = identifier,
            Title = title,
            Body = text,
            ShowInForeground = false,
            Trigger = timeTrigger,
        };
        iOSNotificationCenter.ScheduleNotification(notification);
#endif

        if (debugMode)
            Debug.Log($"[PushNotificationManager] '{identifier}' scheduled for {fireTime} ({delayHours}h)");
    }

    // ===== CANCELLATION =====

    /// <summary>
    /// Cancel all scheduled notifications
    /// </summary>
    public void CancelAllScheduledNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllScheduledNotifications();
#endif

#if UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.ApplicationBadge = 0;
#endif
    }

    // ===== PUBLIC API =====

    /// <summary>
    /// Enable or disable notifications
    /// </summary>
    public void SetNotificationsEnabled(bool enabled)
    {
        notificationsEnabled = enabled;
        PlayerPrefs.SetInt(NOTIFICATIONS_ENABLED_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (!enabled)
        {
            CancelAllScheduledNotifications();
        }

        if (debugMode)
            Debug.Log($"[PushNotificationManager] Notifications set to: {enabled}");
    }

    /// <summary>
    /// Toggle notifications on/off
    /// </summary>
    public void ToggleNotifications()
    {
        SetNotificationsEnabled(!notificationsEnabled);
    }

    /// <summary>
    /// Check if notifications are enabled
    /// </summary>
    public bool AreNotificationsEnabled()
    {
        return notificationsEnabled;
    }

    /// <summary>
    /// Schedule a one-time custom notification (e.g., for special events)
    /// </summary>
    public void ScheduleCustomNotification(string title, string text, float delayHours)
    {
        if (!notificationsEnabled) return;

        ScheduleNotification($"custom_{System.DateTime.Now.Ticks}", title, text, delayHours);
    }

    /// <summary>
    /// Schedule a notification for level completion congratulations
    /// (Called when player completes a level, reminds them to try the next one)
    /// </summary>
    public void ScheduleLevelCompleteReminder(int completedLevel)
    {
        if (!notificationsEnabled) return;

        string title = "Great job!";
        string text = $"You completed Level {completedLevel}! Ready for the next challenge?";
        float delayHours = 6f; // Remind after 6 hours

        ScheduleCustomNotification(title, text, delayHours);

        if (debugMode)
            Debug.Log($"[PushNotificationManager] Level complete reminder scheduled for Level {completedLevel}");
    }

    // ===== DEBUG =====

    [ContextMenu("Test: Schedule Return Reminder (10 sec)")]
    private void DebugScheduleReturnReminder()
    {
        ScheduleCustomNotification("Test Notification", "This is a test return reminder!", 0.003f); // ~10 seconds
        Debug.Log("[PushNotificationManager] DEBUG: Test notification scheduled for ~10 seconds");
    }

    [ContextMenu("Test: Cancel All Notifications")]
    private void DebugCancelAll()
    {
        CancelAllScheduledNotifications();
        Debug.Log("[PushNotificationManager] DEBUG: All notifications cancelled");
    }

    [ContextMenu("Show Notification Status")]
    private void DebugShowStatus()
    {
        Debug.Log("=== NOTIFICATION STATUS ===");
        Debug.Log($"Enabled: {notificationsEnabled}");
        Debug.Log($"24h: \"{returnReminderTitle}\"");
        Debug.Log($"48h: \"{secondReminderTitle}\"");
        Debug.Log($"Weekly: {weeklyRemindersCount} weeks - \"{weeklyReminderTitle}\"");
        Debug.Log("===========================");
    }
}
