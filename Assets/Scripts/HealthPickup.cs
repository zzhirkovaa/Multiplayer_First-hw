using FishNet.Object;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 40;

    private PickupManager _pickupManager;
    private Vector3 _spawnPosition;
    private bool _pickedUp;

    public void Init(PickupManager pickupManager, Vector3 spawnPosition)
    {
        _pickupManager = pickupManager;
        _spawnPosition = spawnPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPickup(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryPickup(other);
    }

    private void TryPickup(Collider other)
    {
        if (!base.IsServerInitialized)
            return;

        if (_pickedUp)
            return;

        PlayerNetwork player = other.GetComponent<PlayerNetwork>();
        if (player == null)
            player = other.GetComponentInParent<PlayerNetwork>();

        if (player == null)
            return;

        if (!player.IsAlive.Value)
            return;

        if (player.HP.Value >= PlayerNetwork.MaxHP)
            return;

        _pickedUp = true;
        player.Heal(_healAmount);

        if (_pickupManager != null)
            _pickupManager.OnPickedUp(_spawnPosition);

        ServerManager.Despawn(gameObject);
    }
}
