using System.Text;
using Unity.Netcode;
using UnityEngine;

public class ConnectionHandler : MonoBehaviour
{
    private void Start()
    {
        // Subscribe to the approval check
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Quest 6: Gatekeeper Approval

        // 1. Read Payload
        byte[] payloadBytes = request.Payload;
        string payloadJson = Encoding.UTF8.GetString(payloadBytes);
        ConnectionPayload payload = JsonUtility.FromJson<ConnectionPayload>(payloadJson);

        // 2. Logic (Here you can ban IPs, check versions, etc. For now, just approve)
        response.Approved = true;
        response.CreatePlayerObject = true;

        // Quest 11: Spawn Constellations (You can set position here later)
        response.Position = Vector3.zero;
        response.Rotation = Quaternion.identity;

        // Log who is joining (Server side log)
        Debug.Log($"Approval Check: Player {payload.playerName} ({request.ClientNetworkId}) connecting...");

        // NOTE: You cannot assign the name to the player object HERE yet because the object
        // hasn't been spawned. You usually store this in a dictionary <ulong, string>
        // to assign it in OnNetworkSpawn later.
    }
}
