using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("SceneTransitionManager");
                instance = go.AddComponent<SceneTransitionManager>();
            }
            return instance;
        }
    }

    private CanvasGroup fadeGroup;
    private bool isLoading;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildFadeOverlay();
    }

    public static bool IsLoading => Instance.isLoading;

    public static void LoadSceneWithFade(string sceneName, float fadeOutDuration = 0.35f, float fadeInDuration = 0.35f)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneTransitionManager: sceneName vuoto.");
            return;
        }

        if (Instance.isLoading)
        {
            return;
        }

        Instance.StartCoroutine(Instance.LoadRoutine(sceneName, fadeOutDuration, fadeInDuration));
    }

    private IEnumerator LoadRoutine(string sceneName, float fadeOutDuration, float fadeInDuration)
    {
        isLoading = true;
        Time.timeScale = 1f;

        yield return StartCoroutine(FadeTo(1f, fadeOutDuration));

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (op == null)
        {
            isLoading = false;
            yield return StartCoroutine(FadeTo(0f, fadeInDuration));
            yield break;
        }

        while (!op.isDone)
        {
            yield return null;
        }

        yield return null;
        yield return StartCoroutine(FadeTo(0f, fadeInDuration));
        isLoading = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeGroup == null)
        {
            yield break;
        }

        float start = fadeGroup.alpha;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeGroup.alpha = Mathf.Lerp(start, targetAlpha, t);
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;
    }

    private void BuildFadeOverlay()
    {
        GameObject canvasObj = new GameObject("SceneFadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20000;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject imageObj = new GameObject("FadeImage", typeof(Image), typeof(CanvasGroup));
        imageObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObj.GetComponent<Image>();
        image.color = Color.black;

        fadeGroup = imageObj.GetComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;
    }
}
