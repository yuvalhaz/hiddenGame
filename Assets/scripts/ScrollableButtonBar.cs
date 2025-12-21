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

    // Public method that can be called externally to refresh the bar
    public void RefreshBar()
    {
        // Check all buttons and mark destroyed ones as inactive
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null)
            {
                buttonStates[i] = false;
            }
        }

        // Recalculate all positions
        RecalculateAllPositions();
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
        // Stop any ongoing animations for this button
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null && buttonsAnimating.ContainsKey(rect))
        {
            buttonsAnimating.Remove(rect);
        }
    }

    public void OnButtonDraggedOut(DraggableButton button, int index)
    {
        // Update state and recalculate positions only once
        if (index >= 0 && index < buttonStates.Count)
        {
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
        int positionIndex = 0;

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttonStates[i])
            {
                float xPos = buttonSpacing + (positionIndex * (buttonWidth + buttonSpacing));
                Vector2 newTarget = new Vector2(xPos, 0);

                targetPositions[i] = newTarget;

                // Update target position - Update loop will handle animation
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

    // PERFORMANCE FIX: Public API to replace reflection usage
    public void MarkButtonAsDestroyed(int index)
    {
        if (index >= 0 && index < buttonStates.Count)
        {
            buttonStates[index] = false;
            RecalculateAllPositions();
        }
    }
}