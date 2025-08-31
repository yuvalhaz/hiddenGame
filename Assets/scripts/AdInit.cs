using System;                     // חשוב בשביל Action<>
using GoogleMobileAds.Api;
using UnityEngine;

public class AdInit : MonoBehaviour
{
    private BannerView banner;

    void Start()
    {
        // אתחול עם callback נדרש
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // אחרי שה-SDK מאותחל — טען מודעה
            LoadBanner();
        });
    }

    private void LoadBanner()
    {
        // Test Ad Unit ID (להחליף לשלך בפרודקשן)
        string testId = "ca-app-pub-3940256099942544/6300978111";

        // אם היה באנר קודם, ננקה
        if (banner != null)
        {
            banner.Destroy();
            banner = null;
        }

        // צור באנר וטעינה
        banner = new BannerView(testId, AdSize.Banner, AdPosition.Bottom);
        AdRequest request = new AdRequest();   // ב-v10+ אין Builder
        banner.LoadAd(request);
    }
}
