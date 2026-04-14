using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;
    [SerializeField] private int _maxAmmo = 10;

    private float _lastShotTime;
    private int _currentAmmo;
    public int CurrentAmmo => _currentAmmo;
    private PlayerNetwork _playerNetwork;

    public override void OnNetworkSpawn()
    {
        _currentAmmo = _maxAmmo;
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Мёртвый игрок не стреляет
        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value) return;

        if (Input.GetKeyDown(KeyCode.Space))
            ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir, ServerRpcParams rpc = default)
    {
        ulong senderId = rpc.Receive.SenderClientId;

        if (!NetworkManager.ConnectedClients.ContainsKey(senderId))
            return;

        var playerObject = NetworkManager.ConnectedClients[senderId].PlayerObject;
        var playerNetwork = playerObject.GetComponent<PlayerNetwork>();

        // 1. Жив ли игрок?
        if (!playerNetwork.IsAlive.Value) return;

        // 2. Есть ли патроны?
        if (_currentAmmo <= 0) return;

        // 3. Прошёл ли кулдаун?
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        _currentAmmo--;

        var go = Instantiate(_projectilePrefab, pos + dir * 1.2f, Quaternion.LookRotation(dir));
        var no = go.GetComponent<NetworkObject>();
        no.SpawnWithOwnership(senderId);
    }
}