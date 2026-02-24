using System.Collections;
using UnityEngine;

public class RaccoonBoss : MonoBehaviour, IDamageable, IBossHealthSource
{
    [Header("Boss Identity")]
    [SerializeField] private string bossName = "Trash Panda King";
    
    [Header("Health & Phases")]
    [SerializeField] private float maxHealth = 350f; // ← RIDOTTO da 500 per renderlo più fattibile
    private float currentHealth;
    [SerializeField] private float phase2Threshold = 0.6f; // 60%
    [SerializeField] private float phase3Threshold = 0.3f; // 30%
    
    [Header("Movement")]
    [SerializeField] private float IdleWalkSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.5f; // ← RIDOTTO da 4 per dare più tempo al player
    
    [Header("Jump Slam Attack")]
    [SerializeField] private float JumpHeight = 6f;
    [SerializeField] private float JumpDuration = 1.2f;
    [SerializeField] private float JumpPreparationTime = 0.6f; // ← AUMENTATO da 0.5 per dare più tempo di reazione
    [SerializeField] private float JumpLandRadius = 2.5f;
    [SerializeField] private float JumpDamage = 18f; // ← RIDOTTO da 25
    [SerializeField] private float JumpCooldown = 4.5f; // ← AUMENTATO da 4 per dare più respiro
    [SerializeField] private GameObject landingIndicatorPrefab;
    [SerializeField] private GameObject JumpImpactEffect;
    
    [Header("Charge Attack")]
    [SerializeField] private float ChargeSpeed = 10f; // ← RIDOTTO da 12
    [SerializeField] private float ChargePreparationTime = 1.2f; // ← AUMENTATO da 1 per dare più tempo
    [SerializeField] private float ChargeDuration = 2.5f;
    [SerializeField] private float ChargeDamage = 15f; // ← RIDOTTO da 20
    [SerializeField] private float ChargeCooldown = 5.5f; // ← AUMENTATO da 5
    [SerializeField] private float wallStunDuration = 2f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float ChargeHitKnockback = 8f;
    
    [Header("Contact Damage")]
    [SerializeField] private float contactDamage = 8f; // ← RIDOTTO da 10
    [SerializeField] private float contactDamageCooldown = 1f;
    private float lastContactDamageTime = 0f;
    
    [Header("Arena")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 15f;
    [SerializeField] private float preferredDistance = 6f;
    
    [Header("Hit Feedback")]
    [SerializeField] private float knockbackResistance = 0.3f;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private int hitFlashCount = 2;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private GameObject hitParticles;
    
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject phase2Effect;
    [SerializeField] private GameObject phase3Effect;
    
    [Header("Audio")]
    [SerializeField] private string JumpSound = "BossJump";
    [SerializeField] private string ChargeSound = "BossCharge";
    [SerializeField] private string landSound = "BossLand";
    [SerializeField] private string hurtSound = "BossHurt";
    [SerializeField] private string deathSound = "BossDeath";
    [SerializeField] private string phaseChangeSound = "BossEnrage";

    [Header("Rewards")]
    [SerializeField] private string bossId = "boss_tutorial";
    [SerializeField] private int xpReward = 220;
    [SerializeField] private GameObject expOrbPrefab;
    [SerializeField] private int xpOrbCount = 8;
    [SerializeField] private float xpOrbSpreadRadius = 1.8f;
    [SerializeField] private bool dropFlowerReward = false;
    [SerializeField] private GameObject flowerDropPrefab;
    [SerializeField] private string flowerId = "flower_tutorial";
    [SerializeField] private string flowerDisplayName = "Fiore del Tutorial";
    
    // Components
    private Rigidbody2D rb;
    private Transform player;
    private Color originalColor;
    
    // State Machine
    private enum BossState { Idle, Chasing, ChargingAttack, Attacking, Jumping, Stunned, PhaseTransition }
    private BossState currentState = BossState.Idle;
    
    private enum BossPhase { Phase1, Phase2, Phase3 }
    private BossPhase currentPhase = BossPhase.Phase1;
    
    private enum AttackType { Jump, Charge }
    
    // Attack Management
    private float lastJumpTime = -999f;
    private float lastChargeTime = -999f;
    private bool isInvulnerable = false;
    
    // Charge State
    private Vector2 ChargeDirection;
    private bool isChargingAttack = false;
    private float ChargeTimer = 0f;
    
    // Jump State
    private GameObject currentLandingIndicator;
    
    // AI Decision
    private float thinkTimer = 0f;
    private float thinkInterval = 1f;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();
            
        originalColor = spriteRenderer.color;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        
        if (arenaCenter == null)
            arenaCenter = transform;
        
        StartCoroutine(BossAI());
    }

    void Update()
    {
        if (currentState == BossState.Stunned || currentState == BossState.PhaseTransition)
            return;
            
        // Flip sprite
        if (player != null && currentState != BossState.ChargingAttack)
        {
            spriteRenderer.flipX = player.position.x < transform.position.x;
        }
    }

    #region Boss AI

    IEnumerator BossAI()
    {
        yield return new WaitForSeconds(1.5f);
        
        while (currentHealth > 0)
        {
            if (player == null)
            {
                currentState = BossState.Idle;
                animator.Play("Idle");
                yield return new WaitForSeconds(0.5f);
                continue;
            }
            
            if (currentState == BossState.Stunned || currentState == BossState.PhaseTransition)
            {
                yield return null;
                continue;
            }
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            AttackType chosenAttack = DecideAttack(distanceToPlayer);
            
            if (chosenAttack == AttackType.Jump && Time.time >= lastJumpTime + JumpCooldown)
            {
                yield return StartCoroutine(JumpSlamAttack());
            }
            else if (chosenAttack == AttackType.Charge && Time.time >= lastChargeTime + ChargeCooldown)
            {
                yield return StartCoroutine(ChargeAttack());
            }
            else
            {
                currentState = BossState.Chasing;
                MoveTowardsPreferredDistance(distanceToPlayer);
                yield return new WaitForSeconds(0.3f);
            }
            
            // ✅ PAUSA PIÙ LUNGA IN FASE 3 per dare respiro al player
            float waitTime = currentPhase == BossPhase.Phase3 ? 0.4f : 0.6f;
            yield return new WaitForSeconds(waitTime);
        }
    }

    AttackType DecideAttack(float distanceToPlayer)
    {
        if (currentPhase == BossPhase.Phase1)
        {
            if (distanceToPlayer > 8f)
                return AttackType.Charge;
            else if (distanceToPlayer < 4f)
                return AttackType.Jump;
            else
                return Random.value > 0.5f ? AttackType.Jump : AttackType.Charge;
        }
        else if (currentPhase == BossPhase.Phase2)
        {
            if (distanceToPlayer > 10f)
                return AttackType.Charge;
            else
                return Random.value > 0.4f ? AttackType.Jump : AttackType.Charge; // ← Meno aggressivo
        }
        else
        {
            // ✅ FASE 3: Meno caos, più prevedibile
            return Random.value > 0.5f ? AttackType.Jump : AttackType.Charge;
        }
    }

    void MoveTowardsPreferredDistance(float currentDistance)
    {
        if (player == null) return;
        
        animator.Play("Idle");
        
        Vector2 direction;
        float moveSpeed;
        
        if (currentDistance > preferredDistance + 2f)
        {
            direction = (player.position - transform.position).normalized;
            moveSpeed = chaseSpeed;
        }
        else if (currentDistance < preferredDistance - 2f)
        {
            direction = (transform.position - player.position).normalized;
            moveSpeed = IdleWalkSpeed;
        }
        else
        {
            Vector2 toPlayer = player.position - transform.position;
            direction = new Vector2(-toPlayer.y, toPlayer.x).normalized;
            moveSpeed = IdleWalkSpeed;
        }
        
        rb.linearVelocity = direction * moveSpeed;
    }

    #endregion

    #region Jump Slam Attack

    IEnumerator JumpSlamAttack()
    {
        currentState = BossState.Jumping;
        rb.linearVelocity = Vector2.zero;
        
        animator.Play("Charge");
        yield return new WaitForSeconds(JumpPreparationTime);
        
        SoundEffectManager.Play(JumpSound);
        animator.Play("Jump");
        
        Vector3 startPos = transform.position;
        Vector3 targetPos = player != null ? player.position : startPos + Vector3.right * 3f;
        Vector3 peakPos = (startPos + targetPos) / 2f + Vector3.up * JumpHeight;
        
        if (landingIndicatorPrefab != null)
        {
            currentLandingIndicator = Instantiate(landingIndicatorPrefab, targetPos, Quaternion.identity);
        }
        
        float ascentTime = JumpDuration * 0.4f;
        yield return StartCoroutine(MoveAlongArc(startPos, peakPos, ascentTime));
        
        yield return new WaitForSeconds(JumpDuration * 0.1f);
        
        animator.Play("Attack");
        
        float descentTime = JumpDuration * 0.5f;
        Vector3 landPos = currentLandingIndicator != null ? 
            currentLandingIndicator.transform.position : targetPos;
            
        yield return StartCoroutine(MoveAlongArc(peakPos, landPos, descentTime));
        
        transform.position = landPos;
        PerformLanding(landPos);
        
        if (currentLandingIndicator != null)
            Destroy(currentLandingIndicator);
        
        lastJumpTime = Time.time;
        
        // ✅ SOLO in Phase 3 il double jump (prima era in Phase 2+)
        if (currentPhase == BossPhase.Phase3)
        {
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(QuickJump());
        }
        
        animator.Play("Idle");
        currentState = BossState.Idle;
    }

    IEnumerator QuickJump()
    {
        if (player == null) yield break;
        
        Vector3 startPos = transform.position;
        Vector3 targetPos = player.position;
        
        float quickDuration = JumpDuration * 0.6f;
        
        SoundEffectManager.Play(JumpSound);
        
        float elapsed = 0f;
        while (elapsed < quickDuration)
        {
            float t = elapsed / quickDuration;
            float height = JumpHeight * 0.6f * Mathf.Sin(t * Mathf.PI);
            transform.position = Vector3.Lerp(startPos, targetPos, t) + Vector3.up * height;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = targetPos;
        PerformLanding(targetPos);
    }

    IEnumerator MoveAlongArc(Vector3 start, Vector3 end, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(start, end, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = end;
    }

    void PerformLanding(Vector3 position)
    {
        SoundEffectManager.Play(landSound);
        
        if (JumpImpactEffect != null)
        {
            GameObject effect = Instantiate(JumpImpactEffect, position, Quaternion.identity);
            Destroy(effect, 1.5f);
        }
        
        CameraShake(0.3f);
        DealAOEDamage(position, JumpLandRadius, JumpDamage);
    }

    #endregion

    #region Charge Attack

    IEnumerator ChargeAttack()
    {
        currentState = BossState.ChargingAttack;
        rb.linearVelocity = Vector2.zero;
        
        if (player == null) yield break;
        
        ChargeDirection = (player.position - transform.position).normalized;
        
        animator.Play("Charge");
        SoundEffectManager.Play(ChargeSound);
        
        yield return new WaitForSeconds(ChargePreparationTime);
        
        animator.Play("Attack");
        
        currentState = BossState.Attacking;
        isChargingAttack = true;
        ChargeTimer = 0f;
        
        float currentChargeSpeed = ChargeSpeed;
        
        // ✅ FASE 3: Velocità ridotta invece che aumentata
        if (currentPhase == BossPhase.Phase3)
        {
            currentChargeSpeed *= 1.2f; // ← ERA 1.5f
        }
        
        while (ChargeTimer < ChargeDuration && isChargingAttack)
        {
            // ✅ ZIGZAG RIMOSSO - troppo difficile da schivare
            rb.linearVelocity = ChargeDirection * currentChargeSpeed;
            
            RaycastHit2D hit = Physics2D.Raycast(transform.position, ChargeDirection, 1f, wallLayer);
            if (hit.collider != null)
            {
                yield return StartCoroutine(WallStun());
                yield break;
            }
            
            ChargeTimer += Time.deltaTime;
            yield return null;
        }
        
        isChargingAttack = false;
        rb.linearVelocity = Vector2.zero;
        animator.Play("Idle");
        lastChargeTime = Time.time;
        currentState = BossState.Idle;
    }

    IEnumerator WallStun()
    {
        currentState = BossState.Stunned;
        isChargingAttack = false;
        rb.linearVelocity = Vector2.zero;
        
        animator.Play("Idle");
        SoundEffectManager.Play(hurtSound);
        
        CameraShake(0.4f);
        
        for (int i = 0; i < 4; i++)
        {
            spriteRenderer.color = Color.gray;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.15f);
        }
        
        yield return new WaitForSeconds(wallStunDuration - 1.2f);
        
        currentState = BossState.Idle;
    }

    #endregion

    #region Damage & Health

    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (isInvulnerable) return;
        
        currentHealth -= damage;
        SoundEffectManager.Play(hurtSound);
        
        if (hitParticles != null)
        {
            Instantiate(hitParticles, transform.position, Quaternion.identity);
        }
        
        if (rb != null && currentState != BossState.Attacking)
        {
            rb.AddForce(hitDirection * knockbackResistance, ForceMode2D.Impulse);
        }
        
        StartCoroutine(HitFlash());
        
        CheckPhaseTransition();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }

    public string GetBossDisplayName()
    {
        return string.IsNullOrWhiteSpace(bossName) ? "TRASH PANDA KING" : bossName.ToUpperInvariant();
    }
    
    void CheckPhaseTransition()
    {
        float healthPercent = currentHealth / maxHealth;
        
        if (healthPercent <= phase3Threshold && currentPhase != BossPhase.Phase3)
        {
            StartCoroutine(TransitionToPhase(BossPhase.Phase3));
        }
        else if (healthPercent <= phase2Threshold && currentPhase != BossPhase.Phase2)
        {
            StartCoroutine(TransitionToPhase(BossPhase.Phase2));
        }
    }

    IEnumerator TransitionToPhase(BossPhase newPhase)
    {
        currentState = BossState.PhaseTransition;
        isInvulnerable = true;
        isChargingAttack = false;
        
        rb.linearVelocity = Vector2.zero;
        animator.Play("Charge");
        SoundEffectManager.Play(phaseChangeSound);
        
        if (newPhase == BossPhase.Phase2 && phase2Effect != null)
        {
            phase2Effect.SetActive(true);
        }
        else if (newPhase == BossPhase.Phase3 && phase3Effect != null)
        {
            phase3Effect.SetActive(true);
        }
        
        CameraShake(0.8f);
        
        for (int i = 0; i < 6; i++)
        {
            spriteRenderer.color = Color.yellow;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(1f);
        
        currentPhase = newPhase;
        
        // ✅ BILANCIAMENTO FASI RIDOTTO
        switch (newPhase)
        {
            case BossPhase.Phase2:
                IdleWalkSpeed *= 1.2f; // ← ERA 1.3f
                chaseSpeed *= 1.2f; // ← ERA 1.3f
                JumpCooldown *= 0.85f; // ← ERA 0.75f
                ChargeCooldown *= 0.85f; // ← ERA 0.8f
                Debug.Log("🦝 BOSS PHASE 2: Angrier Raccoon!");
                break;
                
            case BossPhase.Phase3:
                IdleWalkSpeed *= 1.3f; // ← ERA 1.6f
                chaseSpeed *= 1.3f; // ← ERA 1.6f
                JumpCooldown *= 0.7f; // ← ERA 0.5f (troppo veloce!)
                ChargeCooldown *= 0.7f; // ← ERA 0.6f
                JumpDamage *= 1.2f; // ← ERA 1.3f
                ChargeDamage *= 1.2f; // ← ERA 1.3f
                Debug.Log("🦝 BOSS PHASE 3: RABID TRASH PANDA!");
                break;
        }
        
        isInvulnerable = false;
        animator.Play("Idle");
        currentState = BossState.Idle;
    }

    IEnumerator HitFlash()
    {
        for (int i = 0; i < hitFlashCount; i++)
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashDuration);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(hitFlashDuration);
        }
    }

    void Die()
    {
        StopAllCoroutines();
        
        SoundEffectManager.Play(deathSound);
        animator.Play("Idle");
        
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
        
        BossArenaController arena = FindObjectOfType<BossArenaController>();
        if (arena != null)
        {
            arena.OnBossDefeated();
        }

        GiveRewardsOnce();
        
        StartCoroutine(DeathFade());
    }

    private void GiveRewardsOnce()
    {
        bool firstClear = GameProgress.Instance.MarkBossDefeated(bossId);
        if (!firstClear)
        {
            Debug.Log($"ℹ️ Reward boss già assegnata: {bossId}");
            return;
        }

        if (expOrbPrefab != null && xpOrbCount > 0)
        {
            int xpPerOrb = Mathf.Max(1, Mathf.RoundToInt((float)xpReward / xpOrbCount));
            for (int i = 0; i < xpOrbCount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * xpOrbSpreadRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
                GameObject orb = Instantiate(expOrbPrefab, spawnPos, Quaternion.identity);
                ExperienceOrb orbScript = orb.GetComponent<ExperienceOrb>();
                if (orbScript != null)
                {
                    orbScript.xpAmount = xpPerOrb;
                }
            }
            Debug.Log($"⭐ Boss XP drop: {xpOrbCount} orbs (~{xpReward} XP totale)");
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                PlayerExperience playerExp = playerObj.GetComponent<PlayerExperience>();
                if (playerExp != null)
                {
                    playerExp.GainXP(xpReward);
                    Debug.Log($"⭐ Boss reward XP diretto: +{xpReward}");
                }
            }
        }

        if (!dropFlowerReward)
        {
            return;
        }

        if (flowerDropPrefab != null)
        {
            Vector3 dropPos = transform.position + Vector3.up * 0.5f;
            GameObject flower = Instantiate(flowerDropPrefab, dropPos, Quaternion.identity);
            BossFlowerPickup pickup = flower.GetComponent<BossFlowerPickup>();
            if (pickup != null)
            {
                pickup.Setup(flowerId, flowerDisplayName);
            }
        }
        else
        {
            GameProgress.Instance.CollectFlower(flowerId);
            Debug.Log($"🌸 Fiore sbloccato automaticamente: {flowerDisplayName}");
        }
    }

    IEnumerator DeathFade()
    {
        float fadeTime = 2f;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            float alpha = 1f - (elapsed / fadeTime);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(gameObject);
    }

    #endregion

    #region Utilities

    void DealAOEDamage(Vector2 center, float radius, float damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Vector2 hitDir = ((Vector2)hit.transform.position - center).normalized;
                    playerHealth.TakeDamage(damage, hitDir);
                }
            }
        }
    }

    void CameraShake(float intensity)
    {
        CameraShake shake = Camera.main?.GetComponent<CameraShake>();
        if (shake != null)
        {
            shake.Shake(0.3f, intensity);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (isChargingAttack)
            {
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Vector2 hitDir = (collision.transform.position - transform.position).normalized;
                    playerHealth.TakeDamage(ChargeDamage, hitDir * ChargeHitKnockback);
                    
                    isChargingAttack = false;
                    rb.linearVelocity = Vector2.zero;
                }
            }
            else if (Time.time >= lastContactDamageTime + contactDamageCooldown)
            {
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Vector2 hitDir = (collision.transform.position - transform.position).normalized;
                    playerHealth.TakeDamage(contactDamage, hitDir);
                    lastContactDamageTime = Time.time;
                }
            }
        }
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        Gizmos.DrawWireSphere(center, arenaRadius);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, JumpLandRadius);
        
        if (Application.isPlaying && isChargingAttack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, ChargeDirection * 5f);
        }
    }

    #endregion
}
