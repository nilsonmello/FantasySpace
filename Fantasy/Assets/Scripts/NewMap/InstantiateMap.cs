using System;
using UnityEngine;

public class InstantiateMap : MonoBehaviour
{
    [SerializeField] private GameObject[] mapPrefabs;
    [SerializeField] private bool avoidRepeatingLastMap = true;

    public GameObject CurrentMap { get; private set; }
    public event Action<GameObject> MapReady;

    private static int lastIndex = -1;

    private void Start()
    {
        SpawnMap();
    }

    public void SpawnMap()
    {
        if (mapPrefabs == null || mapPrefabs.Length == 0)
        {
            return;
        }

        if (CurrentMap != null) Destroy(CurrentMap);

        int index = PickIndex();
        CurrentMap = Instantiate(mapPrefabs[index], Vector3.zero, Quaternion.identity);

        var generator = CurrentMap.GetComponentInChildren<ColorMapGenerator>(true);
        if (generator != null) generator.Generate();

        MapReady?.Invoke(CurrentMap);
    }

    private int PickIndex()
    {
        int index = UnityEngine.Random.Range(0, mapPrefabs.Length);

        if (avoidRepeatingLastMap && mapPrefabs.Length > 1)
        {
            while (index == lastIndex)
                index = UnityEngine.Random.Range(0, mapPrefabs.Length);
        }

        lastIndex = index;
        return index;
    }
}