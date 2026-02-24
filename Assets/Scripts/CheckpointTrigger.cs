using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private bool destroyAfterActivation = false;

    private bool activated = false;

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
        if (activated)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        CheckpointManager.RegisterCheckpoint(other.transform.position);
        activated = true;

        if (destroyAfterActivation)
        {
            Destroy(gameObject);
        }
    }
}
