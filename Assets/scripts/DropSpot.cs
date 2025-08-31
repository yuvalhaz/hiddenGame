using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class DropSpot : MonoBehaviour
{
    [Header("Spot Identity")]
    public string spotId;

    [Header("State")]
    public bool isFilled = false;

    [Header("Optional")]
    public GameObject placeholderToKeep;
    public GameObject parentToHideOnSuccess;

    [Header("Events")]
    public UnityEvent onCorrectPlaced;

    public void SettleItem(RectTransform itemRT)
    {
        if (isFilled) return;
        isFilled = true;

        var spotRT = (RectTransform)transform;
        itemRT.SetParent(spotRT, false);
        itemRT.anchorMin = itemRT.anchorMax = new Vector2(0.5f, 0.5f);
        itemRT.pivot = new Vector2(0.5f, 0.5f);
        itemRT.anchoredPosition = Vector2.zero;
        itemRT.localScale = Vector3.one;
        itemRT.localRotation = Quaternion.identity;

        if (parentToHideOnSuccess != null) parentToHideOnSuccess.SetActive(false);
        onCorrectPlaced?.Invoke();
    }

    public bool Accepts(string itemId) => !isFilled && !string.IsNullOrEmpty(itemId) && itemId == spotId;
}
