using UnityEngine;

public class Arrow : MonoBehaviour 
{
    [HideInInspector] public float ArrowVelocity;
    [HideInInspector] public float ArrowDamage;
    [SerializeField] private Rigidbody2D rb;
    
    private bool hasHit = false;
    
    private void Start() 
    {
        if (rb != null) 
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        
        Destroy(gameObject, 4f);
    }
    
    private void FixedUpdate() 
    {
        if (!hasHit) 
        {
            rb.linearVelocity = transform.up * ArrowVelocity;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D other) 
    {
        if (hasHit) return;
        if (ShouldIgnoreProjectileCollision(other.collider))
        {
            return;
        }
        
        hasHit = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        Vector2 hitDirection = (other.transform.position - transform.position).normalized;
        if (TryDamage(other.collider, hitDirection))
        {
            return;
        }

        Destroy(gameObject, 0.5f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || other == null) return;
        if (ShouldIgnoreProjectileCollision(other))
        {
            return;
        }

        // Ignore generic detection triggers; allow trigger hurtboxes only for bosses.
        if (!IsBossTriggerHurtbox(other))
        {
            return;
        }

        hasHit = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Vector2 hitDirection = (other.transform.position - transform.position).normalized;
        if (TryDamage(other, hitDirection))
        {
            return;
        }

        Destroy(gameObject, 0.5f);
    }

    private bool TryDamage(Collider2D hitCollider, Vector2 hitDirection)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.isTrigger && !IsBossTriggerHurtbox(hitCollider))
        {
            return false;
        }

        IDamageable damageable = hitCollider.GetComponent<IDamageable>();
        if (damageable == null)
        {
            // For solid colliders, allow parent lookup.
            damageable = hitCollider.GetComponentInParent<IDamageable>();
        }

        if (damageable == null)
        {
            return false;
        }

        damageable.TakeDamage(ArrowDamage, hitDirection);
        Debug.Log($"Arrow hit {hitCollider.name} for {ArrowDamage} damage!");
        Destroy(gameObject, 0.1f);
        return true;
    }

    private static bool IsBossTriggerHurtbox(Collider2D col)
    {
        if (col == null || !col.isTrigger)
        {
            return false;
        }

        IBossHealthSource boss = col.GetComponent<IBossHealthSource>();
        if (boss == null)
        {
            boss = col.GetComponentInParent<IBossHealthSource>();
        }

        return boss != null;
    }

    private bool ShouldIgnoreProjectileCollision(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (HasHostileProjectileComponent(other))
        {
            Collider2D ownCollider = GetComponent<Collider2D>();
            if (ownCollider != null)
            {
                Physics2D.IgnoreCollision(ownCollider, other, true);
            }
            return true;
        }

        return false;
    }

    private static bool HasHostileProjectileComponent(Collider2D other)
    {
        if (other.GetComponent<FireBossProjectile>() != null || other.GetComponentInParent<FireBossProjectile>() != null)
        {
            return true;
        }

        if (other.GetComponent<FireDropletHazard>() != null || other.GetComponentInParent<FireDropletHazard>() != null)
        {
            return true;
        }

        string name = other.gameObject.name;
        return !string.IsNullOrEmpty(name) && name.ToLowerInvariant().Contains("projectile");
    }
}
