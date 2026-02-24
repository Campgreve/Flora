using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float idleSpeed = 1.5f;
    public float chaseSpeed = 3f;
    
    [Header("Wander Behavior")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float minWanderInterval = 2f;
    [SerializeField] private float maxWanderInterval = 5f;
    [SerializeField] private float waypointReachedDistance = 0.5f;
    
    [Header("Dash Attack")]
    [SerializeField] private float dashChargeTime = 0.8f;
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDistance = 6f; // Quanto viaggia durante il dash
    [SerializeField] private float dashCooldown = 2f;
    [SerializeField] private float circlingRadius = 3f; // Raggio per girare attorno al player
    [SerializeField] private float circlingSpeed = 3f;
    [SerializeField] private float minDashDistance = 2f; // Distanza minima per dashare
    [SerializeField] private float maxDashDistance = 7f; // Distanza massima per dashare
    
    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackSpeed = 1f;
    private float canAttack;
    
    [Header("Detection")]
    [SerializeField] private float alertDuration = 5f;
    [SerializeField] private float visionRange = 8f;
    [SerializeField] private LayerMask obstacleLayer; // Layer per muri/ostacoli
    
    [Header("Enemy Separation")]
    [SerializeField] private float separationDistance = 1.5f;
    [SerializeField] private float separationForce = 2f;
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("Health")]
    private float health;
    [SerializeField] private float maxHealth = 100f;
    
    [Header("Hit Feedback")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private int hitFlashCount = 3;
    [SerializeField] private float knockbackRecoveryTime = 0.3f;
    
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Damage Popup")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private Vector3 popupOffset = new Vector3(0, 1f, 0);

    [Header("Drops")]
    [SerializeField] private GameObject expOrbPrefab;
    [SerializeField] private int minOrbCount = 3;
    [SerializeField] private int maxOrbCount = 8;
    [SerializeField] private int minExpPerOrb = 3;
    [SerializeField] private int maxExpPerOrb = 8;
    [SerializeField] private float orbSpreadRadius = 1.5f;
    [SerializeField, Range(0f, 1f)] private float healDropChance = 0.5f;
    [SerializeField, Range(0.01f, 0.2f)] private float minHealPercent = 0.06f;
    [SerializeField, Range(0.01f, 0.2f)] private float maxHealPercent = 0.1f;
    [SerializeField] private float minHealFlat = 5f;
    [SerializeField] private float maxHealFlat = 16f;

    [Header("Persistence")]
    [SerializeField] private bool persistDefeat = true;
    [SerializeField] private string persistentEnemyId = "";
    
    private Transform target;
    private Vector2 lastPosition;
    private Vector2 wanderTarget;
    private Vector3 spawnPoint;
    private Rigidbody2D rb;
    private bool isHit = false;
    private bool isAlerted = false;
    private bool isDead = false;
    private Color originalColor;
    private Coroutine wanderCoroutine;
    private Coroutine alertCoroutine;
    
    // Dash Attack State Machine
    private enum DashState { Idle, Charging, Dashing, Circling }
    private DashState dashState = DashState.Idle;
    private float dashTimer = 0f;
    private Vector2 dashDirection;
    private Vector2 dashStartPosition;
    private float dashTravelDistance = 0f;
    private float circlingAngle = 0f;
    private bool canDash = true;
    private bool isKnockedBack = false;

    public void SetPersistenceEnabled(bool enabled)
    {
        persistDefeat = enabled;
    }

    private void Start()
    {
        EnsurePersistentId();
        if (persistDefeat && GameProgress.Instance.HasDefeatedEnemy(persistentEnemyId))
        {
            Destroy(gameObject);
            return;
        }

        health = maxHealth;
        lastPosition = transform.position;
        spawnPoint = transform.position;
        rb = GetComponent<Rigidbody2D>();
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        originalColor = spriteRenderer.color;
        
        SetRandomWanderTarget();
        wanderCoroutine = StartCoroutine(WanderRoutine());
    }

    void SetRandomWanderTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle * wanderRadius;
        wanderTarget = (Vector2)spawnPoint + randomDirection;
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minWanderInterval, maxWanderInterval));
            
            if (target == null && !isAlerted)
            {
                SetRandomWanderTarget();
            }
        }
    }

    IEnumerator AlertTimer()
    {
        isAlerted = true;
        yield return new WaitForSeconds(alertDuration);
        
        if (target == null)
        {
            isAlerted = false;
        }
        alertCoroutine = null;
    }

    // Line of Sight Check
    private bool HasLineOfSight(Vector2 targetPos)
    {
        Vector2 origin = transform.position;
        Vector2 direction = targetPos - origin;
        float distance = direction.magnitude;
        
        // Raycast per vedere se ci sono ostacoli
        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, obstacleLayer);
        
        // Debug line per visualizzare il raycast (commentalo in produzione)
        Debug.DrawLine(origin, targetPos, hit.collider != null ? Color.red : Color.green, 0.1f);
        
        return hit.collider == null; // true se non ci sono ostacoli
    }

    public void TakeDamage(float dmg, Vector2 hitDirection)
    {
        health -= dmg;
        Debug.Log("Enemy Health: " + health);
        
        SoundEffectManager.Play("EnemyHitted");

        ShowDamagePopup(dmg);
        
        if (health <= 0)
        {
            Die();
        }
        else
        {
            if (!isAlerted && target == null)
            {
                AlertFromHit(hitDirection);
            }
            
            StartCoroutine(HitFeedback(hitDirection));
        }
    }

    void ShowDamagePopup(float damage)
    {
        if (damagePopupPrefab != null)
        {
            Vector3 popupPosition = transform.position + popupOffset;
            GameObject popup = Instantiate(damagePopupPrefab, popupPosition, Quaternion.identity);
            DamagePopup popupScript = popup.GetComponent<DamagePopup>();
            
            if (popupScript != null)
            {
                popupScript.Setup(damage);
            }
        }
    }

    void AlertFromHit(Vector2 hitDirection)
    {
        Vector2 searchDirection = -hitDirection;
        
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, wanderRadius * 2);
        foreach (Collider2D col in nearbyColliders)
        {
            if (col.CompareTag("Player"))
            {
                target = col.transform;
                break;
            }
        }
        
        if (alertCoroutine != null)
        {
            StopCoroutine(alertCoroutine);
        }
        alertCoroutine = StartCoroutine(AlertTimer());
    }

    IEnumerator HitFeedback(Vector2 hitDirection)
    {
        // Knockback migliorato
        if (rb != null && hitDirection != Vector2.zero)
        {
            isKnockedBack = true;
            dashState = DashState.Idle; // Interrompi il dash se stavi dashando
            
            rb.linearVelocity = Vector2.zero; // Reset velocità
            rb.AddForce(hitDirection * knockbackForce, ForceMode2D.Impulse);
            
            yield return new WaitForSeconds(knockbackRecoveryTime);
            isKnockedBack = false;
        }
        
        // Flash effect
        for (int i = 0; i < hitFlashCount; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(hitFlashDuration);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(hitFlashDuration);
        }
    }

    private void Die()
    {
        isDead = true;
        idleSpeed = 0f;
        chaseSpeed = 0f;

        if (persistDefeat)
        {
            GameProgress.Instance.MarkEnemyDefeated(persistentEnemyId);
        }

        ArenaController arena = FindObjectOfType<ArenaController>();
        if (arena != null)
        {
            arena.OnEnemyDied(gameObject);
        }
        
        if (expOrbPrefab != null)
        {
            int orbCount = Random.Range(minOrbCount, maxOrbCount + 1);
            
            for (int i = 0; i < orbCount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * orbSpreadRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
                
                GameObject orb = Instantiate(expOrbPrefab, spawnPos, Quaternion.identity);
                ExperienceOrb orbScript = orb.GetComponent<ExperienceOrb>();
                
                if (orbScript != null)
                {
                    int randomXP = Random.Range(minExpPerOrb, maxExpPerOrb + 1);
                    orbScript.xpAmount = randomXP;
                }
            }
        }

        TryDropHealth();
        
        if (wanderCoroutine != null) StopCoroutine(wanderCoroutine);
        if (alertCoroutine != null) StopCoroutine(alertCoroutine);
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        Collider2D[] allColliders = GetComponents<Collider2D>();
        foreach (Collider2D c in allColliders)
        {
            c.enabled = false;
        }
        
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        enabled = false;
        Destroy(gameObject, 1f);
    }

    private void EnsurePersistentId()
    {
        if (!string.IsNullOrWhiteSpace(persistentEnemyId))
        {
            return;
        }

        Vector3 p = transform.position;
        persistentEnemyId =
            $"{SceneManager.GetActiveScene().name}_enemy_{gameObject.name}_{Mathf.RoundToInt(p.x * 10f)}_{Mathf.RoundToInt(p.y * 10f)}";
    }

    private void TryDropHealth()
    {
        if (Random.value > healDropChance)
        {
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            return;
        }

        PlayerHealth playerHealth = playerObj.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            return;
        }

        float maxHp = playerHealth.GetMaxHealth();
        float percentHeal = maxHp * Random.Range(minHealPercent, maxHealPercent);
        float healAmount = Mathf.Clamp(percentHeal, minHealFlat, maxHealFlat);

        playerHealth.UpdateHealth(healAmount);
        Debug.Log($"💚 Enemy heal drop: +{healAmount:F1} HP");
    }

    private void FixedUpdate()
    {
        if (isKnockedBack || isDead) return;
        
        // Controlla se il target è ancora visibile
        if (target != null && !HasLineOfSight(target.position))
        {
            // Perdi il target se non lo vedi più
            target = null;
            dashState = DashState.Idle;
            
            if (alertCoroutine != null)
            {
                StopCoroutine(alertCoroutine);
            }
            alertCoroutine = StartCoroutine(AlertTimer());
            return;
        }
        
        // Dash Attack State Machine
        if (target != null)
        {
            HandleDashAttack();
        }
        else if (isAlerted)
        {
            // Modalità allerta ma senza target
            dashState = DashState.Idle;
        }
        else
        {
            // Modalità wander normale
            HandleWander();
        }
    }

    void HandleDashAttack()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, target.position);
        
        switch (dashState)
        {
            case DashState.Idle:
                // Muoviti verso il player finché non sei in range per dashare
                if (distanceToPlayer > minDashDistance && distanceToPlayer <= maxDashDistance && canDash)
                {
                    dashState = DashState.Charging;
                    dashTimer = 0f;
                    dashDirection = (target.position - transform.position).normalized;
                    Debug.Log("Starting charge!");
                    
                    if (animator != null)
                    {
                        animator.SetBool("IsCharging", true);
                    }
                }
                else if (distanceToPlayer > maxDashDistance)
                {
                    // Avvicinati al player
                    MoveTowards(target.position, chaseSpeed);
                }
                else if (distanceToPlayer <= minDashDistance)
                {
                    // Entra in modalità circling (gira attorno al player)
                    dashState = DashState.Circling;
                    dashTimer = 0f;
                    circlingAngle = Mathf.Atan2(
                        transform.position.y - target.position.y,
                        transform.position.x - target.position.x
                    ) * Mathf.Rad2Deg;
                    Debug.Log("Too close, starting circling");
                }
                break;
                
            case DashState.Charging:
                // Fase di carica (il pipistrello si prepara)
                dashTimer += Time.deltaTime;
                
                // Aggiorna la direzione durante la carica
                dashDirection = (target.position - transform.position).normalized;
                
                if (dashTimer >= dashChargeTime)
                {
                    dashState = DashState.Dashing;
                    dashTimer = 0f;
                    dashStartPosition = transform.position;
                    dashTravelDistance = 0f;
                    Debug.Log("Dashing!");
                    
                    if (animator != null)
                    {
                        animator.SetBool("IsCharging", false);
                        animator.SetTrigger("Dash");
                    }
                }
                break;
                
            case DashState.Dashing:
                // Dash veloce ATTRAVERSO il player
                Vector2 dashMovement = dashDirection * dashSpeed * Time.deltaTime;
                Vector2 newDashPosition = (Vector2)transform.position + dashMovement;
                
                // Usa MovePosition per il dash
                if (rb != null)
                {
                    rb.MovePosition(newDashPosition);
                }
                else
                {
                    transform.position = newDashPosition;
                }
                
                FlipSprite(newDashPosition);
                
                dashTravelDistance += dashMovement.magnitude;
                
                // Continua il dash finché non hai percorso la distanza totale
                if (dashTravelDistance >= dashDistance)
                {
                    // Dash completato, passa alla modalità circling
                    dashState = DashState.Circling;
                    dashTimer = 0f;
                    Debug.Log("Dash complete, starting circling");
                    
                    // Calcola l'angolo iniziale per il circling
                    if (target != null)
                    {
                        circlingAngle = Mathf.Atan2(
                            transform.position.y - target.position.y,
                            transform.position.x - target.position.x
                        ) * Mathf.Rad2Deg;
                    }
                }
                break;
                
            case DashState.Circling:
                // Gira attorno al player in cerchio
                if (target != null)
                {
                    dashTimer += Time.deltaTime;
                    
                    // Incrementa l'angolo per girare attorno
                    circlingAngle += circlingSpeed * Time.deltaTime * 50f; // 50 è la velocità angolare
                    
                    // Calcola la posizione target sul cerchio
                    float radians = circlingAngle * Mathf.Deg2Rad;
                    Vector2 circleTarget = (Vector2)target.position + new Vector2(
                        Mathf.Cos(radians) * circlingRadius,
                        Mathf.Sin(radians) * circlingRadius
                    );
                    
                    // Muoviti verso quella posizione
                    MoveTowards(circleTarget, circlingSpeed);
                    
                    // Dopo un po' di tempo in circling, torna a Idle per poter dashare di nuovo
                    if (dashTimer >= 1.5f) // Tempo di circling prima di ricaricare
                    {
                        Debug.Log("Circling complete, back to Idle. CanDash will be: " + canDash);
                        dashState = DashState.Idle;
                        dashTimer = 0f;
                        StartCoroutine(DashCooldown());
                    }
                }
                else
                {
                    dashState = DashState.Idle;
                    dashTimer = 0f;
                }
                break;
        }
    }

    IEnumerator DashCooldown()
    {
        canDash = false;
        Debug.Log("Dash Cooldown started");
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
        Debug.Log("Dash ready!");
    }

    void HandleWander()
    {
        dashState = DashState.Idle;
        
        float distanceToWaypoint = Vector2.Distance(transform.position, wanderTarget);
        
        if (distanceToWaypoint > waypointReachedDistance)
        {
            MoveTowards(wanderTarget, idleSpeed);
        }
        else
        {
            SetRandomWanderTarget();
        }
    }

    void MoveTowards(Vector2 targetPosition, float speed)
    {
        Vector2 directionToTarget = (targetPosition - (Vector2)transform.position).normalized;
        Vector2 separationVector = GetSeparationVector();
        Vector2 finalDirection = (directionToTarget + separationVector).normalized;
        
        float step = speed * Time.deltaTime;
        Vector2 newPosition = (Vector2)transform.position + finalDirection * step;
        FlipSprite(newPosition);
        
        // Usa MovePosition per movimento corretto con Rigidbody2D
        if (rb != null)
        {
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }
        
        lastPosition = newPosition;
    }

    Vector2 GetSeparationVector()
    {
        Vector2 separation = Vector2.zero;
        int nearbyEnemies = 0;
        
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, separationDistance);
        
        foreach (Collider2D col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue;
            
            if (col.CompareTag("Enemy") || (enemyLayer.value != 0 && ((1 << col.gameObject.layer) & enemyLayer.value) != 0))
            {
                Vector2 awayFromEnemy = (Vector2)transform.position - (Vector2)col.transform.position;
                float distance = awayFromEnemy.magnitude;
                
                if (distance > 0 && distance < separationDistance)
                {
                    float strength = 1f - (distance / separationDistance);
                    separation += awayFromEnemy.normalized * strength * separationForce;
                    nearbyEnemies++;
                }
            }
        }
        
        if (nearbyEnemies > 0)
        {
            separation /= nearbyEnemies;
        }
        
        return separation;
    }

    void FlipSprite(Vector2 newPosition)
    {
        if (spriteRenderer == null) return;
        
        float direction = newPosition.x - lastPosition.x;
        
        if (direction < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (direction > 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (isDead) return;
        
        if (other.gameObject.tag == "Player")
        {
            if (attackSpeed <= canAttack)
            {
                PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Vector2 hitDir = (other.transform.position - transform.position).normalized;
                    playerHealth.TakeDamage(attackDamage, hitDir);
                }
                canAttack = 0f;
            }
            else
            {
                canAttack += Time.deltaTime;
            }
        }
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (isDead) return;
        
        if (other.gameObject.tag == "Player")
        {
            if (attackSpeed <= canAttack)
            {
                PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Vector2 hitDir = (other.transform.position - transform.position).normalized;
                    playerHealth.TakeDamage(attackDamage, hitDir);
                }
                canAttack = 0f;
            }
            else
            {
                canAttack += Time.deltaTime;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        
        if (other.gameObject.tag == "Player")
        {
            // Verifica Line of Sight prima di acquisire il target
            if (HasLineOfSight(other.transform.position))
            {
                target = other.transform;
                
                if (alertCoroutine != null)
                {
                    StopCoroutine(alertCoroutine);
                    alertCoroutine = null;
                }
                isAlerted = false;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;
        
        // Controlla continuamente la line of sight mentre il player è nel trigger
        if (other.gameObject.tag == "Player" && target == null)
        {
            if (HasLineOfSight(other.transform.position))
            {
                target = other.transform;
                
                if (alertCoroutine != null)
                {
                    StopCoroutine(alertCoroutine);
                    alertCoroutine = null;
                }
                isAlerted = false;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isDead) return;
        
        if (other.gameObject.tag == "Player")
        {
            target = null;
            dashState = DashState.Idle;
            
            if (alertCoroutine != null)
            {
                StopCoroutine(alertCoroutine);
            }
            alertCoroutine = StartCoroutine(AlertTimer());
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Vector3 center = Application.isPlaying ? spawnPoint : transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(wanderTarget, 0.3f);
            Gizmos.DrawLine(transform.position, wanderTarget);
            
            // Visualizza il dash range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, minDashDistance);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maxDashDistance);
            
            // Visualizza la line of sight
            if (target != null)
            {
                bool hasLOS = HasLineOfSight(target.position);
                Gizmos.color = hasLOS ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, separationDistance);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbSpreadRadius);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}
