using FishNet.Connection;
using FishNet;
using FishNet.Broadcast;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct GameStateBroadcast : IBroadcast
{
    public int State;
    public int ConnectedPlayers;
    public int RequiredPlayers;
    public float MatchTimer;
    public string ResultsText;
}

public class GameManager : NetworkBehaviour
{
    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _matchDuration = 60f;
    [SerializeField] private float _resultsDuration = 5f;

    public readonly SyncVar<GameState> CurrentState = new(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new(0);
    public readonly SyncVar<float> MatchTimer = new(60f);

    public int RequiredPlayers => _requiredPlayers;
    public static GameManager Instance { get; private set; }
    public bool IsMatchInProgress => base.IsServerInitialized
        ? CurrentState.Value == GameState.InProgress
        : _displayState == GameState.InProgress;

    private bool _serverEventsSubscribed;
    private GameObject _runtimePanel;
    private TMP_Text _runtimeStatusText;
    private GameState _displayState = GameState.WaitingForPlayers;
    private int _displayConnectedPlayers;
    private int _displayRequiredPlayers = 2;
    private float _displayMatchTimer = 60f;
    private string _displayResultsText = "Match ended. Returning to lobby...";
    private float _nextTimerBroadcastTime;
    private bool _clientBroadcastRegistered;

    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        ShowingResults
    }

    private void Awake()
    {
        Instance = this;
        CurrentState.OnChange += OnGameStateChanged;
        ConnectedPlayers.OnChange += OnConnectedPlayersChanged;
        MatchTimer.Value = _matchDuration;
        _displayRequiredPlayers = _requiredPlayers;
        CopyNetworkStateToDisplayState();

        CreateRuntimeUi();
        RefreshRuntimeUi();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        CurrentState.OnChange -= OnGameStateChanged;
        ConnectedPlayers.OnChange -= OnConnectedPlayersChanged;

        UnregisterClientBroadcast();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        StartServerLogic();
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        RegisterClientBroadcast();

        if (base.IsServerInitialized)
            StartServerLogic();

        RefreshRuntimeUi();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        RegisterClientBroadcast();
        RefreshRuntimeUi();
    }

    public override void OnStopClient()
    {
        UnregisterClientBroadcast();

        base.OnStopClient();
    }

    public void StartServerLogic()
    {
        if (_serverEventsSubscribed)
            return;

        Debug.Log("[GameManager] Server started. Waiting for players...");
        base.ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;
        _serverEventsSubscribed = true;
        UpdateConnectedPlayers();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (_serverEventsSubscribed && base.ServerManager != null)
        {
            base.ServerManager.OnRemoteConnectionState -= OnPlayerConnectionChanged;
            _serverEventsSubscribed = false;
        }
    }

    private void Update()
    {
        if (!base.IsServerInitialized)
            return;

        if (Time.time >= _nextTimerBroadcastTime)
        {
            _nextTimerBroadcastTime = Time.time + 0.25f;
            SendGameStateBroadcast();
        }

        if (CurrentState.Value != GameState.InProgress)
            return;

        MatchTimer.Value -= Time.deltaTime;

        if (MatchTimer.Value <= 0f)
            EndMatch();
    }

    private void LateUpdate()
    {
        RefreshRuntimeUi();
    }

    private void OnPlayerConnectionChanged(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (!base.IsServerInitialized)
            return;

        Debug.Log($"[GameManager] Connection state changed: {args.ConnectionState}");
        UpdateConnectedPlayers();

        if (CurrentState.Value == GameState.WaitingForPlayers &&
            ConnectedPlayers.Value >= _requiredPlayers)
        {
            StartMatch();
        }
    }

    private void UpdateConnectedPlayers()
    {
        ConnectedPlayers.Value = base.ServerManager.Clients.Count;
        SendGameStateBroadcast();
    }

    private void StartMatch()
    {
        MatchTimer.Value = _matchDuration;
        CurrentState.Value = GameState.InProgress;
        SendGameStateBroadcast();

        Debug.Log("[Server] Match started!");
    }

    private void EndMatch()
    {
        CurrentState.Value = GameState.ShowingResults;
        SendGameStateBroadcast();
        Debug.Log("[Server] Match ended! Showing results...");

        Invoke(nameof(ResetToLobby), _resultsDuration);
    }

    private void ResetToLobby()
    {
        ResetPlayersForNextMatch();

        MatchTimer.Value = _matchDuration;
        CurrentState.Value = GameState.WaitingForPlayers;
        SendGameStateBroadcast();

        Debug.Log("[Server] Lobby reset. Waiting for players...");

        if (ConnectedPlayers.Value >= _requiredPlayers)
            StartMatch();
    }

    private void ResetPlayersForNextMatch()
    {
        if (!base.IsServerInitialized)
            return;

        foreach (NetworkConnection connection in base.ServerManager.Clients.Values)
        {
            foreach (NetworkObject networkObject in connection.Objects)
            {
                PlayerNetwork playerNetwork = networkObject.GetComponent<PlayerNetwork>();
                if (playerNetwork == null)
                    continue;

                Vector3 spawnPosition = SpawnManager.Instance != null
                    ? SpawnManager.Instance.GetSpawnPosition()
                    : Vector3.zero;

                playerNetwork.ResetPlayerServer(spawnPosition);
                playerNetwork.ResetScoreServer();
            }
        }
    }

    private void OnGameStateChanged(GameState oldValue, GameState newValue, bool asServer)
    {
        Debug.Log($"Game state changed: {oldValue} -> {newValue}");
        CopyNetworkStateToDisplayState();
        RefreshRuntimeUi();
    }

    private void OnConnectedPlayersChanged(int oldValue, int newValue, bool asServer)
    {
        Debug.Log($"Connected players: {newValue}/{_requiredPlayers}");
        CopyNetworkStateToDisplayState();
        RefreshRuntimeUi();
    }

    private void OnGameStateBroadcast(GameStateBroadcast message, Channel channel)
    {
        _displayState = (GameState)message.State;
        _displayConnectedPlayers = message.ConnectedPlayers;
        _displayRequiredPlayers = message.RequiredPlayers;
        _displayMatchTimer = message.MatchTimer;
        _displayResultsText = message.ResultsText;

        RefreshRuntimeUi();
    }

    private void RegisterClientBroadcast()
    {
        if (_clientBroadcastRegistered || InstanceFinder.ClientManager == null)
            return;

        InstanceFinder.ClientManager.RegisterBroadcast<GameStateBroadcast>(OnGameStateBroadcast);
        _clientBroadcastRegistered = true;
    }

    private void UnregisterClientBroadcast()
    {
        if (!_clientBroadcastRegistered || InstanceFinder.ClientManager == null)
            return;

        InstanceFinder.ClientManager.UnregisterBroadcast<GameStateBroadcast>(OnGameStateBroadcast);
        _clientBroadcastRegistered = false;
    }

    private void SendGameStateBroadcast()
    {
        if (!base.IsServerInitialized || base.ServerManager == null || !base.ServerManager.Started)
            return;

        GameStateBroadcast message = new GameStateBroadcast
        {
            State = (int)CurrentState.Value,
            ConnectedPlayers = ConnectedPlayers.Value,
            RequiredPlayers = _requiredPlayers,
            MatchTimer = MatchTimer.Value,
            ResultsText = CurrentState.Value == GameState.ShowingResults
                ? BuildResultsText()
                : "Match ended. Returning to lobby..."
        };

        base.ServerManager.Broadcast(message, false, Channel.Reliable);
    }

    private string BuildResultsText()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Results");

        bool hasPlayers = false;
        foreach (NetworkConnection connection in base.ServerManager.Clients.Values)
        {
            foreach (NetworkObject networkObject in connection.Objects)
            {
                PlayerNetwork playerNetwork = networkObject.GetComponent<PlayerNetwork>();
                if (playerNetwork == null)
                    continue;

                hasPlayers = true;
                string nickname = string.IsNullOrWhiteSpace(playerNetwork.Nickname.Value)
                    ? $"Player{playerNetwork.OwnerId}"
                    : playerNetwork.Nickname.Value;

                builder.AppendLine($"{nickname}: {playerNetwork.Score.Value}");
            }
        }

        if (!hasPlayers)
            builder.AppendLine("No players");

        builder.AppendLine("Returning to lobby...");
        return builder.ToString();
    }

    private void CopyNetworkStateToDisplayState()
    {
        _displayState = CurrentState.Value;
        _displayConnectedPlayers = ConnectedPlayers.Value;
        _displayRequiredPlayers = _requiredPlayers;
        _displayMatchTimer = MatchTimer.Value;
    }

    private void CreateRuntimeUi()
    {
        if (Application.isBatchMode)
            return;

        if (_runtimeStatusText != null)
            return;

        GameObject canvasObject = new GameObject("GameStateCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _runtimePanel = new GameObject("GameStatePanel");
        _runtimePanel.transform.SetParent(canvasObject.transform, false);

        Image panelImage = _runtimePanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform panelRect = _runtimePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -20f);
        panelRect.sizeDelta = new Vector2(620f, 80f);

        GameObject textObject = new GameObject("GameStateText");
        textObject.transform.SetParent(_runtimePanel.transform, false);

        _runtimeStatusText = textObject.AddComponent<TextMeshProUGUI>();
        _runtimeStatusText.alignment = TextAlignmentOptions.Center;
        _runtimeStatusText.fontSize = 28f;
        _runtimeStatusText.color = Color.white;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void RefreshRuntimeUi()
    {
        if (_runtimeStatusText == null || _runtimePanel == null)
            return;

        if (_displayState == GameState.WaitingForPlayers)
        {
            _runtimePanel.SetActive(true);
            SetRuntimePanelHeight(80f);
            _runtimeStatusText.text = $"Waiting for players: {_displayConnectedPlayers}/{_displayRequiredPlayers}";
        }
        else if (_displayState == GameState.InProgress)
        {
            _runtimePanel.SetActive(true);
            SetRuntimePanelHeight(80f);
            _runtimeStatusText.text = $"Time: {Mathf.CeilToInt(_displayMatchTimer)}";
        }
        else if (_displayState == GameState.ShowingResults)
        {
            _runtimePanel.SetActive(true);
            SetRuntimePanelHeight(220f);
            _runtimeStatusText.text = _displayResultsText;
        }
    }

    private void SetRuntimePanelHeight(float height)
    {
        RectTransform panelRect = _runtimePanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, height);
    }
}
