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

    [Header("Drag Area Settings")]
    [SerializeField] private bool useCenterDragArea = true;
    [Tooltip("Percentage of button size that can be dragged (0.7 = 70%)")]
    [SerializeField] [Range(0.1f, 1f)] private float dragAreaScale = 0.7f;

    [Header("Button Data")]
    [SerializeField] private List<ButtonData> buttonDataList = new List<ButtonData>();

    [Header("Shuffle Settings")]
    [Tooltip("Shuffle button order randomly at start")]
    [SerializeField] private bool shuffleButtons = false;

    [Header("Animation Settings")]
    [Tooltip("מהירות אנימציה בהתחלה (ברירת מחדל: 50)")]
    [SerializeField] private float initialAnimationSpeed = 50f;
    [Tooltip("מהירות אנימציה רגילה אחרי השינוי (ברירת מחדל: 10)")]
    [SerializeField] private float normalAnimationSpeed = 10f;
    [Tooltip("כמה זמן בשניות עד שהמהירות משתנה (ברירת מחדל: 30 = חצי דקה)")]
    [SerializeField] private float speedChangeDelay = 30f;

    [Header("Entrance Animation")]
    [Tooltip("כמה פיקסלים מימין לנקודת היעד שהכפתורים מתחילים (ברירת מחדל: 800)")]
    [SerializeField] private float entranceOffscreenOffset = 800f;
    [Tooltip("עיכוב בשניות בין כפתור לכפתור (ברירת מחדל: 0.08)")]
    [SerializeField] private float entranceStaggerDelay = 0.08f;

    [Header("References")]
    [SerializeField] private RectTransform contentPanel;
    [SerializeField] private ScrollRect scrollRect;

    private List<DraggableButton> buttons = new List<DraggableButton>();
    private List<bool> buttonStates = new List<bool>();
    private List<Vector2> targetPositions = new List<Vector2>();

    // ✅ Cache לביצועים טובים יותר
    private List<CanvasGroup> buttonCanvasGroups = new List<CanvasGroup>();
    private List<Image> buttonImages = new List<Image>();

    private Dictionary<RectTransform, bool> buttonsAnimating = new Dictionary<RectTransform, bool>();
    private Coroutine revealCoroutine;

    // Used to cancel stale entrance-animation coroutines when a new one starts.
    private int entranceVersion = 0;

    private float currentAnimationSpeed;
    private float startTime;
    private CanvasGroup contentCanvasGroup;

    void Start()
    {
        // Initialize animation speed and start time
        currentAnimationSpeed = initialAnimationSpeed;
        startTime = Time.time;

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

        // Hide content until positions are ready (prevents flash of wrong positions)
        contentCanvasGroup = contentPanel.GetComponent<CanvasGroup>();
        if (contentCanvasGroup == null)
            contentCanvasGroup = contentPanel.gameObject.AddComponent<CanvasGroup>();

        // Always hide until positions are ready (prevents flash of wrong positions)
        contentCanvasGroup.alpha = 0f;

        CreateButtons();

        // Calculate auto spacing AFTER canvas layout is ready (wait one frame)
        if (useAutoSpacing)
        {
            StartCoroutine(InitAutoSpacingAfterLayout());
        }
        else
        {
            // Sprites may be set this frame via SetButtonSprite — reveal after one frame
            revealCoroutine = StartCoroutine(RevealNextFrame());
        }
    }

    private IEnumerator InitAutoSpacingAfterLayout()
    {
        // Wait for canvas to finish layout calculations
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (scrollRect == null || scrollRect.viewport == null || buttons.Count == 0)
            yield break;

        float viewportWidth = scrollRect.viewport.rect.width;

        // If still 0, wait one more frame
        if (viewportWidth <= 0f)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            viewportWidth = scrollRect.viewport.rect.width;
        }

        if (viewportWidth <= 0f)
        {
            Debug.LogWarning("[ScrollableButtonBar] Viewport width is 0 - skipping auto spacing");
            yield break;
        }

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
        float newSpacing = (viewportWidth - totalButtonWidth) / (buttonsToFitOnScreen + 1);

        if (newSpacing < 0f)
        {
            Debug.LogWarning($"[ScrollableButtonBar] Calculated spacing is negative ({newSpacing}px) - buttons too wide for viewport. Clamping to 0.");
            newSpacing = 0f;
        }

        buttonSpacing = newSpacing;
        Debug.Log($"[ScrollableButtonBar] Auto spacing calculated: {buttonSpacing}px (viewport: {viewportWidth}px, avg button: {avgButtonWidth}px, {buttonsToFitOnScreen} buttons to fit)");

        // Snap buttons directly to correct positions (no animation on initial layout)
        RecalculateAllPositions(immediate: true);

        // Play entrance animation (also reveals the bar)
        PlayEntranceAnimation();
    }

    private IEnumerator RevealNextFrame()
    {
        yield return null;
        PlayEntranceAnimation();
        revealCoroutine = null;
    }

    private void PlayEntranceAnimation()
    {
        // Cancel any stale entrance coroutines from a previous call.
        entranceVersion++;
        int myVersion = entranceVersion;

        // Snap all buttons to correct positions first (needed if sprites changed sizes).
        RecalculateAllPositions(immediate: true);

        // Collect active buttons sorted left-to-right by target X.
        List<int> activeIndices = new List<int>();
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttonStates[i] && buttons[i] != null)
                activeIndices.Add(i);
        }
        activeIndices.Sort((a, b) => targetPositions[a].x.CompareTo(targetPositions[b].x));

        // Hide all buttons via their own CanvasGroup (buttons are at correct positions,
        // so content panel size is always right — no scroll gap).
        foreach (int i in activeIndices)
        {
            RectTransform rect = buttons[i].GetComponent<RectTransform>();
            if (rect != null) buttonsAnimating.Remove(rect);

            if (i < buttonCanvasGroups.Count && buttonCanvasGroups[i] != null)
                buttonCanvasGroups[i].alpha = 0f;
        }

        // Reveal bar — buttons invisible, layout correct.
        if (contentCanvasGroup != null)
            contentCanvasGroup.alpha = 1f;

        // Stagger each button's entrance by relative left-to-right index.
        for (int r = 0; r < activeIndices.Count; r++)
        {
            StartCoroutine(EntranceDelayed(activeIndices[r], r * entranceStaggerDelay, myVersion));
        }
    }

    private IEnumerator EntranceDelayed(int globalIndex, float delay, int version)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // Bail out if a newer entrance animation has started.
        if (version != entranceVersion) yield break;

        if (globalIndex >= buttons.Count || buttons[globalIndex] == null)
            yield break;

        RectTransform rect = buttons[globalIndex].GetComponent<RectTransform>();
        if (rect == null) yield break;

        // Move to slide-start (off-screen right), then make visible — one button at a time.
        rect.anchoredPosition = targetPositions[globalIndex] + new Vector2(entranceOffscreenOffset, 0f);

        if (globalIndex < buttonCanvasGroups.Count && buttonCanvasGroups[globalIndex] != null)
            buttonCanvasGroups[globalIndex].alpha = 1f;

        // Update() slides it to targetPositions[globalIndex].
        buttonsAnimating[rect] = true;
    }

    private void OnValidate()
    {
        // Called when values change in Inspector (only in Editor)
        if (Application.isPlaying && buttons != null && buttons.Count > 0)
        {
            RecalculateAllPositions();
        }
    }

    /// <summary>
    /// Manually refresh button positions (Right-click component -> Refresh Button Positions)
    /// </summary>
    [ContextMenu("Refresh Button Positions")]
    public void RefreshButtonPositions()
    {
        if (buttons != null && buttons.Count > 0)
        {
            RecalculateAllPositions();
            Debug.Log("[ScrollableButtonBar] Button positions refreshed manually");
        }
    }

    void Update()
    {
        // Check if it's time to reduce animation speed
        if (currentAnimationSpeed == initialAnimationSpeed && Time.time - startTime >= speedChangeDelay)
        {
            currentAnimationSpeed = normalAnimationSpeed;
            Debug.Log($"[ScrollableButtonBar] Animation speed reduced from {initialAnimationSpeed} to {normalAnimationSpeed}");
        }

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
                float speed = currentAnimationSpeed * 100f; // כפול 100 כי זה פיקסלים לשנייה
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
                // ✅ Scale to match GameBar scale
                float barScale = transform.localScale.x;
                if (barScale > 0f && Mathf.Abs(barScale - 1f) > 0.001f)
                {
                    buttonRect.sizeDelta *= barScale;
                }
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

            // ✅ Cache components לביצועים
            // Ensure every button has a CanvasGroup so entrance-animation alpha works.
            CanvasGroup buttonCG = buttonObj.GetComponent<CanvasGroup>();
            if (buttonCG == null) buttonCG = buttonObj.AddComponent<CanvasGroup>();
            buttonCanvasGroups.Add(buttonCG);
            buttonImages.Add(draggable.GetComponent<Image>());

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

    void RecalculateAllPositions(bool immediate = false)
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

                if (buttons[i] != null && !buttons[i].IsDragging())
                {
                    RectTransform rect = buttons[i].GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        if (immediate)
                        {
                            // Snap directly — no animation (used for initial layout)
                            rect.anchoredPosition = newTarget;
                            buttonsAnimating.Remove(rect);
                        }
                        else
                        {
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
                        // ✅ Scale to match GameBar scale
                        float barScale = transform.localScale.x;
                        if (barScale > 0f && Mathf.Abs(barScale - 1f) > 0.001f)
                        {
                            RectTransform btnRect = buttonObj.GetComponent<RectTransform>();
                            if (btnRect != null)
                            {
                                btnRect.sizeDelta *= barScale;
                            }
                        }
                        // Size changed — recalculate positions immediately
                        RecalculateAllPositions(immediate: true);
                        // Debounce reveal: wait one frame after the last sprite is set
                        if (revealCoroutine != null) StopCoroutine(revealCoroutine);
                        revealCoroutine = StartCoroutine(RevealNextFrame());
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

    /// <summary>
    /// כיבוי raycast על כל הכפתורים בזמן גרירה (משתמש ב-cache לביצועים)
    /// </summary>
    public void DisableAllButtonRaycasts(DraggableButton except = null)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null || buttons[i] == except) continue;

            if (i < buttonCanvasGroups.Count && buttonCanvasGroups[i] != null)
            {
                buttonCanvasGroups[i].blocksRaycasts = false;
            }

            if (i < buttonImages.Count && buttonImages[i] != null)
            {
                buttonImages[i].raycastTarget = false;
            }
        }
    }

    /// <summary>
    /// החזרת raycast לכל הכפתורים אחרי גרירה (משתמש ב-cache לביצועים)
    /// </summary>
    public void EnableAllButtonRaycasts()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null) continue;

            if (i < buttonCanvasGroups.Count && buttonCanvasGroups[i] != null)
            {
                buttonCanvasGroups[i].blocksRaycasts = true;
            }

            if (i < buttonImages.Count && buttonImages[i] != null)
            {
                buttonImages[i].raycastTarget = true;
            }
        }
    }
}