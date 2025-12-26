using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Cinemachine; // Required for Camera switching

public class BossEventDirector : NetworkBehaviour
{
    public static BossEventDirector Instance;

    [Header("Setup")]
    [SerializeField] private Transform arenaSpawnPoint;
    [SerializeField] private GameObject bossPrefab;

    // NEW: Reference to the main spawner so we can disable it
    [SerializeField] private EnemySpawner mainEnemySpawner;

    // NEW: Reference to a static camera that covers the whole arena
    [SerializeField] private CinemachineVirtualCamera bossArenaCamera;

    [Header("Settings")]
    public float bossTimerDuration = 300f;
    private float currentTimer;
    public NetworkVariable<bool> isEventActive = new NetworkVariable<bool>(false);
    public bool IsEventActive => isEventActive.Value;

    private void Awake()
    {
        Instance = this;
        // Ensure the boss camera is off by default
        if (bossArenaCamera != null) bossArenaCamera.gameObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentTimer = 0f;
            isEventActive.Value = false;
        }
    }

    private void Update()
    {
        if (!IsServer || isEventActive.Value) return;

        currentTimer += Time.deltaTime;
        if (currentTimer >= bossTimerDuration)
        {
            StartBossEvent();
        }
    }

    public void ForceStartEvent()
    {
        if (IsServer && !isEventActive.Value) StartBossEvent();
    }

    private void StartBossEvent()
    {
        isEventActive.Value = true;
        Debug.Log(">>> BOSS EVENT STARTED <<<");

        // 1. STOP NORMAL ENEMY SPAWNS
        if (mainEnemySpawner != null)
        {
            // You need to add this public method to your EnemySpawner script!
            mainEnemySpawner.StopSpawning();
        }

        if (PvPDirector.Instance != null)
        {
            PvPDirector.Instance.IsPvPActive.Value = false; // Force PvP flag off
            PvPDirector.Instance.DisablePvPCamera();        // Force Camera off
        }

        // 2. TELEPORT & FIX CAMERA (Client Side)
        TeleportAndSwitchCameraClientRpc(arenaSpawnPoint.position);

        // 3. SPAWN BOSS
        SpawnBoss();
    }

    [ClientRpc]
    private void TeleportAndSwitchCameraClientRpc(Vector3 pos)
    {
        // A. Enable the Boss Camera with high priority
        // Because it has higher priority (setup below), Cinemachine will snap to it
        if (bossArenaCamera != null)
        {
            bossArenaCamera.gameObject.SetActive(true);
            bossArenaCamera.Priority = 20; // Higher than player camera
        }

        // B. Teleport Local Player
        // We only move the player belonging to this specific client
        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;

            if (player.TryGetComponent(out Rigidbody2D rb)) rb.velocity = Vector2.zero;
            player.transform.position = pos;
        }
    }

    private void SpawnBoss()
    {
        Vector3 bossPos = arenaSpawnPoint.position + new Vector3(0, 5, 0);
        GameObject boss = Instantiate(bossPrefab, bossPos, Quaternion.identity);
        boss.GetComponent<NetworkObject>().Spawn();
    }

    [ClientRpc]
    public void ResetCameraClientRpc()
    {
        // Turn off the boss camera so the default follow camera takes priority again
        if (bossArenaCamera != null)
        {
            bossArenaCamera.Priority = 0; // Reset priority
            bossArenaCamera.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Resets the boss camera for a specific client only (e.g., when they die during boss fight).
    /// Other clients who are still alive will keep fighting the boss.
    /// </summary>
    [ClientRpc]
    public void ResetCameraForClientRpc(ulong clientId)
    {
        // Only the dead player should reset their camera
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        if (bossArenaCamera != null)
        {
            bossArenaCamera.Priority = 0; // Reset priority
            bossArenaCamera.gameObject.SetActive(false);
            Debug.Log($"[BossEventDirector] Camera reset for dead player {clientId}");
        }
    }

    /// <summary>
    /// Teleports a specific player to the forest.
    /// Called from ConnectionHandler after respawning a player to ensure proper position sync.
    /// </summary>
    public void TeleportPlayerToForestRpc(ulong clientId, Vector3 targetPos)
    {
        if (!IsServer) return;
        
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        TeleportPlayerClientRpc(targetPos, clientRpcParams);
    }

    [ClientRpc]
    private void TeleportPlayerClientRpc(Vector3 position, ClientRpcParams clientRpcParams = default)
    {
        // Reset any arena cameras first
        if (bossArenaCamera != null)
        {
            bossArenaCamera.Priority = 0;
            bossArenaCamera.gameObject.SetActive(false);
        }

        if (PvPDirector.Instance != null)
        {
            PvPDirector.Instance.DisablePvPCamera();
        }

        // Teleport the player
        if (NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            
            if (player.TryGetComponent(out Rigidbody2D rb))
            {
                rb.velocity = Vector2.zero;
            }
            
            player.transform.position = position;
            Debug.Log($"[BossEventDirector] Player teleported to respawn position: {position}");
        }
    }

    public void OnBossDefeated()
    {
        // 1. Logic runs only on Server
        if (!IsServer) return;
        isEventActive.Value = false;
        
        // Reset the timer so boss doesn't immediately respawn
        currentTimer = 0f;

        Debug.Log(">>> BOSS DEFEATED! RETURNING TO FOREST <<<");

        if (mainEnemySpawner != null)
        {
            mainEnemySpawner.StartSpawning();
        }

        // 2. Loop through players on the Server
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            // Server calculates a random spot for this specific player
            Vector3 targetPos = ConnectionHandler.Instance.GetRandomSpawnPosition();

            // Server whispers to this client: "Go to this specific spot"
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { client.ClientId }
                }
            };

            ReturnToForestClientRpc(targetPos, clientRpcParams);
        }
    }

    /// <summary>
    /// Called when a player dies during the boss event (killed by boss).
    /// Resets the event and resumes normal enemy spawning.
    /// Teleports all surviving players back to the forest.
    /// </summary>
    public void ResetBossEventOnPlayerDeath()
    {
        if (!IsServer) return;
        
        Debug.Log(">>> PLAYER DIED TO BOSS - RESETTING EVENT <<<");
        
        // Reset event state
        isEventActive.Value = false;
        currentTimer = 0f;
        
        // Resume normal enemy spawning
        if (mainEnemySpawner != null)
        {
            mainEnemySpawner.StartSpawning();
        }
        
        // Hide boss health bar on all clients BEFORE despawning the boss
        HideBossHealthBarClientRpc();
        
        // Destroy any existing boss
        BossHealth[] bosses = FindObjectsOfType<BossHealth>();
        foreach (var boss in bosses)
        {
            if (boss.TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
            {
                netObj.Despawn(true);
            }
        }
        
        // Teleport all surviving players back to the forest
        // (The dead player will respawn via RespawnRoutine)
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            // Skip if player object doesn't exist (they're respawning via RespawnRoutine)
            if (client.PlayerObject == null) continue;

            // Server calculates a random spot for this specific player
            Vector3 targetPos = ConnectionHandler.Instance.GetRandomSpawnPosition();

            // Server whispers to this client: "Go to this specific spot"
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { client.ClientId }
                }
            };

            ReturnToForestClientRpc(targetPos, clientRpcParams);
        }
    }

    [ClientRpc]
    private void ReturnToForestClientRpc(Vector3 targetPos, ClientRpcParams clientRpcParams = default)
    {
        // Client just follows orders. No math, no checking lists.

        if (bossArenaCamera != null)
        {
            bossArenaCamera.Priority = 0; // Reset priority
            bossArenaCamera.gameObject.SetActive(false);
        }

        if (PvPDirector.Instance != null)
        {
            PvPDirector.Instance.DisablePvPCamera();
        }

        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;

            if (player.TryGetComponent(out Rigidbody2D rb)) rb.velocity = Vector2.zero;

            // Move to the spot the server picked
            player.transform.position = targetPos;
        }
    }
    
    /// <summary>
    /// Tell all clients to hide the boss health bar.
    /// Called when player dies to boss and event is reset.
    /// </summary>
    [ClientRpc]
    private void HideBossHealthBarClientRpc()
    {
        BossHealthBar healthBar = FindObjectOfType<BossHealthBar>();
        if (healthBar != null)
        {
            healthBar.HideBossHealth();
            Debug.Log("[BossEventDirector] Hiding boss health bar on player death");
        }
    }
}
