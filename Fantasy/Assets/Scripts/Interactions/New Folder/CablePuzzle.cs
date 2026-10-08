using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class DragMe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

    public static GameObject itemBeingDragged;
    Vector3 startPosition;
    [SerializeField] private bool colliding = false;

    private LineRenderer lineRenderer;

    [SerializeField] private Transform lineStart;
    [SerializeField] private Transform lineEnd;


    public void OnBeginDrag (PointerEventData eventData)
    {
        itemBeingDragged = gameObject;
        startPosition = transform.position;
    }

    public void OnDrag (PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag (PointerEventData eventData)
    {
        itemBeingDragged = null;

        if(colliding)
        {
            transform.position = lineEnd.position;            
        }
        else
        {
            transform.position = startPosition;            
        }
    }
}