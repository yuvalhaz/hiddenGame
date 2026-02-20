using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Proxy that forwards drag events from child drag area to parent DraggableButton
/// </summary>
public class DragAreaProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private DraggableButton parentDraggable;

    private void Awake()
    {
        // Find DraggableButton on parent
        if (transform.parent != null)
        {
            parentDraggable = transform.parent.GetComponent<DraggableButton>();
            if (parentDraggable == null)
            {
                Debug.LogWarning("[DragAreaProxy] No DraggableButton found on parent!");
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentDraggable != null)
        {
            parentDraggable.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentDraggable != null)
        {
            parentDraggable.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (parentDraggable != null)
        {
            parentDraggable.OnEndDrag(eventData);
        }
    }
}
