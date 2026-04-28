using FishNet.Managing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private NetworkManager _networkManager;

    public static string PlayerNickname { get; private set; } = "Player";

    public void StartAsHost()
    {
        SaveNickname();
        ReleaseInputFieldFocus();

        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();

        if (_menuPanel != null)
            _menuPanel.SetActive(false);
    }

    public void StartAsClient()
    {
        SaveNickname();
        ReleaseInputFieldFocus();

        _networkManager.ClientManager.StartConnection();

        if (_menuPanel != null)
            _menuPanel.SetActive(false);
    }

    public void QuitGame()
    {
        if (_networkManager != null)
        {
            _networkManager.ClientManager.StopConnection();
            _networkManager.ServerManager.StopConnection(true);
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }

    private void ReleaseInputFieldFocus()
    {
        if (_nicknameInput != null)
            _nicknameInput.DeactivateInputField();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
