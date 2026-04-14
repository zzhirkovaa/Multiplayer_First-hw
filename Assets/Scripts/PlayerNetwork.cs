using Unity.Netcode.Components;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> Nickname = new NetworkVariable<FixedString32Bytes>(
        new FixedString32Bytes("Player"),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> HP = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }

        HP.OnValueChanged += OnHpChanged;
        IsAlive.OnValueChanged += OnIsAliveChanged;
    }

    public override void OnNetworkDespawn()
    {
        HP.OnValueChanged -= OnHpChanged;
        IsAlive.OnValueChanged -= OnIsAliveChanged;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname)
            ? $"Player_{OwnerClientId}"
            : nickname.Trim();

        Nickname.Value = safeValue;
        gameObject.name = safeValue;
    }

    private void OnHpChanged(int prev, int next)
    {

        if (!IsServer) return;

        if (next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(3f);

        Vector3 respawnPosition = Vector3.zero;

        if (SpawnManager.Instance != null)
        {
            respawnPosition = SpawnManager.Instance.GetSpawnPosition();
        }

        RespawnOwnerClientRpc(respawnPosition, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        });

        yield return null;

        HP.Value = 100;
        IsAlive.Value = true;
    }

    private void OnIsAliveChanged(bool prev, bool next)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = next;
        }
    }

    [ClientRpc]
    private void RespawnOwnerClientRpc(Vector3 respawnPosition, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner)
            return;

        CharacterController cc = GetComponent<CharacterController>();
        NetworkTransform networkTransform = GetComponent<NetworkTransform>();
        PlayerMovement movement = GetComponent<PlayerMovement>();

        if (movement != null)
        {
            movement.ResetMotion();
        }

        if (cc != null)
            cc.enabled = false;

        if (networkTransform != null)
        {
            networkTransform.Teleport(respawnPosition, transform.rotation, transform.localScale);
        }
        else
        {
            transform.position = respawnPosition;
        }

        if (cc != null)
            cc.enabled = true;
    }

}