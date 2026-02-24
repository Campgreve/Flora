using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BossArenaController : MonoBehaviour
{
    [Header("Boss Setup")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private string bossId = "boss_tutorial";
    [SerializeField] private bool preferExistingBossInScene = true;
    [SerializeField] private bool disableBossAtStartUntilTriggered = true;
    private GameObject bossInstance;
    
    [Header("Arena")]
    [SerializeField] private GameObject barrier;
    [SerializeField] private GameObject bossHealthBar;
    
    [Header("Camera")]
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private Transform cameraIntroPivot;
    [SerializeField] private float introCameraZoom = 5f;
    [SerializeField] private float arenaFightCameraZoom = 7.5f;
    [SerializeField] private bool useFightCameraVerticalOffset = true;
    [SerializeField] private float fightCameraYOffset = 1.4f;
    [SerializeField] private float fightZoomTransitionDuration = 0.45f;
    [SerializeField] private float restoreZoomTransitionDuration = 0.55f;
    private Camera mainCamera;
    private float originalCameraSize;
    private Transform originalCameraTarget;
    private Vector3 originalCameraFollowOffset;
    
    [Header("Intro Timing")]
    [SerializeField] private float introDuration = 5f;
    [SerializeField] private string bossIntroText = "TRASH PANDA KING";
    [SerializeField] private GameObject bossNameUI;
    [SerializeField] private string bossNameFontResource = "Fonts & Materials/antiquity-print SDF";
    [SerializeField] private Color bossNameColor = new Color(0.78f, 0.14f, 0.14f, 1f);
    
    [Header("Audio")]
    [SerializeField] private string introSoundName = "BossIntro";
    
    [Header("Effects")]
    [SerializeField] private GameObject introLightning;
    [SerializeField] private GameObject dustEffect;

    [Header("Intro Boss Motion")]
    [SerializeField] private bool animateBossDuringIntro = true;
    [SerializeField] private float introBossDriftAmplitudeY = 0.35f;
    [SerializeField] private float introBossDriftAmplitudeX = 0.22f;
    [SerializeField] private float introBossDriftSpeedY = 1.8f;
    [SerializeField] private float introBossDriftSpeedX = 1.2f;
    
    private bool hasBeenTriggered = false;
    private bool bossDefeated = false;
    private IBossHealthSource activeBossSource;
    private MonoBehaviour activeBossBehaviour;
    private TextMeshProUGUI bossNameTmp;
    private Text bossNameLegacy;
    private bool keepFightCameraOffsetActive;
    private Coroutine introBossMotionRoutine;
    
    // ✅ FLAG PUBBLICO PER BLOCCARE LE FRECCE
    public static bool isInBossIntro = false;
    
    
    void Start()
    {
        mainCamera = Camera.main;

        if (GameProgress.Instance.HasDefeatedBoss(bossId))
        {
            bossDefeated = true;
            hasBeenTriggered = true;
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null) trigger.enabled = false;
        }
        
        if (barrier != null)
            barrier.SetActive(false);
        
        if (bossHealthBar != null)
            bossHealthBar.SetActive(false);
        
        if (bossNameUI != null)
            bossNameUI.SetActive(false);

        CacheBossNameLabel();
        
        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CameraFollow>();
        
        if (cameraFollow != null)
            originalCameraFollowOffset = cameraFollow.offset;
        
        if (cameraShake == null && mainCamera != null)
            cameraShake = mainCamera.GetComponent<CameraShake>();
        
        if (mainCamera != null)
            originalCameraSize = mainCamera.orthographicSize;

        if (!bossDefeated && preferExistingBossInScene)
        {
            bossInstance = FindExistingBossInstanceInScene();
            if (disableBossAtStartUntilTriggered && bossInstance != null)
            {
                bossInstance.SetActive(false);
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasBeenTriggered && !bossDefeated)
        {
            hasBeenTriggered = true;
            StartCoroutine(BossIntroSequence());
        }
    }
    
    IEnumerator BossIntroSequence()
    {
        // ✅ BLOCCA LE FRECCE ALL'INIZIO
        isInBossIntro = true;
        
        // 1. Blocca il player
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        Rigidbody2D playerRb = null;
        
        if (player != null)
        {
            player.enabled = false;
            playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
                playerRb.linearVelocity = Vector2.zero;
        }
        
        // 2. Chiudi la barriera
        if (barrier != null)
        {
            barrier.SetActive(true);
            SoundEffectManager.Play("DoorSlam", true);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // 3. Spawna il boss (invisibile)
        if (bossInstance == null && preferExistingBossInScene)
        {
            bossInstance = FindExistingBossInstanceInScene();
        }

        if (bossInstance == null && bossPrefab != null && bossSpawnPoint != null)
        {
            bossInstance = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
        }

        if (bossInstance != null)
        {
            if (!bossInstance.activeSelf)
            {
                bossInstance.SetActive(true);
            }

            if (bossSpawnPoint != null)
            {
                bossInstance.transform.position = bossSpawnPoint.position;
            }

            SpriteRenderer bossSprite = bossInstance.GetComponent<SpriteRenderer>();
            if (bossSprite != null)
            {
                Color c = bossSprite.color;
                c.a = 0f;
                bossSprite.color = c;
            }

            FindBossComponents(bossInstance, out activeBossSource, out activeBossBehaviour);
            if (activeBossBehaviour != null)
            {
                activeBossBehaviour.enabled = false;

                // Collega la health bar
                if (bossHealthBar != null)
                {
                    BossHealthBar healthBarScript = bossHealthBar.GetComponent<BossHealthBar>();
                    if (healthBarScript != null && activeBossSource != null)
                    {
                        healthBarScript.Initialize(activeBossSource);
                    }
                }
            }
        }
        
        // 4. Pausa il CameraFollow
        if (cameraFollow != null)
        {
            cameraFollow.pauseFollow = true;
        }

        // Muovi la camera manualmente
        if (mainCamera != null && cameraIntroPivot != null)
        {
            yield return StartCoroutine(MoveCameraToTarget(cameraIntroPivot.position, 0.8f));
            yield return StartCoroutine(ZoomCamera(introCameraZoom, 0.5f));
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // 5. Effetto spawn drammatico
        if (introLightning != null && bossSpawnPoint != null)
        {
            GameObject lightning = Instantiate(introLightning, bossSpawnPoint.position, Quaternion.identity);
            Destroy(lightning, 2f);
        }
        
        SoundEffectManager.Play(introSoundName);
        
        if (cameraShake != null)
        {
            cameraShake.Shake(0.4f, 0.3f);
        }
        
        yield return new WaitForSeconds(0.4f);
        
        // 6. Fade in del boss
        if (bossInstance != null)
        {
            SpriteRenderer bossSprite = bossInstance.GetComponent<SpriteRenderer>();
            if (bossSprite != null)
            {
                yield return StartCoroutine(FadeInBoss(bossSprite, 0.5f));
            }
        }

        if (bossInstance != null && animateBossDuringIntro)
        {
            StartIntroBossMotion();
        }
        
        yield return new WaitForSeconds(0.2f);
        
        // 7. Animazione "Charge" (urlo/roar del boss)
        if (bossInstance != null)
        {
            Animator bossAnim = bossInstance.GetComponent<Animator>();
            if (bossAnim != null)
            {
                SafePlayBossIntroAnimation(bossAnim);
            }
            
            if (dustEffect != null)
            {
                GameObject dust = Instantiate(dustEffect, bossSpawnPoint.position, Quaternion.identity);
                Destroy(dust, 2f);
            }
            
            if (cameraShake != null)
            {
                cameraShake.Shake(0.6f, 0.5f);
            }
        }
        
        yield return new WaitForSeconds(1.2f);
        
        // 8. Mostra nome boss
        ApplyBossNameStyle();
        SetBossNameText(activeBossSource != null ? activeBossSource.GetBossDisplayName() : bossIntroText);
        if (bossNameUI != null)
        {
            bossNameUI.SetActive(true);
        }
        
        yield return new WaitForSeconds(1.8f);
        
        // 9. Nascondi nome
        if (bossNameUI != null)
        {
            bossNameUI.SetActive(false);
        }
        
        // 10. Zoom out e torna al player
        if (mainCamera != null)
        {
            float targetFightZoom = Mathf.Max(originalCameraSize, arenaFightCameraZoom);
            yield return StartCoroutine(ZoomCamera(targetFightZoom, fightZoomTransitionDuration));
        }
        
        if (cameraFollow != null)
        {
            cameraFollow.pauseFollow = false;
        }

        ApplyFightCameraOffset();
        keepFightCameraOffsetActive = true;
        
        yield return new WaitForSeconds(0.3f);
        
        // 11. Musica del boss
        MusicManager.Instance.PlayBoss();
        
        // 12. Mostra health bar
        if (bossHealthBar != null)
        {
            bossHealthBar.SetActive(true);
        }
        
        // 13. Abilita il boss
        StopIntroBossMotion(true);
        if (activeBossBehaviour != null)
        {
            activeBossBehaviour.enabled = true;
        }
        
        // 14. Riabilita il player
        if (player != null)
        {
            player.enabled = true;
        }
        
        // ✅ SBLOCCA LE FRECCE ALLA FINE
        isInBossIntro = false;
        
        Debug.Log("🦝 BOSS FIGHT STARTED!");
    }
    
    IEnumerator ZoomCamera(float targetSize, float duration)
    {
        float elapsed = 0f;
        float startSize = mainCamera.orthographicSize;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }
        
        mainCamera.orthographicSize = targetSize;
    }

    IEnumerator MoveCameraToTarget(Vector3 targetPosition, float duration)
    {
        if (mainCamera == null) yield break;
        
        Vector3 startPosition = mainCamera.transform.position;
        targetPosition.z = startPosition.z;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        mainCamera.transform.position = targetPosition;
    }
    
    IEnumerator FadeInBoss(SpriteRenderer sprite, float duration)
    {
        float elapsed = 0f;
        Color c = sprite.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            sprite.color = c;
            yield return null;
        }
        
        c.a = 1f;
        sprite.color = c;
    }
    
    public void OnBossDefeated()
    {
        bossDefeated = true;
        StartCoroutine(BossDefeatSequence());
    }
    
    IEnumerator BossDefeatSequence()
    {
        yield return new WaitForSeconds(2f);

        keepFightCameraOffsetActive = false;
        RestoreFightCameraOffset();

        if (mainCamera != null)
        {
            yield return StartCoroutine(ZoomCamera(originalCameraSize, restoreZoomTransitionDuration));
        }
        
        MusicManager.Instance.PlayChill();
        
        if (bossHealthBar != null)
        {
            bossHealthBar.SetActive(false);
        }
        
        yield return new WaitForSeconds(1f);
        
        if (barrier != null)
        {
            barrier.SetActive(false);
            SoundEffectManager.Play("DoorOpen", true);
        }
        
        Debug.Log("Boss defeated! Arena unlocked!");
    }
    
    void OnDrawGizmos()
    {
        if (bossSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bossSpawnPoint.position, 1f);
            Gizmos.DrawLine(bossSpawnPoint.position, bossSpawnPoint.position + Vector3.up * 3f);
        }
        
        if (cameraIntroPivot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(cameraIntroPivot.position, 0.5f);
        }
    }

    private void FindBossComponents(GameObject bossObj, out IBossHealthSource source, out MonoBehaviour behaviour)
    {
        source = null;
        behaviour = null;
        if (bossObj == null)
        {
            return;
        }

        MonoBehaviour[] all = bossObj.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour comp = all[i];
            if (comp is IBossHealthSource bossSource)
            {
                source = bossSource;
                behaviour = comp;
                return;
            }
        }
    }

    private void CacheBossNameLabel()
    {
        if (bossNameUI == null)
        {
            CreateRuntimeBossNameUi();
        }

        if (bossNameUI == null)
        {
            return;
        }

        bossNameTmp = bossNameUI.GetComponentInChildren<TextMeshProUGUI>(true);
        bossNameLegacy = bossNameUI.GetComponentInChildren<Text>(true);
    }

    private void CreateRuntimeBossNameUi()
    {
        GameObject canvasObj = new GameObject("BossNameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 14000;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject labelObj = new GameObject("BossNameLabel", typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1400f, 220f);
        rect.anchoredPosition = new Vector2(0f, 120f);

        bossNameUI = canvasObj;
        bossNameUI.SetActive(false);
    }

    private void ApplyBossNameStyle()
    {
        if (bossNameTmp != null)
        {
            bossNameTmp.alignment = TextAlignmentOptions.Center;
            bossNameTmp.color = bossNameColor;
            bossNameTmp.fontSize = 128f;
            bossNameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            TMP_FontAsset antiquity = Resources.Load<TMP_FontAsset>(bossNameFontResource);
            if (antiquity != null)
            {
                bossNameTmp.font = antiquity;
            }
            return;
        }

        if (bossNameLegacy != null)
        {
            bossNameLegacy.alignment = TextAnchor.MiddleCenter;
            bossNameLegacy.color = bossNameColor;
            bossNameLegacy.fontSize = 96;
        }
    }

    private void SetBossNameText(string text)
    {
        string finalText = string.IsNullOrWhiteSpace(text) ? bossIntroText : text;
        if (bossNameTmp != null)
        {
            bossNameTmp.text = finalText;
        }
        if (bossNameLegacy != null)
        {
            bossNameLegacy.text = finalText;
        }
    }

    private void SafePlayBossIntroAnimation(Animator bossAnim)
    {
        if (bossAnim == null)
        {
            return;
        }

        if (TryPlayStateIfExists(bossAnim, "Charge"))
        {
            return;
        }

        if (TryPlayStateIfExists(bossAnim, "Idle"))
        {
            return;
        }

        if (bossAnim.parameters != null)
        {
            for (int i = 0; i < bossAnim.parameters.Length; i++)
            {
                AnimatorControllerParameter p = bossAnim.parameters[i];
                if (p.type == AnimatorControllerParameterType.Trigger &&
                    string.Equals(p.name, "Charge", System.StringComparison.OrdinalIgnoreCase))
                {
                    bossAnim.SetTrigger(p.name);
                    return;
                }
            }
        }
    }

    private bool TryPlayStateIfExists(Animator anim, string stateName)
    {
        if (anim == null || string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        int hash = Animator.StringToHash(stateName);
        for (int layer = 0; layer < anim.layerCount; layer++)
        {
            if (anim.HasState(layer, hash))
            {
                anim.Play(stateName, layer, 0f);
                return true;
            }
        }

        return false;
    }

    private GameObject FindExistingBossInstanceInScene()
    {
        if (bossPrefab == null)
        {
            return null;
        }

        string prefabName = NormalizePrefabName(bossPrefab.name);
        MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>(true);
        GameObject best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour comp = all[i];
            if (!(comp is IBossHealthSource))
            {
                continue;
            }

            GameObject go = comp.gameObject;
            if (go == null)
            {
                continue;
            }

            string sceneBossName = NormalizePrefabName(go.name);
            if (!sceneBossName.StartsWith(prefabName, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            float d = bossSpawnPoint != null
                ? Vector2.Distance(go.transform.position, bossSpawnPoint.position)
                : 0f;
            if (d < bestDist)
            {
                bestDist = d;
                best = go;
            }
        }

        return best;
    }

    private static string NormalizePrefabName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        return rawName.Replace("(Clone)", "").Trim();
    }

    private void ApplyFightCameraOffset()
    {
        if (!useFightCameraVerticalOffset || cameraFollow == null)
        {
            return;
        }

        Vector3 offset = originalCameraFollowOffset;
        offset.y += fightCameraYOffset;
        cameraFollow.offset = offset;
    }

    private void RestoreFightCameraOffset()
    {
        if (cameraFollow == null)
        {
            return;
        }

        cameraFollow.offset = originalCameraFollowOffset;
    }

    private void StartIntroBossMotion()
    {
        StopIntroBossMotion(false);
        if (bossInstance == null)
        {
            return;
        }

        introBossMotionRoutine = StartCoroutine(IntroBossMotionLoop());
    }

    private void StopIntroBossMotion(bool snapToSpawn)
    {
        if (introBossMotionRoutine != null)
        {
            StopCoroutine(introBossMotionRoutine);
            introBossMotionRoutine = null;
        }

        if (!snapToSpawn || bossInstance == null || bossSpawnPoint == null)
        {
            return;
        }

        Vector3 p = bossSpawnPoint.position;
        p.z = bossInstance.transform.position.z;
        bossInstance.transform.position = p;
    }

    private IEnumerator IntroBossMotionLoop()
    {
        if (bossInstance == null)
        {
            yield break;
        }

        Vector3 basePos = bossInstance.transform.position;
        float seed = Random.Range(0f, 999f);

        while (bossInstance != null && !bossDefeated)
        {
            float t = Time.time + seed;
            Vector3 p = basePos;
            p.x += Mathf.Sin(t * Mathf.Max(0.1f, introBossDriftSpeedX)) * Mathf.Max(0f, introBossDriftAmplitudeX);
            p.y += Mathf.Sin(t * Mathf.Max(0.1f, introBossDriftSpeedY)) * Mathf.Max(0f, introBossDriftAmplitudeY);
            p.z = bossInstance.transform.position.z;
            bossInstance.transform.position = p;
            yield return null;
        }
    }

    private void LateUpdate()
    {
        if (!keepFightCameraOffsetActive || bossDefeated || cameraFollow == null || !useFightCameraVerticalOffset)
        {
            return;
        }

        Vector3 desired = originalCameraFollowOffset;
        desired.y += fightCameraYOffset;
        if ((cameraFollow.offset - desired).sqrMagnitude > 0.0001f)
        {
            cameraFollow.offset = desired;
        }
    }
}
