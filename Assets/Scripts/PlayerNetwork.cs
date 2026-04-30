using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public const int MaxHP = 100;
    public const float RespawnDelay = 3f;

    public readonly SyncVar<int> HP = new(MaxHP);
    public readonly SyncVar<string> Nickname = new("Player");
    public readonly SyncVar<bool> IsAlive = new(true);
    public readonly SyncVar<int> Score = new(0);

    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private PlayerView _playerView;
    [SerializeField] private Renderer[] _renderersToHideOnDeath;

    private Coroutine _respawnCoroutine;

    private void Awake()
    {
        HP.OnChange += OnHpChanged;
        Nickname.OnChange += OnNicknameChanged;
        IsAlive.OnChange += OnIsAliveChanged;
    }

    private void OnDestroy()
    {
        HP.OnChange -= OnHpChanged;
        Nickname.OnChange -= OnNicknameChanged;
        IsAlive.OnChange -= OnIsAliveChanged;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (base.Owner.IsLocalClient)
        {
            SetNicknameServerRpc(ConnectionUI.PlayerNickname);
        }

        UpdateHpText(HP.Value);
        UpdateNicknameText(Nickname.Value);
        SetVisible(IsAlive.Value);
    }

    public override void OnStopNetwork()
    {
        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = null;
        }

        base.OnStopNetwork();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        ResetPlayerServer(GetSpawnPosition(), false);
    }

    [ServerRpc]
    public void SetNicknameServerRpc(string nickname)
    {
        Nickname.Value = string.IsNullOrWhiteSpace(nickname)
            ? $"Player{OwnerId}"
            : nickname.Trim();
    }

    public void TakeDamage(int damage)
    {
        if (!base.IsServerInitialized)
            return;

        if (!IsAlive.Value)
            return;

        if (damage <= 0)
            return;

        HP.Value = Mathf.Max(0, HP.Value - damage);

        if (HP.Value <= 0)
            DieServer();
    }

    public void Heal(int amount)
    {
        if (!base.IsServerInitialized)
            return;

        if (!IsAlive.Value)
            return;

        if (amount <= 0)
            return;

        HP.Value = Mathf.Min(MaxHP, HP.Value + amount);
    }

    public void AddScoreServer(int amount)
    {
        if (!base.IsServerInitialized)
            return;

        if (amount <= 0)
            return;

        Score.Value += amount;
    }

    public void ResetScoreServer()
    {
        if (!base.IsServerInitialized)
            return;

        Score.Value = 0;
    }

    private void DieServer()
    {
        if (!base.IsServerInitialized)
            return;

        if (!IsAlive.Value)
            return;

        HP.Value = 0;
        IsAlive.Value = false;

        if (_respawnCoroutine != null)
            StopCoroutine(_respawnCoroutine);

        _respawnCoroutine = StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(RespawnDelay);

        Vector3 respawnPosition = GetSpawnPosition();
        ResetPlayerServer(respawnPosition, true, false);

        _respawnCoroutine = null;
    }

    public void ResetPlayerServer(Vector3 position, bool notifyOwner = true, bool stopRespawnRoutine = true)
    {
        if (!base.IsServerInitialized)
            return;

        if (stopRespawnRoutine && _respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = null;
        }

        TeleportLocal(position);

        HP.Value = MaxHP;
        IsAlive.Value = true;

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
            shooting.ResetAmmoServer();

        if (notifyOwner && base.Owner.IsValid)
            RespawnOwnerTargetRpc(base.Owner, position);
    }

    [TargetRpc]
    private void RespawnOwnerTargetRpc(NetworkConnection target, Vector3 respawnPosition)
    {
        TeleportLocal(respawnPosition);
    }

    private void OnHpChanged(int oldValue, int newValue, bool asServer)
    {
        UpdateHpText(newValue);
    }

    private void OnNicknameChanged(string oldValue, string newValue, bool asServer)
    {
        UpdateNicknameText(newValue);
    }

    private void OnIsAliveChanged(bool oldValue, bool newValue, bool asServer)
    {
        SetVisible(newValue);
    }

    private void UpdateHpText(int value)
    {
        if (_hpText != null)
        {
            _hpText.text = $"HP: {value}";
        }

        if (_playerView != null)
        {
            _playerView.SetHp(value);
        }
    }

    private void UpdateNicknameText(string value)
    {
        if (_nicknameText != null)
        {
            _nicknameText.text = value;
        }

        if (_playerView != null)
        {
            _playerView.SetNickname(value);
        }
    }

    private void TeleportLocal(Vector3 position)
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.ResetMotion();

        PlayerMovementNoCsp movementNoCsp = GetComponent<PlayerMovementNoCsp>();
        if (movementNoCsp != null)
            movementNoCsp.ResetMotion();

        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = false;

        transform.position = position;

        if (characterController != null)
            characterController.enabled = true;
    }

    private void SetVisible(bool visible)
    {
        if (_renderersToHideOnDeath == null)
            return;

        foreach (Renderer rendererToHide in _renderersToHideOnDeath)
        {
            if (rendererToHide != null)
            rendererToHide.enabled = visible;
        }
    }

    private Vector3 GetSpawnPosition()
    {
        return SpawnManager.Instance != null
            ? SpawnManager.Instance.GetSpawnPosition()
            : Vector3.zero;
    }
}
