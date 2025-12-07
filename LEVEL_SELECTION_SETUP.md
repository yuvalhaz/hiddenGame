# 🎮 מדריך הקמת מסך בחירת Levels

## 📋 מבנה המסך

```
Canvas
└── LevelSelectionPanel (Panel)
    ├── GameLogo (Image) - לוגו המשחק
    ├── TitleText (Text) - "בחר שלב"
    ├── Background (Image) - תמונת רקע
    └── LevelButtonsContainer (Empty GameObject + Grid Layout Group)
        └── [הכפתורים ייווצרו אוטומטית על ידי הסקריפט]
```

## 🔧 שלבי הבנייה ב-Unity

### 1. צור Canvas חדש
1. Right-click in Hierarchy → UI → Canvas
2. שנה Canvas Scaler ל-"Scale With Screen Size"
3. Reference Resolution: 1920x1080

### 2. צור Panel ראשי
1. Right-click על Canvas → UI → Panel
2. שם: `LevelSelectionPanel`
3. Anchor: Stretch (Full Screen)
4. צבע: שקוף או צבע רקע שאתה רוצה

### 3. הוסף לוגו (אופציונלי)
1. Right-click על Panel → UI → Image
2. שם: `GameLogo`
3. Anchor: Top Center
4. גודל: 400x200 (למשל)
5. Position Y: -100 (מלמעלה)

### 4. הוסף כותרת
1. Right-click על Panel → UI → Text
2. שם: `TitleText`
3. טקסט: "בחר שלב"
4. Anchor: Top Center
5. Position Y: -300
6. Font Size: 72
7. Alignment: Center
8. צבע: לבן/שחור לפי העיצוב

### 5. צור Container לכפתורים + Grid Layout
1. Right-click על Panel → Create Empty
2. שם: `LevelButtonsContainer`
3. Anchor: Center
4. גודל: 600x300 (למשל - תלוי בגודל הכפתורים)

**הוסף Grid Layout Group:**
- Add Component → Grid Layout Group
- הגדרות מומלצות:
  - Cell Size: `100 x 100` (כפתורים קטנים)
  - Spacing: `20 x 20` (ריווח צפוף)
  - Constraint: `Fixed Column Count` = `5`
  - Child Alignment: `Middle Center`

### 6. צור Level Button Prefab

**צור כפתור לדוגמה:**
1. Right-click על Canvas → UI → Button
2. שם: `LevelButtonPrefab`

**התאם את הכפתור:**
- גודל: 100x100 (כמו Cell Size)
- Image: צבע בסיס (אפור/לבן)
- Text (הילד של Button):
  - Font Size: 48
  - Alignment: Center
  - טקסט: "1"

**שמור כ-Prefab:**
1. גרור את LevelButtonPrefab לתיקיית Assets/Prefabs
2. מחק את הכפתור מה-Hierarchy

### 7. חבר את הסקריפט

1. בחר את `LevelSelectionPanel`
2. Add Component → Level Selection UI
3. מלא את השדות:

**🎨 Visual Settings:**
- Game Logo: גרור את ה-GameLogo Image (אופציונלי)
- Title Text: גרור את ה-TitleText
- Background Image: אם יש תמונת רקע

**Level Configuration:**
- Total Levels: `10`
- Level Scene Prefix: `"Level"` (אם השמות Level1, Level2...)

**UI References:**
- Level Button Container: גרור את `LevelButtonsContainer`
- Level Button Prefab: גרור את ה-Prefab שיצרת

**🎨 Button Styling (אופציונלי):**
- Locked Icon, Unlocked Icon, Completed Icon
- Locked Color: אפור (128, 128, 128)
- Unlocked Color: לבן (255, 255, 255)
- Completed Color: ירוק (76, 255, 76)

**✨ Animation Settings:**
- Animate Buttons On Start: ✓ (מומלץ!)
- Button Animation Delay: 0.05
- Button Pop Duration: 0.3

## 🎨 טיפים לעיצוב

### צבעים מומלצים:
- **רקע:** כחול כהה או גרדיאנט
- **כפתורים נעולים:** אפור (#808080)
- **כפתורים פתוחים:** לבן או צהוב בהיר
- **כפתורים שהושלמו:** ירוק (#4CFF4C) או זהב

### אנימציות:
- הסקריפט כבר כולל אנימציית pop-in אוטומטית!
- הכפתורים יופיעו אחד אחד עם bounce effect

### לוגו:
- מומלץ PNG שקוף
- גודל מקסימלי: 512x256
- שים בתיקיית Assets/Sprites

## ✅ בדיקה

1. הרץ את המשחק
2. בדוק שהכפתורים מופיעים (10 כפתורים)
3. רק כפתור 1 צריך להיות פעיל (האחרים נעולים)
4. לחיצה על כפתור 1 צריכה לטעון את Level1

## 🔧 Troubleshooting

**הכפתורים לא נוצרים?**
- בדוק שה-Prefab מחובר
- בדוק שה-Container מחובר
- בדוק את ה-Console לשגיאות

**הכפתורים חופפים?**
- בדוק את ה-Grid Layout Group settings
- וודא ש-Cell Size גדול מספיק

**הלוגו לא מופיע?**
- בדוק ש-Source Image מוגדר ב-Image component
- בדוק שה-Sprite לא null

## 📦 Scenes Required

וודא שיש לך scenes עם השמות:
- `Level1.unity`
- `Level2.unity`
- ...
- `Level10.unity`

**או שנה את Level Scene Prefix בסקריפט!**
