using TMPro;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _hpText;

    public void SetNickname(string nickname)
    {
        if (_nicknameText != null)
        {
            _nicknameText.text = nickname;
        }
    }

    public void SetHp(int hp)
    {
        if (_hpText != null)
        {
            _hpText.text = $"HP: {hp}";
        }
    }
}
