using Unity.Netcode;
using UnityEngine;

public class SwarmController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float speed = 2f;

    private Vector2 wanderTarget;

    public override void OnNetworkSpawn()
    {
        // Only the server decides where the swarm goes
        if (IsServer)
        {
            PickNewTarget();
        }
    }

    private void FixedUpdate()
    {
        // Client does not calculate movement; they just receive the position via NetworkTransform
        if (!IsServer) return;

        // Move towards target
        Vector2 currentPos = transform.position;
        Vector2 direction = (wanderTarget - currentPos).normalized;

        // Simple movement logic
        transform.position += (Vector3)direction * speed * Time.fixedDeltaTime;

        // If we reached the target, pick a new one
        if (Vector2.Distance(currentPos, wanderTarget) < 0.5f)
        {
            PickNewTarget();
        }
    }

    private void PickNewTarget()
    {
        // Pick a random spot nearby
        wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
    }
}
