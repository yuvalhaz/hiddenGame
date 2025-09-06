using UnityEngine;

public class BackgroundPriority : MonoBehaviour
{
    [Header("Background Settings")]
    [SerializeField] private bool alwaysBottom = true;
    [SerializeField] private int offsetFromBottom = 0; // 0 = absolute bottom
    
    void Start()
    {
        SetBackgroundPriority();
    }
    
    void SetBackgroundPriority()
    {
        if (alwaysBottom)
        {
            transform.SetAsFirstSibling();
        }
        else
        {
            transform.SetSiblingIndex(offsetFromBottom);
        }
    }
    
    // Call this if hierarchy changes
    public void RefreshPriority()
    {
        SetBackgroundPriority();
    }
}