using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private bool _spawnedOnce = false;

    private IEnumerator Start()
    {
        // Ждём, пока NetworkManager появится
        while (NetworkManager.Singleton == null)
            yield return null;

        // Ждём, пока запустят Host/Server
        while (!NetworkManager.Singleton.IsServer)
            yield return null;

        // Защита от повторного вызова
        if (_spawnedOnce)
            yield break;

        _spawnedOnce = true;
        SpawnAll();
    }

    private void SpawnAll()
    {
        foreach (Transform point in _spawnPoints)
        {
            if (point != null)
            {
                SpawnPickup(point.position);
            }
        }
    }

    public void OnPickedUp(Vector3 position)
    {
        StartCoroutine(RespawnAfterDelay(position));
    }

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnPickup(position);
    }

    private void SpawnPickup(Vector3 position)
    {
        GameObject go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);

        HealthPickup pickup = go.GetComponent<HealthPickup>();
        if (pickup != null)
        {
            pickup.Init(this);
        }

        NetworkObject netObj = go.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
    }
}