using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public struct MoveData : IReplicateData
{
    public float Horizontal;
    public float Vertical;

    private uint _tick;

    public void Dispose()
    {
    }

    public uint GetTick()
    {
        return _tick;
    }

    public void SetTick(uint value)
    {
        _tick = value;
    }
}

public struct ReconcileData : IReconcileData
{
    public Vector3 Position;
    public float VerticalVelocity;

    private uint _tick;

    public void Dispose()
    {
    }

    public uint GetTick()
    {
        return _tick;
    }

    public void SetTick(uint value)
    {
        _tick = value;
    }
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -9.81f;

    private CharacterController _characterController;
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        base.TimeManager.OnTick += OnTick;
    }

    public override void OnStopNetwork()
    {
        base.TimeManager.OnTick -= OnTick;
        base.OnStopNetwork();
    }

    private void OnTick()
    {
        if (base.IsOwner)
        {
            MoveData moveData = new MoveData
            {
                Horizontal = Input.GetAxisRaw("Horizontal"),
                Vertical = Input.GetAxisRaw("Vertical")
            };

            Move(moveData);
        }
        else
        {
            Move(default);
        }

        CreateReconcile();
    }

    public override void CreateReconcile()
    {
        ReconcileData reconcileData = new ReconcileData
        {
            Position = transform.position,
            VerticalVelocity = _verticalVelocity
        };

        Reconcile(reconcileData);
    }

    [Replicate]
    private void Move(MoveData moveData, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        float tickDelta = (float)base.TimeManager.TickDelta;

        Vector3 move = GetCameraRelativeMove(moveData.Horizontal, moveData.Vertical);

        _verticalVelocity += _gravity * tickDelta;
        move.y = _verticalVelocity;

        _characterController.Move(move * tickDelta);

        if (_characterController.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = 0f;
        }
    }

    [Reconcile]
    private void Reconcile(ReconcileData reconcileData, Channel channel = Channel.Unreliable)
    {
        bool wasEnabled = _characterController.enabled;
        _characterController.enabled = false;
        transform.position = reconcileData.Position;
        _characterController.enabled = wasEnabled;

        _verticalVelocity = reconcileData.VerticalVelocity;
    }

    public void ResetMotion()
    {
        _verticalVelocity = 0f;
    }

    private Vector3 GetCameraRelativeMove(float horizontal, float vertical)
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            return new Vector3(horizontal, 0f, vertical).normalized * _speed;
        }

        Vector3 forward = camera.transform.forward;
        Vector3 right = camera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return (forward * vertical + right * horizontal).normalized * _speed;
    }
}
