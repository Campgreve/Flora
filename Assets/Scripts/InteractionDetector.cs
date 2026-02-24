using UnityEngine;

public class InteractionDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask interactableLayer = ~0;
    [SerializeField] private float detectionRadius = 1.6f;
    [SerializeField] private bool useDistancePolling = true;

    [Header("UI")]
    [SerializeField] private GameObject interactionIcon;

    private IInteractable interactableInRange = null;
    
    void Start()
    {
        if (interactionIcon != null)
        {
            interactionIcon.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (useDistancePolling)
        {
            RefreshInteractableByDistance();
        }

        if (GameInput.IsConfirmPressedThisFrame())
        {
            TryInteract();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (useDistancePolling)
        {
            return;
        }

        if (collision.TryGetComponent(out IInteractable interactable) && interactable.CanInteract())
        {
            interactableInRange = interactable;
            if (interactionIcon != null)
            {
                interactionIcon.SetActive(true);
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (useDistancePolling)
        {
            return;
        }

        if (collision.TryGetComponent(out IInteractable interactable) && interactable == interactableInRange)
        {
            interactableInRange = null;
            if (interactionIcon != null)
            {
                interactionIcon.SetActive(false);
            }
        }
    }
    
    void TryInteract()
    {
        if (interactableInRange != null && interactableInRange.CanInteract())
        {
            interactableInRange.Interact();
        }
    }

    private void RefreshInteractableByDistance()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, interactableLayer);

        IInteractable closest = null;
        float closestDistance = detectionRadius;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null || !interactable.CanInteract())
            {
                continue;
            }

            Vector2 closestPoint = hit.ClosestPoint(transform.position);
            float distance = Vector2.Distance(transform.position, closestPoint);
            if (distance < closestDistance)
            {
                closest = interactable;
                closestDistance = distance;
            }
        }

        interactableInRange = closest;
        if (interactionIcon != null)
        {
            interactionIcon.SetActive(interactableInRange != null);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
