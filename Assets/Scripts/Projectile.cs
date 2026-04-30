using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private int _damage = 20;

    private int _ownerId = -1;

    public void Init(int ownerId)
    {
        _ownerId = ownerId;
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized)
            return;

        PlayerNetwork target = other.GetComponent<PlayerNetwork>();
        if (target == null)
            target = other.GetComponentInParent<PlayerNetwork>();

        if (target == null)
            return;

        if (!target.IsAlive.Value)
            return;

        if (target.OwnerId == _ownerId)
            return;

        bool wasAlive = target.IsAlive.Value;
        target.TakeDamage(_damage);

        if (wasAlive && !target.IsAlive.Value)
            AwardKillScore();

        ServerManager.Despawn(gameObject);
    }

    private void AwardKillScore()
    {
        foreach (var connection in ServerManager.Clients.Values)
        {
            foreach (NetworkObject networkObject in connection.Objects)
            {
                PlayerNetwork player = networkObject.GetComponent<PlayerNetwork>();
                if (player == null)
                    continue;

                if (player.OwnerId != _ownerId)
                    continue;

                player.AddScoreServer(1);
                return;
            }
        }
    }
}
