using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class ConnectionHandler : MonoBehaviour
{
    public static ConnectionHandler Instance { get; private set; }

    // Dictionary to store names: ClientID -> Name
    private Dictionary<ulong, string> clientNames = new Dictionary<ulong, string>();

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // 1. Decode Payload
        byte[] payloadBytes = request.Payload;
        string payloadJson = Encoding.UTF8.GetString(payloadBytes);
        ConnectionPayload payload = JsonUtility.FromJson<ConnectionPayload>(payloadJson);

        // 2. Store Name (Server Side Only)
        // Note: We can't use request.ClientNetworkId yet as it's not fully assigned in all contexts,
        // but for NGO 1.0+ this is safe in Approval.
        if (clientNames.ContainsKey(request.ClientNetworkId))
            clientNames[request.ClientNetworkId] = payload.playerName;
        else
            clientNames.Add(request.ClientNetworkId, payload.playerName);

        // 3. Quest 11: Calculate Random Spawn Position
        Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = new Vector3(randomPoint.x, randomPoint.y, 0f);

        // 4. Approve & Set Spawn
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = spawnPos;
        response.Rotation = Quaternion.identity;

        Debug.Log($"Approved {payload.playerName} at {spawnPos}");
    }

    // Helper to retrieve name for PlayerNetworkState
    public string GetPlayerName(ulong clientId)
    {
        if (clientNames.TryGetValue(clientId, out string name))
        {
            return name;
        }
        return "Unknown Survivor";
    }

    private void OnServerStarted()
    {
        // Host (Client 0) needs to register themselves manually if payload was missed or local
        if (!clientNames.ContainsKey(0))
        {
            clientNames.Add(0, PlayerPrefs.GetString("PlayerName", "Host"));
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientNames.ContainsKey(clientId))
        {
            clientNames.Remove(clientId);
        }
    }
}
