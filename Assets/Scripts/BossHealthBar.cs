using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider; // ← CAMBIATO
    [SerializeField] private Text bossNameText; // Opzionale
    
    private IBossHealthSource boss;
    
    public void Initialize(IBossHealthSource bossReference)
    {
        boss = bossReference;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = 1f; // 0-1 per percentuale
            healthSlider.value = 1f;
        }
        
        if (bossNameText != null)
            bossNameText.text = boss != null ? boss.GetBossDisplayName() : "BOSS";
    }
    
    void Update()
    {
        if (boss != null && healthSlider != null)
        {
            float healthPercent = boss.GetHealthPercent();
            healthSlider.value = healthPercent;
            
            // Opzionale: cambia colore fill
            Image fillImage = healthSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (healthPercent <= 0.3f)
                    fillImage.color = Color.red;
                else if (healthPercent <= 0.6f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.green;
            }
        }
    }
}
