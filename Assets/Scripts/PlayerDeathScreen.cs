using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class PlayerDeathScreen : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private float fadeDuration = 1.8f;
    [SerializeField] private float pauseBeforeButtons = 0.45f;
    [SerializeField, Range(0f, 1f)] private float blackAlpha = 1f;

    [Header("Focus")]
    [SerializeField, Range(0f, 1f)] private float focusGlowAlpha = 0.28f;
    [SerializeField] private float minFocusSize = 220f;
    [SerializeField] private float focusSizeMultiplier = 2.5f;

    private static PlayerDeathScreen instance;
    public static PlayerDeathScreen Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PlayerDeathScreen>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PlayerDeathScreen");
                    instance = go.AddComponent<PlayerDeathScreen>();
                }
            }

            return instance;
        }
    }

    public static bool IsVisible => instance != null && instance.isVisible;

    private static Sprite cachedCircleSprite;

    private Canvas canvas;
    private Image blackOverlay;
    private Image focusGlowImage;
    private Image corpseImage;
    private TextMeshProUGUI gameOverText;
    private GameObject menuRoot;
    private Button retryButton;
    private Button menuButton;
    private TextMeshProUGUI retryButtonText;
    private TMP_FontAsset antiquityFont;
    private bool isVisible = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildUi();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void Show(SpriteRenderer playerSpriteRenderer, Vector3 playerWorldPosition, Vector3 playerScale)
    {
        if (isVisible)
        {
            return;
        }

        BuildUi();
        EnsureEventSystem();

        isVisible = true;
        menuRoot.SetActive(false);

        Color overlayColor = blackOverlay.color;
        overlayColor.a = 0f;
        blackOverlay.color = overlayColor;

        SetupCorpseSprite(playerSpriteRenderer, playerWorldPosition, playerScale);
        ConfigureRetryButtonLabel();

        StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        float elapsed = 0f;
        Color c = blackOverlay.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0f, blackAlpha, elapsed / fadeDuration);
            blackOverlay.color = c;
            yield return null;
        }

        c.a = blackAlpha;
        blackOverlay.color = c;

        yield return new WaitForSecondsRealtime(pauseBeforeButtons);

        Time.timeScale = 0f;
        menuRoot.SetActive(true);

        if (retryButton != null)
        {
            retryButton.Select();
        }
    }

    private void BuildUi()
    {
        if (canvas != null)
        {
            return;
        }

        antiquityFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/antiquity-print SDF");

        GameObject canvasGo = new GameObject("DeathCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlayGo = new GameObject("BlackOverlay", typeof(Image));
        overlayGo.transform.SetParent(canvas.transform, false);
        blackOverlay = overlayGo.GetComponent<Image>();
        blackOverlay.color = new Color(0f, 0f, 0f, 0f);
        StretchToFullScreen(blackOverlay.rectTransform);

        GameObject focusGo = new GameObject("FocusGlow", typeof(Image));
        focusGo.transform.SetParent(canvas.transform, false);
        focusGlowImage = focusGo.GetComponent<Image>();
        focusGlowImage.raycastTarget = false;
        focusGlowImage.sprite = GetCircleSprite();
        focusGlowImage.color = new Color(0.8f, 0.85f, 0.8f, focusGlowAlpha);
        focusGlowImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        focusGlowImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        focusGlowImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        focusGlowImage.rectTransform.sizeDelta = new Vector2(minFocusSize, minFocusSize);

        GameObject corpseGo = new GameObject("CorpseSprite", typeof(Image));
        corpseGo.transform.SetParent(canvas.transform, false);
        corpseImage = corpseGo.GetComponent<Image>();
        corpseImage.raycastTarget = false;
        corpseImage.preserveAspect = true;

        GameObject titleGo = new GameObject("GameOverTitle", typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(canvas.transform, false);
        gameOverText = titleGo.GetComponent<TextMeshProUGUI>();
        gameOverText.text = "Game Over";
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.fontSize = 160f;
        gameOverText.enableWordWrapping = false;
        gameOverText.overflowMode = TextOverflowModes.Overflow;
        gameOverText.color = new Color(0.58f, 0.16f, 0.16f, 1f);
        if (antiquityFont != null)
        {
            gameOverText.font = antiquityFont;
        }

        RectTransform titleRect = gameOverText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -120f);
        titleRect.sizeDelta = new Vector2(1400f, 220f);

        GameObject menuGo = new GameObject("DeathMenu");
        menuGo.transform.SetParent(canvas.transform, false);
        menuRoot = menuGo;

        RectTransform menuRect = menuGo.AddComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0f, 0f);
        menuRect.anchorMax = new Vector2(1f, 1f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.anchoredPosition = Vector2.zero;
        menuRect.sizeDelta = Vector2.zero;

        retryButton = CreateButton(
            menuGo.transform,
            "RetryButton",
            new Vector2(-320f, -220f),
            new Color(0.2f, 0.2f, 0.2f, 0.6f),
            new Color(0.92f, 0.88f, 0.76f, 0.98f),
            new Color(0.98f, 0.94f, 0.82f, 1f),
            new Color(0.9f, 0.9f, 0.9f, 1f),
            new Color(0.12f, 0.08f, 0.05f, 1f),
            new Color(0.1f, 0.06f, 0.04f, 1f),
            out retryButtonText);
        retryButton.onClick.AddListener(RetryFromCheckpoint);

        menuButton = CreateButton(
            menuGo.transform,
            "MenuButton",
            new Vector2(320f, -220f),
            new Color(0.2f, 0.2f, 0.2f, 0.6f),
            new Color(0.9f, 0.85f, 0.73f, 0.98f),
            new Color(0.97f, 0.9f, 0.78f, 1f),
            new Color(0.9f, 0.9f, 0.9f, 1f),
            new Color(0.16f, 0.08f, 0.05f, 1f),
            new Color(0.12f, 0.06f, 0.04f, 1f),
            out TextMeshProUGUI menuText);
        menuText.text = "Menu Principale";
        menuButton.onClick.AddListener(GoToMainMenu);

        menuRoot.SetActive(false);
    }

    private Button CreateButton(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        Color normalBg,
        Color hoverBg,
        Color pressedBg,
        Color normalText,
        Color hoverText,
        Color pressedText,
        out TextMeshProUGUI label)
    {
        GameObject buttonGo = new GameObject(objectName, typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        Image image = buttonGo.GetComponent<Image>();
        image.color = normalBg;

        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(380f, 72f);

        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = normalBg;
        colors.highlightedColor = hoverBg;
        colors.selectedColor = hoverBg;
        colors.pressedColor = pressedBg;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.5f);
        button.colors = colors;

        GameObject textGo = new GameObject("Label", typeof(TextMeshProUGUI));
        textGo.transform.SetParent(buttonGo.transform, false);

        label = textGo.GetComponent<TextMeshProUGUI>();
        label.text = "Riprova dal checkpoint";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 28;
        label.color = normalText;
        if (antiquityFont != null)
        {
            label.font = antiquityFont;
        }

        RectTransform textRect = label.rectTransform;
        StretchToFullScreen(textRect);

        DeathButtonVisual visual = buttonGo.AddComponent<DeathButtonVisual>();
        visual.Configure(image, label, normalBg, hoverBg, pressedBg, normalText, hoverText, pressedText);

        return button;
    }

    private void SetupCorpseSprite(SpriteRenderer playerSpriteRenderer, Vector3 playerWorldPosition, Vector3 playerScale)
    {
        if (playerSpriteRenderer == null || playerSpriteRenderer.sprite == null)
        {
            corpseImage.enabled = false;
            focusGlowImage.enabled = false;
            return;
        }

        corpseImage.enabled = true;
        focusGlowImage.enabled = true;

        corpseImage.sprite = playerSpriteRenderer.sprite;
        corpseImage.color = playerSpriteRenderer.color;
        corpseImage.SetNativeSize();

        float flipX = playerSpriteRenderer.flipX ? -1f : 1f;
        float flipY = playerSpriteRenderer.flipY ? -1f : 1f;
        corpseImage.rectTransform.localScale = new Vector3(flipX * Mathf.Abs(playerScale.x), flipY * Mathf.Abs(playerScale.y), 1f);

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            corpseImage.rectTransform.anchoredPosition = Vector2.zero;
            focusGlowImage.rectTransform.anchoredPosition = Vector2.zero;
            return;
        }

        Vector2 screenPoint = mainCam.WorldToScreenPoint(playerWorldPosition);
        RectTransform canvasRect = canvas.transform as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint))
        {
            corpseImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            corpseImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            corpseImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            corpseImage.rectTransform.anchoredPosition = localPoint;

            focusGlowImage.rectTransform.anchoredPosition = localPoint;

            float corpseWidth = corpseImage.rectTransform.rect.width * Mathf.Abs(corpseImage.rectTransform.localScale.x);
            float corpseHeight = corpseImage.rectTransform.rect.height * Mathf.Abs(corpseImage.rectTransform.localScale.y);
            float focusSize = Mathf.Max(minFocusSize, Mathf.Max(corpseWidth, corpseHeight) * focusSizeMultiplier);
            focusGlowImage.rectTransform.sizeDelta = new Vector2(focusSize, focusSize);
        }
    }

    private void ConfigureRetryButtonLabel()
    {
        retryButtonText.text = "Riprova";
    }

    private void RetryFromCheckpoint()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string sceneRef = !string.IsNullOrEmpty(activeScene.path) ? activeScene.path : activeScene.name;
        Debug.Log("🔁 Retry premuto. Ricarico scena: " + sceneRef);

        Time.timeScale = 1f;
        if (SaveSystem.TryLoadForCurrentScene(out _))
        {
            CheckpointManager.CancelPendingRespawn();
            Debug.Log("💾 Retry: uso il save della scena corrente.");
        }
        else
        {
            CheckpointManager.RequestRespawnAtCheckpoint();
            Debug.Log("📍 Retry: nessun save scena, uso checkpoint.");
        }

        bool loaded = false;
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex, LoadSceneMode.Single);
            loaded = true;
        }

#if UNITY_EDITOR
        if (!loaded && !string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.LoadSceneInPlayMode(activeScene.path, new LoadSceneParameters(LoadSceneMode.Single));
            loaded = true;
        }
#endif

        if (!loaded && !string.IsNullOrEmpty(activeScene.name))
        {
            SceneManager.LoadScene(activeScene.name, LoadSceneMode.Single);
            loaded = true;
        }

        if (!loaded)
        {
            Debug.LogError("❌ Retry fallito: impossibile ricaricare la scena corrente.");
        }
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        CheckpointManager.CancelPendingRespawn();
        SceneManager.LoadScene(0);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject ev = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        ev.transform.SetParent(transform, false);
    }

    private static void StretchToFullScreen(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null)
        {
            return cachedCircleSprite;
        }

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "DeathFocusCircle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        cachedCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedCircleSprite;
    }
}
