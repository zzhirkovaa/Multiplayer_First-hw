using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _gamePanel;
    [SerializeField] private GameObject _resultsPanel;
    [SerializeField] private TMP_Text _lobbyText;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _resultsText;

    private GameManager _gameManager;

    private void Start()
    {
        _gameManager = FindFirstObjectByType<GameManager>();

        if (_gameManager == null)
        {
            Debug.LogWarning("GameUI could not find GameManager in the scene.");
            return;
        }

        _gameManager.CurrentState.OnChange += OnGameStateChanged;
        _gameManager.ConnectedPlayers.OnChange += OnConnectedPlayersChanged;
        _gameManager.MatchTimer.OnChange += OnMatchTimerChanged;

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (_gameManager == null)
            return;

        _gameManager.CurrentState.OnChange -= OnGameStateChanged;
        _gameManager.ConnectedPlayers.OnChange -= OnConnectedPlayersChanged;
        _gameManager.MatchTimer.OnChange -= OnMatchTimerChanged;
    }

    private void OnGameStateChanged(GameManager.GameState oldValue, GameManager.GameState newValue, bool asServer)
    {
        RefreshPanels();
    }

    private void OnConnectedPlayersChanged(int oldValue, int newValue, bool asServer)
    {
        RefreshLobbyText();
    }

    private void OnMatchTimerChanged(float oldValue, float newValue, bool asServer)
    {
        RefreshTimerText();
    }

    private void RefreshAll()
    {
        RefreshPanels();
        RefreshLobbyText();
        RefreshTimerText();
        RefreshResultsText();
    }

    private void RefreshPanels()
    {
        GameManager.GameState state = _gameManager.CurrentState.Value;

        if (_lobbyPanel != null)
            _lobbyPanel.SetActive(state == GameManager.GameState.WaitingForPlayers);

        if (_gamePanel != null)
            _gamePanel.SetActive(state == GameManager.GameState.InProgress);

        if (_resultsPanel != null)
            _resultsPanel.SetActive(state == GameManager.GameState.ShowingResults);

        RefreshResultsText();
    }

    private void RefreshLobbyText()
    {
        if (_lobbyText == null || _gameManager == null)
            return;

        _lobbyText.text = $"Waiting for players: {_gameManager.ConnectedPlayers.Value}/{_gameManager.RequiredPlayers}";
    }

    private void RefreshTimerText()
    {
        if (_timerText == null || _gameManager == null)
            return;

        int seconds = Mathf.CeilToInt(_gameManager.MatchTimer.Value);
        _timerText.text = $"Time: {seconds}";
    }

    private void RefreshResultsText()
    {
        if (_resultsText == null || _gameManager == null)
            return;

        _resultsText.text = "Match ended";
    }
}
