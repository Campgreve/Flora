using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public class FireForestLightingController : MonoBehaviour
{
    [Header("Activation")]
    [SerializeField] private string fireSceneName = "FireForest";

    [Header("Global Light")]
    [SerializeField] private Color globalColor = new Color(0.95f, 0.52f, 0.36f, 1f);
    [SerializeField] private float globalIntensity = 0.58f;

    [Header("Player Fire Light")]
    [SerializeField] private Color playerLightColor = new Color(1f, 0.74f, 0.5f, 1f);
    [SerializeField] private float playerLightIntensity = 0.95f;
    [SerializeField] private float playerLightRadius = 3.2f;
    [SerializeField] private float flickerAmplitude = 0.18f;
    [SerializeField] private float flickerSpeed = 4.3f;

    private Light2D globalLight;
    private Light2D playerLight;
    private float flickerSeed;

    private void Start()
    {
        if (!IsFireScene())
        {
            enabled = false;
            return;
        }

        EnsureGlobalLight();
        EnsurePlayerLight();
        flickerSeed = Random.Range(0f, 999f);
    }

    private void Update()
    {
        if (playerLight == null)
        {
            return;
        }

        float n = Mathf.PerlinNoise(flickerSeed, Time.time * flickerSpeed);
        float flicker = Mathf.Lerp(-flickerAmplitude, flickerAmplitude, n);
        playerLight.intensity = playerLightIntensity + flicker;
    }

    private bool IsFireScene()
    {
        return SceneManager.GetActiveScene().name.Equals(fireSceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureGlobalLight()
    {
        Light2D[] allLights = FindObjectsOfType<Light2D>(true);
        foreach (Light2D l in allLights)
        {
            if (l.lightType == Light2D.LightType.Global)
            {
                globalLight = l;
                break;
            }
        }

        if (globalLight == null)
        {
            GameObject go = new GameObject("FireForestGlobalLight2D");
            globalLight = go.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
        }

        globalLight.color = globalColor;
        globalLight.intensity = globalIntensity;
    }

    private void EnsurePlayerLight()
    {
        Transform lightChild = transform.Find("FirePlayerLight2D");
        if (lightChild == null)
        {
            GameObject lightObj = new GameObject("FirePlayerLight2D");
            lightObj.transform.SetParent(transform, false);
            lightChild = lightObj.transform;
        }

        playerLight = lightChild.GetComponent<Light2D>();
        if (playerLight == null)
        {
            playerLight = lightChild.gameObject.AddComponent<Light2D>();
            playerLight.lightType = Light2D.LightType.Point;
        }

        playerLight.color = playerLightColor;
        playerLight.intensity = playerLightIntensity;
        playerLight.pointLightOuterRadius = playerLightRadius;
        playerLight.pointLightInnerRadius = playerLightRadius * 0.28f;
        playerLight.falloffIntensity = 0.65f;
        playerLight.blendStyleIndex = 0;
    }
}
