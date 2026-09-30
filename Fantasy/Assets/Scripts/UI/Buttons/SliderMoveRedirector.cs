using UnityEngine;
using UnityEngine.EventSystems;

public class SliderMoveRedirector : MonoBehaviour, IMoveHandler
{
    [Header("Reference")]
    public VolumeSlider targetSlider;

    public void OnMove(AxisEventData eventData)
    {
        if (targetSlider == null) return;

        if (eventData.moveDir == MoveDirection.Left)
        {
            targetSlider.Decrease();
            eventData.Use(); 
        }
        else if (eventData.moveDir == MoveDirection.Right)
        {
            targetSlider.Increase();
            eventData.Use(); 
        }
    }
}