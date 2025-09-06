using UnityEngine;
using UnityEngine.UI;

public class DropSpot : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("חייב להיות זהה ל-itemId של הכפתור התואם בבר")]
    public string spotId;

    [Header("State (נקבע אוטומטית)")]
    public bool IsSettled { get; private set; }

    /// <summary>
    /// האם הספוט מתאים לפריט זה?
    /// </summary>
    public bool Accepts(string itemId)
    {
        return string.Equals(itemId, spotId, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// נקרא בהנחה נכונה. סמוך לסוף—מסמן שהושלם.
    /// </summary>
    public void SettleItem(RectTransform placed)
    {
        // *** כאן תשאיר את הלוגיקה הקיימת שלך (העלמת קו/מרקר, נעילת פריט וכו') ***
        // אם אין—אין בעיה; רק נותנים דגל:
        IsSettled = true;
    }

    /// <summary>
    /// מרכז היעד בעולם (להדגמת רמז).
    /// </summary>
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
}
