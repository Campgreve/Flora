using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PicnicSavePoint : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private bool autoSaveOnEnter = false;
    [SerializeField] private float saveCooldown = 1f;
    [SerializeField] private string promptText = "Premi E per salvare";
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Vector3 respawnOffset = Vector3.zero;

    [Header("Optional UI")]
    [SerializeField] private TextMeshProUGUI worldPrompt;

    private GameObject playerInRange;
    private float lastSaveTime = -999f;

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
        UpdatePrompt(false, string.Empty);
    }

    private void Update()
    {
        if (playerInRange == null)
        {
            return;
        }

        if (GameInput.IsConfirmPressedThisFrame())
        {
            TrySave();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = other.gameObject;
        UpdatePrompt(true, promptText);

        if (autoSaveOnEnter)
        {
            TrySave();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (playerInRange == null || other.gameObject != playerInRange)
        {
            return;
        }

        playerInRange = null;
        UpdatePrompt(false, string.Empty);
    }

    public bool CanInteract()
    {
        return playerInRange != null;
    }

    public void Interact()
    {
        TrySave();
    }

    private void TrySave()
    {
        if (playerInRange == null)
        {
            return;
        }

        if (Time.unscaledTime - lastSaveTime < saveCooldown)
        {
            return;
        }

        PlayerHealth playerHealth = playerInRange.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.HealToFull();
        }

        Vector3 savePos = respawnPoint != null ? respawnPoint.position : transform.position + respawnOffset;

        if (SaveSystem.SavePlayer(playerInRange, savePos))
        {
            lastSaveTime = Time.unscaledTime;
            UpdatePrompt(true, "Salvato!");
            CancelInvoke(nameof(ResetPromptText));
            Invoke(nameof(ResetPromptText), 1.2f);
        }
    }

    private void ResetPromptText()
    {
        if (playerInRange != null)
        {
            UpdatePrompt(true, promptText);
        }
    }

    private void UpdatePrompt(bool visible, string text)
    {
        if (worldPrompt == null)
        {
            return;
        }

        worldPrompt.gameObject.SetActive(visible);
        worldPrompt.text = text;
    }
}
