using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private GameObject _menuPanel;

    public static string PlayerNickname { get; private set; } = "Player";

    public void StartAsHost()
    {
        SaveNickname();
        ReleaseInputFieldFocus();
        NetworkManager.Singleton.StartHost();

        if (_menuPanel != null)
            _menuPanel.SetActive(false);
    }

    public void StartAsClient()
    {
        SaveNickname();
        ReleaseInputFieldFocus();
        NetworkManager.Singleton.StartClient();

        if (_menuPanel != null)
            _menuPanel.SetActive(false);
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
        Debug.Log($"Nickname saved: {PlayerNickname}");
    }

    private void ReleaseInputFieldFocus()
    {
        if (_nicknameInput != null)
        {
            _nicknameInput.DeactivateInputField();
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void QuitGame()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        Debug.Log("Выход из игры");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
