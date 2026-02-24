using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAttack : MonoBehaviour {
    [SerializeField] GameObject ArrowPrefab;
    [SerializeField] Slider BowPowerSlider;
    [SerializeField] Transform Bow;
    [SerializeField] Animator BowAnimator;
    
    [Range(0, 10)]
    [SerializeField] float BowPower;
    [Range(0, 3)]
    [SerializeField] float MaxBowCharge;
    [Range(0, 3)]
    [SerializeField] float MinBowCharge = 0.5f;
    
    float BowCharge;
    bool CanFire = true;
    bool wasShootHeldLastFrame = false;
    
    // ✅ DANNO DINAMICO - aumenta con i livelli
    private float bonusDamage = 0f; // Danno bonus che aumenta con il livello
    
    private void Start() {
        BowPowerSlider.value = 0f;
        BowPowerSlider.maxValue = MaxBowCharge;
        
        if (BowAnimator == null) {
            Debug.LogWarning("BowAnimator non è assegnato! L'animazione dell'arco non funzionerà.");
        }
    }
    
    private void Update() {
        // ✅ BLOCCO DURANTE BOSS INTRO
        if (BossArenaController.isInBossIntro) {
            // Se siamo nell'intro del boss, resetta tutto e non permettere azioni
            if (BowCharge > 0f) {
                CancelShot();
            }
            return; // Non eseguire nessun input
        }
        
        bool shootHeld = GameInput.IsShootHeld();
        bool shootReleased = !shootHeld && wasShootHeldLastFrame;
        wasShootHeldLastFrame = shootHeld;

        if (shootHeld && CanFire) {
            ChargeBow();
        } else if (shootReleased && CanFire) {
            if (BowCharge >= MinBowCharge) {
                FireBow();
            } else {
                CancelShot();
            }
        } else {
            if (BowCharge > 0f) {
                BowCharge -= 1f * Time.deltaTime;
                UpdateBowAnimation();
            } else {
                BowCharge = 0f;
                CanFire = true;
            }
            BowPowerSlider.value = BowCharge;
        }
    }
    
    void ChargeBow() {
        BowCharge += Time.deltaTime;
        BowPowerSlider.value = BowCharge;
        
        if (BowCharge > MaxBowCharge) {
            BowPowerSlider.value = MaxBowCharge;
        }
        
        UpdateBowAnimation();
    }
    
    void UpdateBowAnimation() {
        if (BowAnimator == null) return;
        
        float normalizedCharge = Mathf.Clamp01(BowCharge / MaxBowCharge);
        BowAnimator.SetFloat("Charge", normalizedCharge);
    }
    
    void FireBow() {
        if (BowCharge > MaxBowCharge) BowCharge = MaxBowCharge;
        
        float ArrowSpeed = BowCharge + BowPower;
        
        // ✅ DANNO SCALABILE
        // Formula originale: BowCharge * BowPower
        // Ora: (BowCharge * BowPower) + bonusDamage
        // Esempio: Livello 1 = 6 danno base, Livello 5 = 6 + 8 = 14 danno
        float ArrowDamage = (BowCharge * BowPower) + bonusDamage;
        
        GameInput.TryGetAimDirection(Bow.position, out Vector2 aimDirection);
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(new Vector3(0f, 0f, angle - 90f));
        
        Arrow Arrow = Instantiate(ArrowPrefab, Bow.position, rot).GetComponent<Arrow>();
        Arrow.ArrowVelocity = ArrowSpeed;
        Arrow.ArrowDamage = ArrowDamage;
        
        BowCharge = 0f;
        
        if (BowAnimator != null) {
            BowAnimator.SetFloat("Charge", 0f);
        }
        
        CanFire = false;
        
        // ✅ Debug per vedere il danno effettivo
        Debug.Log($"🏹 Freccia sparata! Velocità: {ArrowSpeed:F1}, Danno base: {BowCharge * BowPower:F1}, Bonus: +{bonusDamage:F1}, TOTALE: {ArrowDamage:F1}");
    }
    
    void CancelShot() {
        BowCharge = 0f;
        BowPowerSlider.value = 0f;
        
        if (BowAnimator != null) {
            BowAnimator.SetFloat("Charge", 0f);
        }
    }
    
    // ✅ METODO PUBBLICO chiamato da PlayerExperience
    // Riceve il danno bonus totale da aggiungere
    public void SetArrowDamage(float newBonusDamage)
    {
        bonusDamage = newBonusDamage;
        Debug.Log($"🏹 Bow bonus damage aggiornato a: +{bonusDamage:F1}");
    }
    
    // ✅ GETTER per il bonus attuale (opzionale, per debug/UI)
    public float GetBonusDamage()
    {
        return bonusDamage;
    }
}
