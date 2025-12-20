using Unity.Netcode.Components;
using UnityEngine;

// Allows the Owner (Client) to update the position, not just the Server.
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // This tells NGO: "Trust the Client Owner for position updates"
    }
}
