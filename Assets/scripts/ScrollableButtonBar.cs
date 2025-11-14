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

    // ✅ Reusable list to prevent per-frame allocations
    private List<RectTransform> toRemove = new List<RectTransform>();

    // ✅ Cache RectTransform -> button index mapping to avoid GetComponent in Update
    private Dictionary<RectTransform, int> rectToIndexCache = new Dictionary<RectTransform, int>();

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
        
        CreateButtons();
    }

    void Update()
    {
        // ✅ אנימציה חלקה ורציפה בלי קפיצות!
        // ✅ נקה את הרשימה במקום ליצור חדשה (מונע TLS allocations)
        toRemove.Clear();

        foreach (var kvp in buttonsAnimating)
        {
            RectTransform rect = kvp.Key;
            if (rect == null)
            {
                toRemove.Add(rect);
                continue;
            }

            // ✅ מצא את האינדקס מה-cache (ללא GetComponent allocation!)
            int index = -1;
            if (!rectToIndexCache.TryGetValue(rect, out index))
            {
                // Fallback: מצא את האינדקס ושמור ב-cache
                for (int i = 0; i < buttons.Count; i++)
                {
                    if (buttons[i] != null && buttons[i].transform == rect.transform)
                    {
                        index = i;
                        rectToIndexCache[rect] = i;
                        break;
                    }
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

    // ✅ Public API to scroll to a specific button by index with optional smooth scrolling
    public void ScrollToButton(int buttonIndex, bool smooth = false)
    {
        if (scrollRect == null || contentPanel == null)
        {
            Debug.LogWarning("[ScrollableButtonBar] ScrollRect or ContentPanel is null!");
            return;
        }

        if (buttonIndex < 0 || buttonIndex >= buttons.Count)
        {
            Debug.LogWarning($"[ScrollableButtonBar] Invalid button index: {buttonIndex}");
            return;
        }

        if (buttons[buttonIndex] == null)
        {
            Debug.LogWarning($"[ScrollableButtonBar] Button at index {buttonIndex} is null!");
            return;
        }

        // Calculate normalized position for the button
        RectTransform buttonRect = buttons[buttonIndex].GetComponent<RectTransform>();
        if (buttonRect == null) return;

        // Get the button's position relative to content panel
        float buttonPos = -buttonRect.anchoredPosition.x;
        float contentWidth = contentPanel.rect.width;
        float viewportWidth = scrollRect.viewport.rect.width;
        float scrollableWidth = Mathf.Max(0, contentWidth - viewportWidth);

        if (scrollableWidth <= 0)
        {
            scrollRect.horizontalNormalizedPosition = 0;
            return;
        }

        // Calculate normalized position (0 = left, 1 = right)
        float normalizedPos = Mathf.Clamp01(buttonPos / scrollableWidth);

        if (smooth)
        {
            StartCoroutine(SmoothScrollTo(normalizedPos));
        }
        else
        {
            scrollRect.horizontalNormalizedPosition = normalizedPos;
        }

        Debug.Log($"[ScrollableButtonBar] Scrolled to button {buttonIndex} at position {normalizedPos:F2}");
    }

    // ✅ Overload to scroll by button ID with optional smooth scrolling
    public void ScrollToButton(string buttonID, bool smooth = false)
    {
        int index = buttons.FindIndex(b => b != null && b.buttonID == buttonID);
        if (index >= 0)
        {
            ScrollToButton(index, smooth);
        }
        else
        {
            Debug.LogWarning($"[ScrollableButtonBar] Button with ID '{buttonID}' not found!");
        }
    }

    // ✅ Overload to scroll by DraggableButton reference with animation duration
    public void ScrollToButton(DraggableButton button, float duration)
    {
        if (button == null)
        {
            Debug.LogWarning("[ScrollableButtonBar] Button is null!");
            return;
        }

        int index = buttons.IndexOf(button);
        if (index >= 0)
        {
            bool useSmooth = duration > 0;
            ScrollToButton(index, useSmooth);
        }
        else
        {
            Debug.LogWarning($"[ScrollableButtonBar] Button not found in buttons list!");
        }
    }

    // ✅ Coroutine for smooth scrolling
    private IEnumerator SmoothScrollTo(float targetPosition)
    {
        float startPosition = scrollRect.horizontalNormalizedPosition;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = targetPosition;
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

            // ✅ Populate cache to avoid GetComponent calls in Update
            rectToIndexCache[buttonRect] = i;
            
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = buttonDataList[i].buttonID;
            }
        }
        
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
}