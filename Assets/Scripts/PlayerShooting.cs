using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;
    [SerializeField] private int _maxAmmo = 10;

    public readonly SyncVar<int> CurrentAmmo = new(10);

    private PlayerNetwork _playerNetwork;
    private float _lastShotTime;

    private void Awake()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        CurrentAmmo.Value = _maxAmmo;
    }

    private void Update()
    {
        if (!base.IsOwner)
            return;

        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value)
            return;

        if (_firePoint == null)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootServerRpc(_firePoint.position, _firePoint.forward);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 position, Vector3 direction)
    {
        if (_playerNetwork == null)
            _playerNetwork = GetComponent<PlayerNetwork>();

        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value)
            return;

        if (_projectilePrefab == null)
            return;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        if (CurrentAmmo.Value <= 0)
            return;

        if (Time.time < _lastShotTime + _cooldown)
            return;

        _lastShotTime = Time.time;
        CurrentAmmo.Value--;

        GameObject projectileObject = Instantiate(
            _projectilePrefab,
            position + direction.normalized * 1.2f,
            Quaternion.LookRotation(direction)
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Init(OwnerId);
        }

        ServerManager.Spawn(projectileObject);
    }
}
