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
    [SerializeField] private float buttonSpacing = 80f;
    [SerializeField] private float buttonWidth = 100f;

    [Header("Auto Spacing (Optional)")]
    [SerializeField] private bool useAutoSpacing = false;
    [Tooltip("Automatically calculate spacing to fit exactly N buttons on screen")]
    [SerializeField] private int buttonsToFitOnScreen = 3;
    [Tooltip("How many buttons should be visible on screen at once")]

    [Header("Drag Area Settings")]
    [SerializeField] private bool useCenterDragArea = true;
    [Tooltip("If true, only the center area of button can be dragged")]
    [SerializeField] [Range(0.1f, 1f)] private float dragAreaScale = 0.7f;
    [Tooltip("Percentage of button size that can be dragged (0.7 = 70%)")]

    [Header("Button Data")]
    [SerializeField] private List<ButtonData> buttonDataList = new List<ButtonData>();

    [Header("Shuffle Settings")]
    [SerializeField] private bool shuffleButtons = false;
    [Tooltip("Shuffle button order randomly at start")]

    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 10f;
    
    [Header("References")]
    [SerializeField] private RectTransform contentPanel;
    [SerializeField] private ScrollRect scrollRect;

    private List<DraggableButton> buttons = new List<DraggableButton>();
    private List<bool> buttonStates = new List<bool>();
    private List<Vector2> targetPositions = new List<Vector2>();

    private Dictionary<RectTransform, bool> buttonsAnimating = new Dictionary<RectTransform, bool>();
    private int startFrame = -1; // Frame when Start() was called

    void Start()
    {
        startFrame = Time.frameCount;

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

        // Calculate auto spacing AFTER creating buttons (so we know actual button sizes)
        if (useAutoSpacing && scrollRect != null && scrollRect.viewport != null && buttons.Count > 0)
        {
            float viewportWidth = scrollRect.viewport.rect.width;

            // Calculate average button width from first few buttons
            float totalWidth = 0f;
            int sampled = Mathf.Min(buttonsToFitOnScreen, buttons.Count);
            for (int i = 0; i < sampled; i++)
            {
                if (buttons[i] != null)
                {
                    RectTransform rect = buttons[i].GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        totalWidth += rect.sizeDelta.x;
                    }
                }
            }
            float avgButtonWidth = totalWidth / sampled;

            // Calculate spacing to fit exactly N buttons on screen
            // Formula: viewportWidth = (N * avgWidth) + ((N + 1) * spacing)
            // Solving for spacing: spacing = (viewportWidth - (N * avgWidth)) / (N + 1)
            float totalButtonWidth = buttonsToFitOnScreen * avgButtonWidth;
            buttonSpacing = (viewportWidth - totalButtonWidth) / (buttonsToFitOnScreen + 1);

            Debug.Log($"[ScrollableButtonBar] Auto spacing calculated: {buttonSpacing}px (viewport: {viewportWidth}px, avg button: {avgButtonWidth}px, {buttonsToFitOnScreen} buttons to fit)");

            // Recalculate positions with new spacing
            RecalculateAllPositions();
        }
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

        // 🎲 Shuffle buttons if enabled
        if (shuffleButtons)
        {
            ShuffleButtonData();
        }

        for (int i = 0; i < numberOfButtons; i++)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, contentPanel);
            buttonObj.name = buttonDataList[i].buttonID;
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();

            buttonRect.anchorMin = new Vector2(0, 0.5f);
            buttonRect.anchorMax = new Vector2(0, 0.5f);
            buttonRect.pivot = new Vector2(0, 0.5f);

            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null && buttonDataList[i].buttonSprite != null)
            {
                buttonImage.sprite = buttonDataList[i].buttonSprite;
                // ✅ Use native size of the sprite
                buttonImage.SetNativeSize();
            }
            else if (buttonImage != null)
            {
                // Fallback to buttonWidth if no sprite
                buttonRect.sizeDelta = new Vector2(buttonWidth, buttonWidth);
            }

            // Calculate position based on actual button width
            float actualWidth = buttonRect.sizeDelta.x;
            float xPos = buttonSpacing + (i * (actualWidth + buttonSpacing));
            buttonRect.anchoredPosition = new Vector2(xPos, 0);

            // ✅ Create center drag area if enabled
            DraggableButton draggable = buttonObj.GetComponent<DraggableButton>();
            if (draggable == null)
            {
                draggable = buttonObj.AddComponent<DraggableButton>();
            }
            draggable.SetButtonBar(this, i);
            draggable.SetButtonID(buttonDataList[i].buttonID);

            if (useCenterDragArea)
            {
                // Disable raycast on main image
                if (buttonImage != null)
                {
                    buttonImage.raycastTarget = false;
                }

                // Create child GameObject for drag area
                GameObject dragArea = new GameObject("DragArea");
                dragArea.transform.SetParent(buttonObj.transform, false);

                RectTransform dragRect = dragArea.AddComponent<RectTransform>();
                dragRect.anchorMin = new Vector2(0.5f, 0.5f);
                dragRect.anchorMax = new Vector2(0.5f, 0.5f);
                dragRect.pivot = new Vector2(0.5f, 0.5f);
                dragRect.anchoredPosition = Vector2.zero;

                // Set size to percentage of button size
                Vector2 dragSize = buttonRect.sizeDelta * dragAreaScale;
                dragRect.sizeDelta = dragSize;

                // Add transparent Image for raycasting
                Image dragImage = dragArea.AddComponent<Image>();
                dragImage.color = new Color(0, 0, 0, 0); // Transparent
                dragImage.raycastTarget = true;

                // Add proxy to forward drag events to parent DraggableButton
                dragArea.AddComponent<DragAreaProxy>();
            }
            
            buttons.Add(draggable);
            buttonStates.Add(true);
            targetPositions.Add(buttonRect.anchoredPosition);

            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = buttonDataList[i].buttonID;
            }
        }
        
        UpdateContentSize();
    }

    /// <summary>
    /// Shuffles the button data list randomly using Fisher-Yates algorithm
    /// </summary>
    private void ShuffleButtonData()
    {
        Debug.Log("[ScrollableButtonBar] 🎲 Shuffling buttons...");

        int n = buttonDataList.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            // Swap
            ButtonData temp = buttonDataList[i];
            buttonDataList[i] = buttonDataList[j];
            buttonDataList[j] = temp;
        }

        Debug.Log("[ScrollableButtonBar] ✅ Buttons shuffled!");
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

        float currentX = buttonSpacing; // Start from first spacing

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttonStates[i])
            {
                // Use actual width of this specific button
                float actualWidth = buttonWidth; // Default
                if (buttons[i] != null)
                {
                    RectTransform rect = buttons[i].GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        actualWidth = rect.sizeDelta.x;
                    }
                }

                Vector2 newTarget = new Vector2(currentX, 0);
                targetPositions[i] = newTarget;

                Debug.Log($"כפתור {i}: מיקום יעד חדש = {currentX}");

                // ✅ During initial frames, place immediately. Otherwise animate.
                if (buttons[i] != null && !buttons[i].IsDragging())
                {
                    RectTransform rect = buttons[i].GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        // Check if we're within first 3 frames after Start
                        bool isInitialSetup = (Time.frameCount - startFrame) <= 3;

                        if (isInitialSetup)
                        {
                            // Initial setup - place immediately without animation
                            rect.anchoredPosition = newTarget;
                            Debug.Log($"[ScrollableButtonBar] Button {i} placed immediately (frame {Time.frameCount - startFrame})");
                        }
                        else
                        {
                            // Normal operation - animate smoothly
                            buttonsAnimating[rect] = true;
                        }
                    }
                }

                // Move to next position (current button width + spacing)
                currentX += actualWidth + buttonSpacing;
            }
        }

        UpdateContentSize();
    }

    void UpdateContentSize()
    {
        // Calculate total width based on actual button sizes
        float totalWidth = buttonSpacing; // Start with initial spacing

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttonStates[i])
            {
                // Get actual width of this button
                float actualWidth = buttonWidth; // Default
                if (buttons[i] != null)
                {
                    RectTransform rect = buttons[i].GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        actualWidth = rect.sizeDelta.x;
                    }
                }

                totalWidth += actualWidth + buttonSpacing;
            }
        }

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
                    // ✅ Use native size of the sprite
                    if (sprite != null)
                    {
                        img.SetNativeSize();
                    }
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