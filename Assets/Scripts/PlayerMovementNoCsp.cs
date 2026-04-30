using FishNet.Object;
using UnityEngine;

public class PlayerMovementNoCsp : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;

    private PlayerNetwork _playerNetwork;

    private void Awake()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    private void Update()
    {
        if (!base.IsOwner)
            return;

        if (!CanMove())
            return;

        if (GameManager.Instance != null && !GameManager.Instance.IsMatchInProgress)
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

        if (GameManager.Instance != null && !GameManager.Instance.IsMatchInProgress)
            return;

        Vector3 move = new Vector3(horizontal, 0f, vertical).normalized * _speed;
        transform.position += move * Time.deltaTime;
    }

    private bool CanMove()
    {
        bool alive = _playerNetwork == null || _playerNetwork.IsAlive.Value;
        bool matchInProgress = GameManager.Instance == null || GameManager.Instance.IsMatchInProgress;
        return alive && matchInProgress;
    }

    public void ResetMotion()
    {
    }
}
