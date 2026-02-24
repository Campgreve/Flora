using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsMenuUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Runtime UI")]
    [SerializeField] private int sortingOrder = 12000;

    private Canvas canvas;
    private GameObject root;
    private TextMeshProUGUI leftPageText;
    private TextMeshProUGUI rightPageText;
    private TMP_FontAsset antiquityFont;
    private bool isOpen;

    private readonly string[] bossIds =
    {
        "boss_forest_fire",
        "boss_polluted_waters",
        "boss_nuclear"
    };

    private readonly string[] bossNames =
    {
        "Foresta Infuocata",
        "Acque Inquinate",
        "Nucleare"
    };

    private readonly string[] flowerIds =
    {
        "flower_forest",
        "flower_water",
        "flower_nuclear"
    };

    private void Start()
    {
        BuildUi();
        root.SetActive(false);
    }

    private void Update()
    {
        if (PlayerDeathScreen.IsVisible)
        {
            if (isOpen)
            {
                Close();
            }
            return;
        }

        if (Input.GetKeyDown(toggleKey) || GameInput.IsStatsPressedThisFrame())
        {
            Toggle();
        }

        if (isOpen && GameInput.IsCancelPressedThisFrame())
        {
            Close();
            return;
        }

        if (isOpen)
        {
            RefreshContent();
        }
    }

    private void BuildUi()
    {
        if (canvas != null)
        {
            return;
        }

        antiquityFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/antiquity-print SDF");

        GameObject canvasGo = new GameObject("StatsMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = new GameObject("BookRoot", typeof(RectTransform));
        root.transform.SetParent(canvasGo.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        GameObject dimGo = new GameObject("Dim", typeof(Image));
        dimGo.transform.SetParent(root.transform, false);
        Image dim = dimGo.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.58f);
        Stretch(dim.rectTransform);

        GameObject bookGo = new GameObject("Book", typeof(Image), typeof(Outline));
        bookGo.transform.SetParent(root.transform, false);
        Image bookImage = bookGo.GetComponent<Image>();
        bookImage.color = new Color(0.93f, 0.88f, 0.77f, 1f);

        Outline bookOutline = bookGo.GetComponent<Outline>();
        bookOutline.effectColor = new Color(0.18f, 0.11f, 0.07f, 0.9f);
        bookOutline.effectDistance = new Vector2(4f, -4f);

        RectTransform bookRect = bookImage.rectTransform;
        bookRect.anchorMin = new Vector2(0.5f, 0.5f);
        bookRect.anchorMax = new Vector2(0.5f, 0.5f);
        bookRect.pivot = new Vector2(0.5f, 0.5f);
        bookRect.sizeDelta = new Vector2(1480f, 790f);

        GameObject centerLineGo = new GameObject("CenterLine", typeof(Image));
        centerLineGo.transform.SetParent(bookGo.transform, false);
        Image centerLine = centerLineGo.GetComponent<Image>();
        centerLine.color = new Color(0.2f, 0.13f, 0.09f, 0.7f);
        RectTransform lineRect = centerLine.rectTransform;
        lineRect.anchorMin = new Vector2(0.5f, 0.06f);
        lineRect.anchorMax = new Vector2(0.5f, 0.94f);
        lineRect.sizeDelta = new Vector2(6f, 0f);

        GameObject titleGo = new GameObject("Title", typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(bookGo.transform, false);
        TextMeshProUGUI title = titleGo.GetComponent<TextMeshProUGUI>();
        SetupText(title, 50f, TextAlignmentOptions.Center, new Color(0.2f, 0.11f, 0.07f, 1f));
        title.text = "Chronicle of the Dream";
        title.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(1100f, 110f);

        GameObject leftPageGo = new GameObject("LeftPage", typeof(Image), typeof(Outline));
        leftPageGo.transform.SetParent(bookGo.transform, false);
        Image leftPage = leftPageGo.GetComponent<Image>();
        leftPage.color = new Color(0.96f, 0.91f, 0.81f, 1f);
        Outline leftOutline = leftPageGo.GetComponent<Outline>();
        leftOutline.effectColor = new Color(0.26f, 0.16f, 0.1f, 0.55f);
        leftOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform leftRect = leftPage.rectTransform;
        leftRect.anchorMin = new Vector2(0.04f, 0.08f);
        leftRect.anchorMax = new Vector2(0.48f, 0.86f);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;

        GameObject rightPageGo = new GameObject("RightPage", typeof(Image), typeof(Outline));
        rightPageGo.transform.SetParent(bookGo.transform, false);
        Image rightPage = rightPageGo.GetComponent<Image>();
        rightPage.color = new Color(0.96f, 0.91f, 0.81f, 1f);
        Outline rightOutline = rightPageGo.GetComponent<Outline>();
        rightOutline.effectColor = new Color(0.26f, 0.16f, 0.1f, 0.55f);
        rightOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform rightRect = rightPage.rectTransform;
        rightRect.anchorMin = new Vector2(0.52f, 0.08f);
        rightRect.anchorMax = new Vector2(0.96f, 0.86f);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;

        GameObject leftTextGo = new GameObject("LeftPageText", typeof(TextMeshProUGUI));
        leftTextGo.transform.SetParent(leftPageGo.transform, false);
        leftPageText = leftTextGo.GetComponent<TextMeshProUGUI>();
        SetupText(leftPageText, 28f, TextAlignmentOptions.TopLeft, new Color(0.2f, 0.12f, 0.08f, 1f));
        RectTransform leftTextRect = leftPageText.rectTransform;
        leftTextRect.anchorMin = new Vector2(0.07f, 0.08f);
        leftTextRect.anchorMax = new Vector2(0.93f, 0.92f);
        leftTextRect.offsetMin = Vector2.zero;
        leftTextRect.offsetMax = Vector2.zero;

        GameObject rightTextGo = new GameObject("RightPageText", typeof(TextMeshProUGUI));
        rightTextGo.transform.SetParent(rightPageGo.transform, false);
        rightPageText = rightTextGo.GetComponent<TextMeshProUGUI>();
        SetupText(rightPageText, 24f, TextAlignmentOptions.TopLeft, new Color(0.2f, 0.12f, 0.08f, 1f));
        RectTransform rightTextRect = rightPageText.rectTransform;
        rightTextRect.anchorMin = new Vector2(0.07f, 0.08f);
        rightTextRect.anchorMax = new Vector2(0.93f, 0.92f);
        rightTextRect.offsetMin = Vector2.zero;
        rightTextRect.offsetMax = Vector2.zero;
    }

    private void SetupText(TextMeshProUGUI text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.richText = true;
        if (antiquityFont != null)
        {
            text.font = antiquityFont;
        }
    }

    private void Toggle()
    {
        if (isOpen)
        {
            Close();
            return;
        }

        isOpen = true;
        root.SetActive(true);
        Time.timeScale = 0f;
        RefreshContent();
    }

    private void Close()
    {
        isOpen = false;
        root.SetActive(false);
        Time.timeScale = 1f;
    }

    private void RefreshContent()
    {
        PlayerHealth health = FindObjectOfType<PlayerHealth>();
        PlayerExperience experience = FindObjectOfType<PlayerExperience>();
        GameProgress progress = GameProgress.Instance;

        float hpNow = health != null ? health.GetCurrentHealth() : 0f;
        float hpMax = health != null ? health.GetMaxHealth() : 0f;
        int lvl = experience != null ? experience.currentLevel : 1;
        int xp = experience != null ? experience.currentXP : 0;
        int xpNext = experience != null ? experience.xpToNextLevel : 100;
        float xpPct = xpNext > 0 ? (100f * xp / xpNext) : 0f;

        int visibleBossCount = bossIds.Length;
        int visibleDefeated = 0;
        int visibleFlowers = 0;
        for (int i = 0; i < visibleBossCount; i++)
        {
            if (progress.HasDefeatedBoss(bossIds[i])) visibleDefeated++;
            if (progress.HasFlower(flowerIds[i])) visibleFlowers++;
        }

        leftPageText.text =
            "<size=42><b>Hero Ledger</b></size>\n\n" +
            $"<b>Level</b>: {lvl}\n" +
            $"<b>Health</b>: {hpNow:0}/{hpMax:0}\n" +
            $"<b>XP</b>: {xp}/{xpNext} ({xpPct:0}%)\n\n" +
            $"<b>Boss Defeated</b>: {visibleDefeated}/{visibleBossCount}\n" +
            $"<b>Flowers Collected</b>: {visibleFlowers}/{visibleBossCount}\n\n" +
            "<size=24>Close: <b>TAB</b> or <b>O</b></size>";

        string quest = "<size=38><b>Quest Chronicle</b></size>\n\n";
        for (int i = 0; i < bossIds.Length; i++)
        {
            bool bossDone = progress.HasDefeatedBoss(bossIds[i]);
            bool flowerDone = i < flowerIds.Length && progress.HasFlower(flowerIds[i]);

            string line;
            if (bossDone && flowerDone)
            {
                line = $"<color=#2F6A2D>Completato</color>";
            }
            else if (bossDone)
            {
                line = $"<color=#6B5B2A>Boss giu, fiore mancante</color>";
            }
            else
            {
                line = $"<color=#7A6A52>In corso</color>";
            }

            quest += $"• <b>{bossNames[i]}</b>\n  {line}\n\n";
        }

        rightPageText.text = quest;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
