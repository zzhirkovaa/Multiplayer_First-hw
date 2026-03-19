using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private int _damage = 10;

    private Camera _playerCamera;
    private Mouse _mouse; 

    void Start()
    {
        if (IsOwner)
        {
            _playerCamera = Camera.main;
            _mouse = Mouse.current; 
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (_mouse != null && _mouse.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (_playerCamera == null)
        {
            Debug.LogError("Камера не найдена!");
            return;
        }

        Vector2 mousePosition = _mouse.position.ReadValue();
        Ray ray = _playerCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            PlayerNetwork targetPlayer = hit.collider.GetComponent<PlayerNetwork>();

            if (targetPlayer == null || targetPlayer == _playerNetwork)
            {
                return;
            }

            DealDamageServerRpc(targetPlayer.NetworkObjectId, _damage);
        }
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetObjectId, int damage)
    {
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject targetObject))
        {
            PlayerNetwork targetPlayer = targetObject.GetComponent<PlayerNetwork>();

            if (targetPlayer == null || targetPlayer == _playerNetwork) return;

            int nextHp = Mathf.Max(0, targetPlayer.HP.Value - damage);
            targetPlayer.HP.Value = nextHp;
        }
    }
}