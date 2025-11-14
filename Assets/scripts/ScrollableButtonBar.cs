using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class ButtonData
{
    public string buttonID;
    public Sprite buttonSprite;
}

public class ScrollableButtonBar : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private int numberOfButtons = 20;
    [SerializeField] private float buttonSpacing = 40f;
    [SerializeField] private float buttonWidth = 100f;

    [Header("Button Data")]
    [SerializeField] private List<ButtonData> buttonDataList = new List<ButtonData>();
    [SerializeField] private bool shuffleButtons = true;
    [Tooltip("כבה את זה בזמן פיתוח כדי לראות כפתורים בסדר הנכון")]

    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 10f;

    [Header("References")]
    [SerializeField] private RectTransform contentPanel;
    [SerializeField] private ScrollRect scrollRect;

    private List<DraggableButton> buttons = new List<DraggableButton>();
    private List<bool> buttonStates = new List<bool>();
    private List<Vector2> targetPositions = new List<Vector2>();
    private List<Vector2> originalPositions = new List<Vector2>();

    private Dictionary<RectTransform, bool> buttonsAnimating = new Dictionary<RectTransform, bool>();

    void Start()
    {
        if (buttonDataList.Count == 0)
        {
            for (int i = 0; i < numberOfButtons; i++)
            {
                buttonDataList.Add(new ButtonData
                {
                    buttonID = "Button_" + i,
                    buttonSprite = null
                });
            }
        }

        // ✅ ערבב את הכפתורים לפני יצירתם! (רק אם הופעל)
        if (shuffleButtons)
        {
            ShuffleButtonData();
            Debug.Log("[ScrollableButtonBar] כפתורים עורבבו");
        }
        else
        {
            Debug.Log("[ScrollableButtonBar] כפתורים בסדר מקורי (ללא ערבוב)");
        }

        CreateButtons();
    }


    void Update()
    {
        // ✅ אנימציה חלקה ורציפה בלי קפיצות!
        List<RectTransform> toRemove = new List<RectTransform>();

        foreach (var kvp in buttonsAnimating)
        {
            RectTransform rect = kvp.Key;
            if (rect == null)
            {
                toRemove.Add(rect);
                continue;
            }

            // מצא את האינדקס של הכפתור
            int index = -1;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null && buttons[i].GetComponent<RectTransform>() == rect)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1 || index >= targetPositions.Count)
            {
                toRemove.Add(rect);
                continue;
            }

            // בדוק אם הכפתור בגרירה
            if (buttons[index] != null && buttons[index].IsDragging())
            {
                toRemove.Add(rect);
                continue;
            }

            // ✅ תנועה חלקה עם MoveTowards - אין קפיצות!
            Vector2 currentPos = rect.anchoredPosition;
            Vector2 targetPos = targetPositions[index];

            float distance = Vector2.Distance(currentPos, targetPos);

            if (distance < 0.5f)
            {
                rect.anchoredPosition = targetPos;
                toRemove.Add(rect);
            }
            else
            {
                // ✅ תנועה חלקה במהירות קבועה
                float speed = animationSpeed * 100f; // כפול 100 כי זה פיקסלים לשנייה
                Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, speed * Time.deltaTime);
                rect.anchoredPosition = newPos;
            }
        }

        foreach (var rect in toRemove)
        {
            buttonsAnimating.Remove(rect);
        }
    }

    // ✅ פונקציה ציבורית שיכולה להיקרא מבחוץ
    public void RefreshBar()
    {
        Debug.Log("[ScrollableButtonBar] RefreshBar called");

        // ✅ עבור על כל הכפתורים ובדוק אם הם קיימים
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null)
            {
                // הכפתור נמחק - סמן אותו כלא פעיל
                buttonStates[i] = false;
                Debug.Log($"[ScrollableButtonBar] Button {i} is null - marking inactive");
            }
        }

        // ✅ חשב מחדש את כל המיקומים
        RecalculateAllPositions();

        Debug.Log("[ScrollableButtonBar] ✅ Bar refreshed!");
    }


    void CreateButtons()
    {
        while (buttonDataList.Count < numberOfButtons)
        {
            buttonDataList.Add(new ButtonData
            {
                buttonID = "Button_" + buttonDataList.Count,
                buttonSprite = null
            });
        }

        for (int i = 0; i < numberOfButtons; i++)
        {
            // ✅ בדוק אם הכפתור כבר הושם לפני שיוצרים אותו!
            bool alreadyPlaced = false;
            if (GameProgressManager.Instance != null)
            {
                alreadyPlaced = GameProgressManager.Instance.IsItemPlaced(buttonDataList[i].buttonID);
            }

            // ✅ אם הכפתור כבר הושם - אל תיצור אותו בכלל!
            if (alreadyPlaced)
            {
                buttons.Add(null);
                buttonStates.Add(false);
                targetPositions.Add(Vector2.zero);
                originalPositions.Add(Vector2.zero);
                continue; // ✅ דלג על יצירת הכפתור!
            }

            // ✅ רק אם לא הושם - צור את הכפתור
            GameObject buttonObj = Instantiate(buttonPrefab, contentPanel);
            buttonObj.name = buttonDataList[i].buttonID;

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(buttonWidth, buttonWidth);

            buttonRect.anchorMin = new Vector2(0, 0.5f);
            buttonRect.anchorMax = new Vector2(0, 0.5f);
            buttonRect.pivot = new Vector2(0, 0.5f);

            float xPos = buttonSpacing + (i * (buttonWidth + buttonSpacing));
            buttonRect.anchoredPosition = new Vector2(xPos, 0);

            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null && buttonDataList[i].buttonSprite != null)
            {
                buttonImage.sprite = buttonDataList[i].buttonSprite;
            }

            DraggableButton draggable = buttonObj.GetComponent<DraggableButton>();
            if (draggable == null)
            {
                draggable = buttonObj.AddComponent<DraggableButton>();
            }
            draggable.SetButtonBar(this, i);
            draggable.SetButtonID(buttonDataList[i].buttonID);

            buttons.Add(draggable);
            buttonStates.Add(true);
            targetPositions.Add(buttonRect.anchoredPosition);
            originalPositions.Add(buttonRect.anchoredPosition);

            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = buttonDataList[i].buttonID;
            }
        }

        // ✅ חשב מיד את המיקומים הנכונים
        RecalculateAllPositions();
        UpdateContentSize();
    }


    public void OnButtonDragStarted(DraggableButton button, int index)
    {
        Debug.Log("OnButtonDragStarted נקרא לכפתור: " + index);

        // ✅ רק עוצר אנימציות - לא משנה מצבים ולא מחשב מחדש!
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null && buttonsAnimating.ContainsKey(rect))
        {
            buttonsAnimating.Remove(rect);
        }

        // ✅ זהו! לא עושים כלום אחר כאן
    }

    public void OnButtonDraggedOut(DraggableButton button, int index)
    {
        Debug.Log("OnButtonDraggedOut נקרא לכפתור: " + index);

        // ✅ רק כאן משנים מצב וממקמים מחדש - פעם אחת בלבד!
        if (index >= 0 && index < buttonStates.Count)
        {
            // מוודא שלא נקרא פעמיים
            if (buttonStates[index] == true)
            {
                buttonStates[index] = false;
                RecalculateAllPositions();
            }
        }
    }

    public void OnButtonReturned(DraggableButton button, int index)
    {
        buttonStates[index] = true;
        RecalculateAllPositions();

        // ✅ פשוט סמן שצריך להניע - Update יטפל בזה
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            buttonsAnimating[rect] = true;
        }
    }

    public void OnButtonSuccessfullyPlaced(DraggableButton button, int index)
    {
        Debug.Log($"OnButtonSuccessfullyPlaced נקרא לכפתור {index}");

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null && buttonsAnimating.ContainsKey(rect))
        {
            buttonsAnimating.Remove(rect);
        }

        if (index >= 0 && index < buttonStates.Count)
        {
            buttonStates[index] = false;
        }

        RecalculateAllPositions();
    }

    void RecalculateAllPositions()
    {
        Debug.Log("RecalculateAllPositions נקרא");

        int positionIndex = 0;

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttonStates[i])
            {
                float xPos = buttonSpacing + (positionIndex * (buttonWidth + buttonSpacing));
                Vector2 newTarget = new Vector2(xPos, 0);

                targetPositions[i] = newTarget;

                Debug.Log($"כפתור {i}: מיקום יעד חדש = {xPos}");

                // ✅ פשוט עדכן את המיקום היעד - Update יטפל בשאר
                if (buttons[i] != null && !buttons[i].IsDragging())
                {
                    RectTransform rect = buttons[i].GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        buttonsAnimating[rect] = true;
                    }
                }

                positionIndex++;
            }
        }

        UpdateContentSize();
    }

    void UpdateContentSize()
    {
        int buttonsInBar = 0;
        foreach (bool state in buttonStates)
        {
            if (state) buttonsInBar++;
        }

        float totalWidth = buttonSpacing + (buttonsInBar * (buttonWidth + buttonSpacing));
        contentPanel.sizeDelta = new Vector2(totalWidth, contentPanel.sizeDelta.y);
    }

    public ButtonData GetButtonData(int index)
    {
        if (index >= 0 && index < buttonDataList.Count)
            return buttonDataList[index];
        return null;
    }

    public ButtonData GetButtonDataByID(string id)
    {
        return buttonDataList.Find(data => data.buttonID == id);
    }

    public void SetButtonSprite(int index, Sprite sprite)
    {
        if (index >= 0 && index < buttonDataList.Count && index < buttons.Count)
        {
            buttonDataList[index].buttonSprite = sprite;

            GameObject buttonObj = buttons[index].gameObject;
            if (buttonObj != null)
            {
                Image img = buttonObj.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = sprite;
                }
            }
        }
    }

    // ✅ הוסף את זה בסוף הקובץ (לפני הסוגר האחרון של המחלקה)
    private void ShuffleButtonData()
    {
        // Fisher-Yates shuffle algorithm
        for (int i = buttonDataList.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            // Swap
            ButtonData temp = buttonDataList[i];
            buttonDataList[i] = buttonDataList[randomIndex];
            buttonDataList[randomIndex] = temp;
        }



    }



    // ✅ פונקציה זו רצה אוטומטית כשאתה משנה משהו ב-Inspector
    // ✅ פונקציה זו רצה אוטומטית כשאתה משנה משהו ב-Inspector
    private void OnValidate()
    {
        // 1️⃣ מצא את המספר הגבוה ביותר שכבר קיים
        int maxNumber = -1;

        foreach (var data in buttonDataList)
        {
            if (data != null && !string.IsNullOrEmpty(data.buttonID))
            {
                // חלץ מספר מה-buttonID (למשל "spot11" → 11)
                string numPart = data.buttonID.Replace("spot", "").Replace("SPOT", "");
                if (int.TryParse(numPart, out int num))
                {
                    if (num > maxNumber)
                        maxNumber = num;
                }
            }
        }

        // 2️⃣ עבור על כל הכפתורים - תקן רק אלה שריקים!
        for (int i = 0; i < buttonDataList.Count; i++)
        {
            // אם האלמנט null - צור אותו
            if (buttonDataList[i] == null)
            {
                buttonDataList[i] = new ButtonData();
            }

            // ✅ רק אם ה-buttonID ריק - תן לו מספר חדש
            // ✅ אם כבר יש לו buttonID - אל תשנה אותו! (כך אפשר להזיז במיקום)
            if (string.IsNullOrEmpty(buttonDataList[i].buttonID))
            {
                maxNumber++; // הגדל את המספר
                buttonDataList[i].buttonID = "spot" + maxNumber.ToString("D2");
                Debug.Log($"[ScrollableButtonBar] Created new button: {buttonDataList[i].buttonID}");
            }
        }

        // 3️⃣ בדוק כפילויות (למקרה שמישהו העתיק ידנית)
        HashSet<string> seenIDs = new HashSet<string>();

        for (int i = 0; i < buttonDataList.Count; i++)
        {
            if (buttonDataList[i] != null && !string.IsNullOrEmpty(buttonDataList[i].buttonID))
            {
                // אם כבר ראינו את ה-ID הזה - זו כפילות!
                if (seenIDs.Contains(buttonDataList[i].buttonID))
                {
                    maxNumber++;
                    string oldID = buttonDataList[i].buttonID;
                    buttonDataList[i].buttonID = "spot" + maxNumber.ToString("D2");
                    Debug.LogWarning($"[ScrollableButtonBar] Fixed duplicate: {oldID} → {buttonDataList[i].buttonID}");
                }
                else
                {
                    seenIDs.Add(buttonDataList[i].buttonID);
                }
            }
        }
    }

    /// <summary>
    /// גוללת את הבר כך שהכפתור הספציפי יהיה נראה במרכז
    /// </summary>
    /// <param name="button">הכפתור שאליו לגלול</param>
    /// <param name="duration">משך הגלילה בשניות</param>
    public IEnumerator ScrollToButton(DraggableButton button, float duration = 0.5f)
    {
        if (button == null || scrollRect == null)
        {
            Debug.LogWarning("[ScrollableButtonBar] Cannot scroll - button or scrollRect is null");
            yield break;
        }

        // מצא את האינדקס של הכפתור
        int index = buttons.IndexOf(button);
        if (index == -1)
        {
            Debug.LogWarning("[ScrollableButtonBar] Button not found in list");
            yield break;
        }

        Debug.Log($"[ScrollableButtonBar] 📜 גולל לכפתור {index}: {button.GetButtonID()}");

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            yield break;
        }

        // חשב את המיקום של הכפתור ב-content
        float buttonPosX = buttonRect.anchoredPosition.x;

        // חשב את רוחב ה-viewport
        RectTransform viewportRect = scrollRect.viewport;
        float viewportWidth = viewportRect != null ? viewportRect.rect.width : 0f;

        // חשב את רוחב ה-content
        float contentWidth = contentPanel.rect.width;

        // חשב את ה-normalizedPosition הרצוי (0 = שמאל, 1 = ימין)
        // נרצה שהכפתור יהיה במרכז ה-viewport
        float targetNormalizedPos = 0f;

        if (contentWidth > viewportWidth)
        {
            // המיקום של הכפתור ביחס ל-content (ממרכז הכפתור)
            float buttonCenter = buttonPosX + (buttonWidth / 2f);

            // נרצה שמרכז הכפתור יהיה במרכז ה-viewport
            float targetScrollPos = buttonCenter - (viewportWidth / 2f);

            // Normalize לטווח 0-1
            float maxScrollDistance = contentWidth - viewportWidth;
            targetNormalizedPos = Mathf.Clamp01(targetScrollPos / maxScrollDistance);
        }

        Debug.Log($"[ScrollableButtonBar] גלילה ל-position: {targetNormalizedPos:F2}");

        // אנימציית גלילה חלקה
        float startPos = scrollRect.horizontalNormalizedPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // EaseInOut לגלילה חלקה
            float smoothT = t < 0.5f
                ? 2f * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetNormalizedPos, smoothT);

            yield return null;
        }

        // ודא שהגענו למיקום הסופי
        scrollRect.horizontalNormalizedPosition = targetNormalizedPos;

        Debug.Log("[ScrollableButtonBar] ✅ גלילה הושלמה!");
    }

    /// <summary>
    /// מחזיר את הכפתור לפי buttonID
    /// </summary>
    public DraggableButton GetButtonByID(string buttonID)
    {
        return buttons.Find(btn => btn != null && btn.GetButtonID() == buttonID);
    }

}