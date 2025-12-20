using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

public class NetworkStats : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsText;

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            statsText.text = "Offline";
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            // Hosts have 0 ping to the server (themselves).
            // We can show the count of connected clients instead.
            int clients = NetworkManager.Singleton.ConnectedClients.Count;
            statsText.text = $"Host (Clients: {clients})";
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            // Get RTT to the Server (ID 0)
            ulong rtt = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
            statsText.text = $"Ping: {rtt}ms";
        }
    }
}
