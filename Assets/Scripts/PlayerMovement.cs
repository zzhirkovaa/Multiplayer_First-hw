using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -9.81f;

    private CharacterController _cc;
    private float _verticalVelocity;
    private PlayerNetwork _playerNetwork;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Мёртвый игрок не двигается
        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Camera cam = Camera.main;

        Vector3 move;

        if (cam != null)
        {
            Vector3 forward = cam.transform.forward;
            Vector3 right = cam.transform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            move = (forward * v + right * h).normalized * _speed;
        }
        else
        {
            move = new Vector3(h, 0f, v).normalized * _speed;
        }

        _verticalVelocity += _gravity * Time.deltaTime;
        move.y = _verticalVelocity;

        _cc.Move(move * Time.deltaTime);

        if (_cc.isGrounded)
            _verticalVelocity = 0f;
    }

    public void ResetMotion()
    {
        _verticalVelocity = 0f;
    }
}