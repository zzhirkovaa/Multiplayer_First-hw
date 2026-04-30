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

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;

    private PlayerNetwork _playerNetwork;

    private void Awake()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();
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
            bool canMove = CanMove();

            MoveData moveData = new MoveData
            {
                Horizontal = canMove ? Input.GetAxisRaw("Horizontal") : 0f,
                Vertical = canMove ? Input.GetAxisRaw("Vertical") : 0f
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
            Position = transform.position
        };

        Reconcile(reconcileData);
    }

    [Replicate]
    private void Move(MoveData moveData, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        float tickDelta = (float)base.TimeManager.TickDelta;

        bool canMove = _playerNetwork == null || _playerNetwork.IsAlive.Value;
        canMove = canMove && IsMatchInProgress();
        Vector3 move = canMove
            ? new Vector3(moveData.Horizontal, 0f, moveData.Vertical).normalized * _speed
            : Vector3.zero;

        transform.position += move * tickDelta;
    }

    [Reconcile]
    private void Reconcile(ReconcileData reconcileData, Channel channel = Channel.Unreliable)
    {
        transform.position = reconcileData.Position;
    }

    public void ResetMotion()
    {
    }

    private bool CanMove()
    {
        bool alive = _playerNetwork == null || _playerNetwork.IsAlive.Value;
        return alive && IsMatchInProgress();
    }

    private bool IsMatchInProgress()
    {
        return GameManager.Instance == null || GameManager.Instance.IsMatchInProgress;
    }

}
