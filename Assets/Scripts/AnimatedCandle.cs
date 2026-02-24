using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controller per candele animate con luce 2D per URP
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedCandle : MonoBehaviour
{
    [Header("Animation")]
    public Sprite[] animationFrames;
    
    [Range(0.05f, 0.5f)]
    public float frameRate = 0.1f;
    
    private SpriteRenderer spriteRenderer;
    private int currentFrame = 0;
    private float timer = 0f;

    [Header("Light Settings")]
    public bool hasLight = true;
    
    [Range(1f, 5f)]
    public float lightIntensity = 2f;
    
    [Range(0.5f, 3f)]
    public float lightRadius = 1.5f;
    
    public Color lightColor = new Color(1f, 0.8f, 0.4f); // Arancione caldo
    
    [Header("Light Flicker")]
    public bool flickerEnabled = true;
    
    [Range(0f, 0.5f)]
    public float flickerAmount = 0.2f;
    
    [Range(0.05f, 0.3f)]
    public float flickerSpeed = 0.1f;

    private Light2D light2D;
    private float baseLightIntensity;
    private float flickerTimer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (hasLight)
        {
            SetupLight();
        }
        
        // Randomizza il frame iniziale per varietà
        currentFrame = Random.Range(0, animationFrames.Length);
        timer = Random.Range(0f, frameRate);
    }

    void SetupLight()
    {
        // Crea o ottieni il componente Light2D
        light2D = GetComponent<Light2D>();
        
        if (light2D == null)
        {
            light2D = gameObject.AddComponent<Light2D>();
        }

        // Configura la luce
        light2D.lightType = Light2D.LightType.Point;
        light2D.color = lightColor;
        light2D.intensity = lightIntensity;
        light2D.pointLightOuterRadius = lightRadius;
        light2D.pointLightInnerRadius = lightRadius * 0.3f;
        light2D.volumeIntensity = 0.5f;
        
        baseLightIntensity = lightIntensity;
    }

    void Update()
    {
        AnimateSprite();
        
        if (hasLight && light2D != null && flickerEnabled)
        {
            FlickerLight();
        }
    }

    void AnimateSprite()
    {
        if (animationFrames == null || animationFrames.Length == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % animationFrames.Length;
            spriteRenderer.sprite = animationFrames[currentFrame];
        }
    }

    void FlickerLight()
    {
        flickerTimer += Time.deltaTime;

        if (flickerTimer >= flickerSpeed)
        {
            flickerTimer = 0f;
            
            float flicker = Random.Range(-flickerAmount, flickerAmount);
            light2D.intensity = baseLightIntensity + flicker;
            
            // Varia leggermente anche il raggio
            float radiusFlicker = Random.Range(-0.1f, 0.1f);
            light2D.pointLightOuterRadius = lightRadius + radiusFlicker;
        }
    }

    // Helper per impostare i frame da editor o script
    public void SetAnimationFrames(Sprite[] frames)
    {
        animationFrames = frames;
        if (frames.Length > 0)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[0];
            }
        }
    }
}