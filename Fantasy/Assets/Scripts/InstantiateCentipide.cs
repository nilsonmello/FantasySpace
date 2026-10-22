using UnityEngine;

public class InstantiateCentipide : MonoBehaviour
{
    [SerializeField] private GameObject centipidePrefab;
    [SerializeField] private Transform pivotPosition;

    void Awake()
    {
        pivotPosition = GetComponent<Transform>();
    }

    void Start()
    {
        if (centipidePrefab != null)
        {
            Vector3 pos = pivotPosition.transform.position;
            Instantiate(centipidePrefab, pos, transform.rotation);
        }
    }
}