using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MushroomEnemy : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float idleSpeed = 1f;
    public float chaseSpeed = 2.5f;
    
    [Header("Wander Behavior")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float minWanderInterval = 2f;
    [SerializeField] private float maxWanderInterval = 5f;
    [SerializeField] private float waypointReachedDistance = 0.5f;
    
    [Header("Melee Attack")]
    [SerializeField] private float attackRange = 1.5f; // Leggermente più grande per sicurezza
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimationDuration = 0.5f;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    
    [Header("Detection")]
    [SerializeField] private float alertDuration = 5f;
    [SerializeField] private float visionRange = 8f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Enemy Separation")]
    [SerializeField] private float separationDistance = 1.5f;
    [SerializeField] private float separationForce = 2f;
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("Health")]
    private float health;
    [SerializeField] private float maxHealth = 80f;
    
    [Header("Hit Feedback")]
    [SerializeField] private float knockbackForce = 3f;
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
    [SerializeField] private int minOrbCount = 2;
    [SerializeField] private int maxOrbCount = 5;
    [SerializeField] private int minExpPerOrb = 2;
    [SerializeField] private int maxExpPerOrb = 6;
    [SerializeField] private float orbSpreadRadius = 1.2f;
    [SerializeField, Range(0f, 1f)] private float healDropChance = 0.35f;
    [SerializeField, Range(0.01f, 0.2f)] private float minHealPercent = 0.04f;
    [SerializeField, Range(0.01f, 0.2f)] private float maxHealPercent = 0.07f;
    [SerializeField] private float minHealFlat = 3f;
    [SerializeField] private float maxHealFlat = 10f;

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

    private bool HasLineOfSight(Vector2 targetPos)
    {
        Vector2 origin = transform.position;
        Vector2 direction = targetPos - origin;
        float distance = direction.magnitude;
        
        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, obstacleLayer);
        
        Debug.DrawLine(origin, targetPos, hit.collider != null ? Color.red : Color.green, 0.1f);
        
        return hit.collider == null;
    }

    public void TakeDamage(float dmg, Vector2 hitDirection)
    {
        if (isDead) return;
        
        health -= dmg;
        Debug.Log("🍄 Mushroom Health: " + health);

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
        // 🎬 ANIMAZIONE HIT
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
        
        // Knockback
        if (rb != null && hitDirection != Vector2.zero)
        {
            isKnockedBack = true;
            isAttacking = false;
            
            rb.linearVelocity = Vector2.zero;
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
        
        // 💀 XP DROP
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
        
        // Disabilita collider
        Collider2D[] allColliders = GetComponents<Collider2D>();
        foreach (Collider2D c in allColliders)
        {
            c.enabled = false;
        }
        
        // 🎬 ANIMAZIONE MORTE
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        enabled = false;
        
        // ✅ FADE OUT dopo l'animazione
        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy()
    {
        // Aspetta che finisca l'animazione di morte
        yield return new WaitForSeconds(0.3f);
        
        // Fade out veloce
        float fadeTime = 0.2f;
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        Destroy(gameObject);
    }

    private void EnsurePersistentId()
    {
        if (!string.IsNullOrWhiteSpace(persistentEnemyId))
        {
            return;
        }

        Vector3 p = transform.position;
        persistentEnemyId =
            $"{SceneManager.GetActiveScene().name}_mush_{gameObject.name}_{Mathf.RoundToInt(p.x * 10f)}_{Mathf.RoundToInt(p.y * 10f)}";
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
        Debug.Log($"💚 Mushroom heal drop: +{healAmount:F1} HP");
    }

    private void FixedUpdate()
    {
        if (isKnockedBack || isDead || isAttacking) return;
        
        // Line of Sight check
        if (target != null && !HasLineOfSight(target.position))
        {
            target = null;
            
            if (alertCoroutine != null)
            {
                StopCoroutine(alertCoroutine);
            }
            alertCoroutine = StartCoroutine(AlertTimer());
            return;
        }
        
        // AI Logic
        if (target != null)
        {
            HandleChase();
        }
        else if (isAlerted)
        {
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
            }
        }
        else
        {
            HandleWander();
        }
    }

    void HandleChase()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, target.position);
        
        // Se è in range, ATTACCA
        if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(PerformAttack());
        }
        else if (distanceToPlayer > attackRange)
        {
            // Altrimenti insegui
            MoveTowards(target.position, chaseSpeed);
            
            if (animator != null)
            {
                animator.SetBool("IsWalking", true);
            }
        }
        else
        {
            // Sei in range ma stai aspettando il cooldown
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
            }
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
        
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            animator.SetBool("IsWalking", false);
        }
        
        // Aspetta metà animazione prima di fare danno
        yield return new WaitForSeconds(attackAnimationDuration * 0.5f);
        
        // 💥 USA OVERLAP CIRCLE - MOLTO PIÙ AFFIDABILE
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        
        bool hitPlayer = false;
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Vector2 hitDir = (hit.transform.position - transform.position).normalized;
                    playerHealth.TakeDamage(attackDamage, hitDir);
                    Debug.Log("🍄 Mushroom HIT player with OverlapCircle!");
                    hitPlayer = true;
                    break;
                }
            }
        }
        
        if (!hitPlayer)
        {
            Debug.Log("🍄 Mushroom attacked but player not in range");
        }
        
        // Aspetta il resto dell'animazione
        yield return new WaitForSeconds(attackAnimationDuration * 0.5f);
        
        isAttacking = false;
    }

    void HandleWander()
    {
        float distanceToWaypoint = Vector2.Distance(transform.position, wanderTarget);
        
        if (distanceToWaypoint > waypointReachedDistance)
        {
            MoveTowards(wanderTarget, idleSpeed);
            
            if (animator != null)
            {
                animator.SetBool("IsWalking", true);
            }
        }
        else
        {
            SetRandomWanderTarget();
            
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
            }
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        
        if (other.gameObject.tag == "Player")
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

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;
        
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
            
            if (target != null)
            {
                bool hasLOS = HasLineOfSight(target.position);
                Gizmos.color = hasLOS ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
        
        // Range attacco (ROSSO) - Ora più visibile
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Vision range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}
