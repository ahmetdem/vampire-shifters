using Unity.Collections;
using Unity.Netcode;
using TMPro;
using UnityEngine;

public class PlayerNetworkState : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameLabel;

    // Quest 10: Network Variable to sync name automatically
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>();

    public override void OnNetworkSpawn()
    {
        // 1. If we are the Server, we need to assign the name from the ConnectionHandler
        if (IsServer)
        {
            // We ask the ConnectionHandler "Who owns this ClientID?"
            string assignedName = ConnectionHandler.Instance.GetPlayerName(OwnerClientId);
            playerName.Value = assignedName;
        }

        // 2. Everyone (Client & Server) updates the UI when the value changes
        playerName.OnValueChanged += OnNameChanged;

        // Initial update for late joiners
        UpdateNameLabel(playerName.Value.ToString());
    }

    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged -= OnNameChanged;
    }

    private void OnNameChanged(FixedString32Bytes oldVal, FixedString32Bytes newVal)
    {
        UpdateNameLabel(newVal.ToString());
    }

    private void UpdateNameLabel(string name)
    {
        nameLabel.text = name;
    }
}
