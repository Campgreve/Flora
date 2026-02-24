using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LeverSwitch : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [SerializeField] private string leverId = "lever_01";

    [Header("Hub Link")]
    [SerializeField] private HubGateController hubController;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer leverRenderer;
    [SerializeField] private Color inactiveColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private GameObject activatedFxObject;
    [SerializeField] private Animator leverAnimator;
    [SerializeField] private string activateTrigger = "Activate";

    [Header("Audio")]
    [SerializeField] private string activateSfx = "Switch";

    private bool isActivated;

    public string LeverId => leverId;
    public bool IsActivated => isActivated;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    private void Start()
    {
        if (leverRenderer == null)
        {
            leverRenderer = GetComponent<SpriteRenderer>();
        }

        ApplyVisualState();
    }

    public bool CanInteract()
    {
        return !isActivated;
    }

    public void Interact()
    {
        Activate();
    }

    public void Activate()
    {
        if (isActivated)
        {
            return;
        }

        isActivated = true;
        ApplyVisualState();

        if (!string.IsNullOrWhiteSpace(activateSfx))
        {
            SoundEffectManager.Play(activateSfx, true);
        }

        if (hubController != null)
        {
            hubController.NotifyLeverActivated(this);
        }
    }

    public void SetHubController(HubGateController controller)
    {
        hubController = controller;
    }

    private void ApplyVisualState()
    {
        if (leverRenderer != null)
        {
            leverRenderer.color = isActivated ? activeColor : inactiveColor;
        }

        if (activatedFxObject != null)
        {
            activatedFxObject.SetActive(isActivated);
        }

        if (leverAnimator != null && isActivated && !string.IsNullOrWhiteSpace(activateTrigger))
        {
            leverAnimator.SetTrigger(activateTrigger);
        }
    }
}
