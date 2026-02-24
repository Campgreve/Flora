using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema minimap per dungeon grandi
/// Mostra la posizione del player e le stanze esplorate
/// </summary>
public class MinimapSystem : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Camera minimapCamera;
    public RawImage minimapDisplay;

    [Header("Minimap Settings")]
    [Range(10f, 50f)]
    public float minimapHeight = 30f;
    
    public bool followPlayer = true;
    
    [Header("Explored System")]
    public bool useFogOfWar = true;
    public float explorationRadius = 15f;
    
    [Header("UI Position")]
    public Vector2 minimapPosition = new Vector2(20, 20);
    public Vector2 minimapSize = new Vector2(200, 200);

    private RenderTexture minimapTexture;
    private Material fogMaterial;
    private Texture2D explorationTexture;
    private int mapResolution = 512;

    void Start()
    {
        SetupMinimap();
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    void SetupMinimap()
    {
        // Setup camera minimap se non esiste
        if (minimapCamera == null)
        {
            GameObject camObj = new GameObject("MinimapCamera");
            camObj.transform.parent = transform;
            minimapCamera = camObj.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = minimapHeight;
            minimapCamera.cullingMask = LayerMask.GetMask("Default"); // Cambia con i tuoi layer
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = Color.black;
            minimapCamera.depth = 10;
        }

        // Setup render texture
        minimapTexture = new RenderTexture(mapResolution, mapResolution, 16);
        minimapTexture.filterMode = FilterMode.Point;
        minimapCamera.targetTexture = minimapTexture;

        // Setup UI
        if (minimapDisplay == null)
        {
            SetupMinimapUI();
        }
        else
        {
            minimapDisplay.texture = minimapTexture;
        }

        // Setup fog of war
        if (useFogOfWar)
        {
            SetupFogOfWar();
        }
    }

    void SetupMinimapUI()
    {
        // Crea Canvas se necessario
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MinimapCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Crea l'immagine della minimap
        GameObject minimapObj = new GameObject("MinimapDisplay");
        minimapObj.transform.SetParent(canvas.transform);
        
        minimapDisplay = minimapObj.AddComponent<RawImage>();
        minimapDisplay.texture = minimapTexture;
        
        // Posiziona nell'angolo
        RectTransform rect = minimapDisplay.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1); // Top-right
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = -minimapPosition;
        rect.sizeDelta = minimapSize;

        // Aggiungi bordo
        GameObject border = new GameObject("MinimapBorder");
        border.transform.SetParent(minimapObj.transform);
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(1, 1, 1, 0.3f);
        
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);
    }

    void SetupFogOfWar()
    {
        // TODO: Implementa fog of war con texture
        // Per ora è disabilitato
    }

    void LateUpdate()
    {
        if (followPlayer && player != null && minimapCamera != null)
        {
            Vector3 newPos = player.position;
            newPos.z = minimapCamera.transform.position.z;
            minimapCamera.transform.position = newPos;
        }
    }

    // Metodo per toggleare la minimap
    public void ToggleMinimap()
    {
        if (minimapDisplay != null)
        {
            minimapDisplay.gameObject.SetActive(!minimapDisplay.gameObject.activeSelf);
        }
    }

    // Metodo per cambiare lo zoom
    public void SetMinimapZoom(float height)
    {
        minimapHeight = height;
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = minimapHeight;
        }
    }
}