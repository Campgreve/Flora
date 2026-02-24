using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask validLayers = ~0;
    [SerializeField] private string[] validTags = { "Player", "Pushable" };
    [SerializeField] private bool acceptTriggerColliders = false;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer plateRenderer;
    [SerializeField] private Sprite releasedSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private Color releasedColor = new Color(0.75f, 0.85f, 1f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.25f, 0.95f, 1f, 1f);
    [SerializeField] private Vector3 releasedScale = Vector3.one;
    [SerializeField] private Vector3 pressedScale = new Vector3(1f, 0.85f, 1f);

    [Header("Outputs")]
    [SerializeField] private GameObject[] activateWhenPressed;
    [SerializeField] private GameObject[] deactivateWhenPressed;

    [Header("Events")]
    [SerializeField] private UnityEvent onPressed;
    [SerializeField] private UnityEvent onReleased;
    [SerializeField] private UnityEvent<bool> onPressedStateChanged;

    private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
    private bool isPressed;

    public bool IsPressed => isPressed;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        if (plateRenderer == null)
        {
            plateRenderer = GetComponent<SpriteRenderer>();
        }

        ApplyVisualState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidActivator(other))
        {
            return;
        }

        occupants.Add(other);
        RefreshState();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (occupants.Remove(other))
        {
            RefreshState();
        }
    }

    private bool IsValidActivator(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (!acceptTriggerColliders && other.isTrigger)
        {
            return false;
        }

        if (((1 << other.gameObject.layer) & validLayers.value) == 0)
        {
            return false;
        }

        if (validTags == null || validTags.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < validTags.Length; i++)
        {
            string tagName = validTags[i];
            if (!string.IsNullOrWhiteSpace(tagName) && other.CompareTag(tagName))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshState()
    {
        occupants.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy);
        bool newState = occupants.Count > 0;
        if (newState == isPressed)
        {
            return;
        }

        isPressed = newState;
        ApplyVisualState();
        ApplyOutputs();

        if (isPressed)
        {
            onPressed?.Invoke();
        }
        else
        {
            onReleased?.Invoke();
        }

        onPressedStateChanged?.Invoke(isPressed);
        Debug.Log(isPressed ? "✅ Pedana premuta" : "⬜ Pedana rilasciata");
    }

    private void ApplyVisualState()
    {
        if (plateRenderer != null)
        {
            if (isPressed && pressedSprite != null)
            {
                plateRenderer.sprite = pressedSprite;
            }
            else if (!isPressed && releasedSprite != null)
            {
                plateRenderer.sprite = releasedSprite;
            }

            plateRenderer.color = isPressed ? pressedColor : releasedColor;
        }

        transform.localScale = isPressed ? pressedScale : releasedScale;
    }

    private void ApplyOutputs()
    {
        for (int i = 0; i < activateWhenPressed.Length; i++)
        {
            if (activateWhenPressed[i] != null)
            {
                activateWhenPressed[i].SetActive(isPressed);
            }
        }

        for (int i = 0; i < deactivateWhenPressed.Length; i++)
        {
            if (deactivateWhenPressed[i] != null)
            {
                deactivateWhenPressed[i].SetActive(!isPressed);
            }
        }
    }
}
