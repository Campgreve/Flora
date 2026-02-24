using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class FireDropletHazard : MonoBehaviour
{
    private static Sprite cachedDropletSprite;

    private float contactDamage;
    private float burnDuration;
    private float burnDamagePerSecond;
    private bool consumed;

    public void Initialize(
        float directDamage,
        float debuffDuration,
        float debuffDamagePerSecond,
        Vector2 initialVelocity,
        float lifetime,
        float scale)
    {
        contactDamage = Mathf.Max(0f, directDamage);
        burnDuration = Mathf.Max(0f, debuffDuration);
        burnDamagePerSecond = Mathf.Max(0f, debuffDamagePerSecond);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = initialVelocity;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.25f;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = GetDropletSprite();
        sr.sortingOrder = 220;
        sr.color = new Color(1f, 0.4f, 0.1f, 0.95f);
        transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);

        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !other.CompareTag("Player"))
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            return;
        }

        consumed = true;
        Vector2 hitDirection = (other.transform.position - transform.position).sqrMagnitude > 0.001f
            ? ((Vector2)(other.transform.position - transform.position)).normalized
            : Vector2.down;

        if (contactDamage > 0f)
        {
            playerHealth.TakeDamage(contactDamage, hitDirection, PlayerHealth.DamageSource.Projectile);
        }

        if (burnDuration > 0f && burnDamagePerSecond > 0f)
        {
            playerHealth.ApplyFireDebuff(burnDuration, burnDamagePerSecond);
        }

        Destroy(gameObject);
    }

    private static Sprite GetDropletSprite()
    {
        if (cachedDropletSprite != null)
        {
            return cachedDropletSprite;
        }

        const int size = 12;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.45f;
        Color core = new Color(1f, 0.85f, 0.45f, 1f);
        Color edge = new Color(1f, 0.22f, 0.08f, 0.05f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / radius;
                d = Mathf.Clamp01(d);
                Color c = Color.Lerp(core, edge, d);
                c.a *= 1f - d;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        cachedDropletSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedDropletSprite;
    }
}
