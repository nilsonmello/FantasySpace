using UnityEngine;
using UnityEngine.InputSystem;

// fica no player, cuida de achar o interactable mais próximo e disparar a interação
public class PlayerInteract : MonoBehaviour
{
    // raio de busca, maior que o range de cada objeto pra sobrar margem na hora de filtrar
    [SerializeField] private float detectionRadius = 3f;

    // layer só dos objetos interagíveis, evita pegar collider de chão, parede
    [SerializeField] private LayerMask interactableLayer;

    // ação de interagir vinda do Input System (arraste a InputActionReference no Inspector)
    private InputAction  interactAction;
    private InputSystem_Actions actions;

    // guarda o interactable mais próximo nesse frame, dá pra usar isso pra mostrar UI tipo "aperte E" ou fazer highlight
    public InteractionManager CurrentTarget { get; private set; }

    

    private void OnEnable()
    {
        actions = new InputSystem_Actions();
        interactAction = actions.Player.Interact;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.Disable();
    }


    private void Update()
    {
        // atualiza quem é o alvo mais próximo todo frame, antes de checar se apertou o botão
        CurrentTarget = FindClosestInteractable();

        // só interage se tiver um alvo válido e o botão tiver sido apertado nesse frame
        if (CurrentTarget != null && interactAction.WasPressedThisFrame())
        {
            CurrentTarget.interact();
        }

    }

    private InteractionManager FindClosestInteractable()
    {
        // pega todo mundo interagível dentro do raio de detecção usando a layer que filtramos
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, interactableLayer);

        InteractionManager closest = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            // se o collider não tem um Interactable colado, ignora e vai pro próximo
            if (!hit.TryGetComponent(out InteractionManager interactable)) continue;

            // se o objeto tá temporariamente bloqueado (ex: porta já aberta), ignora também
            if (!interactable.Caninteract()) continue;

            float distance = Vector2.Distance(transform.position, hit.transform.position);

            // só considera válido se o player realmente estiver dentro do range específico DAQUELE objeto
            if (distance > interactable.interactionRange) continue;

            // guarda o mais próximo até agora, pra não interagir com um objeto errado quando tem vários por perto
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        return closest;
    }
}