using UnityEngine;

public class InteractionView : MonoBehaviour
{

    [SerializeField] private SpriteRenderer message;

    void Start()
    {
        message = GetComponent<SpriteRenderer>();
    }

    void Update()
    {

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            message.enabled = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            message.enabled = false;
        }
    }
}