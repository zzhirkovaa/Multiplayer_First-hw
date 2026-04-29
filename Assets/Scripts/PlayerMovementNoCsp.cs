using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementNoCsp : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -9.81f;

    private CharacterController _characterController;
    private PlayerNetwork _playerNetwork;
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    private void Update()
    {
        if (!base.IsOwner)
            return;

        if (!CanMove())
            return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        MoveServerRpc(horizontal, vertical);
    }

    [ServerRpc]
    private void MoveServerRpc(float horizontal, float vertical)
    {
        if (_playerNetwork == null)
            _playerNetwork = GetComponent<PlayerNetwork>();

        if (!CanMove())
            return;

        float delta = Time.deltaTime;
        Vector3 move = new Vector3(horizontal, 0f, vertical).normalized * _speed;

        _verticalVelocity += _gravity * delta;
        move.y = _verticalVelocity;

        _characterController.Move(move * delta);

        if (_characterController.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = 0f;
    }

    private bool CanMove()
    {
        bool alive = _playerNetwork == null || _playerNetwork.IsAlive.Value;
        bool matchInProgress = GameManager.Instance == null || GameManager.Instance.IsMatchInProgress;
        return alive && matchInProgress;
    }
}
