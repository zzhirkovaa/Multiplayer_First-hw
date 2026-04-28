using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerHUD : NetworkBehaviour
{
    private TMP_Text _ammoText;
    private PlayerShooting _playerShooting;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!base.IsOwner)
        {
            enabled = false;
            return;
        }

        _playerShooting = GetComponent<PlayerShooting>();
        _ammoText = GameObject.Find("AmmoText")?.GetComponent<TMP_Text>();

        UpdateAmmoText();
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
            _ammoText.text = $"Ammo: {_playerShooting.CurrentAmmo.Value}";
        }
    }
}
