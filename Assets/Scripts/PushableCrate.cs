using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PushableCrate : MonoBehaviour, IInteractable
{
    [Header("Movement")]
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float moveDuration = 0.12f;
    [SerializeField] private LayerMask blockingMask;
    [SerializeField] private Vector2 obstacleCheckSize = new Vector2(0.8f, 0.8f);

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float maxPushDistance = 2f;
    [SerializeField] private bool allowDirectConfirmInput = true;
    [SerializeField] private bool enableDebugLogs = true;

    private Rigidbody2D rb;
    private Collider2D crateCollider;
    private Transform playerTransform;
    private PlayerMovement playerMovement;
    private Collider2D playerCollider;
    private bool isMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        crateCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        CachePlayerTransform();
    }

    private void Update()
    {
        // Fallback mode: allows pushing even if PlayerMovement's interactableLayer is not configured.
        if (!allowDirectConfirmInput || isMoving)
        {
            return;
        }

        // Space is allowed again for interact, but never in the same frame as dash input.
        bool directInteractPressed = GameInput.IsConfirmPressedThisFrame() &&
                                     !GameInput.IsDashPressedThisFrame();
        if (!directInteractPressed)
        {
            return;
        }

        if (!CachePlayerTransform())
        {
            return;
        }

        if (Vector2.Distance(playerTransform.position, transform.position) > maxPushDistance)
        {
            return;
        }

        TryPush();
    }

    private void Reset()
    {
        Rigidbody2D crateRb = GetComponent<Rigidbody2D>();
        if (crateRb != null)
        {
            crateRb.bodyType = RigidbodyType2D.Kinematic;
            crateRb.freezeRotation = true;
            crateRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    public bool CanInteract()
    {
        if (isMoving)
        {
            return false;
        }

        if (!CachePlayerTransform())
        {
            return false;
        }

        return Vector2.Distance(playerTransform.position, transform.position) <= maxPushDistance;
    }

    public void Interact()
    {
        TryPush();
    }

    private bool CachePlayerTransform()
    {
        if (playerTransform != null)
        {
            if (playerMovement == null)
            {
                playerMovement = playerTransform.GetComponent<PlayerMovement>();
            }
            if (playerCollider == null)
            {
                playerCollider = playerTransform.GetComponent<Collider2D>();
            }
            return true;
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            return false;
        }

        playerTransform = player.transform;
        playerMovement = player.GetComponent<PlayerMovement>();
        playerCollider = player.GetComponent<Collider2D>();
        return true;
    }

    private static Vector2 ToCardinalDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector2.zero;
        }

        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        // In diagonale preferiamo l'asse verticale per evitare spinte laterali inattese.
        if (absX > absY)
        {
            return direction.x >= 0f ? Vector2.right : Vector2.left;
        }

        return direction.y >= 0f ? Vector2.up : Vector2.down;
    }

    private Vector2 GetCardinalDirectionFromPlayer()
    {
        Vector2 sideDirection = GetDirectionFromClosestSide();
        if (sideDirection != Vector2.zero)
        {
            return sideDirection;
        }

        Vector2 diff = (Vector2)transform.position - (Vector2)playerTransform.position;
        return ToCardinalDirection(diff);
    }

    private Vector2 GetDirectionFromClosestSide()
    {
        if (crateCollider == null || playerCollider == null)
        {
            return Vector2.zero;
        }

        Bounds crate = crateCollider.bounds;
        Bounds player = playerCollider.bounds;

        float leftGap = Mathf.Abs(player.max.x - crate.min.x);
        float rightGap = Mathf.Abs(player.min.x - crate.max.x);
        float bottomGap = Mathf.Abs(player.max.y - crate.min.y);
        float topGap = Mathf.Abs(player.min.y - crate.max.y);

        float minGap = Mathf.Min(leftGap, rightGap, bottomGap, topGap);

        // In caso di valori quasi uguali (angolo), preferiamo verticale per coerenza col gameplay.
        const float epsilon = 0.0001f;
        bool bottomIsBest = Mathf.Abs(bottomGap - minGap) <= epsilon;
        bool topIsBest = Mathf.Abs(topGap - minGap) <= epsilon;
        bool leftIsBest = Mathf.Abs(leftGap - minGap) <= epsilon;
        bool rightIsBest = Mathf.Abs(rightGap - minGap) <= epsilon;

        if (bottomIsBest)
        {
            return Vector2.up;
        }

        if (topIsBest)
        {
            return Vector2.down;
        }

        if (leftIsBest)
        {
            return Vector2.right;
        }

        if (rightIsBest)
        {
            return Vector2.left;
        }

        return Vector2.zero;
    }

    private bool IsBlocked(Vector2 targetPos)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(targetPos, obstacleCheckSize, 0f, blockingMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i];
            if (col == null || col.isTrigger)
            {
                continue;
            }

            if (col.attachedRigidbody != null && col.attachedRigidbody.gameObject == gameObject)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void TryPush()
    {
        if (isMoving || !CachePlayerTransform())
        {
            return;
        }

        if (IsAnyPlayerDashing())
        {
            Log("Push canceled: player is dashing.");
            return;
        }

        Vector2 pushDirection = GetCardinalDirectionFromPlayer();
        if (pushDirection == Vector2.zero)
        {
            Log("Push canceled: invalid direction.");
            return;
        }

        Vector2 startPos = rb.position;
        Vector2 targetPos = startPos + pushDirection * tileSize;

        if (IsBlocked(targetPos))
        {
            Log($"Push blocked. Target tile occupied at {targetPos}.");
            return;
        }

        Log($"Push success. Moving to {targetPos}.");
        StartCoroutine(MoveToTarget(targetPos));
    }

    private void Log(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[PushableCrate] {name}: {message}");
    }

    private IEnumerator MoveToTarget(Vector2 targetPos)
    {
        isMoving = true;

        Vector2 startPos = rb.position;
        float duration = Mathf.Max(0.01f, moveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector2 next = Vector2.Lerp(startPos, targetPos, t);
            rb.MovePosition(next);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(targetPos);
        isMoving = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, obstacleCheckSize);
    }

    private bool IsAnyPlayerDashing()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].IsDashing)
            {
                return true;
            }
        }

        return false;
    }
}
