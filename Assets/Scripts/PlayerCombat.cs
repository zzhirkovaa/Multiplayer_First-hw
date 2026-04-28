using FishNet.Object;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private int _damage = 25;
    [SerializeField] private float _attackDistance = 3f;
    [SerializeField] private LayerMask _hitMask = ~0;

    private Camera _camera;
    private PlayerNetwork _playerNetwork;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _playerNetwork = GetComponent<PlayerNetwork>();

        if (base.IsOwner)
            _camera = Camera.main;
    }

    private void Update()
    {
        if (!base.IsOwner)
            return;

        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value)
            return;

        if (Input.GetMouseButtonDown(0))
            TryAttack();
    }

    private void TryAttack()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_camera == null)
            return;

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, _attackDistance, _hitMask))
            return;

        PlayerNetwork targetPlayer = hit.collider.GetComponent<PlayerNetwork>();
        if (targetPlayer == null)
            targetPlayer = hit.collider.GetComponentInParent<PlayerNetwork>();

        if (targetPlayer == null)
            return;

        NetworkObject targetNetworkObject = targetPlayer.GetComponent<NetworkObject>();
        if (targetNetworkObject == null)
            return;

        DealDamageServerRpc(targetNetworkObject, _damage);
    }

    [ServerRpc]
    private void DealDamageServerRpc(NetworkObject targetNetworkObject, int damage)
    {
        if (_playerNetwork == null)
            _playerNetwork = GetComponent<PlayerNetwork>();

        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value)
            return;

        if (targetNetworkObject == null)
            return;

        PlayerNetwork targetPlayer = targetNetworkObject.GetComponent<PlayerNetwork>();
        if (targetPlayer == null)
            return;

        if (targetPlayer == _playerNetwork)
            return;

        targetPlayer.TakeDamage(damage);
    }
}
