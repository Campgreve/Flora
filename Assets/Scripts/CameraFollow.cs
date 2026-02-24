using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    
    [Header("Impostazioni Movimento")]
    public Vector3 offset = new Vector3(0, 0, -10f);
    public float smoothTime = 0.3f;
    
    [Header("Movimento Mouse")]
    public bool useMouseOffset = true;
    public float mouseInfluence = 2f;
    public float mouseSmoothing = 5f;
    
    [Header("Limiti Camera (Opzionale)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;
    
    private Transform originalTarget;
    private Vector3 velocity = Vector3.zero;
    private Vector3 currentMouseOffset = Vector3.zero;
    private Vector3 mouseVelocity = Vector3.zero;
    
    [HideInInspector] public bool pauseFollow = false;
    
    void Start()
    {
        originalTarget = player; // Salva il target originale
    }
    
    void LateUpdate()
    {
        if (player == null || pauseFollow)
            return;

        float delta = Time.unscaledDeltaTime;
        
        // Calcola offset del mouse
        Vector3 mouseOffset = Vector3.zero;
        if (useMouseOffset)
        {
            if (GameInput.TryGetRightStick(out Vector2 rightStick, 0.2f))
            {
                mouseOffset = new Vector3(
                    rightStick.x * mouseInfluence,
                    rightStick.y * mouseInfluence,
                    0f
                );
            }
            else
            {
                // Clamp della posizione del mouse per evitare valori fuori schermo
                float mouseX = Mathf.Clamp(Input.mousePosition.x, 0, Screen.width);
                float mouseY = Mathf.Clamp(Input.mousePosition.y, 0, Screen.height);

                // Ottieni la posizione del mouse in coordinate schermo normalizzate (-0.5 a 0.5)
                Vector2 mouseScreenPos = new Vector2(
                    (mouseX / Screen.width) - 0.5f,
                    (mouseY / Screen.height) - 0.5f
                );

                // Converti in offset world space
                mouseOffset = new Vector3(
                    mouseScreenPos.x * mouseInfluence,
                    mouseScreenPos.y * mouseInfluence,
                    0f
                );
            }
            
            // Smooth dell'offset del mouse
            float mouseSmoothTime = mouseSmoothing > 0f ? 1f / mouseSmoothing : 0.0001f;
            currentMouseOffset = Vector3.SmoothDamp(
                currentMouseOffset, 
                mouseOffset, 
                ref mouseVelocity, 
                mouseSmoothTime,
                Mathf.Infinity,
                delta
            );
        }
        
        // Calcola la posizione target (player + offset fisso + offset mouse)
        Vector3 targetPos = player.position + offset + currentMouseOffset;
        
        // Applica i limiti se abilitati
        if (useBounds)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x);
            targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y, maxBounds.y);
        }
        
        // Movimento fluido verso il target
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPos, 
            ref velocity, 
            smoothTime,
            Mathf.Infinity,
            delta
        );
    }
    
    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
    }
    
    public void ResetTarget()
    {
        player = originalTarget;
    }
}
