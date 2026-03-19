using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private GameObject _menuPanel;

    public static string PlayerNickname { get; private set; } = "Player";

    public void StartAsHost()
    {
        SaveNickname();
        NetworkManager.Singleton.StartHost();
                                             
        if (_menuPanel != null)
            _menuPanel.SetActive(false);
    }

    public void StartAsClient()
    {
        SaveNickname();
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

    private Mouse _mouse;
    void Start()
    {
        _mouse = Mouse.current;
    }

    void Update()
    {
        if (_mouse != null)
        {
            if (Keyboard.current.hKey.wasPressedThisFrame)
                StartAsHost();
            if (Keyboard.current.cKey.wasPressedThisFrame)
                StartAsClient();
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
