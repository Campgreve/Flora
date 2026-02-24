using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceDragonBoss : MonoBehaviour, IDamageable, IBossHealthSource
{
    [System.Serializable]
    private class WaveDefinition
    {
        [SerializeField] public string waveName = "Wave";
        [SerializeField] public int enemyCount = 3;
        [SerializeField] public float spawnInterval = 0.25f;
        [SerializeField] public GameObject[] enemyPrefabs;
        [Tooltip("Optional: put one parent here, all children become spawn points.")]
        [SerializeField] public Transform spawnRoot;
        [SerializeField] public Transform[] spawnPoints;
    }

    [Header("Identity")]
    [SerializeField] private string bossDisplayName = "FROST WYRM";

    [Header("Health")]
    [SerializeField] private float maxHealth = 520f;
    [SerializeField, Range(0.1f, 0.95f)] private float phase2Threshold = 0.70f;
    [SerializeField, Range(0.05f, 0.7f)] private float phase3Threshold = 0.35f;

    [Header("Arena Anchors (Area1, Area2, Area3)")]
    [SerializeField] private Transform[] phaseAnchors = new Transform[3];
    [SerializeField] private Collider2D[] phaseBoundaries = new Collider2D[3];
    [SerializeField] private float areaWanderRadius = 3.5f;
    [SerializeField] private float moveSpeed = 5.8f;
    [SerializeField] private float movementSmoothing = 0.04f;
    [SerializeField] private float steeringResponsiveness = 12f;
    [SerializeField] private float turnSmoothing = 14f;
    [SerializeField] private float escapeSpeed = 7.5f;
    [SerializeField] private float preferredDistanceFromPlayer = 4.2f;
    [SerializeField] private float distanceTolerance = 0.9f;
    [SerializeField] private float strafeFlipMinInterval = 2.2f;
    [SerializeField] private float strafeFlipMaxInterval = 4.4f;
    [SerializeField] private bool phase1StartsAnchored = true;
    [SerializeField] private float anchoredOrbitRadius = 1.8f;
    [SerializeField] private float anchoredOrbitSpeed = 1.7f;
    [SerializeField] private float anchoredPullStrength = 2.6f;
    [SerializeField] private float zigzagAmplitude = 0.7f;
    [SerializeField] private float zigzagFrequency = 4.4f;
    [SerializeField] private float zigzagActivationAfterRelease = 0.25f;
    [SerializeField] private float phase3ZigzagChaseWeight = 0.9f;
    [SerializeField] private float phase3MoveSpeedMultiplier = 1.25f;
    [SerializeField] private bool phase3DashCentricMode = true;
    [SerializeField] private float phase3BetweenDashSpeedMultiplier = 0.55f;
    [SerializeField] private float phase3ResetDistanceFromPlayer = 5.2f;
    [SerializeField] private float phase3ResetDistanceTolerance = 1.25f;
    [SerializeField] private bool phase3BreakAnchorCompletely = true;
    [SerializeField] private bool keepPhase3InsideBoundaryIfAssigned = true;
    [SerializeField] private float phase3DashTriggerDistance = 6.2f;
    [SerializeField] private float phase3DashChargeTime = 0.42f;
    [SerializeField] private float phase3DashDuration = 0.28f;
    [SerializeField] private float phase3DashSpeed = 14.5f;
    [SerializeField] private float phase3DashCooldownMin = 1.8f;
    [SerializeField] private float phase3DashCooldownMax = 2.8f;
    [SerializeField] private float phase2EscapeSpeedMultiplier = 1.45f;
    [SerializeField] private float phase3EscapeSpeedMultiplier = 1.2f;
    [SerializeField] private float maxEscapeDuration = 5f;

    [Header("Combat")]
    [SerializeField] private float contactDamage = 12f;
    [SerializeField] private float contactDamageCooldown = 0.7f;
    [SerializeField] private float baseAttackCooldown = 1.3f;
    [SerializeField] private float phase3AttackCooldown = 0.95f;
    [SerializeField] private float attackTelegraph = 0.35f;

    [Header("Ranged Projectiles")]
    [SerializeField] private FireBossProjectile sideProjectilePrefab;
    [SerializeField] private float sideProjectileSpeed = 9f;
    [SerializeField] private float sideProjectileDamage = 9f;
    [SerializeField] private float sideProjectileLifetime = 2.5f;
    [SerializeField] private int volleyShotCount = 5;
    [SerializeField] private float volleySpreadAngle = 42f;
    [SerializeField] private int volleyBursts = 2;
    [SerializeField] private float volleyBurstInterval = 0.2f;
    [SerializeField] private int radialShotCount = 10;
    [SerializeField] private int zigzagSweepBursts = 3;
    [SerializeField] private int zigzagSweepShotsPerBurst = 3;
    [SerializeField] private float zigzagSweepSpacing = 14f;
    [SerializeField] private float zigzagSweepBurstInterval = 0.14f;

    [Header("Gates (optional)")]
    [SerializeField] private GameObject gateToArea2;
    [SerializeField] private GameObject gateToArea3;

    [Header("Intermission Waves")]
    [SerializeField] private List<WaveDefinition> area1IntermissionWaves = new List<WaveDefinition>();
    [SerializeField] private List<WaveDefinition> area2IntermissionWaves = new List<WaveDefinition>();
    [SerializeField] private bool waitAllAddsBeforeNextPhase = true;
    [SerializeField] private bool waitPlayerEnterNextAreaToResumeCombat = true;
    [SerializeField] private Transform defaultSpawnRoot;
    [SerializeField] private string fallbackSpawnRootName = "BossSpawn";

    [Header("Single-Area Phase Flow")]
    [SerializeField] private bool useSingleAreaPhaseFlow = true;
    [SerializeField] private Transform singleAreaAnchor;
    [SerializeField] private Collider2D singleAreaBoundary;
    [SerializeField] private bool phase2RemainsAnchored = true;
    [SerializeField] private bool hideBossDuringIntermission = true;
    [SerializeField] private float hiddenYOffset = 30f;
    [SerializeField] private float reappearDelay = 0.35f;
    [SerializeField] private bool fakeEscapeBeforeHide = true;
    [SerializeField] private float fakeEscapeDuration = 1.2f;
    [SerializeField] private float fakeEscapeDistance = 11.5f;
    [SerializeField] private float fakeEscapeCurveStrength = 1.2f;
    [SerializeField] private bool hideOnlyAfterLeavingCamera = true;
    [SerializeField] private float maxFakeEscapeExtraTime = 0.9f;

    [Header("Final Phase Support Waves")]
    [SerializeField] private List<WaveDefinition> area3SupportWaves = new List<WaveDefinition>();
    [SerializeField] private float phase3SupportWaveCadence = 12f;
    [SerializeField] private int maxAliveAdds = 4;
    [SerializeField, Range(0.01f, 0.3f)] private float stopSupportWavesUnderHealth = 0.15f;

    [Header("Multipart Visual Parts (optional)")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform wingL;
    [SerializeField] private Transform wingR;
    [SerializeField] private Transform body1;
    [SerializeField] private Transform body2;
    [SerializeField] private Transform bodyEnd;
    [SerializeField] private Transform tail;
    [SerializeField] private float wingFlapAngle = 15f;
    [SerializeField] private float wingFlapSpeed = 8f;
    [SerializeField] private float segmentBobAmount = 0.05f;
    [SerializeField] private float segmentBobSpeed = 4f;
    [SerializeField] private float headLeadAmount = 0.12f;
    [SerializeField] private float segmentSpacing = 0.26f;
    [SerializeField] private float segmentWaveAmplitude = 0.12f;
    [SerializeField] private float segmentWaveSpeed = 8f;
    [SerializeField] private float segmentWavePhaseOffset = 0.7f;
    [SerializeField] private float segmentFollowSmoothTime = 0.08f;
    [SerializeField] private float trailPointMinDistance = 0.08f;
    [SerializeField] private float wormFollowSharpness = 22f;
    [SerializeField] private int trailFramesPerSegment = 6;
    [SerializeField] private bool rigidWormChain = true;
    [SerializeField] private float trailSpacingMultiplier = 1.15f;
    [SerializeField] private float segmentMinGapMultiplier = 0.65f;
    [SerializeField] private float segmentMaxGapMultiplier = 1.45f;
    [SerializeField] private float segmentTurnSpeed = 18f;
    [SerializeField] private float segmentFacingOffset = -90f;
    [SerializeField] private float hardSnapDistance = 1.2f;
    [SerializeField] private bool autoGenerateBodySegments = true;
    [SerializeField] private Transform bodySegmentTemplate;
    [SerializeField] private int generatedBodySegmentCount = 6;
    [SerializeField] private bool hideManualBodyWhenGenerated = true;

    [Header("Tail Tip Anchor (optional)")]
    [SerializeField] private bool lockTailTipWhenAnchored = true;
    [SerializeField] private Transform tailTipAnchor;
    [SerializeField] private bool lockTailTipInPhase1 = true;
    [SerializeField] private bool lockTailTipInPhase2 = true;
    [SerializeField] private float tailTipSnapSpeed = 28f;

    [Header("Audio (optional)")]
    [SerializeField] private string hurtSfx = "BossHurt";
    [SerializeField] private string phaseSfx = "BossEnrage";
    [SerializeField] private string deathSfx = "BossDeath";

    private enum BossPhase
    {
        Phase1 = 0,
        Phase2 = 1,
        Phase3 = 2
    }

    private float currentHealth;
    private BossPhase currentPhase = BossPhase.Phase1;
    private bool isDead;
    private bool isInvulnerable;
    private bool isTransitioning;
    private bool isRushing;
    private float nextAttackAt;
    private float nextContactDamageAt;
    private float nextSupportWaveAt;
    private float zigzagUnlockedAt;
    private bool isFreedFromAnchor;
    private float anchoredOrbitSeed;
    private Transform player;
    private Coroutine aiRoutine;
    private readonly List<GameObject> activeAdds = new List<GameObject>();
    private SpriteRenderer[] allRenderers;
    private Collider2D[] allColliders;

    private Vector3 headBaseLocalPos;
    private Vector3 body1BaseLocalPos;
    private Vector3 body2BaseLocalPos;
    private Vector3 bodyEndBaseLocalPos;
    private Quaternion wingLBaseRot;
    private Quaternion wingRBaseRot;
    private Transform[] segmentChain;
    private Vector3[] segmentBaseLocalPos;
    private Vector3[] segmentSmoothDampVelocity;
    private Vector3 lastBossPosition;
    private Vector2 smoothedMoveDir = Vector2.right;
    private float currentMoveSpeed01;
    private readonly List<Vector3> moveTrail = new List<Vector3>();
    private readonly List<Transform> generatedSegments = new List<Transform>();
    private Vector2 moveSmoothVelocity;
    private Vector2 locomotionVelocity;
    private int strafeDirectionSign = 1;
    private float nextStrafeFlipAt;
    private Rigidbody2D rb2d;
    private Transform runtimeArenaAnchor;
    private bool isPhase3DashCharging;
    private bool isPhase3Dashing;
    private float phase3DashEndAt;
    private float nextPhase3DashAt;
    private Vector2 phase3DashDirection = Vector2.down;

    private void Awake()
    {
        AutoAssignMissingParts();
        BuildGeneratedBodySegments();
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        currentHealth = maxHealth;
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        allColliders = GetComponentsInChildren<Collider2D>(true);
        CachePartDefaults();
        BuildSegmentChain();

        if (phaseAnchors == null || phaseAnchors.Length < 3)
        {
            phaseAnchors = new Transform[3];
        }

        bool missingAnchorConfig = false;
        Vector3 fallbackAnchorPosition = transform.position;
        for (int i = 0; i < 3; i++)
        {
            if (phaseAnchors[i] == null)
            {
                phaseAnchors[i] = CreateOrReuseRuntimeArenaAnchor(fallbackAnchorPosition);
                missingAnchorConfig = true;
            }
            else if (phaseAnchors[i] == transform)
            {
                phaseAnchors[i] = CreateOrReuseRuntimeArenaAnchor(fallbackAnchorPosition);
                missingAnchorConfig = true;
                Debug.LogWarning($"IceDragonBoss: phaseAnchors[{i}] era il boss stesso. Sostituito con anchor statico runtime.");
            }
        }

        if (useSingleAreaPhaseFlow)
        {
            Transform sharedAnchor = singleAreaAnchor != null ? singleAreaAnchor : phaseAnchors[0];
            if (sharedAnchor == null)
            {
                sharedAnchor = CreateOrReuseRuntimeArenaAnchor(fallbackAnchorPosition);
            }
            if (sharedAnchor == transform)
            {
                sharedAnchor = CreateOrReuseRuntimeArenaAnchor(fallbackAnchorPosition);
                Debug.LogWarning("IceDragonBoss: singleAreaAnchor era il boss stesso. Creato un anchor statico runtime per evitare drift.");
            }

            for (int i = 0; i < 3; i++)
            {
                phaseAnchors[i] = sharedAnchor;
            }

            if (phaseBoundaries == null || phaseBoundaries.Length < 3)
            {
                phaseBoundaries = new Collider2D[3];
            }

            Collider2D sharedBoundary = singleAreaBoundary != null ? singleAreaBoundary : phaseBoundaries[0];
            for (int i = 0; i < 3; i++)
            {
                phaseBoundaries[i] = sharedBoundary;
            }
        }

        if (missingAnchorConfig)
        {
            Debug.LogWarning("IceDragonBoss: phaseAnchors non configurati. Assegna 3 anchor in scena (Area1/Area2/Area3) per sfruttare le transizioni.");
        }

        if (phaseAnchors[0] != null)
        {
            transform.position = phaseAnchors[0].position;
        }

        if (defaultSpawnRoot == null && !string.IsNullOrWhiteSpace(fallbackSpawnRootName))
        {
            GameObject fallback = GameObject.Find(fallbackSpawnRootName);
            if (fallback == null)
            {
                fallback = GameObject.Find("BossArena/BossSpawn");
            }
            if (fallback != null)
            {
                defaultSpawnRoot = fallback.transform;
            }
        }

        lastBossPosition = transform.position;
        isFreedFromAnchor = !phase1StartsAnchored;
        zigzagUnlockedAt = Time.time + Mathf.Max(0f, zigzagActivationAfterRelease);
        anchoredOrbitSeed = Random.Range(0f, 1000f);
        strafeDirectionSign = Random.value < 0.5f ? -1 : 1;
        nextStrafeFlipAt = Time.time + Random.Range(
            Mathf.Max(0.5f, strafeFlipMinInterval),
            Mathf.Max(0.7f, strafeFlipMaxInterval));
        ResetBodyTrailNow();
    }

    private Transform CreateOrReuseRuntimeArenaAnchor(Vector3 position)
    {
        if (runtimeArenaAnchor == null)
        {
            GameObject anchorObj = new GameObject($"{name}_RuntimeArenaAnchor");
            runtimeArenaAnchor = anchorObj.transform;
        }

        runtimeArenaAnchor.position = position;
        return runtimeArenaAnchor;
    }

    private void OnEnable()
    {
        ResolvePlayer();
        aiRoutine = StartCoroutine(BossLoop());
    }

    private void OnDisable()
    {
        if (aiRoutine != null)
        {
            StopCoroutine(aiRoutine);
            aiRoutine = null;
        }
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        UpdateMoveDirection();
        AnimateMultipartVisual();
        CleanupDeadAdds();

        if (currentPhase == BossPhase.Phase3 && !isTransitioning && !isInvulnerable)
        {
            float hp = currentHealth / Mathf.Max(1f, maxHealth);
            if (hp > stopSupportWavesUnderHealth && Time.time >= nextSupportWaveAt && activeAdds.Count < maxAliveAdds)
            {
                StartCoroutine(SpawnSupportWaveInPhase3());
                nextSupportWaveAt = Time.time + phase3SupportWaveCadence;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }

        if (!isTransitioning && !isInvulnerable && !isRushing)
        {
            MoveInsideCurrentArea(Time.fixedDeltaTime);
        }
    }

    private IEnumerator BossLoop()
    {
        yield return new WaitForSeconds(0.9f);

        while (!isDead)
        {
            if (player == null)
            {
                ResolvePlayer();
                yield return null;
                continue;
            }

            if (isTransitioning || isInvulnerable)
            {
                yield return null;
                continue;
            }

            if (Time.time >= nextAttackAt)
            {
                yield return StartCoroutine(DoRangedAttack());
                nextAttackAt = Time.time + (currentPhase == BossPhase.Phase3 ? phase3AttackCooldown : baseAttackCooldown);
            }

            TryStartPhaseTransition();
            yield return null;
        }
    }

    private void MoveInsideCurrentArea(float deltaTime)
    {
        Transform anchor = phaseAnchors[(int)currentPhase];
        if (anchor == null)
        {
            return;
        }

        Vector2 currentPos = transform.position;
        float speed = Mathf.Max(0.5f, moveSpeed);
        Vector2 desiredDir;

        if (player != null)
        {
            if (!isFreedFromAnchor && currentPhase == BossPhase.Phase1)
            {
                desiredDir = ComputeAnchoredPhase1Direction(anchor, currentPos);
            }
            else
            {
                if (currentPhase == BossPhase.Phase3 && isFreedFromAnchor)
                {
                    TryStartPhase3Dash(currentPos);

                    if (isPhase3DashCharging)
                    {
                        desiredDir = Vector2.zero;
                        speed = 0f;
                    }
                    else if (isPhase3Dashing && Time.time < phase3DashEndAt)
                    {
                        desiredDir = phase3DashDirection;
                        speed = Mathf.Max(moveSpeed, phase3DashSpeed);
                    }
                    else
                    {
                        if (phase3DashCentricMode)
                        {
                            desiredDir = ComputePhase3DashResetDirection(currentPos);
                            speed *= Mathf.Clamp(phase3BetweenDashSpeedMultiplier, 0.2f, 1.2f);
                        }
                        else
                        {
                            desiredDir = ComputePhase3SerpentChaseDirection(currentPos);
                            speed *= Mathf.Max(1f, phase3MoveSpeedMultiplier);
                        }
                    }
                }
                else
                {
                if (Time.time >= nextStrafeFlipAt)
                {
                    strafeDirectionSign *= -1;
                    nextStrafeFlipAt = Time.time + Random.Range(
                        Mathf.Max(0.5f, strafeFlipMinInterval),
                        Mathf.Max(0.7f, strafeFlipMaxInterval));
                }

                Vector2 toPlayer = (Vector2)player.position - currentPos;
                float dist = toPlayer.magnitude;
                Vector2 radial = dist > 0.001f ? toPlayer / dist : (smoothedMoveDir.sqrMagnitude > 0.001f ? smoothedMoveDir.normalized : Vector2.right);
                Vector2 tangent = strafeDirectionSign > 0
                    ? new Vector2(-radial.y, radial.x)
                    : new Vector2(radial.y, -radial.x);

                float minDist = Mathf.Max(0.8f, preferredDistanceFromPlayer - Mathf.Max(0.1f, distanceTolerance));
                float maxDist = preferredDistanceFromPlayer + Mathf.Max(0.1f, distanceTolerance);
                if (dist > maxDist)
                {
                    desiredDir = radial;
                }
                else if (dist < minDist)
                {
                    desiredDir = -radial;
                }
                else
                {
                    desiredDir = tangent;
                }

                if (ShouldUseZigzagMovement())
                {
                    desiredDir = ApplyZigzagToDirection(desiredDir);
                }
                }
            }
        }
        else
        {
            Vector2 toAnchor = (Vector2)anchor.position - currentPos;
            desiredDir = toAnchor.sqrMagnitude > 0.0001f ? toAnchor.normalized : (smoothedMoveDir.sqrMagnitude > 0.001f ? smoothedMoveDir.normalized : Vector2.right);
        }

        Vector2 newPos = currentPos + desiredDir.normalized * (speed * deltaTime);

        Collider2D boundary = phaseBoundaries != null && phaseBoundaries.Length > (int)currentPhase
            ? phaseBoundaries[(int)currentPhase]
            : null;

        bool isPhase3FreeChase = phase3BreakAnchorCompletely && currentPhase == BossPhase.Phase3 && isFreedFromAnchor;
        if (isPhase3FreeChase)
        {
            if (boundary != null && keepPhase3InsideBoundaryIfAssigned)
            {
                newPos = boundary.ClosestPoint(newPos);
            }
        }
        else
        {
            if (boundary != null)
            {
                newPos = boundary.ClosestPoint(newPos);
            }
            else
            {
                Vector2 fromCenter = newPos - (Vector2)anchor.position;
                if (fromCenter.magnitude > areaWanderRadius)
                {
                    fromCenter = fromCenter.normalized * areaWanderRadius;
                    newPos = (Vector2)anchor.position + fromCenter;
                }
            }
        }

        // Same movement style as Enemy: linear step + Rigidbody2D MovePosition.
        SetBossPosition(newPos);
    }

    private Vector2 ComputeAnchoredPhase1Direction(Transform anchor, Vector2 currentPos)
    {
        float t = (Time.time + anchoredOrbitSeed) * Mathf.Max(0.1f, anchoredOrbitSpeed);
        Vector2 ellipse = new Vector2(Mathf.Cos(t), Mathf.Sin(t) * 0.6f);
        Vector2 center = anchor.position;
        float orbitRadius = Mathf.Max(0.3f, anchoredOrbitRadius);
        Vector2 orbitTarget = center + ellipse * orbitRadius;

        Vector2 toOrbit = orbitTarget - currentPos;
        Vector2 tangent = new Vector2(-ellipse.y, ellipse.x).normalized;
        Vector2 orbitDrive = toOrbit.normalized * 0.85f + tangent * 0.5f;

        Vector2 toCenter = center - currentPos;
        float centerDist = toCenter.magnitude;
        float outerLimit = orbitRadius * 1.08f;
        float innerLimit = orbitRadius * 0.5f;
        Vector2 radialCorrection = Vector2.zero;

        if (centerDist > outerLimit && centerDist > 0.0001f)
        {
            float pull = (centerDist - outerLimit) * Mathf.Max(0.2f, anchoredPullStrength);
            radialCorrection = toCenter.normalized * pull;
        }
        else if (centerDist < innerLimit && centerDist > 0.0001f)
        {
            float push = (innerLimit - centerDist) * 0.45f;
            radialCorrection = -toCenter.normalized * push;
        }

        Vector2 mixed = orbitDrive + radialCorrection;
        if (mixed.sqrMagnitude < 0.0001f)
        {
            return tangent.sqrMagnitude > 0.0001f ? tangent : Vector2.right;
        }

        return mixed.normalized;
    }

    private bool ShouldUseZigzagMovement()
    {
        return isFreedFromAnchor && currentPhase != BossPhase.Phase1 && Time.time >= zigzagUnlockedAt;
    }

    private Vector2 ApplyZigzagToDirection(Vector2 baseDirection)
    {
        if (baseDirection.sqrMagnitude < 0.0001f)
        {
            return baseDirection;
        }

        Vector2 dir = baseDirection.normalized;
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
        float zig = Mathf.Sin(Time.time * Mathf.Max(0.1f, zigzagFrequency)) * Mathf.Max(0f, zigzagAmplitude);
        Vector2 zigzagged = dir + perpendicular * zig;
        return zigzagged.sqrMagnitude > 0.0001f ? zigzagged.normalized : dir;
    }

    private Vector2 ComputePhase3SerpentChaseDirection(Vector2 currentPos)
    {
        Vector2 toPlayer = (Vector2)player.position - currentPos;
        Vector2 chaseDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : (smoothedMoveDir.sqrMagnitude > 0.001f ? smoothedMoveDir.normalized : Vector2.down);
        Vector2 zigzagDir = ApplyZigzagToDirection(chaseDir);
        float w = Mathf.Clamp01(phase3ZigzagChaseWeight);
        Vector2 mixed = Vector2.Lerp(chaseDir, zigzagDir, w);
        return mixed.sqrMagnitude > 0.0001f ? mixed.normalized : chaseDir;
    }

    private Vector2 ComputePhase3DashResetDirection(Vector2 currentPos)
    {
        if (player == null)
        {
            return smoothedMoveDir.sqrMagnitude > 0.001f ? smoothedMoveDir.normalized : Vector2.right;
        }

        Vector2 toPlayer = (Vector2)player.position - currentPos;
        float dist = toPlayer.magnitude;
        Vector2 radial = dist > 0.001f ? toPlayer / dist : (smoothedMoveDir.sqrMagnitude > 0.001f ? smoothedMoveDir.normalized : Vector2.right);
        Vector2 tangent = strafeDirectionSign > 0
            ? new Vector2(-radial.y, radial.x)
            : new Vector2(radial.y, -radial.x);

        if (Time.time >= nextStrafeFlipAt)
        {
            strafeDirectionSign *= -1;
            nextStrafeFlipAt = Time.time + Random.Range(
                Mathf.Max(0.7f, strafeFlipMinInterval),
                Mathf.Max(1.2f, strafeFlipMaxInterval));
        }

        float targetDist = Mathf.Max(1f, phase3ResetDistanceFromPlayer);
        float tol = Mathf.Max(0.4f, phase3ResetDistanceTolerance);
        float minDist = targetDist - tol;
        float maxDist = targetDist + tol;

        Vector2 baseDir;
        if (dist < minDist)
        {
            // troppo vicino: allontanati per preparare il prossimo dash
            baseDir = -radial;
        }
        else if (dist > maxDist)
        {
            // troppo lontano: non inseguire diretto, avvicinati poco e in diagonale
            baseDir = (tangent * 0.8f) + (radial * 0.25f);
        }
        else
        {
            // distanza giusta: strafe con un po' di serpentina
            baseDir = tangent;
        }

        return ApplyZigzagToDirection(baseDir);
    }

    private void TryStartPhase3Dash(Vector2 currentPos)
    {
        if (isDead || isTransitioning || isInvulnerable || player == null || isPhase3DashCharging || isPhase3Dashing)
        {
            return;
        }

        if (Time.time < nextPhase3DashAt)
        {
            return;
        }

        float dist = Vector2.Distance(currentPos, player.position);
        if (dist < Mathf.Max(1f, phase3DashTriggerDistance))
        {
            return;
        }

        StartCoroutine(Phase3DashRoutine());
    }

    private IEnumerator Phase3DashRoutine()
    {
        isPhase3DashCharging = true;
        yield return new WaitForSeconds(Mathf.Max(0.05f, phase3DashChargeTime));
        isPhase3DashCharging = false;

        if (isDead || isTransitioning || player == null)
        {
            yield break;
        }

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        phase3DashDirection = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : (smoothedMoveDir.sqrMagnitude > 0.001f ? smoothedMoveDir.normalized : Vector2.down);
        isPhase3Dashing = true;
        phase3DashEndAt = Time.time + Mathf.Max(0.08f, phase3DashDuration);

        while (!isDead && !isTransitioning && Time.time < phase3DashEndAt)
        {
            yield return null;
        }

        isPhase3Dashing = false;
        nextPhase3DashAt = Time.time + Random.Range(
            Mathf.Max(0.2f, phase3DashCooldownMin),
            Mathf.Max(Mathf.Max(0.25f, phase3DashCooldownMin + 0.05f), phase3DashCooldownMax));
    }

    private IEnumerator DoRangedAttack()
    {
        yield return new WaitForSeconds(attackTelegraph);

        int mode = Random.Range(0, 100);
        if (currentPhase == BossPhase.Phase1 && !isFreedFromAnchor)
        {
            if (mode < 45)
            {
                yield return StartCoroutine(FireAimedVolley());
            }
            else if (mode < 80)
            {
                yield return StartCoroutine(FireRadialBurst());
            }
            else
            {
                yield return StartCoroutine(FireAnchorSnare());
            }
            yield break;
        }

        if (mode < 52)
        {
            yield return StartCoroutine(FireAimedVolley());
        }
        else if (mode < 80)
        {
            yield return StartCoroutine(FireRadialBurst());
        }
        else
        {
            yield return StartCoroutine(FireZigzagSweep());
        }
    }

    private IEnumerator FireAimedVolley()
    {
        if (sideProjectilePrefab == null)
        {
            yield break;
        }

        int bursts = currentPhase == BossPhase.Phase3 ? volleyBursts + 1 : volleyBursts;
        bursts = Mathf.Max(1, bursts);

        for (int b = 0; b < bursts; b++)
        {
            Vector2 aimDir = smoothedMoveDir.sqrMagnitude > 0.001f ? smoothedMoveDir.normalized : Vector2.right;
            if (player != null)
            {
                Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    aimDir = toPlayer.normalized;
                }
            }

            float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            SpawnArc(baseAngle, Mathf.Max(1, volleyShotCount), Mathf.Max(0f, volleySpreadAngle));
            yield return new WaitForSeconds(Mathf.Max(0.04f, volleyBurstInterval));
        }
    }

    private IEnumerator FireRadialBurst()
    {
        if (sideProjectilePrefab == null)
        {
            yield break;
        }

        int shots = currentPhase == BossPhase.Phase3 ? radialShotCount + 4 : radialShotCount;
        shots = Mathf.Max(4, shots);
        float step = 360f / shots;
        for (int i = 0; i < shots; i++)
        {
            float angle = step * i;
            SpawnProjectile(AngleToDir(angle));
        }

        yield return null;
    }

    private IEnumerator FireAnchorSnare()
    {
        if (sideProjectilePrefab == null)
        {
            yield break;
        }

        Vector2 aimDir = smoothedMoveDir.sqrMagnitude > 0.001f ? smoothedMoveDir.normalized : Vector2.down;
        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                aimDir = toPlayer.normalized;
            }
        }

        float aimAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        SpawnArc(aimAngle, Mathf.Max(4, volleyShotCount + 1), Mathf.Max(56f, volleySpreadAngle + 16f));
        yield return new WaitForSeconds(0.08f);

        int ringShots = Mathf.Max(6, radialShotCount - 2);
        float step = 360f / ringShots;
        for (int i = 0; i < ringShots; i++)
        {
            if ((i & 1) == 0)
            {
                SpawnProjectile(AngleToDir(step * i));
            }
        }
    }

    private IEnumerator FireZigzagSweep()
    {
        if (sideProjectilePrefab == null)
        {
            yield break;
        }

        int bursts = Mathf.Max(1, zigzagSweepBursts + (currentPhase == BossPhase.Phase3 ? 1 : 0));
        int shotsPerBurst = Mathf.Max(1, zigzagSweepShotsPerBurst);
        float spread = Mathf.Max(8f, zigzagSweepSpacing);

        for (int b = 0; b < bursts; b++)
        {
            Vector2 aimDir = smoothedMoveDir.sqrMagnitude > 0.001f ? smoothedMoveDir.normalized : Vector2.right;
            if (player != null)
            {
                Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    aimDir = toPlayer.normalized;
                }
            }

            float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            float side = (b % 2 == 0 ? -1f : 1f) * spread;
            SpawnArc(baseAngle + side, shotsPerBurst, spread * 1.6f);
            SpawnProjectile(AngleToDir(baseAngle + (side * 1.8f)));

            yield return new WaitForSeconds(Mathf.Max(0.05f, zigzagSweepBurstInterval));
        }
    }

    private void SpawnArc(float baseAngle, int count, float spread)
    {
        if (count <= 0)
        {
            return;
        }

        if (count == 1)
        {
            SpawnProjectile(AngleToDir(baseAngle));
            return;
        }

        float start = baseAngle - spread * 0.5f;
        float step = spread / (count - 1);
        for (int i = 0; i < count; i++)
        {
            float angle = start + step * i;
            SpawnProjectile(AngleToDir(angle));
        }
    }

    private static Vector2 AngleToDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    private void SpawnProjectile(Vector2 direction)
    {
        if (sideProjectilePrefab == null)
        {
            return;
        }

        FireBossProjectile p = Instantiate(sideProjectilePrefab, transform.position, Quaternion.identity);
        p.Initialize(direction, sideProjectileSpeed, sideProjectileDamage, sideProjectileLifetime);
    }

    private void KeepInsideCurrentArea()
    {
        Collider2D boundary = phaseBoundaries != null && phaseBoundaries.Length > (int)currentPhase
            ? phaseBoundaries[(int)currentPhase]
            : null;
        if (boundary != null)
        {
            Vector2 inside = boundary.ClosestPoint(transform.position);
            SetBossPosition(inside);
            return;
        }

        Transform anchor = phaseAnchors[(int)currentPhase];
        if (anchor == null)
        {
            return;
        }

        Vector2 offset = (Vector2)transform.position - (Vector2)anchor.position;
        if (offset.magnitude > areaWanderRadius)
        {
            offset = offset.normalized * areaWanderRadius;
            SetBossPosition(new Vector2(anchor.position.x + offset.x, anchor.position.y + offset.y));
        }
    }

    private void SetBossPosition(Vector2 position)
    {
        if (rb2d != null)
        {
            if (Time.inFixedTimeStep)
            {
                rb2d.MovePosition(position);
            }
            else
            {
                rb2d.position = position;
            }
            return;
        }

        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    private void TryStartPhaseTransition()
    {
        if (isTransitioning || isDead)
        {
            return;
        }

        float hpPercent = currentHealth / Mathf.Max(1f, maxHealth);

        if (currentPhase == BossPhase.Phase1 && hpPercent <= phase2Threshold)
        {
            StartCoroutine(TransitionToPhase2());
        }
        else if (currentPhase == BossPhase.Phase2 && hpPercent <= phase3Threshold)
        {
            StartCoroutine(TransitionToPhase3());
        }
    }

    private IEnumerator TransitionToPhase2()
    {
        isTransitioning = true;
        isInvulnerable = true;
        isRushing = false;
        ResetBodyTrailNow();
        if (!string.IsNullOrWhiteSpace(phaseSfx))
        {
            SoundEffectManager.Play(phaseSfx);
        }

        if (useSingleAreaPhaseFlow)
        {
            yield return StartCoroutine(RunSingleAreaIntermission(
                area1IntermissionWaves,
                BossPhase.Phase2,
                phase2RemainsAnchored));
            yield break;
        }

        SetGate(gateToArea2, true);
        yield return StartCoroutine(EscapeToAnchor(BossPhase.Phase2));
        yield return StartCoroutine(SpawnWaveSet(area1IntermissionWaves));
        if (waitAllAddsBeforeNextPhase)
        {
            yield return StartCoroutine(WaitUntilNoAddsAlive());
        }

        SetGate(gateToArea2, false);
        if (waitPlayerEnterNextAreaToResumeCombat)
        {
            yield return StartCoroutine(WaitForPlayerInPhaseArea(BossPhase.Phase2));
        }
        currentPhase = BossPhase.Phase2;
        isFreedFromAnchor = true;
        zigzagUnlockedAt = Time.time + Mathf.Max(0f, zigzagActivationAfterRelease);
        isInvulnerable = false;
        isTransitioning = false;
        ResetBodyTrailNow();
    }

    private IEnumerator TransitionToPhase3()
    {
        isTransitioning = true;
        isInvulnerable = true;
        isRushing = false;
        ResetBodyTrailNow();
        if (!string.IsNullOrWhiteSpace(phaseSfx))
        {
            SoundEffectManager.Play(phaseSfx);
        }

        if (useSingleAreaPhaseFlow)
        {
            yield return StartCoroutine(RunSingleAreaIntermission(
                area2IntermissionWaves,
                BossPhase.Phase3,
                false));
            yield break;
        }

        SetGate(gateToArea3, true);
        yield return StartCoroutine(EscapeToAnchor(BossPhase.Phase3));
        yield return StartCoroutine(SpawnWaveSet(area2IntermissionWaves));
        if (waitAllAddsBeforeNextPhase)
        {
            yield return StartCoroutine(WaitUntilNoAddsAlive());
        }

        SetGate(gateToArea3, false);
        if (waitPlayerEnterNextAreaToResumeCombat)
        {
            yield return StartCoroutine(WaitForPlayerInPhaseArea(BossPhase.Phase3));
        }
        currentPhase = BossPhase.Phase3;
        isFreedFromAnchor = true;
        zigzagUnlockedAt = Time.time + Mathf.Max(0f, zigzagActivationAfterRelease);
        isInvulnerable = false;
        isTransitioning = false;
        nextSupportWaveAt = Time.time + Mathf.Max(2f, phase3SupportWaveCadence * 0.6f);
        ResetBodyTrailNow();
    }

    private IEnumerator RunSingleAreaIntermission(List<WaveDefinition> waves, BossPhase nextPhase, bool remainAnchoredAfter)
    {
        Transform nextAnchor = phaseAnchors != null && phaseAnchors.Length > (int)nextPhase
            ? phaseAnchors[(int)nextPhase]
            : transform;
        Vector2 resumePos = nextAnchor != null ? nextAnchor.position : (Vector2)transform.position;

        if (hideBossDuringIntermission)
        {
            if (fakeEscapeBeforeHide)
            {
                yield return StartCoroutine(PlayFakeEscape(resumePos));
            }
            SetBossPresenceInArena(false, resumePos);
        }

        yield return StartCoroutine(SpawnWaveSet(waves));
        if (waitAllAddsBeforeNextPhase)
        {
            yield return StartCoroutine(WaitUntilNoAddsAlive());
        }

        currentPhase = nextPhase;
        isFreedFromAnchor = !remainAnchoredAfter;
        zigzagUnlockedAt = Time.time + Mathf.Max(0f, zigzagActivationAfterRelease);

        if (currentPhase == BossPhase.Phase3)
        {
            nextSupportWaveAt = Time.time + Mathf.Max(2f, phase3SupportWaveCadence * 0.6f);
        }

        SetBossPosition(resumePos);
        ResetBodyTrailNow();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }

        if (hideBossDuringIntermission)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, reappearDelay));
            SetBossPresenceInArena(true, resumePos);
        }

        isInvulnerable = false;
        isTransitioning = false;
    }

    private void SetBossPresenceInArena(bool visible, Vector2 anchorPos)
    {
        if (!visible)
        {
            Vector2 hiddenPos = anchorPos + Vector2.up * Mathf.Max(3f, hiddenYOffset);
            SetBossPosition(hiddenPos);
        }
        else
        {
            SetBossPosition(anchorPos);
        }

        if (allRenderers != null)
        {
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null)
                {
                    allRenderers[i].enabled = visible;
                }
            }
        }

        if (allColliders != null)
        {
            for (int i = 0; i < allColliders.Length; i++)
            {
                if (allColliders[i] != null)
                {
                    allColliders[i].enabled = visible;
                }
            }
        }

        if (rb2d != null)
        {
            rb2d.simulated = visible;
            if (!visible)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }
        }
    }

    private IEnumerator PlayFakeEscape(Vector2 anchorPos)
    {
        float duration = Mathf.Max(0.1f, fakeEscapeDuration);
        float t = 0f;
        Vector2 startPos = transform.position;

        Vector2 baseDir;
        if (player != null)
        {
            Vector2 fromPlayer = (Vector2)transform.position - (Vector2)player.position;
            baseDir = fromPlayer.sqrMagnitude > 0.001f ? fromPlayer.normalized : Vector2.up;
        }
        else
        {
            baseDir = (Vector2)transform.position - anchorPos;
            baseDir = baseDir.sqrMagnitude > 0.001f ? baseDir.normalized : Vector2.up;
        }

        Vector2 side = Random.value < 0.5f
            ? new Vector2(-baseDir.y, baseDir.x)
            : new Vector2(baseDir.y, -baseDir.x);

        Vector2 endPos = startPos + baseDir * Mathf.Max(0.5f, fakeEscapeDistance);
        Vector2 controlPos = Vector2.Lerp(startPos, endPos, 0.5f) + side * Mathf.Max(0f, fakeEscapeCurveStrength);

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            Vector2 a = Vector2.Lerp(startPos, controlPos, u);
            Vector2 b = Vector2.Lerp(controlPos, endPos, u);
            Vector2 curvePos = Vector2.Lerp(a, b, u);
            SetBossPosition(curvePos);
            yield return null;
        }

        SetBossPosition(endPos);

        if (!hideOnlyAfterLeavingCamera)
        {
            yield break;
        }

        float extraTimer = 0f;
        float extraMax = Mathf.Max(0f, maxFakeEscapeExtraTime);
        while (IsInsideMainCameraView(transform.position) && extraTimer < extraMax)
        {
            extraTimer += Time.deltaTime;
            Vector2 stepPos = (Vector2)transform.position + baseDir * Mathf.Max(0.5f, fakeEscapeDistance * 0.7f) * Time.deltaTime;
            SetBossPosition(stepPos);
            yield return null;
        }
    }

    private static bool IsInsideMainCameraView(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        Vector3 viewport = cam.WorldToViewportPoint(worldPos);
        return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }

    private IEnumerator EscapeToAnchor(BossPhase targetPhase)
    {
        Transform targetAnchor = phaseAnchors[(int)targetPhase];
        if (targetAnchor == null)
        {
            yield break;
        }

        float phaseSpeedMultiplier = targetPhase == BossPhase.Phase2
            ? Mathf.Max(1f, phase2EscapeSpeedMultiplier)
            : targetPhase == BossPhase.Phase3
                ? Mathf.Max(1f, phase3EscapeSpeedMultiplier)
                : 1f;

        float timer = 0f;
        float maxDuration = Mathf.Max(1.5f, maxEscapeDuration);
        while (Vector2.Distance(transform.position, targetAnchor.position) > 0.2f && timer < maxDuration)
        {
            Vector2 currentPos = transform.position;
            Vector2 targetPos = targetAnchor.position;
            float remainingDistance = Vector2.Distance(currentPos, targetPos);
            float distanceBoost = 1f + Mathf.Clamp01(remainingDistance / 10f) * 0.85f;
            float speed = Mathf.Max(1f, escapeSpeed * phaseSpeedMultiplier * distanceBoost);
            Vector2 nextPos = Vector2.MoveTowards(currentPos, targetPos, speed * Time.deltaTime);
            SetBossPosition(nextPos);
            timer += Time.deltaTime;
            yield return null;
        }

        SetBossPosition(targetAnchor.position);
        ResetBodyTrailNow();
    }

    private IEnumerator SpawnWaveSet(List<WaveDefinition> waves)
    {
        if (waves == null || waves.Count == 0)
        {
            yield break;
        }

        for (int i = 0; i < waves.Count; i++)
        {
            yield return StartCoroutine(SpawnWave(waves[i]));
            yield return new WaitForSeconds(0.35f);
        }
    }

    private IEnumerator SpawnWave(WaveDefinition wave)
    {
        if (wave == null || wave.enemyCount <= 0)
        {
            yield break;
        }

        for (int i = 0; i < wave.enemyCount; i++)
        {
            if (activeAdds.Count >= maxAliveAdds)
            {
                break;
            }

            SpawnOneAdd(wave);
            yield return new WaitForSeconds(Mathf.Max(0.05f, wave.spawnInterval));
        }
    }

    private IEnumerator SpawnSupportWaveInPhase3()
    {
        if (area3SupportWaves == null || area3SupportWaves.Count == 0)
        {
            yield break;
        }

        int index = Random.Range(0, area3SupportWaves.Count);
        yield return StartCoroutine(SpawnWave(area3SupportWaves[index]));
    }

    private IEnumerator WaitForPlayerInPhaseArea(BossPhase phase)
    {
        while (!isDead)
        {
            ResolvePlayer();
            if (player == null)
            {
                yield return null;
                continue;
            }

            if (IsPlayerInsidePhaseArea(phase))
            {
                yield break;
            }

            yield return null;
        }
    }

    private bool IsPlayerInsidePhaseArea(BossPhase phase)
    {
        if (player == null)
        {
            return false;
        }

        int idx = (int)phase;
        Collider2D boundary = phaseBoundaries != null && phaseBoundaries.Length > idx
            ? phaseBoundaries[idx]
            : null;
        if (boundary != null)
        {
            return boundary.OverlapPoint(player.position);
        }

        Transform anchor = phaseAnchors != null && phaseAnchors.Length > idx
            ? phaseAnchors[idx]
            : null;
        if (anchor == null)
        {
            return false;
        }

        return Vector2.Distance(player.position, anchor.position) <= Mathf.Max(2f, areaWanderRadius);
    }

    private void SpawnOneAdd(WaveDefinition wave)
    {
        if (wave.enemyPrefabs == null || wave.enemyPrefabs.Length == 0)
        {
            return;
        }

        List<Transform> points = GetWaveSpawnPoints(wave);
        if (points.Count == 0)
        {
            return;
        }

        GameObject prefab = wave.enemyPrefabs[Random.Range(0, wave.enemyPrefabs.Length)];
        Transform point = points[Random.Range(0, points.Count)];
        if (prefab == null || point == null)
        {
            return;
        }

        GameObject add = Instantiate(prefab, point.position, Quaternion.identity);
        Enemy enemy = add.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetPersistenceEnabled(false);
        }

        activeAdds.Add(add);
    }

    private List<Transform> GetWaveSpawnPoints(WaveDefinition wave)
    {
        List<Transform> result = new List<Transform>(8);

        if (wave.spawnPoints != null)
        {
            for (int i = 0; i < wave.spawnPoints.Length; i++)
            {
                if (wave.spawnPoints[i] != null)
                {
                    result.Add(wave.spawnPoints[i]);
                }
            }
        }

        if (result.Count > 0)
        {
            return result;
        }

        if (wave.spawnRoot != null)
        {
            for (int i = 0; i < wave.spawnRoot.childCount; i++)
            {
                Transform child = wave.spawnRoot.GetChild(i);
                if (child != null)
                {
                    result.Add(child);
                }
            }
        }

        if (result.Count > 0)
        {
            return result;
        }

        if (defaultSpawnRoot != null)
        {
            for (int i = 0; i < defaultSpawnRoot.childCount; i++)
            {
                Transform child = defaultSpawnRoot.GetChild(i);
                if (child != null)
                {
                    result.Add(child);
                }
            }
        }

        if (result.Count == 0)
        {
            Debug.LogWarning($"IceDragonBoss: wave '{wave.waveName}' senza spawn points validi. Usa spawnPoints, spawnRoot o defaultSpawnRoot (BossSpawn).");
        }

        return result;
    }

    private IEnumerator WaitUntilNoAddsAlive()
    {
        while (true)
        {
            CleanupDeadAdds();
            if (activeAdds.Count == 0)
            {
                break;
            }
            yield return null;
        }
    }

    private void CleanupDeadAdds()
    {
        for (int i = activeAdds.Count - 1; i >= 0; i--)
        {
            if (activeAdds[i] == null)
            {
                activeAdds.RemoveAt(i);
            }
        }
    }

    private void SetGate(GameObject gate, bool closed)
    {
        if (gate == null)
        {
            return;
        }

        gate.SetActive(closed);
    }

    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (isDead || isInvulnerable)
        {
            return;
        }

        currentHealth -= damage;
        if (!string.IsNullOrWhiteSpace(hurtSfx))
        {
            SoundEffectManager.Play(hurtSfx);
        }

        StartCoroutine(HitFlash());

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    private IEnumerator HitFlash()
    {
        if (allRenderers == null || allRenderers.Length == 0)
        {
            yield break;
        }

        Color[] baseColors = new Color[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
        {
            baseColors[i] = allRenderers[i].color;
            allRenderers[i].color = Color.white;
        }

        yield return new WaitForSeconds(0.08f);

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null)
            {
                allRenderers[i].color = baseColors[i];
            }
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isInvulnerable = true;
        isRushing = false;

        if (!string.IsNullOrWhiteSpace(deathSfx))
        {
            SoundEffectManager.Play(deathSfx);
        }

        StopAllCoroutines();
        StartCoroutine(DefeatRoutine());
    }

    private IEnumerator DefeatRoutine()
    {
        yield return new WaitForSeconds(0.45f);

        BossArenaController arena = FindObjectOfType<BossArenaController>();
        if (arena != null)
        {
            arena.OnBossDefeated();
        }

        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead || Time.time < nextContactDamageAt)
        {
            return;
        }

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null)
        {
            return;
        }

        Vector2 dir = (other.transform.position - transform.position).normalized;
        health.TakeDamage(contactDamage, dir);
        nextContactDamageAt = Time.time + contactDamageCooldown;
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (isDead || Time.time < nextContactDamageAt)
        {
            return;
        }

        PlayerHealth health = other.collider != null ? other.collider.GetComponent<PlayerHealth>() : null;
        if (health == null)
        {
            return;
        }

        Vector2 dir = (other.transform.position - transform.position).normalized;
        health.TakeDamage(contactDamage, dir);
        nextContactDamageAt = Time.time + contactDamageCooldown;
    }

    private void ResolvePlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void CachePartDefaults()
    {
        headBaseLocalPos = head != null ? head.localPosition : Vector3.zero;
        body1BaseLocalPos = body1 != null ? body1.localPosition : Vector3.zero;
        body2BaseLocalPos = body2 != null ? body2.localPosition : Vector3.zero;
        bodyEndBaseLocalPos = bodyEnd != null ? bodyEnd.localPosition : Vector3.zero;
        wingLBaseRot = wingL != null ? wingL.localRotation : Quaternion.identity;
        wingRBaseRot = wingR != null ? wingR.localRotation : Quaternion.identity;
    }

    private void AnimateMultipartVisual()
    {
        float rootZ = transform.position.z;
        float t = Time.time;
        float flap = Mathf.Sin(t * wingFlapSpeed) * wingFlapAngle * Mathf.Lerp(0.5f, 1f, currentMoveSpeed01);

        if (wingL != null)
        {
            wingL.localRotation = wingLBaseRot * Quaternion.Euler(0f, 0f, flap * 0.2f);
        }
        if (wingR != null)
        {
            wingR.localRotation = wingRBaseRot * Quaternion.Euler(0f, 0f, -flap * 0.2f);
        }
        if (head != null)
        {
            head.localPosition = headBaseLocalPos;
            Vector3 hp = head.position;
            head.position = new Vector3(hp.x, hp.y, rootZ);
        }

        if (segmentChain != null)
        {
            Vector3 prevPos = head != null ? head.position : transform.position;
            for (int i = 0; i < segmentChain.Length; i++)
            {
                Transform segment = segmentChain[i];
                if (segment == null)
                {
                    continue;
                }

                float phase = (t * segmentWaveSpeed) - (i * segmentWavePhaseOffset);
                float wave = rigidWormChain ? 0f : Mathf.Sin(phase) * segmentWaveAmplitude;
                Vector2 tangent = (Vector2)(prevPos - segment.position).normalized;
                if (tangent.sqrMagnitude < 0.0001f)
                {
                    tangent = smoothedMoveDir.sqrMagnitude > 0.0001f ? smoothedMoveDir.normalized : Vector2.right;
                }
                Vector2 normal = new Vector2(-tangent.y, tangent.x);

                Vector3 targetWorldPos = GetTrailPointForSegment(i) + (Vector3)(normal * wave);
                targetWorldPos = ConstrainSegmentDistance(prevPos, targetWorldPos);

                if (rigidWormChain)
                {
                    segment.position = targetWorldPos;
                }
                else
                {
                    float followT = 1f - Mathf.Exp(-Mathf.Max(1f, wormFollowSharpness) * Time.deltaTime);
                    segment.position = Vector3.Lerp(segment.position, targetWorldPos, followT);
                }

                // Keep all body parts on the same Z plane as the boss root.
                Vector3 sp = segment.position;
                segment.position = new Vector3(sp.x, sp.y, rootZ);

                float maxDetach = Mathf.Max(segmentSpacing * segmentMaxGapMultiplier * 2f, hardSnapDistance);
                if (Vector3.Distance(prevPos, segment.position) > maxDetach)
                {
                    segment.position = prevPos + (targetWorldPos - prevPos).normalized * (segmentSpacing * segmentMaxGapMultiplier);
                    segmentSmoothDampVelocity[i] = Vector3.zero;
                }

                Vector2 lookDir = (Vector2)(prevPos - segment.position);
                if (lookDir.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg + segmentFacingOffset;
                    Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);
                    segment.rotation = Quaternion.Slerp(segment.rotation, targetRot, Time.deltaTime * Mathf.Max(1f, segmentTurnSpeed));
                }

                prevPos = segment.position;
            }
        }

        ApplyTailTipLock(rootZ);
    }

    private void ApplyTailTipLock(float rootZ)
    {
        if (!ShouldLockTailTip())
        {
            return;
        }

        Transform tip = tail != null ? tail : (segmentChain != null && segmentChain.Length > 0 ? segmentChain[segmentChain.Length - 1] : null);
        if (tip == null)
        {
            return;
        }

        Vector3 target = tailTipAnchor != null ? tailTipAnchor.position : phaseAnchors[(int)currentPhase].position;
        target.z = rootZ;
        float snap = 1f - Mathf.Exp(-Mathf.Max(1f, tailTipSnapSpeed) * Time.deltaTime);
        tip.position = Vector3.Lerp(tip.position, target, snap);
    }

    private bool ShouldLockTailTip()
    {
        if (!lockTailTipWhenAnchored || isDead || isTransitioning)
        {
            return false;
        }

        if (currentPhase == BossPhase.Phase1)
        {
            return lockTailTipInPhase1;
        }

        if (currentPhase == BossPhase.Phase2)
        {
            // In single-area flow phase2 can remain anchored by design.
            return lockTailTipInPhase2 && !isFreedFromAnchor;
        }

        return false;
    }

    private Vector3 ConstrainSegmentDistance(Vector3 prevPos, Vector3 desiredPos)
    {
        Vector3 delta = desiredPos - prevPos;
        float len = delta.magnitude;
        if (len < 0.0001f)
        {
            return prevPos + (Vector3)(-smoothedMoveDir.normalized * (segmentSpacing * segmentMinGapMultiplier));
        }

        float minGap = segmentSpacing * segmentMinGapMultiplier;
        float maxGap = segmentSpacing * segmentMaxGapMultiplier;
        float clamped = Mathf.Clamp(len, minGap, maxGap);
        return prevPos + delta.normalized * clamped;
    }

    private void BuildSegmentChain()
    {
        List<Transform> chain = new List<Transform>(10);
        if (generatedSegments.Count > 0)
        {
            chain.AddRange(generatedSegments);
            if (bodyEnd != null) chain.Add(bodyEnd);
            if (tail != null) chain.Add(tail);
        }
        else
        {
            if (body1 != null) chain.Add(body1);
            if (body2 != null) chain.Add(body2);
            if (bodyEnd != null) chain.Add(bodyEnd);
            if (tail != null) chain.Add(tail);
        }

        segmentChain = chain.ToArray();
        segmentBaseLocalPos = new Vector3[segmentChain.Length];
        segmentSmoothDampVelocity = new Vector3[segmentChain.Length];

        for (int i = 0; i < segmentChain.Length; i++)
        {
            segmentBaseLocalPos[i] = segmentChain[i].localPosition;
        }
    }

    private void UpdateMoveDirection()
    {
        Vector2 frameVelocity = ((Vector2)transform.position - (Vector2)lastBossPosition) / Mathf.Max(0.0001f, Time.deltaTime);
        if (frameVelocity.sqrMagnitude > 0.0001f)
        {
            Vector2 dir = frameVelocity.normalized;
            smoothedMoveDir = Vector2.Lerp(smoothedMoveDir, dir, Time.deltaTime * Mathf.Max(1f, turnSmoothing)).normalized;
        }

        currentMoveSpeed01 = Mathf.Clamp01(frameVelocity.magnitude / Mathf.Max(0.01f, moveSpeed));
        PushTrailPointIfNeeded();

        lastBossPosition = transform.position;
    }

    private void AutoAssignMissingParts()
    {
        if (head == null) head = FindChildRecursive(transform, "Head");
        if (wingL == null) wingL = FindChildRecursive(transform, "Wing_L");
        if (wingR == null) wingR = FindChildRecursive(transform, "Wing_R");
        if (body1 == null) body1 = FindChildRecursive(transform, "Body1");
        if (body2 == null) body2 = FindChildRecursive(transform, "Body2");
        if (bodyEnd == null) bodyEnd = FindChildRecursive(transform, "BodyEnd");
        if (tail == null) tail = FindChildRecursive(transform, "Tail");
    }

    private void BuildGeneratedBodySegments()
    {
        generatedSegments.Clear();
        if (!autoGenerateBodySegments || generatedBodySegmentCount <= 0)
        {
            return;
        }

        Transform template = bodySegmentTemplate != null ? bodySegmentTemplate : body1;
        if (template == null)
        {
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c.name.StartsWith("AutoBody_"))
            {
                Destroy(c.gameObject);
            }
        }

        for (int i = 0; i < generatedBodySegmentCount; i++)
        {
            Transform seg = Instantiate(template, transform);
            seg.name = $"AutoBody_{i + 1}";
            seg.localPosition = template.localPosition + new Vector3(0f, -(i + 1) * Mathf.Max(0.08f, segmentSpacing), 0f);
            seg.localRotation = template.localRotation;
            generatedSegments.Add(seg);
        }

        if (hideManualBodyWhenGenerated)
        {
            if (body1 != null) body1.gameObject.SetActive(false);
            if (body2 != null) body2.gameObject.SetActive(false);
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void InitializeMoveTrail()
    {
        moveTrail.Clear();
        int minPoints = GetRequiredTrailPointCount();
        for (int i = 0; i < minPoints; i++)
        {
            moveTrail.Add(transform.position);
        }
    }

    private void PushTrailPointIfNeeded()
    {
        if (moveTrail.Count == 0)
        {
            InitializeMoveTrail();
        }

        Vector3 currentPos = transform.position;
        float minDist = Mathf.Max(0.005f, trailPointMinDistance);
        float distFromHead = Vector3.Distance(moveTrail[0], currentPos);
        if (distFromHead >= minDist)
        {
            moveTrail.Insert(0, currentPos);
        }
        else
        {
            moveTrail[0] = currentPos;
        }

        int maxCount = GetRequiredTrailPointCount();
        if (moveTrail.Count > maxCount)
        {
            moveTrail.RemoveRange(maxCount, moveTrail.Count - maxCount);
        }
    }

    private void ResetBodyTrailNow()
    {
        InitializeMoveTrail();
        if (segmentChain == null || segmentChain.Length == 0)
        {
            return;
        }

        Vector3 prev = head != null ? head.position : transform.position;
        Vector2 backDir = smoothedMoveDir.sqrMagnitude > 0.001f ? -smoothedMoveDir.normalized : Vector2.down;

        for (int i = 0; i < segmentChain.Length; i++)
        {
            Transform segment = segmentChain[i];
            if (segment == null)
            {
                continue;
            }

            float dist = Mathf.Max(0.04f, segmentSpacing * trailSpacingMultiplier);
            Vector3 p = prev + (Vector3)(backDir * dist);
            segment.position = p;
            segmentSmoothDampVelocity[i] = Vector3.zero;
            prev = p;
        }

        SeedTrailFromCurrentBodyPose();
    }

    private Vector3 GetTrailPointForSegment(int segmentIndex)
    {
        if (moveTrail.Count == 0)
        {
            return transform.position;
        }

        float spacing = Mathf.Max(0.04f, segmentSpacing * trailSpacingMultiplier);
        float targetDistance = Mathf.Max(0.01f, spacing * (segmentIndex + 1));
        return GetTrailPointAtDistance(targetDistance);
    }

    private int GetRequiredTrailPointCount()
    {
        int segmentCount = segmentChain != null && segmentChain.Length > 0 ? segmentChain.Length : 3;
        float spacing = Mathf.Max(0.04f, segmentSpacing * trailSpacingMultiplier);
        float minDist = Mathf.Max(0.005f, trailPointMinDistance);
        float requiredDistance = spacing * (segmentCount + 4);
        int pointsFromDistance = Mathf.CeilToInt(requiredDistance / minDist) + 10;
        int pointsFromFrames = Mathf.Max(1, trailFramesPerSegment) * (segmentCount + 2);
        return Mathf.Max(40, Mathf.Max(pointsFromDistance, pointsFromFrames));
    }

    private Vector3 GetTrailPointAtDistance(float distanceFromHead)
    {
        if (moveTrail.Count == 0)
        {
            return transform.position;
        }

        if (moveTrail.Count == 1 || distanceFromHead <= 0f)
        {
            return moveTrail[0];
        }

        float remaining = distanceFromHead;
        Vector3 previous = moveTrail[0];

        for (int i = 1; i < moveTrail.Count; i++)
        {
            Vector3 current = moveTrail[i];
            float segmentLen = Vector3.Distance(previous, current);
            if (segmentLen >= remaining)
            {
                float t = segmentLen > 0.0001f ? remaining / segmentLen : 0f;
                return Vector3.Lerp(previous, current, t);
            }

            remaining -= segmentLen;
            previous = current;
        }

        return moveTrail[moveTrail.Count - 1];
    }

    private void SeedTrailFromCurrentBodyPose()
    {
        moveTrail.Clear();

        Vector3 root = head != null ? head.position : transform.position;
        moveTrail.Add(root);

        if (segmentChain != null)
        {
            for (int i = 0; i < segmentChain.Length; i++)
            {
                Transform segment = segmentChain[i];
                if (segment != null)
                {
                    moveTrail.Add(segment.position);
                }
            }
        }

        Vector3 tailPoint = moveTrail[moveTrail.Count - 1];
        int minPoints = GetRequiredTrailPointCount();
        while (moveTrail.Count < minPoints)
        {
            moveTrail.Add(tailPoint);
        }
    }

    public float GetHealthPercent()
    {
        return currentHealth / Mathf.Max(1f, maxHealth);
    }

    public string GetBossDisplayName()
    {
        return bossDisplayName;
    }
}
