using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            var players = FindObjectsOfType<PlayerNetwork>();

            transform.position = new Vector3(players.Length * 2f, 1, 0);
        }
    }
}