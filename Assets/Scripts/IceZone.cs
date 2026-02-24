using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class IceZone : MonoBehaviour
{
    [Header("Ice Effect")]
    [SerializeField] private float speedMultiplier = 1.1f;
    [SerializeField] [Range(0.02f, 1f)] private float controlMultiplier = 0.2f;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        PlayerMovement movement = other.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            return;
        }

        movement.ApplyIceMovement(speedMultiplier, controlMultiplier);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        PlayerMovement movement = other.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            return;
        }

        movement.ClearIceMovement();
    }
}
