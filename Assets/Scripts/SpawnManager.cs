using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [SerializeField] private Transform[] _spawnPoints;
    private int _nextSpawnIndex;

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 GetSpawnPosition()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
            return Vector3.zero;

        Transform spawnPoint = _spawnPoints[_nextSpawnIndex % _spawnPoints.Length];
        _nextSpawnIndex++;

        return spawnPoint.position;
    }
}
