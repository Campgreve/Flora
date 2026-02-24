using UnityEngine;

public class ExperienceOrb : MonoBehaviour
{
    [Header("Experience Settings")]
    public int xpAmount = 5;

    [Header("Movement")]
    [SerializeField] private float attractionSpeed = 5f;
    [SerializeField] private float attractionRange = 3f;
    [SerializeField] private float accelerationRate = 1.5f;

    private Transform player;
    private bool isBeingCollected = false;
    private float currentSpeed;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        currentSpeed = attractionSpeed;
    }

    private void Update()
    {
        if (player == null || isBeingCollected) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attractionRange)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            currentSpeed += accelerationRate * Time.deltaTime;
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                currentSpeed * Time.deltaTime
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBeingCollected) return;

        if (other.CompareTag("Player"))
        {
            isBeingCollected = true;

            PlayerExperience playerExp = other.GetComponent<PlayerExperience>();
            if (playerExp != null)
                playerExp.GainXP(xpAmount);

            SoundEffectManager.Play("XpPickup", true);


            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attractionRange);
    }
}
