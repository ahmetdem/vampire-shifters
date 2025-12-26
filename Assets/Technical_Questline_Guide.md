# CENG462 Multiplayer Game Jam - Technical Questline Implementation Guide

This document maps the **Mandatory Multiplayer Technical Quests** from the CENG462 Handbook to the specific scripts and logic implemented in the **Vampire Shifters** project. Use this as a reference for your GDD, presentation, and oral defense.

---

## STARRED (MANDATORY) QUESTS
*These are the critical grading checkpoints. You must be able to explain these in detail.*

### Quest 6 - Gatekeeper Approval (Mandatory)
**Goal:** Server receives per-client data (Name + Auth ID) on connect and approves/denies.

- **Files:** `ConnectionHandler.cs`, `NetworkPayload.cs`, `LobbyManager.cs`
- **Implementation:**
    1.  **Payload (Client):** In `LobbyManager.cs`, before connecting, we create a JSON payload containing the local `PlayerPrefs` name and Unity Services `AuthId`. This is assigned to `NetworkConfig.ConnectionData`.
    2.  **Approval (Server):** In `ConnectionHandler.cs`, the `ApprovalCheck` method intercepts the connection request.
    3.  **Logic:** It deserializes the JSON payload, stores the player name in a server-side dictionary (`clientNames`), and approves the connection.
    4.  **Spawn:** The response object is assigned a random spawn position via `GetRandomSpawnPosition()`.
- **Teacher Answer:** *"We deserialize the JSON payload byte array, extract the player name and auth ID, store them in server-side dictionaries, and then approve the connection with a random spawn position."*

### Quest 7 - Presence & Departure (Mandatory)
**Goal:** Handle connection/disconnection events correctly on both server and client.

- **Files:** `ConnectionHandler.cs` (Server), `DisconnectHandler.cs` (Client)
- **Implementation:**
    -   **Server-Side:** usage of `OnClientDisconnectCallback` in `ConnectionHandler`. When a client drops, we remove their entry from `clientNames`, `deathCounts`, and other sizing dictionaries to prevent memory leaks and logical errors.
    -   **Client-Side:** The `DisconnectHandler` script listens for the local client's disconnection or transport failure. If detected, it shuts down the `NetworkManager` and loads the Main Menu scene.
- **Teacher Answer:** *"Server-side cleans up lookup dictionaries when players leave. Client-side detects its own disconnection via LocalClientId comparison, shuts down networking, and returns to menu gracefully."*

### Quest 14 - State Packet Crafting (Mandatory)
**Goal:** Create a custom serializable network type for data sync.

- **Files:** `LeaderboardEntry.cs`
- **Implementation:**
    -   We defined a struct `LeaderboardEntry` that implements `INetworkSerializable` and `IEquatable<LeaderboardEntry>`.
    -   **Why `INetworkSerializable`?** To define exactly how the data (ClientId, Name, Coins, Level, Deaths) is packed into the byte stream.
    -   **Why `FixedString32Bytes`?** Standard C# strings are not "blittable" and compliant with Unity Netcode structs; fixed strings are required for struct serialization.
- **Teacher Answer:** *"We implemented a custom struct with INetworkSerializable to efficiently pack player stats (ID, Name, Coins, Level) into a single network variable update, rather than syncing 5 different variables."*

### Quest 17 - Live Score Feed (Mandatory)
**Goal:** Leaderboard updates in real time as players earn coins.

- **Files:** `LeaderboardManager.cs` (Server), `LeaderboardUI.cs` (Client)
- **Implementation:**
    -   **Server:** `LeaderboardManager` maintains a `NetworkList<LeaderboardEntry>`. It runs a periodic refresh (every 1s) to scan all `PlayerEconomy` scripts and update the list.
    -   **Client:** `LeaderboardUI` subscribes to the `OnListChanged` event of the `NetworkList`. The UI is **event-driven**; it only rebuilds the visual list when the server actually modifies the data, ensuring efficiency.
- **Teacher Answer:** *"Server modifies the NetworkList, which automatically replicates to clients. Clients subscribe to OnListChanged events and update their UI reactively, without polling in Update()."*

### Quest 18 - Rank Order & Self Highlight (Mandatory)
**Goal:** Sort leaderboard and highlight local player.

- **Files:** `LeaderboardUI.cs`, `LeaderboardManager.cs`
- **Implementation:**
    -   **Sorting:** `LeaderboardManager.GetSortedEntries()` sorts the list first by **Level (Descending)**, then by **Deaths (Ascending)** as a tie-breaker.
    -   **Highlighting:** Inside `LeaderboardUI`, as we iterate through the entries to spawn UI rows, we compare `entry.ClientId` with `NetworkManager.Singleton.LocalClientId`. If they match, we tint the text yellow to highlight the local player.
- **Teacher Answer:** *"We sort by level descending so the highest level is top. The local player is identified by comparing the entry's ClientId with the LocalClientId and highlighted in yellow."*

---

## NON-STARRED QUESTS (Quick Summary)

### 1. Warp Gate Handshake
- **Implementation:** Used **Relay** with **DTLS** (Datagram Transport Layer Security).
- **Code:** `LobbyManager.cs` uses `SetRelayServerData(..., "dtls")`.

### 2. Lobby Observatory UI
- **Implementation:** `LobbyManager.cs` fetches lobbies via `LobbyService.Instance.QueryLobbiesAsync` and spawns a `LobbyItem` prefab for each result in a ScrollView.

### 3. Host Beacon Protocol
- **Implementation:** Host creates a lobby with `Data["joinCode"]` set to the Relay code. `LobbyBeat.cs` sends a heartbeat ping every 15 seconds to keep the lobby alive.

### 4. Drop-In Boarding
- **Implementation:** Clients click "Join" on a UI item, which calls `JoinLobbyByIdAsync`, retrieves the hidden join code, and starts the Relay connection.

### 5. Callsign Forge
- **Implementation:** `GameBootstrap.cs` handles name input before connection, saving it to `PlayerPrefs` and validating length (2-12 chars).

### 8. Shutdown Discipline
- **Implementation:** `LobbyBeat` handles cleanup. `OnApplicationQuit` calls `DeleteLobbyAsync` to remove the lobby from UGS so it doesn't become a "ghost lobby."

### 9. Personal Camera Rig
- **Implementation:** `CameraFollow.cs` checks `if (IsOwner)` inside `OnNetworkSpawn`. Only the local player assigns the Cinemachine camera to follow their transform.

### 10. Overhead Identity Tags
- **Implementation:** `PlayerNetworkState.cs` has a `NetworkVariable<FixedString32Bytes> playerName`. The server sets this on spawn, and clients listen to `OnValueChanged` to update the world-space UI text.

### 11. Spawn Constellations
- **Implementation:** `ConnectionHandler.GetRandomSpawnPosition()` calculates a random point inside a unit circle, checks for collision overlap, and returns a valid vector.

### 12. Second Chances (Respawn)
- **Implementation:** `HandlePlayerDeath` (Server) destroys the player object, waits for a delay (that increases with death count), and then re-instantiates the player prefab using `SpawnAsPlayerObject`.

### 13 & 15. Scoreboard & Roster Sync
- **Implementation:** Handled by the `NetworkList` in `LeaderboardManager`. Adding/removing players automatically syncs the "roster" count to all clients.

---

## Quick Reference Table

| Quest | Mandatory | Key File(s) | Critical Method/Class |
|-------|---|-------------|----------------------|
| 1. Relay DTLS | | `LobbyManager.cs` | `SetRelayServerData("dtls")` |
| 2. Lobby UI | | `LobbyManager.cs` | `RefreshLobbyList()` |
| 3. Host Beacon | | `LobbyManager.cs`, `LobbyBeat.cs` | `SendHeartbeatPingAsync()` |
| 4. Join Lobby | | `LobbyManager.cs` | `JoinLobby()` |
| 5. Name Entry | | `GameBootstrap.cs` | `PlayerPrefs.SetString("PlayerName")` |
| **6. Approval** | YES | `ConnectionHandler.cs` | `ApprovalCheck()` |
| **7. Disconnect** | YES | `ConnectionHandler.cs` | `OnClientDisconnect()` |
| 8. Cleanup | | `LobbyBeat.cs` | `DeleteLobbyAsync()` |
| 9. Camera | | `CameraFollow.cs` | `if (IsOwner)` |
| 10. Names | | `PlayerNetworkState.cs` | `NetworkVariable<FixedString32Bytes>` |
| 11. Spawn | | `ConnectionHandler.cs` | `GetRandomSpawnPosition()` |
| 12. Respawn | | `ConnectionHandler.cs` | `RespawnRoutine()` |
| **14. Serializable** | YES | `LeaderboardEntry.cs` | `INetworkSerializable`, `IEquatable<T>` |
| **17. Live Feed** | YES | `LeaderboardManager.cs` | `OnListChanged` event |
| **18. Sort+Highlight** | YES | `LeaderboardUI.cs` | `GetSortedEntries()`, `LocalClientId` |
