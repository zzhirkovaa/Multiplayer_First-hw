using FishNet;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    private void Start()
    {
        if (Application.isBatchMode)
        {
            Debug.Log("[Server] Headless mode detected. Starting server...");
            InstanceFinder.ServerManager.StartConnection();

            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("[Server] GameManager was not found in the loaded scene.");
                return;
            }

            Debug.Log("[Server] GameManager found. Starting game cycle logic...");
            gameManager.StartServerLogic();
        }
    }
}