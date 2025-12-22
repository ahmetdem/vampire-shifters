using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Only the owner can control their player
        if (!IsOwner) return;

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(moveX, moveY).normalized;
        rb.velocity = movement * speed;
    }

    // Add this method
    public void ModifySpeed(float amount)
    {
        // Example: If amount is 0.1, we increase speed by 10%
        // Or strictly additive: moveSpeed += amount; 

        // Let's go with Additive for simplicity (e.g. +1 speed)
        speed += amount;
    }
}
