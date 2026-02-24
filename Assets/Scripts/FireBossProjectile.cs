using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FireBossProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private bool rotateToDirection = false;
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField] private bool destroyOnAnySolidHit = true;

    private Vector2 direction = Vector2.right;

    public void Initialize(Vector2 dir, float projectileSpeed, float projectileDamage, float projectileLifetime)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        speed = projectileSpeed;
        damage = projectileDamage;
        lifetime = projectileLifetime;
    }

    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (rotateToDirection)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, direction, PlayerHealth.DamageSource.Projectile);
            }
            Destroy(gameObject);
            return;
        }

        bool hitsBlockingLayer = (blockingLayers.value & (1 << other.gameObject.layer)) != 0;
        bool hitsSolidCollider = destroyOnAnySolidHit && !other.isTrigger && !other.CompareTag("Player");
        if (hitsBlockingLayer || hitsSolidCollider)
        {
            Destroy(gameObject);
        }
    }
}
