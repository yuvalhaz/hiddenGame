using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SpotTransformation
{
    [Tooltip("ה-buttonID של הפריט שגורר לכאן")]
    public string triggerItemId;

    [Tooltip("התמונה החדשה שתחליף את הנוכחית")]
    public Sprite newSprite;
}

public class DropSpot : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("חייב להיות זהה ל-itemId של הכפתור התואם בבר")]
    public string discription;
    public string spotId;

    [Header("Reveal System")]
    [SerializeField] private ImageRevealController revealController;

    [Header("Transformations (שילובי פריטים)")]
    [SerializeField] private List<SpotTransformation> transformations;

    [Header("State (נקבע אוטומטית)")]
    public bool IsSettled { get; set; }

    private void Awake()
    {
        if (revealController == null)
        {
            revealController = GetComponent<ImageRevealController>();
        }
    }

    private void OnEnable()
    {
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnItemPlaced += OnAnyItemPlaced;
        }
    }

    private void OnDisable()
    {
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnItemPlaced -= OnAnyItemPlaced;
        }
    }

    private void Start()
    {
        // Subscribe again in Start in case GameProgressManager wasn't ready in OnEnable
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnItemPlaced -= OnAnyItemPlaced; // avoid double
            GameProgressManager.Instance.OnItemPlaced += OnAnyItemPlaced;
        }
    }

    /// <summary>
    /// Called whenever ANY item is placed anywhere. If it's a transformation trigger for this spot, apply it.
    /// </summary>
    private void OnAnyItemPlaced(string itemId)
    {
        if (!IsSettled || transformations == null) return;

        foreach (var t in transformations)
        {
            if (string.Equals(t.triggerItemId, itemId, System.StringComparison.Ordinal))
            {
                ApplyTransformationSprite(itemId);
                TriggerSparkles();

                // Hide the trigger item's DropSpot image so it doesn't cover the transformed sprite
                HideTriggerSpotImage(itemId);

                Debug.Log($"[DropSpot] Auto-transformation on {spotId}: {itemId} placed → sprite changed");
                break;
            }
        }
    }

    /// <summary>
    /// Find the trigger item's DropSpot and hide its revealed image
    /// </summary>
    private void HideTriggerSpotImage(string triggerItemId)
    {
        DropSpot[] allSpots = FindObjectsOfType<DropSpot>(true);
        foreach (var spot in allSpots)
        {
            if (string.Equals(spot.spotId, triggerItemId, System.StringComparison.Ordinal))
            {
                var triggerReveal = spot.GetComponent<ImageRevealController>();
                if (triggerReveal != null)
                {
                    var bgImage = triggerReveal.GetBackgroundImage();
                    if (bgImage != null)
                    {
                        bgImage.color = new Color(1f, 1f, 1f, 0f);
                        Debug.Log($"[DropSpot] Hidden trigger spot image: {triggerItemId}");
                    }
                }
                break;
            }
        }
    }

    public bool Accepts(string itemId)
    {
        return string.Equals(itemId, spotId, System.StringComparison.Ordinal);
    }

    public void SettleItem(RectTransform placed)
    {
        Destroy(placed.gameObject);
        IsSettled = true;

        // הפעל reveal של התמונה האמיתית
        if (revealController != null)
        {
            revealController.Reveal();

            // ✅ כבה את ה-raycast של התמונה שנחשפה!
            var backgroundImage = revealController.GetBackgroundImage();
            if (backgroundImage != null)
            {
                backgroundImage.raycastTarget = false;
                Debug.Log($"[DropSpot] Disabled raycast on {spotId}");
            }
        }
        else
        {
            Debug.LogWarning($"[DropSpot] No RevealController on {spotId}!");
        }

        // ✨ הפעל אפקט נצנצים עדין!
        TriggerSparkles();

        // 🎓 עדכן את מנהל השקופיות שפריט נכון הונח
        if (TutorialSlideManager.Instance != null)
        {
            TutorialSlideManager.Instance.OnCorrectDrop(spotId);
            Debug.Log($"[DropSpot] Tutorial notified: {spotId} placed correctly");
        }

        // ✅ Notify SmlAnimManager to enable click on this spot
        if (SmlAnimManager.Instance != null)
        {
            SmlAnimManager.Instance.RefreshSpot(this);
            Debug.Log($"[DropSpot] Refreshed SmlAnimManager for {spotId}");
        }

        Debug.Log($"DropSpot {spotId} - Ghost destroyed, revealing background");
    }

    private void TriggerSparkles()
    {
        // מצא את ה-Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning($"[DropSpot] No Canvas found for sparkles on {spotId}");
            return;
        }

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            // הפעל burst של נצנצים קטנים מהמיקום של ה-DropSpot
            SparkleBurstEffect.Burst(canvas, rectTransform, count: 20, duration: 0.8f);
            Debug.Log($"[DropSpot] Sparkles triggered on {spotId}");
        }
    }

    public bool AcceptsTransformation(string itemId)
    {
        if (!IsSettled || transformations == null) return false;

        foreach (var t in transformations)
        {
            if (string.Equals(t.triggerItemId, itemId, System.StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    public void ApplyTransformationSprite(string itemId)
    {
        if (transformations == null) return;

        foreach (var t in transformations)
        {
            if (string.Equals(t.triggerItemId, itemId, System.StringComparison.Ordinal))
            {
                if (revealController != null)
                {
                    var bgImage = revealController.GetBackgroundImage();
                    if (bgImage != null)
                    {
                        bgImage.sprite = t.newSprite;
                        Debug.Log($"[DropSpot] Transformation applied on {spotId}: {itemId} → {t.newSprite.name}");
                    }
                }
                break;
            }
        }
    }

    public Vector3 GetWorldHintPosition()
    {
        var rt = transform as RectTransform;
        if (rt != null)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }
        return transform.position;
    }

    public void ResetSpot()
    {
        IsSettled = false;

        // 🎯 החדש - אפס את ה-reveal
        if (revealController != null)
        {
            revealController.ResetReveal();
        }

        // ✅ Notify SmlAnimManager to disable click on this spot
        if (SmlAnimManager.Instance != null)
        {
            SmlAnimManager.Instance.RefreshSpot(this);
        }
    }
}
