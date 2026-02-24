using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public enum DamageSource
    {
        Enemy,
        Projectile
    }

    [Header("Health")]
    private float health = 0f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Slider healthSlider;
    
    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1f;
    [SerializeField] private int invincibilityFlashCount = 5;
    private bool isInvincible = false;
    
    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 8f;
    
    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Image damageOverlay;
    [SerializeField] private float damageFlashDuration = 0.2f;
    
    [Header("Camera Shake")]
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private float deathScreenDelay = 0.7f;

    [Header("Fire Debuff")]
    [SerializeField] private float fireTickInterval = 0.5f;
    [SerializeField] private float fireTintMaxAlpha = 0.16f;
    [SerializeField] private Color fireTintColor = new Color(1f, 0.35f, 0.12f, 0f);
    [SerializeField] private Color fireDebuffTextColor = new Color(1f, 0.75f, 0.35f, 1f);

    
    private Rigidbody2D rb;
    private Color originalColor;
    private CameraShake cameraShake;
    private bool isDead = false;
    private PlayerMovement playerMovement;
    private Coroutine fireDebuffRoutine;
    private float fireTimeRemaining = 0f;
    private float fireDamagePerSecond = 0f;
    private Canvas fireDebuffCanvas;
    private Text fireDebuffText;
    private Image fireDebuffTint;

    private void Start()
    {
        health = maxHealth;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = health;
        
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        
        cameraShake = Camera.main.GetComponent<CameraShake>();
        
        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = 0f;
            damageOverlay.color = c;
        }
    }

    private void Update()
    {
        UpdateFireVisual();
    }

    public void UpdateHealth(float mod)
    {
        if (isDead) return;
        if (mod < 0 && isInvincible) return;
        
        health += mod;
        
        if (health > maxHealth)
        {
            health = maxHealth;
        }
        else if (health <= 0f)
        {
            health = 0f;
            healthSlider.value = health;
            Die();
        }
        else if (mod < 0)
        {
            Vector2 hitDir = Vector2.down;
            StartCoroutine(DamageResponse(hitDir));
        }
    }

    // ✅ NUOVO METODO: Aumenta la salute massima
    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        healthSlider.maxValue = maxHealth;
        
        Debug.Log($"💚 Max Health aumentata a {maxHealth}");
    }

    public void TakeDamage(float damage, Vector2 hitDirection, DamageSource source = DamageSource.Enemy)
    {
        if (isDead) return;
        if (isInvincible) return;
        if (source == DamageSource.Enemy && playerMovement != null && playerMovement.IsDashing) return;
        
        health -= damage;
        
        SoundEffectManager.Play("PlayerHitted");

        if (health <= 0f)
        {
            health = 0f;
            healthSlider.value = health;
            Die();
        }
        else
        {
            StartCoroutine(DamageResponse(hitDirection));
        }
    }

    IEnumerator DamageResponse(Vector2 hitDirection)
    {
        isInvincible = true;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(hitDirection * knockbackForce, ForceMode2D.Impulse);
        }
        
        if (cameraShake != null)
        {
            cameraShake.Shake(shakeDuration, shakeIntensity);
        }
        
        StartCoroutine(RedFlash());
        
        float flashInterval = invincibilityDuration / (invincibilityFlashCount * 2);
        for (int i = 0; i < invincibilityFlashCount; i++)
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
            yield return new WaitForSeconds(flashInterval);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
        }
        
        isInvincible = false;
    }

    IEnumerator RedFlash()
    {
        if (damageOverlay == null) yield break;
        
        float elapsed = 0f;
        Color c = damageOverlay.color;
        
        while (elapsed < damageFlashDuration / 2)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 0.4f, elapsed / (damageFlashDuration / 2));
            damageOverlay.color = c;
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < damageFlashDuration / 2)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0.4f, 0f, elapsed / (damageFlashDuration / 2));
            damageOverlay.color = c;
            yield return null;
        }
        
        c.a = 0f;
        damageOverlay.color = c;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player Dead!");

        MusicManager musicManager = MusicManager.Instance;
        if (musicManager != null)
        {
            musicManager.StopMusicWithDjSlowdown();
        }

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        CameraFollow camFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (camFollow != null) camFollow.pauseFollow = true;

        PlayerAttack attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;

        PlayerExperience experience = GetComponent<PlayerExperience>();
        if (experience != null) experience.enabled = false;

        PlayerFootsteps footsteps = GetComponent<PlayerFootsteps>();
        if (footsteps != null) footsteps.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        StopAllCoroutines();
        fireDebuffRoutine = null;
        fireTimeRemaining = 0f;
        fireDamagePerSecond = 0f;
        SetFireDebuffVisible(false);

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        StartCoroutine(ShowDeathScreenAfterDelay());
    }

    private IEnumerator ShowDeathScreenAfterDelay()
    {
        yield return new WaitForSeconds(deathScreenDelay);

        if (spriteRenderer != null)
        {
            PlayerDeathScreen.Instance.Show(spriteRenderer, transform.position, transform.lossyScale);
        }
        else
        {
            PlayerDeathScreen.Instance.Show(null, transform.position, transform.lossyScale);
        }

        gameObject.SetActive(false);
    }

    private void OnGUI()
    {
        float t = Time.deltaTime / 1f;
        healthSlider.value = Mathf.Lerp(healthSlider.value, health, t);
    }

    public float GetHealthPercentage()
    {
        return health / maxHealth;
    }

    public float GetCurrentHealth()
    {
        return health;
    }
    
    // ✅ GETTER PER MAX HEALTH (utile per debug)
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public void ApplyFireDebuff(float duration, float damagePerSecond)
    {
        if (isDead || duration <= 0f || damagePerSecond <= 0f)
        {
            return;
        }

        fireTimeRemaining = Mathf.Max(fireTimeRemaining, duration);
        fireDamagePerSecond = Mathf.Max(fireDamagePerSecond, damagePerSecond);

        EnsureFireDebuffUI();
        SetFireDebuffVisible(true);
        RefreshFireDebuffText();

        if (fireDebuffRoutine == null)
        {
            fireDebuffRoutine = StartCoroutine(FireDebuffRoutine());
        }
    }

    private IEnumerator FireDebuffRoutine()
    {
        while (!isDead && fireTimeRemaining > 0f)
        {
            float tick = Mathf.Min(fireTickInterval, fireTimeRemaining);
            yield return new WaitForSeconds(tick);

            if (isDead)
            {
                yield break;
            }

            fireTimeRemaining -= tick;
            float tickDamage = fireDamagePerSecond * tick;
            ApplyDebuffDamage(tickDamage);
            RefreshFireDebuffText();
        }

        fireDebuffRoutine = null;
        fireTimeRemaining = 0f;
        fireDamagePerSecond = 0f;
        SetFireDebuffVisible(false);
    }

    private void ApplyDebuffDamage(float damage)
    {
        if (isDead || damage <= 0f)
        {
            return;
        }

        health -= damage;
        SoundEffectManager.Play("PlayerHitted");

        if (health <= 0f)
        {
            health = 0f;
            if (healthSlider != null)
            {
                healthSlider.value = health;
            }
            Die();
            return;
        }

        StartCoroutine(RedFlash());
    }

    private void EnsureFireDebuffUI()
    {
        if (fireDebuffCanvas != null && fireDebuffText != null && fireDebuffTint != null)
        {
            return;
        }

        GameObject canvasObj = new GameObject("FireDebuffUI");
        fireDebuffCanvas = canvasObj.AddComponent<Canvas>();
        fireDebuffCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fireDebuffCanvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject tintObj = new GameObject("FireTint");
        tintObj.transform.SetParent(canvasObj.transform, false);
        fireDebuffTint = tintObj.AddComponent<Image>();
        fireDebuffTint.raycastTarget = false;
        RectTransform tintRect = fireDebuffTint.rectTransform;
        tintRect.anchorMin = Vector2.zero;
        tintRect.anchorMax = Vector2.one;
        tintRect.offsetMin = Vector2.zero;
        tintRect.offsetMax = Vector2.zero;
        fireDebuffTint.color = fireTintColor;

        GameObject textObj = new GameObject("FireDebuffText");
        textObj.transform.SetParent(canvasObj.transform, false);
        fireDebuffText = textObj.AddComponent<Text>();
        fireDebuffText.alignment = TextAnchor.UpperCenter;
        fireDebuffText.color = fireDebuffTextColor;
        fireDebuffText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        fireDebuffText.fontSize = 28;
        fireDebuffText.raycastTarget = false;
        RectTransform textRect = fireDebuffText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.offsetMin = new Vector2(0f, -76f);
        textRect.offsetMax = new Vector2(0f, -20f);

        SetFireDebuffVisible(false);
    }

    private void RefreshFireDebuffText()
    {
        if (fireDebuffText == null)
        {
            return;
        }

        if (fireTimeRemaining > 0f)
        {
            fireDebuffText.text = $"SCOTTATO  -{fireDamagePerSecond:0.#}/s";
        }
        else
        {
            fireDebuffText.text = string.Empty;
        }
    }

    private void SetFireDebuffVisible(bool visible)
    {
        if (fireDebuffText != null)
        {
            fireDebuffText.enabled = visible;
        }

        if (fireDebuffTint != null && !visible)
        {
            Color c = fireDebuffTint.color;
            c.a = 0f;
            fireDebuffTint.color = c;
        }
    }

    private void UpdateFireVisual()
    {
        if (fireDebuffTint == null)
        {
            return;
        }

        if (fireTimeRemaining <= 0f || isDead)
        {
            Color offColor = fireDebuffTint.color;
            offColor.a = Mathf.Lerp(offColor.a, 0f, Time.deltaTime * 10f);
            fireDebuffTint.color = offColor;
            return;
        }

        float pulse = 0.75f + Mathf.Sin(Time.time * 8f) * 0.25f;
        Color activeColor = fireTintColor;
        activeColor.a = fireTintMaxAlpha * pulse;
        fireDebuffTint.color = activeColor;
    }

    public void LoadHealthState(float savedCurrentHealth, float savedMaxHealth)
    {
        maxHealth = Mathf.Max(1f, savedMaxHealth);
        health = Mathf.Clamp(savedCurrentHealth, 0f, maxHealth);

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }

        isDead = false;
    }

    public void HealToFull()
    {
        health = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.value = health;
        }

        if (fireDebuffRoutine != null)
        {
            StopCoroutine(fireDebuffRoutine);
            fireDebuffRoutine = null;
        }
        fireTimeRemaining = 0f;
        fireDamagePerSecond = 0f;
        SetFireDebuffVisible(false);
    }
}
