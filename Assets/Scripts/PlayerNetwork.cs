using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public readonly SyncVar<int> HP = new(100);

    public readonly SyncVar<string> Nickname = new("Player");

    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private PlayerView _playerView;

    private void Awake()
    {
        HP.OnChange += OnHpChanged;
        Nickname.OnChange += OnNicknameChanged;
    }

    private void OnDestroy()
    {
        HP.OnChange -= OnHpChanged;
        Nickname.OnChange -= OnNicknameChanged;
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
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
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

        HP.Value = Mathf.Max(0, HP.Value - damage);
    }

    private void OnHpChanged(int oldValue, int newValue, bool asServer)
    {
        UpdateHpText(newValue);
    }

    private void OnNicknameChanged(string oldValue, string newValue, bool asServer)
    {
        UpdateNicknameText(newValue);
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
}
