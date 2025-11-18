# 📺 הוראות התקנה - פרסומות Google AdMob

## 🚀 צעדים:

### 1. התקן Google Mobile Ads SDK

**Option A: Package Manager (מומלץ)**
```
1. פתח Unity
2. Window → Package Manager
3. לחץ על + → Add package from git URL
4. הוסף: com.google.unity.ads
```

**Option B: Asset Store**
```
1. פתח Asset Store ב-Unity
2. חפש "Google Mobile Ads"
3. הורד והתקן
```

**Option C: Manual Download**
```
1. לך ל: https://github.com/googleads/googleads-mobile-unity/releases
2. הורד את הגרסה האחרונה (.unitypackage)
3. Assets → Import Package → Custom Package
```

---

### 2. הוסף Scripting Define Symbol

```
1. Edit → Project Settings → Player
2. Other Settings → Scripting Define Symbols
3. הוסף: GOOGLE_MOBILE_ADS
4. לחץ Apply
```

---

### 3. הגדר AdMob App ID

**לבדיקות (Test Mode):**
```
1. Assets → Google Mobile Ads → Settings
2. הכנס Test App IDs:
   - Android: ca-app-pub-3940256099942544~3347511713
   - iOS: ca-app-pub-3940256099942544~1458002511
```

**לפרסומות אמיתיות:**
```
1. לך ל: https://apps.admob.com
2. צור אפליקציה חדשה
3. קבל את ה-App ID שלך
4. הכנס ב-Google Mobile Ads Settings
```

---

### 4. הגדר RewardedAdsManager

```
1. לך ל-RewardedAdsManager בסצנה
2. ב-Inspector:
   ✅ Use Test Ads = true (לבדיקות)
   - Android Ad Unit Id = (ריק - ישתמש ב-Test ID)
   - iOS Ad Unit Id = (ריק - ישתמש ב-Test ID)
```

---

### 5. בדוק שזה עובד

**ב-Editor:**
- הפרסומות לא יופיעו ב-Unity Editor
- תראה לוגים: "Editor simulate: opened -> reward -> closed"

**במכשיר אמיתי (Android/iOS):**
- בנה את המשחק (Build)
- התקן על המכשיר
- כשמציגים פרסומת - תראה **"Google Test Ad"** בפינה
- זו פרסומת דמו - זה אומר שזה עובד! ✅

---

## 🎯 Test Ad Unit IDs (כבר מוגדרים בקוד):

```csharp
// Android Rewarded
"ca-app-pub-3940256099942544/5224354917"

// iOS Rewarded
"ca-app-pub-3940256099942544/1712485313"
```

---

## ⚠️ חשוב!

- **אל תשכח** לשנות ל-`Use Test Ads = false` לפני פרסום למגזין!
- **תמיד השתמש ב-Test Ads** בזמן פיתוח (אחרת החשבון שלך ייחסם)
- פרסומות דמו מסומנות ב-**"Test Ad"** בפינה

---

## 🐛 אם זה לא עובד:

1. וודא ש-`GOOGLE_MOBILE_ADS` מופיע ב-Scripting Define Symbols
2. בדוק ב-Console שיש: `[RewardedAdsManager] Google Mobile Ads initialized!`
3. וודא שיש אינטרנט על המכשיר
4. נסה לעשות Clean Build

---

✅ **מוכן! עכשיו תראה פרסומות דמו של Google כשתריץ על מכשיר אמיתי.**
