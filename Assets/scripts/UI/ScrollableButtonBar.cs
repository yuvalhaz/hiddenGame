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

    /// <summary>
    /// Scrolls the button bar to bring the specified button into view
    /// </summary>
    public IEnumerator ScrollToButtonCoroutine(DraggableButton button, float duration)
    {
        if (scrollRect == null || button == null)
        {
            Debug.LogWarning("[ScrollableButtonBar] ScrollRect or button is null - skipping scroll");
            yield break;
        }

        RectTransform buttonRT = button.GetComponent<RectTransform>();
        if (buttonRT == null) yield break;

        // Calculate the normalized horizontal position to scroll to
        Canvas.ForceUpdateCanvases();

        float buttonPosX = buttonRT.anchoredPosition.x;
        float viewportWidth = scrollRect.viewport.rect.width;
        float contentWidth = contentPanel.rect.width;

        // Calculate target normalized position (0 = left, 1 = right)
        float targetNormalizedPos = Mathf.Clamp01(buttonPosX / (contentWidth - viewportWidth));

        float startPos = scrollRect.horizontalNormalizedPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth easing
            float easedT = 1f - (1f - t) * (1f - t);

            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetNormalizedPos, easedT);
            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = targetNormalizedPos;
        Debug.Log($"[ScrollableButtonBar] Scrolled to button at position {targetNormalizedPos}");
    }

    /// <summary>
    /// Marks a button as destroyed and updates the bar layout
    /// Called by GameProgressManager when restoring saved progress
    /// </summary>
    public void MarkButtonAsDestroyed(int index)
    {
        if (index >= 0 && index < buttonStates.Count)
        {
            Debug.Log($"[ScrollableButtonBar] Marking button {index} as destroyed");
            buttonStates[index] = false;
            RecalculateAllPositions();
        }
    }
}