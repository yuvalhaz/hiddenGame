# 📋 תיעוד סקריפטים - Hidden Game

## תוכן עניינים
1. [ליבת המשחק](#ליבת-המשחק)
2. [מערכת Drag & Drop](#מערכת-drag--drop)
3. [מערכת Batch וניהול התקדמות](#מערכת-batch-וניהול-התקדמות)
4. [מערכת רמזים](#מערכת-רמזים)
5. [מערכת פרסומות](#מערכת-פרסומות)
6. [UI ואנימציות](#ui-ואנימציות)

---

## ליבת המשחק

### **DraggableButton.cs** (347 שורות)
**תפקיד:** הסקריפט המרכזי לכפתורים הניתנים לגרירה

**אחראי על:**
- גרירה של כפתורים מה-ButtonBar
- יצירת "רוח" (ghost) ויזואלית בזמן גרירה
- ולידציה של drop על DropSpots
- אנימציות של החזרה למקום או הצבה מוצלחת
- שמירת מצב "הוצב" ב-GameProgressManager

**Methods עיקריים:**
- `OnBeginDrag()` - מתחיל גרירה
- `OnDrag()` - מעדכן מיקום
- `OnEndDrag()` - בודק drop ומטפל בתוצאה
- `HasBeenPlaced()` - בדיקה האם הכפתור כבר הוצב
- `GetButtonID()` - מחזיר את המזהה של הכפתור

**Dependencies:**
- DragVisualManager
- DragAnimator
- DragDropValidator
- GameProgressManager

---

### **DropSpot.cs**
**תפקיד:** מייצג מקום שאפשר לשים בו כפתור

**תכונות עיקריות:**
- `spotId` - המזהה הייחודי של המקום
- `IsSettled` - האם יש כבר כפתור על המקום הזה
- בדיקת התאמה בין buttonID ל-spotId
- אינטגרציה עם ImageRevealController לחשיפת תמונה

**Methods עיקריים:**
- `AcceptButton()` - מקבל כפתור ומסמן כמאוכלס
- `IsCorrectButton()` - בודק אם הכפתור תואם למקום

---

### **ScrollableButtonBar.cs** (511 שורות)
**תפקיד:** מנהל את פס הכפתורים הגלילים

**יכולות:**
- גלילה אוטומטית או ידנית
- שיטות ScrollToButton מרובות:
  - לפי אינדקס: `ScrollToButton(int index)`
  - לפי ID: `ScrollToButton(string buttonID)`
  - לפי reference: `ScrollToButton(DraggableButton button)`
  - לפי duration: `ScrollToButton(button, float duration)`
- `ScrollToButtonCoroutine()` - לשימוש עם yield return
- אנימציות גלילה חלקות
- טעינת כפתורים דינמית

**Components נדרשים:**
- ScrollRect
- RectTransform של content panel

---

### **GameProgressManager.cs** (Singleton)
**תפקיד:** שומר ומנהל את ההתקדמות של השחקן

**אחריות:**
- שמירת מצב של אילו פריטים הוצבו (PlayerPrefs)
- ניהול batch נוכחי
- מעקב אחרי כמה פריטים הושלמו
- reset של התקדמות
- singleton pattern - גישה דרך `GameProgressManager.Instance`

**Methods עיקריים:**
- `IsItemPlaced(string itemId)` - בדיקה האם פריט הוצב
- `MarkItemPlaced(string itemId)` - סימון פריט כמוצב
- `GetCurrentBatchIndex()` - קבלת אינדקס batch נוכחי
- `ResetProgress()` - איפוס התקדמות

**שמירה:**
- משתמש ב-PlayerPrefs
- שומר HashSet של itemIds שהושלמו

---

### **LevelManager.cs** (Singleton)
**תפקיד:** מנהל רמות במשחק

**תכונות:**
- הגדרת רמות בקוד (Dictionary של itemIds)
- מעבר בין רמות
- events: `OnLevelChanged`, `OnLevelCompleted`
- אינטגרציה עם GameProgressManager ו-RewardedAdsManager

**Methods עיקריים:**
- `LoadLevel(int levelIndex)` - טוען רמה
- `GetCurrentLevel()` - מחזיר רמה נוכחית
- `IsLevelComplete()` - בודק אם רמה הושלמה

**הגדרת רמות:**
```csharp
private Dictionary<int, List<string>> levelConfig = new Dictionary<int, List<string>>()
{
    { 0, new List<string> { "spot00", "spot01", "spot02", ... } },
    { 1, new List<string> { "spot07", "spot08", "spot09", ... } },
    ...
}
```

---

## מערכת Drag & Drop

### **DragVisualManager.cs** (209 שורות)
**תפקיד:** מנהל את הווזואליה של גרירה

**אחריות:**
- יצירת "רוח" (ghost) של הכפתור
- עדכון מיקום בזמן אמת לפי העכבר
- השמדת הרוח בסוף הגרירה
- העתקת Sprite ו-RectTransform מהכפתור המקורי

**Methods עיקריים:**
- `Create(RectTransform buttonRect, MonoBehaviour host)` - יוצר ghost
- `UpdatePosition(PointerEventData eventData)` - מעדכן מיקום
- `Destroy()` - משמיד ghost

**מאפיינים:**
- שומר reference ל-ghost GameObject
- מטפל במצבי Canvas שונים (Overlay, Camera)
- תמיכה בגרירה חלקה

---

### **DragAnimator.cs** (131 שורות) - Static
**תפקיד:** אוסף אנימציות לשימוש חוזר

**Coroutines זמינות:**
- `AnimateSize(target, startSize, endSize, duration)` - שינוי גודל
- `AnimateReturnToBar(dragVisual, targetButton, duration)` - החזרה לפס
- `AnimateScaleBounce(target, bounceAmount, duration)` - bounce חמוד

**שימוש:**
```csharp
yield return StartCoroutine(DragAnimator.AnimateSize(...));
```

**יתרונות:**
- קוד נקי וניתן לשימוש חוזר
- אנימציות מחושבות עם Lerp
- AnimationCurve support

---

### **DragDropValidator.cs** (148 שורות)
**תפקיד:** מאמת את תקינות ה-drop

**אחריות:**
- Raycast למציאת DropSpot מתחת לעכבר
- בדיקת התאמת IDs (buttonID == spotID)
- החזרת סיבת כישלון (failureReason)
- ניהול raycast של DropSpots (enable/disable)

**Methods עיקריים:**
- `ValidateDrop(buttonID, dragVisual, eventData, out failureReason)` - הולידציה מלאה
- `SetDropSpotRaycastEnabled(buttonID, enabled)` - שליטה ב-raycast

**Validation Rules:**
- האם יש DropSpot מתחת לעכבר?
- האם ה-spotId תואם ל-buttonID?
- האם המקום פנוי (לא settled)?
- האם המקום פעיל?

---

### **DropSpotCache.cs** (96 שורות) - Static
**תפקיד:** מערכת cache לביצועים טובים יותר

**Methods:**
- `Get(spotId)` - מחזיר DropSpot לפי ID מה-cache
- `Refresh()` - מרענן את ה-cache (FindObjectsOfType)
- `Clear()` - מנקה את ה-cache

**יתרונות:**
- מונע FindObjectsOfType מרובים (איטי!)
- גישה מהירה ל-DropSpots לפי ID
- Dictionary-based lookup

**שימוש:**
```csharp
DropSpot spot = DropSpotCache.Get("spot05");
```

---

## מערכת Batch וניהול התקדמות

### **DropSpotBatchManager.cs** (457 שורות)
**תפקיד:** מנהל את המשחק ב-batches (קבוצות)

**אחריות:**
- מחלק את ה-DropSpots לקבוצות (למשל 7 spots לכל batch)
- פותח batch רק כשהקודם הושלם
- מציג חגיגה ואולי מודעה בסוף batch
- משתמש ב-3 helper classes

**Helper Classes:**
- `BatchProgressUI` - עדכון פס התקדמות
- `BatchCompletionCelebration` - חגיגה בסוף
- `BatchAdController` - ניהול פרסומות

**Methods עיקריים:**
- `GetCurrentBatchIndex()` - אינדקס batch נוכחי
- `GetCurrentBatchAvailableSpots()` - spots זמינים ב-batch
- `OnItemPlaced(string itemId)` - מופעל כשמציבים פריט
- `IsBatchComplete(int batchIndex)` - בדיקה אם batch הושלם

**Flow:**
```
Batch 0 (spots 0-6) → Complete → Celebration → Ad?
   ↓
Batch 1 (spots 7-13) → Complete → Celebration → Ad?
   ↓
Batch 2 (spots 14-20) → ...
```

---

### **BatchProgressUI.cs** (125 שורות) - Serializable
**תפקיד:** מנהל UI של התקדמות

**Components:**
- ProgressBar (Image) - מתמלא בהדרגה
- טקסט התקדמות (למשל "3/7")
- אנימציות למילוי פס

**Methods עיקריים:**
- `UpdateProgress(currentBatch, totalPlaced, batchSize, ...)` - עדכון מלא
- Animation של fill amount עם Lerp

**שימוש:**
מוגדר כ-[SerializeField] ב-DropSpotBatchManager

---

### **BatchCompletionCelebration.cs** (327 שורות) - Serializable
**תפקיד:** חגיגה בסוף batch

**אפקטים:**
- הצגת הודעת "כל הכבוד!" / "מצוין!"
- אנימציות scale/fade
- השמעת צלילים
- הפעלת קונפטי (UIConfetti)
- זמן תצוגה ניתן להגדרה

**Methods עיקריים:**
- `Show(batchIndex, host)` - מציג חגיגה
- `GetTotalDisplayTime()` - כמה זמן החגיגה תוצג

**Customization:**
- הודעות שונות לפי batch
- צלילים שונים
- כמות קונפטי משתנה

---

### **BatchAdController.cs** (180 שורות) - Serializable
**תפקיד:** שולט מתי להציג פרסומות

**Logic:**
- כל X batches - הצג פרסומת
- דילוג על batch 0 (בדרך כלל)
- המתנה עד שהמודעה תסתיים

**Methods עיקריים:**
- `ShouldShowAd(batchIndex)` - האם להציג מודעה אחרי batch זה?
- `ShowAdAndWait(messageDisplayTime, onAdComplete)` - מציג מודעה

**הגדרות:**
- `showAdEveryNBatches` - כל כמה batches להציג
- `skipFirstBatch` - האם לדלג על batch ראשון
- אינטגרציה עם RewardedAdsManager

---

## מערכת רמזים

### **HintButton.cs** (76 שורות)
**תפקיד:** הכפתור שלוחצים עליו לקבלת רמז

**אחריות:**
- פותח את HintDialog כשלוחצים
- מציג CanvasGroup (alpha, interactable, blocksRaycasts)
- UnityEvent: `onPressed`

**Methods עיקריים:**
- `OnClick()` - מופעל בלחיצה, פותח dialog
- `HideImmediate()` - מסתיר dialog

**Setup:**
- צריך reference ל-Button component
- צריך reference ל-CanvasGroup של HintDialog

---

### **HintDialog.cs** (134 שורות)
**תפקיד:** דיאלוג שמציע לשחקן לצפות בפרסומת לרמז

**UI Elements:**
- כפתור "צפה בפרסומת" (watchAdButton)
- כפתור סגירה (closeButton)
- CanvasGroup לשליטה בתצוגה

**Events:**
- `onHintGranted` - מופעל אחרי צפייה מוצלחת במודעה
- `onClosed` - מופעל בסגירת הדיאלוג

**Methods עיקריים:**
- `Open()` - פותח dialog
- `Close()` - סוגר dialog (עם הגנה מפני infinite loop!)
- `OnWatchAd()` - מציג מודעה rewarded
- `HandleReward()` - מטפל בתגמול אחרי מודעה

**Safety:**
משתמש ב-`isClosing` flag למניעת infinite recursion

---

### **VisualHintSystem.cs** (848 שורות) ⭐
**תפקיד:** מערכת רמזים ויזואלית מתקדמת

**אחריות:**
- מוצא אוטומטית כפתור שצריך למקם
- גוללת אליו בפס הכפתורים
- מציגה אנימציה של "רוח" שעפה ליעד
- אנימציית גדילה ביעד
- cooldown בין רמזים

**Flow של הרמז:**
```
1. TriggerHint() נקרא
   ↓
2. מחפש כפתורים זמינים ב-batch הנוכחי
   ↓
3. בוחר כפתור אקראי
   ↓
4. גולל אליו (ScrollToButtonCoroutine)
   ↓
5. יוצר ghost של הכפתור
   ↓
6. מעיף אותו ליעד עם אנימציה
   ↓
7. מגדיל ביעד
   ↓
8. מחזיר למקור
```

**Methods עיקריים:**
- `TriggerHint()` - נקודת כניסה ראשית
- `ShowHintAnimation()` - Coroutine של האנימציה
- `FindButtonsForSpots()` - מציאת כפתורים תואמים
- `RefreshDropSpotCache()` - רענון cache

**Features:**
- אינטגרציה עם DropSpotBatchManager
- תמיכה במקש H לבדיקות (enableKeyboardHint)
- המון debug logs
- אפקטי אודיו (אופציונלי)

**Setup נדרש:**
- Button Bar
- Main Canvas
- Drop Spots Container
- Batch Manager

---

### **ButtonSpotMatcher.cs** (173 שורות) 🔧
**תפקיד:** כלי עזר לבדיקת התאמות (Debug Tool)

**Context Menu Commands:**
- `[ContextMenu] "🔍 בדוק התאמות"` - בודק אם כל כפתור יש לו DropSpot
- `[ContextMenu] "🔧 תקן שמות אוטומטית"` - מתקן spotIds
- `[ContextMenu] "📝 ייצא רשימת התאמות"` - מציג טבלת התאמות

**שימושים:**
- דיבאג של אי-התאמות בין כפתורים ל-spots
- תיקון אוטומטי של שמות (Editor only)
- ולידציה של setup

**איך להשתמש:**
1. צרף ל-GameObject בסצנה
2. חבר Button Bar ו-Drop Spots Container
3. לחץ ימני על component → Context Menu → בחר פעולה

**Output:**
מציג ב-Console רשימה מפורטת של כל הכפתורים וה-spots והתאמות ביניהם

---

## מערכת פרסומות

### **RewardedAdsManager.cs** (Singleton)
**תפקיד:** מנהל פרסומות rewarded (Unity Ads)

**אחריות:**
- טעינת מודעות rewarded
- הצגת מודעות
- טיפול בקולבקים (success, fail, skip)
- Event: `OnRewardGranted` - מופעל כשהשחקן סיים לצפות

**Methods עיקריים:**
- `ShowRewarded()` - מציג מודעה
- `LoadRewarded()` - טוען מודעה מראש
- Callbacks: OnAdLoaded, OnAdShown, OnAdClosed, OnAdFailed

**Setup:**
- Game ID (Android/iOS)
- Placement ID
- Test mode toggle

**Integration:**
```csharp
RewardedAdsManager.Instance.OnRewardGranted += MyRewardMethod;
RewardedAdsManager.Instance.ShowRewarded();
```

---

### **AdInit.cs** (~50 שורות)
**תפקיד:** מאתחל את מערכת הפרסומות

**אחריות:**
- טוען מודעות מראש עם התחלת המשחק
- `DontDestroyOnLoad` - נשאר בין סצנות
- מחפש אוטומטית את RewardedAdsManager אם לא מחובר

**Settings:**
- `preloadOnStart` - לטעון מראש?
- `dontDestroyOnLoad` - לשמור בין סצנות?

**שימוש:**
צור GameObject עם AdInit בסצנה הראשונה

---

## UI ואנימציות

### **UIConfetti.cs** (כלי סטטי)
**תפקיד:** אפקט קונפטי מבוסס UI (ללא Particle System)

**שימוש:**
```csharp
UIConfetti.Burst(canvas, targetRect, count: 100, duration: 1.2f);
```

**Features:**
- עובד עם Screen Space Overlay
- קונפטי צבעוני (צבעים אקראיים)
- פיזיקה פשוטה (כוח משיכה, מהירות)
- מתאים לחגיגות!

**איך זה עובד:**
1. יוצר GameObject זמני מתחת ל-Canvas
2. משגר X פיסות קונפטי לכיוונים אקראיים
3. מוחק הכל אחרי duration

---

### **ImageRevealController.cs**
**תפקיד:** שולט בחשיפת תמונה ב-DropSpot

**אחריות:**
- חשיפה הדרגתית של תמונה כשמציבים כפתור
- אנימציות reveal (fade, scale)
- שימוש ב-ItemRevealConfig לקונפיגורציה
- אפקטי audio

**Methods עיקריים:**
- `RevealImage()` - מתחיל חשיפה
- `GetBackgroundImage()` - מחזיר Image component

**Components:**
- צריך Image component לתמונת רקע
- אופציונלי: AudioSource

---

### **ItemRevealConfig.cs** (ScriptableObject)
**תפקיד:** הגדרות לאנימציות reveal

**Settings:**
- `revealDuration` - משך חשיפה (שניות)
- `revealCurve` - AnimationCurve
- `hiddenTint` - צבע במצב מוסתר
- `useScalePop` - האם להשתמש ב-scale animation
- `scalePopAmount` - כמה להגדיל (0.15 = 15%)
- `revealSound` - AudioClip

**שימוש:**
1. Assets → Create → Game → Item Reveal Config
2. התאם הגדרות
3. גרור ל-ImageRevealController

---

## 📊 סיכום לפי קטגוריות

### Core Gameplay (6 קבצים)
- DraggableButton.cs (347)
- DropSpot.cs
- ScrollableButtonBar.cs (511)
- GameProgressManager.cs
- LevelManager.cs
- DropSpotCache.cs (96)

### Drag & Drop Helpers (3 קבצים)
- DragVisualManager.cs (209)
- DragAnimator.cs (131)
- DragDropValidator.cs (148)

### Batch System (4 קבצים)
- DropSpotBatchManager.cs (457)
- BatchProgressUI.cs (125)
- BatchCompletionCelebration.cs (327)
- BatchAdController.cs (180)

### Hint System (4 קבצים)
- HintButton.cs (76)
- HintDialog.cs (134)
- VisualHintSystem.cs (848) ⭐ הכי גדול
- ButtonSpotMatcher.cs (173)

### Ads System (2 קבצים)
- RewardedAdsManager.cs
- AdInit.cs

### UI & Effects (3 קבצים)
- UIConfetti.cs
- ImageRevealController.cs
- ItemRevealConfig.cs

---

## 🎯 הסקריפטים החשובים ביותר

### Top 5 Must-Know:
1. **DraggableButton** - ליבת מכניקת הגרירה
2. **DropSpotBatchManager** - מנוע ההתקדמות במשחק
3. **VisualHintSystem** - מערכת הרמזים המתקדמת
4. **GameProgressManager** - שמירת התקדמות
5. **ScrollableButtonBar** - ניהול פס הכפתורים

### Architecture Patterns:
- **Singleton**: GameProgressManager, RewardedAdsManager, LevelManager
- **Static Utilities**: DragAnimator, DropSpotCache, UIConfetti
- **Serializable Helpers**: BatchProgressUI, BatchCompletionCelebration, BatchAdController
- **ScriptableObject**: ItemRevealConfig

---

## 📝 הערות חשובות

### Refactoring שבוצע:
המשחק עבר refactoring גדול:
- **Before**: 2 "god classes" (825+ ו-936+ שורות)
- **After**: 7 helper classes ממוקדות
- **Result**: קוד נקי יותר, SOLID principles

### Helper Classes שנוצרו:
1. DragVisualManager - ניהול רוח הגרירה
2. DragAnimator - אנימציות לשימוש חוזר
3. DragDropValidator - לוגיקת ולידציה
4. DropSpotCache - cache לביצועים
5. BatchProgressUI - UI התקדמות
6. BatchCompletionCelebration - חגיגות
7. BatchAdController - ניהול מודעות

### תיקוני Bugs:
1. ✅ Infinite loop ב-HintDialog.Close() - תוקן עם `isClosing` flag
2. ✅ ScrollToButton compilation errors - נוספו overloads
3. ✅ ScrollToButtonCoroutine - תוקן yield return issue
4. ✅ Hebrew comments - תורגמו לאנגלית
5. ✅ .gitignore case sensitivity - תוקן

---

## 🔗 Dependencies Graph

```
DraggableButton
  ├─ DragVisualManager
  ├─ DragAnimator
  ├─ DragDropValidator
  │   └─ DropSpotCache
  └─ GameProgressManager

DropSpotBatchManager
  ├─ BatchProgressUI
  ├─ BatchCompletionCelebration
  │   └─ UIConfetti
  ├─ BatchAdController
  │   └─ RewardedAdsManager
  └─ GameProgressManager

VisualHintSystem
  ├─ ScrollableButtonBar
  ├─ DropSpotBatchManager
  ├─ ButtonSpotMatcher
  └─ DropSpotCache

HintDialog
  ├─ HintButton
  ├─ RewardedAdsManager
  └─ VisualHintSystem

ImageRevealController
  └─ ItemRevealConfig
```

---

**נוצר ב:** 2025-11-15
**גרסה:** 1.0
**Branch:** claude/review-game-scripts-01YBPuszeTZHbZH86qemd1BU
