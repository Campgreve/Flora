using UnityEngine;

public class DualPlateDoor : MonoBehaviour
{
    [Header("Plates")]
    [SerializeField] private PressurePlate plateA;
    [SerializeField] private PressurePlate plateB;

    [Header("Door")]
    [SerializeField] private GameObject doorRoot;
    [SerializeField] private Collider2D doorCollider;
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private bool hideDoorWhenOpen = true;
    [SerializeField] private bool stayOpenForever = false;

    [Header("Audio")]
    [SerializeField] private string openSfx = "DoorOpen";
    [SerializeField] private string closeSfx = "DoorSlam";

    private bool isOpen;
    private bool lockedOpen;

    private void Start()
    {
        ApplyDoorState(false, true);
    }

    private void Update()
    {
        if (lockedOpen)
        {
            return;
        }

        bool shouldOpen = plateA != null && plateB != null && plateA.IsPressed && plateB.IsPressed;
        if (shouldOpen == isOpen)
        {
            return;
        }

        ApplyDoorState(shouldOpen, false);
    }

    private void ApplyDoorState(bool open, bool silent)
    {
        isOpen = open;
        if (isOpen && stayOpenForever)
        {
            lockedOpen = true;
        }

        if (doorCollider != null)
        {
            doorCollider.enabled = !open;
        }

        if (doorRenderer != null)
        {
            doorRenderer.enabled = !(open && hideDoorWhenOpen);
        }

        if (doorRoot != null && hideDoorWhenOpen)
        {
            doorRoot.SetActive(!open);
        }

        if (silent)
        {
            return;
        }

        if (open && !string.IsNullOrWhiteSpace(openSfx))
        {
            SoundEffectManager.Play(openSfx, true);
        }
        else if (!open && !string.IsNullOrWhiteSpace(closeSfx))
        {
            SoundEffectManager.Play(closeSfx, true);
        }
    }
}
