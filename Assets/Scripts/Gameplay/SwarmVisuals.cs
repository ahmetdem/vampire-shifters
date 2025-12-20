using UnityEngine;

public class SwarmVisuals : MonoBehaviour
{
    [SerializeField] private GameObject visualPrefab; // A simple zombie sprite
    [SerializeField] private int entityCount = 5;
    [SerializeField] private float swarmSpread = 2f;

    private GameObject[] _minions;

    private void Start()
    {
        _minions = new GameObject[entityCount];
        for (int i = 0; i < entityCount; i++)
        {
            // Spawn visual-only objects (NOT NetworkObjects)
            Vector2 randomOffset = Random.insideUnitCircle * swarmSpread;
            _minions[i] = Instantiate(visualPrefab, transform.position + (Vector3)randomOffset, Quaternion.identity);
            _minions[i].transform.SetParent(transform); // Child them to move with the controller
        }
    }

    // Optional: Add some local jitter animation in Update() to make them look alive
}
