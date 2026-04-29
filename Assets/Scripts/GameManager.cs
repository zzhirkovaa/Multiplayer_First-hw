using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public bool IsMatchInProgress => CurrentState.Value == GameState.InProgress;

    private bool _serverEventsSubscribed;
    private GameObject _runtimePanel;
    private TMP_Text _runtimeStatusText;

    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        ShowingResults
    }

    private void Awake()
    {
        Instance = this;
        Debug.Log("[GameManager] Awake. Lobby manager is loaded.");

        CurrentState.OnChange += OnGameStateChanged;
        ConnectedPlayers.OnChange += OnConnectedPlayersChanged;
        MatchTimer.Value = _matchDuration;

        CreateRuntimeUi();
        RefreshRuntimeUi();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        CurrentState.OnChange -= OnGameStateChanged;
        ConnectedPlayers.OnChange -= OnConnectedPlayersChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        StartServerLogic();
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (base.IsServerInitialized)
            StartServerLogic();

        RefreshRuntimeUi();
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
    }

    private void StartMatch()
    {
        MatchTimer.Value = _matchDuration;
        CurrentState.Value = GameState.InProgress;

        Debug.Log("[Server] Match started!");
    }

    private void EndMatch()
    {
        CurrentState.Value = GameState.ShowingResults;
        Debug.Log("[Server] Match ended! Showing results...");

        Invoke(nameof(ResetToLobby), _resultsDuration);
    }

    private void ResetToLobby()
    {
        MatchTimer.Value = _matchDuration;
        CurrentState.Value = GameState.WaitingForPlayers;

        Debug.Log("[Server] Lobby reset. Waiting for players...");

        if (ConnectedPlayers.Value >= _requiredPlayers)
            StartMatch();
    }

    private void OnGameStateChanged(GameState oldValue, GameState newValue, bool asServer)
    {
        Debug.Log($"Game state changed: {oldValue} -> {newValue}");
        RefreshRuntimeUi();
    }

    private void OnConnectedPlayersChanged(int oldValue, int newValue, bool asServer)
    {
        Debug.Log($"Connected players: {newValue}/{_requiredPlayers}");
        RefreshRuntimeUi();
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

        if (CurrentState.Value == GameState.WaitingForPlayers)
        {
            _runtimePanel.SetActive(true);
            _runtimeStatusText.text = $"Waiting for players: {ConnectedPlayers.Value}/{_requiredPlayers}";
        }
        else if (CurrentState.Value == GameState.InProgress)
        {
            _runtimePanel.SetActive(true);
            _runtimeStatusText.text = $"Time: {Mathf.CeilToInt(MatchTimer.Value)}";
        }
        else if (CurrentState.Value == GameState.ShowingResults)
        {
            _runtimePanel.SetActive(true);
            _runtimeStatusText.text = "Match ended. Returning to lobby...";
        }
    }
}
