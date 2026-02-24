using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeathButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Background Colors")]
    [SerializeField] private Color normalBackground = new Color(0.18f, 0.05f, 0.05f, 0.9f);
    [SerializeField] private Color highlightedBackground = new Color(0.38f, 0.08f, 0.08f, 0.98f);
    [SerializeField] private Color pressedBackground = new Color(0.55f, 0.12f, 0.12f, 1f);

    [Header("Text Colors")]
    [SerializeField] private Color normalText = new Color(0.88f, 0.88f, 0.88f, 1f);
    [SerializeField] private Color highlightedText = new Color(1f, 0.96f, 0.72f, 1f);
    [SerializeField] private Color pressedText = new Color(1f, 1f, 1f, 1f);

    private bool isHighlighted;
    private bool isPressed;

    public void Configure(
        Image targetBackground,
        TextMeshProUGUI targetLabel,
        Color normalBg,
        Color hoverBg,
        Color downBg,
        Color normalLabel,
        Color hoverLabel,
        Color downLabel)
    {
        background = targetBackground;
        label = targetLabel;
        normalBackground = normalBg;
        highlightedBackground = hoverBg;
        pressedBackground = downBg;
        normalText = normalLabel;
        highlightedText = hoverLabel;
        pressedText = downLabel;
        ApplyVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHighlighted = true;
        ApplyVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHighlighted = false;
        isPressed = false;
        ApplyVisuals();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isHighlighted = true;
        ApplyVisuals();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isHighlighted = false;
        isPressed = false;
        ApplyVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        ApplyVisuals();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (background == null || label == null)
        {
            return;
        }

        if (isPressed)
        {
            background.color = pressedBackground;
            label.color = pressedText;
            return;
        }

        if (isHighlighted)
        {
            background.color = highlightedBackground;
            label.color = highlightedText;
            return;
        }

        background.color = normalBackground;
        label.color = normalText;
    }
}
