using System.Collections;
using FishNet.Object;
using UnityEngine;

public class PickupManager : NetworkBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private bool _spawnedInitialPickups;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (_spawnedInitialPickups)
            return;

        _spawnedInitialPickups = true;
        SpawnAllPickups();
    }

    private void SpawnAllPickups()
    {
        if (_healthPickupPrefab == null)
            return;

        Transform[] spawnPoints = GetSpawnPoints();
        if (spawnPoints == null)
            return;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
                continue;

            SpawnPickup(spawnPoint.position);
        }
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
