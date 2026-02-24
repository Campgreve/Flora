using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controller per il GrassWindShader
/// Usa questo SOLO se hai applicato il GrassWindShader.shader al tuo Tilemap
/// </summary>
public class GrassWindController : MonoBehaviour
{
    [Header("Wind Settings")]
    [SerializeField] private float windSpeed = 1f;
    [SerializeField] private float windStrength = 0.1f;
    [SerializeField] private Vector2 windDirection = new Vector2(1f, 0.3f);
    
    [Header("Player Interaction")]
    [SerializeField] private float playerRadius = 2f;
    [SerializeField] private float playerStrength = 0.3f;
    
    private Material grassMaterial;
    private Transform player;
    
    // Shader property IDs (per performance)
    private static readonly int WindSpeedID = Shader.PropertyToID("_WindSpeed");
    private static readonly int WindStrengthID = Shader.PropertyToID("_WindStrength");
    private static readonly int WindDirectionID = Shader.PropertyToID("_WindDirection");
    private static readonly int PlayerPosID = Shader.PropertyToID("_PlayerPos");
    private static readonly int PlayerRadiusID = Shader.PropertyToID("_PlayerRadius");
    private static readonly int PlayerStrengthID = Shader.PropertyToID("_PlayerStrength");
    
    void Start()
    {
        // Ottieni il material del Tilemap Renderer
        TilemapRenderer tilemapRenderer = GetComponent<TilemapRenderer>();
        
        if (tilemapRenderer != null)
        {
            grassMaterial = tilemapRenderer.material;
            
            if (grassMaterial == null)
            {
                Debug.LogError("❌ GrassWindController: Material non trovato sul TilemapRenderer!");
                enabled = false;
                return;
            }
            
            // Verifica che sia il material giusto
            if (!grassMaterial.shader.name.Contains("GrassWind"))
            {
                Debug.LogWarning("⚠️ Il material non usa il GrassWindShader! Assicurati di aver applicato lo shader corretto.");
            }
        }
        else
        {
            Debug.LogError("❌ GrassWindController: TilemapRenderer non trovato!");
            enabled = false;
            return;
        }
        
        // Trova il player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("⚠️ Player non trovato! L'interazione non funzionerà.");
        }
        
        // Imposta i parametri iniziali
        UpdateShaderProperties();
        
        Debug.Log("🌿 GrassWindController inizializzato con shader: " + grassMaterial.shader.name);
    }
    
    void Update()
    {
        if (grassMaterial == null) return;
        
        UpdateShaderProperties();
    }
    
    void UpdateShaderProperties()
    {
        // Aggiorna i parametri del vento
        grassMaterial.SetFloat(WindSpeedID, windSpeed);
        grassMaterial.SetFloat(WindStrengthID, windStrength);
        grassMaterial.SetVector(WindDirectionID, windDirection.normalized);
        
        // Aggiorna la posizione del player
        if (player != null)
        {
            grassMaterial.SetVector(PlayerPosID, player.position);
            grassMaterial.SetFloat(PlayerRadiusID, playerRadius);
            grassMaterial.SetFloat(PlayerStrengthID, playerStrength);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            // Visualizza il raggio di interazione
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(player.position, playerRadius);
            
            // Visualizza la direzione del vento
            Gizmos.color = Color.cyan;
            Vector3 start = player.position;
            Gizmos.DrawRay(start, (Vector3)windDirection.normalized * 3f);
        }
    }
}