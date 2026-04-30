using System.Collections;
using FishNet.Object;
using UnityEngine;

public class PickupManager : NetworkBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private bool _spawnedInitialPickups;

    private void Start()
    {
        StartCoroutine(WaitForServerAndSpawnRoutine());
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        StartServerLogic();
    }

    private IEnumerator WaitForServerAndSpawnRoutine()
    {
        while (!base.IsServerInitialized)
            yield return null;

        StartServerLogic();
    }

    private void StartServerLogic()
    {
        if (_spawnedInitialPickups)
            return;

        if (SpawnAllPickups())
            _spawnedInitialPickups = true;
    }

    private bool SpawnAllPickups()
    {
        if (_healthPickupPrefab == null)
        {
            Debug.LogWarning("[PickupManager] Health pickup prefab is not assigned.");
            return false;
        }

        Transform[] spawnPoints = GetSpawnPoints();
        if (spawnPoints == null)
        {
            Debug.LogWarning("[PickupManager] No pickup spawn points found.");
            return false;
        }

        int spawnedCount = 0;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
                continue;

            SpawnPickup(spawnPoint.position);
            spawnedCount++;
        }

        Debug.Log($"[PickupManager] Spawned {spawnedCount} health pickups.");
        return spawnedCount > 0;
    }

    private Transform[] GetSpawnPoints()
    {
        if (_spawnPoints != null && _spawnPoints.Length > 0)
            return _spawnPoints;

        int childCount = transform.childCount;
        if (childCount == 0)
            return null;

        Transform[] childSpawnPoints = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
            childSpawnPoints[i] = transform.GetChild(i);

        return childSpawnPoints;
    }

    private void SpawnPickup(Vector3 position)
    {
        GameObject pickupObject = Instantiate(_healthPickupPrefab, position, Quaternion.identity);

        HealthPickup healthPickup = pickupObject.GetComponent<HealthPickup>();
        if (healthPickup != null)
            healthPickup.Init(this, position);

        ServerManager.Spawn(pickupObject);
    }

    public void OnPickedUp(Vector3 spawnPosition)
    {
        if (!base.IsServerInitialized)
            return;

        StartCoroutine(RespawnPickupRoutine(spawnPosition));
    }

    private IEnumerator RespawnPickupRoutine(Vector3 spawnPosition)
    {
        yield return new WaitForSeconds(_respawnDelay);

        if (!base.IsServerInitialized)
            yield break;

        SpawnPickup(spawnPosition);
    }
}
