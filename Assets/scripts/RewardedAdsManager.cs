using System;
using UnityEngine;

public class RewardedAdsManager : MonoBehaviour
{
    public static RewardedAdsManager Instance;

    /// <summary>נורה כשהתקבל Reward.</summary>
    public event Action OnRewardGranted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ===================== Availability / Preload =====================

    public bool IsReady()
    {
#if UNITY_EDITOR
        return true;
#else
        // TODO: החזר true רק כשהמודעה באמת טעונה.
        return true;
#endif
    }

    public void Preload(Action<bool> onLoaded = null)
    {
#if UNITY_EDITOR
        try { onLoaded?.Invoke(true); } catch (Exception e) { Debug.LogException(e); }
#else
        // TODO: טען מודעת Rewarded אמיתית ואז קרא onLoaded(true/false)
        try { onLoaded?.Invoke(true); } catch (Exception e) { Debug.LogException(e); }
#endif
    }

    // ===================== PUBLIC API =====================

    // 0 args – בטוח, נשענים על OnRewardGranted
    public void ShowRewarded()
        => ShowRewarded(onReward: null, onClosed: (Action<bool>)null, onFailed: (Action<string>)null, onOpened: null);

    // ----- 3 args (תאימות לקריאות קיימות) -----
    public void ShowRewarded(Action onReward, Action onClosed, Action onFailed)
        => ShowRewarded(onReward,
                        onClosed: completed => { SafeInvoke(onClosed); },
                        onFailed: err => { SafeInvoke(onFailed); },
                        onOpened: null);

    public void ShowRewarded(Action onReward, Action<bool> onClosed, Action<string> onFailed)
        => ShowRewarded(onReward, onClosed, onFailed, onOpened: null);

    public void ShowRewarded(Action onReward, Action<bool> onClosed, Action onFailed)
        => ShowRewarded(onReward,
                        onClosed: onClosed,
                        onFailed: err => { SafeInvoke(onFailed); },
                        onOpened: null);

    public void ShowRewarded(Action onReward, Action onClosed, Action<string> onFailed)
        => ShowRewarded(onReward,
                        onClosed: completed => { SafeInvoke(onClosed); },
                        onFailed: onFailed,
                        onOpened: null);

    // ----- 4 args (מלא) – כל הקומבינציות מכוונות לכאן -----
    public void ShowRewarded(Action onReward, Action onClosed, Action onFailed, Action onOpened)
        => ShowRewarded(onReward,
                        onClosed: completed => { SafeInvoke(onClosed); },
                        onFailed: err => { SafeInvoke(onFailed); },
                        onOpened: onOpened);

    public void ShowRewarded(Action onReward, Action<bool> onClosed, Action<string> onFailed, Action onOpened)
        => ShowRewardedInternal(
            onEarned: () => { SafeInvoke(OnRewardGranted); SafeInvoke(onReward); },
            onClosed: onClosed,
            onFailed: onFailed,
            onOpened: onOpened
        );

    public void ShowRewarded(Action onReward, Action<bool> onClosed, Action onFailed, Action onOpened)
        => ShowRewarded(onReward,
                        onClosed: onClosed,
                        onFailed: err => { SafeInvoke(onFailed); },
                        onOpened: onOpened);

    public void ShowRewarded(Action onReward, Action onClosed, Action<string> onFailed, Action onOpened)
        => ShowRewarded(onReward,
                        onClosed: completed => { SafeInvoke(onClosed); },
                        onFailed: onFailed,
                        onOpened: onOpened);

    // ===== Aliases (ShowHintAd) =====
    public void ShowHintAd()                                                     => ShowRewarded();
    public void ShowHintAd(Action onReward, Action onClosed, Action onFailed)    => ShowRewarded(onReward, onClosed, onFailed);
    public void ShowHintAd(Action onReward, Action<bool> onClosed, Action<string> onFailed) => ShowRewarded(onReward, onClosed, onFailed);
    public void ShowHintAd(Action onReward, Action<bool> onClosed, Action onFailed)        => ShowRewarded(onReward, onClosed, onFailed);
    public void ShowHintAd(Action onReward, Action onClosed, Action<string> onFailed)      => ShowRewarded(onReward, onClosed, onFailed);
    public void ShowHintAd(Action onReward, Action onClosed, Action onFailed, Action onOpened)
        => ShowRewarded(onReward, onClosed, onFailed, onOpened);
    public void ShowHintAd(Action onReward, Action<bool> onClosed, Action<string> onFailed, Action onOpened)
        => ShowRewarded(onReward, onClosed, onFailed, onOpened);
    public void ShowHintAd(Action onReward, Action<bool> onClosed, Action onFailed, Action onOpened)
        => ShowRewarded(onReward, onClosed, onFailed, onOpened);
    public void ShowHintAd(Action onReward, Action onClosed, Action<string> onFailed, Action onOpened)
        => ShowRewarded(onReward, onClosed, onFailed, onOpened);

    // ===================== INTERNAL =====================

    private void ShowRewardedInternal(
        Action onEarned,
        Action<bool> onClosed,
        Action<string> onFailed,
        Action onOpened)
    {
#if UNITY_EDITOR
        Debug.Log("[RewardedAdsManager] Editor simulate: opened -> reward -> closed(true).");
        SafeInvoke(onOpened);
        SafeInvoke(onEarned);
        SafeInvoke(onClosed, true);
#else
        // TODO: חבר Google Mobile Ads:
        // rewardedAd.OnAdFullScreenContentOpened += () => SafeInvoke(onOpened);
        // rewardedAd.OnAdFullScreenContentClosed += () => SafeInvoke(onClosed, /*completed:*/ true);
        // rewardedAd.OnAdFailedToPresentFullScreenContent += err => SafeInvoke(onFailed, err.ToString());
        // rewardedAd.Show(reward => SafeInvoke(onEarned));

        // סימולציה זמנית:
        SafeInvoke(onOpened);
        SafeInvoke(onEarned);
        SafeInvoke(onClosed, true);
#endif
    }

    // ===================== Utils =====================

    private static void SafeInvoke(Action a)
    {
        try { a?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
    }
    private static void SafeInvoke(Action<bool> a, bool v)
    {
        try { a?.Invoke(v); } catch (Exception e) { Debug.LogException(e); }
    }
    private static void SafeInvoke(Action<string> a, string v)
    {
        try { a?.Invoke(v); } catch (Exception e) { Debug.LogException(e); }
    }
}
