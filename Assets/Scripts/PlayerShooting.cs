using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;
    [SerializeField] private int _maxAmmo = 10;

    private float _lastShotTime;

    public NetworkVariable<int> CurrentAmmo = new NetworkVariable<int>(
        10,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private PlayerNetwork _playerNetwork;

    public override void OnNetworkSpawn()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();

        if (IsServer)
        {
            CurrentAmmo.Value = _maxAmmo;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootServerRpc(_firePoint.position, _firePoint.forward);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir, ServerRpcParams rpc = default)
    {
        ulong senderId = rpc.Receive.SenderClientId;

        if (!NetworkManager.ConnectedClients.ContainsKey(senderId))
            return;

        var playerObject = NetworkManager.ConnectedClients[senderId].PlayerObject;
        var playerNetwork = playerObject.GetComponent<PlayerNetwork>();
        var playerShooting = playerObject.GetComponent<PlayerShooting>();

        // Жив ли игрок?
        if (!playerNetwork.IsAlive.Value) return;

        // Есть ли патроны?
        if (playerShooting.CurrentAmmo.Value <= 0) return;

        // Прошёл ли кулдаун?
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        playerShooting.CurrentAmmo.Value--;

        var go = Instantiate(_projectilePrefab, pos + dir * 1.2f, Quaternion.LookRotation(dir));
        var no = go.GetComponent<NetworkObject>();
        no.SpawnWithOwnership(senderId);
    }
}