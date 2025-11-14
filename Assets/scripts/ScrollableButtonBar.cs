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
        // Smooth continuous animation
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
            
            // Smooth movement with MoveTowards
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
                // Smooth movement at constant speed
                float speed = animationSpeed * 100f; // Multiply by 100 (pixels per second)
                Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, speed * Time.deltaTime);
                rect.anchoredPosition = newPos;
            }
        }
        
        foreach (var rect in toRemove)
        {
            buttonsAnimating.Remove(rect);
        }
    }

    /// <summary>
    /// Public method to refresh the button bar. Checks for destroyed buttons and recalculates positions.
    /// </summary>
    public void RefreshBar()
    {
        Debug.Log("[ScrollableButtonBar] RefreshBar called");

        // Check all buttons and mark destroyed ones as inactive
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null)
            {
                buttonStates[i] = false;
                Debug.Log($"[ScrollableButtonBar] Button {i} is null - marking inactive");
            }
        }

        // Recalculate all positions
        RecalculateAllPositions();

        Debug.Log("[ScrollableButtonBar] Bar refreshed!");
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
        Debug.Log($"[ScrollableButtonBar] OnButtonDragStarted for button: {index}");

        // Stop animation for this button
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null && buttonsAnimating.ContainsKey(rect))
        {
            buttonsAnimating.Remove(rect);
        }
    }

    public void OnButtonDraggedOut(DraggableButton button, int index)
    {
        Debug.Log($"[ScrollableButtonBar] OnButtonDraggedOut for button: {index}");

        // Mark button as inactive and recalculate positions (only once)
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

        // Mark for animation - Update will handle it
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            buttonsAnimating[rect] = true;
        }
    }

    public void OnButtonSuccessfullyPlaced(DraggableButton button, int index)
    {
        Debug.Log($"[ScrollableButtonBar] OnButtonSuccessfullyPlaced for button {index}");

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
        Debug.Log("[ScrollableButtonBar] RecalculateAllPositions called");

        int positionIndex = 0;

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttonStates[i])
            {
                float xPos = buttonSpacing + (positionIndex * (buttonWidth + buttonSpacing));
                Vector2 newTarget = new Vector2(xPos, 0);

                targetPositions[i] = newTarget;

                Debug.Log($"[ScrollableButtonBar] Button {i}: new target position = {xPos}");

                // Update target position - Update() will animate it
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
    /// Scroll the ScrollRect to make a specific button visible (instant).
    /// </summary>
    public void ScrollToButton(DraggableButton button)
    {
        ScrollToButton(button, 0f);
    }

    /// <summary>
    /// Scroll the ScrollRect to make a specific button visible with animation.
    /// </summary>
    /// <param name="button">The button to scroll to</param>
    /// <param name="duration">Animation duration in seconds (0 = instant)</param>
    public void ScrollToButton(DraggableButton button, float duration)
    {
        if (button == null || scrollRect == null || contentPanel == null)
            return;

        // Find the button's index
        int index = buttons.IndexOf(button);
        if (index < 0) return;

        // Get button's RectTransform
        RectTransform buttonRT = button.GetComponent<RectTransform>();
        if (buttonRT == null) return;

        // Calculate the normalized position (0-1) to scroll to
        float buttonX = buttonRT.anchoredPosition.x;
        float contentWidth = contentPanel.rect.width;
        float viewportWidth = scrollRect.viewport.rect.width;

        if (contentWidth <= viewportWidth)
        {
            // Content fits in viewport, no need to scroll
            scrollRect.horizontalNormalizedPosition = 0f;
            return;
        }

        // Calculate normalized position to center the button
        float normalizedPos = Mathf.Clamp01(buttonX / (contentWidth - viewportWidth));

        if (duration > 0f)
        {
            // Animated scroll - start coroutine
            StartCoroutine(AnimateScrollTo(normalizedPos, duration));
        }
        else
        {
            // Instant scroll
            scrollRect.horizontalNormalizedPosition = normalizedPos;
        }
    }

    /// <summary>
    /// Scroll to button with index (instant).
    /// </summary>
    public void ScrollToButton(int index)
    {
        ScrollToButton(index, 0f);
    }

    /// <summary>
    /// Scroll to button with index with animation.
    /// </summary>
    /// <param name="index">Index of the button</param>
    /// <param name="duration">Animation duration in seconds (0 = instant)</param>
    public void ScrollToButton(int index, float duration)
    {
        if (index >= 0 && index < buttons.Count)
        {
            ScrollToButton(buttons[index], duration);
        }
    }

    /// <summary>
    /// Smoothly animate scrolling to a target position.
    /// </summary>
    private System.Collections.IEnumerator AnimateScrollTo(float targetNormalizedPos, float duration)
    {
        float startPos = scrollRect.horizontalNormalizedPosition;
        float elapsed = 0f;

        // Clamp duration to reasonable values
        duration = Mathf.Max(0.1f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth easing
            float easedT = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetNormalizedPos, easedT);

            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = targetNormalizedPos;
    }
}