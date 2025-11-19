# הוראות התקנה - פרסומות Google AdMob עם Test Ads

המדריך הזה יעזור לך להתקין ולבדוק פרסומות Google AdMob במשחק Unity שלך.

---

## שלב 1: התקנת Google Mobile Ads SDK 📦

### אופציה א' - דרך Unity Package Manager (מומלץ):

1. פתח את Unity Editor
2. לך ל-`Window` → `Package Manager`
3. לחץ על `+` בפינה השמאלית העליונה
4. בחר `Add package from git URL`
5. הדבק את ה-URL הבא:
   ```
   https://github.com/googleads/googleads-mobile-unity.git
   ```
6. לחץ `Add`

### אופציה ב' - הורדה ידנית:

1. גש ל-[Google Mobile Ads Unity Plugin](https://github.com/googleads/googleads-mobile-unity/releases)
2. הורד את הגרסה האחרונה (`.unitypackage`)
3. ב-Unity: `Assets` → `Import Package` → `Custom Package`
4. בחר את הקובץ שהורדת ויבא אותו

---

## שלב 2: הגדרת הסצנה 🎬

### 2.1 יצירת GameObject לפרסומות:

1. בסצנה הראשית שלך (או בסצנת התחלה), צור GameObject חדש
2. קרא לו `AdManager`

### 2.2 הוספת הסקריפטים:

1. הוסף את `AdMobConfig` ל-GameObject
2. הוסף את `RewardedAdsManager` ל-GameObject
3. הוסף את `AdInit` ל-GameObject (אם קיים)

### 2.3 חיבור הרפרנסים:

1. בחר את ה-GameObject `AdManager`
2. ב-Inspector, גרור את `AdMobConfig` לשדה `adMobConfig` של `RewardedAdsManager`
3. ב-`AdInit`, גרור את `RewardedAdsManager` לשדה המתאים

---

## שלב 3: הגדרת AdMobConfig 🎛️

בחר את ה-GameObject `AdManager` וב-Inspector תראה את `AdMobConfig`:

### הגדרות חשובות:

✅ **Use Test Ads**: סמן ✓ (כדי להשתמש ב-Test Ads של Google)

**Test Ad Unit IDs** (כבר מוגדרים):
- Android: `ca-app-pub-3940256099942544/5224354917`
- iOS: `ca-app-pub-3940256099942544/1712485313`

**AdMob App IDs** (כבר מוגדרים):
- Android: `ca-app-pub-3940256099942544~3347511713`
- iOS: `ca-app-pub-3940256099942544~1458002511`

---

## שלב 4: בניית APK לבדיקה 🔨

### 4.1 הגדרות Android:

1. `File` → `Build Settings`
2. בחר `Android` ולחץ `Switch Platform`
3. `Player Settings` → `Other Settings`:
   - **Minimum API Level**: לפחות Android 5.0 (API 21)
   - **Target API Level**: העדכני ביותר (33+)

### 4.2 בניה:

1. `Build Settings` → `Build`
2. שמור את ה-APK
3. העבר למכשיר Android והתקן

---

## שלב 5: בדיקה על מכשיר 📱

### מה צפוי לקרות:

כשתלחץ על כפתור שמציג פרסומת Rewarded:

1. ✅ הפרסומת תטען (יתכן שייקח כמה שניות)
2. ✅ תראה פרסומת אמיתית של Google (במצב טסט)
3. ✅ הפרסומת תהיה מסומנת כ-"Test Ad" בפינה
4. ✅ תוכל לסגור או לצפות במלואה
5. ✅ אם צפית עד הסוף - תקבל את הרוורד

### בדיקת Logs:

חבר את המכשיר ל-Android Studio או השתמש ב-`adb logcat`:

```bash
adb logcat -s Unity:V GoogleMobileAds:V
```

**Logs מצופים:**
```
[RewardedAdsManager] AdMob initialized
[RewardedAdsManager] Running in TEST MODE with Google demo ads
[RewardedAdsManager] Loading ad with ID: ca-app-pub-3940256099942544/5224354917
[RewardedAdsManager] Ad loaded successfully!
```

---

## שלב 6: מעבר לפרסומות אמיתיות (לפרודקשן) 🚀

### כשאתה מוכן לפרסם לחנות:

1. **צור חשבון AdMob**: [admob.google.com](https://admob.google.com)
2. **צור אפליקציה חדשה** ב-AdMob Console
3. **צור Ad Unit** מסוג "Rewarded"
4. **קבל את ה-IDs**:
   - App ID (מתחיל ב-`ca-app-pub-XXXXXXXXXXXXXXXX~XXXXXXXXXX`)
   - Ad Unit ID (מתחיל ב-`ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX`)

5. **עדכן את AdMobConfig**:
   - הכנס את ה-Production Ad Unit IDs בשדות המתאימים
   - **בטל את הסימון** של "Use Test Ads" ❌
   - הכנס את ה-App IDs האמיתיים שלך

6. **בנה מחדש ובדוק!**

---

## פתרון בעיות 🔧

### הפרסומת לא נטענת:

✅ וודא שאתה מחובר לאינטרנט
✅ בדוק את ה-Logs ב-`adb logcat`
✅ וודא ש-API Level מספיק גבוה (21+)
✅ וודא ש-`AdMobConfig` מחובר ל-`RewardedAdsManager`

### שגיאת "Invalid Ad Unit ID":

✅ בדוק שה-ID נכון ב-`AdMobConfig`
✅ וודא שאתה בונה לפלטפורמה הנכונה (Android/iOS)

### הפרסומת לא מוצגת:

✅ וודא ש-`IsReady()` מחזיר `true` לפני `ShowRewarded()`
✅ קרא ל-`Preload()` מראש כדי לטעון פרסומת

---

## דוגמת קוד - שימוש ב-RewardedAdsManager 💻

```csharp
using UnityEngine;

public class HintButton : MonoBehaviour
{
    public void OnHintButtonClicked()
    {
        var adsManager = RewardedAdsManager.Instance;

        if (adsManager == null)
        {
            Debug.LogError("RewardedAdsManager not found!");
            return;
        }

        if (!adsManager.IsReady())
        {
            Debug.Log("Ad is not ready, loading...");
            adsManager.Preload(success =>
            {
                if (success)
                    ShowAd();
            });
            return;
        }

        ShowAd();
    }

    private void ShowAd()
    {
        RewardedAdsManager.Instance.ShowRewarded(
            onReward: () =>
            {
                Debug.Log("User earned reward!");
                // תן רמז למשתמש
                GiveHintToUser();
            },
            onClosed: (completed) =>
            {
                Debug.Log($"Ad closed. Completed: {completed}");
            },
            onFailed: (error) =>
            {
                Debug.LogError($"Ad failed: {error}");
            }
        );
    }

    private void GiveHintToUser()
    {
        // הלוגיקה שלך לרמז
    }
}
```

---

## קבצים שנוצרו 📄

1. **`AdMobConfig.cs`** - מכיל את כל ה-IDs וההגדרות
2. **`RewardedAdsManager.cs`** - מעודכן עם קוד אמיתי של Google Ads
3. **`AdInit.cs`** - אתחול אוטומטי (קיים)

---

## תמיכה ותיעוד נוסף 📚

- [Google Mobile Ads Unity Documentation](https://developers.google.com/admob/unity/start)
- [Rewarded Ads Implementation Guide](https://developers.google.com/admob/unity/rewarded)
- [Test Ads Guide](https://developers.google.com/admob/unity/test-ads)

---

**בהצלחה! 🎉**

אם יש בעיות או שאלות, בדוק את ה-Logs תמיד - הם יגידו לך בדיוק מה קורה!
