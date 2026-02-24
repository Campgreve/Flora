using System.Collections;
using UnityEngine;

public class FireBoss : MonoBehaviour, IDamageable, IBossHealthSource
{
    [Header("Identity")]
    [SerializeField] private string bossDisplayName = "INFERNAL WARDEN";
    [SerializeField] private string bossId = "boss_forest_fire";

    [Header("Health")]
    [SerializeField] private float maxHealth = 620f;
    [SerializeField] private float phase2Threshold = 0.65f;
    [SerializeField] private float phase3Threshold = 0.3f;

    [Header("Arena Movement")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float moveRadius = 7f;
    [SerializeField] private float moveSpeed = 2.4f;
    [SerializeField] private float preferredDistanceFromPlayer = 5.8f;
    [SerializeField] private bool startMovingFromPhase2 = true;
    [SerializeField] private float phase1MoveMultiplier = 0.35f;
    [SerializeField] private float phase2MoveMultiplier = 1f;
    [SerializeField] private float phase3MoveMultiplier = 1.28f;
    [SerializeField] private float idleHoverRadius = 0.45f;

    [Header("Projectile")]
    [SerializeField] private FireBossProjectile projectilePrefab;
    [SerializeField] private Transform shotOrigin;
    [SerializeField] private float projectileSpeed = 9.5f;
    [SerializeField] private float projectileDamage = 11f;
    [SerializeField] private float projectileLifetime = 4f;

    [Header("Attack Pattern: Fan Volley")]
    [SerializeField] private int fanShotCount = 7;
    [SerializeField] private float fanSpreadAngle = 65f;
    [SerializeField] private int fanBursts = 2;
    [SerializeField] private float fanBurstInterval = 0.35f;

    [Header("Attack Pattern: Radial Burst")]
    [SerializeField] private int radialShotCount = 14;
    [SerializeField] private int radialWaves = 2;
    [SerializeField] private float radialWaveInterval = 0.4f;

    [Header("Attack Pattern: Flame Sweep")]
    [SerializeField] private int sweepShots = 10;
    [SerializeField] private float sweepArcAngle = 120f;
    [SerializeField] private float sweepShotInterval = 0.08f;

    [Header("Timing")]
    [SerializeField] private float baseAttackCooldown = 1.2f;
    [SerializeField] private float telegraphDelay = 0.45f;

    [Header("Visual & FX")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject castFxPrefab;
    [SerializeField] private GameObject deathFxPrefab;
    [SerializeField] private Color phase3Tint = new Color(1f, 0.55f, 0.55f, 1f);

    [Header("Audio")]
    [SerializeField] private string castSfx = "BossCharge";
    [SerializeField] private string phaseSfx = "BossEnrage";
    [SerializeField] private string hurtSfx = "BossHurt";
    [SerializeField] private string deathSfx = "BossDeath";

    [Header("Rewards")]
    [SerializeField] private int xpReward = 360;
    [SerializeField] private GameObject expOrbPrefab;
    [SerializeField] private int xpOrbCount = 12;
    [SerializeField] private float xpOrbSpreadRadius = 2.2f;
    [SerializeField] private GameObject flowerDropPrefab;
    [SerializeField] private string flowerId = "flower_forest";
    [SerializeField] private string flowerDisplayName = "Fiore della Foresta Infiammata";

    private enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3
    }

    private enum AttackType
    {
        FanVolley,
        RadialBurst,
        FlameSweep
    }

    private float currentHealth;
    private bool isDead;
    private bool initialized;
    private bool aiActive;
    private Transform player;
    [SerializeField] private Transform playerOverride;
    private Coroutine aiRoutine;
    private Color baseColor = Color.white;
    private BossPhase currentPhase = BossPhase.Phase1;
    private bool warnedMissingProjectile;
    private bool isCastingAttack;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (shotOrigin == null)
        {
            shotOrigin = transform;
        }
        if (arenaCenter == null)
        {
            arenaCenter = transform;
        }
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }

        currentHealth = maxHealth;
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            return;
        }

        EnsureAiRunning();
    }

    private void OnDisable()
    {
        aiActive = false;
        if (aiRoutine != null)
        {
            StopCoroutine(aiRoutine);
            aiRoutine = null;
        }
    }

    private IEnumerator BossAiLoop()
    {
        yield return new WaitForSeconds(0.9f);

        while (!isDead)
        {
            ResolvePlayerReference();

            if (player == null)
            {
                // Fail-safe: se non trova il player, non resta "morto" al centro.
                WanderAroundCenter();
                yield return null;
                continue;
            }

            UpdatePhase();

            AttackType attack = PickAttack();
            yield return StartCoroutine(PerformAttack(attack));

            float phaseFactor = currentPhase == BossPhase.Phase3 ? 0.75f : currentPhase == BossPhase.Phase2 ? 0.9f : 1f;
            yield return new WaitForSeconds(baseAttackCooldown * phaseFactor);
        }
    }

    private void MoveInsideArena()
    {
        if (player == null)
        {
            return;
        }

        float moveMultiplier = GetPhaseMoveMultiplier();
        if (moveMultiplier <= 0.01f)
        {
            HoverAroundCenter();
            return;
        }

        Vector2 toPlayer = (player.position - transform.position);
        float distance = toPlayer.magnitude;
        Vector2 desiredDir;

        if (distance > preferredDistanceFromPlayer + 1f)
        {
            desiredDir = toPlayer.normalized;
        }
        else if (distance < preferredDistanceFromPlayer - 1f)
        {
            desiredDir = -toPlayer.normalized;
        }
        else
        {
            desiredDir = new Vector2(-toPlayer.y, toPlayer.x).normalized;
        }

        Vector3 targetPos = transform.position + (Vector3)(desiredDir * (moveSpeed * moveMultiplier) * Time.deltaTime);
        Vector2 center = arenaCenter.position;
        Vector2 fromCenter = (Vector2)targetPos - center;
        if (fromCenter.magnitude > moveRadius)
        {
            fromCenter = fromCenter.normalized * moveRadius;
            targetPos = center + fromCenter;
        }

        transform.position = targetPos;
    }

    private void WanderAroundCenter()
    {
        Vector2 center = arenaCenter != null ? (Vector2)arenaCenter.position : (Vector2)transform.position;
        float t = Time.time * 1.4f;
        Vector2 offset = new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * Mathf.Min(1.6f, moveRadius * 0.35f);
        Vector2 target = center + offset;
        transform.position = Vector2.Lerp(transform.position, target, Time.deltaTime * moveSpeed);
    }

    private void HoverAroundCenter()
    {
        Vector2 center = arenaCenter != null ? (Vector2)arenaCenter.position : (Vector2)transform.position;
        float t = Time.time * 1.35f;
        Vector2 offset = new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * idleHoverRadius;
        Vector2 target = center + offset;
        transform.position = Vector2.Lerp(transform.position, target, Time.deltaTime * (moveSpeed * 0.75f));
    }

    private AttackType PickAttack()
    {
        float r = Random.value;
        if (currentPhase == BossPhase.Phase1)
        {
            return r < 0.55f ? AttackType.FanVolley : AttackType.RadialBurst;
        }

        if (currentPhase == BossPhase.Phase2)
        {
            if (r < 0.4f) return AttackType.FanVolley;
            if (r < 0.75f) return AttackType.RadialBurst;
            return AttackType.FlameSweep;
        }

        if (r < 0.3f) return AttackType.FanVolley;
        if (r < 0.6f) return AttackType.RadialBurst;
        return AttackType.FlameSweep;
    }

    private IEnumerator PerformAttack(AttackType attackType)
    {
        isCastingAttack = true;

        if (!string.IsNullOrWhiteSpace(castSfx))
        {
            SoundEffectManager.Play(castSfx);
        }

        if (castFxPrefab != null)
        {
            GameObject fx = Instantiate(castFxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 1.2f);
        }

        if (animator != null)
        {
            TryPlayOrTrigger("Charge", "Idle");
        }

        yield return new WaitForSeconds(telegraphDelay);

        switch (attackType)
        {
            case AttackType.FanVolley:
                yield return StartCoroutine(AttackFanVolley());
                break;
            case AttackType.RadialBurst:
                yield return StartCoroutine(AttackRadialBurst());
                break;
            case AttackType.FlameSweep:
                yield return StartCoroutine(AttackFlameSweep());
                break;
        }

        isCastingAttack = false;
    }

    private IEnumerator AttackFanVolley()
    {
        if (player == null)
        {
            yield break;
        }

        int burstCount = currentPhase == BossPhase.Phase3 ? fanBursts + 1 : fanBursts;
        float spread = currentPhase == BossPhase.Phase3 ? fanSpreadAngle + 10f : fanSpreadAngle;

        for (int burst = 0; burst < burstCount; burst++)
        {
            Vector2 toPlayer = ((Vector2)player.position - (Vector2)shotOrigin.position).normalized;
            float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            SpawnArc(baseAngle, fanShotCount, spread, projectileSpeed, projectileDamage);
            yield return new WaitForSeconds(fanBurstInterval);
        }
    }

    private IEnumerator AttackRadialBurst()
    {
        int waves = currentPhase == BossPhase.Phase1 ? radialWaves : radialWaves + 1;
        int shots = currentPhase == BossPhase.Phase3 ? radialShotCount + 4 : radialShotCount;

        for (int wave = 0; wave < waves; wave++)
        {
            float offset = (360f / shots) * (wave * 0.5f);
            for (int i = 0; i < shots; i++)
            {
                float angle = offset + (360f / shots) * i;
                Vector2 dir = AngleToDir(angle);
                SpawnProjectile(dir, projectileSpeed * 0.92f, projectileDamage * 0.95f);
            }
            yield return new WaitForSeconds(radialWaveInterval);
        }
    }

    private IEnumerator AttackFlameSweep()
    {
        if (player == null)
        {
            yield break;
        }

        Vector2 toPlayer = ((Vector2)player.position - (Vector2)shotOrigin.position).normalized;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        int shots = currentPhase == BossPhase.Phase3 ? sweepShots + 4 : sweepShots;

        float start = baseAngle - sweepArcAngle * 0.5f;
        float step = sweepArcAngle / Mathf.Max(1, shots - 1);
        for (int i = 0; i < shots; i++)
        {
            Vector2 dir = AngleToDir(start + step * i);
            SpawnProjectile(dir, projectileSpeed * 1.08f, projectileDamage);
            yield return new WaitForSeconds(sweepShotInterval);
        }

        yield return new WaitForSeconds(0.08f);

        for (int i = shots - 1; i >= 0; i--)
        {
            Vector2 dir = AngleToDir(start + step * i);
            SpawnProjectile(dir, projectileSpeed * 1.08f, projectileDamage);
            yield return new WaitForSeconds(sweepShotInterval);
        }
    }

    private void SpawnArc(float baseAngle, int count, float spread, float speedMul, float damageMul)
    {
        if (count <= 0)
        {
            return;
        }

        if (count == 1)
        {
            SpawnProjectile(AngleToDir(baseAngle), speedMul, damageMul);
            return;
        }

        float start = baseAngle - spread * 0.5f;
        float step = spread / (count - 1);
        for (int i = 0; i < count; i++)
        {
            float angle = start + step * i;
            SpawnProjectile(AngleToDir(angle), speedMul, damageMul);
        }
    }

    private void SpawnProjectile(Vector2 dir, float speedMul, float damageMul)
    {
        if (projectilePrefab == null)
        {
            if (!warnedMissingProjectile)
            {
                warnedMissingProjectile = true;
                Debug.LogWarning("🔥 FireBoss: projectilePrefab non assegnato. Assegna FireBossProjectile nel prefab boss.");
            }
            return;
        }

        FireBossProjectile proj = Instantiate(projectilePrefab, shotOrigin.position, Quaternion.identity);
        proj.Initialize(dir, speedMul, damageMul, projectileLifetime);
    }

    private static Vector2 AngleToDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    private void UpdatePhase()
    {
        float hpPct = GetHealthPercent();
        BossPhase next = hpPct <= phase3Threshold ? BossPhase.Phase3 :
                         hpPct <= phase2Threshold ? BossPhase.Phase2 : BossPhase.Phase1;

        if (next == currentPhase)
        {
            return;
        }

        currentPhase = next;
        if (!string.IsNullOrWhiteSpace(phaseSfx))
        {
            SoundEffectManager.Play(phaseSfx);
        }

        if (currentPhase == BossPhase.Phase3 && spriteRenderer != null)
        {
            spriteRenderer.color = phase3Tint;
        }
    }

    public void TakeDamage(float dmg, Vector2 hitDirection)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= dmg;
        PlayHitAnimation();
        if (!string.IsNullOrWhiteSpace(hurtSfx))
        {
            SoundEffectManager.Play(hurtSfx, true);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        StopAllCoroutines();

        if (!string.IsNullOrWhiteSpace(deathSfx))
        {
            SoundEffectManager.Play(deathSfx);
        }

        if (deathFxPrefab != null)
        {
            GameObject fx = Instantiate(deathFxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        Collider2D[] cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].enabled = false;
        }

        GiveRewardsOnce();

        BossArenaController arena = FindObjectOfType<BossArenaController>();
        if (arena != null)
        {
            arena.OnBossDefeated();
        }

        Destroy(gameObject, 0.9f);
    }

    private void GiveRewardsOnce()
    {
        bool firstClear = GameProgress.Instance.MarkBossDefeated(bossId);
        if (!firstClear)
        {
            return;
        }

        if (expOrbPrefab != null && xpOrbCount > 0)
        {
            int xpPerOrb = Mathf.Max(1, Mathf.RoundToInt((float)xpReward / xpOrbCount));
            for (int i = 0; i < xpOrbCount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * xpOrbSpreadRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
                GameObject orbObj = Instantiate(expOrbPrefab, spawnPos, Quaternion.identity);
                ExperienceOrb orb = orbObj.GetComponent<ExperienceOrb>();
                if (orb != null)
                {
                    orb.xpAmount = xpPerOrb;
                }
            }
        }

        if (flowerDropPrefab != null)
        {
            Vector3 dropPos = transform.position + Vector3.up * 0.6f;
            GameObject flowerObj = Instantiate(flowerDropPrefab, dropPos, Quaternion.identity);
            BossFlowerPickup pickup = flowerObj.GetComponent<BossFlowerPickup>();
            if (pickup != null)
            {
                pickup.Setup(flowerId, flowerDisplayName);
            }
        }
        else
        {
            GameProgress.Instance.CollectFlower(flowerId);
        }
    }

    public float GetHealthPercent()
    {
        return Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
    }

    public string GetBossDisplayName()
    {
        return string.IsNullOrWhiteSpace(bossDisplayName) ? "INFERNAL WARDEN" : bossDisplayName;
    }

    private void OnDrawGizmosSelected()
    {
        if (arenaCenter != null)
        {
            Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(arenaCenter.position, moveRadius);
        }
    }

    private void Start()
    {
        EnsureAiRunning();
    }

    private void Update()
    {
        if (!aiActive || isDead)
        {
            return;
        }

        ResolvePlayerReference();

        if (isCastingAttack)
        {
            return;
        }

        MoveInsideArena();
    }

    private void EnsureAiRunning()
    {
        if (isDead || aiActive || !isActiveAndEnabled)
        {
            return;
        }

        aiActive = true;
        aiRoutine = StartCoroutine(BossAiLoop());
    }

    private void ResolvePlayerReference()
    {
        if (playerOverride != null)
        {
            player = playerOverride;
            return;
        }

        if (player != null)
        {
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            return;
        }

        PlayerMovement movement = FindObjectOfType<PlayerMovement>();
        if (movement != null)
        {
            player = movement.transform;
        }
    }

    private float GetPhaseMoveMultiplier()
    {
        if (startMovingFromPhase2 && currentPhase == BossPhase.Phase1)
        {
            return phase1MoveMultiplier;
        }

        switch (currentPhase)
        {
            case BossPhase.Phase1:
                return phase1MoveMultiplier;
            case BossPhase.Phase2:
                return phase2MoveMultiplier;
            case BossPhase.Phase3:
                return phase3MoveMultiplier;
            default:
                return 1f;
        }
    }

    private void TryPlayOrTrigger(string triggerOrState, string fallbackState)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerOrState))
        {
            return;
        }

        if (HasAnimatorParameter(triggerOrState, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(triggerOrState);
            return;
        }

        if (HasAnimatorState(triggerOrState))
        {
            animator.Play(triggerOrState);
            return;
        }

        if (!string.IsNullOrWhiteSpace(fallbackState) && HasAnimatorState(fallbackState))
        {
            animator.Play(fallbackState);
        }
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == paramName && parameters[i].type == type)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAnimatorState(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        int stateHash = Animator.StringToHash(stateName);
        int layerCount = animator.layerCount;
        for (int layer = 0; layer < layerCount; layer++)
        {
            if (animator.HasState(layer, stateHash))
            {
                return true;
            }
        }

        return false;
    }

    private void PlayHitAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (HasAnimatorParameter("Hit", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Hit");
            return;
        }

        if (HasAnimatorState("Hit"))
        {
            animator.Play("Hit");
        }
    }
}
