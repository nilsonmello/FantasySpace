using UnityEngine;

// objeto de teste: só precisa estar dentro do range e na layer certa pra ser detectado pelo PlayerInteractor
public class SimpleInteractable : InteractionManager
{
    public override void interact()
    {
        Debug.Log("Interagiu");
    }
}