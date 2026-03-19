using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

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

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerClientId}" : nickname.Trim();
        Nickname.Value = safeValue;

        gameObject.name = safeValue;
    }
}