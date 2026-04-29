using System.Collections;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerHUD : NetworkBehaviour
{
    private TMP_Text _ammoText;
    private TMP_Text _respawnText;
    private PlayerShooting _playerShooting;
    private PlayerNetwork _playerNetwork;
    private Coroutine _respawnCountdownCoroutine;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!base.IsOwner)
        {
            enabled = false;
            return;
        }

        _playerShooting = GetComponent<PlayerShooting>();
        _playerNetwork = GetComponent<PlayerNetwork>();
        _ammoText = GameObject.Find("AmmoText")?.GetComponent<TMP_Text>();
        _respawnText = GameObject.Find("RespawnText")?.GetComponent<TMP_Text>();

        if (_playerNetwork != null)
            _playerNetwork.IsAlive.OnChange += OnIsAliveChanged;

        UpdateAmmoText();
        SetRespawnText(string.Empty);
    }

    public override void OnStopClient()
    {
        if (_playerNetwork != null)
            _playerNetwork.IsAlive.OnChange -= OnIsAliveChanged;

        if (_respawnCountdownCoroutine != null)
        {
            StopCoroutine(_respawnCountdownCoroutine);
            _respawnCountdownCoroutine = null;
        }

        base.OnStopClient();
    }

    private void Update()
    {
        if (!base.IsOwner)
            return;

        UpdateAmmoText();
    }

    private void UpdateAmmoText()
    {
        if (_ammoText != null && _playerShooting != null)
        {
            _ammoText.text = $"Патроны: {_playerShooting.CurrentAmmo.Value}";
        }
    }

    private void OnIsAliveChanged(bool oldValue, bool newValue, bool asServer)
    {
        if (newValue)
        {
            if (_respawnCountdownCoroutine != null)
            {
                StopCoroutine(_respawnCountdownCoroutine);
                _respawnCountdownCoroutine = null;
            }

            SetRespawnText(string.Empty);
            return;
        }

        if (_respawnCountdownCoroutine != null)
            StopCoroutine(_respawnCountdownCoroutine);

        _respawnCountdownCoroutine = StartCoroutine(RespawnCountdownRoutine());
    }

    private IEnumerator RespawnCountdownRoutine()
    {
        float remainingTime = PlayerNetwork.RespawnDelay;

        while (remainingTime > 0f)
        {
            SetRespawnText($"Возрождение через: {Mathf.CeilToInt(remainingTime)}");
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        SetRespawnText(string.Empty);
        _respawnCountdownCoroutine = null;
    }

    private void SetRespawnText(string value)
    {
        if (_respawnText != null)
            _respawnText.text = value;
    }
}
