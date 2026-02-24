using UnityEngine;

public class BarrierEffect : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private Color baseColor = Color.red;
    
    private SpriteRenderer spriteRenderer;
    private float alpha = 0.5f;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    void Update()
    {
        // Effetto pulsante
        alpha = 0.3f + Mathf.PingPong(Time.time * pulseSpeed, 0.4f);
        Color newColor = baseColor;
        newColor.a = alpha;
        spriteRenderer.color = newColor;
    }
}