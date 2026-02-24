using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float interactionRange = 2f;
    public LayerMask interactableLayer;
    
    [SerializeField] private Transform hand;
    [SerializeField] private float footstepSpeed = 0.4f;
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 1.1f;
    [Header("Dash Afterimage")]
    [SerializeField] private Color dashAfterimageColor = new Color(0.55f, 0.9f, 1f, 0.8f);
    [SerializeField] private float dashAfterimageLifetime = 0.12f;
    [SerializeField] private float dashAfterimageSpawnInterval = 0.03f;
    [Header("Spawn")]
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private Vector2 spawnSideDirection = Vector2.right;
    [SerializeField] private float spawnSidePadding = 0.35f;
    
    [Header("Combat Detection")]
    [SerializeField] private float combatDetectionRadius = 15f; // Raggio per rilevare nemici
    [SerializeField] private LayerMask enemyLayer; // Layer dei nemici
    [Header("Collision Slide")]
    [SerializeField] private float collisionStep = 0.02f;
    [SerializeField] private float collisionInset = 0.02f;
    
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private Vector2 lastDirection;
    private Vector2 mouseDirection;
    private IInteractable currentInteractable;
    private bool playingFootsteps = false;
    private bool isInCombat = false;
    private bool loadedFromSave = false;
    private Vector3 loadedSavePosition = Vector3.zero;
    private float dashTimeRemaining = 0f;
    private float nextDashAvailableTime = 0f;
    private Vector2 dashDirection = Vector2.down;
    private float dashAfterimageTimer = 0f;
    private SpriteRenderer spriteRenderer;
    private float moveSpeedMultiplier = 1f;
    private float moveControlMultiplier = 1f;
    private Vector2 smoothedVelocity = Vector2.zero;
    private Collider2D bodyCollider;
    private Vector2 colliderCenterOffset;
    private int movementCollisionMask;

    public bool IsDashing => dashTimeRemaining > 0f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        lastDirection = Vector2.down;
        mouseDirection = Vector2.down;

        if (bodyCollider != null)
        {
            colliderCenterOffset = (Vector2)bodyCollider.bounds.center - rb.position;
        }

        movementCollisionMask = Physics2D.GetLayerCollisionMask(gameObject.layer);
    }
    
    private void Start()
    {
        _ = GameProgress.Instance;
        EnsureStatsMenu();
        EnsureFireForestLighting();
        StartCoroutine(InitializeSpawnAndState());
        MusicManager.Instance.PlayChill();
    }

    private void EnsureStatsMenu()
    {
        StatsMenuUI existing = FindObjectOfType<StatsMenuUI>();
        if (existing != null)
        {
            return;
        }

        GameObject statsMenuRoot = new GameObject("StatsMenuRuntime");
        statsMenuRoot.AddComponent<StatsMenuUI>();
    }

    private void EnsureFireForestLighting()
    {
        if (GetComponent<FireForestLightingController>() == null)
        {
            gameObject.AddComponent<FireForestLightingController>();
        }
    }
    
    private IEnumerator InitializeSpawnAndState()
    {
        yield return new WaitForEndOfFrame();

        if (TryApplySaveData())
        {
            loadedFromSave = true;
            Debug.Log("💾 Player caricato dal save file.");
        }
        else if (CheckpointManager.TryGetRespawnPosition(out Vector3 checkpointPos))
        {
            transform.position = checkpointPos;
            Debug.Log("✅ Respawn dal checkpoint a: " + transform.position);
        }
        else
        {
            if (defaultSpawnPoint != null)
            {
                transform.position = GetDefaultSpawnPosition();
                Debug.Log("✅ Player posizionato a: " + transform.position);
            }
            else
            {
                Debug.Log("ℹ️ Nessun save/checkpoint/spawn point: mantengo la posizione del player in scena.");
            }
        }

        // 🎵 AVVIA LA MUSICA CHILL DOPO LO SPAWN
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayChill();
            Debug.Log("🎵 Musica chill avviata!");
        }

        if (loadedFromSave)
        {
            StartCoroutine(EnforceSavedPosition());
        }
    }

    private bool TryApplySaveData()
    {
        if (!SaveSystem.TryLoadForCurrentScene(out SaveData data))
        {
            return false;
        }

        transform.position = new Vector3(data.positionX, data.positionY, data.positionZ);
        loadedSavePosition = transform.position;
        Debug.Log($"💾 Respawn da save a: ({data.positionX:F2}, {data.positionY:F2}, {data.positionZ:F2})");

        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.LoadHealthState(data.currentHealth, data.maxHealth);
        }

        PlayerExperience playerExperience = GetComponent<PlayerExperience>();
        if (playerExperience != null)
        {
            playerExperience.LoadProgress(data.level, data.currentXP, data.xpToNextLevel);
        }

        GameProgress.Instance.LoadProgress(
            data.defeatedBossIds,
            data.collectedFlowerIds,
            data.completedArenaIds,
            data.defeatedEnemyIds);

        return true;
    }

    private IEnumerator EnforceSavedPosition()
    {
        // Alcuni sistemi della scena possono muovere il player nei primi frame (spawn/checkpoint/camera init).
        // Riapplichiamo la posizione salvata dopo un breve delay.
        yield return new WaitForSeconds(0.15f);

        transform.position = loadedSavePosition;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log($"💾 Posizione save riapplicata: ({loadedSavePosition.x:F2}, {loadedSavePosition.y:F2}, {loadedSavePosition.z:F2})");
    }

    private Vector3 GetDefaultSpawnPosition()
    {
        Vector3 fallbackPos = defaultSpawnPoint.position;
        if (defaultSpawnPoint == null)
        {
            return fallbackPos;
        }

        Collider2D spawnCollider = defaultSpawnPoint.GetComponent<Collider2D>();
        if (spawnCollider == null)
        {
            return fallbackPos;
        }

        Vector2 dir = spawnSideDirection.sqrMagnitude > 0.0001f
            ? spawnSideDirection.normalized
            : Vector2.right;

        Bounds b = spawnCollider.bounds;
        float halfExtentAlongDir = Mathf.Abs(dir.x) * b.extents.x + Mathf.Abs(dir.y) * b.extents.y;
        Vector2 center = b.center;
        Vector2 spawnPos2D = center + dir * (halfExtentAlongDir + spawnSidePadding);

        return new Vector3(spawnPos2D.x, spawnPos2D.y, fallbackPos.z);
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            MinimapSystem minimap = FindObjectOfType<MinimapSystem>();
            if (minimap != null)
                minimap.ToggleMinimap();
        }
        
        movement = GameInput.GetMoveInput();

        bool dashPressedThisFrame = GameInput.IsDashPressedThisFrame();
        if (dashPressedThisFrame)
        {
            TryStartDash();
        }
        
        if (!IsDashing && movement.sqrMagnitude > 0.1f)
        {
            StartFootsteps();
        }
        else
        {
            StopFootsteps();
        }

        if (movement.sqrMagnitude > 0.01f)
        {
            lastDirection = movement.normalized;
        }
        
        UpdateMouseDirection();
        
        if (hand != null)
        {
            RotateHand();
        }
        
        CheckForInteractables();
        
        // 🎵 CONTROLLA SE CI SONO NEMICI VICINI
        CheckCombatState();
        
        if (!dashPressedThisFrame && GameInput.IsConfirmPressedThisFrame())
        {
            TryInteract();
        }
    }
    
    private void CheckCombatState()
    {
        MusicManager musicManager = MusicManager.Instance;
        if (musicManager != null && musicManager.CurrentState == MusicManager.MusicState.Boss)
        {
            return;
        }

        Collider2D[] nearbyColliders = enemyLayer.value != 0
            ? Physics2D.OverlapCircleAll(transform.position, combatDetectionRadius, enemyLayer)
            : Physics2D.OverlapCircleAll(transform.position, combatDetectionRadius);

        int aliveEnemies = 0;
        foreach (Collider2D col in nearbyColliders)
        {
            if (!col.gameObject.activeInHierarchy)
            {
                continue;
            }

            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null && enemy.enabled)
            {
                aliveEnemies++;
                continue;
            }

            RaccoonBoss boss = col.GetComponent<RaccoonBoss>();
            if (boss != null && boss.enabled)
            {
                aliveEnemies++;
            }
        }

        bool shouldBeInCombat = aliveEnemies > 0 || ArenaController.AnyArenaActive;

        if (shouldBeInCombat && !isInCombat)
        {
            isInCombat = true;
            if (musicManager != null)
            {
                musicManager.PlayCombat();
                Debug.Log("🎵⚔️ COMBAT MODE! Nemici: " + aliveEnemies);
            }
        }
        else if (!shouldBeInCombat && isInCombat)
        {
            isInCombat = false;
            if (musicManager != null)
            {
                musicManager.PlayChill();
                Debug.Log("🎵😌 CHILL MODE!");
            }
        }
    }
    
    private void FixedUpdate()
    {
        Vector2 targetVelocity = movement * speed * moveSpeedMultiplier;
        float control = Mathf.Clamp01(moveControlMultiplier);
        float blend = control >= 0.999f ? 1f : Mathf.Clamp01(control * 0.35f);
        smoothedVelocity = Vector2.Lerp(smoothedVelocity, targetVelocity, blend);
        Vector2 velocity = smoothedVelocity;

        if (IsDashing)
        {
            dashTimeRemaining -= Time.fixedDeltaTime;
            velocity = dashDirection * dashSpeed;
            dashAfterimageTimer -= Time.fixedDeltaTime;
            if (dashAfterimageTimer <= 0f)
            {
                SpawnDashAfterimage();
                dashAfterimageTimer = Mathf.Max(0.01f, dashAfterimageSpawnInterval);
            }
            if (dashTimeRemaining <= 0f)
            {
                dashTimeRemaining = 0f;
            }
        }

        Vector2 delta = velocity * Time.fixedDeltaTime;
        MoveWithSlide(delta);
        
        animator.SetFloat("MoveX", mouseDirection.x);
        animator.SetFloat("MoveY", mouseDirection.y);
        animator.SetFloat("Speed", IsDashing ? 1f : movement.sqrMagnitude);
    }

    private void TryStartDash()
    {
        if (IsDashing || Time.time < nextDashAvailableTime)
        {
            return;
        }

        Vector2 inputDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : lastDirection;
        if (inputDirection.sqrMagnitude < 0.0001f)
        {
            inputDirection = mouseDirection;
        }

        dashDirection = inputDirection.sqrMagnitude > 0.0001f ? inputDirection.normalized : Vector2.down;
        dashTimeRemaining = dashDuration;
        dashAfterimageTimer = 0f;
        nextDashAvailableTime = Time.time + dashCooldown;
        StopFootsteps();
        SpawnDashAfterimage();
    }

    private void SpawnDashAfterimage()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        GameObject ghost = new GameObject("DashAfterimage");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.lossyScale;

        SpriteRenderer ghostSprite = ghost.AddComponent<SpriteRenderer>();
        ghostSprite.sprite = spriteRenderer.sprite;
        ghostSprite.flipX = spriteRenderer.flipX;
        ghostSprite.flipY = spriteRenderer.flipY;
        ghostSprite.sortingLayerID = spriteRenderer.sortingLayerID;
        ghostSprite.sortingOrder = spriteRenderer.sortingOrder - 1;
        ghostSprite.color = dashAfterimageColor;

        StartCoroutine(FadeAndDestroyAfterimage(ghost, ghostSprite));
    }

    private IEnumerator FadeAndDestroyAfterimage(GameObject ghostObject, SpriteRenderer ghostSprite)
    {
        if (ghostObject == null || ghostSprite == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, dashAfterimageLifetime);
        Color startColor = ghostSprite.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            ghostSprite.color = c;
            yield return null;
        }

        Destroy(ghostObject);
    }
    
    void UpdateMouseDirection()
    {
        GameInput.TryGetAimDirection(transform.position, out Vector2 direction);
        
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);
        
        if (absX > absY)
        {
            mouseDirection = direction.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            mouseDirection = direction.y > 0 ? Vector2.up : Vector2.down;
        }
    }
    
    void RotateHand()
    {
        GameInput.TryGetAimDirection(hand.position, out Vector2 direction);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        hand.rotation = Quaternion.Euler(0f, 0f, angle);
    }
    
    void CheckForInteractables()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRange, interactableLayer);
        
        IInteractable closest = null;
        float closestDistance = interactionRange;
        
        foreach (Collider2D hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            
            if (interactable != null && interactable.CanInteract())
            {
                Vector2 closestPoint = hit.ClosestPoint(transform.position);
                float distance = Vector2.Distance(transform.position, closestPoint);
                
                if (distance < closestDistance)
                {
                    closest = interactable;
                    closestDistance = distance;
                }
            }
        }
        
        currentInteractable = closest;
    }
    
    void TryInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }
    
    void StartFootsteps()
    {
        if (playingFootsteps) return;

        playingFootsteps = true;
        InvokeRepeating(nameof(PlayFootstep), 0f, footstepSpeed);
    }

    void StopFootsteps()
    {
        if (!playingFootsteps) return;

        playingFootsteps = false;
        CancelInvoke(nameof(PlayFootstep));
    }

    void PlayFootstep()
    {
        SoundEffectManager.Play("Footstep", true);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Visualizza il raggio di combat detection
        Gizmos.color = isInCombat ? Color.red : Color.blue;
        Gizmos.DrawWireSphere(transform.position, combatDetectionRadius);
    }

    public void ApplyIceMovement(float speedMultiplier, float controlMultiplier)
    {
        moveSpeedMultiplier = Mathf.Max(0f, speedMultiplier);
        moveControlMultiplier = Mathf.Clamp01(controlMultiplier);
    }

    public void ClearIceMovement()
    {
        moveSpeedMultiplier = 1f;
        moveControlMultiplier = 1f;
    }

    private void MoveWithSlide(Vector2 delta)
    {
        if (rb == null || bodyCollider == null)
        {
            rb.MovePosition(rb.position + delta);
            return;
        }

        Vector2 nextPosition = rb.position;

        if (Mathf.Abs(delta.x) > 0.0001f)
        {
            Vector2 horizontalDir = new Vector2(Mathf.Sign(delta.x), 0f);
            nextPosition = MoveAlongAxis(nextPosition, horizontalDir, Mathf.Abs(delta.x));
        }

        if (Mathf.Abs(delta.y) > 0.0001f)
        {
            Vector2 verticalDir = new Vector2(0f, Mathf.Sign(delta.y));
            nextPosition = MoveAlongAxis(nextPosition, verticalDir, Mathf.Abs(delta.y));
        }

        rb.MovePosition(nextPosition);
    }

    private Vector2 MoveAlongAxis(Vector2 startPosition, Vector2 axisDir, float distance)
    {
        float remaining = distance;
        float step = Mathf.Max(0.005f, collisionStep);
        Vector2 current = startPosition;
        int safety = 0;

        while (remaining > 0f && safety < 128)
        {
            safety++;
            float currentStep = Mathf.Min(step, remaining);
            Vector2 candidate = current + axisDir * currentStep;

            if (CanOccupy(candidate))
            {
                current = candidate;
                remaining -= currentStep;
            }
            else
            {
                break;
            }
        }

        return current;
    }

    private bool CanOccupy(Vector2 rbPosition)
    {
        if (bodyCollider == null)
        {
            return true;
        }

        Bounds b = bodyCollider.bounds;
        Vector2 boxSize = new Vector2(
            Mathf.Max(0.01f, b.size.x - collisionInset * 2f),
            Mathf.Max(0.01f, b.size.y - collisionInset * 2f)
        );
        Vector2 boxCenter = rbPosition + colliderCenterOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, movementCollisionMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit == bodyCollider || hit.isTrigger)
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
