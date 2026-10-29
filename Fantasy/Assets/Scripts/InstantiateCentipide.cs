using UnityEngine;

public class InstantiateCentipide : MonoBehaviour
{
    [SerializeField] private GameObject centipidePrefab;
    [SerializeField] private Transform centipidePivot;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerPivot;

    void Awake()
    {
        centipidePivot = GetComponent<Transform>();
        playerPivot = GetComponent<Transform>();
    }

    void Start()
    {
        InstantiateEntity();
    }

    private void InstantiateEntity()
    {
        if (centipidePrefab != null)
        {
            Vector3 pos = centipidePivot.transform.position;
            Instantiate(centipidePrefab, pos, transform.rotation);
        }

        if (playerPrefab != null)
        {
            Vector3 pos = playerPivot.transform.position;
            Instantiate(playerPrefab, pos, transform.rotation);
        }
    }
}