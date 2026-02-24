using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float fadeSpeed = 1f;
    
    private Color textColor;
    private float timer;

    private void Start()
    {
        if (damageText != null)
        {
            textColor = damageText.color;
        }
    }

    public void Setup(float damage)
    {
        if (damageText != null)
        {
            damageText.text = Mathf.RoundToInt(damage).ToString();
        }
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Muovi il testo verso l'alto
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        
        // Fade out
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
        
        if (damageText != null)
        {
            textColor.a = alpha;
            damageText.color = textColor;
        }
    }
}