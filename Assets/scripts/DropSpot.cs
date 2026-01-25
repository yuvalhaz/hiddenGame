using UnityEngine;
using UnityEngine.UI;

public class DropSpot : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("חייב להיות זהה ל-itemId של הכפתור התואם בבר")]
    public string discription;
    public string spotId;

    [Header("Reveal System")]
    [SerializeField] private ImageRevealController revealController;

    [Header("State (נקבע אוטומטית)")]
    public bool IsSettled { get; set; }

    private void Awake()
    {
        if (revealController == null)
        {
            revealController = GetComponent<ImageRevealController>();
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

            // ✅ כבה את ה-raycast של כל התמונות!
            revealController.DisableAllRaycasts();
            Debug.Log($"[DropSpot] Disabled all raycasts on {spotId}");
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
