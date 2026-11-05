using System;
using UnityEngine;

public class MapSpawnController : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private InstantiateMap mapSource;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject centipidePrefab;
    [SerializeField] private GameObject getOutPrefab;

    [SerializeField] private string playerSpawnTag = "PlayerSpawn";
    [SerializeField] private string centipideSpawnTag = "CentipideSpawn";
    [SerializeField] private string getOutTag = "GetOutPoint";

    [Header("Player")]
    [SerializeField] private bool reuseExistingPlayer = true;
    [SerializeField] private string playerTag = "Player";

    public GameObject Player { get; private set; }
    public GameObject Centipide { get; private set; }
    public GameObject GetOut { get; private set; }

    public Transform PlayerSpawn { get; private set; }
    public Transform CentipideSpawn { get; private set; }
    public Transform GetOutPoint { get; private set; }

    public event Action<MapSpawnController> Spawned;

    private bool subscribed;

    private void Awake()
    {
        if (mapSource == null)
            mapSource = FindFirstObjectByType<InstantiateMap>();
    }

    private void OnEnable()
    {
        if (mapSource == null || subscribed) return;

        mapSource.MapReady += HandleMapReady;
        subscribed = true;
    }

    private void OnDisable()
    {
        if (mapSource != null && subscribed)
            mapSource.MapReady -= HandleMapReady;

        subscribed = false;
    }

    private void Start()
    {
        if (mapSource == null)
        {
            return;
        }

        if (mapSource.CurrentMap != null && PlayerSpawn == null)
            HandleMapReady(mapSource.CurrentMap);
    }

    private void HandleMapReady(GameObject map)
    {
        Spawn(map);
    }

    public void Spawn(GameObject mapRoot)
    {
        if (mapRoot == null) return;

        ClearSpawned();

        PlayerSpawn = FindPoint(mapRoot, playerSpawnTag);
        CentipideSpawn = FindPoint(mapRoot, centipideSpawnTag);
        GetOutPoint = FindPoint(mapRoot, getOutTag);

        SpawnPlayer();
        SpawnCentipide();
        SpawnGetOut();

        Spawned?.Invoke(this);
    }

    public void ClearSpawned()
    {
        if (Centipide != null) Destroy(Centipide);
        if (GetOut != null) Destroy(GetOut);

        Player = null;
        Centipide = null;
        GetOut = null;
        PlayerSpawn = null;
        CentipideSpawn = null;
        GetOutPoint = null;
    }

    private void SpawnPlayer()
    {
        if (PlayerSpawn == null) return;

        if (reuseExistingPlayer)
        {
            var existing = GameObject.FindGameObjectWithTag(playerTag);
            if (existing != null)
            {
                Player = existing;
                MoveTo(existing, PlayerSpawn);
                return;
            }
        }

        if (playerPrefab != null)
            Player = Instantiate(playerPrefab, PlayerSpawn.position, PlayerSpawn.rotation);
    }

    private void SpawnCentipide()
    {
        if (CentipideSpawn == null || centipidePrefab == null) return;

        Centipide = Instantiate(centipidePrefab, CentipideSpawn.position, CentipideSpawn.rotation);
    }

    private void SpawnGetOut()
    {
        if (GetOutPoint == null || getOutPrefab == null) return;

        GetOut = Instantiate(getOutPrefab, GetOutPoint.position, GetOutPoint.rotation, GetOutPoint);
    }

    private Transform FindPoint(GameObject mapRoot, string tag)
    {
        foreach (Transform t in mapRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.CompareTag(tag))
                return t;
        }

        return null;
    }

    private static void MoveTo(GameObject obj, Transform point)
    {
        var rb = obj.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = point.position;
        }

        obj.transform.SetPositionAndRotation(point.position, point.rotation);
    }
}