using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class TestRelay : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TextMeshProUGUI statusText; // Optional: Assign a text object to see status on screen

    private async void Start()
    {
        // Initialize Unity Services (Required for Relay/Lobby)
        await UnityServices.InitializeAsync();

        // Sign in anonymously
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        hostButton.onClick.AddListener(CreateRelay);
        joinButton.onClick.AddListener(() => JoinRelay(joinCodeInput.text));
    }

    // Quest 1: Relay Create (Host)
    private async void CreateRelay()
    {
        try
        {
            // Request allocation for 3 connections (host + 3 clients)
            Allocation allocation = await Relay.Instance.CreateAllocationAsync(3);

            // Get the Join Code to share with the client
            string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"JOIN CODE: {joinCode}");
            if (statusText) statusText.text = $"Code: {joinCode}";

            // Setup Transport for DTLS
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(allocation, "dtls"));

            // Start Host
            NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    // Quest 1: Relay Join (Client)
    private async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"Joining Relay with {joinCode}");

            // Join allocation using the code
            JoinAllocation joinAllocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            // Setup Transport for DTLS
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(joinAllocation, "dtls"));

            // Start Client
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }
}
