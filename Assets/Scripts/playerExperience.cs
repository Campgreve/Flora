using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerExperience : MonoBehaviour
{
    [Header("Level System")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;
    
    [Header("Level Progression")]
    [SerializeField] private float xpMultiplier = 1.5f;
    
    [Header("UI References")]
    [SerializeField] private Slider xpBar;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;
    
    [Header("Level Up Effects")]
    [SerializeField] private GameObject levelUpEffect;
    [SerializeField] private float healthIncreasePerLevel = 10f;
    [SerializeField] private float damageIncreasePerLevel = 2f;
    
    // ✅ USA PLAYERATTACK (il tuo sistema esistente)
    [Header("Attack System Reference")]
    [SerializeField] private PlayerAttack playerAttack;
    
    private PlayerHealth playerHealth;
    
    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        
        // ✅ Auto-trova PlayerAttack se non assegnato
        if (playerAttack == null)
        {
            playerAttack = GetComponent<PlayerAttack>();
            if (playerAttack == null)
            {
                playerAttack = FindObjectOfType<PlayerAttack>();
            }
        }
        
        if (playerAttack == null)
        {
            Debug.LogWarning("⚠️ PlayerAttack non trovato! Il danno arco non verrà aggiornato.");
        }
        
        UpdateXPBar();
        UpdateLevelText();
        UpdateXPText();
        
        // ✅ IMPOSTA IL DANNO INIZIALE DELL'ARCO
        UpdateBowDamage();
    }
    
    public void GainXP(int amount)
    {
        currentXP += amount;
        
        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
        
        UpdateXPBar();
        UpdateXPText();
    }
    
    void LevelUp()
    {
        currentLevel++;
        currentXP -= xpToNextLevel;
        
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpMultiplier);
        
        Debug.Log("🎉 LEVEL UP! Sei ora livello " + currentLevel);
        
        ApplyLevelUpBonuses();
        
        if (levelUpEffect != null)
        {
            Instantiate(levelUpEffect, transform.position, Quaternion.identity);
        }
        
        // Suono level up (se hai il sound effect)
        SoundEffectManager.Play("LevelUp", true);
        
        UpdateLevelText();
    }
    
    void ApplyLevelUpBonuses()
    {
        // Aumenta max health e cura
        if (playerHealth != null)
        {
            playerHealth.IncreaseMaxHealth(healthIncreasePerLevel);
            playerHealth.UpdateHealth(healthIncreasePerLevel);
        }
        
        // ✅ AUMENTA IL DANNO DELL'ARCO
        UpdateBowDamage();
    }
    
    // ✅ METODO CHE CALCOLA E APPLICA IL DANNO DELL'ARCO
    void UpdateBowDamage()
    {
        if (playerAttack == null) return;
        
        // Il danno base dell'arco è BowCharge * BowPower (es. ~6 danno)
        // Aggiungiamo un BONUS per ogni livello oltre il primo
        // Livello 1: 0 bonus
        // Livello 2: +2 bonus
        // Livello 5: +8 bonus
        // Livello 10: +18 bonus
        float bonusDamage = damageIncreasePerLevel * (currentLevel - 1);
        
        // ✅ Passa il BONUS al PlayerAttack
        playerAttack.SetArrowDamage(bonusDamage);
        
        Debug.Log($"⚡ Arrow bonus damage aggiornato: +{bonusDamage:F1} (Livello {currentLevel})");
    }
    
    void UpdateXPBar()
    {
        if (xpBar != null)
        {
            xpBar.maxValue = xpToNextLevel;
            xpBar.value = currentXP;
        }
    }
    
    void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = "Lvl " + currentLevel;
        }
    }
    
    void UpdateXPText()
    {
        if (xpText != null)
        {
            xpText.text = currentXP + " / " + xpToNextLevel;
        }
    }
    
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    public float GetXPPercentage()
    {
        return (float)currentXP / (float)xpToNextLevel;
    }
    
    public float GetCurrentArrowDamage()
    {
        // Ritorna solo il bonus (il danno base dipende dalla carica dell'arco)
        return damageIncreasePerLevel * (currentLevel - 1);
    }

    public void LoadProgress(int savedLevel, int savedCurrentXP, int savedXpToNextLevel)
    {
        currentLevel = Mathf.Max(1, savedLevel);
        currentXP = Mathf.Max(0, savedCurrentXP);
        xpToNextLevel = Mathf.Max(1, savedXpToNextLevel);

        if (currentXP > xpToNextLevel)
        {
            currentXP = xpToNextLevel;
        }

        UpdateXPBar();
        UpdateLevelText();
        UpdateXPText();
        UpdateBowDamage();
    }
}
