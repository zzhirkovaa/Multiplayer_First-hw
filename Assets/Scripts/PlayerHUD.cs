using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerHUD : NetworkBehaviour
{
    private TMP_Text _ammoText;
    private TMP_Text _respawnText;

    private PlayerShooting _playerShooting;
    private PlayerNetwork _playerNetwork;

    private float _respawnTimer = 0f;
    private bool _countRespawn = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        _playerShooting = GetComponent<PlayerShooting>();
        _playerNetwork = GetComponent<PlayerNetwork>();

        // ИЩЕМ UI НА СЦЕНЕ
        _ammoText = GameObject.Find("AmmoText")?.GetComponent<TMP_Text>();
        _respawnText = GameObject.Find("RespawnText")?.GetComponent<TMP_Text>();

        if (_playerNetwork != null)
        {
            _playerNetwork.IsAlive.OnValueChanged += OnIsAliveChanged;
        }

        if (_respawnText != null)
            _respawnText.gameObject.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        if (_playerNetwork != null)
        {
            _playerNetwork.IsAlive.OnValueChanged -= OnIsAliveChanged;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (_playerShooting != null && _ammoText != null)
        {
            _ammoText.text = $"Патроны: {_playerShooting.CurrentAmmo}";
        }

        if (_countRespawn && _respawnText != null)
        {
            _respawnTimer -= Time.deltaTime;

            int secondsLeft = Mathf.CeilToInt(_respawnTimer);
            _respawnText.text = $"Возрождение через: {Mathf.Max(0, secondsLeft)}";

            if (_respawnTimer <= 0f)
                _countRespawn = false;
        }
    }

    private void OnIsAliveChanged(bool prev, bool next)
    {
        if (!IsOwner) return;

        if (!next)
        {
            _respawnTimer = 3f;
            _countRespawn = true;

            if (_respawnText != null)
                _respawnText.gameObject.SetActive(true);
        }
        else
        {
            _countRespawn = false;

            if (_respawnText != null)
                _respawnText.gameObject.SetActive(false);
        }
    }
}