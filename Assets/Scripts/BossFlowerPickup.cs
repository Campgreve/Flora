using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class BossFlowerPickup : MonoBehaviour
{
    [SerializeField] private string flowerId = "flower_unknown";
    [SerializeField] private string flowerDisplayName = "Fiore Misterioso";
    [SerializeField] private float lifeTime = 30f;
    [SerializeField] private TextMeshProUGUI worldLabel;
    [SerializeField] private UnityEvent onCollected;

    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (worldLabel != null)
        {
            worldLabel.text = flowerDisplayName;
        }

        Destroy(gameObject, lifeTime);
    }

    public void Setup(string id, string displayName)
    {
        flowerId = id;
        flowerDisplayName = displayName;

        if (worldLabel != null)
        {
            worldLabel.text = flowerDisplayName;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        bool collected = GameProgress.Instance.CollectFlower(flowerId);
        if (collected)
        {
            Debug.Log($"🌸 Fiore ottenuto: {flowerDisplayName} ({flowerId})");
            onCollected?.Invoke();
        }

        Destroy(gameObject);
    }
}
