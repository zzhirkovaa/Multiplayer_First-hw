using Unity.Netcode;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 40;

    private PickupManager _manager;
    private Vector3 _spawnPosition;
    private bool _pickedUp = false;

    public void Init(PickupManager manager)
    {
        _manager = manager;
        _spawnPosition = transform.position;
        _pickedUp = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;
        if (_pickedUp) return;

        PlayerNetwork player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        // Мёртвый не подбирает
        if (!player.IsAlive.Value) return;

        // Не лечим при полном HP
        if (player.HP.Value >= 100) return;

        _pickedUp = true;

        player.HP.Value = Mathf.Min(100, player.HP.Value + _healAmount);

        if (_manager != null)
        {
            _manager.OnPickedUp(_spawnPosition);
        }

        NetworkObject.Despawn(true);
    }
}