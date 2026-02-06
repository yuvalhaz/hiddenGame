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

    [Header("Return Reminder")]
    [SerializeField] private float returnReminderHours = 24f;
    [Tooltip("Hours after leaving the game to send a 'come back' reminder")]
    [SerializeField] private string returnReminderTitle = "We miss you!";
    [SerializeField] private string returnReminderText = "Your puzzles are waiting! Come back and find the hidden objects.";

    [Header("Daily Challenge")]
    [SerializeField] private bool enableDailyReminder = true;
    [SerializeField] private int dailyReminderHour = 19;
    [Tooltip("Hour of the day (0-23) to send daily reminder")]
    [SerializeField] private string dailyReminderTitle = "Daily Puzzle Time!";
    [SerializeField] private string dailyReminderText = "Ready for today's challenge? New puzzles are waiting for you!";

    [Header("Level Progress Reminder")]
    [SerializeField] private float progressReminderHours = 48f;
    [Tooltip("Hours after leaving mid-level to remind about unfinished level")]
    [SerializeField] private string progressReminderTitle = "Unfinished puzzle!";
    [SerializeField] private string progressReminderText = "You were so close! Come back and finish your puzzle.";

    private const string NOTIFICATIONS_ENABLED_KEY = "NotificationsEnabled";
    private const string ANDROID_CHANNEL_ID = "hidden_game_channel";
    private const string RETURN_REMINDER_ID_KEY = "ReturnReminderNotifId";
    private const string DAILY_REMINDER_ID_KEY = "DailyReminderNotifId";
    private const string PROGRESS_REMINDER_ID_KEY = "ProgressReminderNotifId";

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
                ScheduleReturnReminder();
                ScheduleDailyReminder();
                ScheduleProgressReminder();

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
            ScheduleReturnReminder();
            ScheduleDailyReminder();
            ScheduleProgressReminder();

            if (debugMode)
                Debug.Log("[PushNotificationManager] App quitting - notifications scheduled");
        }
    }

    // ===== SCHEDULING =====

    /// <summary>
    /// Schedule a "come back and play" reminder
    /// </summary>
    private void ScheduleReturnReminder()
    {
        var fireTime = System.DateTime.Now.AddHours(returnReminderHours);

#if UNITY_ANDROID
        var notification = new AndroidNotification()
        {
            Title = returnReminderTitle,
            Text = returnReminderText,
            FireTime = fireTime,
            SmallIcon = "icon_small",
            LargeIcon = "icon_large",
        };
        int id = AndroidNotificationCenter.SendNotification(notification, ANDROID_CHANNEL_ID);
        PlayerPrefs.SetInt(RETURN_REMINDER_ID_KEY, id);
#endif

#if UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = System.TimeSpan.FromHours(returnReminderHours),
            Repeats = false,
        };
        var notification = new iOSNotification()
        {
            Identifier = "return_reminder",
            Title = returnReminderTitle,
            Body = returnReminderText,
            ShowInForeground = false,
            Trigger = timeTrigger,
        };
        iOSNotificationCenter.ScheduleNotification(notification);
#endif

        if (debugMode)
            Debug.Log($"[PushNotificationManager] Return reminder scheduled for {fireTime}");
    }

    /// <summary>
    /// Schedule a daily reminder at the configured hour
    /// </summary>
    private void ScheduleDailyReminder()
    {
        if (!enableDailyReminder) return;

        // Calculate next occurrence of the daily reminder hour
        var now = System.DateTime.Now;
        var fireTime = new System.DateTime(now.Year, now.Month, now.Day, dailyReminderHour, 0, 0);

        // If the time already passed today, schedule for tomorrow
        if (fireTime <= now)
        {
            fireTime = fireTime.AddDays(1);
        }

#if UNITY_ANDROID
        var notification = new AndroidNotification()
        {
            Title = dailyReminderTitle,
            Text = dailyReminderText,
            FireTime = fireTime,
            SmallIcon = "icon_small",
            LargeIcon = "icon_large",
        };
        int id = AndroidNotificationCenter.SendNotification(notification, ANDROID_CHANNEL_ID);
        PlayerPrefs.SetInt(DAILY_REMINDER_ID_KEY, id);
#endif

#if UNITY_IOS
        var calendarTrigger = new iOSNotificationCalendarTrigger()
        {
            Hour = dailyReminderHour,
            Minute = 0,
            Repeats = false,
        };
        var notification = new iOSNotification()
        {
            Identifier = "daily_reminder",
            Title = dailyReminderTitle,
            Body = dailyReminderText,
            ShowInForeground = false,
            Trigger = calendarTrigger,
        };
        iOSNotificationCenter.ScheduleNotification(notification);
#endif

        if (debugMode)
            Debug.Log($"[PushNotificationManager] Daily reminder scheduled for {fireTime}");
    }

    /// <summary>
    /// Schedule a reminder about unfinished level progress
    /// </summary>
    private void ScheduleProgressReminder()
    {
        // Only schedule if the player has an unfinished level
        if (GameProgressManager.Instance == null) return;

        var progressData = GameProgressManager.Instance.GetProgressData();
        if (progressData == null || progressData.placedItems == null || progressData.placedItems.Count == 0)
            return; // No progress to remind about

        var fireTime = System.DateTime.Now.AddHours(progressReminderHours);

        // Customize message with level info
        string title = progressReminderTitle;
        string text = progressReminderText;

        if (LevelManager.Instance != null)
        {
            int levelNum = LevelManager.Instance.GetCurrentLevelNumber();
            text = $"You were working on Level {levelNum}. Come back and complete it!";
        }

#if UNITY_ANDROID
        var notification = new AndroidNotification()
        {
            Title = title,
            Text = text,
            FireTime = fireTime,
            SmallIcon = "icon_small",
            LargeIcon = "icon_large",
        };
        int id = AndroidNotificationCenter.SendNotification(notification, ANDROID_CHANNEL_ID);
        PlayerPrefs.SetInt(PROGRESS_REMINDER_ID_KEY, id);
#endif

#if UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = System.TimeSpan.FromHours(progressReminderHours),
            Repeats = false,
        };
        var notification = new iOSNotification()
        {
            Identifier = "progress_reminder",
            Title = title,
            Body = text,
            ShowInForeground = false,
            Trigger = timeTrigger,
        };
        iOSNotificationCenter.ScheduleNotification(notification);
#endif

        if (debugMode)
            Debug.Log($"[PushNotificationManager] Progress reminder scheduled for {fireTime}");
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
            Identifier = $"custom_{System.DateTime.Now.Ticks}",
            Title = title,
            Body = text,
            ShowInForeground = false,
            Trigger = timeTrigger,
        };
        iOSNotificationCenter.ScheduleNotification(notification);
#endif

        if (debugMode)
            Debug.Log($"[PushNotificationManager] Custom notification scheduled: '{title}' at {fireTime}");
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
        Debug.Log($"Return Reminder: {returnReminderHours}h");
        Debug.Log($"Daily Reminder: {enableDailyReminder} at {dailyReminderHour}:00");
        Debug.Log($"Progress Reminder: {progressReminderHours}h");
        Debug.Log("===========================");
    }
}
