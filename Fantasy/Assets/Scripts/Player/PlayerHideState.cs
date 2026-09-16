using UnityEngine;

public class PlayerHideState : MonoBehaviour
{
    public bool IsHidden { get; private set; }

    public void SetHidden(bool hidden)
    {
        IsHidden = hidden;
    }
}